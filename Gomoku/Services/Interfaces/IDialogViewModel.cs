namespace Gomoku.Services.Interfaces
{
    public interface IDialogViewModel
    {
        public bool IsConfirmed { get; }
        public bool CloseRequested { get; }
        event Action? RequestClose;
        void Close();
    }
}
