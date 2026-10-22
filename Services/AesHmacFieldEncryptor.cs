using System.Security.Cryptography;
using System.Text;

namespace RailwayErp.MvvmDemo.Services;

/// <summary>
/// Програмне шифрування окремих полів: AES-256-CBC + випадковий IV для кожного значення
/// + HMAC-SHA256 (encrypt-then-MAC) для перевірки цілісності.
/// Формат збереженого значення: "enc:v1:" + Base64(IV[16] | шифротекст | HMAC[32]).
/// </summary>
public class AesHmacFieldEncryptor : IFieldEncryptor
{
    private const string Prefix = "enc:v1:";
    private const int IvSize = 16;
    private const int TagSize = 32;

    private readonly IKeyProvider _keys;

    public AesHmacFieldEncryptor(IKeyProvider keys) => _keys = keys;

    public async Task<string?> EncryptAsync(string? plainText)
    {
        if (string.IsNullOrEmpty(plainText))
            return plainText;

        var key = await _keys.GetKeyAsync();
        var encKey = key[..32];
        var macKey = key[32..];

        using var aes = Aes.Create();
        aes.Key = encKey;
        aes.GenerateIV();
        var iv = aes.IV;

        var cipher = aes.EncryptCbc(Encoding.UTF8.GetBytes(plainText), iv, PaddingMode.PKCS7);

        var body = new byte[IvSize + cipher.Length];
        Buffer.BlockCopy(iv, 0, body, 0, IvSize);
        Buffer.BlockCopy(cipher, 0, body, IvSize, cipher.Length);

        var tag = HMACSHA256.HashData(macKey, body);

        var result = new byte[body.Length + TagSize];
        Buffer.BlockCopy(body, 0, result, 0, body.Length);
        Buffer.BlockCopy(tag, 0, result, body.Length, TagSize);

        return Prefix + Convert.ToBase64String(result);
    }

    public async Task<string?> DecryptAsync(string? cipherText)
    {
        // порожнє значення або старий незашифрований запис — повертаємо як є
        if (string.IsNullOrEmpty(cipherText) || !cipherText.StartsWith(Prefix, StringComparison.Ordinal))
            return cipherText;

        var all = Convert.FromBase64String(cipherText[Prefix.Length..]);
        if (all.Length < IvSize + TagSize + 16)
            throw new CryptographicException("Некоректні зашифровані дані.");

        var key = await _keys.GetKeyAsync();
        var encKey = key[..32];
        var macKey = key[32..];

        var body = all[..^TagSize];
        var tag = all[^TagSize..];

        var expected = HMACSHA256.HashData(macKey, body);
        if (!CryptographicOperations.FixedTimeEquals(tag, expected))
            throw new CryptographicException("Дані пошкоджено або змінено (HMAC не збігається).");

        var iv = body[..IvSize];
        var cipher = body[IvSize..];

        using var aes = Aes.Create();
        aes.Key = encKey;

        var plain = aes.DecryptCbc(cipher, iv, PaddingMode.PKCS7);
        return Encoding.UTF8.GetString(plain);
    }
}
