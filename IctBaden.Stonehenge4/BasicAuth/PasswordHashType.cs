namespace IctBaden.Stonehenge.BasicAuth;

/// <summary>
/// Password encodings understood by Apache htpasswd, nginx, and mosquitto_passwd.
/// </summary>
public enum PasswordHashType
{
    /// <summary>mosquitto_passwd SHA-512 ($6$), default when writing new passwords.</summary>
    Sha512 = 0,

    /// <summary>mosquitto_passwd SHA-256 ($5$).</summary>
    Sha256,

    /// <summary>Apache MD5 ($apr1$), htpasswd -m.</summary>
    Apr1,

    /// <summary>bcrypt ($2y$), htpasswd -B.</summary>
    Bcrypt,

    /// <summary>Unsalted SHA-1 ({SHA}), htpasswd -s.</summary>
    Sha1,

    /// <summary>glibc SHA-256 crypt ($5$), htpasswd -2.</summary>
    Sha256Crypt,

    /// <summary>glibc SHA-512 crypt ($6$), htpasswd -5.</summary>
    Sha512Crypt,

    /// <summary>MD5 crypt ($1$).</summary>
    Md5Crypt,

    /// <summary>Traditional Unix DES crypt, htpasswd -d.</summary>
    Crypt,

    /// <summary>Plaintext, htpasswd -p.</summary>
    Plaintext,

    /// <summary>mosquitto_passwd PBKDF2-SHA512 ($7$).</summary>
    Pbkdf2
}
