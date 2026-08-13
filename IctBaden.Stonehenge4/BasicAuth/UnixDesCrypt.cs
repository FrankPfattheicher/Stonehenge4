using System;
using System.Text;

namespace IctBaden.Stonehenge.BasicAuth;

/// <summary>
/// Traditional Unix DES crypt(3) as used by htpasswd -d.
/// </summary>
internal static class UnixDesCrypt
{
    private static readonly int[] Ip =
    [
        58, 50, 42, 34, 26, 18, 10, 2,
        60, 52, 44, 36, 28, 20, 12, 4,
        62, 54, 46, 38, 30, 22, 14, 6,
        64, 56, 48, 40, 32, 24, 16, 8,
        57, 49, 41, 33, 25, 17, 9, 1,
        59, 51, 43, 35, 27, 19, 11, 3,
        61, 53, 45, 37, 29, 21, 13, 5,
        63, 55, 47, 39, 31, 23, 15, 7
    ];

    private static readonly int[] Fp =
    [
        40, 8, 48, 16, 56, 24, 64, 32,
        39, 7, 47, 15, 55, 23, 63, 31,
        38, 6, 46, 14, 54, 22, 62, 30,
        37, 5, 45, 13, 53, 21, 61, 29,
        36, 4, 44, 12, 52, 20, 60, 28,
        35, 3, 43, 11, 51, 19, 59, 27,
        34, 2, 42, 10, 50, 18, 58, 26,
        33, 1, 41, 9, 49, 17, 57, 25
    ];

    private static readonly int[] E =
    [
        32, 1, 2, 3, 4, 5,
        4, 5, 6, 7, 8, 9,
        8, 9, 10, 11, 12, 13,
        12, 13, 14, 15, 16, 17,
        16, 17, 18, 19, 20, 21,
        20, 21, 22, 23, 24, 25,
        24, 25, 26, 27, 28, 29,
        28, 29, 30, 31, 32, 1
    ];

    private static readonly int[] P =
    [
        16, 7, 20, 21, 29, 12, 28, 17,
        1, 15, 23, 26, 5, 18, 31, 10,
        2, 8, 24, 14, 32, 27, 3, 9,
        19, 13, 30, 6, 22, 11, 4, 25
    ];

    private static readonly int[] Pc1 =
    [
        57, 49, 41, 33, 25, 17, 9,
        1, 58, 50, 42, 34, 26, 18,
        10, 2, 59, 51, 43, 35, 27,
        19, 11, 3, 60, 52, 44, 36,
        63, 55, 47, 39, 31, 23, 15,
        7, 62, 54, 46, 38, 30, 22,
        14, 6, 61, 53, 45, 37, 29,
        21, 13, 5, 28, 20, 12, 4
    ];

    private static readonly int[] Pc2 =
    [
        14, 17, 11, 24, 1, 5,
        3, 28, 15, 6, 21, 10,
        23, 19, 12, 4, 26, 8,
        16, 7, 27, 20, 13, 2,
        41, 52, 31, 37, 47, 55,
        30, 40, 51, 45, 33, 48,
        44, 49, 39, 56, 34, 53,
        46, 42, 50, 36, 29, 32
    ];

    private static readonly int[] Shifts = [1, 1, 2, 2, 2, 2, 2, 2, 1, 2, 2, 2, 2, 2, 2, 1];

