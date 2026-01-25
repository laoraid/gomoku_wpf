using CommunityToolkit.Mvvm.Messaging;
using Gomoku.Models;
using Gomoku.Models.DTO;
using Gomoku.Models.Interfaces;
using Gomoku.Services.Applications;
using Gomoku.Services.Interfaces;
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
        private IPlayerTrackerService _playerTrackerService = null!;

        [TestInitialize]
        public void Setup()
        {
            var GameClientFactory = Substitute.For<IGameClientFactory>();
            client = Substitute.For<IGameClient>();
            server = Substitute.For<IGameServer>();
            messenger = Substitute.For<IMessenger>();
            _playerTrackerService = Substitute.For<IPlayerTrackerService>();

            GameClientFactory.CreateClient(Arg.Any<ConnectionType>()).Returns(client);
            gameSession = new GameSessionService(messenger, _playerTrackerService);
            gameSession.Receive(new ClientActivatedMessage(client));
        }

        [TestMethod]
        public async Task Game_End_Test()
        {
            var me = new Player(1, "", "나", PlayerType.Observer, new Record(0, 0, 0));
            var player1 = new Player(2, "", "상대", PlayerType.Observer, new Record(0, 0, 0));

            _playerTrackerService.GetManagedPlayer(Arg.Is<Player>(p => p.Nickname == "나")).Returns(me);
            _playerTrackerService.GetManagedPlayer(Arg.Is<Player>(p => p.Nickname == "상대")).Returns(player1);

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
            var me = new Player(1, "", "나", PlayerType.Observer, new Record(0, 0, 0));
            var player1 = new Player(2, "", "상대", PlayerType.Observer, new Record(0, 0, 0));

            _playerTrackerService.GetManagedPlayer(Arg.Is<Player>(p => p.Nickname == "나")).Returns(me);
            _playerTrackerService.GetManagedPlayer(Arg.Is<Player>(p => p.Nickname == "상대")).Returns(player1);

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

            _playerTrackerService.GetManagedPlayer(Arg.Is<Player>(p => p.Nickname == "흑")).Returns(player1);
            _playerTrackerService.GetManagedPlayer(Arg.Is<Player>(p => p.Nickname == "백")).Returns(player2);

            gameSession.Receive(new GameJoinData { Player = player1, Type = PlayerType.Black });
            gameSession.Receive(new GameJoinData { Player = player2, Type = PlayerType.White });

            gameSession.Receive(new GameStartedData { BlackPlayer = player1, WhitePlayer = player2 });

            gameSession.Receive(new PlayerDisconnectedInternalMessage(player1));
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
