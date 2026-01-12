using CommunityToolkit.Mvvm.Input;
using Gomoku.Dialogs;
using System.Diagnostics;
using System.Windows.Input;

namespace Gomoku.ViewModels
{
    public partial class InformationViewModel : ViewModelBase, IDialogViewModel
    {
        public ICommand CloseButtonCommand => new RelayCommand(() => RequestClose?.Invoke());

        public bool IsConfirmed => true;

        public event Action? RequestClose;

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
