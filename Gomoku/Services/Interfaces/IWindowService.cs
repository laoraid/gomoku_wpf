namespace Gomoku.Services.Interfaces
{
    public interface IWindowService
    {
        T? ShowDialog<T>(T viewModel) where T : class, IDialogViewModel;
    }
}
