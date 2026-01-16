using CommunityToolkit.Mvvm.DependencyInjection;
using Gomoku.Models;
using Gomoku.Models.DTO;
using Gomoku.Services.Interfaces;
using Gomoku.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

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

        [TestInitialize]
        public void Setup()
        {
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

        /// <summary>
        /// 연결 창에서 서버로 시작 시 서버와 클라이언트를 구성해야 함
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task OpenConnectWindow_ServerMode_Should_StartServerAndClient()
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

            var vm = GetMainViewModel();

            await vm.OpenConnectWindowCommand.ExecuteAsync(null);

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
    }
}
