using CommunityToolkit.Mvvm.DependencyInjection;
using Gomoku.Models;
using Gomoku.Models.DTO;
using Gomoku.Services.Interfaces;

namespace Gomoku.Services.Applications
{
    // GomokuManager 조작이 필요한 부분은 여기서 처리하고,
    // 내 턴 계산 등도 여기서 처리하고
    // UI 변경이 필요한 이벤트만 MainViewModel로 넘긴다
    public class GameSessionService : IGameSessionService
    {
        public bool IsGameStarted => _Game.IsGameStarted;
        public PlayerType CurrentTurn => _Game.CurrentPlayer;
        public bool IsSessionAlive => _client != null && _client.IsConnected;

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


        private readonly IGameServer _server;
        private IGameClient? _client;
        private readonly GomokuManager _Game = new();

        public string RulesInfo => string.Join('\n', _Game.Rules.Select(r => r.RuleInfoString));
        public int StoneCount => _Game.Board.Count;
        public GameMove? LastStone => _Game.Board.GetLastStonePos();

        public GameSessionService(IGameServer server)
        {
            _server = server;

            _Game.OnStonePlaced += m => StonePlaced?.Invoke(m);
            _Game.OnTurnChanged += p => TurnChanged?.Invoke(p);
            _Game.OnGameReset += () => GameReset?.Invoke();
            _Game.OnGameStarted += () => GameStarted?.Invoke();
            _Game.OnGameSync += d => GameSynced?.Invoke(d);
        }

        public void StopSession()
        {
            _server.StopServer();
            _client?.Disconnect();
        }

        public List<(int x, int y)> GetAllForbiddenPositions(PlayerType player)
        {
            var forbiddenlist = new List<(int x, int y)>();

            lock (_Game)
            {
                for (int x = 0; x < GomokuManager.BOARD_SIZE; x++)
                {
                    for (int y = 0; y < GomokuManager.BOARD_SIZE; y++)
                    {
                        if (_Game.Board[x, y] != 0) continue;

                        var testpos = new GameMove(x, y, 0, player);

                        bool isForbidden = false;

                        foreach (var rule in _Game.Rules)
                        {
                            if (!rule.IsValidMove(_Game, testpos))
                            {
                                isForbidden = true;
                                break;
                            }
                        }

                        if (isForbidden)
                            forbiddenlist.Add((x, y));
                    }

                }
            }
            return forbiddenlist;
        }

        private void SetClient(IGameClient client)
        {   // 클라이언트에 이벤트 등록하는 메서드

            if (_client != null)
            {
                _client.Disconnect();
                _client.ConnectionLost -= OnDisConnect;
                _client.PlaceReceived -= PlaceReceived;
                _client.PlaceRejected -= PlaceRejectedReceived;
                _client.ChatReceived -= OnChatReceived;
                _client.PlayerJoinReceived -= PlayerJoinReceived;
                _client.ClientJoinResponseReceived -= ClientJoinResponseReceived;
                _client.PlayerLeftReceived -= PlayerLeaveReceived;
                _client.GameJoinReceived -= GameJoinReceived;
                _client.GameLeaveReceived -= GameLeaveReceived;
                _client.GameEndReceived -= GameEndReceived;
                _client.GameSyncReceived -= GameSyncReceived;
                _client.GameStartReceived -= GameStartReceived;
                _client.TimePassedReceived -= TimePassedReceived;
            }

            _client = client;

            _client.ConnectionLost += OnDisConnect;
            _client.PlaceReceived += PlaceReceived;
            _client.PlaceRejected += PlaceRejectedReceived;
            _client.ChatReceived += OnChatReceived;
            _client.PlayerJoinReceived += PlayerJoinReceived;
            _client.ClientJoinResponseReceived += ClientJoinResponseReceived;
            _client.PlayerLeftReceived += PlayerLeaveReceived;
            _client.GameJoinReceived += GameJoinReceived;
            _client.GameLeaveReceived += GameLeaveReceived;
            _client.GameEndReceived += GameEndReceived;
            _client.GameSyncReceived += GameSyncReceived;
            _client.GameStartReceived += GameStartReceived;
            _client.TimePassedReceived += TimePassedReceived;
        }

