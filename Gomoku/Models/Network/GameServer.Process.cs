using Gomoku.Models.Common;
using Gomoku.Models.Domain;
using Gomoku.Models.DTO;
using Gomoku.Models.Messages;
using Gomoku.Models.Network;
using System.Net;
using System.Net.Sockets;

namespace Gomoku.Models
{
    public partial class GameServer
    {
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
                    TcpClient client = await _listener!.AcceptTcpClientAsync(ct);

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
            session.OnDataReceived += async (s, d) => await ProcessDataAsync(s, d);
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
        internal async Task<bool> ProcessDBAsync(INetworkSession session, Player sender, GameData data)
        {
            switch (data)
            {
                case RequestJoinData rjd:
                    if (sender.Id != -1) // 이미 로그인 완료한 계정
                        return false;
                    if (rjd.AuthInfo.LoginType == LoginType.Login)
                        return await ProcessLoginAsync(session, sender, rjd);
                    break;
                case RequestCreateAccountData rcad:
                    if (sender.Id != -1)
                        return false;
                    return await ProcessCreateAccountAsync(session, sender, rcad);
                case RequestDeleteAccountData rdad:
                    if (sender.AccountId != rdad.UserId)    // 본인 아닌 사람이 삭제 요청한 경우
                        return false;
                    return await ProcessDeleteAccountAsync(session, sender, rdad);
                case RequestGameStartData rgsd:
                    if (_blackPlayer != session)
                    {   // 흑 플레이어가 요청한게 아니라면
                        Logger.Error($"게임 시작 거부: 흑 플레이어 아님");
                        return false;
                    }

                    var black = _sessions[_blackPlayer!];
                    var white = _sessions[_whitePlayer!];

                    black.LeftCancelLast = _connectionOption!.LeftCancelCount;
                    white.LeftCancelLast = _connectionOption!.LeftCancelCount;

                    Record? BlackRelativeRecord = null;
                    Record? WhiteRelativeRecord = null;

                    if (black.Id != 1 && white.Id != 1) // 둘 중 한명이라도 게스트가 아니라면 상대 전적 불러오기
                        (BlackRelativeRecord, WhiteRelativeRecord) = await _databaseService.GetRelativeRecordsAsync(black, white);

                    var gamestartdata = new GameStartedData
                    {
                        BlackPlayer = black,
                        WhitePlayer = white,
                        BlackRelativeRecord = BlackRelativeRecord,
                        WhiteRelativeRecord = WhiteRelativeRecord
                    };

                    AddBroadcast(gamestartdata);
                    StartGame();
                    break;
                case ChangeNicknameRequestData cnrd:
                    if (sender.Id == 1) // 게스트 계정은 닉네임 변경 불가
                    {
                        AddUnicast(session, new ChangeNicknameResponseData
                        {
                            Accepted = false,
                            Message = "게스트 계정은 닉네임을 변경할 수 없습니다."
                        });
                        return false;
                    }

                    Logger.Info($"{sender.AccountId} 닉네임 변경 요청: {sender.Nickname} -> {cnrd.NewNickname}");

                    string newnickname = cnrd.NewNickname.Trim();
                    if (newnickname == sender.Nickname)
                    {
                        // 원래 닉네임과 같으면
                        AddUnicast(session, new ChangeNicknameResponseData
                        {
                            Accepted = false,
                            Message = "기존 닉네임과 동일합니다."
                        });
                        return false;
                    }

                    await ProcessChangeNicknameAsync(session, sender, newnickname);

                    break;

                case RequestRankingsData rrd:
                    var rankings = await _databaseService.GetPlayerRanksAsync();
                    AddUnicast(session, new RankingsData
                    {
                        Accepted = true,
                        Rankings = rankings.ToList()
                    });
                    break;
                case RequestMatchesData rmd:
                    var matches = await _databaseService.GetMatchesAsync(
                        rmd.PlayerNickname, rmd.BlackPlayerNickname, rmd.WhitePlayerNickname,
                        rmd.from, rmd.to, rmd.PageNumber, rmd.PageSize
                        );
                    AddUnicast(session, new MatchesData { Accepted = true, Matches = matches });
                    break;
                default:
                    return true;
            }
            return true;
        }

