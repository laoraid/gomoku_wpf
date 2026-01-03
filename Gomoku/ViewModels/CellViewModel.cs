using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Text;

namespace Gomoku.ViewModels
{
    public partial class CellViewModel : ViewModelBase
    {
        public int X { get; }
        public int Y { get; }

        [ObservableProperty]
        private int _stoneState = 0; // 0: 없음, 1: 흑, 2: 백

        [ObservableProperty]
        private bool _isForbidden;

        public CellViewModel(int x, int y)
        {
            X = x;
            Y = y;
        }
    }
}
