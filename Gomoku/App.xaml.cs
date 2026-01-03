using CommunityToolkit.Mvvm.DependencyInjection;
using Gomoku.Dialogs;
using Gomoku.Models;
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

            services.AddSingleton<IDialogService, DialogService>();
            services.AddSingleton<IWindowService, WindowService>();

            services.AddTransient<MainViewModel>();
            services.AddTransient<ConnectViewModel>();

            var serviceProvider = services.BuildServiceProvider();
            Ioc.Default.ConfigureServices(serviceProvider);


            var mainVM = Ioc.Default.GetRequiredService<MainViewModel>();

            var mainWindow = new MainWindow();
            mainWindow.DataContext = mainVM;

            mainWindow.Show();
        }
    }

}
