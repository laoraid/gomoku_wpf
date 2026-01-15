using Gomoku.Models.DTO;
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
        public abstract bool IsValidMove(GomokuManager manager, GameMove pos);
        public string ViolationMessage { get; protected set; } = string.Empty;
        public RuleInfo RuleInfo { get; } = info;
        public abstract string RuleInfoString { get; }
    }

    public enum DoubleThreeRuleType
    {
        BothAllowed, WhiteOnlyAllow, BothForbidden
    }

    public class DoubleThreeRule(DoubleThreeRuleInfo ruleInfo) : Rule(ruleInfo)
    {
        public readonly DoubleThreeRuleType DTRuleType = ruleInfo.RuleType;

        public override string RuleInfoString
        {
            get
            {
                if (DTRuleType == DoubleThreeRuleType.WhiteOnlyAllow)
                    return "백만 쌍삼 허용";
                else if (DTRuleType == DoubleThreeRuleType.BothForbidden)
                    return "둘 다 쌍삼 금지";
                else if (DTRuleType == DoubleThreeRuleType.BothAllowed)
                    return "둘 다 쌍삼 허용";

                throw new InvalidOperationException("불가능한 룰 타입");
            }
        }

        public override bool IsValidMove(GomokuManager manager, GameMove pos)
        {
            if (DTRuleType == DoubleThreeRuleType.BothAllowed) return true;
            if (DTRuleType == DoubleThreeRuleType.WhiteOnlyAllow && pos.PlayerType == PlayerType.White) return true;

            int x = pos.X;
            int y = pos.Y;
            PlayerType playertype = pos.PlayerType;

            var seq = manager.GetLineSequences(x, y, playertype);

            int openthreecount = 0;
            int fourcount = 0;

            bool hasFive = false;
            bool hasOverSix = false;

            foreach (var linestr in seq)
            {
                if (CheckFive(linestr))
                {
                    hasFive = true;
                    continue;
                }

                if (linestr.Contains("111111"))
                {
                    if (DTRuleType == DoubleThreeRuleType.BothForbidden ||
                        (DTRuleType == DoubleThreeRuleType.WhiteOnlyAllow && pos.PlayerType == PlayerType.Black))
                    {
                        hasOverSix = true;
                        continue;
                    }
                }

                if (CheckFour(linestr))
                    fourcount++;

                if (CheckOpenThree(linestr))
                    openthreecount++;
            }

            if (hasFive) return true;

            if (hasOverSix)
            {
                ViolationMessage = "6목 이상 금수입니다.";
                return false;
            }

            if (fourcount >= 2)
            {
                ViolationMessage = "4-4 금수입니다.";
                return false;
            }

            if (openthreecount >= 2)
            {
                ViolationMessage = "3-3 금수입니다.";
                return false;
            }

            return true;
        }

        private bool CheckPatterns(string[] patterns, string line)
        {
            int centeridx = 4;  // 두는 돌이 포함되어야 함

            foreach (var p in patterns)
            {
                int startpos = 0;

                while ((startpos = line.IndexOf(p, startpos)) != -1)
                {
                    if (centeridx >= startpos && centeridx < startpos + p.Length)
                        return true;
                    startpos++;
                }
            }
            return false;
        }

        private bool CheckOpenThree(string line)
        {
            string[] patterns = { "01110", "010110", "011010" };
            return CheckPatterns(patterns, line);
        }

        private bool CheckFour(string line)
        {
            string[] patterns = { "01111", "11110", "11011", "10111", "11101" };
            return CheckPatterns(patterns, line);
        }

        private bool CheckFive(string line)
        {
            int idx = line.IndexOf("11111");

            if (idx != -1)
            {
                bool isfrontme = (idx > 0 && line[idx - 1] == '1');
                bool isendme = (idx + 5 < line.Length && line[idx + 5] == '1');

                if (!isfrontme && !isendme) return true;
            }
            return false;
        }
    }
}
