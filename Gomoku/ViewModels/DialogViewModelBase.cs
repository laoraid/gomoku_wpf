using CommunityToolkit.Mvvm.Input;
using Gomoku.Services.Interfaces;

namespace Gomoku.ViewModels
{
    public partial class DialogViewModelBase : ViewModelBase, IDialogViewModel
    {
        public bool IsConfirmed { get; protected set; } = false;

        public event Action? RequestClose;

        [RelayCommand(CanExecute = nameof(CanConfirm))]
        public virtual void Confirm()
        {
            IsConfirmed = true;
            RequestClose?.Invoke();
        }

        [RelayCommand]
        public virtual void Cancel()
        {
            IsConfirmed = false;
            RequestClose?.Invoke();
        }

        public void Close() => RequestClose?.Invoke();

        protected virtual bool CanConfirm() => true;
    }
}
