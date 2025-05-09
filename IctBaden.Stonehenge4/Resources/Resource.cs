﻿// ReSharper disable AutoPropertyCanBeMadeGetOnly.Local

using System;
using System.Diagnostics;

namespace IctBaden.Stonehenge.Resources;

[DebuggerDisplay("{Name}")]
public class Resource
{
    public string ContentType { get; private set; }
    public string Name { get; private init; }
    public string Source { get; private set; }

    public bool IsBinary => Data != null;
    public bool IsNoContent => string.IsNullOrEmpty(Name) && string.IsNullOrEmpty(Source);

    public enum Cache
    {
        None,
        Revalidate,
        OneDay
    };
    /// <summary>
    /// Is allowed to be cached at the client.
    /// </summary>
    public Cache CacheMode { get; private set; }

    public byte[]? Data { get; }
    public string? Text { get; set; }

    public ViewModelInfo? ViewModel { get; init; }

        
    public static readonly Resource NoContent = new Resource("", "", ResourceType.Text, Cache.None);

    public Resource(string name, string source, ResourceType type, string text, Cache cacheMode)
        : this(name, source, type, cacheMode)
    {
        if (type.IsBinary)
        {
            throw new ArgumentException(@"Resource " + name + @" is expected as text", nameof(text));
        }
        Text = text;
    }
    public Resource(string name, string source, ResourceType type, byte[] data, Cache cacheMode)
        : this(name, source, type, cacheMode)
    {
        if (!type.IsBinary)
        {
            throw new ArgumentException(@"Resource " + name + @" is expected as binary", nameof(data));
        }
        Data = data;
    }

    private Resource(string name, string source, ResourceType type, Cache cacheMode)
    {
        Name = name;
        Source = source;
        ContentType = type.ContentType;
        CacheMode = cacheMode;
    }

    internal void SetCacheMode(Cache mode)
    {
        CacheMode = mode;
    }
}