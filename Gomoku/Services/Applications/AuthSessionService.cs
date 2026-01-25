using CommunityToolkit.Mvvm.Messaging;
using Gomoku.Models;
using Gomoku.Models.DTO;
using Gomoku.Models.Interfaces;
using Gomoku.Services.Interfaces;

namespace Gomoku.Services.Applications
{
    public class AuthSessionService : IAuthSessionService,
        IRecipient<ClientJoinResponseData>,
        IRecipient<ClientExitData>,
        IRecipient<SessionConnectLostInternalMessage>,
        IRecipient<CreateAccountRejectedData>,
        IRecipient<LoginFailedData>,
        IRecipient<ClientJoinData>
    {
        private IGameServer? _server;
        private IGameClient? _client;
        private readonly IGameClientFactory _gameClientFactory;
        Func<IGameServer> _serverFactory;
        private readonly IPlayerTrackerService _playerTracker;
        private readonly IMessenger _messenger;

        private TaskCompletionSource<AuthResult>? _authTcs;

        public AuthSessionService(IGameClientFactory gameClientFactory, Func<IGameServer> serverFactory,
            IPlayerTrackerService playerTracker, IMessenger messenger)
        {
            _gameClientFactory = gameClientFactory;
            _serverFactory = serverFactory;
            _playerTracker = playerTracker;
            _messenger = messenger;
            _messenger.RegisterAll(this);
        }
        public async Task<AuthResult> RequestCreateAccountAsync(string userid, string password, string nickname)
        {
            if (_client == null)
                throw new InvalidOperationException("클라이언트가 초기화되지 않았습니다.");
            _authTcs = new TaskCompletionSource<AuthResult>();
            await _client.SendCreateAccountAsync(userid, password, nickname);

            return await _authTcs.Task;
        }

        public async Task RequestDeleteAccountAsync(string userid, string password)
        {
            if (_client == null)
                throw new InvalidOperationException("클라이언트가 초기화되지 않았습니다.");
            if (!_client.IsAuthenticated)
                throw new InvalidOperationException("인증되지 않았습니다.");

            throw new NotImplementedException();
            // TODO: 삭제 요청 전송
        }

        public async Task<AuthResult> RequestLoginAsync(string userid, string password)
        {
            if (_client == null)
                throw new InvalidOperationException("클라이언트가 초기화되지 않았습니다.");
            _authTcs = new TaskCompletionSource<AuthResult>();
            await _client.SendAuthAsync(new AuthInfo(LoginType.Login, userid, password));

            return await _authTcs.Task;
        }

        public async Task RequestGuestLoginAsync()
        {
            if (_client == null)
                throw new InvalidOperationException("클라이언트가 초기화되지 않았습니다.");
            await _client.SendAuthAsync(new AuthInfo(LoginType.Guest, "", ""));
        }

        public async Task<bool> StartSessionAsync(ConnectionOption option)
        {
            IGameClient targetclient;

            await StopSessionAsync();

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

                        await _client.ConnectAsync("127.0.0.1", option.port, option.CancellationToken);
                        break;
                    case ConnectionType.Client:
                        await _client.ConnectAsync(option.Ip, option.port, option.CancellationToken);
                        break;
                    case ConnectionType.Single:
                        await _client.ConnectAsync("", 0, CancellationToken.None);
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
            if (_client != null && _client.IsConnected)
                _client.Disconnect();
            _client = null;
            if (_server != null)
            {
                await _server.DisposeAsync();
                _server = null;
            }
            _messenger.Send(new ClientDeactivatedMessage());
            // 클라이언트 없어졌다고 다른 서비스에게 알림
            _messenger.Send(new SessionConnectLostMessage());
            // 연결 해제되었다고 뷰모델에 알림
        }
        public void Receive(ClientExitData data)
        {
            var player = _playerTracker.GetManagedPlayer(data.Player);

            _messenger.Send(new PlayerDisconnectedInternalMessage(player));
            // 먼저 서비스에게 메시지 발송
            _playerTracker.RemovePlayer(data.Player.Nickname);
            _messenger.Send(new PlayerDisconnectedMessage(player));
            // 그다음 뷰모델이 듣게 발송
        }

        public void Receive(ClientJoinResponseData data)
        {
            var players = data.Users;

            _authTcs?.TrySetResult(new AuthResult(true, ""));

            _playerTracker.AddPlayers(players);
            _messenger.Send(new ClientActivatedMessage(_client!));
            _messenger.Send(new SessionInitializedMessage(data.Me, _playerTracker.AllPlayers));
        }
        public void Receive(ClientJoinData data)
        {
            var player = _playerTracker.GetManagedPlayer(data.Player);
            _messenger.Send(new PlayerConnectedMessage(player));
        }

        public void Receive(SessionConnectLostInternalMessage message)
        {
            Logger.Info("클라이언트 연결 끊김 감지됨");
            _ = StopSessionAsync();
        }

        public void Receive(CreateAccountRejectedData message)
        {
            _authTcs?.TrySetResult(new AuthResult(false, message.Reason));
        }

        public void Receive(LoginFailedData message)
        {
            _authTcs?.TrySetResult(new AuthResult(false, message.Reason));
        }
    }
}
