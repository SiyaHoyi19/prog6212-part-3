using System.Security.Cryptography;

namespace RaceDay.Api.Services;

public static class PasswordService
{
    private const int Iterations = 210_000;
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const string CurrentPrefix = "pbkdf2-sha256";
    private const string LegacyPrefix = "TKFY4-sha256";

    public static string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, HashSize);
        return CurrentPrefix + "$" + Iterations + "$" +
            Convert.ToBase64String(salt) + "$" + Convert.ToBase64String(hash);
    }

    public static bool Verify(string password, string stored)
    {
        try
        {
            var parts = stored.Split('$');
            if (parts.Length != 4 ||
                (parts[0] != CurrentPrefix && parts[0] != LegacyPrefix) ||
                !int.TryParse(parts[1], out var iterations) ||
                iterations < 10_000 || iterations > 2_000_000)
                return false;

            var salt = Convert.FromBase64String(parts[2]);
            var expectedHash = Convert.FromBase64String(parts[3]);
            if (salt.Length != SaltSize || expectedHash.Length != HashSize)
                return false;

            var actualHash = Rfc2898DeriveBytes.Pbkdf2(
                password, salt, iterations, HashAlgorithmName.SHA256, expectedHash.Length);
            return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
        }
        catch (FormatException) { return false; }
        catch (ArgumentException) { return false; }
    }
}
