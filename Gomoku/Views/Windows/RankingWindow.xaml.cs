using Gomoku.ViewModels;
using System.Windows;

namespace Gomoku.Views
{
    /// <summary>
    /// RankingWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class RankingWindow : Window
    {
        public RankingWindow()
        {
            InitializeComponent();

            this.Loaded += RankingWindow_Loaded;
        }

        private async void RankingWindow_Loaded(object sender, RoutedEventArgs e)
        {
            this.Loaded -= RankingWindow_Loaded;

            if (DataContext is RankingViewModel vm)
            {
                await vm.LoadRankingsAsync();
            }
        }
    }
}
