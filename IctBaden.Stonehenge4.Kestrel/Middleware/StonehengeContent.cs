﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using HttpMultipartParser;
using IctBaden.Stonehenge.Core;
using IctBaden.Stonehenge.Hosting;
using IctBaden.Stonehenge.Resources;
using IctBaden.Stonehenge.ViewModel;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

// ReSharper disable TemplateIsNotCompileTimeConstantProblem

// ReSharper disable ConvertToUsingDeclaration

namespace IctBaden.Stonehenge.Kestrel.Middleware;

// ReSharper disable once ClassNeverInstantiated.Global
[SuppressMessage("Design", "MA0051:Method is too long")]
[SuppressMessage("ReSharper", "ReplaceSubstringWithRangeIndexer")]
public class StonehengeContent
{
    private static readonly object LockViews = new();
    private static readonly object LockEvents = new();
    private readonly RequestDelegate _next;

    // ReSharper disable once UnusedMember.Global
    public StonehengeContent(RequestDelegate next)
    {
        _next = next;
    }

    // ReSharper disable once UnusedMember.Global
    public Task Invoke(HttpContext context)
    {
        if (context.Request.Path.Value == null) return Task.CompletedTask;
            
        if (context.Request.Path.Value.Contains("/Events"))
        {
            lock (LockEvents)
            {
                return InvokeLocked(context);
            }
        }

        lock (LockViews)
        {
            return InvokeLocked(context);
        }
    }

