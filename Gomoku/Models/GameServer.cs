using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Timers;

namespace Gomoku.Models
{
    public class GameServer : IDisposable
    {
        private TcpListener? _listener;

        private readonly Dictionary<INetworkSession, Player> _sessions = new Dictionary<INetworkSession, Player>();

        private readonly GomokuManager manager = new GomokuManager();

        private readonly object _handlelock = new object();

        private readonly INetworkSessionFactory _sessionFactory;

        private INetworkSession? _blackPlayer;
        private INetworkSession? _whitePlayer;
        private readonly System.Timers.Timer _gametimer = new System.Timers.Timer(1000);
        private readonly System.Timers.Timer _heartbeattimer = new System.Timers.Timer(5000);

        public GameServer(INetworkSessionFactory sessionFactory)
        {
            _sessionFactory = sessionFactory;

            _gametimer.Elapsed += SetTimer;
            manager.OnGameEnded += async (winner, reason) => // 게임 종료 시에 모든 클라에게 결과 방송
            {
                _gametimer.Stop();
                GameEndData enddata = new GameEndData()
                {
                    Winner = winner,
                    Reason = reason
                };

                await Broadcast(enddata);
            };

            _heartbeattimer.Elapsed += async (s, e) => // 핑 송신 및 오래된 세션 정리
            {
                await Broadcast(new PingData());

                List<KeyValuePair<INetworkSession, Player>> sessionToDisconnect = [];

                lock (_handlelock)
                {
                    var now = DateTime.Now;

                    foreach (var sessionplayer in _sessions)
                    {
                        if ((now - sessionplayer.Key.LastActiveTime).TotalSeconds > 15) // 오래 응답 없는 세션 
                            sessionToDisconnect.Add(sessionplayer);
                    }
                }

                foreach (var sessionplayer in sessionToDisconnect)
                {
                    sessionplayer.Key.Disconnect();
                    await Broadcast(new ClientExitData() { Player = sessionplayer.Value });
                }
            };
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
                await Broadcast(timepasspacket);
        }

        private Player? GetPlayerOrNull(INetworkSession? session)
        {
            if (session == null) return null;

            if (_sessions.TryGetValue(session, out Player? value))
                return value;

            return null;
        }

        public async Task StartAsync(int port)
        {
            if (_listener != null)
                StopServer();

            _listener = new TcpListener(IPAddress.Any, port);
            _listener.Start();
            Logger.System($"서버 시작 됨. 포트 : {port}");

            _ = Task.Run(AccpetClientsAsync); // 비동기적으로 클라이언트 수락 시작

            _heartbeattimer.Start();
        }

        public void StopServer()
        {
            try { _listener?.Stop(); } catch { }
            _listener = null;

            _heartbeattimer.Stop();
            _gametimer.Stop();

            lock (_handlelock)
            {
                foreach (var sessionplayer in _sessions)
                {
                    sessionplayer.Key.Disconnect();
                }
                _sessions.Clear();
                _blackPlayer = null;
                _whitePlayer = null;

                manager.NewSession();
            }
        }

        private async Task AccpetClientsAsync()
        {
            try
            {
                while (true)
                {
                    if (_listener == null)
                        throw new ArgumentNullException("listener가 null 입니다.");
                    TcpClient client = await _listener.AcceptTcpClientAsync();

                    var newSession = _sessionFactory.Create(client);

                    SessionAdd(newSession);
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"클라이언트 연결 수락 중 오류 발생 : {ex.Message}");
                StopServer();
            }
        }

        internal Player SessionAdd(INetworkSession session)
        {
            session.OnDataReceived += async (s, d) => await ProcessDataAsync(s, d);
            session.OnDisconnected += HandleClientDisconnected;

            Player tempplayer = new Player();
            tempplayer.Nickname = "임시";

            lock (_handlelock)
            {
                _sessions.Add(session, tempplayer);
            }
            Logger.System($"새 클라이언트 연결됨. 세션 ID : {session.SessionId}");

            return tempplayer;
        }

