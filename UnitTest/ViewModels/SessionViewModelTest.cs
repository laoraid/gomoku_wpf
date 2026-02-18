using CommunityToolkit.Mvvm.Messaging;
using Gomoku.Models.Domain;
using Gomoku.Models.DTO;
using Gomoku.Models.Messages;
using Gomoku.Services.Applications.Game;
using Gomoku.Services.Wpf;
using Gomoku.ViewModels;
using NSubstitute;
using System.ComponentModel;

namespace UnitTest.ViewModels
{
    [TestClass]
    public class SessionViewModelTest
    {
        public IDispatcher dispatcher = null!;
        public IMessenger messenger = null!;
        public IGameSessionService gameSession = null!;
        public SessionViewModel vm = null!;

        [TestInitialize]
        public void Setup()
        {
            gameSession = Substitute.For<IGameSessionService>();
            dispatcher = Substitute.For<IDispatcher>();

            dispatcher.When(x => x.Invoke(Arg.Any<Action>()))
                     .Do(call => call.Arg<Action>()());
            dispatcher.InvokeAsync(Arg.Any<Action>()).Returns(c =>
            {
                c.Arg<Action>()();
                return Task.CompletedTask;
            });

            messenger = Substitute.For<IMessenger>();

            vm = new(dispatcher, gameSession, messenger);

        }

        [TestMethod]
        public void PlayerConnect_Test()
        {
            Player me = new Player(3, "myid", "본인", PlayerType.Observer);
            vm.Receive(new SessionInitializedMessage(me, [me]));

            Assert.IsNotNull(vm.Me);
            // Me 가 초기화 되어야 함
            Assert.HasCount(1, vm.UserList);
            // 한명만 있으니 1이어야 함
            Assert.HasCount(1, vm._userMap);
            // 동일

            Player p1 = new Player(4, "id", "p1", PlayerType.Observer);
            var msg = new PlayerConnectedMessage(p1);
            vm.Receive(msg);

            var p1vm = vm.UserList.Last();

            Assert.IsNotNull(p1vm);
            Assert.AreEqual(p1.Nickname, p1vm.Nickname);
        }

        [TestMethod]
        public void PlayerDisconnect_Test()
        {
            Player me = new Player(3, "myid", "본인", PlayerType.Observer);
            vm.Receive(new SessionInitializedMessage(me, [me]));

            Player p1 = new Player(4, "id", "p1", PlayerType.Observer);
            var msg = new PlayerConnectedMessage(p1);
            vm.Receive(msg);

            var leftmsg = new PlayerDisconnectedMessage(p1);
            vm.Receive(leftmsg);

            Assert.HasCount(1, vm.UserList);
            // 나갔으니 1명만 남아야 함 
            Assert.HasCount(1, vm._userMap);
        }

        [TestMethod]
        public void GameJoin_Test()
        {
            Player me = new Player(3, "myid", "본인", PlayerType.Observer);
            vm.Receive(new SessionInitializedMessage(me, [me]));

            Player p1 = new Player(4, "id", "p1", PlayerType.Observer);
            var msg = new PlayerConnectedMessage(p1);
            vm.Receive(msg);

            var joinblackmsg = new GameJoinMessage(PlayerType.Black, me);
            me.Type = PlayerType.Black;
            vm.Receive(joinblackmsg);

            Assert.IsNotNull(vm.BlackPlayer);
            // 흑으로 참가 했으니 null 이 아니어야 함
            Assert.AreEqual(PlayerType.Black, vm.BlackPlayer.Type);
            // 뷰모델도 흑으로 바뀌어야 함

            var joinwhitemsg = new GameJoinMessage(PlayerType.White, p1);
            p1.Type = PlayerType.White;
            vm.Receive(joinwhitemsg);

            Assert.IsNotNull(vm.WhitePlayer);
            // 백으로 참가 했으니 null 이 아니어야 함
            Assert.AreEqual(PlayerType.White, vm.WhitePlayer.Type);
            // 뷰모델도 바뀌어야 함
        }

