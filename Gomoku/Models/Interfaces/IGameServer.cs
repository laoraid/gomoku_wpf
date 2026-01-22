using Gomoku.Models.DTO;

namespace Gomoku.Models.Interfaces
{
    public interface IGameServer : IAsyncDisposable
    {
        bool IsRunning { get; }

        Task StartAsync(ConnectionOption option);
        void StartGame();
        void AddRule(Rule rule);
    }
}
