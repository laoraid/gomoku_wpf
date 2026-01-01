using System;
using System.Collections.Generic;
using System.Text;

namespace Gomoku.ViewModels
{
    public class CellViewModel : ViewModelBase
    {
        public int X { get; }
        public int Y { get; }

        private int _stoneState = 0; // 0: 없음, 1: 흑, 2: 백
        public int StoneState
        {
            get => _stoneState;
            set => SetProperty(ref _stoneState, value);
        }

        public CellViewModel(int x, int y)
        {
            X = x;
            Y = y;
        }
    }
}
