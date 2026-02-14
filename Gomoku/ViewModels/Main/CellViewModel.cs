using CommunityToolkit.Mvvm.ComponentModel;

namespace Gomoku.ViewModels
{
    /// <summary>
    /// 오목알 뷰모델입니다.
    /// 
    /// 오목알의 스타일 GomokuCellStyle.xaml 에서 참조할 바인딩 속성들을 정의합니다.
    /// </summary>
    public partial class CellViewModel : ObservableObject
    {
        public int X { get; }
        public int Y { get; }

        [ObservableProperty]
        private int _stoneState = 0; // 0: 없음, 1: 흑, 2: 백

        [ObservableProperty]
        private bool _isForbidden;

        [ObservableProperty]
        private bool _isLastStone = false;

        [ObservableProperty]
        private bool _isWinStone = false;

        [ObservableProperty]
        private int _stoneNumber = 0;

        public CellViewModel(int x, int y)
        {
            X = x;
            Y = y;
        }

        public void Clear()
        {
            StoneState = 0;
            IsForbidden = false;
            IsLastStone = false;
            StoneNumber = 0;
            IsWinStone = false;
        }
    }
}
