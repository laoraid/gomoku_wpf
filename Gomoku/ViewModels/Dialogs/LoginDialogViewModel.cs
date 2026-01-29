using CommunityToolkit.Mvvm.ComponentModel;
using Gomoku.Models.Common;
using Gomoku.Services.Wpf;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace Gomoku.ViewModels
{
    public partial class LoginDialogViewModel : DialogViewModelBase
    {
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(ConfirmCommand))]
        [NotifyDataErrorInfo]
        [CustomValidation(typeof(LoginDialogViewModel), nameof(ValidateId))]
        private string _username = "";

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(ConfirmCommand))]
        [NotifyDataErrorInfo]
        [CustomValidation(typeof(LoginDialogViewModel), nameof(ValidatePassword))]
        private string _password = "";

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(ConfirmCommand))]
        [NotifyDataErrorInfo]
        [CustomValidation(typeof(LoginDialogViewModel), nameof(ValidatePasswordRepeat))]
        private string _passwordRepeat = "";

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(ConfirmCommand))]
        [NotifyDataErrorInfo]
        [CustomValidation(typeof(LoginDialogViewModel), nameof(ValidateNickname))]
        private string _nickname = "";

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(ConfirmCommand))]
        private AuthType _authType = Models.Common.AuthType.Login;

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

        public static ValidationResult? ValidateId(string text, ValidationContext context)
        {
            if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(text.Trim()))
            {
                return new ValidationResult("필드를 입력해주세요.");
            }
            if (text.Contains(' '))
                return new ValidationResult("공백이 없어야 합니다.");

            string pattern = @"^[a-z0-9_]{5,12}$";
            if (!Regex.IsMatch(text, pattern))
                return new ValidationResult("5~12자 영문, 숫자만 사용해주세요.");
            return ValidationResult.Success;
        }

        public static ValidationResult? ValidatePassword(string text, ValidationContext context)
        {
            if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(text.Trim()))
            {
                return new ValidationResult("필드를 입력해주세요.");
            }
            if (text.Contains(' '))
                return new ValidationResult("공백이 없어야 합니다.");

            string pattern = @"^(?=.*[a-zA-Z])(?=.*\d)(?=.*[!@#$%^&*()_+])[A-Za-z\d!@#$%^&*()_+]{8,20}$";
            if (!Regex.IsMatch(text, pattern))
                return new ValidationResult("8~20자 영문, 숫자, 특수문자가 포함되어야 합니다.");
            return ValidationResult.Success;
        }

        public static ValidationResult? ValidatePasswordRepeat(string text, ValidationContext context)
        {
            LoginDialogViewModel? vm = context.ObjectInstance as LoginDialogViewModel;

            if (vm == null)
                return new ValidationResult("뷰모델이 아닙니다.");

            if (vm.AuthType != AuthType.CreateAccount)
                return ValidationResult.Success;

            if (text != vm.Password)
                return new ValidationResult("패스워드가 일치하지 않습니다.");

            return ValidationResult.Success;
        }

        public static ValidationResult? ValidateNickname(string text, ValidationContext context)
        {
            LoginDialogViewModel? vm = context.ObjectInstance as LoginDialogViewModel;

            if (vm == null)
                return new ValidationResult("뷰모델이 아닙니다.");

            if (vm.AuthType != AuthType.CreateAccount)
                return ValidationResult.Success;

            if (string.IsNullOrWhiteSpace(text))
                return new ValidationResult("닉네임을 입력해 주세요.");

            if (text.Contains(' '))
                return new ValidationResult("공백이 없어야 합니다.");

            return ValidationResult.Success;
        }

    }
}
