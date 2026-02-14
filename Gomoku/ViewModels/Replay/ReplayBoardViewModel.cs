using Gomoku.Models.DTO;
using Gomoku.Services.Wpf;

namespace Gomoku.ViewModels.Replay
{
    /// <summary>
    /// 리플레이 창의 보드 뷰모델
    /// 
    /// 리플레이 시에 보드뷰모델에 필요없는 부분은 제거하고 착수 명령등의 메신저도 반영하지 않습니다.
    /// </summary>
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