        internal async Task ProcessDataAsync(INetworkSession session, GameData data)
        {
            //Logger.Debug($"데이터 수신. 세션 ID : {session.SessionId}, 데이터 타입 : {data.GetType().Name}");
            List<GameData> responses = new List<GameData>();
            List<GameData> broadcast_res = new List<GameData>();

            Player player = GetPlayerOrNull(session)!;

            lock (_handlelock)
            {
                switch (data) // 데이터 분기 처리 (서버)
                {
                    case ChatData chatData:
                        chatData.Sender.Nickname = player!.Nickname; // 닉네임 바꿔서 패킷 전송해도 그냥 서버에서 저장된 닉네임으로
                        broadcast_res.Add(chatData);
                        break;
                    case PositionData positionData:
                        if (!manager.IsGameStarted) return;
                        try
                        {
                            manager.TryPlaceStone(positionData.Move);
                            _gametimer.Stop();
                            broadcast_res.Add(positionData); // catch 안되면 돌 둔것
                            if (!manager.CheckWin(positionData.Move))
                            {
                                _gametimer.Start();
                            }
                        }
                        catch (InvalidPlaceException)
                        {
                            ResponseData response = new PlaceResponseData()
                            {
                                Accepted = false,
                                Position = positionData,
                            };
                            responses.Add(response);
                        }
                        break;
                    case RequestJoinData joinData: // 클라이언트 최초 접속시
                        string finalnickname = GenerateUniqueNickname(session, joinData.Nickname);

                        player.Nickname = finalnickname;

                        var res = new ClientJoinResponseData() // 접속 확인 응답
                        {
                            Accepted = true,
                            Me = player,
                            Users = _sessions.Values.ToList()
                        };

                        responses.Add(res);

                        var join_broadcast = new ClientJoinData() // 모두에게 접속했다고 방송
                        {
                            Player = player
                        };

                        broadcast_res.Add(join_broadcast);

                        var syncdata = new GameSyncData() // 게임 진행 데이터 전송
                        {
                            SyncData = new DTO.GameSync(manager.StoneHistory, manager.CurrentPlayer,
                            manager.Rules.Select(r => r.RuleInfo), GetPlayerOrNull(_blackPlayer), GetPlayerOrNull(_whitePlayer))
                        };

                        responses.Add(syncdata);
                        break;

                    case GameJoinData joindata:
                        if (_blackPlayer == session || _whitePlayer == session)
                            // 이미 흑백 들어간 사람이라면
                            break;
                        if (joindata.Type == PlayerType.Black)
                            _blackPlayer = session;
                        else
                            _whitePlayer = session;

                        broadcast_res.Add(joindata);
                        break;

                    case GameLeaveData leaveData:
                        if (_blackPlayer != session && _whitePlayer != session)
                            // 안들어간 사람이 나가기 요청한거라면
                            break;

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

                        broadcast_res.Add(leaveData);
                        break;
                    case GameStartData gamestartdata:
                        if (_blackPlayer != session) // 흑 플레이어가 요청한게 아니라면
                            break;

                        broadcast_res.Add(gamestartdata);
                        StartGame();
                        break;
                }
            }

            if (responses.Count > 0)
            {
                foreach (var response in responses)
                    await session.SendAsync(response);
            }

            if (broadcast_res.Count > 0)
            {
                foreach (var response in broadcast_res)
                    await Broadcast(response);
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

        private async void HandleClientDisconnected(INetworkSession session)
        {
            if (_listener == null) return; // 서버 종료 중에는 연결 끊김 신호 안보냄


            Logger.System($"서버: 클라이언트 연결 끊김. 세션 ID : {session.SessionId}");

            await Broadcast(new ClientExitData() { Player = GetPlayerOrNull(session)! });

            lock (_handlelock)
            {
                _sessions.Remove(session);
            }

            if (manager.IsGameStarted)
            {
                if (session == _blackPlayer || session == _whitePlayer)
                { // 게임 참가자가 나간거라면?
                    var winner = (session == _blackPlayer) ? PlayerType.White : PlayerType.Black;
                    manager.ForceGameEnd(winner, "게임 나감");

                    if (session == _blackPlayer)
                        _blackPlayer = null;
                    else if (session == _whitePlayer)
                        _whitePlayer = null;
                }
            }
        }

        public async Task Broadcast(GameData data)
        {
            List<INetworkSession> targetSessions;

            lock (_handlelock)
            { // 브로드캐스트 도중 세션 종료된 경우 보호
                targetSessions = new List<INetworkSession>(_sessions.Keys);
            }

            foreach (var session in targetSessions)
            {
                await session.SendAsync(data);
            }
        }

        public async Task BroadcastChat(Player sender, string msg)
        {
            var chatdata = new ChatData
            {
                Sender = sender,
                Message = msg
            };

            await Broadcast(chatdata);
        }

        public void Dispose()
        {
            StopServer();
            GC.SuppressFinalize(this);
        }
    }
}
