using System;
using System.Collections.Generic;
using System.IO;
using IctBaden.Stonehenge.Hosting;

namespace IctBaden.Stonehenge.BasicAuth;

public class Passwords
{
    /// <summary>
    /// File name used for basic auth passwords
    /// </summary>
    public readonly string FileName = string.Empty;

    /// <summary>
    /// Apache htpasswd / mosquitto_passwd compatible password file.
    /// </summary>
    /// <param name="fileName">Path to the .htpasswd file.</param>
    public Passwords(string fileName)
    {
        if(!File.Exists(fileName))
        {
            fileName = Path.Combine(StonehengeApplication.BaseDirectory, fileName);
        }
        if(File.Exists(fileName))
        {
            FileName = fileName;
        }
    }

    public IList<string> GetUsers()
    {
        if (string.IsNullOrEmpty(FileName)) return [];
        
        var users = new List<string>();
        var lines = File.ReadAllLines(FileName);
        // ReSharper disable once LoopCanBeConvertedToQuery
        foreach (var line in lines)
        {
            if (!TryParseEntry(line, out var user, out _))
            {
                continue;
            }

            users.Add(user);
        }

        return users;
    }

    // ReSharper disable once UnusedMember.Global
    public bool IsValid(string user, string password)
    {
        if (string.IsNullOrEmpty(FileName)) return false;
        if (string.IsNullOrEmpty(password)) return false;

        var lines = File.ReadAllLines(FileName);
        foreach (var line in lines)
        {
            if (!TryParseEntry(line, out var name, out var stored))
            {
                continue;
            }

            if (!string.Equals(name, user, StringComparison.Ordinal))
            {
                continue;
            }

            return HtpasswdHash.Verify(password, stored);
        }

        return false;
    }

    private List<string> LinesWithoutUser(string user)
    {
        var newLines = new List<string>();
        var lines = File.ReadAllLines(FileName);
        foreach (var line in lines)
        {
            if (TryParseEntry(line, out var name, out _) &&
                string.Equals(name, user, StringComparison.Ordinal))
            {
                continue;
            }

            newLines.Add(line);
        }

        return newLines;
    }

    public void RemoveUser(string user)
    {
        var newLines = LinesWithoutUser(user);
        File.WriteAllLines(FileName, newLines);
    }

    public void SetPassword(string user, string password, PasswordHashType hashType = PasswordHashType.Sha512)
    {
        var newLines = LinesWithoutUser(user);
        newLines.Add($"{user}:{HtpasswdHash.Hash(password, hashType)}");
        File.WriteAllLines(FileName, newLines);
    }

    private static bool TryParseEntry(string line, out string user, out string hash)
    {
        user = string.Empty;
        hash = string.Empty;
        if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
        {
            return false;
        }

        var colon = line.IndexOf(':', StringComparison.Ordinal);
        if (colon <= 0 || colon == line.Length - 1)
        {
            return false;
        }

        user = line[..colon];
        hash = line[(colon + 1)..];
        return true;
    }
}
