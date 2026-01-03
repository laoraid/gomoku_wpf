using System.Windows;
using System.Windows.Controls;

namespace Gomoku.Controls
{
    /// <summary>
    /// NumericTextBox.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class NumericTextBox : UserControl
    {
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(
                "Value",
                typeof(int),
                typeof(NumericTextBox),
                new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault)
            );

        public int Value
        {
            get => (int)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        public int Minimum { get; set; } = 0;
        public int Maximum { get; set; } = 65535;
        public NumericTextBox()
        {
            InitializeComponent();
        }

        private void Up_Click(object sender, RoutedEventArgs e)
        {
            if (Value < Maximum)
                Value++;
        }

        private void Down_Click(object sender, RoutedEventArgs e)
        {
            if (Value > Minimum)
                Value--;
        }
    }
}
