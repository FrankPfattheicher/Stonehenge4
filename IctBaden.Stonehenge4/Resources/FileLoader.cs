using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using IctBaden.Stonehenge.Core;
using IctBaden.Stonehenge.Hosting;
using Microsoft.Extensions.Logging;

// ReSharper disable TemplateIsNotCompileTimeConstantProblem

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Local

namespace IctBaden.Stonehenge.Resources;

public sealed class FileLoader(ILogger logger, string path) : IStonehengeResourceProvider
{
    public string RootPath { get; private set; } = path;

    public void InitProvider(StonehengeResourceLoader loader, StonehengeHostOptions options)
    {
    }

    public IList<ViewModelInfo> GetViewModelInfos() => [];

    public void Dispose()
    {
    }

    public Task<Resource?> Get(AppSession? session, CancellationToken requestAborted, IStonehengeResourceProvider stonehengeResourceProvider,
        string resourceName, IDictionary<string, string> parameters)
    {
        var fullFileName = Path.Combine(RootPath, resourceName);
        if(!File.Exists(fullFileName)) return Task.FromResult<Resource?>(null);

        var resourceExtension = Path.GetExtension(resourceName);
        var resourceType = ResourceType.GetByExtension(resourceExtension);

        logger.LogTrace("FileLoader({ResourceName}): {FullFileName}", resourceName, fullFileName);
        return Task.FromResult<Resource?>(resourceType.IsBinary 
            ? new Resource(resourceName, "file://" + fullFileName, resourceType, File.ReadAllBytes(fullFileName), Resource.Cache.OneDay) 
            : new Resource(resourceName, "file://" + fullFileName, resourceType, File.ReadAllText(fullFileName), Resource.Cache.OneDay));
    }

    public Task<Resource?> Post(AppSession? session, string resourceName, IDictionary<string, string> parameters, IDictionary<string, string> formData) => Task.FromResult<Resource?>(null);
    public Task<Resource?> Put(AppSession? session, string resourceName, IDictionary<string, string> parameters, IDictionary<string, string> formData) => Task.FromResult<Resource?>(null);
    public Task<Resource?> Delete(AppSession? session, string resourceName, IDictionary<string, string> parameters, IDictionary<string, string> formData) => Task.FromResult<Resource?>(null);
}