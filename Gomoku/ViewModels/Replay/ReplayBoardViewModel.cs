using Gomoku.Models.DTO;
using Gomoku.Services.Wpf;

namespace Gomoku.ViewModels.Replay
{
    public partial class ReplayBoardViewModel : BoardViewModelBase
    {
        private List<GameMove>? _moveHistory;

        public ReplayBoardViewModel(IDispatcher dispatcher) : base(dispatcher)
        {
        }

        public void SetMoveHistory(IEnumerable<GameMove> moveHistory)
        {
            _moveHistory = moveHistory.ToList();
        }

        public void SetStep(int step)
        {
            if (_moveHistory == null)
                throw new InvalidOperationException("착수 히스토리가 등록되지 않음");

            var targetMoves = _moveHistory.Slice(0, step);

            SetBoard(targetMoves);
        }

        protected override Task PlaceStone(CellViewModel? cell)
        {
            return Task.CompletedTask;
        }
    }
}
