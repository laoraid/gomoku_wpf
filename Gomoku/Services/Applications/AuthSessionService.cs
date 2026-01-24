using Gomoku.Models;
using Gomoku.Models.DTO;
using Gomoku.Models.Interfaces;
using Gomoku.Services.Interfaces;

namespace Gomoku.Services.Applications
{
    public class AuthSessionService : IAuthSessionService
    {
        private IGameServer? _server;
        private IGameClient? _client;
        private readonly IGameClientFactory _gameClientFactory;
        Func<IGameServer> _serverFactory;

        public AuthSessionService(IGameClientFactory gameClientFactory, Func<IGameServer> serverFactory)
        {
            _gameClientFactory = gameClientFactory;
            _serverFactory = serverFactory;
        }
        public async Task RequestCreateAccountAsync(string userid, string password, string nickname)
        {
            if (_client == null)
                throw new InvalidOperationException("클라이언트가 초기화되지 않았습니다.");
            await _client.SendCreateAccountAsync(userid, password, nickname);
        }

        public async Task RequestDeleteAccountAsync(string userid, string password)
        {
            if (_client == null)
                throw new InvalidOperationException("클라이언트가 초기화되지 않았습니다.");
            throw new NotImplementedException();
            // TODO: 삭제 요청 전송
        }

        public async Task RequestLoginAsync(string userid, string password)
        {
            if (_client == null)
                throw new InvalidOperationException("클라이언트가 초기화되지 않았습니다.");
            await _client.SendAuthAsync(new AuthInfo(true, userid, password));
        }

        public async Task<bool> StartSessionAsync(ConnectionOption option)
        {
            IGameClient targetclient;

            if (_client != null && _client.IsConnected)
                _client.Disconnect();
            if (_server != null)
            {
                await _server.DisposeAsync();
                _server = null;
            }

            targetclient = _gameClientFactory.CreateClient(option.ConnectionType);

            if (option.ConnectionType == ConnectionType.Single)
            {
                if (targetclient is SoloGameClient soloGameClient)
                    soloGameClient.AddRule(RuleFactory.CreateRule(new DoubleThreeRuleInfo(option.DoubleThreeRuleType)));
            }

            _client = targetclient;

            try
            {
                switch (option.ConnectionType)
                {
                    case ConnectionType.Server:
                        _server = _serverFactory();

                        await _server.StartAsync(option);
                        _server.AddRule(RuleFactory.CreateRule(new DoubleThreeRuleInfo(option.DoubleThreeRuleType)));

                        await _client!.ConnectAsync("127.0.0.1", option.port, option.CancellationToken);
                        break;
                    case ConnectionType.Client:
                        await _client!.ConnectAsync(option.Ip, option.port, option.CancellationToken);
                        break;
                    case ConnectionType.Single:
                        await _client!.ConnectAsync("", 0, CancellationToken.None);
                        break;
                }

                return true;
            }
            catch (OperationCanceledException)
            {   // 사용자 요청으로 취소
                Logger.Info("세션 연결 취소됨");

                await StopSessionAsync();

                return false;
            }
            catch (Exception e)
            {
                Logger.Error($"세션 연결 중 에러 발생 {e.Message}");
                await StopSessionAsync();
                throw;
            }
        }

        public async Task StopSessionAsync()
        {
            _client?.Disconnect();
            if (_server != null)
            {
                await _server.DisposeAsync();
                _server = null;
            }
        }
    }
}
