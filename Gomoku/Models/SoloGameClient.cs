using Gomoku.Models.DTO;

namespace Gomoku.Models
{
    public class SoloGameClient : IGameClient
    {
        public Player? Me { get; private set; }
        private readonly GomokuManager _manager = new GomokuManager();

        public bool IsConnected => true;

        public event Action<GameMove>? PlaceReceived;
        public event Action<Player, string>? ChatReceived;
        public event Action<Player>? PlayerJoinReceived;
        public event Action<Player>? PlayerLeaveReceived;
        public event Action<GameMove>? CantPlaceReceived;
        public event Action<Player, IEnumerable<Player>>? ClientJoinResponseReceived;
        public event Action<GameSync>? GameSyncReceived;
        public event Action<PlayerType, int>? TimePassedReceived;
        public event Action<PlayerType, Player>? GameJoinReceived;
        public event Action<PlayerType, Player>? GameLeaveReceived;
        public event Action? GameStartReceived;
        public event Action<PlayerType, string>? GameEndReceived;
        public event Action? ConnectionLost;

        public SoloGameClient()
        {
            _manager.OnGameEnded += async (t, r) =>
            {
                await SendDataAsync(new GameEndData { Reason = r, Winner = t });
                GameJoinReceived?.Invoke(PlayerType.Black, Me!);
            };
        }

        public async Task<bool> ConnectAsync(string ip, int port, string nickname, CancellationToken cts)
        {
            Me = new Player()
            {
                Nickname = nickname,
            };
            ClientJoinResponseReceived?.Invoke(Me, new List<Player> { Me });
            GameSyncReceived?.Invoke(new GameSync(new List<GameMove>(), PlayerType.Black,
                _manager.Rules.Select(r => r.RuleInfo), null, null));
            return true;
        }

        public void Disconnect()
        {
        }

        public Task SendChatAsync(string message)
        {
            ChatReceived?.Invoke(Me!, message);
            return Task.CompletedTask;
        }

        public Task SendDataAsync(GameData data)
        {
            switch (data)
            {
                case ChatData cd:
                    ChatReceived?.Invoke(cd.Sender, cd.Message);
                    break;
                case GameEndData ged:
                    GameEndReceived?.Invoke(ged.Winner, ged.Reason);
                    break;
                case TimePassedData tpd:
                    TimePassedReceived?.Invoke(tpd.PlayerType, tpd.CurrentLeftTimeSeconds);
                    break;
                default:
                    break;
            }
            return Task.CompletedTask;
        }

        public async Task SendGameStartAsync()
        {
            _manager.StartGame();
            GameStartReceived?.Invoke();
            await Task.CompletedTask;
        }

        public async Task SendJoinGameAsync(PlayerType type)
        {
            GameJoinReceived?.Invoke(type, Me!);

            if (type != PlayerType.Observer)
                Me!.Type = PlayerType.Black;

            await Task.CompletedTask;
        }

        public async Task SendLeaveGameAsync()
        {
            await SendDataAsync(new GameLeaveData { Player = Me! });
        }

        public async Task SendPlaceAsync(GameMove move)
        {
            try
            {
                var nextturn = move.PlayerType == PlayerType.Black ? PlayerType.White : PlayerType.Black;

                Me!.Type = nextturn;
                GameJoinReceived?.Invoke(nextturn, Me!);

                _manager.TryPlaceStone(move);
                PlaceReceived?.Invoke(move);

                if (_manager.IsWin(move))
                {
                    Me!.Type = PlayerType.Black;
                    GameJoinReceived?.Invoke(PlayerType.Black, Me!);
                }
            }
            catch
            {
                CantPlaceReceived?.Invoke(move);
            }
            await Task.CompletedTask;
        }

        public void AddRule(Rule rule)
        {
            _manager.Rules.Add(rule);
        }
    }
}
