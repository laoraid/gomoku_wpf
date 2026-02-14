/*
 * BoardViewModel.cs
 * 오목판의 뷰모델입니다.
 * 
 * BoardViewModelBase 를 상속하여 IMessenger로 들어온 메시지를 오목판에 반영하고
 * 서비스를 사용하여 착수 명령을 내립니다.
 */
using CommunityToolkit.Mvvm.Messaging;
using Gomoku.Models.Common;
using Gomoku.Models.Domain;
using Gomoku.Models.DTO;
using Gomoku.Models.Messages;
using Gomoku.Services.Applications.Game;
using Gomoku.Services.Wpf;
using Gomoku.Services.Wpf.Media;

namespace Gomoku.ViewModels
{
    public partial class BoardViewModel : BoardViewModelBase,
        IRecipient<StonePlacedMessage>,
        IRecipient<GameResetMessage>,
        IRecipient<GameEndMessage>,
        IRecipient<TurnChangedMessage>,
        IRecipient<GameSyncMessage>,
        IRecipient<LastStoneCanceledMessage>
    {
        private readonly IGameSessionService _gameSession;
        private readonly ISoundService _soundService;

        public SessionViewModel Session { get; }

        public BoardViewModel(IGameSessionService gameSession, IDispatcher dispatcher,
            ISoundService soundService, IMessenger messenger,
            SessionViewModel sessionViewModel) : base(dispatcher)
        {
            _gameSession = gameSession;
            _soundService = soundService;

            Session = sessionViewModel;

            messenger.RegisterAll(this);
        }

        private void UpdateForbiddenMarks(PlayerType obj)
        {
            if (!_gameSession.IsGameStarted) return;
            // 금수 시에 X자 업데이트
            if (Session.Me!.Type == PlayerType.Observer) return;

            var forbiddenpos = _gameSession.GetAllForbiddenPositions(obj);

            ReMarkForbidden(forbiddenpos);
        }

        public void Receive(StonePlacedMessage msg) => ReceiveInvoke(HandleStonePlaced, msg);
        public void Receive(GameResetMessage msg) => ReceiveInvoke(HandleGameReset);
        public void Receive(GameEndMessage msg) => ReceiveInvoke(HandleGameEnded, msg);
        public void Receive(TurnChangedMessage msg) => ReceiveInvoke(HandleTurnChanged, msg);
        public void Receive(GameSyncMessage msg) => ReceiveInvoke(HandleGameSynced, msg);
        public void Receive(LastStoneCanceledMessage msg) => ReceiveInvoke(HandleLastStoneCanceled, msg);

        private void HandleLastStoneCanceled(LastStoneCanceledMessage msg)
        {
            CancelLastStone();
        }

        private void HandleGameSynced(GameSyncMessage sync)
        {
            ClearBoard(); // 보드 초기화
            var moves = sync.MoveHistory.OrderBy(x => x.MoveNumber);
            SetBoard(moves);
            Logger.Info("보드 동기화 완료");
        }
        private void HandleTurnChanged(TurnChangedMessage msg)
        {
            UpdateForbiddenMarks(msg.Type);
        }
        private void HandleGameEnded(GameEndMessage data)
        {
            if (data.Stones != null)
            {   // 승리 시에 승리한 돌에 표시하기
                foreach (var move in data.Stones)
                {
                    var cell = GetCell(move.X, move.Y);
                    cell.IsWinStone = true;
                }
            }
            Logger.Info("게임 종료 수신. 보드 동기화 완료");
        }
        private void HandleGameReset()
        {   // 게임 리셋시 모든 셀 초기화
            ClearBoard();
            Logger.Info("게임 리셋 수신. 보드 초기화 완료");
        }

        private void HandleStonePlaced(StonePlacedMessage msg)
        {
            var move = msg.Move;

            Place(move);

            _soundService.Play(SoundType.StonePlace);
            Logger.Info($"착수 수신. 보드 반영 완료 {move.X} {move.Y}");
        }

        protected override async Task PlaceStone(CellViewModel? cell)
        { // 보드 클릭 시
            if (cell == null)
                return;

            if (!_gameSession.IsGameStarted)
                return;

            if (cell.StoneState != 0) return; // 이미 놓은 곳 (클라이언트 체크)

            if (Session.Me?.Type != _gameSession.CurrentTurn) return; // 사용자 턴 아님

            var move = new GameMove(cell.X, cell.Y, 0, Session.Me.Type);

            await _gameSession.PlaceStoneAsync(move);
        }
    }
}
