using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace Gomoku.ViewModel
{
    public class MainViewModel : ViewModelBase
    {
        private GameClient _client; // 서버도 하나의 클라이언트로 자기 자신에게 접속
        private GameServer _server; // 서버일 경우만 생성

        private GomokuManager _localgame; // 클라이언트 전용

        private PlayerType _myPlayerType = PlayerType.Observer;
        public PlayerType MyPlayerType
        {
            get => _myPlayerType;
            set => SetProperty(ref _myPlayerType, value);
        }

        private PlayerType _currentTurn = PlayerType.Black;
        public PlayerType CurrentTurn
        {
            get => _currentTurn;
            set => SetProperty(ref _currentTurn, value);
        }

        public ObservableCollection<CellViewModel> BoardCells { get; } = new ObservableCollection<CellViewModel>();
        // 격자 버튼 (15x15) 누르면 돌 보임
        public ObservableCollection<string> UserList { get; } = new ObservableCollection<string>();
        // 참가자 리스트
        public ObservableCollection<string> ChatMessages { get; } = new ObservableCollection<string>();
        // 채팅

        private string _blackNickname = "흑돌 대기 중...";
        public string BlackNickname
        {
            get => _blackNickname;
            set => SetProperty(ref _blackNickname, value);
        }

        private string _WhiteNickname = "백돌 대기 중...";
        public string WhiteNickname
        {
            get => _WhiteNickname;
            set => SetProperty(ref _WhiteNickname, value);
        }

        private string _chatInput = string.Empty;
        public string ChatInput
        {
            get => _chatInput;
            set => SetProperty(ref _chatInput, value);
        }

        public ICommand PlaceStoneCommand { get; }
        public ICommand SendChatCommand { get; }

        public MainViewModel()
        {
            _client = new GameClient();
            _localgame = new GomokuManager();

            _localgame.OnStonePlaced += SyncStoneUI; // 돌 놓았을때 UI 반영
            _localgame.OnTurnChanged += (player) => CurrentTurn = player;
            _localgame.OnGameEnded += GameEnd; // 게임 종료

            for (int y = 0; y < 15; y++)
            { 
                for (int x = 0; x < 15; x++)
                    BoardCells.Add(new CellViewModel(x, y)); // 돌 생성
            }

            PlaceStoneCommand = new RelayCommand<CellViewModel>(OnPlaceStone);
            SendChatCommand = new RelayCommand(OnSendChat);

            _client.OnDataReceived += HandleClientDataReceived;
        }

        private void GameEnd(PlayerType winner)
        {
            string result;
            if (winner == PlayerType.Observer)
                result = "경기 종료. 비겼습니다.";
            else if (winner == PlayerType.Black)
                result = $"경기 종료. 흑돌 {BlackNickname} 승리!";
            else
                result = $"경기 종료. 백돌 {WhiteNickname} 승리!";

            ChatMessages.Add(result);
        }

        private void SyncStoneUI(PositionData data)
        {
            int index = data.Y * 15 + data.X; // 2차원 격자 주소를 1차원 ItemsControl 주소로 바꾸기
            BoardCells[index].StoneState = (int)data.Player;
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
                    case ChatData chat:
                        ChatMessages.Add($"{chat.SenderNickname} : {chat.Message}");
                        break;
                    case ClientJoinData data:
                        string joinnotify = $"{data.Nickname}님이 참가하였습니다.";
                        ChatMessages.Add(joinnotify);
                        break;
                    case ClientExitData data:
                        string exitnotify = $"{data.Nickname}님이 나가셨습니다.";
                        ChatMessages.Add(exitnotify);
                        break;
                    case GameJoinData data:
                        var jointype = data.Type;
                        if (jointype == PlayerType.Black)
                            BlackNickname = data.Nickname;
                        else
                            WhiteNickname = data.Nickname;
                        break;
                    case GameLeaveData data:
                        var leavetype = data.Type;
                        if (leavetype == PlayerType.Black)
                            BlackNickname = "흑돌 대기 중...";
                        else
                            WhiteNickname = "백돌 대기 중...";
                        break;
                    case GameEndData data:
                        GameEnd(data.Winner);
                        break;
                    case GameSyncData data:
                        _localgame.ResetGame();
                        foreach (var place in data.MoveHistory)
                        {
                            _localgame.TryPlaceStone(place);
                        }

                        CurrentTurn = data.CurrentTurn;
                        break;


                    // TODO: 기타 데이터 처리
                }
            });
        }

        private async void OnPlaceStone(CellViewModel? cell)
        { // 보드 클릭 시
            if (cell == null)
                return;

            if (cell.StoneState != 0) return;

            if (MyPlayerType != CurrentTurn) return;

            var moveData = new PositionData
            {
                X = cell.X,
                Y = cell.Y,
                Player = MyPlayerType
            };

            await _client.SendData(moveData);
        }

        private async void OnSendChat()
        {
            if(!string.IsNullOrEmpty(ChatInput))
            {
                var chatdata = new ChatData
                {
                    Message = ChatInput,
                    SenderNickname = _client.Nickname
                };

                await _client.SendData(chatdata);
            }
        }
    }
}
