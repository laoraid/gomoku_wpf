using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Gomoku.Converter
{
    public class BoolToHiddenConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool v && v)
            {
                return Visibility.Visible;
            }
            return Visibility.Hidden;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is Visibility visible && visible == Visibility.Visible;
        }
    }
}
