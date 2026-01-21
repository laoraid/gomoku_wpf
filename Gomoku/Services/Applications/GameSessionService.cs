using CommunityToolkit.Mvvm.Messaging;
using Gomoku.Models;
using Gomoku.Models.DTO;
using Gomoku.Services.Interfaces;
using System.Collections.Concurrent;

namespace Gomoku.Services.Applications
{
    // GomokuManager 조작이 필요한 부분은 여기서 처리하고,
    // 내 턴 계산 등도 여기서 처리하고
    // UI 변경이 필요한 이벤트만 MainViewModel로 넘긴다
    public class GameSessionService : IGameSessionService,
        IRecipient<ClientJoinData>,
        IRecipient<ClientExitData>,
        IRecipient<PositionData>,
        IRecipient<PlaceResponseData>,
        IRecipient<ClientJoinResponseData>,
        IRecipient<GameSyncData>,
        IRecipient<TimePassedData>,
        IRecipient<GameJoinData>,
        IRecipient<GameLeaveData>,
        IRecipient<GameStartData>,
        IRecipient<GameEndData>,
        IRecipient<CancelLastData>
    {
        // 게임 정보 속성
        private readonly ConcurrentDictionary<string, Player> _players = new();

        public Player? BlackPlayer { get; private set; }
        public Player? WhitePlayer { get; private set; }
        public Player? Me => _client?.Me;

        private readonly GomokuManager _Game = new();
        public bool IsGameStarted => _Game.IsGameStarted;
        public PlayerType CurrentTurn => _Game.CurrentPlayer;
        public bool IsSessionAlive => _client != null && _client.IsConnected;
        public bool IsMyTurn => IsGameStarted && Me?.Type == _Game.CurrentPlayer;
        public bool IsOpponentTurn
        {
            get
            {
                if (!IsGameStarted) return false;
                if (IsMyTurn) return false;

                if (Me?.Type == PlayerType.Observer)
                    return false;
                return true;
            }
        }

        private readonly IGameServer _server;
        private IGameClient? _client;

        private readonly IGameClientFactory _gameClientFactory;

        private readonly IMessenger _messenger;

        public string RulesInfo => string.Join('\n', _Game.Rules.Select(r => r.RuleInfoString));
        public int StoneCount => _Game.Board.Count;
        public GameMove? LastStone => _Game.Board.GetLastStonePos();

        public GameSessionService(IGameServer server, IGameClientFactory gameClientFactory, IMessenger messenger)
        {
            _server = server;
            _gameClientFactory = gameClientFactory;
            _messenger = messenger;
            _messenger.RegisterAll(this);
        }

        public Player GetManagedPlayer(Player player)
        {
            return _players.GetOrAdd(player.Nickname, player);
        }

        public void StopSession()
        {
            _server.StopServer();
            _client?.Disconnect();
        }

        public List<(int x, int y)> GetAllForbiddenPositions(PlayerType player)
        {
            return _Game.GetAllForbiddenPositions(player);
        }

        public void Receive(CancelLastData data)
        {
            var type = data.Sender.Type;
            var LeftCancelCount = data.LeftCancelLastCount;

            Logger.Info("무르기 실행됨");
            _Game.CancelLastStone(type, LeftCancelCount);

            if (type == PlayerType.Black)
                BlackPlayer!.LeftCancelLast = LeftCancelCount;
            else if (type == PlayerType.White)
                WhitePlayer!.LeftCancelLast = LeftCancelCount;

            _messenger.Send(new LastStoneCanceledMessage(type, LeftCancelCount));
            _messenger.Send(new TurnChangedMessage(type));
        }

        public void Receive(PlaceResponseData data)
        {
            var msg = new PlaceRejectedMessage(data.Position.Move);
            _messenger.Send(msg);
        }

        public void Receive(TimePassedData data)
        {
            var msg = new TimePassedMessage(data.PlayerType, data.CurrentLeftTimeSeconds);
            _messenger.Send(msg);
        }

        public void Receive(GameStartData data)
        {
            _Game.StartGame();
            _messenger.Send(new GameResetMessage());
            _messenger.Send(new TurnChangedMessage(PlayerType.Black));
            _messenger.Send(new GameStartMessage());
        }

        public void Receive(GameSyncData data)
        {
            var sync = data.SyncData;

            var blackplayer = sync.BlackPlayer == null ? null : GetManagedPlayer(sync.BlackPlayer);
            var whiteplayer = sync.WhitePlayer == null ? null : GetManagedPlayer(sync.WhitePlayer);

            var newsync = new GameSyncMessage(sync.IsGameStarted, sync.MoveHistory, sync.CurrentTurn,
                sync.Rules, blackplayer, whiteplayer);

            BlackPlayer = blackplayer;
            WhitePlayer = whiteplayer;

            _Game.SyncState(newsync);
            _messenger.Send(newsync);
            _messenger.Send(new TurnChangedMessage(newsync.CurrentTurn));
        }

        public void Receive(GameEndData data)
        {
            var end = data.EndData;

            if (end.Winner == PlayerType.Black)
            {
                BlackPlayer?.Records.Win += 1;
                WhitePlayer?.Records.Loss += 1;
            }
            else if (end.Winner == PlayerType.White)
            {
                WhitePlayer?.Records.Win += 1;
                BlackPlayer?.Records.Loss += 1;
            }
            else
            {
                WhitePlayer?.Records.Draw += 1;
                BlackPlayer?.Records.Draw += 1;
            }

            _Game.ForceGameEnd(end.Winner, end.Reason);
            _messenger.Send(end);
        }

        public void Receive(GameLeaveData data)
        {
            var player = GetManagedPlayer(data.Player);
            var type = data.Player.Type;

            if (type == PlayerType.Black)
                BlackPlayer = null;
            else if (type == PlayerType.White)
                WhitePlayer = null;

            player.Type = PlayerType.Observer;
            _messenger.Send(new GameLeftMessage(type, player));
        }

        public void Receive(GameJoinData data)
        {
            var player = GetManagedPlayer(data.Player);
            var type = player.Type;

            if (type == PlayerType.Black)
                BlackPlayer = player;
            else if (type == PlayerType.White)
                WhitePlayer = player;

            player.Type = type;

            _messenger.Send(new GameJoinMessage(type, player));
        }

        public void Receive(ClientExitData data)
        {
            var player = GetManagedPlayer(data.Player);

            if (BlackPlayer == player)
                BlackPlayer = null;
            else if (WhitePlayer == player)
                WhitePlayer = null;

            _players.TryRemove(player.Nickname, out _);
            _messenger.Send(new PlayerDisconnectedMessage(player));
        }

        public void Receive(ClientJoinResponseData data)
        {
            var players = data.Users;

            foreach (var p in players)
            {
                _players.TryAdd(p.Nickname, p);
            }

            _messenger.Send(new SessionInitializedMessage(data.Me, _players.Values));
        }

        public void Receive(ClientJoinData data)
        {
            var player = GetManagedPlayer(data.Player);
            _messenger.Send(new PlayerConnectedMessage(player));
        }

        public void Receive(PositionData data)
        {
            var move = data.Move;

            _Game.TryPlaceStone(move);
            _messenger.Send(new StonePlacedMessage(move));
            _messenger.Send(new TurnChangedMessage(_Game.CurrentPlayer));
        }

        public async Task<bool> StartSessionAsync(ConnectionOption option)
        {
            IGameClient targetclient;

            if (_server.IsRunning)
                _server.StopServer();
            if (_client != null && _client.IsConnected)
                _client.Disconnect();

            targetclient = _gameClientFactory.CreateClient(option.ConnectionType);

            if (option.ConnectionType == ConnectionType.Single)
            {
                if (targetclient is SoloGameClient soloGameClient)
                    soloGameClient.AddRule(RuleFactory.CreateRule(new DoubleThreeRuleInfo(option.DoubleThreeRuleType)));
            }

            _client = targetclient;

            try
            {
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
            catch (OperationCanceledException)
            {   // 사용자 요청으로 취소
                Logger.Info("세션 연결 취소됨");
                StopSession();
                return false;
            }
            catch (Exception e)
            {
                Logger.Error($"세션 연결 중 에러 발생 {e.Message}");
                StopSession();
                throw;
            }
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

        public async Task<bool> CancelLastStoneAsync()
        {
            if (_client == null) return false;
            if (!IsOpponentTurn) throw new NotYourTurnException("무를 수 있는 턴이 아닙니다.");

            if (Me!.LeftCancelLast <= 0)
                throw new CancelNotAvailableException("무르기 횟수가 없습니다.");

            await _client.CancelLastStoneAsync(Me!.LeftCancelLast);
            return true;
        }
    }
}
