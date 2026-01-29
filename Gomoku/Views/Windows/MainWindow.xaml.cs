using Gomoku.ViewModels;
using System.Collections.Specialized;
using System.Windows;

namespace Gomoku.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            this.DataContextChanged += (s, e) =>
            {
                if (DataContext is MainViewModel vm)
                {
                    ((INotifyCollectionChanged)vm.ChatMessages).CollectionChanged += (s, e) =>
                    { // 채팅창 자동 스크롤
                        if (e.Action == NotifyCollectionChangedAction.Add)
                        {
                            Dispatcher.BeginInvoke(new Action(() =>
                            {
                                if (ChatListBox.Items.Count > 0)
                                    ChatListBox.ScrollIntoView(ChatListBox.Items[ChatListBox.Items.Count - 1]);
                            }));
                        }
                    };
                }
            };

            this.Loaded += async (s, e) =>
            {
                if (DataContext is MainViewModel vm)
                    await vm.OpenConnectWindowCommand.ExecuteAsync(null);

            };

        }
    }
}