using Gomoku.Models;
using Gomoku.Models.DTO;

namespace UnitTest
{
    [TestClass]
    public class DoubleThreeRuleTest
    {
        private GomokuManager CreateManager()
        {
            var manager = new GomokuManager();
            manager.StartGame();
            return manager;
        }

        private DoubleThreeRule CreateRule(DoubleThreeRuleType type = DoubleThreeRuleType.BothForbidden)
        {
            return new DoubleThreeRule(new DoubleThreeRuleInfo(type));
        }

        [TestMethod]
        public void SingleThree_Should_Return_True()
        {
            var manager = CreateManager();
            var rule = CreateRule();

            manager.Board[7, 8] = 1;
            manager.Board[7, 9] = 1;

            var pos = new GameMove(7, 10, 2, PlayerType.Black);

            bool result = rule.IsValidMove(manager, pos);
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void Continuous_DoubleThree_Should_Return_False()
        {
            var manager = CreateManager();
            var rule = CreateRule();

            manager.Board[5, 7] = 1;
            manager.Board[6, 6] = 1;

            manager.Board[7, 6] = 1;
            manager.Board[7, 7] = 1;

            var pos = new GameMove(7, 5, 4, PlayerType.Black);

            bool result = rule.IsValidMove(manager, pos);
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void Jump_DoubleThree_Sould_Return_False()
        {
            var manager = CreateManager();
            var rule = CreateRule();

            manager.Board[7, 5] = 1;
            manager.Board[7, 8] = 1;

            manager.Board[5, 5] = 1;
            manager.Board[6, 6] = 1;

            var pos = new GameMove(7, 7, 5, PlayerType.Black);
            bool result = rule.IsValidMove(manager, pos);

            Assert.IsFalse(result);
        }

        [TestMethod]
        public void ClosedThree_Should_Return_True()
        {
            var manager = CreateManager();
            var rule = CreateRule();

            manager.Board[7, 8] = (int)PlayerType.Black;
            manager.Board[7, 9] = (int)PlayerType.Black;
            manager.Board[7, 10] = (int)PlayerType.White;

            manager.Board[8, 7] = (int)PlayerType.Black;
            manager.Board[9, 7] = (int)PlayerType.Black;

            var pos = new GameMove(7, 7, 5, PlayerType.Black);

            bool result = rule.IsValidMove(manager, pos);

            Assert.IsTrue(result);
        }

        [TestMethod]
        public void FourThree_Should_Return_True()
        {
            var manager = CreateManager();
            var rule = CreateRule();

            manager.Board[7, 6] = 1;
            manager.Board[7, 7] = 1;
            manager.Board[7, 8] = 1;
            // _XXX_

            manager.Board[5, 9] = 1;
            manager.Board[6, 9] = 1;
            // _XX_X_

            // 4-3 인 경우 둘 수 있어야 함

            var pos = new GameMove(7, 9, 5, PlayerType.Black);
            bool result = rule.IsValidMove(manager, pos);

            Assert.IsTrue(result);
        }

        [TestMethod]
        public void FiveInRow_Should_Return_True()
        {
            var manager = CreateManager();
            var rule = CreateRule();

            manager.Board[7, 6] = 1;
            manager.Board[7, 7] = 1;
            manager.Board[7, 8] = 1;
            manager.Board[7, 9] = 1;
            // 4개 _XXXX_

            // _XX_
            manager.Board[5, 10] = 1;
            manager.Board[6, 10] = 1;

            // 대각선 _XX_
            manager.Board[6, 9] = 1;
            manager.Board[5, 8] = 1;

            // 5목이 완성되면서, 쌍삼인 자리에 두는 경우
            var pos = new GameMove(7, 10, 6, PlayerType.Black);
            bool result = rule.IsValidMove(manager, pos);

            Assert.IsTrue(result);
        }

        [TestMethod]
        public void EdgeBoard_WallBlocked_Should_Return_True()
        {
            var manager = CreateManager();
            var rule = CreateRule();

            // _XX_
            manager.Board[0, 1] = 1;
            manager.Board[0, 2] = 1;

            manager.Board[1, 0] = 1;
            manager.Board[2, 0] = 1;
            // 이것도 _XX_ (가로)

            var pos = new GameMove(0, 0, 4, PlayerType.Black);
            bool result = rule.IsValidMove(manager, pos);

            Assert.IsTrue(result);
        }

        [TestMethod]
        public void FiveInRow_But_SixInRow_Should_Return_True()
        {
            var manager = CreateManager();
            var rule = CreateRule();

            manager.Board[7, 4] = 1;
            manager.Board[7, 5] = 1;
            manager.Board[7, 6] = 1;
            manager.Board[7, 8] = 1;
            manager.Board[7, 9] = 1;

            manager.Board[5, 7] = 1;
            manager.Board[6, 7] = 1;
            manager.Board[8, 7] = 1;
            manager.Board[9, 7] = 1;

            // 5목이 되면서 6목도 되는 경우

            var move = new GameMove(7, 7, 0, PlayerType.Black);

            bool isValid = rule.IsValidMove(manager, move);

            Assert.IsTrue(isValid);
        }

        [TestMethod]
        public void SixInRow_Should_Return_False()
        {
            var manager = CreateManager();
            var rule = CreateRule();

            manager.Board[7, 4] = 1;
            manager.Board[7, 5] = 1;
            manager.Board[7, 6] = 1;
            manager.Board[7, 8] = 1;
            manager.Board[7, 9] = 1;

            var move = new GameMove(7, 7, 0, PlayerType.Black);

            // Act
            bool isValid = rule.IsValidMove(manager, move);

            Assert.IsFalse(isValid);
        }
    }
}
