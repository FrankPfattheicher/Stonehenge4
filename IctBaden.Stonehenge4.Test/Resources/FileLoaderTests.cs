﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using IctBaden.Stonehenge.Core;
using IctBaden.Stonehenge.Hosting;
using IctBaden.Stonehenge.Resources;
using Microsoft.Extensions.Logging;
using Xunit;

namespace IctBaden.Stonehenge.Test.Resources;

public sealed class FileLoaderTests : IDisposable
{
    private readonly ILogger _logger = StonehengeLogger.DefaultLogger;
    private readonly FileLoader _loader;
    private readonly AppSession _session = new();
    private string? _fullFileName;

    public FileLoaderTests()
    {
        var path = Path.GetTempPath();
        _loader = new FileLoader(_logger, path);
    }

    public void Dispose()
    {
        if (_fullFileName != null && File.Exists(_fullFileName))
        {
            File.Delete(_fullFileName);
        }
        _loader.Dispose();
    }

    internal void CreateTextFile(string name)
    {
        _fullFileName = Path.Combine(_loader.RootPath, name);
        try
        {
            using var file = File.CreateText(_fullFileName);
            file.Write("<!DOCTYPE html>" + Environment.NewLine + "<h1>Testfile</h1>");
            file.Close();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, nameof(CreateTextFile));
        }
    }

    internal void CreateBinaryFile(string name)
    {
        _fullFileName = Path.Combine(_loader.RootPath, name);
        try
        {
            using var file = File.Create(_fullFileName);
            var data = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 };
            file.Write(data, 0, data.Length);
            file.Close();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, nameof(CreateBinaryFile));
        }
    }

    [Fact]
    public async Task Load_file_unknown_txt()
    {
        var resource = await _loader.Get(_session, CancellationToken.None, "unknown.txt", new Dictionary<string, string>(StringComparer.Ordinal));
        Assert.Null(resource);
    }

    [Fact]
    public async Task Load_file_icon_png()
    {
        var name = $"icon_{Guid.NewGuid():N}.png";
        CreateBinaryFile(name);
        var resource = await _loader.Get(_session, CancellationToken.None, name, new Dictionary<string, string>(StringComparer.Ordinal));
        Assert.NotNull(resource);
        Assert.Equal("image/png", resource.ContentType);
        Assert.True(resource.IsBinary);
        Assert.Equal(16, resource.Data!.Length);
    }

    [Fact]
    public async Task Load_file_index_html()
    {
        CreateTextFile("index.html");
        var resource = await _loader.Get(_session, CancellationToken.None, "index.html", new Dictionary<string, string>(StringComparer.Ordinal));
        Assert.NotNull(resource);
        Assert.Equal("text/html", resource.ContentType);
        Assert.False(resource.IsBinary);
        Assert.StartsWith("<!DOCTYPE html>", resource.Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Load_file_image_png()
    {
        var name = $"image_{Guid.NewGuid():N}.jpg";
        CreateBinaryFile(name);
        var resource = await _loader.Get(_session, CancellationToken.None, name, new Dictionary<string, string>(StringComparer.Ordinal));
        Assert.NotNull(resource);
        Assert.Equal("image/jpeg", resource.ContentType);
        Assert.True(resource.IsBinary);
        Assert.Equal(16, resource.Data!.Length);
    }

    [Fact]
    public async Task Load_file_test_html()
    {
        CreateTextFile("test.htm");
        var resource = await _loader.Get(_session, CancellationToken.None, "test.htm", new Dictionary<string, string>(StringComparer.Ordinal));
        Assert.NotNull(resource);
        Assert.Equal("text/html", resource.ContentType);
        Assert.False(resource.IsBinary);
        Assert.StartsWith("<!DOCTYPE html>", resource.Text, StringComparison.Ordinal);
    }

}