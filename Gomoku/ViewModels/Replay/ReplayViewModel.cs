using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Gomoku.Models.DTO;
using Gomoku.Models.Interfaces;
using Gomoku.Services.Applications.Request;
using Gomoku.Services.Wpf;
using Gomoku.ViewModels.Dialogs;

namespace Gomoku.ViewModels.Replay
{
    /// <summary>
    /// 리플레이 창의 뷰모델
    /// </summary>
    public partial class ReplayViewModel : DialogViewModelBase
    {
        [ObservableProperty]
        private double _sliderMaximum = 0.0;

        [ObservableProperty]
        private double _sliderValue = 0.0;

        private MatchInfo _match;
        private readonly IServerRequestService _serverRequestService;
        private List<GameMove> _moves = new();

        [ObservableProperty]
        private ReplayBoardViewModel _board;

        public ReplayViewModel(IDispatcher dispatcher, IServerRequestService serverRequestService,
            IViewModelFactory viewModelFactory, MatchInfo match) : base(dispatcher)
        {
            _serverRequestService = serverRequestService;
            _match = match;
            Board = viewModelFactory.Create<ReplayBoardViewModel>();
        }

        public async Task LoadMatchMovesAsync()
        {
            _moves = (await _serverRequestService.RequestMatchMovesAsync(_match)).ToList();

            _dispatcher.Invoke(() =>
            {
                Board.SetMoveHistory(_moves);
                SliderMaximum = _moves.Count;
            });
        }

        partial void OnSliderValueChanged(double value)
        {
            Board.SetStep((int)SliderValue);
        }

        [RelayCommand]
        private void StoneNext()
        {
            if (SliderValue < SliderMaximum)
            {
                SliderValue++;
            }
        }

        [RelayCommand]
        private void SkipToStart()
        {
            SliderValue = 0;
        }

        [RelayCommand]
        private void SkipToEnd()
        {
            SliderValue = SliderMaximum;
        }
    }
}