        private async Task ProcessChangeNicknameAsync(INetworkSession session, Player sender, string newnickname)
        {
            try
            {
                await _databaseService.ChangeNicknameAsync(sender.AccountId, newnickname);
                string oldnickname = sender.Nickname;
                sender.Nickname = newnickname;
                AddBroadcast(new ChangeNicknameResponseData
                {
                    Accepted = true,
                    Message = $"{oldnickname} 님이 {newnickname}(으)로 닉네임을 변경했습니다.",
                    OldNickname = oldnickname,
                    NewNickname = newnickname
                });
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

        private async Task<bool> ProcessDeleteAccountAsync(INetworkSession session, Player sender, RequestDeleteAccountData data)
        {
            try
            {
                await _databaseService.DeleteAccountAsync(data.UserId, data.PasswordHashed);
                DisconnectSession(session);
                return true;
            }
            catch (Exception e) when (e is PasswordWrongException || e is AccountNotExistException)
            {
                Logger.Info($"{sender.AccountId} 계정 삭제 실패 : {e.Message}");
                AddUnicast(session, new DeleteAccountRejectedData { Reason = e.Message });
            }
            return false;
        }
        internal async Task<bool> ProcessLoginAsync(INetworkSession session, Player sender, RequestJoinData data)
        {
            try
            {
                var authinfo = data.AuthInfo;
                var dbplayer = await _databaseService.TryLoginAsync(authinfo.UserId, authinfo.Password);

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
                return true;
            }
            catch (Exception e) when (e is PasswordWrongException || e is AccountNotExistException)
            {
                Logger.Info($"{data.AuthInfo.UserId} 로그인 실패: {e.Message}");
                AddUnicast(session, new LoginFailedData { Reason = e.Message });
            }
            return false;
        }

        private async Task<bool> ProcessCreateAccountAsync(INetworkSession session, Player sender, RequestCreateAccountData data)
        {
            try
            {
                var dbplayer = await _databaseService.CreateAccountAsync(data.UserId, data.PasswordHashed, data.Nickname);
                sender.Nickname = dbplayer.Nickname;
                sender.Id = dbplayer.Id;
                sender.AccountId = dbplayer.AccountId;
                sender.Records = dbplayer.Records;

                AfterJoinSuccess(session, sender);
                return true;
            }
            catch (Exception e) when (e is IdDuplicateException || e is NicknameDuplicateException)
            {
                Logger.Info($"{data.UserId} 회원가입 실패: {e.Message}");
                AddUnicast(session, new CreateAccountRejectedData { Reason = e.Message });
                return false;
            }

        }
        #endregion

        internal async Task ProcessDataAsync(INetworkSession session, GameData data)
        {
            Player sender = GetPlayerOrNull(session) ?? throw new InvalidOperationException("플레이어를 찾을 수 없음");

            if (data is not PingData && data is not PongData)
            {
                Logger.Debug($"서버 패킷 수신 : {data.GetType().Name}");
            }

            // false 면 ProcessDBAsync 내부에서 거부 패킷 전송 처리 완료함
            if (!await ProcessDBAsync(session, sender, data))
                return;


            lock (_gameLock)
            {
                switch (data) // 데이터 분기 처리 (서버)
                {
                    case ChatData chatData:
                        HandleChatReceive(sender, chatData);
                        break;
                    case PositionData positionData:
                        if (!manager.IsGameStarted) return;
                        HandlePlaceReceive(session, positionData);
                        break;
                    case RequestJoinData joinData: // 클라이언트 최초 접속 및 인증시
                        HandleClientRequestJoinReceive(session, sender, joinData);
                        break;

                    case GameJoinData joindata:
                        if (_blackPlayer == session || _whitePlayer == session)
                        {   // 이미 흑백 들어간 사람이라면
                            Logger.Error($"흑백 참가 거부: 이미 들어간 사람 {joindata.Player.Nickname}");
                            break;
                        }

                        if ((_blackPlayer != null && joindata.Type == PlayerType.Black)
                            || (_whitePlayer != null && joindata.Type == PlayerType.White))
                        {
                            Logger.Error($"이미 들어가있는 슬롯에 들어가려 함 {joindata.Player.Nickname}");
                            break;
                        }

                        if (joindata.Type == PlayerType.Black)
                            _blackPlayer = session;
                        else
                            _whitePlayer = session;

                        AddBroadcast(joindata);
                        break;

                    case GameLeaveData leaveData:
                        if (_blackPlayer != session && _whitePlayer != session)
                        {   // 안들어간 사람이 나가기 요청한거라면
                            Logger.Error($"흑백 나가기 거부: 이미 관전자 {leaveData.Player.Nickname}");
                            break;
                        }
                        HandleGameLeaveReceive(leaveData);
                        break;
                    case CancelLastData cancelLastData:
                        if (!manager.IsGameStarted)
                        {
                            Logger.Error($"게임 시작 안했는데 무르기 요청 {sender.Nickname}");
                            break;
                        }

                        if (_blackPlayer != session && _whitePlayer != session)
                        {
                            Logger.Error($"참가자 아닌 플레이어가 무르기 요청 {sender.Nickname}");
                            break;
                        }

                        int leftcount = sender.LeftCancelLast - 1;

                        if (leftcount < 0) // 무르기 카운트 없음
                            break;


                        sender.LeftCancelLast = leftcount;

                        cancelLastData.LeftCancelLastCount = leftcount;

                        if (manager.CancelLastStone(cancelLastData.SenderType, cancelLastData.LeftCancelLastCount))
                        {
                            AddBroadcast(cancelLastData);
                        }
                        break;
                }
            }
        }

        private void HandleGameLeaveReceive(GameLeaveData leaveData)
        {
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

        private void HandleClientRequestJoinReceive(INetworkSession session, Player sender, RequestJoinData joinData)
        {
            // 게스트 모드일시, 인증 모드는 ProcessDBAsync에서 처리 후 여기로 옴
            if (joinData.AuthInfo.LoginType == LoginType.Guest)
            {
                sender.Nickname = GenerateGuestNickname(session);
                sender.Id = 1;
                sender.AccountId = "Guest";
                Logger.Info($"게스트 클라이언트 접속됨: {sender.Nickname}");
            }
            AfterJoinSuccess(session, sender);
        }

        private void HandlePlaceReceive(INetworkSession session, PositionData positionData)
        {
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

        private void HandleChatReceive(Player sender, ChatData chatData)
        {
            chatData.Sender.Nickname = sender.Nickname; // 닉네임 바꿔서 패킷 전송해도 그냥 서버에서 저장된 닉네임으로
            Logger.Info($"채팅 수신 : {chatData.Sender.Nickname}:{chatData.Message}");
            AddBroadcast(chatData);
        }
    }
}
