using System.Windows;

namespace Gomoku.Services.Interfaces
{
    public abstract class WpfServiceBase
    {
        protected static Window ActiveWindow =>
            Application.Current.Windows.OfType<Window>().FirstOrDefault(x => x.IsActive) ?? Application.Current.MainWindow;

    }
    public interface IDialogService
    {
        bool Confirm(string title, string message);
        void Alert(string message);

        bool Caution(string title, string message);
        void Error(string message);
    }

    public interface IDialogViewModel
    {
        public bool IsConfirmed { get; }
        event Action? RequestClose;
    }

    public interface IWindowService
    {
        T? ShowDialog<T>(T viewModel) where T : class, IDialogViewModel;
    }
}
