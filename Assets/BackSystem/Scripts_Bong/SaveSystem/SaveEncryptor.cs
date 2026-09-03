using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

public static class SaveEncryptor
{
    private const string RawKeyPhrase = "OZ_07_SentryIdle_SaveKey_2026";
    private static byte[] _derivedKey;

    // AES-256 암호화 키 생성 및 반환
    private static byte[] GetKey()
    {
        if (_derivedKey != null) return _derivedKey;

        using (SHA256 sha = SHA256.Create())
        {
            _derivedKey = sha.ComputeHash(Encoding.UTF8.GetBytes(RawKeyPhrase));
        }
        return _derivedKey;
    }

    // 평문 문자열 암호화 바이트 배열 반환
    public static byte[] Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
            throw new ArgumentException("[SaveEncryptor] Encrypt: plainText가 비어있습니다.");

        byte[] key = GetKey();

        using (Aes aes = Aes.Create())
        {
            aes.KeySize = 256;
            aes.BlockSize = 128;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.Key = key;
            aes.GenerateIV();
            byte[] iv = aes.IV;

            using (ICryptoTransform encryptor = aes.CreateEncryptor())
            using (MemoryStream ms = new MemoryStream())
            {
                ms.Write(iv, 0, iv.Length);

                using (CryptoStream cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                using (StreamWriter sw = new StreamWriter(cs, Encoding.UTF8))
                {
                    sw.Write(plainText);
                }

                return ms.ToArray();
            }
        }
    }

    // 암호화 바이트 배열 복호화 문자열 반환
    public static string Decrypt(byte[] cipherData)
    {
        if (cipherData == null || cipherData.Length <= 16)
            throw new ArgumentException("[SaveEncryptor] Decrypt: 데이터가 없거나 너무 짧습니다.");

        byte[] key = GetKey();

        byte[] iv = new byte[16];
        Buffer.BlockCopy(cipherData, 0, iv, 0, 16);

        int cipherLength = cipherData.Length - 16;
        byte[] cipherBody = new byte[cipherLength];
        Buffer.BlockCopy(cipherData, 16, cipherBody, 0, cipherLength);

        using (Aes aes = Aes.Create())
        {
            aes.KeySize = 256;
            aes.BlockSize = 128;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.Key = key;
            aes.IV = iv;

            using (ICryptoTransform decryptor = aes.CreateDecryptor())
            using (MemoryStream ms = new MemoryStream(cipherBody))
            using (CryptoStream cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
            using (StreamReader sr = new StreamReader(cs, Encoding.UTF8))
            {
                return sr.ReadToEnd();
            }
        }
    }
}
