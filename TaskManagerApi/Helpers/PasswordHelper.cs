using System.Security.Cryptography;
using System.Text;

namespace TaskManagerApi.Helpers
{
    public static class PasswordHelper
    {
        public static string HashPassword(string password)
        {
            return HashValue(password);
        }

        public static string HashValue(string value)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(value));
            return Convert.ToBase64String(bytes);
        }
    }
}
