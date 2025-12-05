using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using IctBaden.Stonehenge.Core;
using IctBaden.Stonehenge.Hosting;
using IctBaden.Stonehenge.Resources;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

// ReSharper disable TemplateIsNotCompileTimeConstantProblem

namespace IctBaden.Stonehenge.Kestrel.Middleware;

// ReSharper disable once ClassNeverInstantiated.Global
[SuppressMessage("Design", "MA0051:Method is too long")]
[SuppressMessage("ReSharper", "ReplaceSubstringWithRangeIndexer")]
[SuppressMessage("Security", "MA0009:Add regex evaluation timeout")]
[SuppressMessage("Performance", "MA0023:Add RegexOptions.ExplicitCapture")]
public partial class StonehengeSession
{
    private static readonly Regex StonehengeIdRegex = RegexStonehengeId();
    private readonly RequestDelegate _next;

    public StonehengeSession(RequestDelegate next)
    {
        _next = next;
    }

    // ReSharper disable once UnusedMember.Global
    public async Task Invoke(HttpContext context)
    {
        if (context.Items["stonehenge.Logger"] is not ILogger logger ||
            context.Items["stonehenge.AppSessions"] is not AppSessions appSessions)
        {
            Debugger.Break();
            return;
        }

        var timer = new Stopwatch();
        timer.Start();

        var path = context.Request.Path.ToString();

        if (path.ToLower(CultureInfo.InvariantCulture).Contains("/user/"))
        {
            logger.LogTrace("Kestrel Begin USER {Method} {Path}", context.Request.Method, path);
            await _next.Invoke(context).ConfigureAwait(false);
            logger.LogTrace("Kestrel End USER {Method} {Path}", context.Request.Method, path);
            return;
        }

        // Header id has first priority
        var stonehengeId = context.Request.Headers["X-Stonehenge-Id"].FirstOrDefault();
        if (string.IsNullOrEmpty(stonehengeId))
        {
            // URL id has second priority
            stonehengeId = context.Request.Query["stonehenge-id"];
        }

        if (string.IsNullOrEmpty(stonehengeId))
        {
            // see referer
            var referer = context.Request.Headers.Referer.FirstOrDefault() ?? string.Empty;
            stonehengeId = StonehengeIdRegex
                .Matches(referer)
                .Select(m => m.Groups[1].Value)
                .FirstOrDefault();
        }

        if (string.IsNullOrEmpty(stonehengeId))
        {
            var cookie = context.Request.Headers
                .FirstOrDefault(h => string.Equals(h.Key, "Cookie", StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrEmpty(cookie.Value.ToString()))
            {
                // workaround for double stonehenge-id values in cookie - take the last one
                var ids = StonehengeIdRegex
                    .Matches(cookie.Value.ToString())
                    .Select(m => m.Groups[1].Value).ToArray();
                if (ids.Length > 1)
                {
                    logger.LogError("Multiple Stonehenge Ids in cookie: {StonehengeIds}", string.Join(", ", ids));
                }

                if (ids.Length > 0)
                {
                    stonehengeId = ids.LastOrDefault(id => appSessions.GetAllSessions().Any(s => string.Equals(s.Id, id, StringComparison.Ordinal)));
                }
            }
        }

        logger.LogTrace("Kestrel[{StonehengeId}] Begin {Method} {Path}{QueryString}",
            stonehengeId ?? "<none>", context.Request.Method, path, context.Request.QueryString);

        CleanupTimedOutSessions(logger, appSessions);
        var session = appSessions.GetSessionById(stonehengeId);
        if (session == null)
        {
            var stonehengeNonce = context.Request.Query["stonehenge-nonce"].FirstOrDefault();
            session = appSessions.GetSessionByNonce(stonehengeNonce);
        }
        
        if (session == null && path.StartsWith("/ViewModel", StringComparison.OrdinalIgnoreCase))
        {
            // session not found
            var resourceLoader = context.Items["stonehenge.ResourceLoader"] as StonehengeResourceLoader;
            var directoryName = Path.GetDirectoryName(path) ?? "/";
            var resource = resourceLoader != null
                ? await resourceLoader.Get(null, context.RequestAborted, path.Substring(1).Replace('/', '.'),
                        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase))
                    .ConfigureAwait(false)
                : null;
            if (directoryName.Length > 1 && resource == null && stonehengeId != null)
            {
                logger.LogTrace("Kestrel[{StonehengeId}] Abort {Method} {Path}{QueryString}", 
                    stonehengeId, context.Request.Method, path, context.Request.QueryString);
                return;
            }

            if (directoryName.Length <= 1 || resource == null || string.Equals(directoryName, "\\ViewModel", StringComparison.OrdinalIgnoreCase))
            {
                // redirect to new session
#pragma warning disable IDISP001
                session?.Dispose();
                session = NewSession(logger, context, resourceLoader, appSessions);
#pragma warning restore IDISP001
                context.Response.Headers.Append("X-Stonehenge-id", new StringValues(session.Id));
                // context.Response.Headers.Append("Set-Cookie", new StringValues("stonehenge-id=" + session.Id));
                
                session.Nonce = Guid.NewGuid().ToString();

                var redirectUrl = "/index.html";
                var query = HttpUtility.ParseQueryString(context.Request.QueryString.ToString());
                query.Remove("stonehenge-nonce");
                query.Add("stonehenge-nonce", session.Nonce);
                query.Remove("stonehenge-id");
                redirectUrl += $"?{query}";

                context.Response.Redirect(redirectUrl);

                var remoteHost = context.Connection.RemoteIpAddress?.ToString() ?? string.Empty;
                var forwarded = context.Request.Headers["X-Forwarded-Host"].FirstOrDefault() ?? string.Empty;
                if (!string.IsNullOrEmpty(forwarded))
                {
                    forwarded = $"(forwarded from {forwarded}) ";
                }

                var remotePort = context.Connection.RemotePort;
                logger.LogTrace("Kestrel[{StonehengeId}] From {RemoteHost}:{RemotePort} {Forwarded}- redirect to {SessionId}",
                    stonehengeId ?? "<none>", remoteHost, remotePort, forwarded, session.Id);
                return;
            }
        }

        var etag = context.Request.Headers.IfNoneMatch.ToString();
        if (string.Equals(context.Request.Method, "GET", StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrEmpty(etag) &&
            string.Equals(etag, AppSession.GetResourceETag(path), StringComparison.Ordinal))
        {
            logger.LogTrace("ETag match");
            context.Response.StatusCode = (int)HttpStatusCode.NotModified;
        }
        else
        {
            context.Items.Add("stonehenge.AppSession", session);
            await _next.Invoke(context).ConfigureAwait(false);
        }

        timer.Stop();

        if (context.RequestAborted.IsCancellationRequested)
        {
            logger.LogTrace("Kestrel[{StonehengeId}] Canceled {Method}={StatusCode} {Path}, {ElapsedMilliseconds}ms",
                stonehengeId ?? "<none>", context.Request.Method, context.Response.StatusCode, path,
                timer.ElapsedMilliseconds);
            throw new TaskCanceledException();
        }

        logger.LogTrace("Kestrel[{StonehengeId}] End {Method}={StatusCode} {Path}, {ElapsedMilliseconds}ms",
            stonehengeId ?? "<none>", context.Request.Method, context.Response.StatusCode, path,
            timer.ElapsedMilliseconds);
    }

    private static void CleanupTimedOutSessions(ILogger logger, AppSessions appSessions)
    {
        var timedOutSessions = appSessions.GetTimedOutSessions();
        foreach (var session in timedOutSessions)
        {
            var vm = session.ViewModel as IDisposable;
#pragma warning disable IDISP007
            vm?.Dispose();
#pragma warning restore IDISP007
            session.ViewModel = null;

            appSessions.RemoveSessionById(session.Id);

            logger.LogInformation("Kestrel Session timed out {SessionId}", session.Id);
            session.Dispose();
        }

        if (timedOutSessions.Length != 0)
        {
            logger.LogInformation("Kestrel {Count} sessions", appSessions.Count);
        }
    }

    private static AppSession NewSession(ILogger logger, HttpContext context, StonehengeResourceLoader? resourceLoader,
        AppSessions appSessions)
    {
        var options = context.Items["stonehenge.HostOptions"] as StonehengeHostOptions ?? new StonehengeHostOptions();
        var session = new AppSession(resourceLoader, options, appSessions);
        var isLocal = context.IsLocal();
        var userAgent = context.Request.Headers.UserAgent.ToString();
        var userLanguages = context.Request.Headers.AcceptLanguage.ToString();
        if (options.UseClientLocale)
        {
            session.SetSessionCulture(GetCulture(userLanguages));
        }

        var httpContext = context.Request.HttpContext;
        var clientAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty;
        var clientPort = httpContext.Connection.RemotePort;
        var hostDomain = context.Request.Host.Value;
        var hostUrl = $"{context.Request.Scheme}://{hostDomain}";
        session.Initialize(options, hostUrl, hostDomain, isLocal, clientAddress, clientPort, userAgent);
        appSessions.AddSession(session);
        logger.LogInformation("Kestrel New session {SessionId}. {Count} sessions", session.Id, appSessions.Count);
        return session;
    }

    private static CultureInfo GetCulture(string languages)
    {
        if (string.IsNullOrEmpty(languages)) return CultureInfo.CurrentCulture;

        foreach (var language in languages.Split(';'))
        {
            var realLanguage = RegexRealLanguage().Replace(language, "");
            var locale = realLanguage.Split(',').FirstOrDefault();
            //first one should be the used language that is set for a browser (if user did not change it their self).
            if (locale != null)
            {
                return new CultureInfo(locale);
            }
        }

        return CultureInfo.CurrentCulture;
    }

    [GeneratedRegex("stonehenge-id=([a-f0-9A-F]+)", RegexOptions.RightToLeft | RegexOptions.Compiled)]
    private static partial Regex RegexStonehengeId();
    [GeneratedRegex("[;q=(0-9).]", RegexOptions.Compiled)]
    private static partial Regex RegexRealLanguage();
}