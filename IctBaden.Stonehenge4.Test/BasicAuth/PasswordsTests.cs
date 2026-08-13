using System;
using System.IO;
using IctBaden.Stonehenge.BasicAuth;
using IctBaden.Stonehenge.Core;
using Xunit;

namespace IctBaden.Stonehenge.Test.BasicAuth;

public sealed class PasswordsTests : IDisposable
{
    private readonly string _fileName = Path.Combine(Path.GetTempPath(), $"htpasswd-{Guid.NewGuid():N}");

    public void Dispose()
    {
        if (File.Exists(_fileName))
        {
            File.Delete(_fileName);
        }
    }

    [Fact]
    public void MosquittoSha512_SampleFile_AcceptsPassword()
    {
        WriteFile("""
                  # test 1234
                  test:$6$aJR3wDKgE3+wIsv2$x4X7D0jDtjYiEGgV0rhh3fi60ZtCRaZgUM60Oow9T6NEJDpduVIED8z/3sKRYblkitG8fLJB2T1cNvT1zVZ9lQ==
                  """);

        var passwords = new Passwords(_fileName);
        Assert.True(passwords.IsValid("test", "1234"));
        Assert.False(passwords.IsValid("test", "wrong"));
        Assert.False(passwords.IsValid("other", "1234"));
        Assert.Equal(["test"], passwords.GetUsers());
    }

    [Theory]
    [InlineData("myName:$apr1$r31.....$HqJZimcKQFAMYayBlzkrA/", "myPassword")]
    [InlineData("myName:{SHA}VBPuJHI7uixaa6LQGWx4s+5GKNE=", "myPassword")]
    [InlineData("myName:$2y$05$c4WoMPo3SXsafkva.HHa6uXQZWr7oboPiC2bT/r7q1BB8I2s0BRqC", "myPassword")]
    [InlineData("myName:rqXexS6ZhobKA", "myPassword")]
    [InlineData("myName:$1$rasmusle$rISCgZzpwk3UhDidwXvin0", "rasmuslerdorf")]
    [InlineData("myName:myPassword", "myPassword")]
    public void ApacheHtpasswd_OfficialVectors_AcceptsPassword(string line, string password)
    {
        WriteFile(line);
        Assert.True(new Passwords(_fileName).IsValid("myName", password));
        Assert.False(new Passwords(_fileName).IsValid("myName", "wrong"));
    }

    [Theory]
    [InlineData("$5$saltstring$5B8vYYiY.CVt1RlTTf8KbXBH3hsxY/GNooZaBBGWEc5", "Hello world!")]
    [InlineData("$5$rounds=10000$saltstringsaltst$3xv.VbSHBb41AL9AvLeujZkZRBAwqFMz2.opqey6IcA", "Hello world!")]
    [InlineData("$6$saltstring$svn8UoSVapNtMuq1ukKS4tPQd8iKwSMHWjl/O817G3uBnIFNjnQJuesI68u4OTLiBFdcbYEdFCoEOfaS35inz1", "Hello world!")]
    [InlineData("$6$rounds=10000$saltstringsaltst$OW1/O6BYHV6BcXZu8QVeXbDWra3Oeqh0sbHbbMCVNSnCM/UrjmM0Dp8vOuZeHBy/YTBmSK6H9qs/y3RnOaw5v.", "Hello world!")]
    public void ShaCrypt_SpecVectors_AcceptsPassword(string hash, string password)
    {
        WriteFile($"user:{hash}");
        Assert.True(new Passwords(_fileName).IsValid("user", password));
        Assert.False(new Passwords(_fileName).IsValid("user", "wrong"));
    }

    [Theory]
    [InlineData(PasswordHashType.Sha512)]
    [InlineData(PasswordHashType.Sha256)]
    [InlineData(PasswordHashType.Apr1)]
    [InlineData(PasswordHashType.Bcrypt)]
    [InlineData(PasswordHashType.Sha1)]
    [InlineData(PasswordHashType.Sha256Crypt)]
    [InlineData(PasswordHashType.Sha512Crypt)]
    [InlineData(PasswordHashType.Md5Crypt)]
    [InlineData(PasswordHashType.Crypt)]
    [InlineData(PasswordHashType.Plaintext)]
    [InlineData(PasswordHashType.Pbkdf2)]
    public void SetPassword_Roundtrip_AcceptsPassword(PasswordHashType hashType)
    {
        File.WriteAllText(_fileName, string.Empty);
        var passwords = new Passwords(_fileName);
        passwords.SetPassword("alice", "s3cret", hashType);

        Assert.True(passwords.IsValid("alice", "s3cret"));
        Assert.False(passwords.IsValid("alice", "other"));
        Assert.Equal(["alice"], passwords.GetUsers());
    }

    [Fact]
    public void SetPassword_Default_WritesMosquittoSha512()
    {
        File.WriteAllText(_fileName, string.Empty);
        var passwords = new Passwords(_fileName);
        passwords.SetPassword("bob", "hunter2");

        var line = File.ReadAllText(_fileName).Trim();
        Assert.StartsWith("bob:$6$", line, StringComparison.Ordinal);
        Assert.True(passwords.IsValid("bob", "hunter2"));
    }

    [Fact]
    public void RemoveUser_LeavesOtherUsers()
    {
        File.WriteAllText(_fileName, string.Empty);
        var passwords = new Passwords(_fileName);
        passwords.SetPassword("alice", "a");
        passwords.SetPassword("bob", "b");
        passwords.RemoveUser("alice");

        Assert.Equal(["bob"], passwords.GetUsers());
        Assert.False(passwords.IsValid("alice", "a"));
        Assert.True(passwords.IsValid("bob", "b"));
    }

    [Fact]
    public void Ssha_AcceptsPassword()
    {
        var password = "honey";
        var salt = "salt"u8.ToArray();
        var material = new byte[password.Length + salt.Length];
        System.Text.Encoding.UTF8.GetBytes(password, material);
        Buffer.BlockCopy(salt, 0, material, password.Length, salt.Length);
        var digest = System.Security.Cryptography.SHA1.HashData(material);
        var payload = new byte[digest.Length + salt.Length];
        Buffer.BlockCopy(digest, 0, payload, 0, digest.Length);
        Buffer.BlockCopy(salt, 0, payload, digest.Length, salt.Length);
        WriteFile("user:{SSHA}" + Convert.ToBase64String(payload));

        Assert.True(new Passwords(_fileName).IsValid("user", password));
        Assert.False(new Passwords(_fileName).IsValid("user", "wrong"));
    }

    private void WriteFile(string content)
    {
        File.WriteAllText(_fileName, content + Environment.NewLine);
    }
}
