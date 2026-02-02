using System.Windows;
using System.Windows.Controls;

namespace Gomoku.Views.Controls
{
    /// <summary>
    /// Board.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class Board : UserControl
    {
        public Board()
        {
            InitializeComponent();
        }



        public Style CellStyle
        {
            get { return (Style)GetValue(CellStyleProperty); }
            set { SetValue(CellStyleProperty, value); }
        }

        public static readonly DependencyProperty CellStyleProperty =
            DependencyProperty.Register(nameof(CellStyle), typeof(Style), typeof(Board), new PropertyMetadata(null));


    }
}
