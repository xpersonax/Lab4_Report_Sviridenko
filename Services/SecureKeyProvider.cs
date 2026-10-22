using System.Security.Cryptography;

namespace RailwayErp.MvvmDemo.Services;

/// <summary>
/// Ключ шифрування генерується один раз і зберігається в SecureStorage
/// (Android Keystore / iOS Keychain / Windows). Якщо SecureStorage недоступне
/// (наприклад, непакетований Windows-запуск) — запасний варіант: файл у AppDataDirectory.
/// Запасний варіант лише для навчального стенду.
/// </summary>
public class SecureKeyProvider : IKeyProvider
{
    private const string KeyName = "railway_erp_db_key";

    private readonly SemaphoreSlim _lock = new(1, 1);
    private byte[]? _key;

    public async Task<byte[]> GetKeyAsync()
    {
        if (_key is not null)
            return _key;

        await _lock.WaitAsync();
        try
        {
            if (_key is not null)
                return _key;

            string base64;
            try
            {
                var stored = await SecureStorage.Default.GetAsync(KeyName);
                if (stored is null)
                {
                    stored = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
                    await SecureStorage.Default.SetAsync(KeyName, stored);
                }

                base64 = stored;
            }
            catch (Exception)
            {
                base64 = GetOrCreateFileKey();
            }

            _key = Convert.FromBase64String(base64);
            return _key;
        }
        finally
        {
            _lock.Release();
        }
    }

    private static string GetOrCreateFileKey()
    {
        var path = Path.Combine(FileSystem.AppDataDirectory, "railway.key");
        if (File.Exists(path))
            return File.ReadAllText(path).Trim();

        var base64 = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        File.WriteAllText(path, base64);
        return base64;
    }
}
