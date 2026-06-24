using System.Security.Cryptography;
using System.Text;

namespace BusinessLayer.Helpers;

/// <summary>
/// Security utilities — password hashing, validation.
/// </summary>
public static class SecurityHelper
{
    /// <summary>Hash a plain-text password using SHA-256</summary>
    public static string HashPassword(string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    /// <summary>Verify a plain-text password against a stored SHA-256 hash</summary>
    public static bool VerifyPassword(string password, string hash)
        => HashPassword(password).Equals(hash, StringComparison.OrdinalIgnoreCase);
}
