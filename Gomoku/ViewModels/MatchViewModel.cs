using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Gomoku.Models.DTO;
using Gomoku.Services.Wpf;
using System.Collections.ObjectModel;

namespace Gomoku.ViewModels
{
    public partial class MatchViewModel : DialogViewModelBase
    {
        [ObservableProperty]
        private string _playerNickname = string.Empty;

        [ObservableProperty]
        private string _blackPlayerNickname = string.Empty;

        [ObservableProperty]
        private string _whitePlayerNickname = string.Empty;

        [ObservableProperty]
        private DateTime? _startDate = null;

        [ObservableProperty]
        private DateTime? _endDate = null;

        [ObservableProperty]
        private bool _isLoading = false;

        public ObservableCollection<MatchInfo> SearchedMatches { get; } = new();

        public MatchViewModel(IDispatcher dispatcher) : base(dispatcher)
        {
        }

        [RelayCommand]
        private void Search()
        {

        }
    }
}
