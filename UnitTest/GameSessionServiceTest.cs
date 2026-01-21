using Gomoku.Models;
using Gomoku.Models.DTO;
using Gomoku.Services.Applications;
using NSubstitute;

namespace UnitTest
{
    [TestClass]
    public class GameSessionServiceTest
    {
        private GameSessionService gameSession = null!;
        private IGameClient client = null!;
        private IGameServer server = null!;

        [TestInitialize]
        public void Setup()
        {
            var GameClientFactory = Substitute.For<IGameClientFactory>();
            client = Substitute.For<IGameClient>();
            server = Substitute.For<IGameServer>();

            GameClientFactory.CreateClient(Arg.Any<ConnectionType>()).Returns(client);
            gameSession = new GameSessionService(server, GameClientFactory);
        }

        [TestMethod]
        public async Task StartSession_Test()
        {
            var option = new ConnectionOption("", 1234, "닉네임",
                DoubleThreeRuleType.WhiteOnlyAllow, ConnectionType.Server, CancellationToken.None);

            await gameSession.StartSessionAsync(option);

            await server.Received().StartAsync(1234);
            // 포트 그대로 server에서 생성했는지 확인
            server.Received().AddRule(Arg.Is<DoubleThreeRule>(r => r.DTRuleType == DoubleThreeRuleType.WhiteOnlyAllow));
            // 룰 확인
            await client.Received().ConnectAsync("127.0.0.1", 1234, "닉네임", CancellationToken.None);
            // 클라이언트 접속 확인
        }

        [TestMethod]
        public async Task Game_Join_Chat_Test()
        {
            var option = new ConnectionOption("", 1234, "본인",
                DoubleThreeRuleType.WhiteOnlyAllow, ConnectionType.Client, CancellationToken.None);
            await gameSession.StartSessionAsync(option);

            var me = new Player("본인", PlayerType.Observer, new Record(0, 0, 0));

            bool sessionInitializedRaised = false;

            gameSession.SessionInitialized += (p, ps) => sessionInitializedRaised = true;

            client.ClientJoinResponseReceived += Raise.Event<Action<Player, IEnumerable<Player>>>(
                me,
                new List<Player>() { me }
            );

            Assert.IsTrue(sessionInitializedRaised);
            // 세션 생성 이벤트 확인

            string? msg = null;
            Player? player = null;

            gameSession.ChatReceived += (p, m) =>
            {
                player = p;
                msg = m;
            };

            client.ChatReceived += Raise.Event<Action<Player, string>>(
                new Player("본인", PlayerType.Observer, new Record(1, 2, 3)),
                "채팅"
            );

            Assert.IsNotNull(msg);
            Assert.IsNotNull(player);

            Assert.AreEqual("채팅", msg);
            // 채팅 확인
            Assert.AreEqual(me, player);
            // 채팅 이벤트의 Player가 동일한지 확인
        }

        [TestMethod]
        public async Task Game_End_Test()
        {
            var option = new ConnectionOption("", 1234, "나",
                DoubleThreeRuleType.WhiteOnlyAllow, ConnectionType.Client, CancellationToken.None);
            await gameSession.StartSessionAsync(option);

            var me = new Player("나", PlayerType.Observer, new Record(0, 0, 0));
            var player1 = new Player("상대", PlayerType.Observer, new Record(0, 0, 0));

            client.ClientJoinResponseReceived += Raise.Event<Action<Player, IEnumerable<Player>>>(
                me,
                new List<Player>() { me, player1 }
            );

            client.Me.Returns(me);

            client.GameJoinReceived += Raise.Event<Action<PlayerType, Player>>(
                PlayerType.Black,
                me
            );
            client.GameJoinReceived += Raise.Event<Action<PlayerType, Player>>(
                PlayerType.White,
                player1
            );
            // 흑 백 참여

            client.GameStartReceived += Raise.Event<Action>();
            // 게임 시작
            Assert.IsTrue(gameSession.IsGameStarted);
            Assert.AreEqual(PlayerType.Black, gameSession.CurrentTurn);
            Assert.AreEqual(me, gameSession.BlackPlayer);
            Assert.AreEqual(player1, gameSession.WhitePlayer);
            Assert.IsTrue(gameSession.IsMyTurn);

            client.GameEndReceived += Raise.Event<Action<GameEndMessage>>(
                new GameEndMessage(true, PlayerType.Black, null, "우승"));

            Assert.IsFalse(gameSession.IsGameStarted);
            Assert.AreEqual(1, me.Records.Win);
            Assert.AreEqual(1, player1.Records.Loss);
        }
    }
}
