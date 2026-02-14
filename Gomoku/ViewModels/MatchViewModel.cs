using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Gomoku.Models.DTO;
using Gomoku.Models.Interfaces;
using Gomoku.Services.Applications.Request;
using Gomoku.Services.Wpf;
using Gomoku.Services.Wpf.Window;
using Gomoku.ViewModels.Dialogs;
using Gomoku.ViewModels.Replay;
using System.Collections.ObjectModel;

namespace Gomoku.ViewModels
{
    /// <summary>
    /// 매치 검색 창의 뷰모델
    /// </summary>
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

        private readonly IServerRequestService _requestService;
        private readonly IWindowService _windowService;
        private readonly IViewModelFactory _viewModelFactory;

        public MatchViewModel(IDispatcher dispatcher,
            IServerRequestService requestService,
            IWindowService windowService,
            IViewModelFactory viewModelFactory) : base(dispatcher)
        {
            _requestService = requestService;
            _windowService = windowService;
            _viewModelFactory = viewModelFactory;
        }

        [RelayCommand]
        private async Task Search()
        {
            _dispatcher.Invoke(SearchedMatches.Clear);
            var matches = await SearchMatches(1);

            foreach (var match in matches)
            {
                _dispatcher.Invoke(() => SearchedMatches.Add(match));
            }
        }

        private async Task<IEnumerable<MatchInfo>> SearchMatches(int Page)
        {
            _dispatcher.Invoke(() => IsLoading = true);
            var playerNickname = PlayerNickname == string.Empty ? null : PlayerNickname;
            var blackPNickname = BlackPlayerNickname == string.Empty ? null : BlackPlayerNickname;
            var whitePNickname = WhitePlayerNickname == string.Empty ? null : WhitePlayerNickname;

            var matches = await _requestService.RequestSearchMatchesAsync(
                playerNickname, blackPNickname, whitePNickname, StartDate, EndDate, Page
                );

            if (matches == null)
                return Enumerable.Empty<MatchInfo>();

            _dispatcher.Invoke(() => IsLoading = false);

            return matches;
        }

        [RelayCommand]
        private void OpenReplayWindow(MatchInfo match)
        {
            _windowService.ShowDialog(_viewModelFactory.Create<ReplayViewModel>(match));
        }

    }
}
