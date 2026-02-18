namespace Gomoku.ViewModels
{
    public interface ISessionViewModel
    {
        PlayerViewModel? Me { get; }
        PlayerViewModel? BlackPlayer { get; }
        PlayerViewModel? WhitePlayer { get; }
        bool IsGameStarted { get; }
        bool CanShowStartButton { get; }

        bool IsMyTurn { get; }
        bool IsOpponentTurn { get; }
        bool CanCancelLast { get; }
    }
}
