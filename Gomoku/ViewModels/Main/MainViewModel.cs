using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Gomoku.Models.Common;
using Gomoku.Models.Domain;
using Gomoku.Models.Interfaces;
using Gomoku.Models.Messages;
using Gomoku.Services.Applications.Auth;
using Gomoku.Services.Applications.Command;
using Gomoku.Services.Applications.Game;
using Gomoku.Services.Wpf;
using Gomoku.Services.Wpf.Dialogs;
using Gomoku.Services.Wpf.Window;
using System.Collections.ObjectModel;

// TODO: 여기 코드가 너무 많다 분리하자

namespace Gomoku.ViewModels
{
    public partial class MainViewModel : ViewModelBase,
        IRecipient<GameStartMessage>,
        IRecipient<GameSyncMessage>,
        IRecipient<GameEndMessage>,
        IRecipient<PlayerDisconnectedMessage>,
        IRecipient<PlayerConnectedMessage>,
        IRecipient<ChatReceivedMessage>,
        IRecipient<SessionConnectLostMessage>,
        IRecipient<LastStoneCanceledMessage>,
        IRecipient<PlaceRejectedMessage>,
        IRecipient<PlayerNicknameChangedMessage>
    {
        private readonly IMessageBoxService _messageBoxService;
        private readonly IWindowService _windowService;
        private readonly IDialogService _dialogService;
        private readonly ISnackbarService _snackbarService;

        private readonly IGameSessionService _gameSession;
        private readonly IAuthSessionService _authSession;
        private readonly IViewModelFactory _viewModelFactory;

        private readonly IServerCommandService _serverCommandService;

        public object MainSnackBarQueue => _snackbarService.MessageQueue;

        #region 바인딩 속성들
        [ObservableProperty]
        private BoardViewModel _board;
        public SessionViewModel Session { get; }
        public ObservableCollection<string> ChatMessages { get; } = new ObservableCollection<string>();
        // 채팅

        [ObservableProperty]
        private string _chatInput = string.Empty;
        #endregion

        public MainViewModel(IMessageBoxService messageBoxService, IWindowService windowService,
            IDialogService dialogService, ISnackbarService snackbarService,
            IDispatcher dispatcher, IGameSessionService gameSessionService, IAuthSessionService authSession,
            IViewModelFactory viewModelFactory,
            IMessenger messenger, IServerCommandService serverCommandService,
            BoardViewModel boardViewModel, SessionViewModel sessionViewModel) : base(dispatcher)
        {
            _messageBoxService = messageBoxService;
            _windowService = windowService;
            _dialogService = dialogService;
            _snackbarService = snackbarService;
            _viewModelFactory = viewModelFactory;

            _serverCommandService = serverCommandService;

            _board = boardViewModel;

            _gameSession = gameSessionService;
            _authSession = authSession;

            Session = sessionViewModel;

            messenger.RegisterAll(this);
        }

        #region 클라이언트 이벤트
        #region Receives
        public void Receive(SessionConnectLostMessage msg) => ReceiveInvoke(HandleConnectionLost);
        public void Receive(GameStartMessage msg) => ReceiveInvoke(HandleGameStarted, msg);
        public void Receive(GameSyncMessage msg) => ReceiveInvoke(HandleGameSynced, msg);
        public void Receive(GameEndMessage msg) => ReceiveInvoke(HandleGameEnded, msg);
        public void Receive(PlayerDisconnectedMessage msg) => ReceiveInvoke(HandlePlayerDisconnected, msg);
        public void Receive(PlayerConnectedMessage msg) => ReceiveInvoke(HandlePlayerConnected, msg);
        public void Receive(ChatReceivedMessage msg) => ReceiveInvoke(HandleChatReceived, msg);
        public void Receive(LastStoneCanceledMessage msg) => ReceiveInvoke(HandleLastStoneCanceled, msg);
        public void Receive(PlaceRejectedMessage msg) => ReceiveInvoke(HandlePlaceRejectedReceived, msg);
        public void Receive(PlayerNicknameChangedMessage message) => ReceiveInvoke(HandlePlayerNicknameChanged, message);
        #endregion

        private void HandleConnectionLost()
        {
            ResetAllUI();
            _snackbarService.Show("서버와의 연결이 끊어졌습니다.", "확인");
        }

        private void HandleLastStoneCanceled(LastStoneCanceledMessage msg)
        {
            var playerstr = msg.Type == PlayerType.Black ? "흑" : "백";

            _snackbarService.Show($"{playerstr}이 무르기를 사용하였습니다. 남은 무르기 횟수: {msg.LeftCancelCount}", "확인");
        }

        private void HandleGameStarted(GameStartMessage msg)
        {
            string gamestartstring = "게임이 시작되었습니다.";
            ChatMessages.Add(gamestartstring);

            _snackbarService.Show(gamestartstring);

            if (msg.IsRecordUse)
            {
                var brecord = msg.BlackRelativeRecord!;
                var wrecord = msg.WhiteRelativeRecord!;

                ChatMessages.Add("흑 상대 전적 (승/패/무):");
                ChatMessages.Add($"{brecord.Win}/{brecord.Loss}/{brecord.Draw}");
                ChatMessages.Add("백 상대 전적 (승/패/무):");
                ChatMessages.Add($"{wrecord.Win}/{wrecord.Loss}/{wrecord.Draw}");
            }
        }

        private void HandleGameSynced(GameSyncMessage syncdata)
        {
            ChatMessages.Add("******");
            ChatMessages.Add("서버 참가 완료");
            ChatMessages.Add("룰:");

            ChatMessages.Add(_gameSession.RulesInfo);

            ChatMessages.Add("******");
        }

        private void HandleGameEnded(GameEndMessage data)
        {
            string winnerstr;
            PlayerViewModel? winplayer = null;
            switch (data.Winner)
            {
                case PlayerType.Black:
                    winnerstr = "흑";
                    winplayer = Session.BlackPlayer;
                    break;
                case PlayerType.White:
                    winnerstr = "백";
                    winplayer = Session.WhitePlayer;
                    break;
                default:
                    winnerstr = "";
                    break;
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
                result = $"경기 종료. 흑돌 {Session.BlackPlayer?.Nickname} 승리!";
            else
                result = $"경기 종료. 백돌 {Session.WhitePlayer?.Nickname} 승리!";

            ChatMessages.Add("*****");
            ChatMessages.Add(result);
            ChatMessages.Add($" 이유: {data.Reason}");
            ChatMessages.Add("*****");
        }

        private void HandlePlayerDisconnected(PlayerDisconnectedMessage msg)
        {
            var player = msg.Player;

            string exitnotify = $"{player.Nickname}님이 나가셨습니다.";
            ChatMessages.Add(exitnotify);

            _snackbarService.Show(exitnotify, "확인");
        }

        private void HandlePlayerConnected(PlayerConnectedMessage msg)
        {
            var newplayer = msg.Player;

            string joinnotify = $"{newplayer.Nickname}님이 참가하였습니다.";

            ChatMessages.Add(joinnotify);
            _snackbarService.Show(joinnotify, "확인");
        }

        private void HandleChatReceived(ChatReceivedMessage msg)
        {
            ChatMessages.Add($"{msg.sender.Nickname} : {msg.Message}");
        }

        private void HandlePlaceRejectedReceived(PlaceRejectedMessage msg)
        {
            var move = msg.Move;

            int x = move.X;
            int y = move.Y;

            _ = _messageBoxService.ErrorAsync($"{x}, {y}에 둘 수 없습니다.");
        }

        private void HandlePlayerNicknameChanged(PlayerNicknameChangedMessage message)
        {
            ChatMessages.Add($"{message.OldNickname}님이 {message.NewNickname}(으)로 닉네임을 변경하였습니다.");
        }

        #endregion

        #region UI 상태 변경 메서드


        private void ResetAllUI()
        {
            ChatMessages.Clear();
        }

        #endregion

        #region 커맨드
        [RelayCommand]
        private async Task SendChat()
        {
            if (_gameSession.IsSessionAlive && !string.IsNullOrEmpty(ChatInput))
            {
                if (ChatInput.StartsWith('/')) // 명령어인 경우
                {
                    var result = await _serverCommandService.ExecuteCommandAsync(ChatInput);

                    if (!result.IsSuccess)
                    {
                        ChatMessages.Add($"{result.Message}");
                    }
                }
                else
                    await _gameSession.SendChatAsync(ChatInput);

                ChatInput = "";
            }
        }

        [RelayCommand]
        private async Task JoinGame(PlayerType type)
        {
            if (Session.Me?.Type != PlayerType.Observer || !_gameSession.IsSessionAlive) return;
            if (Session.BlackPlayer != null && type == PlayerType.Black) return;
            if (Session.WhitePlayer != null && type == PlayerType.White) return;

            await _gameSession.JoinGameAsync(type);
        }

        [RelayCommand]
        private async Task LeaveGame()
        {
            if (Session.Me?.Type == PlayerType.Observer) return;
            if (!_gameSession.IsSessionAlive) return;

            if (Session.IsGameStarted)
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
                await _authSession.StopSessionAsync();
            }
            var connectVM = _viewModelFactory.Create<ConnectViewModel>();

            _windowService.ShowDialog(connectVM);
        }

        [RelayCommand]
        private void OpenInformationWindow()
        {
            var infoVM = _viewModelFactory.Create<InformationViewModel>();
            _windowService.ShowDialog(infoVM);
        }

        [RelayCommand]
        private async Task CancelLastStone()
        {
            if (!_gameSession.IsSessionAlive) return;

            try
            {
                await _gameSession.CancelLastStoneAsync();
            }
            catch (CancelNotAvailableException)
            {
                await _messageBoxService.ErrorAsync("무르기 횟수가 없습니다.");
            }
        }

        [RelayCommand]
        private async Task GameStart()
        {
            await _gameSession.StartGameAsync();
        }

        [RelayCommand]
        private async Task OpenRankingWindow()
        {
            var rankingVM = _viewModelFactory.Create<RankingViewModel>();
            _windowService.ShowDialog(rankingVM);
        }

        [RelayCommand]
        private void OpenMatchWindow()
        {
            var MatchVM = _viewModelFactory.Create<MatchViewModel>();
            _windowService.ShowDialog(MatchVM);
        }

        #endregion
    }
}
