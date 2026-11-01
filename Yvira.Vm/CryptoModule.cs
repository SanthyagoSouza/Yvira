using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace Yvira.Vm;

public static class CryptoModule
{
    public static YviraMap CreateModule()
    {
        var methods = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        // 1. crypto.sha256(text) -> hex
        methods["sha256"] = new HostFunction("crypto.sha256", (interp, args) =>
        {
            if (args.Count == 0 || args[0] == null)
                throw new Exception("crypto.sha256 requer o texto para calcular o hash.");
            byte[] input = Encoding.UTF8.GetBytes(args[0]!.ToString()!);
            byte[] hash = SHA256.HashData(input);
            return Convert.ToHexString(hash).ToLowerInvariant();
        });

        // 2. crypto.sha512(text) -> hex
        methods["sha512"] = new HostFunction("crypto.sha512", (interp, args) =>
        {
            if (args.Count == 0 || args[0] == null)
                throw new Exception("crypto.sha512 requer o texto para calcular o hash.");
            byte[] input = Encoding.UTF8.GetBytes(args[0]!.ToString()!);
            byte[] hash = SHA512.HashData(input);
            return Convert.ToHexString(hash).ToLowerInvariant();
        });

        // 3. crypto.hmac(segredo, mensagem, algoritmo?) -> hex
        methods["hmac"] = new HostFunction("crypto.hmac", (interp, args) =>
        {
            if (args.Count < 2 || args[0] == null || args[1] == null)
                throw new Exception("crypto.hmac requer (segredo, mensagem).");

            byte[] key = Encoding.UTF8.GetBytes(args[0]!.ToString()!);
            byte[] data = Encoding.UTF8.GetBytes(args[1]!.ToString()!);
            byte[] hash = HMACSHA256.HashData(key, data);
            return Convert.ToHexString(hash).ToLowerInvariant();
        });

        // 4. crypto.randomBytes(tamanho) -> hex
        methods["randomBytes"] = new HostFunction("crypto.randomBytes", (interp, args) =>
        {
            int count = args.Count > 0 ? Convert.ToInt32(args[0]) : 32;
            if (count <= 0 || count > 4096)
                throw new Exception("Tamanho de bytes aleatórios deve estar entre 1 e 4096.");
            byte[] bytes = new byte[count];
            RandomNumberGenerator.Fill(bytes);
            return Convert.ToHexString(bytes).ToLowerInvariant();
        });

        // 5. crypto.uuid() -> string UUID v4 seguro
        methods["uuid"] = new HostFunction("crypto.uuid", (interp, args) =>
        {
            return Guid.NewGuid().ToString("D");
        });

        // 6. crypto.hashPassword(senha) -> string segura com Salt + PBKDF2
        methods["hashPassword"] = new HostFunction("crypto.hashPassword", (interp, args) =>
        {
            if (args.Count == 0 || args[0] == null)
                throw new Exception("crypto.hashPassword requer a senha.");

            string password = args[0]!.ToString()!;
            byte[] salt = new byte[16];
            RandomNumberGenerator.Fill(salt);

            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
                Encoding.UTF8.GetBytes(password),
                salt,
                iterations: 100_000,
                HashAlgorithmName.SHA256,
                outputLength: 32
            );

            // Formato blindado: $pbkdf2$100000$saltHex$hashHex
            return $"$pbkdf2$100000${Convert.ToHexString(salt).ToLowerInvariant()}${Convert.ToHexString(hash).ToLowerInvariant()}";
        });

        // 7. crypto.verifyPassword(senha, hashSalvo) -> bool
        methods["verifyPassword"] = new HostFunction("crypto.verifyPassword", (interp, args) =>
        {
            if (args.Count < 2 || args[0] == null || args[1] == null)
                throw new Exception("crypto.verifyPassword requer (senha, hashSalvo).");

            string password = args[0]!.ToString()!;
            string saved = args[1]!.ToString()!;

            var parts = saved.Split('$');
            if (parts.Length != 5 || parts[1] != "pbkdf2")
                return false;

            if (!int.TryParse(parts[2], out int iterations))
                return false;

            byte[] salt = Convert.FromHexString(parts[3]);
            byte[] expectedHash = Convert.FromHexString(parts[4]);

            byte[] computedHash = Rfc2898DeriveBytes.Pbkdf2(
                Encoding.UTF8.GetBytes(password),
                salt,
                iterations,
                HashAlgorithmName.SHA256,
                outputLength: expectedHash.Length
            );

            // Comparação de tempo constante (Prevenção de Timing Attack)
            return CryptographicOperations.FixedTimeEquals(computedHash, expectedHash);
        });

        // 8. crypto.encrypt(texto, chave) -> envelope seguro AES-256-GCM / CBC
        methods["encrypt"] = new HostFunction("crypto.encrypt", (interp, args) =>
        {
            if (args.Count < 2 || args[0] == null || args[1] == null)
                throw new Exception("crypto.encrypt requer (texto, chave).");

            string plaintext = args[0]!.ToString()!;
            string pass = args[1]!.ToString()!;

            byte[] salt = new byte[16];
            RandomNumberGenerator.Fill(salt);

            byte[] key = Rfc2898DeriveBytes.Pbkdf2(
                Encoding.UTF8.GetBytes(pass),
                salt,
                iterations: 10_000,
                HashAlgorithmName.SHA256,
                outputLength: 32
            );

            using var aes = Aes.Create();
            aes.Key = key;
            aes.GenerateIV();
            byte[] iv = aes.IV;

            using var ms = new System.IO.MemoryStream();
            ms.Write(salt, 0, salt.Length);
            ms.Write(iv, 0, iv.Length);

            using (var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
            using (var sw = new System.IO.StreamWriter(cs, Encoding.UTF8))
            {
                sw.Write(plaintext);
            }

            return Convert.ToBase64String(ms.ToArray());
        });

        // 9. crypto.decrypt(envelopeBase64, chave) -> texto original
        methods["decrypt"] = new HostFunction("crypto.decrypt", (interp, args) =>
        {
            if (args.Count < 2 || args[0] == null || args[1] == null)
                throw new Exception("crypto.decrypt requer (envelopeBase64, chave).");

            string base64 = args[0]!.ToString()!;
            string pass = args[1]!.ToString()!;

            byte[] fullCipher = Convert.FromBase64String(base64);
            if (fullCipher.Length < 32)
                throw new Exception("Envelope cifrado corrompido ou inválido.");

            byte[] salt = new byte[16];
            Buffer.BlockCopy(fullCipher, 0, salt, 0, 16);

            byte[] iv = new byte[16];
            Buffer.BlockCopy(fullCipher, 16, iv, 0, 16);

            int cipherTextLen = fullCipher.Length - 32;
            byte[] cipherText = new byte[cipherTextLen];
            Buffer.BlockCopy(fullCipher, 32, cipherText, 0, cipherTextLen);

            byte[] key = Rfc2898DeriveBytes.Pbkdf2(
                Encoding.UTF8.GetBytes(pass),
                salt,
                iterations: 10_000,
                HashAlgorithmName.SHA256,
                outputLength: 32
            );

            using var aes = Aes.Create();
            aes.Key = key;
            aes.IV = iv;

            using var ms = new System.IO.MemoryStream(cipherText);
            using var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read);
            using var sr = new System.IO.StreamReader(cs, Encoding.UTF8);

            return sr.ReadToEnd();
        });

        return new YviraMap(methods);
    }
}
