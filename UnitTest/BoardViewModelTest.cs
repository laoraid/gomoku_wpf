using Gomoku.Models;
using Gomoku.Models.DTO;
using Gomoku.Services.Interfaces;
using Gomoku.ViewModels;
using NSubstitute;

namespace UnitTest
{
    [TestClass]
    public class BoardViewModelTest
    {
        private IGameSessionService gameSessionService = null!;
        private IDispatcher dispatcher = null!;
        private IMessageBoxService messageBoxService = null!;
        private ISoundService soundService = null!;

        private BoardViewModel vm = null!;

        [TestInitialize]
        public void Setup()
        {
            gameSessionService = Substitute.For<IGameSessionService>();
            dispatcher = Substitute.For<IDispatcher>();
            messageBoxService = Substitute.For<IMessageBoxService>();
            soundService = Substitute.For<ISoundService>();

            dispatcher.Invoke(Arg.Do<Action>(f => f()));
            dispatcher.InvokeAsync(Arg.Do<Action>(f => f()));

            vm = new BoardViewModel(gameSessionService, dispatcher, messageBoxService, soundService);
        }

        [TestMethod]
        public void PlaceStone_Test()
        {
            gameSessionService.StonePlaced += Raise.Event<Action<GameMove>>(new GameMove(5, 5, 0, PlayerType.Black));
            // 5,5 에 돌 놓기

            var cell = vm.BoardCells.First(c => c.X == 5 && c.Y == 5);

            Assert.IsNotNull(cell);
            Assert.AreEqual(5, cell.X);
            Assert.AreEqual(5, cell.Y);

            Assert.IsTrue(cell.IsLastStone);
            Assert.IsFalse(cell.IsWinStone);
            Assert.IsFalse(cell.IsForbidden);
        }

        [TestMethod]
        public void GameSync_Test()
        {
            var laststone = new GameMove(5, 7, 3, PlayerType.Black);
            var moves = new List<GameMove>()
            {
                new(5, 5, 1, PlayerType.Black),
                new(5, 6, 2, PlayerType.White),
                laststone,
            };

            var black = new Player("흑", PlayerType.Black, new Record(0, 0, 0));
            var white = new Player("백", PlayerType.White, new Record(0, 0, 0));

            gameSessionService.LastStone.Returns(laststone);

            var syncdata = new GameSyncMessage(true, moves, PlayerType.White, [], black, white);
            gameSessionService.GameSynced += Raise.Event<Action<GameSyncMessage>>(syncdata);

            var cell1 = vm.BoardCells.First(c => c.X == 5 && c.Y == 5);
            var cell2 = vm.BoardCells.First(c => c.X == 5 && c.Y == 6);
            var cell3 = vm.BoardCells.First(c => c.X == 5 && c.Y == 7);


            List<CellViewModel> cells = [cell1, cell2, cell3];

            foreach (var cell in cells)
            {
                Assert.IsNotNull(cell);
                Assert.IsFalse(cell.IsWinStone);
                Assert.IsFalse(cell.IsForbidden);

                if (cell.Y == 7)
                    Assert.IsTrue(cell.IsLastStone);
                else
                    Assert.IsFalse(cell.IsLastStone);
            }
        }

        [TestMethod]
        public void Forbidden_Mark_Test()
        {
            var moves = new List<GameMove>
            {
                new(5, 7, 1, PlayerType.Black),
                new(6, 6, 2, PlayerType.Black),
                new(7, 6, 3, PlayerType.Black),
                new(7, 7, 4, PlayerType.Black),
            };

            gameSessionService.GetAllForbiddenPositions(PlayerType.Black).Returns([(7, 5)]);
            gameSessionService.IsGameStarted.Returns(true);
            gameSessionService.IsMyTurn.Returns(true);

            vm.Me = new PlayerViewModel(new Player("테스트", PlayerType.Black, new Record(0, 0, 0)));

            gameSessionService.TurnChanged += Raise.Event<Action<PlayerType>>(PlayerType.Black);

            var forbiddenCell = vm.BoardCells[5 * 15 + 7];

            Assert.IsTrue(forbiddenCell.IsForbidden);
        }
    }
}
