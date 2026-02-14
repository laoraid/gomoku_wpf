using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Gomoku.Converter
{
    /// <summary>
    /// true 를 Visible로, false 를 Hidden으로 변환합니다
    /// </summary>
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
