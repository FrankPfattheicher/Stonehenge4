﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using IctBaden.Stonehenge.Core;
using IctBaden.Stonehenge.Hosting;
using IctBaden.Stonehenge.Resources;
using IctBaden.Stonehenge.Types;
using IctBaden.Stonehenge.Vue.Client;
using Microsoft.Extensions.Logging;

// ReSharper disable TemplateIsNotCompileTimeConstantProblem

// ReSharper disable MemberCanBePrivate.Global

namespace IctBaden.Stonehenge.Vue;

[SuppressMessage("Security", "MA0009:Add regex evaluation timeout")]
[SuppressMessage("Performance", "SYSLIB1045:In „GeneratedRegexAttribute“ konvertieren.")]
[SuppressMessage("ReSharper", "ReplaceSubstringWithRangeIndexer")]
[SuppressMessage("Performance", "MA0023:Add RegexOptions.ExplicitCapture")]
public sealed partial class VueResourceProvider : IStonehengeResourceProvider
{
    private readonly ILogger _logger;
    private readonly Dictionary<string, Resource> _vueContent = new(StringComparer.Ordinal);
    private List<Assembly> _assemblies;
    private Assembly _appAssembly = Assembly.GetExecutingAssembly();
    private readonly List<ViewModelInfo> _viewModels;

    public VueResourceProvider(ILogger logger)
    {
        _logger = logger;
        _assemblies = new List<Assembly?>
            {
                Assembly.GetEntryAssembly(),
                Assembly.GetAssembly(GetType())
            }
            .Concat(AppDomain.CurrentDomain.GetAssemblies())
            .Where(a => a != null)
            .Distinct()
            .Cast<Assembly>()
            .ToList();
        _viewModels = new List<ViewModelInfo>();
    }

    public void InitProvider(StonehengeResourceLoader loader, StonehengeHostOptions options)
    {
        _vueContent.Clear();

        if (loader.Providers
                .FirstOrDefault(p => p is ResourceLoader) is ResourceLoader resourceLoader)
        {
            _assemblies = resourceLoader.ResourceAssemblies.ToList();
            _appAssembly = resourceLoader.AppAssembly;
        }

        var appCreator = new VueAppCreator(_logger, loader, options, _appAssembly, _vueContent);

        AddFileSystemContent(options.AppFilesPath);
        AddResourceContent();

        var contentPages = _vueContent
            .Where(res => string.IsNullOrEmpty(res.Value.ViewModel?.ElementName))
            .Select(res => res.Value.ViewModel)
            .OfType<ViewModelInfo>()
            .OrderBy(vmInfo => Math.Abs(vmInfo.SortIndex))
            .ToArray();
        AppPages.SetPages(contentPages);

        appCreator.CreateApplication(contentPages);
        appCreator.CreateComponents(loader);
    }

    public IList<ViewModelInfo> GetViewModelInfos() => _viewModels;

    public void Dispose()
    {
        _vueContent.Clear();
    }

    private static readonly Regex ExtractName = RegexExtractName();
    private static readonly Regex ExtractElement = RegexExtractElement();
    private static readonly Regex ExtractTitle = RegexExtractTitle();

    [SuppressMessage("Design", "MA0051:Method is too long")]
    private ViewModelInfo GetViewModelInfo(string route, string pageText)
    {
        if (route.Length > 1)
            route = string.Concat(char.ToUpper(route[0], CultureInfo.InvariantCulture).ToString(), route.AsSpan(1));
        var info = new ViewModelInfo(route.ToLower(CultureInfo.InvariantCulture), route + "Vm");

        var match = ExtractElement.Match(pageText);
        if (match.Success)
        {
            info.ElementName = NamingConverter.SnakeToPascalCase(route);
            if (!string.IsNullOrEmpty(match.Groups[2].Value))
            {
                info.Bindings = match.Groups[2].Value
                    .Split(',')
                    .Select(b => b.Trim())
                    .ToList();
            }

            match = ExtractName.Match(pageText);
            info.VmName = match.Success
                ? match.Groups[1].Value
                : string.Empty;
            info.SortIndex = 0;
        }
        else
        {
            match = ExtractName.Match(pageText);
            if (match.Success)
            {
                info.VmName = match.Groups[1].Value;
            }

            match = ExtractTitle.Match(pageText);
            if (match.Success)
            {
                info.Title = match.Groups[1].Value;
                info.SortIndex = string.IsNullOrEmpty(match.Groups[3].Value)
                    ? 0
                    : int.Parse(match.Groups[3].Value, NumberStyles.Number, CultureInfo.InvariantCulture);
            }
            else
            {
                info.Title = route;
            }

            info.TitleId = info.Title;
        }

        info.Visible = info.SortIndex > 0;
        info.SortIndex = Math.Abs(info.SortIndex);

        info.I18Names = I18nRegex().Matches(pageText).Select(m => m.Groups[1].Value).ToList();
        if (pageText.Contains("<app-message-box", StringComparison.OrdinalIgnoreCase) ||
            pageText.Contains("<app-dialog", StringComparison.OrdinalIgnoreCase))
        {
            info.I18Names.AddRange(["BtnOK", "BtnClose", "BtnCancel"]);
        }

        if (!string.IsNullOrEmpty(info.VmName))
        {
            _viewModels.Add(info);
        }

        return info;
    }

