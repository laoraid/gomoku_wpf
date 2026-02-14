/*
 * DialogViewModelBase.cs
 * 모든 다이얼로그 윈도우의 뷰모델은 이 클래스를 상속합니다.
 * 
 * 확인 버튼, 취소 버튼의 동작이 미리 정의되어 있습니다.
 * 확인 버튼 클릭을 제한하려면 CanConfirm을 오버라이드 하여 사용합니다.
 * 뷰에선 확인 버튼 커맨드에 ConfirmCommand, 취소 버튼에는 CancelCommand 로 사용합니다.
 */
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
