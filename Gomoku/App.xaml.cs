using CommunityToolkit.Mvvm.DependencyInjection;
using Gomoku.Models;
using Gomoku.Services.Applications;
using Gomoku.Services.Interfaces;
using Gomoku.Services.Wpf;
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

            var materialService = new MaterialDialogService();

            services.AddSingleton<IMessageBoxService>(materialService);
            services.AddSingleton<IDialogService>(materialService);
            services.AddSingleton<IWindowService, WindowService>();
            services.AddSingleton<INetworkSessionFactory, NetworkSessionFactory>();
            services.AddSingleton<ISoundService, SoundService>();
            services.AddSingleton<ISnackbarService, SnackbarService>();
            services.AddSingleton<IDispatcher, WpfDispatcher>();
            services.AddSingleton<IGameSessionService, GameSessionService>();

            services.AddSingleton<IGameClient, GameClient>();
            services.AddSingleton<IGameServer, GameServer>();
            services.AddSingleton<SoloGameClient>();

            services.AddTransient<MainViewModel>();
            services.AddTransient<ConnectViewModel>();
            services.AddTransient<InformationViewModel>();
            services.AddTransient<LoadingDialogViewModel>();
            services.AddTransient<MessageDialogViewModel>();
            services.AddTransient<BoardViewModel>();

            var serviceProvider = services.BuildServiceProvider();
            Ioc.Default.ConfigureServices(serviceProvider);


            var mainVM = Ioc.Default.GetRequiredService<MainViewModel>();

            var mainWindow = new MainWindow();
            mainWindow.DataContext = mainVM;

            mainWindow.Show();
        }
    }

}
