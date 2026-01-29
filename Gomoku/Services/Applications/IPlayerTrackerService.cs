using Gomoku.Models.Domain;

namespace Gomoku.Services.Applications
{
    public interface IPlayerTrackerService
    {
        Player GetManagedPlayer(Player player);
        Player GetManagedPlayer(string nickname);
        void AddPlayers(IEnumerable<Player> players);
        void RemovePlayer(string nickname);
        void Clear();
        public IEnumerable<Player> AllPlayers { get; }

    }
}
