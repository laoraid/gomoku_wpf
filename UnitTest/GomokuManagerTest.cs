using Gomoku.Models;
using Gomoku.Models.DTO;
namespace UnitTest
{
    [TestClass]
    public sealed class GomokuMangerTest
    {
        [TestMethod]
        public void TryPlaceStone_ChangeTurn_After_ValidMove()
        {
            var manager = new GomokuManager();

            Assert.IsFalse(manager.IsGameStarted);

            manager.ResetGame();

            Assert.IsFalse(manager.IsGameStarted);

            manager.StartGame();

            Assert.IsTrue(manager.IsGameStarted);
            Assert.AreEqual(PlayerType.Black, manager.CurrentPlayer);

            var move = new GameMove(7, 7, 0, PlayerType.Black);
            var result = manager.TryPlaceStone(move);

            Assert.IsTrue(result);
            Assert.AreEqual(PlayerType.White, manager.CurrentPlayer);
            Assert.AreEqual(1, manager.GetStoneAt(7, 7));
        }

        [TestMethod]
        public void CheckWin_True_When_FiveInRow()
        {
            var manager = new GomokuManager();

            manager.StartGame();
            int count = 0;

            for (int x = 0; x < 4; x++)
            {
                manager.TryPlaceStone(new GameMove(x, 0, count, PlayerType.Black));
                count++;
                manager.TryPlaceStone(new GameMove(x, 1, count, PlayerType.White));
                count++;
            }

            var winplace = new GameMove(4, 0, count, PlayerType.Black);
            Assert.IsTrue(manager.TryPlaceStone(winplace));

            bool iswin = manager.IsWin(winplace);

            Assert.IsTrue(iswin);
            Assert.IsFalse(manager.IsGameStarted);
        }

        [TestMethod]
        public void TryPlaceStone_WrongPlace_Throw_Exceptions()
        {
            var manager = new GomokuManager();

            manager.StartGame();

            var move = new GameMove(5, 5, 0, PlayerType.Black);
            manager.TryPlaceStone(move);

            Assert.Throws<AlreadyPlacedException>(() =>
            {
                manager.TryPlaceStone(new GameMove(5, 5, 1, PlayerType.White));
            });
        }
    }
}