    private static readonly byte[][] S =
    [
        [
            14, 4, 13, 1, 2, 15, 11, 8, 3, 10, 6, 12, 5, 9, 0, 7,
            0, 15, 7, 4, 14, 2, 13, 1, 10, 6, 12, 11, 9, 5, 3, 8,
            4, 1, 14, 8, 13, 6, 2, 11, 15, 12, 9, 7, 3, 10, 5, 0,
            15, 12, 8, 2, 4, 9, 1, 7, 5, 11, 3, 14, 10, 0, 6, 13
        ],
        [
            15, 1, 8, 14, 6, 11, 3, 4, 9, 7, 2, 13, 12, 0, 5, 10,
            3, 13, 4, 7, 15, 2, 8, 14, 12, 0, 1, 10, 6, 9, 11, 5,
            0, 14, 7, 11, 10, 4, 13, 1, 5, 8, 12, 6, 9, 3, 2, 15,
            13, 8, 10, 1, 3, 15, 4, 2, 11, 6, 7, 12, 0, 5, 14, 9
        ],
        [
            10, 0, 9, 14, 6, 3, 15, 5, 1, 13, 12, 7, 11, 4, 2, 8,
            13, 7, 0, 9, 3, 4, 6, 10, 2, 8, 5, 14, 12, 11, 15, 1,
            13, 6, 4, 9, 8, 15, 3, 0, 11, 1, 2, 12, 5, 10, 14, 7,
            1, 10, 13, 0, 6, 9, 8, 7, 4, 15, 14, 3, 11, 5, 2, 12
        ],
        [
            7, 13, 14, 3, 0, 6, 9, 10, 1, 2, 8, 5, 11, 12, 4, 15,
            13, 8, 11, 5, 6, 15, 0, 3, 4, 7, 2, 12, 1, 10, 14, 9,
            10, 6, 9, 0, 12, 11, 7, 13, 15, 1, 3, 14, 5, 2, 8, 4,
            3, 15, 0, 6, 10, 1, 13, 8, 9, 4, 5, 11, 12, 7, 2, 14
        ],
        [
            2, 12, 4, 1, 7, 10, 11, 6, 8, 5, 3, 15, 13, 0, 14, 9,
            14, 11, 2, 12, 4, 7, 13, 1, 5, 0, 15, 10, 3, 9, 8, 6,
            4, 2, 1, 11, 10, 13, 7, 8, 15, 9, 12, 5, 6, 3, 0, 14,
            11, 8, 12, 7, 1, 14, 2, 13, 6, 15, 0, 9, 10, 4, 5, 3
        ],
        [
            12, 1, 10, 15, 9, 2, 6, 8, 0, 13, 3, 4, 14, 7, 5, 11,
            10, 15, 4, 2, 7, 12, 9, 5, 6, 1, 13, 14, 0, 11, 3, 8,
            9, 14, 15, 5, 2, 8, 12, 3, 7, 0, 4, 10, 1, 13, 11, 6,
            4, 3, 2, 12, 9, 5, 15, 10, 11, 14, 1, 7, 6, 0, 8, 13
        ],
        [
            4, 11, 2, 14, 15, 0, 8, 13, 3, 12, 9, 7, 5, 10, 6, 1,
            13, 0, 11, 7, 4, 9, 1, 10, 14, 3, 5, 12, 2, 15, 8, 6,
            1, 4, 11, 13, 12, 3, 7, 14, 10, 15, 6, 8, 0, 5, 9, 2,
            6, 11, 13, 8, 1, 4, 10, 7, 9, 5, 0, 15, 14, 2, 3, 12
        ],
        [
            13, 2, 8, 4, 6, 15, 11, 1, 10, 9, 3, 14, 5, 0, 12, 7,
            1, 15, 13, 8, 10, 3, 7, 4, 12, 5, 6, 11, 0, 14, 9, 2,
            7, 11, 4, 1, 9, 12, 14, 2, 0, 6, 10, 13, 15, 3, 5, 8,
            2, 1, 14, 7, 4, 10, 8, 13, 15, 12, 9, 0, 3, 5, 6, 11
        ]
    ];

    public static string Crypt(string password, string salt)
    {
        if (string.IsNullOrEmpty(salt) || salt.Length < 2)
        {
            throw new ArgumentException(@"DES crypt salt must be 2 characters.", nameof(salt));
        }

        salt = salt[..2];
        var subkeys = KeySchedule(PasswordToKey(password));
        var saltBits = DecodeSalt(salt);
        ulong block = 0;
        for (var i = 0; i < 25; i++)
        {
            block = Encrypt(block, subkeys, saltBits);
        }

        return Encode(salt, block);
    }

