using CommunityToolkit.Mvvm.DependencyInjection;
using Gomoku.Models;
using Gomoku.Models.DTO;
using Gomoku.Services.Interfaces;
using Gomoku.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NSubstitute.ClearExtensions;

namespace UnitTest
{
    [TestClass]
    public class MainViewModelTest
    {
        IGameClient _client = Substitute.For<IGameClient>();
        SoloGameClient _soloGameClient = Substitute.ForPartsOf<SoloGameClient>();
        IGameServer _server = Substitute.For<IGameServer>();
        IMessageBoxService _MessageBoxService = Substitute.For<IMessageBoxService>();
        IWindowService _windowService = Substitute.For<IWindowService>();
        ISoundService _soundService = Substitute.For<ISoundService>();
        IDialogService _dialogService = Substitute.For<IDialogService>();
        ISnackbarService _snackbarService = Substitute.For<ISnackbarService>();
        IDispatcher _dispatcher = Substitute.For<IDispatcher>();

        private static bool _isInitialized = false;

        [TestInitialize]
        public void Setup()
        {
            if (_isInitialized)
            {
                _client.ClearSubstitute();
                _soloGameClient.ClearSubstitute();
                _server.ClearSubstitute();
                _MessageBoxService.ClearSubstitute();
                _windowService.ClearSubstitute();
                _soundService.ClearSubstitute();
                _dialogService.ClearSubstitute();
                _snackbarService.ClearSubstitute();
                return;
            }

            _isInitialized = true;

            _dispatcher.Invoke(Arg.Do<Action>(a => a()));
            _dispatcher.InvokeAsync(Arg.Do<Action>(a => a()));


            var services = new ServiceCollection();

            services.AddSingleton(_client);
            services.AddSingleton(_server);
            services.AddSingleton(_windowService);
            services.AddSingleton(_MessageBoxService);
            services.AddSingleton(_snackbarService);
            services.AddSingleton(_soundService);
            services.AddSingleton(_dialogService);
            services.AddSingleton(_snackbarService);
            services.AddSingleton(_dispatcher);
            services.AddSingleton(_soloGameClient);

            services.AddTransient<ConnectViewModel>();
            services.AddTransient<LoadingDialogViewModel>();
            services.AddTransient<InformationViewModel>();
            services.AddTransient<MainViewModel>();

            Ioc.Default.ConfigureServices(services.BuildServiceProvider());
        }

        public MainViewModel GetMainViewModel() => Ioc.Default.GetRequiredService<MainViewModel>();

        private async Task<MainViewModel> Prepare_Server()
        {
            var result = new ConnectViewModel
            {
                Nickname = "방장",
                Port = 6666,
                ConnectionType = ConnectionType.Server,
            };

            _windowService.ShowDialog(Arg.Any<ConnectViewModel>()).Returns(result);

            // 클라이언트 접속이 성공했다고 가정
            _client.ConnectAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<string>(),
                Arg.Any<CancellationToken>()).Returns(true);
            _client.IsConnected.Returns(true);

            var vm = GetMainViewModel();

            await vm.OpenConnectWindowCommand.ExecuteAsync(null);
            return vm;
        }

        /// <summary>
        /// 연결 창에서 서버로 시작 시 서버와 클라이언트를 구성해야 함
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task OpenConnectWindow_ServerMode_Should_StartServerAndClient()
        {
            await Prepare_Server();

            await _server.Received().StartAsync(6666);
            // 서버가 포트 6666으로 시작했나?

            await _client.Received().ConnectAsync("127.0.0.1", 6666, "방장", Arg.Any<CancellationToken>());
            // 클라이언트가 자기 자신에게 접속시도했나?
        }

        /// <summary>
        /// 클라이언트를 교체할때(싱글모드 - 접속모드 등)
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task Change_GameMode_Test()
        {
            var vm = await Prepare_Server();

            _MessageBoxService.CautionAsync(Arg.Any<string>(), Arg.Any<string>()).Returns(true);
            // 경고 메시지 예 눌렀다고 가정

            var result2 = new ConnectViewModel
            {
                ConnectionType = ConnectionType.Single,
            };

            _windowService.ShowDialog(Arg.Any<ConnectViewModel>()).Returns(result2);

            SoloGameClient soloGameClient = Ioc.Default.GetRequiredService<SoloGameClient>();

            await vm.OpenConnectWindowCommand.ExecuteAsync(null);

            _client.Received().Disconnect(); // 이전 클라이언트의 Disconnect가 호출되었는가?
            _server.Received().StopServer(); // 서버의 StopServer 호출되었는가

            Assert.IsFalse(_server.IsRunning);

            Assert.IsTrue(soloGameClient.IsConnected);

            var move = new GameMove(0, 0, 0, PlayerType.Black);
            await vm.PlaceStoneCommand.ExecuteAsync(vm.BoardCells[0]);

            await soloGameClient.Received().SendPlaceAsync(move);
        }

        [TestMethod]
        public async Task When_Player_Join_Test()
        {
            var meplayer = new Player("방장", PlayerType.Observer, new Record(0, 0, 0));
            var player1 = new Player("인원1", PlayerType.Observer, new Record(0, 0, 0));
            var player2 = new Player("인원2", PlayerType.Observer, new Record(0, 0, 0));
            var vm = await Prepare_Server();

            _client.ClientJoinResponseReceived += Raise.Event<Action<Player, IEnumerable<Player>>>(
                    meplayer,
                    new List<Player>() { meplayer });

            Assert.IsNotNull(vm.Me);

            _client.PlayerJoinReceived += Raise.Event<Action<Player>>(player1);

            _client.PlayerJoinReceived += Raise.Event<Action<Player>>(player2);

            Assert.HasCount(3, vm.UserList);
            // 인원 제대로 3명인지(본인포함)

            _client.GameJoinReceived += Raise.Event<Action<PlayerType, Player>>(PlayerType.Black, meplayer);
            // 본인 흑으로 참가
            _client.GameJoinReceived += Raise.Event<Action<PlayerType, Player>>(PlayerType.White, player1);
            // 플레이어1 백으로 참가

            Assert.IsNotNull(vm.BlackPlayer);
            Assert.IsNotNull(vm.WhitePlayer);

            Assert.IsTrue(vm.IsMeBlack);
            Assert.IsTrue(vm.CanShowStartButton);

            Assert.AreSame(vm.BlackPlayer, vm.Me);
            Assert.AreEqual(PlayerType.Black, vm.Me.Type);

            Assert.AreEqual(PlayerType.White, player1.Type);

            _client.GameLeaveReceived += Raise.Event<Action<PlayerType, Player>>(PlayerType.White, player1);
            // 플레이어1 백에서 나감

            Assert.IsFalse(vm.CanShowStartButton);
            Assert.IsNull(vm.WhitePlayer);
        }
    }
}
