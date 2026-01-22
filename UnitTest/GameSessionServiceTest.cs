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

        private ConnectionOption defaultoption = new ConnectionOption("", 1234, "닉네임",
                DoubleThreeRuleType.WhiteOnlyAllow, ConnectionType.Server, CancellationToken.None, 3);

        [TestInitialize]
        public void Setup()
        {
            var GameClientFactory = Substitute.For<IGameClientFactory>();
            client = Substitute.For<IGameClient>();
            server = Substitute.For<IGameServer>();
            messenger = Substitute.For<IMessenger>();

            GameClientFactory.CreateClient(Arg.Any<ConnectionType>()).Returns(client);
            gameSession = new GameSessionService(GameClientFactory, messenger, () => server);
        }

        private async Task SetupClient()
        {
            await gameSession.StartSessionAsync(defaultoption);
        }

        [TestMethod]
        public async Task StartSession_Test()
        {
            await SetupClient();
            await server.Received().StartAsync(defaultoption);
            // 옵션 그대로 server에서 생성했는지 확인
            server.Received().AddRule(Arg.Is<DoubleThreeRule>(r => r.DTRuleType == DoubleThreeRuleType.WhiteOnlyAllow));
            // 룰 확인
            await client.Received().ConnectAsync("127.0.0.1", 1234, "닉네임", CancellationToken.None);
            // 클라이언트 접속 확인
        }

        [TestMethod]
        public async Task Game_End_Test()
        {
            await SetupClient();
            var me = new Player("나", PlayerType.Observer, new Record(0, 0, 0));
            var player1 = new Player("상대", PlayerType.Observer, new Record(0, 0, 0));

            gameSession.Receive(new ClientJoinResponseData { Me = me, Users = [player1, me] });

            client.Me.Returns(me);

            gameSession.Receive(new GameJoinData { Player = me, Type = PlayerType.Black });
            gameSession.Receive(new GameJoinData { Player = player1, Type = PlayerType.White });
            // 흑 백 참여

            gameSession.Receive(new GameStartedData { BlackPlayer = me, WhitePlayer = player1 });
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

        [TestMethod]
        public async Task PlaceStone_Should_Change_Turn()
        {
            await SetupClient();

            var me = new Player("나", PlayerType.Observer, new Record(0, 0, 0));
            var player1 = new Player("상대", PlayerType.Observer, new Record(0, 0, 0));

            gameSession.Receive(new ClientJoinResponseData { Me = me, Users = [player1, me] });

            client.Me.Returns(me);

            gameSession.Receive(new GameJoinData { Player = me, Type = PlayerType.Black });
            gameSession.Receive(new GameJoinData { Player = player1, Type = PlayerType.White });

            gameSession.Receive(new GameStartedData { BlackPlayer = me, WhitePlayer = player1 });

            var move = new GameMove(5, 5, 1, PlayerType.Black);
            gameSession.Receive(new PositionData { Move = move });

            Assert.IsTrue(gameSession.IsGameStarted);
            Assert.AreEqual(PlayerType.White, gameSession.CurrentTurn);
        }

        [TestMethod]
        public void JoinPlayer_Disconnect_Should_Be_Null()
        {
            var player1 = new Player();
            player1.Nickname = "흑";
            var player2 = new Player();
            player2.Nickname = "백";
            gameSession.Receive(new ClientJoinData { Player = player1 });
            gameSession.Receive(new ClientJoinData { Player = player2 });

            gameSession.Receive(new GameJoinData { Player = player1, Type = PlayerType.Black });
            gameSession.Receive(new GameJoinData { Player = player2, Type = PlayerType.White });

            gameSession.Receive(new GameStartedData { BlackPlayer = player1, WhitePlayer = player2 });

            gameSession.Receive(new ClientExitData { Player = player1 });
            // 게임 도중 흑 플레이어 나감
            gameSession.Receive(new GameEndData { EndData = new GameEndMessage(true, PlayerType.White, null, "나감") });

            Assert.AreEqual(1, player2.Records.Win);
            // 백 플레이어 1승 해야 함

            Assert.IsFalse(gameSession.IsGameStarted);
            Assert.IsNull(gameSession.BlackPlayer);
            Assert.IsNotNull(gameSession.WhitePlayer);
        }
    }
}
