using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using IctBaden.Stonehenge.Core;
using IctBaden.Stonehenge.Hosting;
using IctBaden.Stonehenge.Resources;
using Xunit;

namespace IctBaden.Stonehenge.Test.Resources;

public sealed class LoaderTests : IDisposable
{
    private readonly StonehengeResourceLoader _loader;
    private readonly AppSession _session = new();

    private readonly FileLoaderTests _fileTest;

    public LoaderTests()
    {
        var assemblies = new List<Assembly?>
            {
                Assembly.GetAssembly(typeof(ResourceLoader)),
                Assembly.GetExecutingAssembly(),
                Assembly.GetCallingAssembly()
            }
            .Where(a => a != null)
            .Distinct()
            .Cast<Assembly>()
            .ToList();
#pragma warning disable IDISP001
        var resLoader = new ResourceLoader(StonehengeLogger.DefaultLogger, assemblies, Assembly.GetCallingAssembly());
        var fileLoader = new FileLoader(StonehengeLogger.DefaultLogger, Path.GetTempPath());
#pragma warning restore IDISP001

        _loader = new StonehengeResourceLoader(StonehengeLogger.DefaultLogger, [
            fileLoader,
            resLoader
        ]);

        _fileTest = new FileLoaderTests();
    }

    public void Dispose()
    {
        _fileTest.Dispose();
        _loader.Dispose();
    }

    // ReSharper disable InconsistentNaming

    [Fact]
    public async Task Load_from_file_icon_png()
    {
        var name = $"icon_{Guid.NewGuid():N}.png";
        _fileTest.CreateBinaryFile(name);
        var resource = await _loader.Get(_session, CancellationToken.None, name, new Dictionary<string, string>());
        Assert.NotNull(resource);
        Assert.Equal("image/png", resource.ContentType);
        Assert.True(resource.IsBinary);
        Assert.Equal(16, resource.Data!.Length);
        Assert.StartsWith("file://", resource.Source);
    }

    [Fact]
    public async Task Load_from_resource_icon_png()
    {
        var resource = await _loader.Get(_session, CancellationToken.None, "image.jpg", new Dictionary<string, string>());
        Assert.NotNull(resource);
        Assert.Equal("image/jpeg", resource.ContentType);
        Assert.True(resource.IsBinary);
        Assert.Equal(1009, resource.Data!.Length);
        Assert.StartsWith("res://", resource.Source);
    }

    [Fact]
    public async Task Load_from_file_over_resource_icon_png()
    {
        var name = $"index_{Guid.NewGuid():N}.html";
        _fileTest.CreateTextFile(name);
        var resource = await _loader.Get(_session, CancellationToken.None, name, new Dictionary<string, string>());
        Assert.NotNull(resource);
        Assert.Equal("text/html", resource.ContentType);
        Assert.False(resource.IsBinary);
        Assert.StartsWith("<!DOCTYPE html>", resource.Text);
        Assert.StartsWith("file://", resource.Source);
    }


}