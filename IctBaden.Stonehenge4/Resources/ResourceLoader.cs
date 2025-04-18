﻿// ReSharper disable AutoPropertyCanBeMadeGetOnly.Local

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using IctBaden.Stonehenge.Client;
using IctBaden.Stonehenge.Core;
using IctBaden.Stonehenge.Hosting;
using Microsoft.Extensions.Logging;

// ReSharper disable TemplateIsNotCompileTimeConstantProblem

namespace IctBaden.Stonehenge.Resources;

[SuppressMessage("Design", "MA0051:Method is too long")]
[SuppressMessage("Security", "MA0009:Add regex evaluation timeout")]
[SuppressMessage("Performance", "MA0023:Add RegexOptions.ExplicitCapture")]
public sealed partial class ResourceLoader : IStonehengeResourceProvider
{
    public readonly IList<Assembly> ResourceAssemblies;
    private readonly ILogger _logger;

    /// <summary>
    /// Assembly containing the embedded application resources
    /// in the app folder.
    /// </summary>
    public readonly Assembly AppAssembly;
    private readonly Lazy<Dictionary<string, AssemblyResource>> _resources;

    public void Dispose()
    {
        ResourceAssemblies.Clear();
    }

    public ResourceLoader(ILogger logger, IEnumerable<Assembly> assembliesToUse, Assembly appAssembly)
    {
        ResourceAssemblies = assembliesToUse.ToList();
        _logger = logger;
        AppAssembly = appAssembly;

        _resources = new Lazy<Dictionary<string, AssemblyResource>>(
            () =>
            {
                var dict = new Dictionary<string, AssemblyResource>(StringComparer.Ordinal);
                foreach (var assembly in ResourceAssemblies.Distinct())
                {
                    AddAssemblyResources(assembly, dict);
                }
                return dict;
            });
    }

    public void InitProvider(StonehengeResourceLoader loader, StonehengeHostOptions options)
    {
    }

    public IList<ViewModelInfo> GetViewModelInfos() => new List<ViewModelInfo>();

    public static string GetShortResourceName(Assembly appAssembly, string baseName, string resourceName)
    {
        resourceName = resourceName.Replace(appAssembly.GetName().Name ?? "_", "_");
        var ixBase = resourceName.IndexOf(baseName, StringComparison.InvariantCultureIgnoreCase);
        return ixBase >= 0 
            ? resourceName.Substring(ixBase + baseName.Length) 
            : string.Empty;
    }

    private static readonly Regex RscName = RegexRscName();  
    public static string RemoveResourceProtocol(string resourceName)
    {
        var match = RscName.Match(resourceName);
        return match.Success 
            ? match.Groups[1].Value 
            : resourceName;
    }

    public void AddAssembly(Assembly assembly)
    {
        ResourceAssemblies.Add(assembly);

        var asmResources = _resources.Value;
        if (asmResources.Values.Any(res => res.Assembly == assembly)) 
            return;

        AddAssemblyResources(assembly, asmResources);
    }

    private void AddAssemblyResources(Assembly assembly, IDictionary<string, AssemblyResource> dict)
    {
        foreach (var resource in assembly.GetManifestResourceNames())
        {
            var shortName = GetShortResourceName(AppAssembly, ".app.", resource);
            if (string.IsNullOrEmpty(shortName))
            {
                continue;
            }
            var resourceId = shortName
                .Replace("@", "_")
                .Replace("-", "_")
                .Replace("._0", ".0")
                .Replace("._1", ".1")
                .Replace("._2", ".2")
                .Replace("._3", ".3")
                .Replace("._4", ".4")
                .Replace("._5", ".5")
                .Replace("._6", ".6")
                .Replace("._7", ".7")
                .Replace("._8", ".8")
                .Replace("._9", ".9");
            var asmResource = new AssemblyResource(resource, shortName, assembly);
            if (!dict.ContainsKey(resourceId))
            {
                _logger.LogDebug("ResourceLoader.AddAssemblyResources: Added {ShortName}", shortName);
                dict.Add(resourceId, asmResource);
            }
        }
    }

