using Gomoku.Models;
using Gomoku.Views;
using System.Configuration;
using System.Data;
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
            MainWindow main = new();
            main.ShowDialog();
        }
    }

}
