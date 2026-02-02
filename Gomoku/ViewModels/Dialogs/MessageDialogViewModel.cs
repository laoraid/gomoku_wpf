using CommunityToolkit.Mvvm.ComponentModel;
using Gomoku.Services.Wpf;

namespace Gomoku.ViewModels.Dialogs
{
    public partial class MessageDialogViewModel(IDispatcher dispatcher) : DialogViewModelBase(dispatcher)
    {
        [ObservableProperty]
        private string _title = "알림";
        [ObservableProperty]
        private string _message = "";
        [ObservableProperty]
        private bool _isConfirmMode; // 취소 버튼 노출할지 결정
    }
}
