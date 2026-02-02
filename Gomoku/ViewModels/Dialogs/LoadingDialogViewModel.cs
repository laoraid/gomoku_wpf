using CommunityToolkit.Mvvm.ComponentModel;
using Gomoku.Services.Wpf;

namespace Gomoku.ViewModels.Dialogs
{
    public partial class LoadingDialogViewModel(IDispatcher dispatcher) : DialogViewModelBase(dispatcher)
    {
        [ObservableProperty]
        private string _title = "로딩 중...";

        [ObservableProperty]
        private string _message = "";
    }
}