        private void PlaceRejectedReceived(GameMove move)
        {
            PlaceRejected?.Invoke(move);
        }

        private void TimePassedReceived(PlayerType type, int time)
        {
            TimeUpdated?.Invoke(type, time);
        }

        private void GameStartReceived()
        {
            _Game.StartGame();
        }

        private void GameSyncReceived(GameSync sync)
        {
            _Game.SyncState(sync);
        }

        private void GameEndReceived(GameEnd end)
        {
            _Game.ForceGameEnd(end.Winner, end.Reason);
            GameEnded?.Invoke(end);
        }

        private void GameLeaveReceived(PlayerType type, Player player)
        {
            PlayerGameLeft?.Invoke(type, player);
        }

        private void GameJoinReceived(PlayerType type, Player player)
        {
            PlayerGameJoined?.Invoke(type, player);
        }

        private void PlayerLeaveReceived(Player player)
        {
            PlayerDisconnected?.Invoke(player);
        }

        private void ClientJoinResponseReceived(Player player, IEnumerable<Player> enumerable)
        {
            SessionInitialized?.Invoke(player, enumerable);
        }

        private void PlayerJoinReceived(Player player)
        {
            PlayerConnected?.Invoke(player);
        }

        private void OnChatReceived(Player player, string arg2)
        {
            ChatReceived?.Invoke(player, arg2);
        }

        private void PlaceReceived(GameMove move)
        {
            lock (_Game)
                _Game.TryPlaceStone(move);
        }

        private void OnDisConnect()
        {
            ConnectionLost?.Invoke();
        }

        public async Task<bool> StartSessionAsync(ConnectionOption option)
        {
            IGameClient targetclient;

            if (_server.IsRunning)
                _server.StopServer();
            if (_client != null && _client.IsConnected)
                _client.Disconnect();

            if (option.ConnectionType == ConnectionType.Single)
            {
                targetclient = Ioc.Default.GetRequiredService<SoloGameClient>();

                if (targetclient is SoloGameClient soloGameClient)
                    soloGameClient.AddRule(RuleFactory.CreateRule(new DoubleThreeRuleInfo(option.DoubleThreeRuleType)));
            }
            else
                targetclient = Ioc.Default.GetRequiredService<IGameClient>();

            SetClient(targetclient);

            switch (option.ConnectionType)
            {
                case ConnectionType.Server:
                    if (_server.IsRunning)
                        _server.StopServer();

                    await _server.StartAsync(option.port);
                    _server.AddRule(RuleFactory.CreateRule(new DoubleThreeRuleInfo(option.DoubleThreeRuleType)));

                    await _client!.ConnectAsync("127.0.0.1", option.port, option.nickname, option.CancellationToken);
                    break;
                case ConnectionType.Client:
                    await _client!.ConnectAsync(option.Ip, option.port, option.nickname, option.CancellationToken);
                    break;
                case ConnectionType.Single:
                    await _client!.ConnectAsync("", 0, "혼자두기", CancellationToken.None);
                    break;
            }

            return true;
        }

        public async Task JoinGameAsync(PlayerType type)
        {
            if (_client != null)
                await _client.SendJoinGameAsync(type);
        }

        public async Task LeaveGameAsync()
        {
            if (_client != null)
                await _client.SendLeaveGameAsync();
        }

        public async Task PlaceStoneAsync(GameMove move)
        {
            if (_client == null || !IsGameStarted) return;

            if (_client.Me!.Type == PlayerType.Observer) return;

            await _client.SendPlaceAsync(move);
        }

        public async Task SendChatAsync(string message)
        {
            if (_client != null)
                await _client.SendChatAsync(message);
        }

        public async Task StartGameAsync()
        {
            if (_client == null) return;

            if (_client.Me!.Type == PlayerType.Observer) return;

            await _client.SendGameStartAsync();
        }
    }
}
