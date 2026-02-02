using CommunityToolkit.Mvvm.Input;
using Gomoku.Services.Wpf;

namespace Gomoku.ViewModels.Dialogs
{
    public partial class DialogViewModelBase(IDispatcher dispatcher) : ViewModelBase(dispatcher), IDialogViewModel
    {
        public bool IsConfirmed { get; protected set; } = false;
        public bool CloseRequested { get; private set; } = false;

        public event Action? RequestClose;

        [RelayCommand(CanExecute = nameof(CanConfirm))]
        public virtual async Task ConfirmAsync()
        {
            IsConfirmed = true;
            RequestClose?.Invoke();
        }

        [RelayCommand]
        public virtual async Task CancelAsync()
        {
            IsConfirmed = false;
            RequestClose?.Invoke();
        }

        public virtual async Task CloseAsync()
        {
            CloseRequested = true;
            RequestClose?.Invoke();
        }

        public void ResetStatus()
        {
            IsConfirmed = false;
            CloseRequested = false;
        }

        protected virtual bool CanConfirm() => true;
    }
}
