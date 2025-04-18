﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using IctBaden.Stonehenge.Core;
using IctBaden.Stonehenge.Hosting;
using IctBaden.Stonehenge.Resources;
using Xunit;

namespace IctBaden.Stonehenge.Test.Resources;

public sealed class ResourceLoaderTests : IDisposable
{
    private readonly ResourceLoader _loader;
    private readonly AppSession _session = new();

    public ResourceLoaderTests()
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
        _loader = new ResourceLoader(StonehengeLogger.DefaultLogger, assemblies, Assembly.GetCallingAssembly());
    }

    public void Dispose()
    {
        _loader.Dispose();
    }

    // ReSharper disable InconsistentNaming

    [Fact]
    public async Task Load_resource_unknown_txt()
    {
        var resource = await _loader.Get(_session, CancellationToken.None, "unknown.txt", new Dictionary<string, string>(StringComparer.Ordinal));
        Assert.Null(resource);
    }

    [Fact]
    public async Task Load_resource_icon_png()
    {
        var resource = await _loader.Get(_session, CancellationToken.None, "icon.png", new Dictionary<string, string>(StringComparer.Ordinal));
        Assert.NotNull(resource);
        Assert.Equal("image/png", resource.ContentType);
        Assert.True(resource.IsBinary);
        Assert.NotNull(resource.Data);
        Assert.Equal(201, resource.Data.Length);
    }

    [Fact]
    public async Task Load_resource_icon32_png()
    {
        var resource = await _loader.Get(_session, CancellationToken.None, "icon32.png", new Dictionary<string, string>(StringComparer.Ordinal));
        Assert.NotNull(resource);
        Assert.Equal("image/png", resource.ContentType);
        Assert.True(resource.IsBinary);
        Assert.NotNull(resource.Data);
        Assert.Equal(354, resource.Data.Length);
    }

    [Fact]
    public async Task Load_resource_image_png()
    {
        var resource = await _loader.Get(_session, CancellationToken.None, "image.jpg", new Dictionary<string, string>(StringComparer.Ordinal));
        Assert.NotNull(resource);
        Assert.Equal("image/jpeg", resource.ContentType);
        Assert.True(resource.IsBinary);
        Assert.NotNull(resource.Data);
        Assert.Equal(1009, resource.Data.Length);
    }

    [Fact]
    public async Task Load_resource_test_html()
    {
        var resource = await _loader.Get(_session, CancellationToken.None, "test.html", new Dictionary<string, string>(StringComparer.Ordinal));
        Assert.NotNull(resource);
        Assert.Equal("text/html", resource.ContentType);
        Assert.False(resource.IsBinary);
        Assert.StartsWith("<!DOCTYPE html>", resource.Text, StringComparison.Ordinal);

        resource = await _loader.Get(_session, CancellationToken.None, "TesT.HTML", new Dictionary<string, string>(StringComparer.Ordinal));
        Assert.NotNull(resource);
        Assert.Equal("text/html", resource.ContentType);
        Assert.False(resource.IsBinary);
        Assert.StartsWith("<!DOCTYPE html>", resource.Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Load_resource_testscript_js()
    {
        var resource = await _loader.Get(_session, CancellationToken.None, "lib/testscript.js", new Dictionary<string, string>(StringComparer.Ordinal));
        Assert.NotNull(resource);
        Assert.Equal("text/javascript", resource.ContentType);
        Assert.False(resource.IsBinary);
        Assert.Contains("function Test()", resource.Text, StringComparison.Ordinal);
    }
}