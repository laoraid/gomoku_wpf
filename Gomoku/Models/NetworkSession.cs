using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace Gomoku.Models
{
    public interface INetworkSession
    {
        string SessionId { get; set; }
        PlayerType Player { get; set; }
        DateTime LastActiveTime { get; set; }
        bool IsConnected { get; }

        event Action<INetworkSession, GameData> OnDataReceived;
        event Action<INetworkSession> OnDisconnected;

        Task SendAsync(GameData data);
        void Disconnect();

    }

    public interface INetworkSessionFactory
    {
        INetworkSession Create(TcpClient tcpclient);
    }

    public class NetworkSessionFactory : INetworkSessionFactory
    {   // 팩토리는 싱글턴으로 해야 하나?
        public INetworkSession Create(TcpClient client) => new NetworkSession(client);
    }
    public class NetworkSession : INetworkSession
    {
        private readonly TcpClient _client;
        private readonly StreamWriter _writer;
        private readonly StreamReader _reader;

        public DateTime LastActiveTime { get; set; } = DateTime.Now;

        public bool IsConnected { get; private set; } = false;

        public string SessionId { get; set; } // 서버만 사용함. 로그용

        public PlayerType Player { get; set; }
        public string Nickname { get; set; } = "익명";

        public event Action<INetworkSession, GameData>? OnDataReceived;
        public event Action<INetworkSession>? OnDisconnected;

        private JsonSerializerOptions _options = new JsonSerializerOptions
        {
            WriteIndented = false,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        public NetworkSession(TcpClient client)
        {
            _client = client;
            var stream = _client.GetStream();
            _writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };
            _reader = new StreamReader(stream, Encoding.UTF8);

            SessionId = Guid.NewGuid().ToString();
            IsConnected = true;
            Player = Models.PlayerType.Observer;
            ReceiveLoopAsync();
        }

        public async Task SendAsync(GameData data)
        {
            try
            {
                //Logger.Debug($"보내려는 데이터 타입 : {data.GetType().Name}");
                if (!_client.Connected)
                    throw new InvalidOperationException("Client is not connected.");

                string json = JsonSerializer.Serialize<GameData>(data, _options);
                await _writer.WriteLineAsync(json);
            }
            catch (Exception)
            {
                Disconnect();
            }
        }

        private async void ReceiveLoopAsync()
        {
            try
            {
                while (IsConnected)
                {
                    string? line = await _reader.ReadLineAsync();
                    if (line == null) // 연결 끊김
                    {
                        Disconnect();
                        break;
                    }

                    LastActiveTime = DateTime.Now; // 뭐든 받으면 갱신

                    GameData? data = JsonSerializer.Deserialize<GameData>(line);

                    if (data == null)
                        throw new ArgumentNullException("역직렬화 오류. data가 null입니다.");
                    OnDataReceived?.Invoke(this, data);
                }
            }
            catch (Exception)
            {
                Disconnect();
            }
            finally
            {
                Disconnect();
            }
        }

        public void Disconnect()
        {
            if (_client != null)
                _client.Close();
            if (IsConnected) // 연결이 처음 끊기는 경우에만 이벤트 발생
            {
                IsConnected = false;
                OnDisconnected?.Invoke(this);
            }
        }
    }
}
