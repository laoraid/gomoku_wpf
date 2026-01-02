using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Gomoku.Messages;
using Gomoku.Models;
using Gomoku.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace Gomoku.ViewModels
{
    public partial class MainViewModel : ViewModelBase
    {
        private GameClient _client; // 서버도 하나의 클라이언트로 자기 자신에게 접속
        private GameServer _server; // 서버일 경우만 생성

        private GomokuManager _localgame; // 클라이언트 전용

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CanShowStartButton))]
        [NotifyPropertyChangedFor(nameof(IsMeBlack))]
        [NotifyPropertyChangedFor(nameof(IsMeWhite))]
        [NotifyPropertyChangedFor(nameof(CanJoin))] // MyPlayerType 바뀔때 아래것들도 새로고침됨
        private PlayerType _myPlayerType = PlayerType.Observer;

        public bool IsMeBlack => MyPlayerType == PlayerType.Black;
        public bool IsMeWhite => MyPlayerType == PlayerType.White;
        public bool CanJoin => MyPlayerType == PlayerType.Observer;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CanShowStartButton))]
        private bool _isGameStarted = false;

        public bool CanShowStartButton =>
            MyPlayerType == PlayerType.Black &&
            !IsGameStarted &&
            WhiteNickname != "백돌 대기 중...";

        [ObservableProperty]
        private PlayerType _currentTurn = PlayerType.Black;

        public ObservableCollection<CellViewModel> BoardCells { get; } = new ObservableCollection<CellViewModel>();
        // 격자 버튼 (15x15) 누르면 돌 착수
        public ObservableCollection<string> UserList { get; set; } = new ObservableCollection<string>();
        // 참가자 리스트
        public ObservableCollection<string> ChatMessages { get; } = new ObservableCollection<string>();
        // 채팅

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CanShowStartButton))]
        private string _blackNickname = "흑돌 대기 중...";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CanShowStartButton))]
        private string _WhiteNickname = "백돌 대기 중...";

        [ObservableProperty]
        private string _chatInput = string.Empty;

        [ObservableProperty]
        private int _blacktime = 30;

        [ObservableProperty]
        private int _whitetime = 30;

        public MainViewModel()
        {
            _client = new GameClient();
            _localgame = new GomokuManager();

            _localgame.OnStonePlaced += SyncStoneUI; // 돌 놓았을때 UI 반영
            _localgame.OnTurnChanged += (player) => CurrentTurn = player;
            _localgame.OnGameEnded += (winner, reason) =>
            {
                GameEnd(winner, reason);
                IsGameStarted = false;
            };
            _localgame.OnGameReset += () =>
            {
                ResetStoneUI();
            };
            _localgame.OnGameStarted += () =>
            {
                IsGameStarted = true;
            };

            for (int y = 0; y < 15; y++)
            { 
                for (int x = 0; x < 15; x++)
                    BoardCells.Add(new CellViewModel(x, y)); // 돌 생성
            }

            _client.OnDataReceived += HandleClientDataReceived;
            _client.ConnectionLost += () =>
            {
                IsGameStarted = false;
                WeakReferenceMessenger.Default.Send(new DialogMessage("오류", "연결이 종료되었습니다."));
            };
        }

        private void GameEnd(PlayerType winner, string reason)
        {
            string result;
            if (winner == PlayerType.Observer)
                result = "경기 종료. 비겼습니다.";
            else if (winner == PlayerType.Black)
                result = $"경기 종료. 흑돌 {BlackNickname} 승리!";
            else
                result = $"경기 종료. 백돌 {WhiteNickname} 승리!";

            ChatMessages.Add(result + $" 이유: {reason}");
        }

        private void SyncStoneUI(PositionData data)
        {
            int index = data.Y * 15 + data.X; // 2차원 격자 주소를 1차원 ItemsControl 주소로 바꾸기
            BoardCells[index].StoneState = (int)data.Player;
        }

        private void ResetStoneUI()
        {
            foreach (var cell in BoardCells)
            {
                cell.StoneState = 0;
            }
        }

        private void ResetGamerUI(PlayerType type)
        {
            if (type == PlayerType.Black)
                BlackNickname = "흑돌 대기 중...";
            else if (type == PlayerType.White)
                WhiteNickname = "백돌 대기 중...";
        }

        private void HandleClientDataReceived(GameData data)
        { // 클라이언트 데이터 수신 처리
            Application.Current.Dispatcher.Invoke(() =>
            {
                switch(data)
                {
                    case PositionData move:
                        _localgame.TryPlaceStone(move);
                        break;
                    case PlaceResponseData placeresdata:
                        int x = placeresdata.Position.X;
                        int y = placeresdata.Position.Y;

                        WeakReferenceMessenger.Default.Send(new DialogMessage("오류", $"{x}, {y} 에 둘 수 없습니다."));
                        break;
                    case ChatData chat:
                        ChatMessages.Add($"{chat.SenderNickname} : {chat.Message}");
                        break;
                    case ClientJoinData data:
                        string joinnotify = $"{data.Nickname}님이 참가하였습니다.";
                        ChatMessages.Add(joinnotify);

                        if (data.Nickname != _client.Nickname) // 자기 자신이 아닌 경우만
                            UserList.Add(data.Nickname);

                        break;
                    case ClientJoinResponseData joinresdata:
                        string realnick = joinresdata.ConfirmedNickname;
                        _client.Nickname = realnick;

                        UserList.Clear();

                        foreach(var item in joinresdata.Users)
                        {
                            UserList.Add(item);
                        }
                        break;
                    case ClientExitData data:
                        string exitnotify = $"{data.Nickname}님이 나가셨습니다.";
                        UserList.Remove(data.Nickname);
                        ChatMessages.Add(exitnotify);

                        if(data.Nickname == _client.Nickname)
                        {
                            _client.DisConnect();
                        }

                        if (BlackNickname == data.Nickname)
                            ResetGamerUI(PlayerType.Black);
                        else if(WhiteNickname == data.Nickname)
                            ResetGamerUI(PlayerType.White);

                        break;
                    case GameJoinData data:
                        var jointype = data.Type;
                        if (jointype == PlayerType.Black)
                            BlackNickname = data.Nickname;
                        else
                            WhiteNickname = data.Nickname;

                        if (data.Nickname == _client.Nickname)
                            MyPlayerType = data.Type;
                        break;
                    case GameLeaveData data:
                        var leavetype = data.Type;
                        if (leavetype == PlayerType.Black)
                            ResetGamerUI(PlayerType.Black);
                        else
                            ResetGamerUI(PlayerType.White);

                        if (data.Nickname == _client.Nickname)
                            MyPlayerType = PlayerType.Observer;
                        break;
                    case GameEndData data:
                        _localgame.ForceGameEnd(data.Winner, data.Reason);
                        break;
                    case GameSyncData data:
                        _localgame.SyncState(data);
                        break;
                    case GameStartData data:
                        ChatMessages.Add("게임이 시작되었습니다.");
                        _localgame.StartGame();

                        break;

                    case TimePassedData data:
                        if (data.Player == PlayerType.Black)
                            Blacktime = data.CurrentLeftTimeSeconds;
                        else
                            Whitetime = data.CurrentLeftTimeSeconds;
                        break;

                    // TODO: 기타 데이터 처리
                }
            });
        }

        [RelayCommand]
        private async Task PlaceStone(CellViewModel? cell)
        { // 보드 클릭 시
            if (cell == null)
                return;

            if (!IsGameStarted)
                return;

            if (cell.StoneState != 0) return; // 이미 놓은 곳 (클라이언트 체크)

            if (MyPlayerType != CurrentTurn) return; // 사용자 턴 아님

            var moveData = new PositionData
            {
                X = cell.X,
                Y = cell.Y,
                Player = MyPlayerType
            };

            await _client.SendData(moveData);
        }

        [RelayCommand]
        private async Task SendChat()
        {
            
            if(!string.IsNullOrEmpty(ChatInput))
            {
                var chatdata = new ChatData
                {
                    Message = ChatInput,
                    SenderNickname = _client.Nickname
                };

                await _client.SendData(chatdata);
                ChatInput = "";
            }
        }

        [RelayCommand]
        private async Task JoinGame(PlayerType type)
        {
            if (!CanJoin) return;

            var joinData = new GameJoinData
            {
                Type = type,
                Nickname = _client.Nickname
            };

            await _client.SendData(joinData);
        }

        [RelayCommand]
        private async Task LeaveGame()
        {
            if (MyPlayerType == PlayerType.Observer) return;

            var leaveData = new GameLeaveData
            {
                Type = MyPlayerType,
                Nickname = _client.Nickname
            };

            await _client.SendData(leaveData);
        }

        [RelayCommand]
        private async Task OpenConnectWindow() // 연결 창 여는 커맨드
        {
            ConnectWindow win = new ConnectWindow();

            bool? result = win.ShowDialog();

            if(result == true && win.DataContext is ConnectViewModel vm && vm.IsConfirmed)
            {
                string nick = vm.Nickname;
                string ip = vm.IpAddress;
                int port = vm.Port;
                var rule = vm.SelectedDTRule;

                if(vm.ConnectionType == ConnectionType.Server)
                {
                    _server = new GameServer();
                    await _server.StartAsync(port);
                    ChatMessages.Add("서버 생성 완료.");

                    await _client.ConnectAsync("127.0.0.1", port, nick);
                }
                else
                {
                    await _client.ConnectAsync(ip, port, nick);
                }
            }
        }

        [RelayCommand]
        private async Task StartGame() // 게임 시작 버튼 클릭
        {
            await _client.SendData(new GameStartData());
        }
    }
}
