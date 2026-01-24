using Gomoku.Models;

namespace Gomoku.Services.Interfaces
{
    public interface IPlayerTrackerService
    {
        Player GetManagedPlayer(Player player);
        void AddPlayers(IEnumerable<Player> players);
        void RemovePlayer(string nickname);
        void Clear();
        public IEnumerable<Player> AllPlayers { get; }

    }
}
