using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows.Controls;

namespace Gomoku.Helpers
{
    public class IpAddressValidationRule : ValidationRule
    {
        public static bool IsValid(string? ip)
        {
            if (string.IsNullOrWhiteSpace(ip)) return false;
            if (ip.ToLower() == "localhost") return true;

            Regex ipRegex = new Regex(@"^(25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)(\.(25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)){3}$");

            return ipRegex.IsMatch(ip);
        }
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            string ip = value as string ?? string.Empty;

            if (IsValid(ip))
                return ValidationResult.ValidResult;
            return new ValidationResult(false, "올바른 IP 주소가 아닙니다.");
        }
    }

    public class NotEmptyValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            if (!string.IsNullOrWhiteSpace(value as string ?? string.Empty))
                return ValidationResult.ValidResult;
            return new ValidationResult(false, "닉네임은 공백이 아니어야 합니다.");
        }
    }
}
