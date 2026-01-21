using CommunityToolkit.Mvvm.Messaging;
using Gomoku.Models;
using Gomoku.Models.DTO;
using Gomoku.Models.Interfaces;
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
        private IMessenger messenger = null!;

        [TestInitialize]
        public void Setup()
        {
            var GameClientFactory = Substitute.For<IGameClientFactory>();
            client = Substitute.For<IGameClient>();
            server = Substitute.For<IGameServer>();
            messenger = Substitute.For<IMessenger>();

            GameClientFactory.CreateClient(Arg.Any<ConnectionType>()).Returns(client);
            gameSession = new GameSessionService(server, GameClientFactory, messenger);
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
        public async Task Game_End_Test()
        {
            var option = new ConnectionOption("", 1234, "나",
                DoubleThreeRuleType.WhiteOnlyAllow, ConnectionType.Client, CancellationToken.None);
            await gameSession.StartSessionAsync(option);

            var me = new Player("나", PlayerType.Observer, new Record(0, 0, 0));
            var player1 = new Player("상대", PlayerType.Observer, new Record(0, 0, 0));

            gameSession.Receive(new ClientJoinResponseData { Me = me, Users = [player1, me] });

            client.Me.Returns(me);

            gameSession.Receive(new GameJoinData { Player = me, Type = PlayerType.Black });
            gameSession.Receive(new GameJoinData { Player = player1, Type = PlayerType.White });
            // 흑 백 참여

            gameSession.Receive(new GameStartData());
            // 게임 시작
            Assert.IsTrue(gameSession.IsGameStarted);
            Assert.AreEqual(PlayerType.Black, gameSession.CurrentTurn);
            Assert.AreEqual(me, gameSession.BlackPlayer);
            Assert.AreEqual(player1, gameSession.WhitePlayer);
            Assert.IsTrue(gameSession.IsMyTurn);

            gameSession.Receive(new GameEndData { EndData = new GameEndMessage(true, PlayerType.Black, null, "우승") });

            Assert.IsFalse(gameSession.IsGameStarted);
            Assert.AreEqual(1, me.Records.Win);
            Assert.AreEqual(1, player1.Records.Loss);
        }
    }
}
