namespace RailwayErp.MvvmDemo.Services;

public interface IFieldEncryptor
{
    Task<string?> EncryptAsync(string? plainText);
    Task<string?> DecryptAsync(string? cipherText);
}

public interface IKeyProvider
{
    /// <summary>64 байти: перші 32 — ключ AES-256, останні 32 — ключ HMAC-SHA256.</summary>
    Task<byte[]> GetKeyAsync();
}