    private static ulong PasswordToKey(string password)
    {
        var bytes = Encoding.Latin1.GetBytes(password);
        ulong key = 0;
        for (var i = 0; i < 8; i++)
        {
            key <<= 8;
            if (i < bytes.Length)
            {
                key |= (byte)(bytes[i] << 1);
            }
        }

        return key;
    }

    private static int DecodeSalt(string salt)
    {
        return AsciiToBin(salt[0]) | (AsciiToBin(salt[1]) << 6);
    }

    private static int AsciiToBin(char ch)
    {
        var c = (int)ch;
        if (c > 'Z')
        {
            c -= 6;
        }

        if (c > '9')
        {
            c -= 7;
        }

        return (c - '.') & 0x3f;
    }

    private static ulong[] KeySchedule(ulong key)
    {
        var perm = Permute(key, Pc1, 64);
        var c = (uint)((perm >> 28) & 0x0FFFFFFF);
        var d = (uint)(perm & 0x0FFFFFFF);
        var subkeys = new ulong[16];
        for (var round = 0; round < 16; round++)
        {
            c = RotateLeft28(c, Shifts[round]);
            d = RotateLeft28(d, Shifts[round]);
            var cd = ((ulong)c << 28) | d;
            subkeys[round] = Permute(cd, Pc2, 56);
        }

        return subkeys;
    }

    private static ulong Encrypt(ulong block, ulong[] subkeys, int saltBits)
    {
        var ip = Permute(block, Ip, 64);
        var l = (uint)(ip >> 32);
        var r = (uint)ip;
        for (var round = 0; round < 16; round++)
        {
            var next = l ^ F(r, subkeys[round], saltBits);
            l = r;
            r = next;
        }

        var preoutput = ((ulong)r << 32) | l;
        return Permute(preoutput, Fp, 64);
    }

    private static uint F(uint r, ulong subkey, int saltBits)
    {
        var expanded = Permute(r, E, 32);
        for (var i = 0; i < 12; i++)
        {
            if ((saltBits & (1 << i)) == 0)
            {
                continue;
            }

            var s1 = 47 - i;
            var s2 = 23 - i;
            var b1 = (expanded >> s1) & 1UL;
            var b2 = (expanded >> s2) & 1UL;
            if (b1 != b2)
            {
                expanded ^= (1UL << s1) | (1UL << s2);
            }
        }

        expanded ^= subkey;

        uint sOut = 0;
        for (var i = 0; i < 8; i++)
        {
            var six = (int)((expanded >> (42 - i * 6)) & 0x3f);
            var row = ((six >> 4) & 2) | (six & 1);
            var col = (six >> 1) & 0xf;
            sOut = (sOut << 4) | S[i][row * 16 + col];
        }

        return (uint)Permute(sOut, P, 32);
    }

    private static uint RotateLeft28(uint value, int n)
    {
        value &= 0x0FFFFFFF;
        return ((value << n) | (value >> (28 - n))) & 0x0FFFFFFF;
    }

    private static ulong Permute(ulong input, int[] table, int inputBits)
    {
        ulong output = 0;
        foreach (var src in table)
        {
            output <<= 1;
            output |= (input >> (inputBits - src)) & 1UL;
        }

        return output;
    }

    private static string Encode(string salt, ulong block)
    {
        var chars = new char[13];
        chars[0] = salt[0];
        chars[1] = salt[1];
        for (var i = 0; i < 11; i++)
        {
            var c = 0;
            for (var j = 0; j < 6; j++)
            {
                var bitIndex = i * 6 + j;
                c <<= 1;
                if (bitIndex < 64)
                {
                    c |= (int)((block >> (63 - bitIndex)) & 1UL);
                }
            }

            chars[i + 2] = HtpasswdHash.Itoa64[c];
        }

        return new string(chars);
    }
}
