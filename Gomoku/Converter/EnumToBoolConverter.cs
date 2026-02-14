using System.Globalization;
using System.Windows.Data;

namespace Gomoku.Converter
{
    /// <summary>
    /// 라디오버튼 바인딩에서 Enum을 bool로, bool을 Enum으로 변환합니다.
    /// </summary>
    public class EnumToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value?.ToString() == parameter?.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isChecked && isChecked)
                return Enum.Parse(targetType, parameter.ToString());
            return Binding.DoNothing;
        }
    }
}
