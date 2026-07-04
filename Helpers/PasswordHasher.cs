using System.Security.Cryptography;
using System.Text;

namespace SmartSpice.Helpers;

/// <summary>
/// Salted SHA-256 password hashing. Format stored: {base64Salt}:{base64Hash}.
/// (For a production system PBKDF2/bcrypt would be preferred; this keeps the
/// prototype dependency-free while still never storing plaintext.)
/// </summary>
public static class PasswordHasher
{
    public static string Hash(string password)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(16);
        byte[] hash = Compute(password, salt);
        return $"{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hash)}";
    }

    public static bool Verify(string password, string stored)
    {
        var parts = stored.Split(':');
        if (parts.Length != 2) return false;
        try
        {
            byte[] salt = Convert.FromBase64String(parts[0]);
            byte[] expected = Convert.FromBase64String(parts[1]);
            byte[] actual = Compute(password, salt);
            return CryptographicOperations.FixedTimeEquals(expected, actual);
        }
        catch
        {
            return false;
        }
    }

    private static byte[] Compute(string password, byte[] salt)
    {
        byte[] pwd = Encoding.UTF8.GetBytes(password);
        byte[] combined = new byte[salt.Length + pwd.Length];
        Buffer.BlockCopy(salt, 0, combined, 0, salt.Length);
        Buffer.BlockCopy(pwd, 0, combined, salt.Length, pwd.Length);
        return SHA256.HashData(combined);
    }
}
