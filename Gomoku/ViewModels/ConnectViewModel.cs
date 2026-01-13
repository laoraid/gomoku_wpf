using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Gomoku.Helpers;
using Gomoku.Models;
using Gomoku.Services.Interfaces;
using System.Net.Http;

namespace Gomoku.ViewModels
{
    public enum ConnectionType
    {
        Server, Client
    }

    public partial class ConnectViewModel : ViewModelBase, IDialogViewModel
    {
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(ConnectCommand))]
        private string _ipAddress = "";

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(ConnectCommand))]
        private int _port = 7777;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(ConnectCommand))]
        private string _nickname = "익명";

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(ConnectCommand))]
        private DoubleThreeRuleType _selectedDTRule = DoubleThreeRuleType.WhiteOnly;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(ConnectCommand))]
        private ConnectionType _connectionType = ConnectionType.Server;

        [ObservableProperty]
        private string _serverIpAddress = "IP 주소 불러오는 중...";

        private static string _cachedServerIp = string.Empty;

        public bool IsConfirmed { get; set; }

        public event Action? RequestClose;

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


        [RelayCommand(CanExecute = nameof(CanConnect))]
        private void Connect()
        {
            IsConfirmed = true;
            RequestClose?.Invoke();
        }

        private bool CanConnect()
        {
            bool isIpOk = true;
            bool isPortOk = 1024 <= Port && Port <= 65535;
            bool isNickOk = !string.IsNullOrWhiteSpace(Nickname);
            if (ConnectionType == ConnectionType.Client)
            {
                isIpOk = IpAddressValidationRule.IsValid(IpAddress);
            }

            return isIpOk && isPortOk && isNickOk;
        }
    }
}