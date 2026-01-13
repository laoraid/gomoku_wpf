using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Gomoku.Models;
using Gomoku.Services.Interfaces;
using System.Net.Http;
using System.Text.RegularExpressions;

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
            if (ConnectionType == ConnectionType.Client)
            {
                Regex ipRegex = new Regex(@"^(25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)(\.(25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)){3}$");

                if (!ipRegex.IsMatch(IpAddress))
                    return false;
            }

            if (Port < 1024 || Port > 65535)
                return false;

            if (string.IsNullOrWhiteSpace(Nickname))
                return false;

            return true;
        }
    }
}