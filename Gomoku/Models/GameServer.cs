using Gomoku.Models.DTO;
using Gomoku.Models.Interfaces;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading.Channels;
using System.Timers;

namespace Gomoku.Models
{
    public class GameServer : IGameServer
    {
        private TcpListener? _listener;

        private Channel<(INetworkSession, GameData)> _sendChannel = Channel.CreateUnbounded<(INetworkSession, GameData)>();
        private Task? _sendTask;
        // 패킷 보내기 채널
        private Task? _acceptTask;

        private readonly ConcurrentDictionary<INetworkSession, Player> _sessions = new();

        private readonly CancellationTokenSource _token = new();

        private readonly GomokuManager manager = new GomokuManager();

        private readonly object _handlelock = new object();

        private readonly INetworkSessionFactory _sessionFactory;

        private INetworkSession? _blackPlayer;
        private INetworkSession? _whitePlayer;
        private readonly System.Timers.Timer _gametimer = new System.Timers.Timer(1000);
        private readonly System.Timers.Timer _heartbeattimer = new System.Timers.Timer(5000);

        public bool IsRunning => _listener != null;

        internal ConnectionOption? _connectionOption;

        public GameServer(INetworkSessionFactory sessionFactory)
        {
            _sessionFactory = sessionFactory;

            _gametimer.Elapsed += SetTimer;
            manager.GameEnded += (gameend) => // 게임 종료 시에 모든 클라에게 결과 방송
            {
                _gametimer.Stop();
                GameEndData enddata = new GameEndData()
                {
                    EndData = gameend
                };

                AddBroadcast(enddata);
            };

            _heartbeattimer.Elapsed += async (s, e) => // 핑 송신 및 오래된 세션 정리
            {
                AddBroadcast(new PingData());

                List<KeyValuePair<INetworkSession, Player>> sessionToDisconnect = [];

                var now = DateTime.Now;

                foreach (var sessionplayer in _sessions)
                {
                    if ((now - sessionplayer.Key.LastActiveTime).TotalSeconds > 15) // 오래 응답 없는 세션 
                        sessionToDisconnect.Add(sessionplayer);
                }

                foreach (var sessionplayer in sessionToDisconnect)
                {
                    sessionplayer.Key.Disconnect();
                    AddBroadcast(new ClientExitData() { Player = sessionplayer.Value });
                    _sessions.Remove(sessionplayer.Key, out _);
                }
            };

            _sendTask = StartSenderAsync(_token.Token);
        }

        public void StartGame()
        {
            manager.StartGame();
            _gametimer.Start();
        }

        public void AddRule(Rule rule)
        {
            manager.Rules.Add(rule);
        }
        public async void SetTimer(object? sender, ElapsedEventArgs e)
        {
            TimePassedData? timepasspacket = null;
            lock (_handlelock) // 초마다 시간 까는 타이머, 다까졌으면 게임 종료, 아니면 시간 패킷 전송
            {
                if (!manager.IsGameStarted) return;

                manager.Tick(manager.CurrentPlayer);

                if (manager.CurrentPlayer == PlayerType.Black)
                {
                    if (manager.BlackSeconds <= 0)
                    {
                        manager.ForceGameEnd(PlayerType.White, "시간 초과");
                        return;
                    }
                }
                else if (manager.CurrentPlayer == PlayerType.White)
                {
                    if (manager.WhiteSeconds <= 0)
                    {
                        manager.ForceGameEnd(PlayerType.Black, "시간 초과");
                        return;
                    }
                }

                timepasspacket = new TimePassedData()
                {
                    CurrentLeftTimeSeconds = manager.CurrentPlayer == PlayerType.Black ? manager.BlackSeconds : manager.WhiteSeconds,
                    PlayerType = manager.CurrentPlayer
                };
            }

            if (timepasspacket != null)
                AddBroadcast(timepasspacket);
        }

