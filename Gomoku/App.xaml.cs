using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using Gomoku.Models;
using Gomoku.Models.Common;
using Gomoku.Models.Interfaces;
using Gomoku.Models.Network;
using Gomoku.Services.Applications;
using Gomoku.Services.Applications.Auth;
using Gomoku.Services.Applications.Command;
using Gomoku.Services.Applications.Database;
using Gomoku.Services.Applications.Game;
using Gomoku.Services.Wpf;
using Gomoku.Services.Wpf.Dialogs;
using Gomoku.Services.Wpf.Media;
using Gomoku.Services.Wpf.Window;
using Gomoku.ViewModels;
using Gomoku.Views;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using System.Windows;

namespace Gomoku
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            Logger.OnLogReceived += (msg, type) =>
            {
                // 콘솔에 로그 출력
                Debug.WriteLine($"[{type}] {msg}");
            };

            var services = new ServiceCollection(); // DI 컨테이너 생성

            services.AddSingleton<MaterialDialogService>();
            services.AddSingleton<IMessageBoxService>(sp => sp.GetRequiredService<MaterialDialogService>());
            services.AddSingleton<IDialogService>(sp => sp.GetRequiredService<MaterialDialogService>());
            services.AddSingleton<IWindowService, WindowService>();
            services.AddSingleton<INetworkSessionFactory, NetworkSessionFactory>();
            services.AddSingleton<ISoundService, SoundService>();
            services.AddSingleton<ISnackbarService, SnackbarService>();
            services.AddSingleton<IDispatcher, WpfDispatcher>();
            services.AddSingleton<IGameSessionService, GameSessionService>();
            services.AddSingleton<IAuthSessionService, AuthSessionService>();
            services.AddSingleton<IDatabaseService, DatabaseService>();
            services.AddSingleton<IPlayerTrackerService, PlayerTrackerService>();
            services.AddSingleton<IServerCommandService, ServerCommandService>();

            services.AddSingleton<IGameClientFactory, GameClientFactory>();
            services.AddSingleton<IViewModelFactory, ViewModelFactory>();
            services.AddSingleton<Func<IGameServer>>(sp => () => sp.GetRequiredService<IGameServer>());

            services.AddSingleton<IMessenger>(WeakReferenceMessenger.Default);

            services.AddSingleton<IGameDataRouter, GameDataRouter>();

            services.AddSingleton<IGameClient, GameClient>();
            services.AddTransient<IGameServer, GameServer>();
            services.AddSingleton<SoloGameClient>();

            services.AddTransient<MainViewModel>();
            services.AddTransient<ConnectViewModel>();
            services.AddTransient<InformationViewModel>();
            services.AddTransient<LoadingDialogViewModel>();
            services.AddTransient<MessageDialogViewModel>();
            services.AddTransient<LoginDialogViewModel>();
            services.AddTransient<BoardViewModel>();
            services.AddTransient<RankingViewModel>();

            services.AddSingleton<SessionViewModel>();

            var serviceProvider = services.BuildServiceProvider();
            Ioc.Default.ConfigureServices(serviceProvider);

            var _ = Ioc.Default.GetRequiredService<IGameDataRouter>();
            // 라우터 작동을 위한 인스턴스 생성

            var mainVM = Ioc.Default.GetRequiredService<MainViewModel>();

            var mainWindow = new MainWindow();
            mainWindow.DataContext = mainVM;

            mainWindow.Show();
        }
    }

}
