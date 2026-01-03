using Gomoku.Models;
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

            var move = new PositionData { X = 7, Y = 7, Player = PlayerType.Black };
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

            for (int x = 0; x < 4; x++)
            {
                manager.TryPlaceStone(new PositionData { X = x, Y = 0, Player = PlayerType.Black });
                manager.TryPlaceStone(new PositionData { X = x, Y = 1, Player = PlayerType.White });
            }

            var winplace = new PositionData { X = 4, Y = 0, Player = PlayerType.Black };
            Assert.IsTrue(manager.TryPlaceStone(winplace));

            bool iswin = manager.CheckWin(winplace);

            Assert.IsTrue(iswin);
            Assert.IsFalse(manager.IsGameStarted);
        }

        [TestMethod]
        public void TryPlaceStone_WrongPlace_Throw_Exceptions()
        {
            var manager = new GomokuManager();

            manager.StartGame();

            var move = new PositionData { X = 5, Y = 5, Player = PlayerType.Black };
            manager.TryPlaceStone(move);

            Assert.Throws<AlreadyPlacedException>(() =>
            {
                manager.TryPlaceStone(new PositionData { X = 5, Y = 5, Player = PlayerType.White });
            });
        }
    }
}
