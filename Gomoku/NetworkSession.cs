using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace Gomoku
{
    public class NetworkSession
    {
        private readonly TcpClient _client;
        private readonly StreamWriter _writer;
        private readonly StreamReader _reader;
        
        private bool isconnected = false;

        public string SessionId { get; set; } // 서버만 사용함. 로그용

        public PlayerType Player { get; set; }
        public string Nickname { get; set; } = "익명";
        public int PlayerType { get; set; } = 0;
        // 관전 흑 백 구분
        public event Action<NetworkSession, GameData> OnDataReceived;
        public event Action<NetworkSession> OnDisconnected;

        public NetworkSession(TcpClient client)
        {
            _client = client;
            var stream = _client.GetStream();
            _writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };
            _reader = new StreamReader(stream, Encoding.UTF8);

            SessionId = new Guid().ToString();
            isconnected = true;
            Player = Gomoku.PlayerType.Observer;
            ReceiveLoopAsync();
        }

        public async Task SendAsync(GameData data)
        {
            try
            {
                if (_client.Connected)
                    throw new InvalidOperationException("Client is not connected.");

                var options = new JsonSerializerOptions
                {
                    WriteIndented = false,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };

                string json = JsonSerializer.Serialize<GameData>(data, options);
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
                while (true)
                {
                    string line = await _reader.ReadLineAsync();
                    if (line == null) // 연결 끊김
                    {
                        Disconnect();
                        break;
                    }

                    GameData data = JsonSerializer.Deserialize<GameData>(line);
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
            if (_client.Connected)
                _client.Close();
            if (isconnected) // 연결이 처음 끊기는 경우에만 이벤트 발생
                OnDisconnected?.Invoke(this);
        }
    }
}