        [TestMethod]
        public void GameStart_Test()
        {
            var handler = Substitute.For<PropertyChangedEventHandler>();
            // 뷰모델 값 변경 이벤트 감지 핸들러
            vm.PropertyChanged += handler;

            Player me = new Player(3, "myid", "본인", PlayerType.Observer);
            vm.Receive(new SessionInitializedMessage(me, [me]));

            Player p1 = new Player(4, "id", "p1", PlayerType.Observer);
            var msg = new PlayerConnectedMessage(p1);
            vm.Receive(msg);

            var joinblackmsg = new GameJoinMessage(PlayerType.Black, me);
            me.Type = PlayerType.Black;
            vm.Receive(joinblackmsg);

            var joinwhitemsg = new GameJoinMessage(PlayerType.White, p1);
            p1.Type = PlayerType.White;
            vm.Receive(joinwhitemsg);

            var gamestartmsg = new GameStartMessage(false, null, null);

            gameSession.IsGameStarted.Returns(true);
            gameSession.BlackPlayer.Returns(me);
            gameSession.WhitePlayer.Returns(p1);
            gameSession.IsMyTurn.Returns(true);
            gameSession.IsOpponentTurn.Returns(false);

            vm.Receive(gamestartmsg);

            handler.Received()(vm, Arg.Is<PropertyChangedEventArgs>(e =>
                e.PropertyName == nameof(vm.IsGameStarted)));
            handler.Received()(vm, Arg.Is<PropertyChangedEventArgs>(e =>
                e.PropertyName == nameof(vm.CanShowStartButton)));
            handler.Received()(vm, Arg.Is<PropertyChangedEventArgs>(e =>
                e.PropertyName == nameof(vm.IsMyTurn)));
            handler.Received()(vm, Arg.Is<PropertyChangedEventArgs>(e =>
                e.PropertyName == nameof(vm.IsOpponentTurn)));

            Assert.IsTrue(vm.IsGameStarted);
            Assert.IsTrue(vm.IsMyTurn);
            Assert.IsFalse(vm.IsOpponentTurn);
        }

        [TestMethod]
        public void GameLeft_Test()
        {
            Player me = new Player(3, "myid", "본인", PlayerType.Observer);
            vm.Receive(new SessionInitializedMessage(me, [me]));

            Player p1 = new Player(4, "id", "p1", PlayerType.Observer);
            var msg = new PlayerConnectedMessage(p1);
            vm.Receive(msg);

            var joinblackmsg = new GameJoinMessage(PlayerType.Black, me);
            me.Type = PlayerType.Black;
            vm.Receive(joinblackmsg);

            var joinwhitemsg = new GameJoinMessage(PlayerType.White, p1);
            p1.Type = PlayerType.White;
            vm.Receive(joinwhitemsg);

            var leftmsg = new GameLeftMessage(PlayerType.White, p1);
            vm.Receive(leftmsg);

            Assert.IsNull(vm.WhitePlayer);
            // 백 플레이어 나갔으니 null 이어야 함
        }

        [TestMethod]
        public void GameSync_Test()
        {
            Player me = new Player(3, "myid", "본인", PlayerType.Observer);
            Player p1 = new Player(4, "id", "p1", PlayerType.Black);
            Player p2 = new Player(5, "p2id", "p2", PlayerType.White);

            vm.Receive(new SessionInitializedMessage(me, [p1, p2, me]));

            var syncmsg = new GameSyncMessage(true, Enumerable.Empty<GameMove>(),
                PlayerType.Black, Enumerable.Empty<RuleInfo>(),
                p1, p2);
            vm.Receive(syncmsg);

            Assert.IsNotNull(vm.WhitePlayer);
            Assert.IsNotNull(vm.BlackPlayer);
            // 게임 진행 중 시나리오이므로 흑 백 플레이어 있어야 함
        }
    }
}
