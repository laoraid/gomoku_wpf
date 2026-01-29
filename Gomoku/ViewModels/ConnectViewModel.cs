using CommunityToolkit.Mvvm.ComponentModel;
using Gomoku.Models.Common;
using Gomoku.Models.Domain;
using Gomoku.Models.DTO;
using Gomoku.Models.Interfaces;
using Gomoku.Services.Applications.Auth;
using Gomoku.Services.Wpf;
using Gomoku.Services.Wpf.Dialogs;
using System.ComponentModel.DataAnnotations;
using System.Net.Http;
using System.Text.RegularExpressions;

namespace Gomoku.ViewModels
{

    public partial class ConnectViewModel : DialogViewModelBase
    {
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(ConfirmCommand))]
        [NotifyDataErrorInfo]
        [CustomValidation(typeof(ConnectViewModel), nameof(ValidateIpAddress))]
        private string _ipAddress = "";

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(ConfirmCommand))]
        private int _port = 7777;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(ConfirmCommand))]
        private DoubleThreeRuleType _selectedDTRule = DoubleThreeRuleType.WhiteOnlyAllow;

        [ObservableProperty]
        private LoginType _selectedLoginType = LoginType.Guest;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(ConfirmCommand))]
        private ConnectionType _connectionType = ConnectionType.Server;

        [ObservableProperty]
        private int _cancelLastStoneCount = 3;

        partial void OnConnectionTypeChanged(ConnectionType value)
        {
            ValidateProperty(IpAddress, nameof(IpAddress));
        }

        [ObservableProperty]
        private string _serverIpAddress = "IP 주소 불러오는 중...";

        private static string _cachedServerIp = string.Empty;

        private readonly IViewModelFactory _viewModelFactory;
        private readonly IDialogService _dialogService;
        private readonly IAuthSessionService _authSessionService;
        private readonly IMessageBoxService _messageBoxService;

        public ConnectViewModel(
            IDispatcher dispatcher, IViewModelFactory viewModelFactory,
            IDialogService dialogService, IAuthSessionService authSessionService,
            IMessageBoxService messageBoxService) : base(dispatcher)
        {
            _viewModelFactory = viewModelFactory;
            _dialogService = dialogService;
            _authSessionService = authSessionService;
            _messageBoxService = messageBoxService;
            _ = _GetIpAddressAsync();
        }

        private async Task _GetIpAddressAsync()
        {
            if (!string.IsNullOrEmpty(_cachedServerIp))
            {   // 새 창 열때마다 불러오지 않게 캐싱
                ServerIpAddress = _cachedServerIp;
                return;
            }

            string ip = "IP 주소를 불러오는데 실패했습니다.";
            using (var client = new HttpClient())
            {
                try
                {
                    ip = await client.GetStringAsync("https://ident.me");
                    ip = ip.Trim();
                    _cachedServerIp = ip;
                }
                catch (HttpRequestException)
                { // IP 주소 확인 실패
                }
            }
            ServerIpAddress = ip;
        }

        protected override bool CanConfirm()
        {
            if (HasErrors) return false;
            bool isPortOk = 1024 <= Port && Port <= 65535;

            return isPortOk;
        }

        public static ValidationResult? ValidateIpAddress(string ip, ValidationContext context)
        {
            ConnectViewModel? vm = context.ObjectInstance as ConnectViewModel;

            if (vm != null && vm.ConnectionType != ConnectionType.Client)
                return ValidationResult.Success; // 서버 모드인 경우 IP 주소 그냥 패스

            if (string.IsNullOrWhiteSpace(ip))
                return new ValidationResult("IP 주소를 입력하세요.");

            if (ip.ToLower() == "localhost")
                return ValidationResult.Success;

            Regex ipRegex = new Regex(@"^(25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)(\.(25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)){3}$");

            if (ipRegex.IsMatch(ip))
                return ValidationResult.Success;

            return new ValidationResult("올바른 IP 주소가 아닙니다.");
        }

        public override async Task ConfirmAsync()
        {
            using var cts = new CancellationTokenSource();

            var option = new ConnectionOption(
                IpAddress, Port, SelectedLoginType, SelectedDTRule,
                ConnectionType, cts.Token, CancelLastStoneCount);

            var loadingVM = _viewModelFactory.Create<LoadingDialogViewModel>();
            loadingVM.Title = "서버에 연결하는 중...";

            try
            {
                var dialogTask = _dialogService.ShowAsync(loadingVM, DialogSection.Connect);
                var connectTask = _authSessionService.StartSessionAsync(option);

                var completedTask = await Task.WhenAny(connectTask, dialogTask);

                if (completedTask == dialogTask)
                {   // 로딩 다이얼로그가 닫혔으면 취소된 것
                    cts.Cancel();
                    await connectTask;
                    await loadingVM.CloseAsync();

                    await _authSessionService.StopSessionAsync();

                    await _messageBoxService.CautionAsync("경고", "서버 연결이 취소되었습니다.", DialogSection.Connect);
                    return;
                }

                bool isConnected = await connectTask;

                if (!isConnected)
                {
                    await loadingVM.CloseAsync();
                    await _authSessionService.StopSessionAsync();
                    await _messageBoxService.CautionAsync("경고", "서버에 연결하지 못했습니다.", DialogSection.Connect);
                    return;
                }

                await loadingVM.CloseAsync(); // 연결 성공, 로딩 다이얼로그 닫기

                if (SelectedLoginType == LoginType.Login)
                {
                    bool isAuthorized = await ProcessAuthenticationAsync();
                    if (!isAuthorized) // 인증 실패, 취소
                    {
                        await _authSessionService.StopSessionAsync();
                        await _messageBoxService.CautionAsync("경고", "인증에 실패했습니다.", DialogSection.Connect);
                        return;
                    }
                }
                else
                {
                    // 게스트 로그인은 인증 과정 없음
                    await _authSessionService.RequestGuestLoginAsync();
                }

                await base.ConfirmAsync();
                // 모든 과정 성공, 다이얼로그 닫기
            }
            catch (Exception ex)
            {
                await loadingVM.CloseAsync();
                await _authSessionService.StopSessionAsync();
                await _messageBoxService.ErrorAsync($"오류가 발생했습니다: {ex.Message}", DialogSection.Connect);
            }

        }

        private async Task<bool> ProcessAuthenticationAsync()
        {
            var authVM = _viewModelFactory.Create<LoginDialogViewModel>();

            while (true)
            {
                var authResultVM = await _dialogService.ShowAsync(authVM, DialogSection.Connect);
                if (authResultVM == null) return false; // 취소됨

                AuthResult result;
                if (authResultVM.AuthType == AuthType.Login) // 로그인
                {
                    result = await _authSessionService.RequestLoginAsync(
                        authResultVM.Username, authResultVM.Password);
                }
                else // 계정 생성
                {
                    result = await _authSessionService.RequestCreateAccountAsync(
                        authResultVM.Username, authResultVM.Password, authResultVM.Nickname);
                }

                if (result.IsSuccess) return true;

                await _messageBoxService.ErrorAsync(result.Reason, DialogSection.Connect);
                authVM.ResetStatus();
            }
        }

        public override async Task CancelAsync()
        {
            await _authSessionService.StopSessionAsync();
            await base.CancelAsync();
        }
    }
}