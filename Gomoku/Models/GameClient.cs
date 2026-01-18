using Gomoku.Models.DTO;
using System.Net.Sockets;

namespace Gomoku.Models
{
    public interface INetworkService
    {
        event Action? ConnectionLost;

        Task SendDataAsync(GameData data);
        bool IsConnected { get; }
        void Disconnect();
    }

    public interface IGameClient : INetworkService
    {
        event Action<GameMove>? PlaceReceived;
        event Action<Player, string>? ChatReceived;

        event Action<Player>? PlayerJoinReceived;
        event Action<Player>? PlayerLeftReceived;

        event Action<GameMove>? PlaceRejected;

        event Action<Player, IEnumerable<Player>>? ClientJoinResponseReceived;
        event Action<GameSync>? GameSyncReceived;

        event Action<PlayerType, int>? TimePassedReceived;

        event Action<PlayerType, Player>? GameJoinReceived;
        event Action<PlayerType, Player>? GameLeaveReceived;

        event Action? GameStartReceived;
        event Action<GameEnd>? GameEndReceived;

        Task SendPlaceAsync(GameMove move);
        Task SendChatAsync(string message);
        Task SendJoinGameAsync(PlayerType type);
        Task SendLeaveGameAsync();
        Task SendGameStartAsync();
        Task<bool> ConnectAsync(string ip, int port, string nickname, CancellationToken cts);

        Player? Me { get; }
    }

    public class GameClient : IDisposable, IGameClient
    {
        public Player? Me { get; protected set; }

        private INetworkSession? session;
        private readonly INetworkSessionFactory _sessionFactory;

        public event Action? ConnectionLost;

        public event Action<GameMove>? PlaceReceived;
        public event Action<Player, string>? ChatReceived;

        public event Action<Player>? PlayerJoinReceived;
        public event Action<Player>? PlayerLeftReceived;

        public event Action<GameMove>? PlaceRejected;

        public event Action<Player, IEnumerable<Player>>? ClientJoinResponseReceived;
        public event Action<GameSync>? GameSyncReceived;

        public event Action<PlayerType, int>? TimePassedReceived;

        public event Action<PlayerType, Player>? GameJoinReceived;
        public event Action<PlayerType, Player>? GameLeaveReceived;

        public event Action? GameStartReceived;
        public event Action<GameEnd>? GameEndReceived;

        public bool IsConnected => session != null && session.IsConnected;

        private System.Timers.Timer _heartbeatTimer;

        public GameClient(INetworkSessionFactory sessionFactory, int timeout_seconds = 15)
        {   // 세션 연결 확인용 하트비트 데이터 수신 타이머 - 타이머 터지면 연결 끊긴 것으로 간주
            _heartbeatTimer = new System.Timers.Timer(timeout_seconds * 1000);
            _heartbeatTimer.Elapsed += (s, e) => OnHeartbeatTimeout();
            _heartbeatTimer.AutoReset = false; // 한번만 터지면 끝

            _sessionFactory = sessionFactory;
        }

        public void Disconnect()
        {
            if (session == null) return;
            Logger.Info("클라이언트 연결 끊김");
            _heartbeatTimer.Stop();

            var currentSession = session;
            session = null;

            currentSession.OnDataReceived -= OnDataReceived;
            currentSession?.Disconnect();
            ConnectionLost?.Invoke();
        }
        private void OnHeartbeatTimeout()
        {
            Logger.Error("서버 응답 시간 초과. 연결 종료.");
            Disconnect();
        }

        private void ResetHeartbeatTimer()
        {
            _heartbeatTimer.Stop();
            _heartbeatTimer.Start();
        }

        public async Task<bool> ConnectAsync(string ip, int port, string nickname, CancellationToken cts)
        {
            if (session != null)
            {
                Disconnect();
            }

            TcpClient client = new TcpClient();
            using var timeoutCt = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            using var ctss = CancellationTokenSource.CreateLinkedTokenSource(cts, timeoutCt.Token);
            try
            {

                await client.ConnectAsync(ip, port, ctss.Token);
                await InitializeSessionAsync(client, nickname);
                return true;
            }
            catch (OperationCanceledException)
            {
                client.Dispose();
                if (cts.IsCancellationRequested)
                {
                    Logger.Info("사용자 요청으로 연결 취소됨");
                    return false;
                }
                else
                {
                    Logger.Error("서버 연결 시간 초과 (5초)");
                    throw new TimeoutException("서버 연결 시간 초과 (5초)");
                }

            }
            catch (Exception ex)
            {
                Logger.Error($"서버 연결 실패: {ex.Message}");
                throw;
            }

        }

