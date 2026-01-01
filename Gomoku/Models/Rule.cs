using System;
using System.Collections.Generic;
using System.Text;

namespace Gomoku.Models
{
    public abstract class Rule
    {
        public abstract bool IsVaildMove(GomokuManager manager, PositionData pos);
        public readonly string violation_message = string.Empty;
    }
}
