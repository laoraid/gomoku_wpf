using CommunityToolkit.Mvvm.Messaging;
using Gomoku.Models.Messages;
using Gomoku.Models.Network;

namespace Gomoku.Services.Applications
{
    public class GameDataRouter : IGameDataRouter
    {
        private readonly IMessenger _messenger;
        public GameDataRouter(IMessenger messenger)
        {
            _messenger = messenger;
            _messenger.Register(this, "Network");
            // 클라이언트가 'Network' 토큰 붙여서 전송
            // 토큰은 왜? GameData 로 메시지 들어오면 받아서 또 뿌리고.. 무한루프니까
            _messenger.Register(this, "Solo");
        }

        public void Receive(GameData message)
        {
            ((dynamic)this).Handle((dynamic)message);
            // 실제 자식 타입으로 바꿔서 호출
        }

        private void Handle(ChatData data)
        {
            // 채팅은 GameSessionService를 거칠 필요 없이 바로 전송
            var sender = data.Sender;
            var chat = data.Message;

            _messenger.Send(new ChatReceivedMessage(sender, chat));
        }

        private void Handle<T>(T message) where T : GameData
        {
            // 실제 자식 클래스로 전송
            _messenger.Send<T>(message);
        }
    }
}
