using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Gomoku.Models;
using Gomoku.Models.DTO;
using Gomoku.Services.Interfaces;
using System.Collections.ObjectModel;

namespace Gomoku.ViewModels
{
    public partial class BoardViewModel : ViewModelBase
    {
        private readonly IGameSessionService _gameSession;
        private readonly IDispatcher _dispatcher;
        private readonly IMessageBoxService _MessageBoxService;
        private readonly ISoundService _soundService;

        public ObservableCollection<CellViewModel> BoardCells { get; } = new();

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CanShowStartButton))]
        public PlayerViewModel? _me;

        public bool CanShowStartButton =>
            Me?.Type == PlayerType.Black &&
            !_gameSession.IsGameStarted &&
            _gameSession.WhitePlayer != null;

        public bool IsMyTurn => _gameSession.IsMyTurn;

        public BoardViewModel(IGameSessionService gameSession, IDispatcher dispatcher,
            IMessageBoxService messageBoxService, ISoundService soundService)
        {
            _gameSession = gameSession;
            _dispatcher = dispatcher;
            _MessageBoxService = messageBoxService;
            _soundService = soundService;

            for (int y = 0; y < GomokuManager.BOARD_SIZE; y++)
            {
                for (int x = 0; x < GomokuManager.BOARD_SIZE; x++)
                {
                    BoardCells.Add(new CellViewModel(x, y));
                }
            }

            _gameSession.StonePlaced += HandleStonePlaced;
            _gameSession.GameReset += HandleGameReset;
            _gameSession.GameEnded += HandleGameEnded;
            _gameSession.TurnChanged += HandleTurnChanged;
            _gameSession.GameSynced += HandleGameSynced;

            _gameSession.PlayerGameJoined += (_, _) => HandlePlayerChanged();
            _gameSession.PlayerGameLeft += (_, _) => HandlePlayerChanged();
            _gameSession.GameEnded += (_) => HandlePlayerChanged();
            _gameSession.GameStarted += () => HandlePlayerChanged();
        }

        private void HandlePlayerChanged()
        {
            _dispatcher.Invoke(() =>
            {
                OnPropertyChanged(nameof(CanShowStartButton));
                OnPropertyChanged(nameof(IsMyTurn));
            });
        }

        private void HandleGameSynced(GameSync sync)
        {
            int index;
            foreach (var move in sync.MoveHistory)
            {
                index = move.Y * 15 + move.X;
                BoardCells[index].StoneState = (int)move.PlayerType;
            }

            var lastmove = _gameSession.LastStone;

            if (lastmove == null) return;

            index = lastmove.Y * 15 + lastmove.X;
            BoardCells[index].IsLastStone = true;
        }

        private void HandleTurnChanged(PlayerType obj)
        {
            OnPropertyChanged(nameof(IsMyTurn));
            UpdateForbiddenMarks(obj);

        }

        private void HandleGameEnded(GameEnd data)
        {
            if (data.Stones != null)
            {   // 승리 시에 승리한 돌에 표시하기
                foreach (var move in data.Stones)
                {
                    int x = move.X, y = move.Y;
                    var cell = BoardCells.First(c => c.X == x && c.Y == y);
                    cell.IsWinStone = true;
                }
            }

        }
        private void HandleGameReset()
        {
            foreach (var cell in BoardCells)
            {
                cell.IsLastStone = false;
                cell.IsWinStone = false;
                cell.StoneState = 0;
                cell.IsForbidden = false;
            }
        }

        private void HandleStonePlaced(GameMove data)
        {
            foreach (var cell in BoardCells)
            {
                cell.IsLastStone = false;
            }

            int index = data.Y * 15 + data.X; // 2차원 격자 주소를 1차원 ItemsControl 주소로 바꾸기
            BoardCells[index].StoneState = (int)data.PlayerType;
            BoardCells[index].IsLastStone = true;
            _soundService.Play(SoundType.StonePlace);
        }


        private void UpdateForbiddenMarks(PlayerType obj)
        {
            if (!_gameSession.IsGameStarted) return;
            // 금수 시에 X자 업데이트
            _dispatcher.Invoke(() =>
            {
                if (Me!.Type == PlayerType.Observer) return;

                var forbiddenpos = _gameSession.GetAllForbiddenPositions(obj);
                foreach (var cell in BoardCells) // 보드 셀 순회하며
                {
                    cell.IsForbidden = _gameSession.IsMyTurn && forbiddenpos.Any(p => p.x == cell.X && p.y == cell.Y);
                }
            });
        }

        [RelayCommand]
        private async Task PlaceStone(CellViewModel? cell)
        { // 보드 클릭 시
            if (cell == null)
                return;

            if (!_gameSession.IsGameStarted)
                return;

            if (cell.StoneState != 0) return; // 이미 놓은 곳 (클라이언트 체크)

            if (Me?.Type != _gameSession.CurrentTurn) return; // 사용자 턴 아님

            var move = new GameMove(cell.X, cell.Y, 0, Me.Type);

            await _gameSession.PlaceStoneAsync(move);
        }

        [RelayCommand]
        private async Task StartGame() // 게임 시작 버튼 클릭
        {
            await _gameSession.StartGameAsync();
        }
    }
}
