using System.Security.Cryptography;

namespace GestionEspaces.Infrastructure.Security;

/// <summary>
/// PBKDF2-SHA256 password hashing shared by login verification, self-service password
/// changes, and Admin-created accounts. Format: Base64( salt[16] || hash[32] ).
/// </summary>
public static class PasswordHasher
{
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int Iterations = 10_000;

    public static string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, HashSize);
        return Convert.ToBase64String([.. salt, .. hash]);
    }

    public static bool Verify(string password, string storedHash)
    {
        try
        {
            var hashBytes = Convert.FromBase64String(storedHash);
            if (hashBytes.Length != SaltSize + HashSize) return false;
            var salt = hashBytes[..SaltSize];
            var expected = hashBytes[SaltSize..];
            var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, HashSize);
            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }
        catch
        {
            return false;
        }
    }
}
