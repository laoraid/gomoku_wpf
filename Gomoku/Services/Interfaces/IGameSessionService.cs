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

        string RulesInfo { get; }

        int StoneCount { get; }

        GameMove? LastStone { get; }

        List<(int x, int y)> GetAllForbiddenPositions(PlayerType player);

        // 이벤트
        public event Action<GameMove>? StonePlaced;
        public event Action<PlayerType>? TurnChanged;
        public event Action<GameEnd>? GameEnded;
        public event Action? GameStarted;
        public event Action? GameReset;

        public event Action<GameMove>? PlaceRejected;
        public event Action<PlayerType, int>? TimeUpdated;

        public event Action<PlayerType, Player>? PlayerGameJoined;
        public event Action<PlayerType, Player>? PlayerGameLeft;

        public event Action<Player, string>? ChatReceived;

        public event Action<Player>? PlayerConnected;
        public event Action<Player>? PlayerDisconnected;
        public event Action<Player, IEnumerable<Player>>? SessionInitialized;
        public event Action<GameSync>? GameSynced;
        public event Action? ConnectionLost;

        // 네트워크
        Task<bool> StartSessionAsync(ConnectionOption option);
        Task PlaceStoneAsync(GameMove move);
        Task SendChatAsync(string message);
        Task JoinGameAsync(PlayerType type);
        Task LeaveGameAsync();
        Task StartGameAsync();
        void StopSession();


    }
}