        private Player? GetPlayerOrNull(INetworkSession? session)
        {
            if (session == null) return null;

            if (_sessions.TryGetValue(session, out Player? value))
                return value;

            return null;
        }

        public async Task StartAsync(ConnectionOption option)
        {
            _connectionOption = option;

            _listener = new TcpListener(IPAddress.Any, _connectionOption.port);
            _listener.Start();
            Logger.System($"서버 시작 됨. 포트 : {_connectionOption.port}");

            _acceptTask = Task.Run(() => AccpetClientsAsync(_token.Token)); // 비동기적으로 클라이언트 수락 시작

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
            session.OnDataReceived += (s, d) => ProcessData(s, d);
            session.OnDisconnected += HandleClientDisconnected;

            Player tempplayer = new Player();
            tempplayer.Nickname = "임시";
            tempplayer.LeftCancelLast = _connectionOption!.LeftCancelCount;

            _sessions.TryAdd(session, tempplayer);

            Logger.System($"서버: 새 클라이언트 연결됨. 세션 ID : {session.SessionId}");

            return tempplayer;
        }

        internal void ProcessData(INetworkSession session, GameData data)
        {
            Player sender = GetPlayerOrNull(session) ?? throw new InvalidOperationException("플레이어를 찾을 수 없음");

            if (data is not PingData && data is not PongData)
            {
                Logger.Debug($"서버 패킷 수신 : {data.GetType().Name}");
            }

            lock (_handlelock)
            {
                switch (data) // 데이터 분기 처리 (서버)
                {
                    case ChatData chatData:
                        Logger.Info($"채팅 수신 : {chatData.Sender.Nickname}:{chatData.Message}");
                        chatData.Sender.Nickname = sender!.Nickname; // 닉네임 바꿔서 패킷 전송해도 그냥 서버에서 저장된 닉네임으로
                        AddBroadcast(chatData);
                        break;
                    case PositionData positionData:
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
                        break;
                    case RequestJoinData joinData: // 클라이언트 최초 접속시
                        string finalnickname = GenerateUniqueNickname(session, joinData.Nickname);
                        Logger.Info($"클라이언트 접속됨: {joinData.Nickname} -> {finalnickname}");

                        sender.Nickname = finalnickname;

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
                            SyncData = new DTO.GameSyncMessage(manager.IsGameStarted, manager.Board.GetHistory(), manager.CurrentPlayer,
                            manager.Rules.Select(r => r.RuleInfo), GetPlayerOrNull(_blackPlayer), GetPlayerOrNull(_whitePlayer))
                        };

                        AddUnicast(session, syncdata);
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
                        PlayerType winner;

                        if (leaveData.Type == PlayerType.Black)
                        {
                            _blackPlayer = null;
                            winner = PlayerType.White;
                        }
                        else
                        {
                            _whitePlayer = null;
                            winner = PlayerType.Black;
                        }

                        manager.ForceGameEnd(winner, "게임 나감");

                        AddBroadcast(leaveData);
                        break;
                    case RequestGameStartData reqgamestartdata:
                        if (_blackPlayer != session)
                        {   // 흑 플레이어가 요청한게 아니라면
                            Logger.Error($"게임 시작 거부: 흑 플레이어 아님");
                            break;
                        }

                        var black = _sessions[_blackPlayer!];
                        var white = _sessions[_whitePlayer!];

                        black.LeftCancelLast = _connectionOption!.LeftCancelCount;
                        white.LeftCancelLast = _connectionOption!.LeftCancelCount;

                        var gamestartdata = new GameStartedData { BlackPlayer = black, WhitePlayer = white };

                        AddBroadcast(gamestartdata);
                        StartGame();
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
                        // TODO: 무르기는 상대편 턴에 사용, 상대편이 먼저 두면 취소

                        cancelLastData.LeftCancelLastCount = leftcount;

                        if (manager.CancelLastStone(cancelLastData.SenderType, cancelLastData.LeftCancelLastCount))
                        {
                            AddBroadcast(cancelLastData);
                        }
                        break;
                }
            }
        }

