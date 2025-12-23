using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using IctBaden.Stonehenge.Extensions;
using IctBaden.Stonehenge.Resources;
// ReSharper disable ReplaceSubstringWithRangeIndexer

namespace IctBaden.Stonehenge.Client;

public static class UserContentLinks
{
    private const string CssInsertPoint = "<!--stonehengeUserStylesheets-->";
    private const string CssLinkTemplate = "<link type='text/css' rel='stylesheet' href='{0}'>";

    private const string ExtensionsInsertPoint = "<!--stonehengeExtensions-->";
    private const string JsUserScriptsInsertPoint = "<!--stonehengeUserScripts-->";
    private const string JsLinkTemplate = "<script type='application/javascript' src='{0}'></script>";
    private const string JsmLinkTemplate = "<script type='module' src='{0}'></script>";

    private static readonly Dictionary<string, string> StyleSheets = new(StringComparer.OrdinalIgnoreCase);
    private static readonly List<string> ThemeInitialized = [];
    private static readonly string AppPath = Path.DirectorySeparatorChar + "app" + Path.DirectorySeparatorChar;
    private static string _userJs = string.Empty;
    private static string _extensions = string.Empty;

    public static void AddStyleSheet(string theme, string css)
    {
        var link = Environment.NewLine + string.Format(CssLinkTemplate, css);
        StyleSheets.TryAdd(theme, link);
    }

    public static void InitializeUserContentLinks(Assembly appAssembly, IList<Assembly> resourceAssemblies, string appFilesPath, string theme)
    {
        if (ThemeInitialized.Contains(theme, StringComparer.Ordinal))
        {
            return;
        }
        CreateUserCssLinks(appAssembly, appFilesPath, theme);
        CreateUserJsLinks(appAssembly, appFilesPath);
        CreateExtensionLinks(resourceAssemblies);
        ThemeInitialized.Add(theme);
    }

    public static string InsertUserLinks(string text, string theme) =>
        text.Replace(CssInsertPoint, StyleSheets[theme])
            .Replace(JsUserScriptsInsertPoint, _userJs)
            .Replace(ExtensionsInsertPoint, _extensions);

    private static void CreateUserCssLinks(Assembly appAssembly, string appFilesPath, string theme)
    {
        // ReSharper disable once CanSimplifyDictionaryLookupWithTryGetValue
        var styleSheets = StyleSheets.ContainsKey(theme)
            ? StyleSheets[theme]
            : string.Empty;

        var path = Path.Combine(appFilesPath, "styles");
        if (Directory.Exists(path))
        {
            var links = Directory.GetFiles(path, "*.css", SearchOption.AllDirectories)
                .Select(dir => string.Format(CultureInfo.InvariantCulture, CssLinkTemplate,
                    dir.Substring(dir.IndexOf(AppPath, StringComparison.InvariantCulture) + 1).Replace('\\', '/')));
            styleSheets = string.Join(Environment.NewLine, links);
        }

        const string resourceBaseName = ".app.";
        const string baseNameStyles = resourceBaseName + "styles.";
        const string baseNameTheme = resourceBaseName + "themes.";
        var resourceNames = appAssembly.GetManifestResourceNames();
        var cssResources = resourceNames.Where(name => name.EndsWith(".css", StringComparison.OrdinalIgnoreCase)).ToList();
        // styles first
        // ReSharper disable once LoopCanBeConvertedToQuery
        foreach (var resourceName in cssResources.Where(name => name.Contains(baseNameStyles, StringComparison.OrdinalIgnoreCase)))
        {
            var css = ResourceLoader.GetShortResourceName(appAssembly, resourceBaseName, resourceName)
                .Replace(".", "/").Replace("/css", ".css");
            styleSheets += Environment.NewLine + string.Format(CssLinkTemplate, css);
        }

        // then themes
        // ReSharper disable once LoopCanBeConvertedToQuery
        foreach (var resourceName in cssResources.Where(name => name.Contains(baseNameTheme + theme, StringComparison.OrdinalIgnoreCase)))
        {
            var css = ResourceLoader.GetShortResourceName(appAssembly, resourceBaseName, resourceName)
                .Replace(".", "/").Replace("/css", ".css");
            styleSheets += Environment.NewLine + string.Format(CssLinkTemplate, css);
        }

        path = Path.Combine(appFilesPath, "app", "themes", theme + ".css");
        if (File.Exists(path))
        {
            var css = path.Substring(path.IndexOf(AppPath, StringComparison.InvariantCulture) + 1)
                .Replace('\\', '/');
            styleSheets += Environment.NewLine + string.Format(CssLinkTemplate, css);
        }

        if (StyleSheets.ContainsKey(theme))
            StyleSheets[theme] += styleSheets;
        else
            StyleSheets.TryAdd(theme, styleSheets);

        
    }

