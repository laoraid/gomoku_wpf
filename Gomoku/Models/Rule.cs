using System.Text.Json.Serialization;

namespace Gomoku.Models
{
    [JsonDerivedType(typeof(DoubleThreeRuleInfo), nameof(DoubleThreeRuleInfo))]
    public abstract record RuleInfo;
    public record DoubleThreeRuleInfo(DoubleThreeRuleType RuleType) : RuleInfo;

    public static class RuleFactory
    {
        public static Rule CreateRule(RuleInfo info)
        {
            switch (info)
            {
                case DoubleThreeRuleInfo dt:
                    return new DoubleThreeRule(dt);
                default:
                    throw new InvalidOperationException("없는 룰을 생성하려고 함");
            }

        }
    }
    public abstract class Rule(RuleInfo info)
    {
        public abstract bool IsValidMove(GomokuManager manager, PositionData pos);
        public string ViolationMessage { get; protected set; } = string.Empty;
        public RuleInfo RuleInfo { get; } = info;
        public abstract string RuleInfoString { get; }
    }

    public enum DoubleThreeRuleType
    {
        BothAllowed, WhiteOnly, BothForbidden
    }

    public class DoubleThreeRule(DoubleThreeRuleInfo ruleInfo) : Rule(ruleInfo)
    {
        public readonly DoubleThreeRuleType DTRuleType = ruleInfo.RuleType;

        public override string RuleInfoString
        {
            get
            {
                if (DTRuleType == DoubleThreeRuleType.WhiteOnly)
                    return "백만 쌍삼 허용";
                else if (DTRuleType == DoubleThreeRuleType.BothForbidden)
                    return "둘 다 쌍삼 금지";
                else if (DTRuleType == DoubleThreeRuleType.BothAllowed)
                    return "둘 다 쌍삼 허용";

                throw new InvalidOperationException("불가능한 룰 타입");
            }
        }

        public override bool IsValidMove(GomokuManager manager, PositionData pos)
        {
            if (DTRuleType == DoubleThreeRuleType.BothAllowed) return true;
            if (DTRuleType == DoubleThreeRuleType.WhiteOnly && pos.Player == PlayerType.White) return true;

            int x = pos.X;
            int y = pos.Y;
            PlayerType player = pos.Player;

            int openthreecount = 0;

            manager.Board[x, y] = (int)player; // 임시 착수

            int[] dx = { 1, 0, 1, 1 };
            int[] dy = { 0, 1, 1, -1 };

            for (int i = 0; i < 4; i++) // 8방향
            {
                if (CheckOpenThree(manager, x, y, dx[i], dy[i], player))
                {
                    openthreecount++;
                }
            }

            manager.Board[x, y] = 0;

            if (openthreecount >= 2)
            {
                ViolationMessage = "쌍삼입니다.";
                Logger.Debug($"{x}, {y} 착수 불가 : 쌍삼");
                return false;
            }
            return true;
        }
        /// <summary>
        /// 특정 방향으로 열린 3목인지 판별합니다.
        /// </summary>
        /// <param name="manager"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="v1"></param>
        /// <param name="v2"></param>
        /// <param name="player"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        private bool CheckOpenThree(GomokuManager manager, int x, int y, int dx, int dy, PlayerType player)
        {
            // 열린 3 : 수를 하나 더 두었을때 열린 4가 되는 3
            // 열린 4 : _1111_ 형태(양끝이 비어있는 형태)

            int color = (int)player;

            for (int offset = -4; offset <= 4; offset++)
            {   // 8방향, 4칸 탐색
                int nx = x + dx * offset;
                int ny = y + dy * offset;

                if (GomokuManager.IsValidPos(nx, ny) && manager.Board[nx, ny] == 0)
                { // 탐색할 칸이 비어있는지
                    manager.Board[nx, ny] = color; // 임시 착수
                    if (CheckOpenFour(manager, nx, ny, dx, dy, color)) // 해봤더니 열린 4가 되면?
                    {
                        manager.Board[nx, ny] = 0; // 임시 착수 되돌리기
                        return true; // 열린 3
                    }
                    manager.Board[nx, ny] = 0;
                }
            }

            return false;

        }

        private bool CheckOpenFour(GomokuManager manager, int x, int y, int dx, int dy, int color)
        {
            // 왼쪽, 오른쪽으로 탐색하면서 
            int count = 1;

            // 정방향으로 연속 돌 확인
            for (int i = 1; i < 5; i++)
            {
                int nx = x + dx * i, ny = y + dy * i;
                if (GomokuManager.IsValidPos(nx, ny) && manager.Board[nx, ny] == color) count++;
                else break;
            }

            // 역방향 연속 돌 확인
            for (int i = 1; i < 5; i++)
            {
                int nx = x - dx * i, ny = y - dy * i;
                if (GomokuManager.IsValidPos(nx, ny) && manager.Board[nx, ny] == color) count++;
                else break;
            }

            if (count != 4) return false; // 돌이 딱 4개 아니면 열린 4 아님

            return CheckBothOpen(manager, x, y, dx, dy, color);
        }

        private bool CheckBothOpen(GomokuManager manager, int x, int y, int dx, int dy, int color)
        {
            // 열린 4: 양쪽이 다 열려있어야 열린 4임(벽에 막혀도 안됨)

            // 같은색인 맨 오ㅓ른쪽 찾기
            int rightX = x, rightY = y;
            while (GomokuManager.IsValidPos(rightX + dx, rightY + dy) && manager.Board[rightX + dx, rightY + dy] == color)
            {
                rightX += dx; rightY += dy;
            }

            // 같은 색인 맨 왼쪽 찾기
            int leftX = x, leftY = y;
            while (GomokuManager.IsValidPos(leftX - dx, leftY - dy) && manager.Board[leftX - dx, leftY - dy] == color)
            {
                leftX -= dx; leftY -= dy;
            }

            // 맨 끝 다음 - _XXXX_ 이면 왼쪽 _ 이나 오른쪽 _ 찾는거
            int outerRightX = rightX + dx, outerRightY = rightY + dy;
            int outerLeftX = leftX - dx, outerLeftY = leftY - dy;

            // 양쪽이 비어있어야 열린 4임 
            if (GomokuManager.IsValidPos(outerRightX, outerRightY) && manager.Board[outerRightX, outerRightY] == 0 &&
                GomokuManager.IsValidPos(outerLeftX, outerLeftY) && manager.Board[outerLeftX, outerLeftY] == 0)
                return true;


            return false;
        }
    }
}
