using CommunityToolkit.Mvvm.ComponentModel;

namespace Gomoku.ViewModels
{
    public partial class MessageDialogViewModel : ViewModelBase
    {
        [ObservableProperty]
        private string _title = "알림";

        [ObservableProperty]
        private string _message = "";

        [ObservableProperty]
        private bool _isConfirm;
    }
}
