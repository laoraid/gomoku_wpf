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

        private readonly List<CellViewModel> _lastForbiddenMarkedCells = new();
        // 마지막 금수 표시 셀
        private readonly Stack<CellViewModel> _lastCell = new();
        // 마지막 돌 표시 셀

        public bool CanShowStartButton =>
            Me?.Type == PlayerType.Black &&
            !_gameSession.IsGameStarted &&
            _gameSession.WhitePlayer != null;

        public bool IsMyTurn => _gameSession.IsMyTurn;
        public bool IsOpponentTurn => _gameSession.IsOpponentTurn;

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
            _gameSession.LastStoneCanceled += LastStoneCanceled;

            _gameSession.PlayerGameJoined += (_, _) => HandlePlayerChanged();
            _gameSession.PlayerGameLeft += (_, _) => HandlePlayerChanged();
            _gameSession.GameEnded += (_) => HandlePlayerChanged();
            _gameSession.GameStarted += () => HandlePlayerChanged();
        }

        private void LastStoneCanceled(PlayerType arg1, int arg2)
        {
            Logger.Info("무르기 보드 반영");
            if (_lastCell.TryPop(out var last))
            {
                last.IsLastStone = false;
                last.StoneState = 0;
                return;
            }
            throw new InvalidOperationException("마지막 돌이 없음");
        }

        private void HandlePlayerChanged()
        {
            _dispatcher.Invoke(() =>
            {
                Me?.UpdateFromModel();
                OnPropertyChanged(nameof(CanShowStartButton));
                OnPropertyChanged(nameof(IsMyTurn));
                OnPropertyChanged(nameof(IsOpponentTurn));
            });
        }
        private CellViewModel GetCell(int x, int y)
        {
            return BoardCells[y * GomokuManager.BOARD_SIZE + x];
        }
        private void HandleGameSynced(GameSync sync)
        {
            HandleGameReset(); // 보드 초기화
            foreach (var move in sync.MoveHistory)
            {
                GetCell(move.X, move.Y).StoneState = (int)move.PlayerType;
            }

            var lastmove = _gameSession.LastStone;

            if (lastmove == null) return;

            var last = GetCell(lastmove.X, lastmove.Y);
            _lastCell.Push(last);
            last.IsLastStone = true;
        }
        private void HandleTurnChanged(PlayerType obj)
        {
            _dispatcher.Invoke(() =>
            {
                Me?.UpdateFromModel();
                OnPropertyChanged(nameof(IsMyTurn));
                OnPropertyChanged(nameof(IsOpponentTurn));
                UpdateForbiddenMarks(obj);
            });
        }
        private void HandleGameEnded(GameEnd data)
        {
            if (data.Stones != null)
            {   // 승리 시에 승리한 돌에 표시하기
                foreach (var move in data.Stones)
                {
                    var cell = GetCell(move.X, move.Y);
                    cell.IsWinStone = true;
                }
            }
        }
        private void HandleGameReset()
        {   // 게임 리셋시 모든 셀 초기화
            foreach (var cell in BoardCells)
            {
                cell.IsLastStone = false;
                cell.IsWinStone = false;
                cell.StoneState = 0;
                cell.IsForbidden = false;
            }
            _lastCell.Clear();
            _lastForbiddenMarkedCells.Clear();
        }

        private void HandleStonePlaced(GameMove data)
        {
            if (_lastCell.TryPeek(out var last))
                last.IsLastStone = false;

            var targetcell = GetCell(data.X, data.Y);
            _lastCell.Push(targetcell);

            targetcell.StoneState = (int)data.PlayerType;
            targetcell.IsLastStone = true;
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

                foreach (var cell in _lastForbiddenMarkedCells)
                    cell.IsForbidden = false;

                _lastForbiddenMarkedCells.Clear();

                foreach (var pos in forbiddenpos) // 보드 셀 순회하며
                {
                    var cell = GetCell(pos.x, pos.y);
                    _lastForbiddenMarkedCells.Add(cell);
                    cell.IsForbidden = true;
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
