using CommunityToolkit.Mvvm.ComponentModel;
using Gomoku.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;

namespace Gomoku.ViewModels
{
    public partial class LoadingDialogViewModel : DialogViewModelBase
    {
        [ObservableProperty]
        private string _title = "로딩 중...";

        [ObservableProperty]
        private string _message = "";

    }
}
