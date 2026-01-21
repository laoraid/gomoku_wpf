using CommunityToolkit.Mvvm.Input;
using Gomoku.Services.Interfaces;
using System.Diagnostics;

namespace Gomoku.ViewModels
{
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
