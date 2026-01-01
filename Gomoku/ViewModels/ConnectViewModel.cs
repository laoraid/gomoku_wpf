using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Gomoku.ViewModels
{
    public enum DoubleThreeRule
    {
        BothAllowed, WhiteOnly, BothForbidden
    }

    public enum ConnectionType
    {
        Server, Client
    }

    public partial class ConnectViewModel : ViewModelBase
    {
        [ObservableProperty]
        private string _ipAddress = "";

        [ObservableProperty]
        private int _port = 7777;

        [ObservableProperty]
        private string _nickname = "익명";

        public bool IsConfirmed { get; private set; } = false;

        [ObservableProperty]
        private DoubleThreeRule _selectedDTRule = DoubleThreeRule.WhiteOnly;

        [ObservableProperty]
        private ConnectionType _connectionType = ConnectionType.Server;

        public event Action? RequestClose;

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
                Regex ipRegex = new Regex(@"^(((?!25?[6-9])[12]\d[1-9]?\d.?\b){4}|localhost)$");

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