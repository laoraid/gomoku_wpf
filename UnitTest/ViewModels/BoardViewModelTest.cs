using CommunityToolkit.Mvvm.Messaging;
using Gomoku.Models.Domain;
using Gomoku.Models.DTO;
using Gomoku.Models.Messages;
using Gomoku.Services.Applications.Game;
using Gomoku.Services.Wpf;
using Gomoku.Services.Wpf.Media;
using Gomoku.ViewModels;
using NSubstitute;

namespace UnitTest.ViewModels
{
    [TestClass]
    public class BoardViewModelTest
    {
        private IGameSessionService gameSessionService = null!;
        private IDispatcher dispatcher = null!;
        private ISoundService soundService = null!;
        private IMessenger messenger = null!;
        private ISessionViewModel sessionViewModel = null!;

        private BoardViewModel vm = null!;

        [TestInitialize]
        public void Setup()
        {
            gameSessionService = Substitute.For<IGameSessionService>();
            dispatcher = Substitute.For<IDispatcher>();
            soundService = Substitute.For<ISoundService>();
            messenger = Substitute.For<IMessenger>();

            sessionViewModel = Substitute.For<ISessionViewModel>();

            dispatcher.When(x => x.Invoke(Arg.Any<Action>()))
                      .Do(call => call.Arg<Action>()());
            dispatcher.InvokeAsync(Arg.Any<Action>()).Returns(c =>
            {
                c.Arg<Action>()();
                return Task.CompletedTask;
            });

            vm = new BoardViewModel(gameSessionService, dispatcher, soundService, messenger, sessionViewModel);
        }

        [TestMethod]
        public void PlaceStone_Test()
        {
            vm.Receive(new StonePlacedMessage(new GameMove(5, 5, 0, PlayerType.Black)));
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

            var black = new Player(1, "", "흑", PlayerType.Black, new Record(0, 0, 0));
            var white = new Player(1, "", "백", PlayerType.White, new Record(0, 0, 0));

            gameSessionService.LastStone.Returns(laststone);

            vm.Receive(new GameSyncMessage(true, moves, PlayerType.White, [], black, white));

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

            vm.Session.Me.Returns(new PlayerViewModel(new Player(1, "", "테스트", PlayerType.Black, new Record(0, 0, 0))));

            vm.Receive(new TurnChangedMessage(PlayerType.Black));

            var forbiddenCell = vm.BoardCells[5 * 15 + 7];

            Assert.IsTrue(forbiddenCell.IsForbidden);
        }
    }
}
