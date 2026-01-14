using CommunityToolkit.Mvvm.Input;
using System.Diagnostics;

namespace Gomoku.ViewModels
{
    public partial class InformationViewModel : DialogViewModelBase
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
