using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using BCryptNet = BCrypt.Net.BCrypt;

namespace IctBaden.Stonehenge.BasicAuth;

/// <summary>
/// Verify and create hashes used in .htpasswd files.
/// </summary>
internal static class HtpasswdHash
{
    internal const string Itoa64 = "./0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";

    public static bool Verify(string password, string stored)
    {
        try
        {
            if (stored.StartsWith("{SHA}", StringComparison.Ordinal))
            {
                return HashEquals(HashSha1(password), stored);
            }

            if (stored.StartsWith("{SSHA}", StringComparison.Ordinal))
            {
                return VerifySsha(password, stored);
            }

            if (stored.StartsWith("$apr1$", StringComparison.Ordinal))
            {
                return HashSuffixEquals(Md5Crypt(password, stored, "$apr1$"), stored);
            }

            if (stored.StartsWith("$1$", StringComparison.Ordinal))
            {
                return HashSuffixEquals(Md5Crypt(password, stored, "$1$"), stored);
            }

            if (stored.StartsWith("$2a$", StringComparison.Ordinal) ||
                stored.StartsWith("$2b$", StringComparison.Ordinal) ||
                stored.StartsWith("$2x$", StringComparison.Ordinal) ||
                stored.StartsWith("$2y$", StringComparison.Ordinal))
            {
                return BCryptNet.Verify(password, stored);
            }

            if (stored.StartsWith("$7$", StringComparison.Ordinal))
            {
                return VerifyMosquittoPbkdf2(password, stored);
            }

            if (stored.StartsWith("$5$", StringComparison.Ordinal) ||
                stored.StartsWith("$6$", StringComparison.Ordinal))
            {
                return VerifyShaFamily(password, stored);
            }

            if (stored.Length == 13 && IsCryptAlphabet(stored))
            {
                return HashEquals(UnixDesCrypt.Crypt(password, stored[..2]), stored);
            }

            return string.Equals(password, stored, StringComparison.Ordinal);
        }
        catch (Exception ex)
        {
            Trace.TraceError($"Passwords: Failed to verify hash: {ex.Message}");
            return false;
        }
    }

    public static string Hash(string password, PasswordHashType hashType)
    {
        return hashType switch
        {
            PasswordHashType.Sha256 => HashMosquitto(password, "5"),
            PasswordHashType.Sha512 => HashMosquitto(password, "6"),
            PasswordHashType.Apr1 => Md5Crypt(password, RandomSalt(8), "$apr1$"),
            PasswordHashType.Bcrypt => BCryptNet.HashPassword(password, workFactor: 10),
            PasswordHashType.Sha1 => HashSha1(password),
            PasswordHashType.Sha256Crypt => ShaCrypt(password, RandomSalt(16), sha512: false, rounds: 5000),
            PasswordHashType.Sha512Crypt => ShaCrypt(password, RandomSalt(16), sha512: true, rounds: 5000),
            PasswordHashType.Md5Crypt => Md5Crypt(password, RandomSalt(8), "$1$"),
            PasswordHashType.Crypt => UnixDesCrypt.Crypt(password, RandomSalt(2)),
            PasswordHashType.Plaintext => password,
            PasswordHashType.Pbkdf2 => HashMosquittoPbkdf2(password),
            _ => HashMosquitto(password, "6")
        };
    }

    private static bool VerifyShaFamily(string password, string stored)
    {
        if (LooksLikeShaCrypt(stored))
        {
            return HashSuffixEquals(ComputeShaCrypt(password, stored), stored);
        }

        return VerifyMosquittoSha(password, stored);
    }

    private static bool LooksLikeShaCrypt(string stored)
    {
        var parts = stored.Split('$', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 3)
        {
            return false;
        }

        var hash = parts[^1];
        if (hash.Contains('+', StringComparison.Ordinal) || hash.Contains('=', StringComparison.Ordinal))
        {
            return false;
        }

        if (parts[1].StartsWith("rounds=", StringComparison.Ordinal))
        {
            return true;
        }

        return parts[0] switch
        {
            "5" => hash.Length == 43,
            "6" => hash.Length == 86,
            _ => false
        };
    }

    private static string ComputeShaCrypt(string password, string stored)
    {
        var parts = stored.Split('$', StringSplitOptions.RemoveEmptyEntries);
        var sha512 = string.Equals(parts[0], "6", StringComparison.Ordinal);
        var rounds = 5000;
        string salt;
        if (parts[1].StartsWith("rounds=", StringComparison.Ordinal))
        {
            if (!int.TryParse(parts[1].AsSpan(7), NumberStyles.Integer, CultureInfo.InvariantCulture, out rounds))
            {
                rounds = 5000;
            }
            salt = parts[2];
        }
        else
        {
            salt = parts[1];
        }

        return ShaCrypt(password, salt, sha512, rounds);
    }

