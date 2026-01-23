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

    public class CancelNotAvailableException(string msg) : Exception(msg) { }

    public class GameNotStartException(string msg) : Exception(msg) { }

    public class IdDuplicateException(string msg) : Exception(msg) { }
    public class PasswordWrongException(string msg) : Exception(msg) { }
    public class AccountNotExistException(string msg) : Exception(msg) { }
    public class GuestPlayerException(string msg) : Exception(msg) { }
}
