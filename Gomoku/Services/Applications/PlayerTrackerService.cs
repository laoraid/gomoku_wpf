using CommunityToolkit.Mvvm.Messaging;
using Gomoku.Models.Domain;
using Gomoku.Models.Network;
using System.Collections.Concurrent;

namespace Gomoku.Services.Applications
{
    public class PlayerTrackerService : IPlayerTrackerService, IRecipient<ChangeNicknameResponseData>
    {
        private readonly ConcurrentDictionary<string, Player> _players = new();

        public IEnumerable<Player> AllPlayers => _players.Values;

        private readonly IMessenger _messenger;
        public PlayerTrackerService(IMessenger messenger)
        {
            _messenger = messenger;
            _messenger.RegisterAll(this);
        }

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

        public Player GetManagedPlayer(string nickname)
        {
            if (_players.TryGetValue(nickname, out var player))
            {
                return player;
            }
            else
            {
                throw new KeyNotFoundException($"플레이어 '{nickname}'을(를) 찾을 수 없습니다.");
            }
        }

        public void Receive(ChangeNicknameResponseData data)
        {
            if (data.Accepted)
            {
                if (_players.TryRemove(data.OldNickname, out var player) == false)
                {
                    throw new KeyNotFoundException($"플레이어 '{data.OldNickname}'을(를) 찾을 수 없습니다.");
                }

                player.Nickname = data.NewNickname;

                _players.TryAdd(player.Nickname, player);

                _messenger.Send(new PlayerNicknameChangedMessage(
                    player,
                    data.OldNickname,
                    data.NewNickname
                ));
            }
        }

        public void RemovePlayer(string nickname)
        {
            _players.TryRemove(nickname, out var _);
        }
    }
}