    private static string GetAssemblyResourceName(string name) => name
        .Replace('@', '_')
        .Replace('-', '_')
        .Replace('/', '.');


    public Task<Resource?> Get(AppSession? session, CancellationToken requestAborted, string name, IDictionary<string, string> parameters)
    {
        if (name.StartsWith("Events/", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult<Resource?>(null);
        }

        if (name.EndsWith(".js.map", StringComparison.OrdinalIgnoreCase) || name.EndsWith(".css.map", StringComparison.OrdinalIgnoreCase))
        {
            var map = new Resource(name, "auto", ResourceType.Json, "{}", Resource.Cache.OneDay);
            return Task.FromResult<Resource?>(map);
        }

        var resourceName = GetAssemblyResourceName(name);

        var asmResource = _resources.Value
            .FirstOrDefault(res => string.Compare(res.Key, resourceName, true, CultureInfo.InvariantCulture) == 0);

        if (asmResource.Key == null)
        {
            var shortName = GetShortResourceName(AppAssembly, ".app.", resourceName);
            asmResource = _resources.Value
                .FirstOrDefault(res => string.Compare(res.Key, shortName, true, CultureInfo.InvariantCulture) == 0);
        }
            
        if (asmResource.Key == null)
        {
            _logger.LogInformation("ResourceLoader({ResourceName}): not found", resourceName);
            return Task.FromResult<Resource?>(null);
        }

        var resourceExtension = Path.GetExtension(resourceName);
        var resourceType = ResourceType.GetByExtension(resourceExtension);

        using (var stream = asmResource.Value.Assembly.GetManifestResourceStream(asmResource.Value.FullName))
        {
            if (stream != null)
            {
                if (resourceType.IsBinary)
                {
                    // ReSharper disable once ConvertToUsingDeclaration
                    using (var reader = new BinaryReader(stream))
                    {
                        var data = reader.ReadBytes((int)stream.Length);
                        _logger.LogDebug("ResourceLoader({ResourceName}): {FullName}", resourceName, asmResource.Value.FullName);
                        return Task.FromResult<Resource?>(new Resource(resourceName, "res://" + asmResource.Value.FullName, resourceType, data, Resource.Cache.Revalidate));
                    }
                }

                // ReSharper disable once ConvertToUsingDeclaration
                using (var reader = new StreamReader(stream))
                {
                    var text = reader.ReadToEnd();
                    _logger.LogDebug("ResourceLoader({ResourceName}): {FullName}", resourceName, asmResource.Value.FullName);
                    if (resourceName.EndsWith("index.html", StringComparison.InvariantCultureIgnoreCase))
                    {
                        var theme = session?.SubDomain ?? string.Empty;
                        UserContentLinks.InitializeUserContentLinks(AppAssembly, ResourceAssemblies, string.Empty, theme);
                        text = UserContentLinks.InsertUserLinks(text,theme);
                    }
                    text = text.Replace("{.min}", (session?.IsDebug ?? false) ? "" : ".min");
                    return Task.FromResult<Resource?>(new Resource(resourceName, "res://" + asmResource.Value.FullName, resourceType, text, Resource.Cache.Revalidate));
                }
            }
        }

        _logger.LogInformation("ResourceLoader({ResourceName}): not found", resourceName);
        return Task.FromResult<Resource?>(null);
    }
    
    public Task<Resource?> Post(AppSession? session, string resourceName, IDictionary<string, string> parameters, IDictionary<string, string> formData) => Task.FromResult<Resource?>(null);
    public Task<Resource?> Put(AppSession? session, string resourceName, IDictionary<string, string> parameters, IDictionary<string, string> formData) => Task.FromResult<Resource?>(null);
    public Task<Resource?> Delete(AppSession? session, string resourceName, IDictionary<string, string> parameters, IDictionary<string, string> formData) => Task.FromResult<Resource?>(null);
    
    [GeneratedRegex(@"\w+://(.*)", RegexOptions.Compiled | RegexOptions.Singleline)]
    private static partial Regex RegexRscName();
}