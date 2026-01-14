using CommunityToolkit.Mvvm.ComponentModel;

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
