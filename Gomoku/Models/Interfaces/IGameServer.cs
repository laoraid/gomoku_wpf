using Gomoku.Models.DTO;

namespace Gomoku.Models.Interfaces
{
    public interface IGameServer
    {
        bool IsRunning { get; }

        Task StartAsync(ConnectionOption option);
        void StopServer();

        void StartGame();
        void AddRule(Rule rule);

        Task Broadcast(GameData data);
    }
}
