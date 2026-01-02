using CommunityToolkit.Mvvm.Messaging;
using Gomoku.Messages;
using Gomoku.ViewModels;
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
            this.DataContext = new MainViewModel();

            WeakReferenceMessenger.Default.Register<DialogMessage>(this, (r, m) =>
            {
                this.Dispatcher.Invoke(() => MessageBox.Show(m.Title, m.Message));
            });
        }
    }
}