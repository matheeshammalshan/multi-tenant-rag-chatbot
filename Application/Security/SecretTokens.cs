using System.Security.Cryptography;
using System.Text;

namespace Application.Security
{
    public static class SecretTokens
    {
        public static string Generate()
        {
            return Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
        }

        public static string Hash(string value)
        {
            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
        }

        public static bool Matches(string value, string? hash)
        {
            return hash != null && CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(Hash(value)), Encoding.UTF8.GetBytes(hash));
        }
    }
}
