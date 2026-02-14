using CommunityToolkit.Mvvm.ComponentModel;
using Gomoku.Services.Wpf;

namespace Gomoku.ViewModels.Dialogs
{
    /// <summary>
    /// 로딩중임을 띄우는 다이얼로그의 뷰모델
    /// </summary>
    /// <param name="dispatcher"></param>
    public partial class LoadingDialogViewModel(IDispatcher dispatcher) : DialogViewModelBase(dispatcher)
    {
        [ObservableProperty]
        private string _title = "로딩 중...";

        [ObservableProperty]
        private string _message = "";
    }
}
