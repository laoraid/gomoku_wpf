using CommunityToolkit.Mvvm.ComponentModel;

namespace Gomoku.ViewModels
{
    public partial class MessageDialogViewModel : DialogViewModelBase
    {
        [ObservableProperty]
        private string _title = "알림";
        [ObservableProperty]
        private string _message = "";
        [ObservableProperty]
        private bool _isConfirmMode; // 취소 버튼 노출할지 결정
    }
}