    private static bool VerifyMosquittoSha(string password, string stored)
    {
        var parts = stored.Split('$', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 3)
        {
            return false;
        }

        var salt = Convert.FromBase64String(parts[1]);
        var pwBytes = Encoding.UTF8.GetBytes(password);
        var pwSalt = new byte[pwBytes.Length + salt.Length];
        Buffer.BlockCopy(pwBytes, 0, pwSalt, 0, pwBytes.Length);
        Buffer.BlockCopy(salt, 0, pwSalt, pwBytes.Length, salt.Length);

        var computed = parts[0] switch
        {
            "5" => Convert.ToBase64String(SHA256.HashData(pwSalt)),
            "6" => Convert.ToBase64String(SHA512.HashData(pwSalt)),
            _ => null
        };

        return computed != null && HashEquals(computed, parts[2]);
    }

    private static string HashMosquitto(string password, string method)
    {
        var salt = RandomNumberGenerator.GetBytes(12);
        var pwBytes = Encoding.UTF8.GetBytes(password);
        var pwSalt = new byte[pwBytes.Length + salt.Length];
        Buffer.BlockCopy(pwBytes, 0, pwSalt, 0, pwBytes.Length);
        Buffer.BlockCopy(salt, 0, pwSalt, pwBytes.Length, salt.Length);
        var hash = string.Equals(method, "5", StringComparison.Ordinal)
            ? Convert.ToBase64String(SHA256.HashData(pwSalt))
            : Convert.ToBase64String(SHA512.HashData(pwSalt));
        return $"${method}${Convert.ToBase64String(salt)}${hash}";
    }

    private static bool VerifyMosquittoPbkdf2(string password, string stored)
    {
        var parts = stored.Split('$', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 4 ||
            !int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var iterations) ||
            iterations < 1)
        {
            return false;
        }