        internal async Task InitializeSessionAsync(TcpClient client, string nickname)
        {
            session = _sessionFactory.Create(client);

            session.OnDataReceived += OnDataReceived;
            session.OnDisconnected += (s) => Disconnect();

            _heartbeatTimer.Start();

            var joindata = new RequestJoinData()
            {
                Nickname = nickname
            };
            await session.SendAsync(joindata);
        }

        private void OnDataReceived(INetworkSession session, GameData data)
        {
            ResetHeartbeatTimer(); // 아무거나 데이터 받으면 타이머 리셋

            if (data is PingData)
            {
                _ = session.SendAsync(new PongData()); // 핑 데이터면 퐁 응답
                return;
            }

            Logger.Debug($"데이터 수신 : {data.GetType().Name}");

            switch (data)
            {
                case PositionData pd:
                    PlaceReceived?.Invoke(pd.Move);
                    break;
                case PlaceResponseData prd:
                    PlaceRejected?.Invoke(prd.Position.Move);
                    break;
                case ChatData cd:
                    ChatReceived?.Invoke(cd.Sender, cd.Message);
                    break;
                case ClientJoinData cjd:
                    PlayerJoinReceived?.Invoke(cjd.Player);
                    break;
                case ClientJoinResponseData cjrd:
                    Me = cjrd.Me;
                    ClientJoinResponseReceived?.Invoke(cjrd.Me, cjrd.Users);
                    break;
                case ClientExitData ced:
                    if (ced.Player.Nickname == Me!.Nickname)
                        Disconnect();
                    else
                        PlayerLeftReceived?.Invoke(ced.Player);
                    break;
                case GameSyncData gsd:
                    GameSyncReceived?.Invoke(gsd.SyncData);
                    break;
                case GameJoinData gjd:
                    if (gjd.Player.Nickname == Me!.Nickname)
                        Me.Type = gjd.Type;
                    GameJoinReceived?.Invoke(gjd.Type, gjd.Player);
                    break;
                case GameLeaveData gld:
                    if (gld.Player.Nickname == Me!.Nickname)
                        Me.Type = PlayerType.Observer;
                    GameLeaveReceived?.Invoke(gld.Type, gld.Player);
                    break;
                case GameStartData gstd:
                    GameStartReceived?.Invoke();
                    break;
                case GameEndData ged:
                    GameEndReceived?.Invoke(ged.EndData);
                    break;
                case TimePassedData tpd:
                    TimePassedReceived?.Invoke(tpd.PlayerType, tpd.CurrentLeftTimeSeconds);
                    break;
                default:
                    Logger.Error($"알 수 없는 데이터 수신 : {data.GetType().Name}");
                    break;
            }
        }

        public async Task SendPlaceAsync(GameMove move)
        {
            var data = new PositionData
            {
                Move = move
            };
            await SendDataAsync(data);
        }

        public async Task SendChatAsync(string message)
        {
            var data = new ChatData
            {
                Sender = Me ?? throw new InvalidOperationException("서버에 접속하지 않았는데 채팅을 하려고 함"),
                Message = message
            };
            await SendDataAsync(data);
        }

        public async Task SendJoinGameAsync(PlayerType type)
        {
            if (type != PlayerType.Black && type != PlayerType.White)
                throw new InvalidOperationException("흑 또는 백 이외로 접속하려 함");

            var data = new GameJoinData
            {
                Player = Me ?? throw new InvalidOperationException("서버에 접속하지 않았는데 흑백에 들어가려 함"),
                Type = type
            };

            await SendDataAsync(data);
        }

        public async Task SendLeaveGameAsync()
        {
            if (Me?.Type != PlayerType.Black && Me?.Type != PlayerType.White)
                throw new InvalidOperationException("흑백이 아닌데 게임에서 나가려고 함");

            var data = new GameLeaveData
            {
                Player = Me,
                Type = Me.Type
            };

            await SendDataAsync(data);
        }

        public async Task SendGameStartAsync()
        {
            if (Me == null)
                throw new InvalidOperationException("서버에 접속하지 않았는데 게임을 시작하려 함");

            if (Me.Type != PlayerType.Black)
                throw new InvalidOperationException("흑이 아닌데 게임을 시작하려 함");

            await SendDataAsync(new GameStartData());
        }

        public async Task SendDataAsync(GameData data)
        {
            if (session != null)
                await session.SendAsync(data);
        }

        public void Dispose()
        {
            Disconnect();
            GC.SuppressFinalize(this);
        }
    }
}
