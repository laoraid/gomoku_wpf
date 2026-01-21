using CommunityToolkit.Mvvm.Messaging;
using Gomoku.Models.DTO;
using Gomoku.Models.Interfaces;

namespace Gomoku.Models
{
    public class SoloGameClient : IGameClient
    {
        public Player? Me { get; private set; }
        public bool HasOpponent => false;

        private readonly GomokuManager _manager = new GomokuManager();

        public bool IsConnected => true;

        public event Action? ConnectionLost { add { } remove { } }

        private readonly IMessenger _messenger;

        public SoloGameClient(IMessenger messenger)
        {
            _messenger = messenger;
            _manager.GameEnded += (enddata) =>
            {
                _messenger.Send(new GameEndData { EndData = enddata });
            };
        }

        public async Task<bool> ConnectAsync(string ip, int port, string nickname, CancellationToken cts)
        {
            Me = new Player()
            {
                Nickname = nickname,
            };
            _messenger.Send(new ClientJoinResponseData { Me = Me, Users = new List<Player> { Me } });
            _messenger.Send(new GameSyncData
            {
                SyncData = new GameSyncMessage(false, new List<GameMove>(), PlayerType.Black,
                _manager.Rules.Select(r => r.RuleInfo), null, null)
            });

            await SendJoinGameAsync(PlayerType.White);
            await SendJoinGameAsync(PlayerType.Black);
            await SendGameStartAsync();
            return true;
        }

        public void Disconnect()
        {
        }

        public virtual Task SendChatAsync(string message)
        {
            _messenger.Send(new ChatData { Sender = Me!, Message = message });
            return Task.CompletedTask;
        }

        public virtual async Task SendGameStartAsync()
        {
            _manager.StartGame();
            _messenger.Send(new GameStartData());
            await Task.CompletedTask;
        }

        public virtual async Task SendJoinGameAsync(PlayerType type)
        {
            _messenger.Send(new GameJoinData { Type = type, Player = Me! });
            if (type != PlayerType.Observer)
                Me!.Type = PlayerType.Black;

            await Task.CompletedTask;
        }

        public async Task SendLeaveGameAsync()
        {
            await Task.CompletedTask;
        }
        // TODO: 메신저 로깅 필요
        public virtual async Task SendPlaceAsync(GameMove move)
        {   // 뷰모델에선 추상화된 이거 실행, 실제로는 서버에 뭐 안보내고 로컬에서 게임 돌림
            try
            {
                var nextturn = move.PlayerType == PlayerType.Black ? PlayerType.White : PlayerType.Black;

                Me!.Type = nextturn;
                _messenger.Send(new GameJoinData { Type = nextturn, Player = Me! });

                _manager.TryPlaceStone(move);
                _messenger.Send(new PositionData { Move = move });

                if (_manager.IsWin(move))
                {
                    Me!.Type = PlayerType.Black;
                    _messenger.Send(new GameJoinData { Type = PlayerType.Black, Player = Me! });
                }
            }
            catch
            {
                _messenger.Send(new PlaceResponseData { Position = new PositionData { Move = move } });
            }
            await Task.CompletedTask;
        }

        public void AddRule(Rule rule)
        {
            _manager.Rules.Add(rule);
        }

        public async Task CancelLastStoneAsync(int LeftCancelCount)
        {
            var optype = Me!.Type == PlayerType.Black ? PlayerType.White : PlayerType.Black;

            Me!.Type = optype;
            _messenger.Send(new GameJoinData { Player = Me!, Type = optype });
            // 반대편으로 변경

            _manager.CancelLastStone(optype, LeftCancelCount);
            // 무르기 실행

            _messenger.Send(new CancelLastData { SenderType = optype, LeftCancelLastCount = LeftCancelCount });
            await Task.CompletedTask;
        }
    }
}
