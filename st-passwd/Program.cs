using IctBaden.Framework.AppUtils;
using IctBaden.Stonehenge.Core;
// ReSharper disable ReplaceSubstringWithRangeIndexer

namespace st_passwd;

internal static class Program
{
    static void Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine();
            Console.WriteLine(@"Stonehenge password tool");
            Console.WriteLine(@"  usage:");
            Console.WriteLine(@"  st-passwd [-? | -h] [-l] [[-d] [-p password] username]");
            Environment.Exit(0);
        }

        var appBasePath = ApplicationInfo.ApplicationDirectory;
        var pwdFileName = Path.Combine(appBasePath, ".htpasswd");
        if (!File.Exists(pwdFileName))
        {
            File.WriteAllText(pwdFileName, Environment.NewLine);
            Console.WriteLine();
            Console.WriteLine(@"Created password file " + pwdFileName);
        }

        var appPasswords = new Passwords(pwdFileName);

        if (args.Any(arg => arg == "-?") || args.Any(arg => arg == "-h"))
        {
            Console.WriteLine(@"-? or -h     show this help");
            Console.WriteLine(@"-l           list all users] [[-d] [-p password] username]");
            Console.WriteLine(@"-d           delete password for username");
            Console.WriteLine(@"-p           set password for username");
            Environment.Exit(0);
        }

        if (args.Any(arg => arg == "-l"))
        {
            Console.WriteLine();
            Console.WriteLine(@"file:");
            Console.WriteLine(@$"    {pwdFileName}");
            Console.WriteLine(@"users:");

            var users = appPasswords.GetUsers()
                .Select(u => $"    {u}")
                .ToList();
            Console.WriteLine(users.Count == 0 
                ? "<none>" 
                : string.Join(Environment.NewLine, users));
            Environment.Exit(0);
        }

        var user = args.Last();
        if (string.IsNullOrEmpty(user))
        {
            Environment.Exit(1);
        }
                
        if (args.Any(arg => arg == "-d"))
        {
            appPasswords.RemoveUser(user);
            Environment.Exit(0);
        }

        for (var px = 0; px < args.Length; px++)
        {
            if (args[px] != "-p") continue;
                
            px++;
            if (px >= args.Length)
            {
                Environment.Exit(2);
            }
                
            var pwd = args[px];
            if (!string.IsNullOrEmpty(pwd))
            {
                appPasswords.SetPassword(user, pwd);
                Environment.Exit(0);
            }
        }

        Console.Write(@$"Enter password for user {user} : ");
        var pw1 = ReadPassword();
        Console.Write(@$"Repeat password for user {user} : ");
        var pw2 = ReadPassword();

        if (pw1 != pw2)
        {
            Console.WriteLine(@"Passwords do not match");
            Environment.Exit(0);
        }

        appPasswords.SetPassword(user, pw1);
    }

    private static string ReadPassword()
    {
        var pass = "";
        while (true)
        {
            ConsoleKeyInfo key = Console.ReadKey(true);
            // Backspace Should Not Work
            if (key.Key != ConsoleKey.Backspace && key.Key != ConsoleKey.Enter)
            {
                pass += key.KeyChar;
                Console.Write("*");
            }
            else
            {
                if (key.Key == ConsoleKey.Backspace && pass.Length > 0)
                {
                    pass = pass.Substring(0, (pass.Length - 1));
                    Console.Write(@"\b \b");
                }
                else if (key.Key == ConsoleKey.Enter)
                {
                    break;
                }
            }
        }

        Console.WriteLine();
        return pass;
    }

}