using System.Security.Cryptography;
using System.Text;

namespace Gomoku.Helpers
{
    /// <summary>
    /// 문자열을 SHA256으로 해시하는 클래스
    /// </summary>
    public static class HashHelper
    {
        public static string SHA256Hash(string str)
        {
            string salt = "12312412593963043046";
            byte[] bytearray = Encoding.Default.GetBytes(str + salt);
            byte[] hashed;

            using (var sha = SHA256.Create())
            {
                hashed = sha.ComputeHash(bytearray);
            }

            return Convert.ToBase64String(hashed);
        }
    }
}
