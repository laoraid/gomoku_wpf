using Gomoku.ViewModels.Dialogs;

namespace Gomoku.Services.Wpf.Dialogs
{
    public interface IDialogService
    {
        Task<T?> ShowAsync<T>(T vm, DialogSection section = DialogSection.Main)
            where T : class, IDialogViewModel;
    }
}
