using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Gomoku.Models.Common;
using Gomoku.Models.Domain;
using Gomoku.Models.Messages;
using Gomoku.Services.Applications.Game;
using Gomoku.Services.Wpf;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Windows.Interop;

namespace Gomoku.ViewModels
{
    public partial class SessionViewModel : ViewModelBase,
        IRecipient<SessionConnectLostMessage>,
        IRecipient<GameStartMessage>,
        IRecipient<GameEndMessage>,
        IRecipient<GameJoinMessage>,
        IRecipient<GameLeftMessage>,
        IRecipient<SessionInitializedMessage>,
        IRecipient<PlayerConnectedMessage>,
        IRecipient<PlayerDisconnectedMessage>,
        IRecipient<GameSyncMessage>,
        IRecipient<GameResetMessage>,
        IRecipient<TimePassedMessage>,
        IRecipient<PlayerNicknameChangedMessage>,
        IRecipient<LastStoneCanceledMessage>,
        IRecipient<TurnChangedMessage>
    {
        private readonly IGameSessionService _gameSessionService;
        public SessionViewModel(IDispatcher dispatcher, IGameSessionService gameSessionService, IMessenger messenger) : base(dispatcher)
        {
            _gameSessionService = gameSessionService;
            messenger.RegisterAll(this);
        }

        [ObservableProperty]
        private PlayerViewModel? _me;

        [ObservableProperty]
        private PlayerViewModel? _blackPlayer;

        [ObservableProperty]
        private PlayerViewModel? _whitePlayer;

        public ObservableCollection<PlayerViewModel> UserList { get; } = [];
        private readonly Dictionary<string, PlayerViewModel> _userMap = new();

        public bool IsGameStarted => _gameSessionService.IsGameStarted;
        public bool CanShowStartButton =>
            Me?.Type == PlayerType.Black &&
            !_gameSessionService.IsGameStarted &&
            _gameSessionService.WhitePlayer != null;

        public bool IsMyTurn => _gameSessionService.IsMyTurn;
        public bool IsOpponentTurn => _gameSessionService.IsOpponentTurn;
        public bool CanCancelLast => _gameSessionService.CanCancelLast;

        private void NotifyGameStatusChanged()
        {
            OnPropertyChanged(nameof(IsGameStarted));
            OnPropertyChanged(nameof(CanShowStartButton));
            OnPropertyChanged(nameof(IsMyTurn));
            OnPropertyChanged(nameof(IsOpponentTurn));
            OnPropertyChanged(nameof(CanCancelLast));
            Me?.UpdateFromModel();
        }

        private void AddPlayer(Player model)
        {
            if (_userMap.ContainsKey(model.Nickname))
                return;
            var playerVm = new PlayerViewModel(model);
            _userMap[model.Nickname] = playerVm;
            UserList.Add(playerVm);
        }

        private void RemovePlayer(string nickname)
        {
            if (_userMap.TryGetValue(nickname, out var playerVm))
            {
                UserList.Remove(playerVm);
                _userMap.Remove(nickname);
            }
        }

        private PlayerViewModel GetPlayer(string nickname)
        {
            if(!_userMap.TryGetValue(nickname, out var playerVm))
                throw new KeyNotFoundException($"플레이어 '{nickname}'를 찾을 수 없습니다.");
            return playerVm;
        }

        private void ClearPlayers()
        {
            UserList.Clear();
            _userMap.Clear();
        }

        public void Receive(SessionConnectLostMessage msg) => ReceiveInvoke(HandleConnectLost);
        public void Receive(GameStartMessage msg) => ReceiveInvoke(HandleGameStarted);
        public void Receive(GameJoinMessage msg) => ReceiveInvoke(HandleGameJoined, msg);
        public void Receive(GameLeftMessage msg) => ReceiveInvoke(HandleGameLeft, msg);
        public void Receive(GameEndMessage msg) => ReceiveInvoke(HandleGameEnded);
        public void Receive(SessionInitializedMessage msg) => ReceiveInvoke(HandleSessionInitialized, msg);
        public void Receive(PlayerConnectedMessage msg) => ReceiveInvoke(HandlePlayerConnected, msg);
        public void Receive(PlayerDisconnectedMessage msg) => ReceiveInvoke(HandlePlayerDisconnected, msg);
        public void Receive(GameSyncMessage msg) => ReceiveInvoke(HandleGameSynced, msg);
        public void Receive(GameResetMessage msg) => ReceiveInvoke(NotifyGameStatusChanged);
        public void Receive(TimePassedMessage msg) => ReceiveInvoke(HandleTimePassed, msg);
        public void Receive(PlayerNicknameChangedMessage msg) => ReceiveInvoke(HandlePlayerNicknameChanged, msg);
        public void Receive(LastStoneCanceledMessage msg) => ReceiveInvoke(NotifyGameStatusChanged);
        public void Receive(TurnChangedMessage msg) => ReceiveInvoke(NotifyGameStatusChanged);

        private void HandlePlayerNicknameChanged(PlayerNicknameChangedMessage message)
        {
            var playerVM = GetPlayer(message.OldNickname);
            playerVM.UpdateFromModel();
            _userMap.Remove(message.OldNickname);
            _userMap[message.NewNickname] = playerVM;
        }

        private void HandleConnectLost()
        {
            BlackPlayer = null;
            WhitePlayer = null;
            Me = null;
            NotifyGameStatusChanged();
        }
        private void HandleTimePassed(TimePassedMessage msg)
        {
            if (msg.Type == PlayerType.Black)
                BlackPlayer?.RemainingTime = msg.Lefttime;
            else
                WhitePlayer?.RemainingTime = msg.Lefttime;
        }

        private void HandleGameSynced(GameSyncMessage msg)
        {
            if (msg.WhitePlayer != null)
            {
                WhitePlayer = GetPlayer(msg.WhitePlayer.Nickname);
                WhitePlayer.UpdateFromModel();
            }

            if (msg.BlackPlayer != null)
            {
                BlackPlayer = GetPlayer(msg.BlackPlayer.Nickname);
                BlackPlayer.UpdateFromModel();
            }
        }

        private void HandlePlayerDisconnected(PlayerDisconnectedMessage msg)
        {
            var playerVM = GetPlayer(msg.Player.Nickname);
            RemovePlayer(msg.Player.Nickname);

            if (BlackPlayer?.Nickname == playerVM.Nickname)
            {
                BlackPlayer = null;
            }
            else if (WhitePlayer?.Nickname == playerVM.Nickname)
            {
                WhitePlayer = null;
            }
        }

        private void HandlePlayerConnected(PlayerConnectedMessage msg)
        {
            var newplayer = msg.Player;

            if (newplayer.Nickname != Me?.Nickname)
                AddPlayer(newplayer);
        }

        private void HandleSessionInitialized(SessionInitializedMessage msg)
        {
            ClearPlayers();
            var users = msg.Players;

            foreach (var user in users)
            {
                AddPlayer(user);
            }

            Me = GetPlayer(msg.Me.Nickname);
            Me.UpdateFromModel();
        }

        private void HandleGameEnded()
        {
            BlackPlayer?.UpdateFromModel();
            WhitePlayer?.UpdateFromModel();
            NotifyGameStatusChanged();
        }

        private void HandleGameLeft(GameLeftMessage msg)
        {
            if (msg.Type == PlayerType.Black)
            {
                BlackPlayer = null;

            }
            else if (msg.Type == PlayerType.White)
            {
                WhitePlayer = null;
            }

            var playerVM = GetPlayer(msg.player.Nickname);
            playerVM.UpdateFromModel();
            NotifyGameStatusChanged();
        }

        private void HandleGameJoined(GameJoinMessage msg)
        {

            var playerVM = GetPlayer(msg.Player.Nickname);
            playerVM.UpdateFromModel();

            NotifyGameStatusChanged();

            if (msg.Type == PlayerType.Black)
                BlackPlayer = playerVM;
            else if (msg.Type == PlayerType.White)
                WhitePlayer = playerVM;
        }

        private void HandleGameStarted()
        {
            NotifyGameStatusChanged();
            if (BlackPlayer == null || WhitePlayer == null)
            {
                Logger.Error("동기화 오류 발생: 게임 시작 메시지 수신, 플레이어 정보가 없음");
                throw new InvalidOperationException("플레이어 정보가 없음");
            }

            BlackPlayer.RemainingTime = 30;
            WhitePlayer.RemainingTime = 30;
            BlackPlayer.UpdateFromModel();
            WhitePlayer.UpdateFromModel();
        }
    }
}
