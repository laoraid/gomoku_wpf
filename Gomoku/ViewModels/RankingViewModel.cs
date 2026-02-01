using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Gomoku.Models.DTO;
using Gomoku.Services.Applications.Request;
using Gomoku.Services.Wpf;
using System.Collections.ObjectModel;

namespace Gomoku.ViewModels
{
    public partial class RankingViewModel : DialogViewModelBase
    {
        public ObservableCollection<RankInfo> Rankings { get; } = new();
        private readonly IServerRequestService _serverRequestService;

        [ObservableProperty]
        private bool _isLoading = true;

        public RankingViewModel(IDispatcher dispatcher, IServerRequestService serverRequestService) : base(dispatcher)
        {
            _dispatcher = dispatcher;
            _serverRequestService = serverRequestService;
        }

        public async Task LoadRankingsAsync()
        {
            await _dispatcher.InvokeAsync(() =>
            {
                IsLoading = true;
                Rankings.Clear();
            });

            var ranks = await _serverRequestService.RequestRankingsAsync();

            await _dispatcher.InvokeAsync(() =>
            {
                foreach (var rank in ranks)
                {
                    Rankings.Add(rank);
                }
                IsLoading = false;
            });
        }

        [RelayCommand]
        private async Task Refresh()
        {
            await LoadRankingsAsync();
        }
    }
}