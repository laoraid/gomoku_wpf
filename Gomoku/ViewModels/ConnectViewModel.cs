using CommunityToolkit.Mvvm.ComponentModel;
using Gomoku.Models;
using Gomoku.Models.DTO;
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
        [NotifyDataErrorInfo]
        [Required(ErrorMessage = "닉네임은 공백이 아니어야 합니다.")]
        private string _nickname = "익명";

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(ConfirmCommand))]
        private DoubleThreeRuleType _selectedDTRule = DoubleThreeRuleType.WhiteOnlyAllow;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(ConfirmCommand))]
        private ConnectionType _connectionType = ConnectionType.Server;

        partial void OnConnectionTypeChanged(ConnectionType value)
        {
            ValidateProperty(IpAddress, nameof(IpAddress));
        }

        [ObservableProperty]
        private string _serverIpAddress = "IP 주소 불러오는 중...";

        private static string _cachedServerIp = string.Empty;

        public ConnectViewModel()
        {
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
    }
}