        internal string GenerateUniqueNickname(INetworkSession client, string nickname)
        {
            nickname = nickname.Trim().Replace(" ", ""); // 공백 제거
            if (string.IsNullOrEmpty(nickname)) nickname = "익명";

            string escapedNickname = Regex.Escape(nickname);
            string pattern = $@"^{escapedNickname}\s\((\d+)\)$";
            // 닉네임 (숫자) 형태

            var usedNumbers = new HashSet<int>();
            bool isBaseNameUsed = false;

            foreach (var sessionplayer in _sessions)
            {
                if (client == sessionplayer.Key) continue; // 자기 자신은 제외

                if (sessionplayer.Value.Nickname == nickname)
                    isBaseNameUsed = true; // 이름 이미 사용 중
                else
                {
                    Match match = Regex.Match(sessionplayer.Value.Nickname, pattern);

                    if (match.Success) // 패턴과 일치하면
                    {
                        if (int.TryParse(match.Groups[1].Value, out int num))
                            usedNumbers.Add(num); // 집합에 숫자 추가
                    }
                }
            }

            if (!isBaseNameUsed) return nickname; // 이름 사용 안하고 있으니

            int nicknum = 1;

            while (usedNumbers.Contains(nicknum)) // 집합에서 숫자 찾으면 1증가 (안쓰는 가장 작은 숫자 찾기)
                nicknum++;

            return $"{nickname} ({nicknum})";
        }

        private void HandleClientDisconnected(INetworkSession session)
        {
            if (!IsRunning) return; // 서버 종료 중에는 연결 끊김 신호 안보냄


            Logger.System($"클라이언트 연결 끊김 세션 ID : {session.SessionId}");

            AddBroadcast(new ClientExitData() { Player = GetPlayerOrNull(session)! });

            _sessions.Remove(session, out _);

            if (manager.IsGameStarted)
            {
                if (session == _blackPlayer || session == _whitePlayer)
                { // 게임 참가자가 나간거라면?
                    var winner = (session == _blackPlayer) ? PlayerType.White : PlayerType.Black;
                    Logger.Info("게임 참가자 나감. 게임 종료 처리");
                    manager.ForceGameEnd(winner, "게임 나감");

                    if (session == _blackPlayer)
                        _blackPlayer = null;
                    else if (session == _whitePlayer)
                        _whitePlayer = null;
                }
            }
        }

        private void AddUnicast(INetworkSession target, GameData data)
        {
            _sendChannel.Writer.TryWrite((target, data));
        }

        private void AddBroadcast(GameData data)
        {
            foreach (var session in _sessions.Keys)
            {
                _sendChannel.Writer.TryWrite((session, data));
            }
        }

        private async Task StartSenderAsync(CancellationToken ct)
        {
            try
            {
                await foreach (var (target, data) in _sendChannel.Reader.ReadAllAsync(ct))
                {
                    try
                    {
                        await target.SendAsync(data);
                    }
                    catch (Exception e)
                    {
                        Logger.Error($"전송 중 오류 발생: {e.Message}");
                    }
                }
            }
            catch (OperationCanceledException) { }
        }
        public async ValueTask DisposeAsync()
        {
            _token.Cancel();

            try { _listener?.Stop(); } catch { }
            _listener = null;

            _heartbeattimer.Stop();
            _heartbeattimer.Dispose();
            _gametimer.Stop();
            _gametimer.Dispose();

            foreach (var sessionplayer in _sessions)
            {
                sessionplayer.Key.Disconnect();
            }

            _sendChannel.Writer.Complete();

            if (_sendTask != null)
                await _sendTask;

            if (_acceptTask != null)
                await _acceptTask;

            _token.Dispose();

            GC.SuppressFinalize(this);
        }
    }
}
