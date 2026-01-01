using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace Gomoku
{
    public class GameClient
    {
        private NetworkSession session;
        public event Action<GameData>? OnDataReceived;
        public event Action<string, int>? ServerConnectFailed;

        public async Task ConnectAsync(string ip, int port, string nickname)
        {
            TcpClient client = new TcpClient();
            try
            {
                await client.ConnectAsync(ip, port);
            }
            catch (Exception ex)
            {
                Logger.Error($"서버 연결 실패: {ex.Message}");
                ServerConnectFailed?.Invoke(ip, port);
                return;
            }

            Logger.Info($"서버에 연결됨: {ip}:{port}");
            session = new NetworkSession(client);
            
            session.OnDataReceived += (s, data) => OnDataReceived?.Invoke(data);

            var joindata = new ClientJoinData()
            {
                Nickname = nickname
            };
            await session.SendAsync(joindata);
        }

        public async Task SendData(GameData data)
        {
            session?.SendAsync(data);
        }
    }
}
