using Gomoku.Models;
using Gomoku.Models.DTO;

namespace Gomoku.Services.Interfaces
{
    public interface IGameSessionService
    {
        public Player? BlackPlayer { get; }
        public Player? WhitePlayer { get; }
        public Player? Me { get; }

        bool IsSessionAlive { get; }

        bool IsGameStarted { get; }
        PlayerType CurrentTurn { get; }
        bool IsMyTurn { get; }
        bool IsOpponentTurn { get; }
        bool CanCancelLast { get; }

        string RulesInfo { get; }

        int StoneCount { get; }

        GameMove? LastStone { get; }

        List<(int x, int y)> GetAllForbiddenPositions(PlayerType player);

        Task PlaceStoneAsync(GameMove move);
        Task SendChatAsync(string message);
        Task JoinGameAsync(PlayerType type);
        Task LeaveGameAsync();
        Task StartGameAsync();
        Task<bool> CancelLastStoneAsync();
    }
}
