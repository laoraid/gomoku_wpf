using Gomoku.Models.Domain;
using System.Globalization;
using System.Windows.Data;

namespace Gomoku.Converter
{
    /// <summary>
    /// PlayerType을 표현하는 문자열로 변환합니다.
    /// 0 : 무승부, 1: 흑 승리, 2: 백 승리
    /// </summary>
    public class MatchResultConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int winnerType || value is PlayerType)
            {
                int intvalue = (int)value;

                return intvalue switch
                {
                    0 => "무승부",
                    1 => "흑 승리",
                    2 => "백 승리",
                    _ => "알 수 없음",
                };
            }
            return "알 수 없음";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
