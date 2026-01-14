namespace Gomoku.Services.Interfaces
{
    public interface ISnackbarService
    {
        object MessageQueue { get; }
        void Show(string message, string? buttonContent = null, Action? actionhandler = null);
    }
}
