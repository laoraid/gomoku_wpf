using System.Net.Sockets;

namespace Gomoku.Models
{
    public class GameClient : IDisposable
    {
        private NetworkSession? session;
        public event Action<GameData>? OnDataReceived;
        public event Action<string, int>? ServerConnectFailed;
        public event Action? ConnectionLost;

        private System.Timers.Timer _heartbeatTimer;
        private const int TIMEOUT_SECONDS = 15;

        public string Nickname { get; set; } = "익명";

        public GameClient()
        {   // 세션 연결 확인용 하트비트 데이터 수신 타이머 - 타이머 터지면 연결 끊긴 것으로 간주
            _heartbeatTimer = new System.Timers.Timer(TIMEOUT_SECONDS * 1000);
            _heartbeatTimer.Elapsed += (s, e) => OnHeartbeatTimeout();
            _heartbeatTimer.AutoReset = false; // 한번만 터지면 끝
        }

        public void DisConnect()
        {
            _heartbeatTimer.Stop();

            if (session != null)
            {
                session.OnDataReceived -= HandleHeartbeatData;
                session?.Disconnect();
                session = null;
                ConnectionLost?.Invoke();
            }
        }
        private void OnHeartbeatTimeout()
        {
            Logger.Error("서버 응답 시간 초과. 연결 종료.");
            DisConnect();
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
                DisConnect();
            }
            Nickname = nickname;
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
            }
            catch (Exception ex)
            {
                Logger.Error($"서버 연결 실패: {ex.Message}");
                ServerConnectFailed?.Invoke(ip, port);
                return;
            }

            Logger.Info($"서버에 연결됨: {ip}:{port}");
            session = new NetworkSession(client);

            session.OnDataReceived += HandleHeartbeatData;

            var joindata = new ClientJoinData()
            {
                Nickname = nickname
            };
            await session.SendAsync(joindata);
        }

        private void HandleHeartbeatData(NetworkSession session, GameData data)
        {
            ResetHeartbeatTimer(); // 아무거나 데이터 받으면 타이머 리셋

            if (data is PingData)
            {
                _ = session?.SendAsync(new PongData()); // 핑 데이터면 퐁 응답
                return;
            }

            OnDataReceived?.Invoke(data); // 아니면 이벤트 발생
        }

        public async Task SendData(GameData data)
        {
            session?.SendAsync(data);
        }

        public void Dispose()
        {
            DisConnect();
        }
    }
}
