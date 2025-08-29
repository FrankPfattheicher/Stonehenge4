using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace IctBaden.Stonehenge.Core;

public class Passwords
{
    private readonly string _fileName;

    /// <summary>
    /// Use mosquitto_passwd to create and maintain password file
    /// </summary>
    /// <param name="fileName"></param>
    public Passwords(string fileName)
    {
        _fileName = fileName;
    }

    public IList<string> GetUsers()
    {
        var users = new List<string>();
        var lines = File.ReadAllLines(_fileName);
        // ReSharper disable once LoopCanBeConvertedToQuery
        foreach (var line in lines)
        {
            var userPw = line.Split(':');
            if (userPw.Length != 2) continue;

            users.Add(userPw[0]);
        }
        return users;
    }

    // ReSharper disable once UnusedMember.Global
    public bool IsValid(string user, string password)
    {
        if (string.IsNullOrEmpty(password)) return false;

        var lines = File.ReadAllLines(_fileName);
        foreach (var line in lines)
        {
            var userPw = line.Split(':');
            if (userPw.Length != 2) continue;
            if (!string.Equals(userPw[0], user, StringComparison.Ordinal)) continue;

            var pw = userPw[1].Split("$", StringSplitOptions.RemoveEmptyEntries);
            if (pw.Length != 3) continue;

            var method = pw[0];
            var salt = Convert.FromBase64String(pw[1]);
            var hash = pw[2];

            var pwBytes = Encoding.ASCII.GetBytes(password);
            var pwSalt = new byte[pwBytes.Length + salt.Length];
            Array.Copy(pwBytes, pwSalt, pwBytes.Length);
            Array.Copy(salt, 0, pwSalt, pwBytes.Length, salt.Length);

            switch (method)
            {
                case "5":
                    return string.Equals(hash, Convert.ToBase64String(SHA256.HashData(pwSalt)), StringComparison.Ordinal);
                case "6":
                    return string.Equals(hash, Convert.ToBase64String(SHA512.HashData(pwSalt)), StringComparison.Ordinal);
                default:
                    Trace.TraceError($"Passwords: Unsupported hashing method {method}");
                    break;
            }
        }

        return false;
    }

    private List<string> LinesWithoutUser(string user)
    {
        var newLines = new List<string>();
        var lines = File.ReadAllLines(_fileName);
        foreach (var line in lines)
        {
            var userPw = line.Split(':');
            if (userPw.Length != 2) continue;
            if (!string.Equals(userPw[0], user, StringComparison.Ordinal))
            {
                newLines.Add(line);
            }
        }
        return newLines;
    }

    public void RemoveUser(string user)
    {
        var newLines = LinesWithoutUser(user);
        File.WriteAllLines(_fileName, newLines);
    }

    public void SetPassword(string user, string password)
    {
        var newLines = LinesWithoutUser(user);

        var rnd = new Random();
        var salt = new byte[12];
        rnd.NextBytes(salt);
        var pwBytes = Encoding.ASCII.GetBytes(password);
        var pwSalt = new byte[pwBytes.Length + salt.Length];
        Array.Copy(pwBytes, pwSalt, pwBytes.Length);
        Array.Copy(salt, 0, pwSalt, pwBytes.Length, salt.Length);
        var hash = Convert.ToBase64String(SHA512.HashData(pwSalt));

        var newLine = $"{user}:$6${Convert.ToBase64String(salt)}${hash}";
        newLines.Add(newLine);

        File.WriteAllLines(_fileName, newLines);
    }

}
