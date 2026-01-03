using Gomoku.Models;
using System;
using System.Collections.Generic;
using System.Text;

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

            var pos = new PositionData { X = 7, Y = 10, Player = PlayerType.Black };

            bool result = rule.IsVaildMove(manager, pos);
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

            var pos = new PositionData { X = 7, Y = 5, Player = PlayerType.Black };

            bool result = rule.IsVaildMove(manager, pos);
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void Jump_DoubleThree_Sould_Return_False()
        {
            var manager = CreateManager();
            var rule = CreateRule();

            manager.Board[7, 5] = 1;
            manager.Board[7, 8] = 1;
            manager.Board[7, 9] = 1;

            manager.Board[5, 5] = 1;
            manager.Board[6, 6] = 1;

            var pos = new PositionData { X = 7, Y = 7, Player = PlayerType.Black };

            bool result = rule.IsVaildMove(manager, pos);

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

            var pos = new PositionData { X = 7, Y = 7, Player = PlayerType.Black };

            bool result = rule.IsVaildMove(manager, pos);

            Assert.IsTrue(result);
        }
    }
}
