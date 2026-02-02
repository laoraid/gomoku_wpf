using CommunityToolkit.Mvvm.Input;
using Gomoku.Models.Common;
using Gomoku.Models.Domain;
using Gomoku.Models.DTO;
using Gomoku.Services.Wpf;
using System.Collections.ObjectModel;

namespace Gomoku.ViewModels
{
    public abstract partial class BoardViewModelBase : ViewModelBase
    {
        public ObservableCollection<CellViewModel> BoardCells { get; } = new();

        private readonly List<CellViewModel> _lastForbiddenMarkedCells = new();
        // 마지막 금수 표시 셀
        private readonly Stack<CellViewModel> _lastCell = new();
        // 마지막 돌 표시 셀

        public BoardViewModelBase(IDispatcher dispatcher) : base(dispatcher)
        {
            for (int y = 0; y < GomokuManager.BOARD_SIZE; y++)
            {
                for (int x = 0; x < GomokuManager.BOARD_SIZE; x++)
                {
                    BoardCells.Add(new CellViewModel(x, y));
                }
            }
        }

        public void Place(GameMove move)
        {
            if (_lastCell.TryPeek(out var last))
                last.IsLastStone = false;

            var targetcell = GetCell(move.X, move.Y);

            targetcell.StoneState = (int)move.PlayerType;
            targetcell.IsLastStone = true;
            targetcell.StoneNumber = move.MoveNumber;

            _lastCell.Push(targetcell);
        }

        public void ClearBoard()
        {
            foreach (var cell in BoardCells)
            {
                cell.Clear();
            }
            _lastCell.Clear();
            _lastForbiddenMarkedCells.Clear();
        }

        public void MarkWinStone(IEnumerable<GameMove> stones)
        {
            foreach (var move in stones)
            {
                var cell = GetCell(move.X, move.Y);
                cell.IsWinStone = true;
            }
        }
        public CellViewModel GetCell(int x, int y)
        {
            return BoardCells[y * GomokuManager.BOARD_SIZE + x];
        }

        public void ReMarkForbidden(IEnumerable<(int x, int y)> stones)
        {
            foreach (var cell in _lastForbiddenMarkedCells)
            {
                cell.IsForbidden = false;
            }

            _lastForbiddenMarkedCells.Clear();

            foreach (var move in stones)
            {
                var cell = GetCell(move.x, move.y);
                cell.IsForbidden = true;
                _lastForbiddenMarkedCells.Add(cell);
            }
        }

        public void CancelLastStone()
        {
            if (_lastCell.TryPop(out var last))
            {
                last.Clear();
                Logger.Info("무르기 보드 반영 완료");
            }
            else
                throw new InvalidOperationException("마지막 돌이 없음");
        }

        public void SetBoard(IEnumerable<GameMove> moves)
        {
            foreach (var cell in _lastCell)
            {
                cell.Clear();
            }

            _lastCell.Clear();
            _lastForbiddenMarkedCells.Clear();

            foreach (var move in moves)
            {
                var cell = GetCell(move.X, move.Y);
                cell.StoneState = (int)move.PlayerType;
                cell.StoneNumber = move.MoveNumber;
                _lastCell.Push(cell);
            }

            if (_lastCell.TryPeek(out var last))
            {
                last.IsLastStone = true;
            }
        }

        [RelayCommand]
        protected abstract Task PlaceStone(CellViewModel? cell);
    }
}
