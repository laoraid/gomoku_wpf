using CommunityToolkit.Mvvm.Messaging;
using Gomoku.Dialogs;
using Gomoku.Messages;
using Gomoku.ViewModels;
using System.Collections.Specialized;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

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
            this.DataContext = new MainViewModel(new DialogService(), new WindowService());

            WeakReferenceMessenger.Default.Register<DialogMessage>(this, (r, m) =>
            {
                this.Dispatcher.Invoke(() =>
                {
                    var result = MessageBox.Show(m.Title, m.Message);
                    m.Result = (result == MessageBoxResult.Yes);
                });
            });

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

            this.Loaded += async (s, e) =>
            {
                if (DataContext is MainViewModel vm)
                    await vm.OpenConnectWindowCommand.ExecuteAsync(null);

            };

        }
    }
}