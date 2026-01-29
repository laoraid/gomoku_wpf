namespace Gomoku.Services.Wpf.Dialogs
{
    public interface ISnackbarService
    {
        object MessageQueue { get; }
        void Show(string message, string? buttonContent = null, Action? actionhandler = null);
    }
}