    private async Task InvokeLocked(HttpContext context)
    {
        if (context.Request.Path.Value == null) return;

        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (context.Items["stonehenge.Logger"] is not ILogger logger)
        {
            Debugger.Break();
            return;
        }
        
        var path = context.Request.Path.Value.Replace("//", "/");
        try
        {
            var response = context.Response.Body;
            var resourceLoader = context.Items["stonehenge.ResourceLoader"] as IStonehengeResourceProvider;
            var resourceName = path.Substring(1);
            var appSession = context.Items["stonehenge.AppSession"] as AppSession;
            var requestVerb = context.Request.Method;
            var cookiesHeader = context.Request.Headers
                .FirstOrDefault(h => string.Equals(h.Key, HeaderNames.Cookie, StringComparison.Ordinal)).Value.ToString();
            var requestCookies = cookiesHeader
                .Split(';')
                .Select(s => s.Trim())
                .Select(s => s.Split('='));
            var cookies = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var cookie in requestCookies)
            {
                if (!cookies.ContainsKey(cookie[0]) && (cookie.Length > 1))
                {
                    cookies.Add(cookie[0], cookie[1]);
                }
            }

            var queryString = HttpUtility.ParseQueryString(context.Request.QueryString.ToString());
            var parameters = queryString.AllKeys
                .Where(k => !string.IsNullOrEmpty(k))
                .ToDictionary(key => key!, key => queryString[key]!, StringComparer.Ordinal);
            
            Resource? content = null;

            appSession?.SetParameters(parameters);
            if ((appSession?.UseBasicAuth ?? false) && !CheckBasicAuthFromContext(appSession, context))
            {
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                context.Response.Headers.Append("WWW-Authenticate", "Basic");
                return;
            }

            if (appSession?.HostOptions.UseKeycloakAuthentication != null
                && appSession.RequestLogin
                && !context.Request.Path.Value.Contains("/Events"))
            {
                var o = appSession.HostOptions.UseKeycloakAuthentication;
                var requestQuery =
                    HttpUtility.ParseQueryString(context.Request.QueryString.ToString());

                var state = requestQuery["state"] ?? "";
                if (state.StartsWith(appSession.Id, StringComparison.Ordinal))
                {
                    var code = requestQuery["code"];
                    var data = $"grant_type=authorization_code&client_id={o.ClientId}&code={code}&redirect_uri={HttpUtility.UrlEncode(appSession.AuthorizeRedirectUrl)}";

#pragma warning disable IDISP014
                    using var client = new HttpClient();
#pragma warning restore IDISP014
                    var tokenUrl = $"{o.AuthUrl}/realms/{o.Realm}/protocol/openid-connect/token";
                    using var authParams = new StringContent(data, Encoding.UTF8, "application/x-www-form-urlencoded");
                    using var result = client.PostAsync(tokenUrl, authParams, context.RequestAborted).Result;
                    var json = result.Content.ReadAsStringAsync(context.RequestAborted).Result;
                    var authResponse = JsonSerializer.Deserialize<JsonObject>(json);
                    if (authResponse != null)
                    {
                        appSession.AccessToken = authResponse["id_token"]?.ToString() ?? string.Empty;
                        if (string.IsNullOrEmpty(appSession.AccessToken))
                        {
                            appSession.AccessToken = authResponse["access_token"]?.ToString() ?? string.Empty;
                        }

                        appSession.RefreshToken = authResponse["refresh_token"]?.ToString() ?? string.Empty;

                        if (!string.IsNullOrEmpty(appSession.AccessToken))
                        {
                            var handler = new JwtSecurityTokenHandler();
                            var jwtToken = handler.ReadToken(appSession.AccessToken) as JwtSecurityToken;
                            var identityId = jwtToken?.Subject ?? string.Empty;
                            var identityName = jwtToken?.Payload["name"]?.ToString() ?? string.Empty;
                            var identityMail = jwtToken?.Payload["email"]?.ToString() ?? string.Empty;
                            appSession.SetUser(identityName, identityId, identityMail);

                            // remove login redirect parameters
                            appSession.Parameters.Remove("ts");
                            appSession.Parameters.Remove("state");
                            appSession.Parameters.Remove("session_state");
                            appSession.Parameters.Remove("iss");
                            appSession.Parameters.Remove("code");

                            var uri = new Uri(appSession.AuthorizeRedirectUrl);
                            var query = string.Join('&', appSession.Parameters.Select(p => $"{p.Key}={p.Value}"));
                            var navigate = $"{uri.Scheme}://{uri.AbsolutePath}?{query}";
                            (appSession.ViewModel as ActiveViewModel)?.NavigateTo(navigate);
                        }
                    }

                    logger.LogTrace("Auth result: {Result}", result);
                }
                else
                {
                    var newSession = $"{context.Request.Scheme}://{context.Request.Host.Value}{context.Request.Path}?stonehenge-id=new";
                    context.Response.Redirect(newSession);
                    return;
                }
            }

            if (appSession is { HostOptions.UseKeycloakAuthentication: null }
                && string.IsNullOrEmpty(appSession.UserIdentity))
            {
                SetUserNameFromContext(appSession, context);
            }

            if (appSession?.SessionCulture != null)
            {
                Thread.CurrentThread.CurrentCulture = appSession.SessionCulture;
                Thread.CurrentThread.CurrentUICulture = appSession.SessionCulture;
            }

            switch (requestVerb)
            {
                case "GET":
                    appSession?.Accessed(cookies, false);
                    content = resourceLoader != null
                        ? await resourceLoader.Get(appSession, context.RequestAborted, resourceName, parameters).ConfigureAwait(false) 
                        : null;
                    var isIndex = resourceName.EndsWith("index.html", StringComparison.InvariantCultureIgnoreCase);
                    if (content == null && appSession != null && isIndex)
                    {
                        logger.LogError("Invalid path in index resource {ResourceName} - redirecting to root index", resourceName);

                        var query = HttpUtility.ParseQueryString(context.Request.QueryString.ToString());
                        query["stonehenge-id"] = appSession.Id;
                        context.Response.Redirect($"/index.html?{query}");
                        return;
                    }

                    if (content != null && isIndex)
                    {
                        if (appSession != null && context.Request.Query.ContainsKey("state"))
                        {
                            // remove login redirect parameters
                            appSession.Parameters.Remove("ts");
                            appSession.Parameters.Remove("state");
                            appSession.Parameters.Remove("session_state");
                            appSession.Parameters.Remove("iss");
                            appSession.Parameters.Remove("code");

                            var url = "/index.html";
                            var query = string.Join('&', appSession.Parameters.Select(p => $"{p.Key}={p.Value}"));
                            if (!string.IsNullOrEmpty(query))
                            {
                                url += "?" + query;
                            }
                            context.Response.Redirect(url);
                            return;
                        }

                        HandleIndexContent(context, content);
                    }
                    if (content != null && !isIndex)
                    {
                        appSession?.SetTimeout(appSession.HostOptions.SessionTimeout);
                    }
                    break;

                case "POST":
                case "PUT":
                case "PATCH":
                case "DELETE":
                    appSession?.Accessed(cookies, true);

                    try
                    {
                        var formData = new Dictionary<string, string>(StringComparer.Ordinal);
                        context.Request.EnableBuffering();
                        using var bodyReader = new StreamReader(context.Request.Body); 
                        var body = bodyReader.ReadToEndAsync().Result;
                        if (body.StartsWith('{'))
                        {
                            try
                            {
                                var jsonObject = JsonSerializer.Deserialize<JsonObject>(body);
                                if (jsonObject != null)
                                {
                                    foreach (var kv in jsonObject.AsObject())
                                    {
                                        if(kv.Value != null)
                                            formData.Add(kv.Key, kv.Value.ToString());
                                    }
                                }
                            }
                            catch (Exception)
                            {
                                logger.LogWarning("Failed to parse post data as json");
                            }
                        }
                        else if (string.Equals(context.Request.ContentType, "application/x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase))
                        {
                            var result = HttpUtility.ParseQueryString(body);
                            foreach (string key in result)
                            {
                                var value = result[key];
                                if(value != null) formData.Add(key, value);
                            }
                        }
                        else
                        {
                            try
                            {
                                context.Request.Body.Seek(0, SeekOrigin.Begin);
                                var parser = await MultipartFormDataParser.ParseAsync(context.Request.Body).ConfigureAwait(false);
                                foreach (var p in parser.Parameters)
                                {
                                    formData.Add(p.Name, p.Data);
                                }

                                foreach (var f in parser.Files)
                                {
                                    // Save temp file
                                    var fileName = Path.GetTempFileName();
                                    var file = File.OpenWrite(fileName);
                                    await using (file.ConfigureAwait(false))
                                    {
                                        await f.Data.CopyToAsync(file, context.RequestAborted).ConfigureAwait(false);
                                    file.Close();
                                    formData.Add(f.Name, fileName);
                                    formData.Add(f.Name + ".SourceName", f.FileName);
                                    formData.Add(f.Name + ".ContentType", f.ContentType);
                                    }
                                }
                            }
                            catch (Exception)
                            {
                                logger.LogWarning("Failed to parse post data as multipart form data");
                            }
                        }

                        if (resourceLoader != null)
                        {
                            switch (requestVerb)
                            {
                                case "PUT":
                                case "PATCH":
                                    content = await resourceLoader.Put(appSession, resourceName, parameters, formData).ConfigureAwait(false);
                                    break;
                                case "DELETE":
                                    content = await resourceLoader.Delete(appSession, resourceName, parameters, formData).ConfigureAwait(false);
                                    break;
                                default: // POST
                                    content = await resourceLoader.Post(appSession, resourceName, parameters, formData).ConfigureAwait(false);
                                    break;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        if (ex.InnerException != null) ex = ex.InnerException;
                        logger.LogError("Request Exception {Message}\r\n{StackTrace}", ex.Message, ex.StackTrace);

                        var exResource = new Dictionary<string, string>(StringComparer.Ordinal)
                        {
                            { "Message", ex.Message },
                            { "StackTrace", ex.StackTrace ?? string.Empty }
                        };
                        content = new Resource(resourceName, $"StonehengeContent.Invoke.{requestVerb}", ResourceType.Json,
                            JsonSerializer.Serialize(exResource), Resource.Cache.None);
                        
                        Debugger.Break();
                    }

                    break;
            }

            if (content == null)
            {
                await _next.Invoke(context).ConfigureAwait(false);
                return;
            }

            context.Response.ContentType = content.ContentType;

            if (context.Items["stonehenge.HostOptions"] is StonehengeHostOptions { DisableClientCache: true })
            {
                context.Response.Headers.Append("Cache-Control",
                    (string[]) ["no-cache", "no-store", "must-revalidate", "proxy-revalidate"]);
                context.Response.Headers.Append("Pragma", (string[]) ["no-cache"]);
                context.Response.Headers.Append("Expires", (string[]) ["0"]);
            }
            else
            {
                switch (content.CacheMode)
                {
                    case Resource.Cache.None:
                        context.Response.Headers.Append("Cache-Control", (string[]) ["no-cache"]);
                        break;
                    case Resource.Cache.Revalidate:
                        context.Response.Headers.Append("Cache-Control",
                            (string[]) ["max-age=3600", "must-revalidate", "proxy-revalidate"]);
                        var etag = AppSession.GetResourceETag(path);
                        context.Response.Headers.Append(HeaderNames.ETag, new StringValues(etag));
                        break;
                    case Resource.Cache.OneDay:
                        context.Response.Headers.Append("Cache-Control", (string[]) ["max-age=86400"]);
                        break;
                }
            }

            if (appSession != null)
            {
                context.Response.Headers.Append("X-Stonehenge-Id", new[] { appSession.Id });
            }

            if (content.IsNoContent)
            {
                context.Response.StatusCode = (int)HttpStatusCode.NoContent;
            }
            else if (content.IsBinary)
            {
                var writer = new StreamWriter(response);
                await using (writer.ConfigureAwait(false))
                {
                    await writer.BaseStream.WriteAsync(content.Data, context.RequestAborted).ConfigureAwait(false);
                }
            }
            else
            {
                var writer = new StreamWriter(response);
                await using (writer.ConfigureAwait(false))
                {
                    await writer.WriteAsync(content.Text).ConfigureAwait(false);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError("StonehengeContent write response: {Message}\r\n{StackTrace}", ex.Message, ex.StackTrace);
            while (ex.InnerException != null)
            {
                ex = ex.InnerException;
                logger.LogError("Inner exception: {Message}\r\n{StackTrace}", ex.Message, ex.StackTrace);
            }
            Debugger.Break();
        }
    }


    private bool CheckBasicAuthFromContext(AppSession appSession, HttpContext context)
    {
        var auth = context.Request.Headers.Authorization.FirstOrDefault();
        if (auth == null) return false;

        if (auth.StartsWith("Basic ", StringComparison.InvariantCultureIgnoreCase))
        {
            if (string.Equals(auth, appSession.VerifiedBasicAuth, StringComparison.Ordinal))
            {
                return true;
            }

            var userPassword = Encoding.ASCII.GetString(Convert.FromBase64String(auth.Substring(6)));
            var usrPwd = userPassword.Split(':');
            if (usrPwd.Length != 2)
            {
                return false;
            }

            var user = usrPwd[0];
            var pwd = usrPwd[1];
            var isValid = appSession.Passwords.IsValid(user, pwd);
            appSession.VerifiedBasicAuth = isValid ? auth : string.Empty;
            return isValid;
        }

        return false;
    }

    private void SetUserNameFromContext(AppSession appSession, HttpContext context)
    {
        var identityId = context.User.Identity?.Name ?? string.Empty;
        if (!string.IsNullOrEmpty(identityId))
        {
            appSession.SetUser(identityId, identityId, "");
            return;
        }

        var identityName = string.Empty;
        var identityMail = string.Empty;

        var auth = context.Request.Headers.Authorization.FirstOrDefault();
        if (auth != null)
        {
            if (auth.StartsWith("Basic ", StringComparison.InvariantCultureIgnoreCase))
            {
                var userPassword = Encoding.ASCII.GetString(Convert.FromBase64String(auth.Substring(6)));
                identityId = userPassword.Split(':').FirstOrDefault() ?? string.Empty;
            }
            else if (auth.StartsWith("Bearer ", StringComparison.InvariantCultureIgnoreCase))
            {
                var token = auth.Substring(7);
                var handler = new JwtSecurityTokenHandler();
                var jwtToken = handler.ReadToken(token) as JwtSecurityToken;
                identityId = jwtToken?.Subject ?? string.Empty;
                identityName = jwtToken?.Payload["name"]?.ToString() ?? string.Empty;
                identityMail = jwtToken?.Payload["email"]?.ToString() ?? string.Empty;
            }

            appSession.SetUser(identityName, identityId, identityMail);
        }

        var isLocal = context.IsLocal();
        if (!isLocal) return;

        var explorers = Process.GetProcessesByName("explorer");
        if (explorers.Length == 1)
        {
            identityId = $"{Environment.UserDomainName}\\{Environment.UserName}";
            appSession.SetUser(identityId, "", "");
        }

        // RDP with more than one session: How to find app and session using request's client IP port
    }

    private void HandleIndexContent(HttpContext context, Resource content)
    {
        const string placeholderAppTitle = "stonehengeAppTitle";
        var appTitle = context.Items["stonehenge.AppTitle"]?.ToString() ?? string.Empty;
        content.Text = content.Text?.Replace(placeholderAppTitle, appTitle);
    }
}