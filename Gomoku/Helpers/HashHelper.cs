using System.Security.Cryptography;
using System.Text;

namespace Gomoku.Helpers
{
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
