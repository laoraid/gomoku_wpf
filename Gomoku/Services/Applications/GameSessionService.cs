using CommunityToolkit.Mvvm.Messaging;
using Gomoku.Models;
using Gomoku.Models.DTO;
using Gomoku.Models.Interfaces;
using Gomoku.Services.Interfaces;
using System.Data;

namespace Gomoku.Services.Applications
{
    // GomokuManager 조작이 필요한 부분은 여기서 처리하고,
    // 내 턴 계산 등도 여기서 처리하고
    // UI 변경이 필요한 이벤트만 MainViewModel로 넘긴다
    public class GameSessionService : IGameSessionService,
        IRecipient<ClientJoinData>,
        IRecipient<PositionData>,
        IRecipient<PlaceResponseData>,
        IRecipient<GameSyncData>,
        IRecipient<TimePassedData>,
        IRecipient<GameJoinData>,
        IRecipient<GameLeaveData>,
        IRecipient<GameStartedData>,
        IRecipient<GameEndData>,
        IRecipient<CancelLastData>,
        IRecipient<PlayerDisconnectedInternalMessage>,
        IRecipient<ClientActivatedMessage>,
        IRecipient<ClientDeactivatedMessage>
    {
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

                if (_client != null && !_client.HasOpponent) return false;

                if (IsMyTurn) return false;

                if (Me?.Type == PlayerType.Observer)
                    return false;
                return true;
            }
        }
        public bool CanCancelLast
        {
            get
            {
                if (!IsGameStarted || Me == null || Me.Type == PlayerType.Observer) return false;
                if (Me.LeftCancelLast <= 0) return false;
                if (StoneCount <= 0) return false;

                if (_client != null && !_client.HasOpponent) return true;
                return IsOpponentTurn;
            }
        }

        private IGameClient? _client;
        private readonly IMessenger _messenger;
        private readonly IPlayerTrackerService _playerTracker;

        public string RulesInfo => string.Join('\n', _Game.Rules.Select(r => r.RuleInfoString));
        public int StoneCount => _Game.Board.Count;
        public GameMove? LastStone => _Game.Board.GetLastStonePos();

        public GameSessionService(IMessenger messenger, IPlayerTrackerService playerTracker)
        {
            _messenger = messenger;
            _messenger.RegisterAll(this);
            _playerTracker = playerTracker;
        }

        public List<(int x, int y)> GetAllForbiddenPositions(PlayerType player)
        {
            return _Game.GetAllForbiddenPositions(player);
        }

        public void Receive(CancelLastData data)
        {
            var type = data.SenderType;
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

        public void Receive(GameStartedData data)
        {
            _Game.StartGame();
            BlackPlayer = _playerTracker.GetManagedPlayer(data.BlackPlayer);
            BlackPlayer!.LeftCancelLast = data.BlackPlayer.LeftCancelLast;
            WhitePlayer = _playerTracker.GetManagedPlayer(data.WhitePlayer);
            WhitePlayer!.LeftCancelLast = data.WhitePlayer.LeftCancelLast;

            _messenger.Send(new GameResetMessage());
            _messenger.Send(new TurnChangedMessage(PlayerType.Black));
            _messenger.Send(new GameStartMessage());
        }

        public void Receive(GameSyncData data)
        {
            var sync = data.SyncData;

            var blackplayer = sync.BlackPlayer == null ? null : _playerTracker.GetManagedPlayer(sync.BlackPlayer);
            var whiteplayer = sync.WhitePlayer == null ? null : _playerTracker.GetManagedPlayer(sync.WhitePlayer);

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
            var player = _playerTracker.GetManagedPlayer(data.Player);
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
            var player = _playerTracker.GetManagedPlayer(data.Player);
            var type = data.Type;

            if (type == PlayerType.Black)
                BlackPlayer = player;
            else if (type == PlayerType.White)
                WhitePlayer = player;

            player.Type = type;

            _messenger.Send(new GameJoinMessage(type, player));
        }

        public void Receive(ClientJoinData data)
        {
            var player = _playerTracker.GetManagedPlayer(data.Player);
            _messenger.Send(new PlayerConnectedMessage(player));
        }

        public void Receive(PositionData data)
        {
            var move = data.Move;

            _Game.TryPlaceStone(move);
            _messenger.Send(new StonePlacedMessage(move));
            _messenger.Send(new TurnChangedMessage(_Game.CurrentPlayer));
        }
        public void Receive(PlayerDisconnectedInternalMessage message)
        {
            var player = _playerTracker.GetManagedPlayer(message.Player);

            if (BlackPlayer == player)
                BlackPlayer = null;
            else if (WhitePlayer == player)
                WhitePlayer = null;
        }
        public void Receive(ClientActivatedMessage message)
        {
            _client = message.Client;
        }

        public void Receive(ClientDeactivatedMessage message)
        {
            _client = null;
        }

        public async Task JoinGameAsync(PlayerType type)
        {
            if (_client != null && _client.IsAuthenticated)
                await _client.SendJoinGameAsync(type);
        }

        public async Task LeaveGameAsync()
        {
            if (_client != null && _client.IsAuthenticated)
                await _client.SendLeaveGameAsync();
        }

        public async Task PlaceStoneAsync(GameMove move)
        {
            if (_client == null || !IsGameStarted) return;

            if (_client.Me!.Type == PlayerType.Observer) return;

            if (!_client.IsAuthenticated) return;

            await _client.SendPlaceAsync(move);
        }

        public async Task SendChatAsync(string message)
        {
            if (_client != null && _client.IsAuthenticated)
                await _client.SendChatAsync(message);
        }

        public async Task StartGameAsync()
        {
            if (_client == null) return;

            if (_client.Me!.Type == PlayerType.Observer) return;
            if (!_client.IsAuthenticated) return;

            await _client.SendGameStartAsync();
        }

        public async Task<bool> CancelLastStoneAsync()
        {
            if (_client == null) return false;
            if (!_client.IsAuthenticated) return false;
            if (!CanCancelLast) throw new NotYourTurnException("무를 수 없습니다.");

            if (Me!.LeftCancelLast <= 0)
                throw new CancelNotAvailableException("무르기 횟수가 없습니다.");

            await _client.CancelLastStoneAsync(Me!.LeftCancelLast);
            return true;
        }

    }
}
