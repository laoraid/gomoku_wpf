/*
 * SoloGameClient.cs
 * 혼자두기 클라이언트 클래스
 * 뷰모델에선 IGameClient만 알고있기 때문에
 * 여기서 통신 부분의 작동을 변경하여 실제 통신 없이 혼자두기를 구현함
 */
using CommunityToolkit.Mvvm.Messaging;
using Gomoku.Models.Domain;
using Gomoku.Models.DTO;
using Gomoku.Models.Messages;

namespace Gomoku.Models.Network
{
    public class SoloGameClient : IGameClient
    {
        public Player? Me { get; private set; }
        public bool HasOpponent => false;

        private readonly GomokuManager _manager = new GomokuManager();

        public bool IsConnected => true;

        public event Action? ConnectionLost { add { } remove { } }

        private readonly IMessenger _messenger;

        public string MessageToken => "Solo";

        public bool IsAuthenticated => true;

        public SoloGameClient(IMessenger messenger)
        {
            _messenger = messenger;
            _manager.GameEnded += (enddata) =>
            {
                _messenger.Send(new GameEndData { EndData = enddata });
            };
        }

        private void MessengerSend(GameData data)
        {
            _messenger.Send(data, MessageToken);
        }

        public async Task<bool> ConnectAsync(string ip, int port, CancellationToken cts)
        {
            Me = new Player()
            {
                Nickname = "혼자두기",
            };
            MessengerSend(new ClientJoinResponseData { Me = Me, Users = new List<Player> { Me } });
            MessengerSend(new GameSyncData
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
            MessengerSend(new ChatData { Sender = Me!, Message = message });
            return Task.CompletedTask;
        }

        public virtual async Task SendGameStartAsync()
        {
            _manager.StartGame();
            MessengerSend(new GameStartedData { BlackPlayer = Me!, WhitePlayer = Me! });
            await Task.CompletedTask;
        }

        public virtual async Task SendJoinGameAsync(PlayerType type)
        {
            MessengerSend(new GameJoinData { Type = type, Player = Me! });
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

                _manager.TryPlaceStone(move);

                Me!.Type = nextturn;
                MessengerSend(new GameJoinData { Type = nextturn, Player = Me! });

                int stonenumber = _manager.Board.GetHistory().Count;
                var newmove = new GameMove(move.X, move.Y, stonenumber, move.PlayerType);

                MessengerSend(new PositionData { Move = newmove });

                if (_manager.IsWin(move))
                {
                    Me!.Type = PlayerType.Black;
                    MessengerSend(new GameJoinData { Type = PlayerType.Black, Player = Me! });
                }
            }
            catch
            {
                MessengerSend(new PlaceResponseData { Position = new PositionData { Move = move } });
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
            MessengerSend(new GameJoinData { Player = Me!, Type = optype });
            // 반대편으로 변경

            _manager.CancelLastStone(optype, LeftCancelCount);
            // 무르기 실행

            MessengerSend(new CancelLastData { SenderType = optype, LeftCancelLastCount = LeftCancelCount });
            await Task.CompletedTask;
        }

        public async Task SendAuthAsync(AuthInfo authInfo)
        {
            await Task.CompletedTask;
        }

        public Task SendCreateAccountAsync(string username, string password, string nickname)
        {
            throw new NotImplementedException();
        }

        public Task SendDataAsync(GameData data)
        {
            throw new NotImplementedException();
        }
    }
}
