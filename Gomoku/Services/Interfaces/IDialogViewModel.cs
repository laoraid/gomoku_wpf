namespace Gomoku.Services.Interfaces
{
    public interface IDialogViewModel
    {
        public bool IsConfirmed { get; }
        event Action? RequestClose;
        void Close();
    }
}
