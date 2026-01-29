namespace Gomoku.ViewModels.Dialogs
{
    public interface IDialogViewModel
    {
        public bool IsConfirmed { get; }
        public bool CloseRequested { get; }
        event Action? RequestClose;
        void Close();
    }
}