    private static void CreateUserJsLinks(Assembly userAssembly, string appFilesPath)
    {
        var path = Path.Combine(appFilesPath, "scripts");
        if (Directory.Exists(path))
        {
            var links = Directory.GetFiles(path, "*.js", SearchOption.AllDirectories)
                .Select(dir => string.Format(CultureInfo.InvariantCulture, JsLinkTemplate,
                    dir.Substring(dir.IndexOf(AppPath, StringComparison.InvariantCulture) + 1).Replace('\\', '/')));
            _userJs = string.Join(Environment.NewLine, links);
        }

        const string resourceBaseName = ".app.";
        const string baseNameScripts = resourceBaseName + "scripts.";
        var resourceNames = userAssembly.GetManifestResourceNames();
        var jsResources = resourceNames
            .Where(name => name.EndsWith(".js", StringComparison.OrdinalIgnoreCase))
            .ToList();
        // ReSharper disable once LoopCanBeConvertedToQuery
        foreach (var resourceName in jsResources.Where(name => name.Contains(baseNameScripts, StringComparison.OrdinalIgnoreCase)))
        {
            var js = ResourceLoader.GetShortResourceName(userAssembly, resourceBaseName, resourceName)
                .Replace(".", "/")
                .Replace("/js", ".js");
            _userJs += Environment.NewLine + string.Format(JsLinkTemplate, js);
        }
    }

    private static void CreateExtensionLinks(IList<Assembly> assemblies)
    {
        const string resourceBaseName = ".app.";
        const string baseNameSrc = resourceBaseName + "src.";
        foreach (var assembly in assemblies)
        {
            if (assembly.GetTypes().FirstOrDefault(t => t.GetInterfaces().Contains(typeof(IStonehengeExtension))) ==
                null) continue;

            var resources = assembly.GetManifestResourceNames();

            var jsResources = resources
                .Where(name => name.EndsWith(".js", StringComparison.OrdinalIgnoreCase) && !name.Contains(".min.", StringComparison.OrdinalIgnoreCase))
                .ToArray();
            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var resourceName in jsResources.Where(name => name.Contains(baseNameSrc, StringComparison.OrdinalIgnoreCase)))
            {
                var js = ResourceLoader.GetShortResourceName(assembly, resourceBaseName, resourceName)
                    .Replace('.', '/')
                    .Replace("/js", ".js", StringComparison.OrdinalIgnoreCase);
                if (resources.Contains(resourceName.Replace(".js", ".min.js"), StringComparer.OrdinalIgnoreCase))
                {
                    js = js.Replace(".js", "{.min}.js", StringComparison.OrdinalIgnoreCase);
                }
                _extensions += Environment.NewLine + string.Format(JsLinkTemplate, js);
            }
            var mjsResources = resources
                .Where(name => name.EndsWith(".mjs", StringComparison.OrdinalIgnoreCase) && !name.Contains(".min.", StringComparison.OrdinalIgnoreCase))
                .ToArray();
            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var resourceName in mjsResources.Where(name => name.Contains(baseNameSrc, StringComparison.OrdinalIgnoreCase)))
            {
                var mjs = ResourceLoader.GetShortResourceName(assembly, resourceBaseName, resourceName)
                    .Replace('.', '/')
                    .Replace("/mjs", ".mjs", StringComparison.OrdinalIgnoreCase)
                    .Replace("/esm.", ".esm.", StringComparison.OrdinalIgnoreCase);
                if (resources.Contains(resourceName.Replace(".mjs", ".min.mjs"), StringComparer.OrdinalIgnoreCase))
                {
                    mjs = mjs.Replace(".mjs", "{.min}.mjs", StringComparison.OrdinalIgnoreCase);
                }
                _extensions += Environment.NewLine + string.Format(JsmLinkTemplate, mjs);
            }

            var cssResources = resources
                .Where(name => name.EndsWith(".css", StringComparison.OrdinalIgnoreCase) && !name.Contains(".min.", StringComparison.OrdinalIgnoreCase))
                .ToArray();
            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var resourceName in cssResources.Where(name => name.Contains(baseNameSrc)))
            {
                var css = ResourceLoader.GetShortResourceName(assembly, resourceBaseName, resourceName)
                    .Replace(".", "/")
                    .Replace("/css", ".css");
                if (resources.Contains(resourceName.Replace(".css", ".min.css"), StringComparer.OrdinalIgnoreCase))
                {
                    css = css.Replace(".css", "{.min}.css");
                }
                _extensions += Environment.NewLine + string.Format(CssLinkTemplate, css);
            }
        }
    }
    
}