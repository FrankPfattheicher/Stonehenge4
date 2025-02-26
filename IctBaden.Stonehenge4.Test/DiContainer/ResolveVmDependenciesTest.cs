﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using IctBaden.Stonehenge.Core;
using IctBaden.Stonehenge.Hosting;
using IctBaden.Stonehenge.Resources;
using Xunit;

namespace IctBaden.Stonehenge.Test.DiContainer;

public sealed class ResolveVmDependenciesTest : IDisposable
{
    // ReSharper disable once MemberCanBePrivate.Global
    public Guid Id;

    private readonly StonehengeResourceLoader _loader;
    private readonly AppSession _session;

    public ResolveVmDependenciesTest()
    {
        Id = Guid.NewGuid();
        _loader = StonehengeResourceLoader.CreateDefaultLoader(StonehengeLogger.DefaultLogger, null);
        _loader.Services.AddService(typeof(ResolveVmDependenciesTest), this);
        _session = new AppSession(_loader, new StonehengeHostOptions(), new AppSessions());
    }

    public void Dispose()
    {
        _loader.Dispose();
        _session.Dispose();
    }

    [Fact]
    public async Task SimpleVmShouldGetReferenceToThisTest()
    {
        Id = Guid.NewGuid();
        await _loader.Get(_session, CancellationToken.None, "ViewModel/" + nameof(TestSimpleVmWithDependency), new Dictionary<string, string>(StringComparer.Ordinal));
        var vm = _session.ViewModel as TestSimpleVmWithDependency;
        Assert.NotNull(vm);
        Assert.Equal(Id, vm.Test.Id);
    }

    [Fact]
    public async Task ActiveVmShouldGetReferenceToThisTest()
    {
        Id = Guid.NewGuid();
        await _loader.Get(_session, CancellationToken.None, "ViewModel/" + nameof(TestActiveVmWithDependency), new Dictionary<string, string>(StringComparer.Ordinal));
        var vm = _session.ViewModel as TestActiveVmWithDependency;
        Assert.NotNull(vm);
        Assert.Equal(_session, vm.Session);
        Assert.Equal(Id, vm.Test.Id);
    }

}