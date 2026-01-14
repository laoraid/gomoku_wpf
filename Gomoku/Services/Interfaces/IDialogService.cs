using Gomoku.Services.Wpf;

namespace Gomoku.Services.Interfaces
{
    public interface IDialogService
    {
        Task<T?> ShowAsync<T>(T vm, DialogSection section = DialogSection.Main)
            where T : class, IDialogViewModel;
    }
}
