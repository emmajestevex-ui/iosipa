using System.Security.Cryptography;

namespace RemoControlMobile;

public static class LockSecurityService
{
    private const string PasswordHashKey = "app_lock_password_hash";
    private const string PasswordSaltKey = "app_lock_password_salt";
    private const string PasswordConfiguredKey = "app_lock_password_configured";
    private const string BiometricsKey = "app_lock_biometrics";

    public static bool IsPasswordConfigured =>
        Preferences.Default.Get(PasswordConfiguredKey, false);

    public static bool BiometricsEnabled
    {
        get => Preferences.Default.Get(BiometricsKey, false);
        set => Preferences.Default.Set(BiometricsKey, value);
    }

    public static async Task SetPasswordAsync(string password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 4)
            throw new ArgumentException("La contraseña debe tener al menos 4 caracteres.");

        byte[] salt = RandomNumberGenerator.GetBytes(16);
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            100_000,
            HashAlgorithmName.SHA256,
            32);

        await SecureStorage.Default.SetAsync(PasswordSaltKey, Convert.ToBase64String(salt));
        await SecureStorage.Default.SetAsync(PasswordHashKey, Convert.ToBase64String(hash));
        Preferences.Default.Set(PasswordConfiguredKey, true);
    }

    public static async Task<bool> VerifyPasswordAsync(string password)
    {
        try
        {
            string? saltText = await SecureStorage.Default.GetAsync(PasswordSaltKey);
            string? hashText = await SecureStorage.Default.GetAsync(PasswordHashKey);

            if (string.IsNullOrWhiteSpace(saltText) || string.IsNullOrWhiteSpace(hashText))
                return false;

            byte[] salt = Convert.FromBase64String(saltText);
            byte[] expected = Convert.FromBase64String(hashText);
            byte[] actual = Rfc2898DeriveBytes.Pbkdf2(
                password ?? string.Empty,
                salt,
                100_000,
                HashAlgorithmName.SHA256,
                expected.Length);

            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }
        catch
        {
            return false;
        }
    }

    public static void DisableLock()
    {
        AppConfig.BloqueoApp = false;
        BiometricsEnabled = false;
    }
}
