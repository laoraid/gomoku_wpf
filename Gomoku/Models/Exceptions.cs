namespace Gomoku.Models
{
    public class InvalidPlaceException : Exception
    {
        public InvalidPlaceException(string msg) : base(msg) { }
    }

    public class OutOfBoardException(string msg) : InvalidPlaceException(msg)
    {
    }

    public class AlreadyPlacedException(string msg) : InvalidPlaceException(msg)
    {
    }

    public class NotYourTurnException(string msg) : InvalidPlaceException(msg)
    {
    }

    public class RuleException(string msg) : InvalidPlaceException(msg)
    {
    }
}
