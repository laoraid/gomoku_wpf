using System;
using System.Collections.Generic;
using System.Text;

namespace Gomoku
{
    public class InvaildPlaceException : Exception
    {
        public InvaildPlaceException(string msg) : base(msg) { }
    }

    public class OutOfBoardException(string msg) : InvaildPlaceException(msg)
    {
    }

    public class AlreadyPlacedException(string msg) : InvaildPlaceException(msg)
    {
    }

    public class NotYourTurnException(string msg) : InvaildPlaceException(msg)
    {
    }

    public class RuleException(string msg) : InvaildPlaceException(msg)
    {
    }
}
