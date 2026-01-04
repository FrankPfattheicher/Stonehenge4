using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using IctBaden.Stonehenge.Core;
using IctBaden.Stonehenge.Hosting;
using IctBaden.Stonehenge.ViewModel;
using Microsoft.Extensions.Logging;

// ReSharper disable TemplateIsNotCompileTimeConstantProblem

// ReSharper disable MemberCanBePrivate.Global

namespace IctBaden.Stonehenge.Resources;

public sealed class StonehengeResourceLoader(ILogger logger, IList<IStonehengeResourceProvider> loaders)
    : IStonehengeResourceProvider
{
    public readonly ILogger Logger = logger;
        
    public IList<IStonehengeResourceProvider> Providers { get; } = loaders;
    public readonly ServiceContainer Services = new();

    public void InitProvider(StonehengeResourceLoader loader, StonehengeHostOptions options)
    {
        foreach (var provider in Providers)
        {
            provider.InitProvider(loader, options);
        }
    }

    public IList<ViewModelInfo> GetViewModelInfos() => Providers
        .SelectMany(p => p.GetViewModelInfos()).ToList();

    public void Dispose()
    {
        foreach (var provider in Providers)
        {
            provider.Dispose();
        }
        Providers.Clear();
    }

    public async Task<Resource?> Get(AppSession? session, CancellationToken requestAborted, IStonehengeResourceProvider stonehengeResourceProvider,
        string resourceName, IDictionary<string, string> parameters)
    {
        var disableCache = false;

        if (resourceName.Contains("${", StringComparison.Ordinal) || resourceName.Contains("{{", StringComparison.Ordinal))
        {
            resourceName = ReplaceFields(session, resourceName);
            disableCache = true;
        }

        Resource? loadedResource = null;
        foreach (var loader in Providers)
        {
            try
            {
                loadedResource = await loader
                    .Get(session, requestAborted, this, resourceName, parameters)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.LogError("StonehengeResourceLoader.{Name}({ResourceName}) exception: {Message}\r\n{StackTrace}",
                    loader.GetType().Name, resourceName, ex.Message, ex.StackTrace);
                Debugger.Break();
            }
            if (loadedResource != null) break;
        }

        if (disableCache)
        {
            loadedResource?.SetCacheMode(Resource.Cache.None);
        }

        return loadedResource;
    }
        
    private string ReplaceFields(AppSession? session, string resourceName)
    {
        // support es6 format "${}"
        var replaced = string.Empty;
        while (resourceName.Length > 0)
        {
            var start = resourceName.IndexOf("${", StringComparison.InvariantCulture);
            var closing = 1;
            if (start == -1)
            {
                start = resourceName.IndexOf("{{", StringComparison.InvariantCulture);
                closing = 2;
            }
            if (start == -1)
            {
                replaced += resourceName;
                break;
            }
            replaced += resourceName.Substring(0, start);
            var field = resourceName.Substring(start + 2);
            resourceName = resourceName.Substring(start + 2);

            var end = field.IndexOf('}');
            field = field.Substring(0, end);

            if (session != null && session.Cookies.TryGetValue(field, out var fieldCookie))
            {
                replaced += fieldCookie;
            }

            resourceName = resourceName.Substring(end + closing);
        }
        return replaced;
    }

    public static StonehengeResourceLoader CreateDefaultLoader(ILogger logger, IStonehengeResourceProvider? provider)
    {
        return CreateDefaultLoader(logger, provider, Assembly.GetCallingAssembly());
    }
    public static StonehengeResourceLoader CreateDefaultLoader(ILogger logger, IStonehengeResourceProvider? provider, Assembly appAssembly)
    {
        var assemblies = new List<Assembly?>
            {
                appAssembly,
                Assembly.GetEntryAssembly(),
                Assembly.GetExecutingAssembly(),
                Assembly.GetAssembly(typeof(ResourceLoader))
            }
            .Where(a => a != null)
            .Distinct()
            .Cast<Assembly>()
            .ToList();

#pragma warning disable IDISP001
        
        var resLoader = new ResourceLoader(logger, assemblies, appAssembly);
        if (provider != null)
        {
            resLoader.AddAssembly(provider.GetType().Assembly);
        }

        var fileLoader = new FileLoader(logger, Path.Combine(AppContext.BaseDirectory, "app"));

        var viewModelCreator = new ViewModelProvider(logger);

#pragma warning restore IDISP001

        var loader = new StonehengeResourceLoader(logger, [fileLoader, resLoader, viewModelCreator]);
        if (provider != null)
        {
            loader.Providers.Add(provider);
        }

        return loader;
    }

    public void AddResourceAssembly(Assembly assembly)
    {
        var resourceLoader = Providers.FirstOrDefault(p => p is ResourceLoader) as ResourceLoader;
        resourceLoader?.AddAssembly(assembly);
    }

    public async Task<Resource?> Post(AppSession? session, string resourceName, IDictionary<string, string> parameters, IDictionary<string, string> formData)
    {
        foreach (var provider in Providers)
        {
            var resource = await provider.Post(session, resourceName, parameters, formData).ConfigureAwait(false);
            if (resource != null) return resource;
        }
        return null;
    }
    public async Task<Resource?> Put(AppSession? session, string resourceName, IDictionary<string, string> parameters, IDictionary<string, string> formData)
    {
        foreach (var provider in Providers)
        {
            var resource = await provider.Put(session, resourceName, parameters, formData).ConfigureAwait(false);
            if (resource != null) return resource;
        }
        return null;
    }
    public async Task<Resource?> Delete(AppSession? session, string resourceName, IDictionary<string, string> parameters, IDictionary<string, string> formData)
    {
        foreach (var provider in Providers)
        {
            var resource = await provider.Delete(session, resourceName, parameters, formData).ConfigureAwait(false);
            if (resource != null) return resource;
        }
        return null;
    }

}