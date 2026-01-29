using CommunityToolkit.Mvvm.Messaging;
using Gomoku.Helpers;
using Gomoku.Models.Common;
using Gomoku.Models.Domain;
using Gomoku.Models.DTO;
using Gomoku.Models.Interfaces;
using System.Net.Sockets;

namespace Gomoku.Models.Network
{

    public class GameClient : IDisposable, IGameClient
    {
        private IMessenger _messenger;
        public Player? Me { get; protected set; }
        public bool HasOpponent => true;
        public bool IsAuthenticated { get; private set; }


        private INetworkSession? session;
        private readonly INetworkSessionFactory _sessionFactory;

        public bool IsConnected => session != null && session.IsConnected;

        private System.Timers.Timer _heartbeatTimer;

        public string MessageToken => "Network";


        public GameClient(INetworkSessionFactory sessionFactory, IMessenger messenger, int timeout_seconds = 15)
        {
            _messenger = messenger;
            _sessionFactory = sessionFactory;

            // 세션 연결 확인용 하트비트 데이터 수신 타이머 - 타이머 터지면 연결 끊긴 것으로 간주
            _heartbeatTimer = new System.Timers.Timer(timeout_seconds * 1000);
            _heartbeatTimer.Elapsed += (s, e) => OnHeartbeatTimeout();
            _heartbeatTimer.AutoReset = false; // 한번만 터지면 끝
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
            _messenger.Send(new SessionConnectLostInternalMessage());
            IsAuthenticated = false;
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

        public async Task<bool> ConnectAsync(string ip, int port, CancellationToken cts)
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
                await InitializeSessionAsync(client);
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

        internal async Task InitializeSessionAsync(TcpClient client)
        {
            session = _sessionFactory.Create(client);

            session.OnDataReceived += OnDataReceived;
            session.OnDisconnected += (s) => Disconnect();

            _heartbeatTimer.Start();
        }

        private void OnDataReceived(INetworkSession session, GameData data)
        {
            ResetHeartbeatTimer(); // 아무거나 데이터 받으면 타이머 리셋

            if (data is PingData)
            {
                _ = session.SendAsync(new PongData()); // 핑 데이터면 퐁 응답
                return;
            }

            if (data is not TimePassedData)
            {
                Logger.Debug($"클라이언트 패킷 수신 : {data.GetType().Name}");
            }

            switch (data)
            {
                case ClientJoinResponseData cjrd:
                    Me = cjrd.Me;
                    IsAuthenticated = true;
                    break;
                case ClientExitData ced:
                    if (ced.Player.Nickname == Me!.Nickname)
                        Disconnect();
                    break;
                case GameJoinData gjd:
                    if (gjd.Player.Nickname == Me!.Nickname)
                        Me.Type = gjd.Type;
                    break;
                case GameLeaveData gld:
                    if (gld.Player.Nickname == Me!.Nickname)
                        Me.Type = PlayerType.Observer;
                    break;
            }

            _messenger.Send(data, MessageToken);
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

            await SendDataAsync(new RequestGameStartData());
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

        public async Task CancelLastStoneAsync(int LeftCancelCount)
        {
            if (session != null)
                await session.SendAsync(new CancelLastData { SenderType = Me!.Type, LeftCancelLastCount = LeftCancelCount });
        }

        public async Task SendAuthAsync(AuthInfo authInfo)
        {
            AuthInfo newauth;

            if (authInfo.LoginType == LoginType.Login)
                newauth = new AuthInfo(LoginType.Login, authInfo.UserId, HashHelper.SHA256Hash(authInfo.Password));
            else
                newauth = authInfo;

            var joindata = new RequestJoinData()
            {
                AuthInfo = newauth
            };
            await session!.SendAsync(joindata);
        }

        public async Task SendCreateAccountAsync(string username, string password, string nickname)
        {
            var createAccountData = new RequestCreateAccountData
            {
                Nickname = nickname,
                UserId = username,
                PasswordHashed = HashHelper.SHA256Hash(password)
            };
            await session!.SendAsync(createAccountData);
        }
    }
}
