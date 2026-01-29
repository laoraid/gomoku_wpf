using System.Windows;

namespace Gomoku.Services.Wpf.Dialogs
{
    public abstract class WpfServiceBase
    {
        protected static System.Windows.Window ActiveWindow =>
            Application.Current.Windows.OfType<System.Windows.Window>().FirstOrDefault(x => x.IsActive) ?? Application.Current.MainWindow;

    }
    public interface IMessageBoxService
    {
        Task<bool> ConfirmAsync(string title, string message, DialogSection section = DialogSection.Main);
        Task AlertAsync(string message, DialogSection section = DialogSection.Main);

        Task<bool> CautionAsync(string title, string message, DialogSection section = DialogSection.Main);
        Task ErrorAsync(string message, DialogSection section = DialogSection.Main);
    }
}
