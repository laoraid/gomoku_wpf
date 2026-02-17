using CommunityToolkit.Mvvm.Messaging;
using Gomoku.Models.Domain;
using Gomoku.Models.Interfaces;
using Gomoku.Models.Messages;
using Gomoku.Services.Applications.Auth;
using Gomoku.Services.Applications.Command;
using Gomoku.Services.Applications.Game;
using Gomoku.Services.Wpf;
using Gomoku.Services.Wpf.Dialogs;
using Gomoku.Services.Wpf.Media;
using Gomoku.Services.Wpf.Window;
using Gomoku.ViewModels;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Text;

namespace UnitTest.ViewModels
{
    [TestClass]
    public class MainViewModelTest
    {
        public IMessageBoxService _messageBoxService = null!;
        public IWindowService _windowService = null!;
        public IDialogService _dialogService = null!;
        public ISnackbarService _snackbarService = null!;
        public IDispatcher dispatcher = null!;
        public IGameSessionService _gameSession = null!;
        public IAuthSessionService _authSession = null!;
        public IViewModelFactory _viewModelFactory = null!;
        public IMessenger messenger = null!;
        public IServerCommandService _serverCommandService = null!;
        public BoardViewModel _boardViewModel = null!;
        public ISessionViewModel _sessionViewModel = null!;

        public MainViewModel _vm = null!;

        [TestInitialize]
        public void Setup()
        {
            _messageBoxService = Substitute.For<IMessageBoxService>();
            _windowService = Substitute.For<IWindowService>();
            _dialogService = Substitute.For<IDialogService>();
            _snackbarService = Substitute.For<ISnackbarService>();
            _gameSession = Substitute.For<IGameSessionService>();
            _authSession = Substitute.For<IAuthSessionService>();
            _viewModelFactory = Substitute.For<IViewModelFactory>();
            _serverCommandService = Substitute.For<IServerCommandService>();

            dispatcher = Substitute.For<IDispatcher>();

            dispatcher.When(x => x.Invoke(Arg.Any<Action>()))
                      .Do(call => call.Arg<Action>()());
            dispatcher.InvokeAsync(Arg.Any<Action>()).Returns(c => {
                c.Arg<Action>()();
                return Task.CompletedTask;
            });

            messenger = Substitute.For<IMessenger>();
            _sessionViewModel = Substitute.For<ISessionViewModel>();
            _boardViewModel = new BoardViewModel(_gameSession, dispatcher, 
                Substitute.For<ISoundService>(), messenger, _sessionViewModel);

            _vm = new MainViewModel(_messageBoxService, _windowService,
                _dialogService, _snackbarService, dispatcher, _gameSession,
                _authSession, _viewModelFactory, messenger, _serverCommandService,
                _boardViewModel, _sessionViewModel);
        }

        [TestMethod]
        public void Receive_Chat_Test()
        {
            var p = new Player(1, "id", "닉넴", PlayerType.Observer);
            var chatmsg = new ChatReceivedMessage(p, "안녕");
            _vm.Receive(chatmsg);

            Assert.HasCount(1, _vm.ChatMessages);
            Assert.AreEqual("닉넴 : 안녕", _vm.ChatMessages[0]);
        }

        [TestMethod]
        public void Receive_Player_Connect_Test()
        {
            var p = new Player(1, "id", "닉넴", PlayerType.Observer);
            var joinmsg = new PlayerConnectedMessage(p);
            _vm.Receive(joinmsg);

            Assert.HasCount(1, _vm.ChatMessages);
            _snackbarService.Received(1).Show(Arg.Any<string>(), Arg.Any<string>());
        }
    }
}