    private void AddFileSystemContent(string appFilesPath)
    {
        _logger.LogInformation("VueResourceProvider: Using file system app path {AppFilesPath}", appFilesPath);
        if (Directory.Exists(appFilesPath))
        {
            var appPath = Path.DirectorySeparatorChar + "app" + Path.DirectorySeparatorChar;
            var appFiles = Directory.GetFiles(appFilesPath, "*.html", SearchOption.AllDirectories);

            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var appFile in appFiles)
            {
                var resourceId = appFile.Substring(appFile.IndexOf(appPath, StringComparison.InvariantCulture) + 5)
                    .Replace('@', '_')
                    .Replace('-', '_')
                    .Replace('\\', '/');
                var route = resourceId.Replace(".html", string.Empty, StringComparison.OrdinalIgnoreCase);
                var pageText = File.ReadAllText(appFile);

                var resource = new Resource(route, appFile, ResourceType.Html, pageText, Resource.Cache.OneDay)
                    { ViewModel = GetViewModelInfo(route, pageText) };
                _vueContent.Add(resourceId, resource);
            }
        }
    }

    private void AddResourceContent()
    {
        foreach (var assembly in _assemblies)
        {
            foreach (var resourceName in assembly.GetManifestResourceNames()
                         .Where(name =>
                             name.EndsWith(".html", StringComparison.OrdinalIgnoreCase) &&
                             !name.Contains("index.html", StringComparison.OrdinalIgnoreCase) &&
                             !name.Contains("src.app.html", StringComparison.OrdinalIgnoreCase))
                         .Order(StringComparer.Ordinal))
            {
                var resourceId = GetResourceId(resourceName);
                if (_vueContent.ContainsKey(resourceId))
                {
                    _logger.LogWarning(
                        "VueResourceProvider.AddResourceContent: Resource with id {ResourceId} already exits",
                        resourceId);
                    continue;
                }

                var route = resourceId.Replace(".html", string.Empty, StringComparison.OrdinalIgnoreCase);
                var pageText = string.Empty;
                using (var stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream != null)
                    {
                        // ReSharper disable once ConvertToUsingDeclaration
                        using (var reader = new StreamReader(stream))
                        {
                            pageText = reader.ReadToEnd();
                        }
                    }
                }

                try
                {
                    var resource = new Resource(route, "res://" + resourceName, ResourceType.Html, pageText,
                        Resource.Cache.Revalidate)
                    {
                        ViewModel = GetViewModelInfo(route, pageText)
                    };
                    var key = "src." + resourceId;
                    _vueContent.TryAdd(key, resource);
                }
                catch (Exception ex)
                {
                    var message = ex.Message;
                    if (ex.InnerException != null)
                    {
                        message += Environment.NewLine + ex.InnerException.Message;
                    }
                    _logger.LogCritical(ex, "VueResourceProvider.AddResourceContent: Failed to add {ResourceName}: {Message}", 
                        resourceName, message);
                }
            }
        }
    }

    private string GetResourceId(string resourceName) =>
        ResourceLoader.GetShortResourceName(_appAssembly, ".app.", resourceName)
            .Replace('@', '_')
            .Replace('-', '_')
            .Replace("._0", ".0")
            .Replace("._1", ".1")
            .Replace("._2", ".2")
            .Replace("._3", ".3")
            .Replace("._4", ".4")
            .Replace("._5", ".5")
            .Replace("._6", ".6")
            .Replace("._7", ".7")
            .Replace("._8", ".8")
            .Replace("._9", ".9", StringComparison.Ordinal);


    public Task<Resource?> Post(AppSession? session, string resourceName, IDictionary<string, string> parameters,
        IDictionary<string, string> formData) => Task.FromResult<Resource?>(null);

    public Task<Resource?> Put(AppSession? session, string resourceName, IDictionary<string, string> parameters,
        IDictionary<string, string> formData) => Task.FromResult<Resource?>(null);

    public Task<Resource?> Delete(AppSession? session, string resourceName, IDictionary<string, string> parameters,
        IDictionary<string, string> formData) => Task.FromResult<Resource?>(null);

    public Task<Resource?> Get(AppSession? session, CancellationToken requestAborted, string resourceName,
        IDictionary<string, string> parameters)
    {
        resourceName = resourceName
            .Replace('/', '.')
            .Replace('@', '_')
            .Replace('-', '_');
        return _vueContent.TryGetValue(resourceName, out var value)
            ? Task.FromResult<Resource?>(value)
            : Task.FromResult<Resource?>(null);
    }

    [GeneratedRegex("<!--ViewModel:(\\w+)-->", RegexOptions.Compiled)]
    private static partial Regex RegexExtractName();

    [GeneratedRegex("<!--CustomElement(:([\\w, ]+))?-->", RegexOptions.Compiled)]
    private static partial Regex RegexExtractElement();

    [GeneratedRegex("<!--Title:([^:]*)(:(-?\\d*))?-->", RegexOptions.Compiled)]
    private static partial Regex RegexExtractTitle();

    [GeneratedRegex("I18n\\.([a-zA-Z0-9]+)")]
    // ReSharper disable once InconsistentNaming
    private static partial Regex I18nRegex();
}