using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using Gomoku.Models;
using Gomoku.Models.DTO;
using Gomoku.Services.Interfaces;
using System.Collections.ObjectModel;
using System.Net.Sockets;

namespace Gomoku.ViewModels
{
    public partial class MainViewModel : ViewModelBase
    {
        private readonly IMessageBoxService _messageBoxService;
        private readonly IWindowService _windowService;
        private readonly ISoundService _soundService;
        private readonly IDialogService _dialogService;
        private readonly ISnackbarService _snackbarService;
        private readonly IDispatcher _dispatcher;

        public object MainSnackBarQueue => _snackbarService.MessageQueue;

        private IGameClient? _client; // 서버도 하나의 클라이언트로 자기 자신에게 접속
        private IGameServer _server; // 서버일 경우만 생성

        private GomokuManager _localgame = new GomokuManager(); // 클라이언트 전용

        #region 바인딩 속성들

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CanShowStartButton))]
        [NotifyPropertyChangedFor(nameof(IsMyTurn))]
        [NotifyPropertyChangedFor(nameof(IsMeBlack))]
        [NotifyPropertyChangedFor(nameof(IsMeWhite))]
        [NotifyPropertyChangedFor(nameof(CanJoin))] // MyPlayerType 바뀔때 아래것들도 새로고침됨
        private PlayerViewModel? _me;

        private PlayerType CurrentTurn => _localgame.CurrentPlayer;

        private bool IsGameStarted => _localgame.IsGameStarted;

        public bool IsMeBlack => Me?.Type == PlayerType.Black;
        public bool IsMeWhite => Me?.Type == PlayerType.White;
        public bool CanJoin => Me?.Type == PlayerType.Observer;
        public bool IsMyTurn => _client is SoloGameClient
            ? IsGameStarted : (IsGameStarted && Me?.Type == CurrentTurn);

        public bool CanShowStartButton =>
            IsMeBlack &&
            !IsGameStarted &&
            WhitePlayer != null;

        public ObservableCollection<CellViewModel> BoardCells { get; } = new ObservableCollection<CellViewModel>();
        // 격자 버튼 (15x15) 누르면 돌 착수
        public ObservableCollection<PlayerViewModel> UserList { get; } = new ObservableCollection<PlayerViewModel>();
        // 참가자 리스트
        public ObservableCollection<string> ChatMessages { get; } = new ObservableCollection<string>();
        // 채팅

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CanShowStartButton))]
        private PlayerViewModel? _blackPlayer;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CanShowStartButton))]
        private PlayerViewModel? _whitePlayer;

        [ObservableProperty]
        private string _chatInput = string.Empty;

        #endregion

        public MainViewModel(IMessageBoxService messageBoxService, IWindowService windowService,
            ISoundService soundService, IDialogService dialogService, ISnackbarService snackbarService,
            IGameServer server, IDispatcher dispatcher)
        {
            _messageBoxService = messageBoxService;
            _windowService = windowService;
            _soundService = soundService;
            _dialogService = dialogService;
            _snackbarService = snackbarService;
            _dispatcher = dispatcher;

            _server = server;

            _localgame.OnStonePlaced += StonePlaced; // 돌 놓았을때 UI 반영
            _localgame.OnTurnChanged += UpdateForbiddenMarks;
            _localgame.OnGameEnded += (windata) =>
            {
                NotifyGameStates();
            };
            _localgame.OnGameReset += () =>
            {
                ResetStoneUI();
                NotifyGameStates();
            };
            _localgame.OnGameStarted += () =>
            {
                NotifyGameStates();
            };
            _localgame.OnTurnChanged += (player) =>
            {
                _dispatcher.Invoke(() =>
                {
                    OnPropertyChanged(nameof(CurrentTurn));
                    OnPropertyChanged(nameof(IsMyTurn));
                });
            };
            _localgame.OnGameSync += () =>
            {
                SyncStone();
            };

            for (int y = 0; y < 15; y++)
            {
                for (int x = 0; x < 15; x++)
                    BoardCells.Add(new CellViewModel(x, y)); // 돌 생성
            }

        }

        internal void SetClient(IGameClient client)
        {   // 클라이언트에 이벤트 등록하는 메서드

            if (_client != null)
            {
                _client.Disconnect();
                _client.ConnectionLost -= OnDisConnect;
                _client.PlaceReceived -= PlaceReceived;
                _client.CantPlaceReceived -= CantPlaceReceived;
                _client.ChatReceived -= ChatReceived;
                _client.PlayerJoinReceived -= PlayerJoinReceived;
                _client.ClientJoinResponseReceived -= ClientJoinResponseReceived;
                _client.PlayerLeaveReceived -= PlayerLeaveReceived;
                _client.GameJoinReceived -= GameJoinReceived;
                _client.GameLeaveReceived -= GameLeaveReceived;
                _client.GameEndReceived -= GameEndReceived;
                _client.GameSyncReceived -= GameSyncReceived;
                _client.GameStartReceived -= GameStartReceived;
                _client.TimePassedReceived -= TimePassedReceived;
            }

            _client = client;

            _client.ConnectionLost += OnDisConnect;
            _client.PlaceReceived += PlaceReceived;
            _client.CantPlaceReceived += CantPlaceReceived;
            _client.ChatReceived += ChatReceived;
            _client.PlayerJoinReceived += PlayerJoinReceived;
            _client.ClientJoinResponseReceived += ClientJoinResponseReceived;
            _client.PlayerLeaveReceived += PlayerLeaveReceived;
            _client.GameJoinReceived += GameJoinReceived;
            _client.GameLeaveReceived += GameLeaveReceived;
            _client.GameEndReceived += GameEndReceived;
            _client.GameSyncReceived += GameSyncReceived;
            _client.GameStartReceived += GameStartReceived;
            _client.TimePassedReceived += TimePassedReceived;
        }

        partial void OnMeChanged(PlayerViewModel? value)
        {
            // Me의 속성이 바뀌더라도, Me 자체는 바뀌지 않기 때문에
            // IsMeBlack 같은거에 바인딩된게 안바뀜
            // 따라서 이걸로 바뀌었다는 알람 울리는거 등록해놓기
            if (value == null) return;

            value.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(PlayerViewModel.Type))
                {
                    OnPropertyChanged(nameof(IsMeBlack));
                    OnPropertyChanged(nameof(IsMeWhite));
                    OnPropertyChanged(nameof(CanJoin));
                    OnPropertyChanged(nameof(IsMyTurn));
                    OnPropertyChanged(nameof(CanShowStartButton));
                }
            };
        }

        private void NotifyGameStates() // 게임 상태 변경시(시작, 종료, 리셋 등등) 변경 알림
        {
            OnPropertyChanged(nameof(IsGameStarted));
            OnPropertyChanged(nameof(CurrentTurn));
            OnPropertyChanged(nameof(IsMyTurn));
            OnPropertyChanged(nameof(CanShowStartButton));
        }

        private PlayerViewModel FindPlayer(string nickname)
        {   // Player로 PlayerViewModel 찾기, TODO: 리스트라 O(n)임
            PlayerViewModel player = UserList.FirstOrDefault(p => p!.Nickname == nickname, null)
                ?? throw new Exception("리스트에 없는 플레이어를 찾으려 함");
            return player;
        }

        #region 클라이언트 이벤트
        private async void OnDisConnect()
        {
            NotifyGameStates();
            await _messageBoxService.AlertAsync("연결이 종료되었습니다.");
        }
        private void TimePassedReceived(PlayerType type, int currentlefttime)
        {
            _dispatcher.Invoke(() =>
            {
                if (type == PlayerType.Black)
                    BlackPlayer!.RemainingTime = currentlefttime;
                else
                    WhitePlayer!.RemainingTime = currentlefttime;
            });
        }

        private void GameStartReceived()
        {
            _dispatcher.Invoke(() =>
            {
                BlackPlayer!.RemainingTime = 30;
                WhitePlayer!.RemainingTime = 30;

                string gamestartstring = "게임이 시작되었습니다.";
                ChatMessages.Add(gamestartstring);
                _localgame.StartGame();

                _snackbarService.Show(gamestartstring);

                ChatMessages.Add("흑 전적 (승/패/무):");
                ChatMessages.Add($"{BlackPlayer.Win}/{BlackPlayer.Loss}/{BlackPlayer.Draw}");
                ChatMessages.Add("백 전적 (승/패/무):");
                ChatMessages.Add($"{WhitePlayer.Win}/{WhitePlayer.Loss}/{WhitePlayer.Draw}");
            });
        }

        private void GameSyncReceived(GameSync syncdata)
        {
            _dispatcher.Invoke(() =>
            {
                _localgame.SyncState(syncdata);
                ChatMessages.Add("******");
                ChatMessages.Add("서버 참가 완료");
                ChatMessages.Add("룰:");
                foreach (var rule in _localgame.Rules)
                {
                    ChatMessages.Add(rule.RuleInfoString);
                }
                ChatMessages.Add("******");

                if (syncdata.WhitePlayer != null)
                {
                    var white = FindPlayer(syncdata.WhitePlayer.Nickname);
                    WhitePlayer = white;
                }

                if (syncdata.BlackPlayer != null)
                {
                    var black = FindPlayer(syncdata.BlackPlayer.Nickname);
                    BlackPlayer = black;
                }
            });
        }

        private void GameEndReceived(GameEnd data)
        {
            _dispatcher.Invoke(() =>
            {
                _localgame.ForceGameEnd(data.Winner, data.Reason);
                string winnerstr;
                PlayerViewModel? winplayer;
                switch (data.Winner)
                {
                    case PlayerType.Black:
                        winnerstr = "흑";
                        winplayer = BlackPlayer;
                        BlackPlayer!.AddWin();
                        WhitePlayer?.AddLoss();
                        break;
                    case PlayerType.White:
                        winnerstr = "백";
                        winplayer = WhitePlayer;
                        WhitePlayer!.AddWin();
                        BlackPlayer?.AddLoss();
                        break;
                    default:
                        winnerstr = "";
                        BlackPlayer?.AddDraw();
                        WhitePlayer?.AddDraw();
                        break;
                }

                if (data.Stones != null)
                {   // 승리 시에 승리한 돌에 표시하기
                    foreach (var move in data.Stones)
                    {
                        int x = move.X, y = move.Y;
                        var cell = BoardCells.First(c => c.X == x && c.Y == y);
                        cell.IsWinStone = true;
                    }
                }

                string snackstr;

                if (winnerstr == "")
                    snackstr = "게임이 종료되었습니다. 비겼습니다.";
                else
                    snackstr = $"게임이 종료되었습니다. {data.Winner} 승리!";

                _snackbarService.Show(snackstr, "확인");

                string result;
                if (data.Winner == PlayerType.Observer)
                    result = "경기 종료. 비겼습니다.";
                else if (data.Winner == PlayerType.Black)
                    result = $"경기 종료. 흑돌 {BlackPlayer?.Nickname} 승리!";
                else
                    result = $"경기 종료. 백돌 {WhitePlayer?.Nickname} 승리!";

                ChatMessages.Add("*****");
                ChatMessages.Add(result);
                ChatMessages.Add($" 이유: {data.Reason}");
                ChatMessages.Add("*****");

                // TODO: 전적 업데이트
            });
        }

        private void GameLeaveReceived(PlayerType type, Player player)
        {
            _dispatcher.Invoke(() =>
            {
                var leaveType = type;

                if (leaveType == PlayerType.Black)
                {
                    BlackPlayer!.Type = PlayerType.Observer;
                    BlackPlayer = null;
                }
                else
                {
                    WhitePlayer!.Type = PlayerType.Observer;
                    WhitePlayer = null;
                }

                if (player.Nickname == Me?.Nickname)
                    Me.Type = PlayerType.Observer;
            });
        }

        private void GameJoinReceived(PlayerType type, Player player)
        {
            _dispatcher.Invoke(() =>
            {
                var findplayer = FindPlayer(player.Nickname);

                findplayer.RemainingTime = 30;
                findplayer.Type = type;

                if (type == PlayerType.Black)
                    BlackPlayer = findplayer;
                else
                    WhitePlayer = findplayer;
            });
        }

        private void PlayerLeaveReceived(Player player)
        {
            _dispatcher.Invoke(() =>
            {
                string exitnotify = $"{player.Nickname}님이 나가셨습니다.";
                var playerviewmodel = FindPlayer(player.Nickname);
                UserList.Remove(playerviewmodel);
                ChatMessages.Add(exitnotify);

                if (player.Nickname == Me!.Nickname)
                {
                    _client!.Disconnect();
                }

                if (BlackPlayer?.Nickname == player.Nickname)
                    BlackPlayer = null;
                else if (WhitePlayer?.Nickname == player.Nickname)
                    WhitePlayer = null;

                _snackbarService.Show(exitnotify, "확인");
            });
        }

        private void ClientJoinResponseReceived(Player me, IEnumerable<Player> users)
        {
            _dispatcher.Invoke(() =>
            {
                UserList.Clear();

                foreach (var item in users)
                {
                    UserList.Add(new PlayerViewModel(item));
                }

                Me = FindPlayer(me.Nickname);
            });
        }

        private void PlayerJoinReceived(Player newplayer)
        {
            _dispatcher.Invoke(() =>
            {
                string joinnotify = $"{newplayer.Nickname}님이 참가하였습니다.";
                ChatMessages.Add(joinnotify);

                if (newplayer.Nickname != Me?.Nickname) // 자기 자신이 아닌 경우만
                    UserList.Add(new PlayerViewModel(newplayer));

                _snackbarService.Show(joinnotify, "확인");
            });
        }

        private void ChatReceived(Player sender, string message)
        {
            _dispatcher.Invoke(() =>
            {
                ChatMessages.Add($"{sender.Nickname} : {message}");
            });
        }

        private void CantPlaceReceived(GameMove move)
        {
            _dispatcher.Invoke(() =>
            {
                int x = move.X;
                int y = move.Y;

                _ = _messageBoxService.ErrorAsync($"{x}, {y}에 둘 수 없습니다.");
            });
        }

        private void PlaceReceived(GameMove move)
        {
            _dispatcher.Invoke(() =>
            {
                lock (_localgame)
                {
                    _localgame.TryPlaceStone(move);
                }
                Logger.Debug($"{move.X}, {move.Y} {move.PlayerType} 착수");
            });
        }

        #endregion

        #region UI 상태 변경 메서드
        List<CellViewModel> lastMarked = new List<CellViewModel>();

        private void UpdateForbiddenMarks(PlayerType obj)
        {
            // 금수 시에 X자 업데이트
            _dispatcher.Invoke(() =>
            {
                if (Me!.Type == PlayerType.Observer) return;

                if (!IsMyTurn)
                {
                    foreach (var cell in lastMarked)
                    {
                        cell.IsForbidden = false;
                    }
                    lastMarked.Clear();
                    return;
                }

                lock (_localgame)
                {
                    foreach (var cell in BoardCells) // 보드 셀 순회하며
                    {
                        if (_localgame.GetStoneAt(cell.X, cell.Y) != 0)
                        {
                            cell.IsForbidden = false;
                            continue;
                        }
                        var temppos = new GameMove(cell.X, cell.Y, _localgame.Board.Count, Me.Type);
                        cell.IsForbidden = false;

                        foreach (var rule in _localgame.Rules) // 룰 순회
                        {
                            if (!rule.IsValidMove(_localgame, temppos))
                            {
                                cell.IsForbidden = true;
                                lastMarked.Add(cell);
                                break;
                            }
                        }
                    }

                }
            });
        }

        private void SyncStone()
        {
            int lastmove = _localgame.Board.Count;
            lock (_localgame)
            {
                foreach (var move in _localgame.Board.GetHistory())
                {
                    int index = move.Y * 15 + move.X;
                    BoardCells[index].StoneState = (int)move.PlayerType;

                    if (move.MoveNumber == lastmove)
                        BoardCells[index].IsLastStone = true;
                }
            }
        }
        private void StonePlaced(GameMove data)
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

        private void ResetStoneUI()
        {
            foreach (var cell in BoardCells)
            {
                cell.StoneState = 0;
                cell.IsLastStone = false;
                cell.IsForbidden = false;
                cell.IsWinStone = false;
            }
        }

        private void ResetAllUI()
        {
            UserList.Clear();
            ChatMessages.Clear();

            _localgame.ResetGame();
            WhitePlayer = null;
            BlackPlayer = null;
        }

        #endregion

        #region 커맨드

        [RelayCommand]
        private async Task PlaceStone(CellViewModel? cell)
        { // 보드 클릭 시
            if (cell == null)
                return;

            if (!IsGameStarted)
                return;

            if (cell.StoneState != 0) return; // 이미 놓은 곳 (클라이언트 체크)

            if (Me?.Type != CurrentTurn) return; // 사용자 턴 아님

            if (_client == null) return;

            var move = new GameMove(cell.X, cell.Y, 0, Me.Type);

            await _client.SendPlaceAsync(move);
        }

        [RelayCommand]
        private async Task SendChat()
        {
            if (_client != null && !string.IsNullOrEmpty(ChatInput))
            {
                await _client.SendChatAsync(ChatInput);
                ChatInput = "";
            }
        }

        [RelayCommand]
        private async Task JoinGame(PlayerType type)
        {
            if (!CanJoin || _client == null) return;

            await _client.SendJoinGameAsync(type);
        }

        [RelayCommand]
        private async Task LeaveGame()
        {
            if (Me?.Type == PlayerType.Observer) return;
            if (_client == null) return;

            if (IsGameStarted)
            {
                var response = await _messageBoxService.CautionAsync("주의", "게임 진행 중입니다. 정말로 나가시겠습니까?");

                if (!response)
                    return;
            }

            await _client.SendLeaveGameAsync();
        }

        [RelayCommand(AllowConcurrentExecutions = false)]
        private async Task OpenConnectWindow() // 연결 창 여는 커맨드
        {
            if (_client != null && _client.IsConnected)
            {
                var result = await _messageBoxService.CautionAsync("주의", "연결이 종료됩니다. 계속하시겠습니까?");
                if (!result) return;
                _client.Disconnect();
                _server.StopServer();
            }

            var connectVM = Ioc.Default.GetRequiredService<ConnectViewModel>();

            var resultVM = _windowService.ShowDialog(connectVM);

            if (resultVM != null)
            {
                using var cts = new CancellationTokenSource();

                string nick = resultVM.Nickname;
                string ip = resultVM.IpAddress;
                int port = resultVM.Port;
                var rule = resultVM.SelectedDTRule;

                ResetAllUI();

                var loadingVM = Ioc.Default.GetRequiredService<LoadingDialogViewModel>();
                loadingVM.Title = "연결 중...";
                var dialogTask = _dialogService.ShowAsync(loadingVM);
                await Task.Delay(100);
                // 다이얼로그 뜨기도 전에 바로 연결해버려서 다이얼로그 끄기를 하면 에러남

                if (resultVM.ConnectionType == ConnectionType.Single)
                {
                    await StartSoloMode(rule);
                    return;
                }

                if (resultVM.ConnectionType == ConnectionType.Server)
                {
                    try
                    {
                        await _server.StartAsync(port);
                        ChatMessages.Add("서버 생성 완료.");

                        _server.AddRule(RuleFactory.CreateRule(new DoubleThreeRuleInfo(rule)));

                        ip = "127.0.0.1";
                        // 서버인 경우 클라이언트를 자기 자신에게 연결 
                    }
                    catch (SocketException ex) when (ex.SocketErrorCode == SocketError.AddressAlreadyInUse)
                    {
                        await _messageBoxService.ErrorAsync("포트가 이미 사용중입니다. 다른 포트를 사용해 보세요.");
                        _server.StopServer();
                        _client?.Disconnect();
                    }
                }
                var onlineclient = Ioc.Default.GetRequiredService<IGameClient>();
                SetClient(onlineclient);

                var connectTask = _client!.ConnectAsync(ip, port, nick, cts.Token);

                var completeTask = await Task.WhenAny(connectTask, dialogTask);

                if (completeTask == dialogTask) // 다이얼로그가 먼저 닫힌 경우
                {
                    cts.Cancel();
                    _snackbarService.Show("연결이 취소되었습니다.", "확인");
                }
                else
                {
                    bool isSuccess = await connectTask;
                    loadingVM.Close();

                    if (isSuccess)
                    {
                        // TODO: 연결 성공했다는 알림
                    }
                    else if (!cts.IsCancellationRequested)
                    {
                        await _messageBoxService.ErrorAsync("연결에 실패했습니다.");
                    }
                }
            }
        }

        [RelayCommand]
        private async Task StartGame() // 게임 시작 버튼 클릭
        {
            await _client!.SendGameStartAsync();
        }

        [RelayCommand]
        private void OpenInformationWindow()
        {
            var infoVM = Ioc.Default.GetRequiredService<InformationViewModel>();
            _windowService.ShowDialog(infoVM);
        }
        #endregion
        private async Task StartSoloMode(DoubleThreeRuleType ruletype)
        {
            var soloclient = Ioc.Default.GetRequiredService<SoloGameClient>();
            SetClient(soloclient);

            soloclient.AddRule(new DoubleThreeRule(new DoubleThreeRuleInfo(ruletype)));
            await _client!.ConnectAsync("", 0, "혼자하기", CancellationToken.None);
            await _client.SendJoinGameAsync(PlayerType.White);
            await _client.SendJoinGameAsync(PlayerType.Black);
            await _client.SendGameStartAsync();
        }
    }
}
