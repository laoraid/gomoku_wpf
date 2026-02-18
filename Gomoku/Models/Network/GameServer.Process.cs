/*
 * GameServer.Process.cs
 * 게임 서버에서 수신 패킷 처리 부분
 * 
 * 수신 처리 흐름
 * 수신 처리 메서드 - 비동기 작업은 멀티스레드로 처리, 동기 작업은 채널로 들어감
 * 채널에선 동기 작업 수행(게임 진행, 모델 수정 등) - 순서가 중요한 작업은 동기 작업으로
 * 
 * 수신 처리 등록 방법
 * 비동기 작업이 포함되었다면 - 생성자의 _dbHandler에 작업 메서드 등록
 * 동기 작업만 있다면 - 생성자의 _logicHandler에 작업 메서드 등록
 * 
 * 작업 메서드 인수 - INetworkSession, Player, GameData
 */
using Gomoku.Models.Common;
using Gomoku.Models.Domain;
using Gomoku.Models.DTO;
using Gomoku.Models.Messages;
using Gomoku.Models.Network;
using System.Net;
using System.Net.Sockets;

namespace Gomoku.Models
{
    public delegate void SyncHandler(INetworkSession session, Player sender, GameData data);
    public delegate Task AsyncHandler(INetworkSession session, Player sender, GameData data);
    public record struct ProcessWork(INetworkSession Session,
        Player Player, GameData Data, SyncHandler SyncAction);
    public partial class GameServer
    {
        internal async Task ProcessDataAsync(INetworkSession session, GameData data)
        {
            // 수신시 최초 호출 메서드
            Player sender = GetPlayerOrNull(session) ?? throw new InvalidOperationException("플레이어를 찾을 수 없음");

            if (data is not PingData && data is not PongData)
            {
                Logger.Debug($"서버 패킷 수신 : {data.GetType().Name}");
            }

            if (data is IReadOnlyRequest or IDbRequiredRequest)
            {
                // 단순 DB 조회 요청이면 비동기로 실행
                // DB 조회 + 로직 수정 작업이면 핸들러 메서드가 DB 조회 후 로직 수정 작업을 채널에 넣음
                if (_dbHandler.TryGetValue(data.GetType(), out var dbhandler))
                {
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await dbhandler(session, sender, data);
                        }
                        catch (Exception ex)
                        {
                            Logger.Error($"서버 DB 처리 중 예외: {ex.Message}");
                        }
                    });
                }
                return;
            }

            if (_logicHandler.TryGetValue(data.GetType(), out var handler))
            {
                await _processChannel.Writer.WriteAsync(new ProcessWork(session, sender, data, handler));
                return;
            }

            Logger.Error($"처리되지 않은 패킷 : {data.GetType().Name}");
        }
        private async Task ProcessLoopAsync(CancellationToken ct)
        {
            await foreach (var item in _processChannel.Reader.ReadAllAsync(ct).ConfigureAwait(false))
            {
                try
                {
                    item.SyncAction(item.Session, item.Player, item.Data);
                }
                catch (Exception ex)
                {
                    Logger.Error($"서버 동기 액션 처리 중 에러: {ex.Message}");
                }
            }
        }

        #region 연결
        public async Task StartAsync(ConnectionOption option)
        {
            _connectionOption = option;

            _listener = new TcpListener(IPAddress.Any, _connectionOption.port);
            _listener.Start();
            Logger.System($"서버 시작 됨. 포트 : {_connectionOption.port}");

            _acceptTask = Task.Run(() => AccpetClientsAsync(_ServerCts.Token)); // 비동기적으로 클라이언트 수락 시작

            _heartbeattimer.Start();
        }

        private async Task AccpetClientsAsync(CancellationToken ct)
        {
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    TcpClient client = await _listener!.AcceptTcpClientAsync(ct).ConfigureAwait(false);

                    var newSession = _sessionFactory.Create(client);

                    AddSession(newSession);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Logger.Error($"서버: 클라이언트 연결 수락 중 오류 발생 : {ex.Message}");
            }
        }

        internal Player AddSession(INetworkSession session)
        {
            session.OnDataReceived += async (s, d) => await ProcessDataAsync(s, d).ConfigureAwait(false);
            session.OnDisconnected += HandleClientDisconnected;

            Player tempplayer = new Player();
            tempplayer.Nickname = "NULL";
            tempplayer.LeftCancelLast = _connectionOption!.LeftCancelCount;
            tempplayer.Id = -1; // 인증 전
            tempplayer.AccountId = "NULL";

            _sessions.TryAdd(session, tempplayer);

            Logger.System($"서버: 새 클라이언트 연결됨. 세션 ID : {session.SessionId}");

            return tempplayer;
        }

        private void AfterJoinSuccess(INetworkSession session, Player sender)
        {
            session.IsAuthenticated = true;
            var res = new ClientJoinResponseData() // 접속 확인 응답
            {
                Accepted = true,
                Me = sender,
                Users = _sessions.Values.ToList()
            };

            AddUnicast(session, res);

            var join_broadcast = new ClientJoinData() // 모두에게 접속했다고 방송
            {
                Player = sender
            };

            AddBroadcast(join_broadcast);

            var syncdata = new GameSyncData() // 게임 진행 데이터 전송
            {
                SyncData = new GameSyncMessage(manager.IsGameStarted, manager.Board.GetHistory(), manager.CurrentPlayer,
                manager.Rules.Select(r => r.RuleInfo), GetPlayerOrNull(_blackPlayer), GetPlayerOrNull(_whitePlayer))
            };

            AddUnicast(session, syncdata);
        }
        #endregion



        #region DB 연동
        private async Task ProcessRequestMatchMoveAsync(INetworkSession session, Player sender, RequestMatchMoveData rmmd)
        {
            var moves = await _databaseService.GetMatchMovesAsync(rmmd.Match);
            AddUnicast(session, new MatchMoveData { Accepted = true, Moves = moves });
        }

        private async Task ProcessRequestMatchesAsync(INetworkSession session, Player sender, RequestMatchesData rmd)
        {
            var matches = await _databaseService.GetMatchesAsync(
                                    rmd.PlayerNickname, rmd.BlackPlayerNickname, rmd.WhitePlayerNickname,
                                    rmd.from, rmd.to, rmd.PageNumber, rmd.PageSize
                                    );
            AddUnicast(session, new MatchesData { Accepted = true, Matches = matches });
        }

        private async Task ProcessRequestRankingsAsync(INetworkSession session, Player sender, RequestRankingsData data)
        {
            var rankings = await _databaseService.GetPlayerRanksAsync();
            AddUnicast(session, new RankingsData
            {
                Accepted = true,
                Rankings = rankings.ToList()
            });
        }

        private async Task ProcessGameStartAsync(INetworkSession session, Player sender, RequestGameStartData data)
        {
            if (_blackPlayer != session)
            {   // 흑 플레이어가 요청한게 아니라면
                Logger.Error($"게임 시작 거부: 흑 플레이어 아님");
                return;
            }

            var black = _sessions[_blackPlayer!];
            var white = _sessions[_whitePlayer!];

            Record? BlackRelativeRecord = null;
            Record? WhiteRelativeRecord = null;

            if (black.Id != 1 && white.Id != 1) // 둘 중 한명이라도 게스트가 아니라면 상대 전적 불러오기
                (BlackRelativeRecord, WhiteRelativeRecord) = await _databaseService.GetRelativeRecordsAsync(black, white);

            SyncHandler syncAction = (session, sender, data) =>
            {

                black.LeftCancelLast = _connectionOption!.LeftCancelCount;
                white.LeftCancelLast = _connectionOption!.LeftCancelCount;


                var gamestartdata = new GameStartedData
                {
                    BlackPlayer = black,
                    WhitePlayer = white,
                    BlackRelativeRecord = BlackRelativeRecord,
                    WhiteRelativeRecord = WhiteRelativeRecord
                };

                StartGame();
                AddBroadcast(gamestartdata);
            };

            await _processChannel.Writer.WriteAsync(new ProcessWork(session, sender, data, syncAction));
        }

        private async Task ProcessChangeNicknameAsync(INetworkSession session, Player sender, ChangeNicknameRequestData data)
        {
            if (sender.Id == 1) // 게스트 계정은 닉네임 변경 불가
            {
                AddUnicast(session, new ChangeNicknameResponseData
                {
                    Accepted = false,
                    Message = "게스트 계정은 닉네임을 변경할 수 없습니다."
                });
                return;
            }

            string newnickname = data.NewNickname;
            Logger.Info($"{sender.AccountId} 닉네임 변경 요청: {sender.Nickname} -> {newnickname}");

            try
            {
                await _databaseService.ChangeNicknameAsync(sender.AccountId, newnickname);

                SyncHandler syncAction = (session, sender, data) =>
                {
                    string oldnickname = sender.Nickname;
                    sender.Nickname = ((ChangeNicknameRequestData)data).NewNickname;
                    AddBroadcast(new ChangeNicknameResponseData
                    {
                        Accepted = true,
                        Message = $"{oldnickname} 님이 {newnickname}(으)로 닉네임을 변경했습니다.",
                        OldNickname = oldnickname,
                        NewNickname = newnickname
                    });
                };

                await _processChannel.Writer.WriteAsync(new ProcessWork(session, sender, data, syncAction));
            }
            catch (NicknameDuplicateException nde)
            {
                Logger.Info($"{sender.AccountId} 닉네임 변경 실패: {nde.Message}");
                AddUnicast(session, new ChangeNicknameResponseData
                {
                    Accepted = false,
                    Message = nde.Message
                });
            }
        }

        private async Task ProcessDeleteAccountAsync(INetworkSession session, Player sender, RequestDeleteAccountData data)
        {
            try
            {
                await _databaseService.DeleteAccountAsync(data.UserId, data.PasswordHashed);
                SyncHandler syncAction = (session, sender, data) =>
                {
                    DisconnectSession(session);
                };

                await _processChannel.Writer.WriteAsync(new ProcessWork(session, sender, data, syncAction));
            }
            catch (Exception e) when (e is PasswordWrongException || e is AccountNotExistException)
            {
                Logger.Info($"{sender.AccountId} 계정 삭제 실패 : {e.Message}");
                AddUnicast(session, new DeleteAccountRejectedData { Reason = e.Message });
            }
        }
        internal async Task ProcessLoginAsync(INetworkSession session, Player sender, RequestJoinData data)
        {
            if (sender.Id != -1) // 이미 로그인한 계정
                return;

            if (data.AuthInfo.LoginType == LoginType.Guest)
            {
                SyncHandler syncAction = (session, sender, data) =>
                {
                    sender.Nickname = GenerateGuestNickname(session);
                    sender.Id = 1;
                    sender.AccountId = "Guest";
                    Logger.Info($"게스트 클라이언트 접속됨: {sender.Nickname}");
                    AfterJoinSuccess(session, sender);
                };

                await _processChannel.Writer.WriteAsync(new ProcessWork(session, sender, data, syncAction));
                return;
            }

            try
            {
                var authinfo = data.AuthInfo;
                var dbplayer = await _databaseService.TryLoginAsync(authinfo.UserId, authinfo.Password);

                SyncHandler syncAction = (session, sender, data) =>
                {
                    var pair = _sessions.FirstOrDefault((pair) => pair.Value.Id == dbplayer.Id);

                    if (pair.Key != null)
                    {
                        DisconnectSession(pair.Key);
                        Logger.Info("중복 로그인 감지로 기존 플레이어 킥");
                    }
                    sender.Id = dbplayer.Id;
                    sender.AccountId = dbplayer.AccountId;
                    sender.Records = dbplayer.Records;
                    sender.Nickname = dbplayer.Nickname;
                    AfterJoinSuccess(session, sender);
                };

                await _processChannel.Writer.WriteAsync(new ProcessWork(session, sender, data, syncAction));
            }
            catch (Exception e) when (e is PasswordWrongException || e is AccountNotExistException)
            {
                Logger.Info($"{data.AuthInfo.UserId} 로그인 실패: {e.Message}");
                AddUnicast(session, new LoginFailedData { Reason = e.Message });
            }
        }

        private async Task ProcessCreateAccountAsync(INetworkSession session, Player sender, RequestCreateAccountData data)
        {
            if (sender.Id != -1)
                return;

            try
            {
                var dbplayer = await _databaseService.CreateAccountAsync(data.UserId, data.PasswordHashed, data.Nickname);

                SyncHandler syncAction = (session, sender, data) =>
                {
                    sender.Nickname = dbplayer.Nickname;
                    sender.Id = dbplayer.Id;
                    sender.AccountId = dbplayer.AccountId;
                    sender.Records = dbplayer.Records;

                    AfterJoinSuccess(session, sender);
                };

                await _processChannel.Writer.WriteAsync(new ProcessWork(session, sender, data, syncAction));
            }
            catch (Exception e) when (e is IdDuplicateException || e is NicknameDuplicateException)
            {
                Logger.Info($"{data.UserId} 회원가입 실패: {e.Message}");
                AddUnicast(session, new CreateAccountRejectedData { Reason = e.Message });
            }

        }
        #endregion

        #region 동기 처리
        private void HandleCancelLastReceive(INetworkSession session, Player sender, CancelLastData cancelLastData)
        {

            if (!manager.IsGameStarted)
            {
                Logger.Error($"게임 시작 안했는데 무르기 요청 {sender.Nickname}");
                return;
            }

            if (_blackPlayer != session && _whitePlayer != session)
            {
                Logger.Error($"참가자 아닌 플레이어가 무르기 요청 {sender.Nickname}");
                return;
            }

            int leftcount = sender.LeftCancelLast - 1;

            if (leftcount < 0) // 무르기 카운트 없음
                return;


            sender.LeftCancelLast = leftcount;

            cancelLastData.LeftCancelLastCount = leftcount;

            if (manager.CancelLastStone(cancelLastData.SenderType, cancelLastData.LeftCancelLastCount))
            {
                AddBroadcast(cancelLastData);
            }
            return;
        }

        private void HandleGameJoinReceive(INetworkSession session, Player sender, GameJoinData joindata)
        {
            if (_blackPlayer == session || _whitePlayer == session)
            {   // 이미 흑백 들어간 사람이라면
                Logger.Error($"흑백 참가 거부: 이미 들어간 사람 {joindata.Player.Nickname}");
                return;
            }

            if ((_blackPlayer != null && joindata.Type == PlayerType.Black)
                || (_whitePlayer != null && joindata.Type == PlayerType.White))
            {
                Logger.Error($"이미 들어가있는 슬롯에 들어가려 함 {joindata.Player.Nickname}");
                return;
            }

            if (joindata.Type == PlayerType.Black)
                _blackPlayer = session;
            else
                _whitePlayer = session;

            AddBroadcast(joindata);
            return;
        }

        private void HandleGameLeaveReceive(INetworkSession session, Player sender, GameLeaveData leaveData)
        {
            if (_blackPlayer != session && _whitePlayer != session)
            {   // 안들어간 사람이 나가기 요청한거라면
                Logger.Error($"흑백 나가기 거부: 이미 관전자 {leaveData.Player.Nickname}");
                return;
            }

            PlayerType winner;

            if (leaveData.Type == PlayerType.Black)
            {
                winner = PlayerType.White;
            }
            else
            {
                winner = PlayerType.Black;
            }

            manager.ForceGameEnd(winner, "게임 나감");

            if (winner == PlayerType.White)
                _blackPlayer = null;
            else
                _whitePlayer = null;


            AddBroadcast(leaveData);
        }

        private void HandlePlaceReceive(INetworkSession session, Player sender, PositionData positionData)
        {
            if (!manager.IsGameStarted) return;
            try
            {
                manager.TryPlaceStone(positionData.Move);
                _gametimer.Stop();

                var newmove = new GameMove(positionData.Move.X, positionData.Move.Y,
                    manager.Board.Count, positionData.Move.PlayerType);
                AddBroadcast(new PositionData { Move = newmove }); // catch 안되면 돌 둔것
                if (!manager.IsWin(positionData.Move))
                {
                    _gametimer.Start();
                }
            }
            catch (InvalidPlaceException)
            {
                Logger.Info($"불가능한 착수: {positionData.Move.X}, {positionData.Move.Y}");
                ResponseData response = new PlaceResponseData()
                {
                    Accepted = false,
                    Position = positionData,
                };
                AddUnicast(session, response);
            }
        }

        private void HandleChatReceive(INetworkSession session, Player sender, ChatData chatData)
        {
            chatData.Sender.Nickname = sender.Nickname; // 닉네임 바꿔서 패킷 전송해도 그냥 서버에서 저장된 닉네임으로
            Logger.Info($"채팅 수신 : {chatData.Sender.Nickname}:{chatData.Message}");
            AddBroadcast(chatData);
        }
        #endregion
    }
}
