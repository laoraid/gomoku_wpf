using Gomoku.Models.Common;
using Gomoku.Models.Domain;

namespace Gomoku.Models.Network
{
    public interface IGameServer : IAsyncDisposable
    {
        bool IsRunning { get; }

        Task StartAsync(ConnectionOption option);
        void StartGame();
        void AddRule(Rule rule);
    }
}
