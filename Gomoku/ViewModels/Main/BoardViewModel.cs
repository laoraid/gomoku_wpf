using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Gomoku.Models.Common;
using Gomoku.Models.Domain;
using Gomoku.Models.DTO;
using Gomoku.Models.Messages;
using Gomoku.Services.Applications.Game;
using Gomoku.Services.Wpf;
using Gomoku.Services.Wpf.Dialogs;
using Gomoku.Services.Wpf.Media;
using System.Collections.ObjectModel;

namespace Gomoku.ViewModels
{
    public partial class BoardViewModel : ViewModelBase,
        IRecipient<StonePlacedMessage>,
        IRecipient<GameResetMessage>,
        IRecipient<GameEndMessage>,
        IRecipient<TurnChangedMessage>,
        IRecipient<GameSyncMessage>,
        IRecipient<LastStoneCanceledMessage>
    {
        private readonly IGameSessionService _gameSession;
        private readonly IMessageBoxService _MessageBoxService;
        private readonly ISoundService _soundService;

        public ObservableCollection<CellViewModel> BoardCells { get; } = new();

        private readonly List<CellViewModel> _lastForbiddenMarkedCells = new();
        // 마지막 금수 표시 셀
        private readonly Stack<CellViewModel> _lastCell = new();
        // 마지막 돌 표시 셀

        public SessionViewModel Session { get; }

        public BoardViewModel(IGameSessionService gameSession, IDispatcher dispatcher,
            IMessageBoxService messageBoxService, ISoundService soundService, IMessenger messenger,
            SessionViewModel sessionViewModel) : base(dispatcher)
        {
            _gameSession = gameSession;
            _MessageBoxService = messageBoxService;
            _soundService = soundService;

            Session = sessionViewModel;

            messenger.RegisterAll(this);

            for (int y = 0; y < GomokuManager.BOARD_SIZE; y++)
            {
                for (int x = 0; x < GomokuManager.BOARD_SIZE; x++)
                {
                    BoardCells.Add(new CellViewModel(x, y));
                }
            }
        }

        private CellViewModel GetCell(int x, int y)
        {
            return BoardCells[y * GomokuManager.BOARD_SIZE + x];
        }

        private void UpdateForbiddenMarks(PlayerType obj)
        {
            if (!_gameSession.IsGameStarted) return;
            // 금수 시에 X자 업데이트
            if (Session.Me!.Type == PlayerType.Observer) return;

            var forbiddenpos = _gameSession.GetAllForbiddenPositions(obj);

            foreach (var cell in _lastForbiddenMarkedCells)
                cell.IsForbidden = false;

            _lastForbiddenMarkedCells.Clear();

            foreach (var pos in forbiddenpos) // 보드 셀 순회하며
            {
                var cell = GetCell(pos.x, pos.y);
                _lastForbiddenMarkedCells.Add(cell);
                cell.IsForbidden = true;
            }
        }

        public void Receive(StonePlacedMessage msg) => ReceiveInvoke(HandleStonePlaced, msg);
        public void Receive(GameResetMessage msg) => ReceiveInvoke(HandleGameReset);
        public void Receive(GameEndMessage msg) => ReceiveInvoke(HandleGameEnded, msg);
        public void Receive(TurnChangedMessage msg) => ReceiveInvoke(HandleTurnChanged, msg);
        public void Receive(GameSyncMessage msg) => ReceiveInvoke(HandleGameSynced, msg);
        public void Receive(LastStoneCanceledMessage msg) => ReceiveInvoke(HandleLastStoneCanceled, msg);

        private void HandleLastStoneCanceled(LastStoneCanceledMessage msg)
        {
            if (_lastCell.TryPop(out var last))
            {
                last.IsLastStone = false;
                last.StoneState = 0;
                last.StoneNumber = 0;
                Logger.Info("무르기 보드 반영 완료");
                return;
            }
            throw new InvalidOperationException("마지막 돌이 없음");
        }

        private void HandleGameSynced(GameSyncMessage sync)
        {
            HandleGameReset(); // 보드 초기화
            var moves = sync.MoveHistory.OrderBy(x => x.MoveNumber).ToList();
            foreach (var move in moves)
            {
                var cell = GetCell(move.X, move.Y);
                _lastCell.Push(cell);
                cell.StoneState = (int)move.PlayerType;
                cell.StoneNumber = move.MoveNumber;
            }

            var lastmove = _gameSession.LastStone;

            if (lastmove == null) return;

            var last = GetCell(lastmove.X, lastmove.Y);
            last.IsLastStone = true;
            Logger.Info("보드 동기화 완료");
        }
        private void HandleTurnChanged(TurnChangedMessage msg)
        {
            UpdateForbiddenMarks(msg.Type);
        }
        private void HandleGameEnded(GameEndMessage data)
        {
            if (data.Stones != null)
            {   // 승리 시에 승리한 돌에 표시하기
                foreach (var move in data.Stones)
                {
                    var cell = GetCell(move.X, move.Y);
                    cell.IsWinStone = true;
                }
            }
            Logger.Info("게임 종료 수신. 보드 동기화 완료");
        }
        private void HandleGameReset()
        {   // 게임 리셋시 모든 셀 초기화
            foreach (var cell in BoardCells)
            {
                cell.IsLastStone = false;
                cell.IsWinStone = false;
                cell.StoneState = 0;
                cell.IsForbidden = false;
                cell.StoneNumber = 0;
            }
            _lastCell.Clear();
            _lastForbiddenMarkedCells.Clear();
            Logger.Info("게임 리셋 수신. 보드 초기화 완료");
        }

        private void HandleStonePlaced(StonePlacedMessage msg)
        {
            var move = msg.Move;

            if (_lastCell.TryPeek(out var last))
                last.IsLastStone = false;

            var targetcell = GetCell(move.X, move.Y);
            _lastCell.Push(targetcell);

            targetcell.StoneState = (int)move.PlayerType;
            targetcell.IsLastStone = true;
            targetcell.StoneNumber = msg.Move.MoveNumber;

            _soundService.Play(SoundType.StonePlace);
            Logger.Info($"착수 수신. 보드 반영 완료 {move.X} {move.Y}");
        }

        [RelayCommand]
        private async Task PlaceStone(CellViewModel? cell)
        { // 보드 클릭 시
            if (cell == null)
                return;

            if (!_gameSession.IsGameStarted)
                return;

            if (cell.StoneState != 0) return; // 이미 놓은 곳 (클라이언트 체크)

            if (Session.Me?.Type != _gameSession.CurrentTurn) return; // 사용자 턴 아님

            var move = new GameMove(cell.X, cell.Y, 0, Session.Me.Type);

            await _gameSession.PlaceStoneAsync(move);
        }
    }
}
