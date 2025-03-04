﻿using System;
using System.Linq;
using System.Text.Json;
using IctBaden.Stonehenge.Core;
using IctBaden.Stonehenge.Hosting;
using IctBaden.Stonehenge.Kestrel.Middleware;
using IctBaden.Stonehenge.Resources;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace IctBaden.Stonehenge.Kestrel;

public class Startup : IStartup
{
    private readonly string _appTitle;
    private readonly ILogger _logger;
    private readonly IStonehengeResourceProvider _resourceLoader;
    private readonly AppSessions _appSessions;
    private readonly StonehengeHostOptions? _options;

    // ReSharper disable once UnusedMember.Global
    public Startup(ILogger logger, IConfiguration configuration, IStonehengeResourceProvider resourceLoader, AppSessions appSessions)
    {
        Configuration = configuration;
        _logger = logger;
        _resourceLoader = resourceLoader;
        _appSessions = appSessions;
        _appTitle = Configuration["AppTitle"] ?? string.Empty;
        var json = Configuration["HostOptions"] ?? "{}";
        _options = JsonSerializer.Deserialize<StonehengeHostOptions>(json);
    }

    // ReSharper disable once MemberCanBePrivate.Global
    public IConfiguration Configuration { get; }

    // This method gets called by the runtime. Use this method to add services to the container.
    // ReSharper disable once UnusedMember.Global
    public IServiceProvider ConfigureServices(IServiceCollection services)
    {
        services.AddResponseCompression(options =>
        {
            options.Providers.Add<GzipCompressionProvider>();
        });
        services.AddCors(o => o.AddPolicy("StonehengePolicy", builder =>
        {
            builder.AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader();
        }));
#pragma warning disable IDISP005
        return services.BuildServiceProvider();
#pragma warning restore IDISP005
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app)
    {
        app.UseMiddleware<ServerExceptionLogger>();
        app.UseMiddleware<StonehengeAcme>();
        app.Use((context, next) =>
        {
            context.Items.Add("stonehenge.Logger", _logger);
            context.Items.Add("stonehenge.AppSessions", _appSessions);
            context.Items.Add("stonehenge.AppTitle", _appTitle);
            context.Items.Add("stonehenge.HostOptions", _options);
            context.Items.Add("stonehenge.ResourceLoader", _resourceLoader);
            return next.Invoke();
        });
        if (_options?.CustomMiddleware != null)
        {
            foreach (var cm in _options.CustomMiddleware)
            {
                var cmType = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(a => a.GetTypes())
                    .FirstOrDefault(type => string.Equals(type.Name, cm, StringComparison.Ordinal));
                if (cmType != null)
                {
                    app.UseMiddleware(cmType);
                }    
            }
        }
        app.UseResponseCompression();
        app.UseCors("StonehengePolicy");
        app.UseMiddleware<StonehengeSession>();
        app.UseMiddleware<StonehengeHeaders>();
        app.UseMiddleware<StonehengeRoot>();
        app.UseMiddleware<StonehengeContent>();
    }
}