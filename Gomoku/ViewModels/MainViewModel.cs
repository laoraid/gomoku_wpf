using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using Gomoku.Models;
using Gomoku.Models.DTO;
using Gomoku.Services.Interfaces;
using System.Collections.ObjectModel;
using System.Net.Sockets;

// TODO: 여기 코드가 너무 많다 분리하자

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

        private readonly IGameSessionService _gameSession;

        public object MainSnackBarQueue => _snackbarService.MessageQueue;

        #region 바인딩 속성들

        [ObservableProperty]
        private PlayerViewModel? _me;

        [ObservableProperty]
        private BoardViewModel _board;

        private bool IsGameStarted => _gameSession.IsGameStarted;

        public ObservableCollection<CellViewModel> BoardCells { get; } = new ObservableCollection<CellViewModel>();
        // 격자 버튼 (15x15) 누르면 돌 착수
        public ObservableCollection<PlayerViewModel> UserList { get; } = new ObservableCollection<PlayerViewModel>();
        // 참가자 리스트
        public ObservableCollection<string> ChatMessages { get; } = new ObservableCollection<string>();
        // 채팅

        [ObservableProperty]
        private PlayerViewModel? _blackPlayer;

        [ObservableProperty]
        private PlayerViewModel? _whitePlayer;

        [ObservableProperty]
        private string _chatInput = string.Empty;

        #endregion

        public MainViewModel(IMessageBoxService messageBoxService, IWindowService windowService,
            ISoundService soundService, IDialogService dialogService, ISnackbarService snackbarService,
            IDispatcher dispatcher, IGameSessionService gameSessionService,
            BoardViewModel boardViewModel)
        {
            _messageBoxService = messageBoxService;
            _windowService = windowService;
            _soundService = soundService;
            _dialogService = dialogService;
            _snackbarService = snackbarService;
            _dispatcher = dispatcher;

            _board = boardViewModel;

            _gameSession = gameSessionService;

            _gameSession.GameEnded += (windata) =>
            {
                NotifyGameStates();
            };
            _gameSession.GameReset += () =>
            {
                NotifyGameStates();
            };
            _gameSession.GameStarted += () =>
            {
                NotifyGameStates();
            };

            _gameSession.ConnectionLost += HandleConnectionLost;
            _gameSession.TimeUpdated += HandleTimeUpdated;
            _gameSession.GameStarted += HandleGameStarted;
            _gameSession.GameSynced += HandleGameSynced;
            _gameSession.GameEnded += HandleGameEnded;
            _gameSession.PlayerGameLeft += HandlePlayerGameLeft;
            _gameSession.PlayerGameJoined += HandlePlayerGameJoined;

            _gameSession.PlayerDisconnected += HandlePlayerDisconnected;
            _gameSession.SessionInitialized += HandleSessionInitialized;
            _gameSession.PlayerConnected += HandlePlayerConnected;

            _gameSession.ChatReceived += HandleChatReceived;
            _gameSession.PlaceRejected += HandleCantPlaceReceived;

            for (int y = 0; y < 15; y++)
            {
                for (int x = 0; x < 15; x++)
                    BoardCells.Add(new CellViewModel(x, y)); // 돌 생성
            }

        }



        partial void OnMeChanged(PlayerViewModel? value)
        {
            // Me의 속성이 바뀌더라도, Me 자체는 바뀌지 않기 때문에
            // IsMeBlack 같은거에 바인딩된게 안바뀜
            // 따라서 이걸로 바뀌었다는 알람 울리는거 등록해놓기
            if (value == null) return;

            Board.Me = value;

            value.PropertyChanged += (s, e) =>
            {
                OnPropertyChanged(nameof(Me));
            };
        }

        private void NotifyGameStates() // 게임 상태 변경시(시작, 종료, 리셋 등등) 변경 알림
        {
            OnPropertyChanged(nameof(IsGameStarted));
        }

        private PlayerViewModel FindPlayer(string nickname)
        {   // Player로 PlayerViewModel 찾기, TODO: 리스트라 O(n)임
            PlayerViewModel player = UserList.FirstOrDefault(p => p!.Nickname == nickname, null)
                ?? throw new Exception("리스트에 없는 플레이어를 찾으려 함");
            return player;
        }

        #region 클라이언트 이벤트
        private async void HandleConnectionLost()
        {
            NotifyGameStates();
            await _messageBoxService.AlertAsync("연결이 종료되었습니다.");
        }
        private void HandleTimeUpdated(PlayerType type, int currentlefttime)
        {
            _dispatcher.Invoke(() =>
            {
                if (type == PlayerType.Black)
                    BlackPlayer!.RemainingTime = currentlefttime;
                else
                    WhitePlayer!.RemainingTime = currentlefttime;
            });
        }

        private void HandleGameStarted()
        {
            _dispatcher.Invoke(() =>
            {
                BlackPlayer!.RemainingTime = 30;
                WhitePlayer!.RemainingTime = 30;

                string gamestartstring = "게임이 시작되었습니다.";
                ChatMessages.Add(gamestartstring);

                _snackbarService.Show(gamestartstring);

                ChatMessages.Add("흑 전적 (승/패/무):");
                ChatMessages.Add($"{BlackPlayer.Win}/{BlackPlayer.Loss}/{BlackPlayer.Draw}");
                ChatMessages.Add("백 전적 (승/패/무):");
                ChatMessages.Add($"{WhitePlayer.Win}/{WhitePlayer.Loss}/{WhitePlayer.Draw}");
            });
        }

        private void HandleGameSynced(GameSync syncdata)
        {
            _dispatcher.Invoke(() =>
            {
                ChatMessages.Add("******");
                ChatMessages.Add("서버 참가 완료");
                ChatMessages.Add("룰:");

                ChatMessages.Add(_gameSession.RulesInfo);

                ChatMessages.Add("******");

                if (syncdata.WhitePlayer != null)
                {
                    var white = FindPlayer(syncdata.WhitePlayer.Nickname);
                    WhitePlayer = white;
                    white.UpdateFromModel();
                }

                if (syncdata.BlackPlayer != null)
                {
                    var black = FindPlayer(syncdata.BlackPlayer.Nickname);
                    BlackPlayer = black;
                    black.UpdateFromModel();
                }
            });
        }

        private void HandleGameEnded(GameEnd data)
        {
            _dispatcher.Invoke(() =>
            {
                string winnerstr;
                PlayerViewModel? winplayer = null;
                switch (data.Winner)
                {
                    case PlayerType.Black:
                        winnerstr = "흑";
                        winplayer = BlackPlayer;
                        break;
                    case PlayerType.White:
                        winnerstr = "백";
                        winplayer = WhitePlayer;
                        break;
                    default:
                        winnerstr = "";
                        break;
                }

                winplayer?.UpdateFromModel();

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
            });
        }

        private void HandlePlayerGameLeft(PlayerType type, Player player)
        {
            _dispatcher.Invoke(() =>
            {
                var leaveType = type;

                if (leaveType == PlayerType.Black)
                {
                    BlackPlayer = null;
                }
                else
                {
                    WhitePlayer = null;
                }

                var findplayer = FindPlayer(player.Nickname);
                findplayer.UpdateFromModel();
            });
        }

        private void HandlePlayerGameJoined(PlayerType type, Player player)
        {
            _dispatcher.Invoke(() =>
            {
                var findplayer = FindPlayer(player.Nickname);

                findplayer.RemainingTime = 30;
                findplayer.UpdateFromModel();

                if (type == PlayerType.Black)
                    BlackPlayer = findplayer;
                else
                    WhitePlayer = findplayer;
            });
        }

        private void HandlePlayerDisconnected(Player player)
        {
            _dispatcher.Invoke(() =>
            {
                string exitnotify = $"{player.Nickname}님이 나가셨습니다.";
                var playerviewmodel = FindPlayer(player.Nickname);
                UserList.Remove(playerviewmodel);
                ChatMessages.Add(exitnotify);

                if (BlackPlayer?.Nickname == player.Nickname)
                    BlackPlayer = null;
                else if (WhitePlayer?.Nickname == player.Nickname)
                    WhitePlayer = null;

                _snackbarService.Show(exitnotify, "확인");
            });
        }

        private void HandleSessionInitialized(Player me, IEnumerable<Player> users)
        {
            _dispatcher.Invoke(() =>
            {
                UserList.Clear();

                foreach (var item in users)
                {
                    UserList.Add(new PlayerViewModel(item));
                }

                Me = FindPlayer(me.Nickname);
                Me.UpdateFromModel();
            });
        }

        private void HandlePlayerConnected(Player newplayer)
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

        private void HandleChatReceived(Player sender, string message)
        {
            _dispatcher.Invoke(() =>
            {
                ChatMessages.Add($"{sender.Nickname} : {message}");
            });
        }

        private void HandleCantPlaceReceived(GameMove move)
        {
            _dispatcher.Invoke(() =>
            {
                int x = move.X;
                int y = move.Y;

                _ = _messageBoxService.ErrorAsync($"{x}, {y}에 둘 수 없습니다.");
            });
        }

        #endregion

        #region UI 상태 변경 메서드


        private void ResetAllUI()
        {
            UserList.Clear();
            ChatMessages.Clear();
            WhitePlayer = null;
            BlackPlayer = null;
        }

        #endregion

        #region 커맨드
        [RelayCommand]
        private async Task SendChat()
        {
            if (!_gameSession.IsSessionAlive && !string.IsNullOrEmpty(ChatInput))
            {
                await _gameSession.SendChatAsync(ChatInput);
                ChatInput = "";
            }
        }

        [RelayCommand]
        private async Task JoinGame(PlayerType type)
        {
            if (Me?.Type != PlayerType.Observer || !_gameSession.IsSessionAlive) return;

            await _gameSession.JoinGameAsync(type);
        }

        [RelayCommand]
        private async Task LeaveGame()
        {
            if (Me?.Type == PlayerType.Observer) return;
            if (!_gameSession.IsSessionAlive) return;

            if (IsGameStarted)
            {
                var response = await _messageBoxService.CautionAsync("주의", "게임 진행 중입니다. 정말로 나가시겠습니까?");

                if (!response)
                    return;
            }

            await _gameSession.LeaveGameAsync();
        }

        [RelayCommand(AllowConcurrentExecutions = false)]
        private async Task OpenConnectWindow() // 연결 창 여는 커맨드
        {
            if (_gameSession.IsSessionAlive)
            {
                var result = await _messageBoxService.CautionAsync("주의", "연결이 종료됩니다. 계속하시겠습니까?");
                if (!result) return;
                _gameSession.StopSession();
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

                ConnectionOption option = new ConnectionOption(ip, port, nick, rule, resultVM.ConnectionType, cts.Token);

                ResetAllUI();
                await Task.Delay(100);
                var loadingVM = Ioc.Default.GetRequiredService<LoadingDialogViewModel>();
                loadingVM.Title = "연결 중...";
                var dialogTask = _dialogService.ShowAsync(loadingVM);
                await Task.Delay(100);
                // 다이얼로그 뜨기도 전에 바로 연결해버려서 다이얼로그 끄기를 하면 에러남


                var connectTask = _gameSession.StartSessionAsync(option);

                var completeTask = await Task.WhenAny(connectTask, dialogTask);

                if (completeTask == dialogTask) // 다이얼로그가 먼저 닫힌 경우
                {
                    cts.Cancel();
                    await connectTask;
                    _gameSession.StopSession();
                    _snackbarService.Show("연결이 취소되었습니다.", "확인");
                }
                else
                {
                    try // 연결이 먼저 완료됨
                    {
                        bool isSuccess = await connectTask;


                        if (isSuccess)
                        {
                            _snackbarService.Show($"연결에 성공했습니다.", "확인");
                        }
                        else if (!cts.IsCancellationRequested)
                        {
                            await _messageBoxService.ErrorAsync("연결에 실패했습니다.");
                        }
                    }
                    catch (TimeoutException)
                    {
                        await _messageBoxService.ErrorAsync("연결 시간이 초과되었습니다.");
                    }
                    catch (SocketException)
                    {
                        await _messageBoxService.ErrorAsync("서버에 접속할 수 없습니다. (요청 거부됨)");
                    }
                    catch (Exception ex)
                    {
                        await _messageBoxService.ErrorAsync($"연결 중 오류 : {ex.Message}");
                    }
                    finally
                    {
                        loadingVM.Close();
                    }
                }
            }
        }

        [RelayCommand]
        private void OpenInformationWindow()
        {
            var infoVM = Ioc.Default.GetRequiredService<InformationViewModel>();
            _windowService.ShowDialog(infoVM);
        }
        #endregion
    }
}
