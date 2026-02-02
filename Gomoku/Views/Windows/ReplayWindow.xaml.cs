using Gomoku.ViewModels.Replay;
using System.Windows;

namespace Gomoku.Views.Windows
{
    /// <summary>
    /// ReplayWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class ReplayWindow : Window
    {
        public ReplayWindow()
        {
            InitializeComponent();

            this.Loaded += ReplayWindow_Loaded;
        }

        private async void ReplayWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is ReplayViewModel vm)
            {
                await vm.LoadMatchMovesAsync();
            }
        }
    }
}
