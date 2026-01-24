using CommunityToolkit.Mvvm.ComponentModel;
using Gomoku.Services.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace Gomoku.ViewModels
{
    public partial class LoginDialogViewModel : DialogViewModelBase
    {
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(ConfirmCommand))]
        [NotifyDataErrorInfo]
        [CustomValidation(typeof(LoginDialogViewModel), nameof(ValidateText))]
        private string _username = "";

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(ConfirmCommand))]
        [NotifyDataErrorInfo]
        [CustomValidation(typeof(LoginDialogViewModel), nameof(ValidateText))]
        private string _password = "";

        public LoginDialogViewModel(IDispatcher dispatcher) : base(dispatcher)
        {
            ValidateAllProperties();
            // 최초에 공백일때 유효성 검사
        }

        protected override bool CanConfirm()
        {
            if (HasErrors) return false;
            return true;
        }

        public static ValidationResult? ValidateText(string text, ValidationContext context)
        {
            if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(text.Trim()))
            {
                return new ValidationResult("필드를 입력해주세요.");
            }
            if (text.Contains(' '))
                return new ValidationResult("공백이 없어야 합니다.");

            return ValidationResult.Success;
        }
    }
}
