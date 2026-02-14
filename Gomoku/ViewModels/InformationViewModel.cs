using CommunityToolkit.Mvvm.Input;
using Gomoku.Services.Wpf;
using Gomoku.ViewModels.Dialogs;
using System.Diagnostics;

namespace Gomoku.ViewModels
{
    /// <summary>
    /// 오픈 소스 정보 창의 뷰모델
    /// </summary>
    /// <param name="dispatcher"></param>
    public partial class InformationViewModel(IDispatcher dispatcher) : DialogViewModelBase(dispatcher)
    {
        [RelayCommand]
        private void OpenUrl(string url)
        {
            if (string.IsNullOrEmpty(url)) return;

            try
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch { }
        }
    }
}
