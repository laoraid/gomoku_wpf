using Gomoku.ViewModels.Dialogs;

namespace Gomoku.Services.Wpf.Window
{
    public interface IWindowService
    {
        T? ShowDialog<T>(T viewModel) where T : class, IDialogViewModel;
    }
}