        var salt = Convert.FromBase64String(parts[2]);
        var expected = Convert.FromBase64String(parts[3]);
        var derived = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA512, expected.Length);
        return CryptographicOperations.FixedTimeEquals(derived, expected);
    }

    private static string HashMosquittoPbkdf2(string password)
    {
        const int iterations = 101;
        var salt = RandomNumberGenerator.GetBytes(12);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA512, 64);
        return $"$7${iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    [SuppressMessage("Security", "CA5350:Do not use weak cryptographic algorithms")]
    private static string HashSha1(string password)
    {
        return "{SHA}" + Convert.ToBase64String(SHA1.HashData(Encoding.UTF8.GetBytes(password)));
    }

    [SuppressMessage("Security", "CA5350:Do not use weak cryptographic algorithms")]
    private static bool VerifySsha(string password, string stored)
    {
        var payload = Convert.FromBase64String(stored["{SSHA}".Length..]);
        if (payload.Length <= 20)
        {
            return false;
        }

        var digest = payload.AsSpan(0, 20);
        var salt = payload.AsSpan(20);
        var pwBytes = Encoding.UTF8.GetBytes(password);
        var combined = new byte[pwBytes.Length + salt.Length];
        Buffer.BlockCopy(pwBytes, 0, combined, 0, pwBytes.Length);
        salt.CopyTo(combined.AsSpan(pwBytes.Length));
        return CryptographicOperations.FixedTimeEquals(SHA1.HashData(combined), digest);
    }

    [SuppressMessage("Design", "MA0051:Method is too long")]
    [SuppressMessage("Security", "CA5351:Do not use broken cryptographic algorithms")]
    internal static string Md5Crypt(string password, string storedOrSalt, string magic)
    {
        var salt = ExtractMd5Salt(storedOrSalt, magic);
        var pwBytes = Encoding.UTF8.GetBytes(password);
        var saltBytes = Encoding.ASCII.GetBytes(salt);
        var magicBytes = Encoding.ASCII.GetBytes(magic);

        var alt = Concat(pwBytes, saltBytes, pwBytes);
        var altHash = MD5.HashData(alt);

        using var ctx = IncrementalHash.CreateHash(HashAlgorithmName.MD5);
        ctx.AppendData(pwBytes);
        ctx.AppendData(magicBytes);
        ctx.AppendData(saltBytes);

        for (var remaining = pwBytes.Length; remaining > 0; remaining -= 16)
        {
            ctx.AppendData(altHash.AsSpan(0, Math.Min(16, remaining)));
        }

        for (var bits = pwBytes.Length; bits > 0; bits >>= 1)
        {
            if ((bits & 1) != 0)
            {
                ctx.AppendData([0]);
            }
            else
            {
                ctx.AppendData(pwBytes.AsSpan(0, 1));
            }
        }

        var final = ctx.GetHashAndReset();

        for (var i = 0; i < 1000; i++)
        {
            if ((i & 1) != 0)
            {
                ctx.AppendData(pwBytes);
            }
            else
            {
                ctx.AppendData(final);
            }

            if (i % 3 != 0)
            {
                ctx.AppendData(saltBytes);
            }

            if (i % 7 != 0)
            {
                ctx.AppendData(pwBytes);
            }

            if ((i & 1) != 0)
            {
                ctx.AppendData(final);
            }
            else
            {
                ctx.AppendData(pwBytes);
            }

            final = ctx.GetHashAndReset();
        }

        var sb = new StringBuilder(magic.Length + salt.Length + 1 + 22);
        sb.Append(magic);
        sb.Append(salt);
        sb.Append('$');
        B64From24Bit(sb, final[0], final[6], final[12], 4);
        B64From24Bit(sb, final[1], final[7], final[13], 4);
        B64From24Bit(sb, final[2], final[8], final[14], 4);
        B64From24Bit(sb, final[3], final[9], final[15], 4);
        B64From24Bit(sb, final[4], final[10], final[5], 4);
        B64From24Bit(sb, 0, 0, final[11], 2);
        return sb.ToString();
    }

    private static string ExtractMd5Salt(string storedOrSalt, string magic)
    {
        var value = storedOrSalt;
        if (value.StartsWith(magic, StringComparison.Ordinal))
        {
            value = value[magic.Length..];
        }

        var end = value.IndexOf('$', StringComparison.Ordinal);
        if (end >= 0)
        {
            value = value[..end];
        }

        return value.Length > 8 ? value[..8] : value;
    }

    [SuppressMessage("Design", "MA0051:Method is too long")]
    internal static string ShaCrypt(string password, string salt, bool sha512, int rounds)
    {
        if (salt.Length > 16)
        {
            salt = salt[..16];
        }

        rounds = Math.Clamp(rounds, 1000, 999_999_999);

        var name = sha512 ? HashAlgorithmName.SHA512 : HashAlgorithmName.SHA256;
        var digestSize = sha512 ? 64 : 32;
        var pwBytes = Encoding.UTF8.GetBytes(password);
        var saltBytes = Encoding.ASCII.GetBytes(salt);

        var digestB = HashParts(name, pwBytes, saltBytes, pwBytes);

        using var hasherA = IncrementalHash.CreateHash(name);
        hasherA.AppendData(pwBytes);
        hasherA.AppendData(saltBytes);

        var remaining = pwBytes.Length;
        while (remaining > digestSize)
        {
            hasherA.AppendData(digestB);
            remaining -= digestSize;
        }

        hasherA.AppendData(digestB.AsSpan(0, remaining));

        for (var bits = pwBytes.Length; bits > 0; bits >>= 1)
        {
            hasherA.AppendData((bits & 1) != 0 ? digestB : pwBytes);
        }

        var digestA = hasherA.GetHashAndReset();

        using var hasherDp = IncrementalHash.CreateHash(name);
        for (var i = 0; i < pwBytes.Length; i++)
        {
            hasherDp.AppendData(pwBytes);
        }

        var pSeq = RepeatToLength(hasherDp.GetHashAndReset(), pwBytes.Length);

        using var hasherDs = IncrementalHash.CreateHash(name);
        var dsCount = 16 + digestA[0];
        for (var i = 0; i < dsCount; i++)
        {
            hasherDs.AppendData(saltBytes);
        }

        var sSeq = RepeatToLength(hasherDs.GetHashAndReset(), saltBytes.Length);

        var c = digestA;
        using var hasherC = IncrementalHash.CreateHash(name);
        for (var i = 0; i < rounds; i++)
        {
            if ((i & 1) != 0)
            {
                hasherC.AppendData(pSeq);
            }
            else
            {
                hasherC.AppendData(c);
            }

            if (i % 3 != 0)
            {
                hasherC.AppendData(sSeq);
            }

            if (i % 7 != 0)
            {
                hasherC.AppendData(pSeq);
            }

            if ((i & 1) != 0)
            {
                hasherC.AppendData(c);
            }
            else
            {
                hasherC.AppendData(pSeq);
            }

            c = hasherC.GetHashAndReset();
        }

        var sb = new StringBuilder(sha512 ? 123 : 80);
        sb.Append(sha512 ? "$6$" : "$5$");
        if (rounds != 5000)
        {
            sb.Append("rounds=");
            sb.Append(rounds);
            sb.Append('$');
        }

        sb.Append(salt);
        sb.Append('$');
        EncodeShaCrypt(sb, c, sha512);
        return sb.ToString();
    }

    private static void EncodeShaCrypt(StringBuilder sb, byte[] c, bool sha512)
    {
        if (sha512)
        {
            B64From24Bit(sb, c[0], c[21], c[42], 4);
            B64From24Bit(sb, c[22], c[43], c[1], 4);
            B64From24Bit(sb, c[44], c[2], c[23], 4);
            B64From24Bit(sb, c[3], c[24], c[45], 4);
            B64From24Bit(sb, c[25], c[46], c[4], 4);
            B64From24Bit(sb, c[47], c[5], c[26], 4);
            B64From24Bit(sb, c[6], c[27], c[48], 4);
            B64From24Bit(sb, c[28], c[49], c[7], 4);
            B64From24Bit(sb, c[50], c[8], c[29], 4);
            B64From24Bit(sb, c[9], c[30], c[51], 4);
            B64From24Bit(sb, c[31], c[52], c[10], 4);
            B64From24Bit(sb, c[53], c[11], c[32], 4);
            B64From24Bit(sb, c[12], c[33], c[54], 4);
            B64From24Bit(sb, c[34], c[55], c[13], 4);
            B64From24Bit(sb, c[56], c[14], c[35], 4);
            B64From24Bit(sb, c[15], c[36], c[57], 4);
            B64From24Bit(sb, c[37], c[58], c[16], 4);
            B64From24Bit(sb, c[59], c[17], c[38], 4);
            B64From24Bit(sb, c[18], c[39], c[60], 4);
            B64From24Bit(sb, c[40], c[61], c[19], 4);
            B64From24Bit(sb, c[62], c[20], c[41], 4);
            B64From24Bit(sb, 0, 0, c[63], 2);
            return;
        }

        B64From24Bit(sb, c[0], c[10], c[20], 4);
        B64From24Bit(sb, c[21], c[1], c[11], 4);
        B64From24Bit(sb, c[12], c[22], c[2], 4);
        B64From24Bit(sb, c[3], c[13], c[23], 4);
        B64From24Bit(sb, c[24], c[4], c[14], 4);
        B64From24Bit(sb, c[15], c[25], c[5], 4);
        B64From24Bit(sb, c[6], c[16], c[26], 4);
        B64From24Bit(sb, c[27], c[7], c[17], 4);
        B64From24Bit(sb, c[18], c[28], c[8], 4);
        B64From24Bit(sb, c[9], c[19], c[29], 4);
        B64From24Bit(sb, 0, c[31], c[30], 3);
    }

    internal static void B64From24Bit(StringBuilder sb, int b2, int b1, int b0, int n)
    {
        var w = (b2 << 16) | (b1 << 8) | b0;
        while (n-- > 0)
        {
            sb.Append(Itoa64[w & 0x3f]);
            w >>= 6;
        }
    }

    private static byte[] HashParts(HashAlgorithmName name, params byte[][] parts)
    {
        using var hasher = IncrementalHash.CreateHash(name);
        foreach (var part in parts)
        {
            hasher.AppendData(part);
        }

        return hasher.GetHashAndReset();
    }

    private static byte[] RepeatToLength(byte[] source, int length)
    {
        var result = new byte[length];
        var offset = 0;
        while (offset < length)
        {
            var n = Math.Min(source.Length, length - offset);
            Buffer.BlockCopy(source, 0, result, offset, n);
            offset += n;
        }

        return result;
    }

    private static byte[] Concat(params byte[][] parts)
    {
        var length = 0;
        foreach (var part in parts)
        {
            length += part.Length;
        }

        var result = new byte[length];
        var offset = 0;
        foreach (var part in parts)
        {
            Buffer.BlockCopy(part, 0, result, offset, part.Length);
            offset += part.Length;
        }

        return result;
    }

    private static string RandomSalt(int length)
    {
        var bytes = RandomNumberGenerator.GetBytes(length);
        var chars = new char[length];
        for (var i = 0; i < length; i++)
        {
            chars[i] = Itoa64[bytes[i] & 0x3f];
        }

        return new string(chars);
    }

    private static bool IsCryptAlphabet(string value)
    {
        foreach (var ch in value)
        {
            if (Itoa64.IndexOf(ch, StringComparison.Ordinal) < 0)
            {
                return false;
            }
        }

        return true;
    }

    internal static bool HashEquals(string a, string b)
    {
        var ba = Encoding.UTF8.GetBytes(a);
        var bb = Encoding.UTF8.GetBytes(b);
        if (ba.Length != bb.Length)
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(ba, bb);
    }

    private static bool HashSuffixEquals(string computed, string stored)
    {
        var computedHash = computed[(computed.LastIndexOf('$') + 1)..];
        var storedHash = stored[(stored.LastIndexOf('$') + 1)..];
        return HashEquals(computedHash, storedHash);
    }
}
