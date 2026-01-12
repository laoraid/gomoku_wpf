using System.Net.Sockets;

namespace Gomoku.Models
{
    public interface INetworkService
    {
        event Action<GameData>? OnDataReceived;
        event Action? ConnectionLost;

        Task SendDataAsync(GameData data);
        bool IsConnected { get; }
        void Disconnect();
    }

    public class GameClient : IDisposable, INetworkService
    {
        private INetworkSession? session;
        private readonly INetworkSessionFactory _sessionFactory;
        public event Action<GameData>? OnDataReceived;
        public event Action<string, int>? ServerConnectFailed;
        public event Action? ConnectionLost;

        public bool IsConnected => session != null && session.IsConnected;

        private System.Timers.Timer _heartbeatTimer;

        public string Nickname { get; set; } = "익명";

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
            Logger.Debug("클라이언트 연결 끊김");
            _heartbeatTimer.Stop();

            var currentSession = session;
            session = null;

            currentSession.OnDataReceived -= HandleHeartbeatData;
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

        public async Task ConnectAsync(string ip, int port, string nickname)
        {
            if (session != null)
            {
                Disconnect();
            }
            TcpClient client = new TcpClient();
            try
            {
                var connectTesk = client.ConnectAsync(ip, port);
                var timeoutTesk = Task.Delay(5000);

                if (await Task.WhenAny(connectTesk, timeoutTesk) == timeoutTesk)
                {
                    throw new TimeoutException("서버 연결 시간 초과");
                }
                await connectTesk;

                await InitializeSessionAsync(client, nickname);
            }
            catch (Exception ex)
            {
                Logger.Error($"서버 연결 실패: {ex.Message}");
                ServerConnectFailed?.Invoke(ip, port);
                return;
            }

        }

        internal async Task InitializeSessionAsync(TcpClient client, string nickname)
        {
            Nickname = nickname;
            session = _sessionFactory.Create(client);

            session.OnDataReceived += HandleHeartbeatData;
            session.OnDisconnected += (s) => Disconnect();

            _heartbeatTimer.Start();

            var joindata = new ClientJoinData()
            {
                Nickname = nickname
            };
            await session.SendAsync(joindata);
        }

        private void HandleHeartbeatData(INetworkSession session, GameData data)
        {
            ResetHeartbeatTimer(); // 아무거나 데이터 받으면 타이머 리셋

            if (data is PingData)
            {
                _ = session.SendAsync(new PongData()); // 핑 데이터면 퐁 응답
                return;
            }

            OnDataReceived?.Invoke(data); // 아니면 이벤트 발생
        }

        public async Task SendDataAsync(GameData data)
        {
            session?.SendAsync(data);
        }

        public void Dispose()
        {
            Disconnect();
        }
    }
}
