using CommunityToolkit.Mvvm.Messaging;
using Gomoku.Models;
using Gomoku.Models.Common;
using Gomoku.Models.Domain;
using Gomoku.Models.Messages;
using Gomoku.Models.Network;
using Gomoku.Services.Applications;
using Gomoku.Services.Applications.Auth;
using NSubstitute;

namespace UnitTest
{
    [TestClass]
    public class AuthSessionServiceTest
    {
        private IGameClientFactory gameClientFactory = null!;
        private Func<IGameServer> serverFactory = null!;
        private IPlayerTrackerService playerTracker = null!;
        private IMessenger messenger = null!;
        private IGameClient gameClient = null!;
        private IGameServer gameServer = null!;
        private AuthSessionService _service = null!;

        private ConnectionOption _guestClientOption = new ConnectionOption("127.0.0.1", 7777,
            LoginType.Guest, DoubleThreeRuleType.BothForbidden, ConnectionType.Client, new CancellationToken(), 3);

        private ConnectionOption _LoginClientoption = new ConnectionOption("127.0.0.1", 7777,
            LoginType.Login, DoubleThreeRuleType.BothForbidden, ConnectionType.Client, new CancellationToken(), 3);

        [TestInitialize]
        public void Setup()
        {
            gameClientFactory = Substitute.For<IGameClientFactory>();
            serverFactory = () => gameServer;
            messenger = new WeakReferenceMessenger();
            playerTracker = new PlayerTrackerService(messenger);
            gameClient = Substitute.For<IGameClient>();
            gameServer = Substitute.For<IGameServer>();

            gameClientFactory.CreateClient(Arg.Any<ConnectionType>()).Returns(gameClient);

            _service = new(gameClientFactory, serverFactory, playerTracker, messenger);
        }

        [TestMethod]
        public async Task CreateAccount_Success_Test()
        {
            await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await _service.RequestCreateAccountAsync("asd", "password", "닉네임"));
            // 클라이언트 초기화 이전에 요청 시 예외 던짐

            bool activatedMsgRecieved = false;
            messenger.Register<ClientActivatedMessage>(this, (r, m) => activatedMsgRecieved = true);

            var me = new Player(3, "id", "nickname", PlayerType.Observer);

            bool SessionInitializedMsgRecieved = false;
            messenger.Register<SessionInitializedMessage>(this, (r, m) =>
            {
                if (m.Me.Id == me.Id &&
                    m.Me.Nickname == me.Nickname &&
                    m.Me.AccountId == me.AccountId &&
                    m.Players.Count() == 1)
                    SessionInitializedMsgRecieved = true;
            });

            await _service.StartSessionAsync(_LoginClientoption);
            var createTask = _service.RequestCreateAccountAsync("id", "password", "nickname");


            _service.Receive(new ClientJoinResponseData
            {
                Accepted = true,
                Me = me,
                Users = new List<Player> { me }
            });
            // 서버 응답 모킹

            var authresult = await createTask;

            await gameClient.Received().SendCreateAccountAsync("id", "password", "nickname");
            // 클라이언트에게 계정 생성 요청 보냈는지
            Assert.IsTrue(authresult.IsSuccess);
            // 계정 생성 결과 성공했는지
            Assert.IsTrue(activatedMsgRecieved);
            // 클라이언트 작동 메시지 발송했는지
            Assert.IsTrue(SessionInitializedMsgRecieved);
            // 세션 연결 성공 UI 메시지 발송 및 내용 확인
        }

        [TestMethod]
        public async Task CreateAccount_Failed_Test()
        {
            await _service.StartSessionAsync(_LoginClientoption);
            var createTask = _service.RequestCreateAccountAsync("id", "password", "nickname");

            var me = new Player(3, "id", "nickname", PlayerType.Observer);

            _service.Receive(new CreateAccountRejectedData
            {
                Reason = "그냥"
            });
            // 서버 응답 모킹

            var authresult = await createTask;
            Assert.IsFalse(authresult.IsSuccess);
            // 실패 테스트
        }
    }
}
