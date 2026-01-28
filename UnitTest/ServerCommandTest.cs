using CommunityToolkit.Mvvm.Messaging;
using Gomoku.Models.DTO;
using Gomoku.Models.Interfaces;
using Gomoku.Services.Applications;
using Gomoku.Services.Interfaces;
using NSubstitute;

namespace UnitTest
{
    [TestClass]
    public class ServerCommandTest
    {
        private IMessenger _messenger = null!;
        private IPlayerTrackerService _playerTracker = null!;
        private IGameClient _gameClient = null!;
        private ServerCommandService _serverCommandService = null!;

        [TestInitialize]
        public void Setup()
        {
            _messenger = Substitute.For<IMessenger>();
            _playerTracker = new PlayerTrackerService(_messenger);
            _gameClient = Substitute.For<IGameClient>();
            _serverCommandService = new ServerCommandService(_messenger, _playerTracker);

            _serverCommandService.Receive(new ClientActivatedMessage(_gameClient));
        }

        [TestMethod]
        public async Task ExecuteCommandAsync_UnknownCommand()
        {
            var result = await _serverCommandService.ExecuteCommandAsync("/unknowncommand");
            Assert.IsFalse(result.IsSuccess);
            Assert.Contains("알 수 없는 명령어", result.Message);
        }

        [TestMethod]
        public async Task ExecuteCommandAsync_NoSlash()
        {
            var result = await _serverCommandService.ExecuteCommandAsync("changename NewName");
            Assert.IsFalse(result.IsSuccess);
            Assert.Contains("명령어는 '/'로 시작해야 합니다", result.Message);
        }
    }
}
