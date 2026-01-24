using Gomoku.Models;
using Gomoku.Services.Interfaces;
using System.Collections.Concurrent;

namespace Gomoku.Services.Applications
{
    public class PlayerTrackerService : IPlayerTrackerService
    {
        private readonly ConcurrentDictionary<string, Player> _players = new();

        public IEnumerable<Player> AllPlayers => _players.Values;

        public void AddPlayers(IEnumerable<Player> players)
        {
            foreach (var player in players)
            {
                _players.TryAdd(player.Nickname, player);
            }
        }

        public void Clear()
        {
            _players.Clear();
        }

        public Player GetManagedPlayer(Player player)
        {
            return _players.GetOrAdd(player.Nickname, player);
        }

        public void RemovePlayer(string nickname)
        {
            _players.TryRemove(nickname, out var _);
        }
    }
}
