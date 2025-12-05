using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using IctBaden.Stonehenge.Hosting;
using IctBaden.Stonehenge.Resources;
using IctBaden.Stonehenge.ViewModel;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Logging;
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global

[assembly: InternalsVisibleTo("IctBaden.Stonehenge.Test")]

namespace IctBaden.Stonehenge.Core;

[SuppressMessage("Design", "MA0046:Use EventHandler<T> to declare events")]
[SuppressMessage("Design", "MA0051:Method is too long")]
public sealed class AppSession : INotifyPropertyChanged, IDisposable
{
    public static string AppInstanceId { get; private set; } = Guid.NewGuid().ToString("N");

    public StonehengeHostOptions HostOptions { get; private set; } = new();
    public string HostDomain { get; private set; } = string.Empty;
    public string HostUrl { get; private set; } = string.Empty;
    public bool IsLocal { get; private set; }
    public bool IsDebug { get; private set; }
    public string ClientAddress { get; private set; } = string.Empty;
    public int ClientPort { get; private set; }
    public string UserAgent { get; private set; } = string.Empty;
    public string Platform { get; private set; } = string.Empty;
    public string Browser { get; private set; } = string.Empty;
    public int SessionCount => _appSessions.Count;
    public bool CookiesSupported { get; private set; }
    public bool StonehengeCookieSet { get; private set; }
    public IDictionary<string, string> Cookies { get; }
    public IDictionary<string, string> Parameters { get; }

    public DateTime ConnectedSince { get; private set; }
    public DateTime LastAccess { get; private set; }
    public string CurrentRoute => _history.FirstOrDefault() ?? string.Empty;
    public string Context { get; private set; } = string.Empty;


    /// User login is requested on next request 
    public bool RequestLogin;

    /// Redirect URL used to complete authorization 
    public string AuthorizeRedirectUrl = string.Empty;
    /// Respond with 401 to complete unauthorization 
    public bool UnauthorizeRedirect;

    /// Access token given from authorization 
    public string AccessToken = string.Empty;

    /// Refresh token given from authorization 
    public string RefreshToken = string.Empty;


    /// Security token issued on login (only if provided by identity provider) 
    public JwtSecurityToken? SecurityToken { get; private set; } 
    /// Name of user identity 
    public string UserIdentity { get; private set; } = string.Empty;
    /// Name of user identity 
    public string UserIdentityId { get; private set; } = string.Empty;
    /// Name of user identity 
    public string UserIdentityEMail { get; private set; } = string.Empty;


    public CultureInfo SessionCulture { get; private set; } = CultureInfo.CurrentUICulture;


    public DateTime LastUserAction { get; private set; }

    private readonly Guid _id;
    public string Id => $"{_id:N}";

    public string PermanentSessionId { get; private set; } = string.Empty;

    public bool UseBasicAuth;
    public readonly Passwords Passwords = new(string.Empty);
    public string VerifiedBasicAuth = string.Empty;

    private readonly int _eventTimeoutMs;

    private readonly List<string> _events = [];
    private readonly AppSessions _appSessions;

    private CancellationTokenSource? _eventRelease;
    private bool _forceUpdate;
    private readonly List<string> _history = [];

    public string GetBackRoute()
    {
        if (_history.Count == 0)
        {
            return string.Empty;
        }

        var route = _history.Skip(1).First();
        _history.RemoveAt(0);
        return route;
    }

    public bool IsWaitingForEvents { get; private set; }

    public bool SecureCookies { get; private set; }

    public readonly ILogger Logger;

    // ReSharper disable once ReturnTypeCanBeEnumerable.Global
    public async Task<string[]> CollectEvents(CancellationToken requestAborted)
    {
        try
        {
            while (IsWaitingForEvents && !requestAborted.IsCancellationRequested)
            {
                await Task.Delay(1000, requestAborted).ConfigureAwait(false);
            }
        }
        catch
        {
            // ignore TaskCanceledException on request abort
        }

        await WaitForEvents().ConfigureAwait(false);

        lock (_events)
        {
            if (ViewModel is ActiveViewModel { SupportsEvents: false })
            {
                _events.Clear();
            }
            var events = _events.ToArray();
            _events.Clear();
            return events;
        }
    }

    private async Task WaitForEvents()
    {
        try
        {
            IsWaitingForEvents = true;
            var eventVm = ViewModel;

            _eventRelease?.Dispose();
            _eventRelease = new CancellationTokenSource();

            // wait _eventTimeoutMs for events - if there is one - continue
            var max = _eventTimeoutMs / 100;
            while (!_forceUpdate && max > 0 && _eventRelease != null)
            {
                await Wait(_eventRelease, 100).ConfigureAwait(false);
                max--;
            }

            if (ViewModel == eventVm)
            {
                // wait for maximum 500ms for more events - if there is none within - continue
                max = 50;
                while (!_forceUpdate && max > 0 && _eventRelease != null)
                {
                    await Wait(_eventRelease, 10).ConfigureAwait(false);
                    max--;
                }
            }
            else
            {
                // VM has changed
                EventsClear(false);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError("AppSession.CollectEvents({ViewModel}): {Message}",
                ViewModel?.GetType().Name ?? "<UNKNOWN>", ex.Message);
            Debugger.Break();
        }
        finally
        {
            _eventRelease?.Dispose();
            _eventRelease = null;

            _forceUpdate = false;
            IsWaitingForEvents = false;
        }
    }

    private async Task Wait(CancellationTokenSource cts, int milliseconds)
    {
        try
        {
            await Task
                .Delay(milliseconds, cts.Token)
                .ContinueWith(_ =>
                {
                    if (cts is { IsCancellationRequested: true })
                    {
                        _forceUpdate = true;
                    }
                }, TaskContinuationOptions.None)
                .ConfigureAwait(false);
        }
        catch
        {
            // ignore cts is disposed during wait
        }
    }

    public event Action<string>? OnNavigate;


    private object? _viewModel;

    public object? ViewModel
    {
        get => _viewModel;
        set
        {
            if (_viewModel is IDisposable vm)
            {
                vm.Dispose();
            }

            var previousViewModel = _viewModel;
            _viewModel = value;
            if (value is not INotifyPropertyChanged notifyPropertyChanged) return;

            if (previousViewModel == null)
            {
                SetSessionCulture(SessionCulture);
            }
            notifyPropertyChanged.PropertyChanged += (sender, args) =>
            {
                if (sender is not ActiveViewModel activeViewModel) return;

                lock (activeViewModel.Session._events)
                {
                    if (!string.IsNullOrEmpty(args.PropertyName))
                    {
                        activeViewModel.Session.UpdateProperty(args.PropertyName);
                    }
                }
            };
        }
    }

    // ReSharper disable once UnusedMember.Global
    public void ClientAddressChanged(string address)
    {
        ClientAddress = address;
        NotifyPropertyChanged(nameof(ClientAddress));
    }

    internal object? SetViewModelType(string typeName)
    {
        var oldViewModel = ViewModel;
        if (oldViewModel != null)
        {
            if (string.Equals(oldViewModel.GetType().FullName, typeName, StringComparison.Ordinal))
            {
                // no change
                return oldViewModel;
            }

            EventsClear(true);
            var disposable = oldViewModel as IDisposable;
            disposable?.Dispose();
        }

        if (_resourceLoader.Providers
                .FirstOrDefault(ld => ld is ResourceLoader) is not ResourceLoader)
        {
            ViewModel = null;
            Logger.LogError("Could not create ViewModel - No resourceLoader specified: {TypeName}", typeName);
            return null;
        }

        var newViewModelType = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .FirstOrDefault(type => type.FullName?.EndsWith($".{typeName}", StringComparison.Ordinal) ?? false);

        if (newViewModelType == null)
        {
            ViewModel = null;
            Logger.LogError("Could not create ViewModel: {TypeName}", typeName);
            return null;
        }

        ViewModel = CreateType($"ViewModel({typeName})", newViewModelType);
        var info = _resourceLoader.GetViewModelInfos()
            .FirstOrDefault(vmInfo => string.Equals(vmInfo.VmName, typeName, StringComparison.Ordinal));
        if (info != null && ViewModel is ActiveViewModel avm)
        {
            avm.I18Names = info.I18Names.ToArray();
        }

        var viewModelInfo = _resourceLoader.Providers
            .SelectMany(p => p.GetViewModelInfos())
            .FirstOrDefault(vmInfo => string.Equals(vmInfo.VmName, typeName, StringComparison.Ordinal));

        var route = viewModelInfo?.Route ?? string.Empty;
        _history.Insert(0, route);
        if (!string.IsNullOrEmpty(route))
        {
            OnNavigate?.Invoke(route);
        }

        return ViewModel;
    }

    public object? CreateType(string context, Type type)
    {
        object? instance = null;
        var typeConstructors = type.GetConstructors();
        if (!typeConstructors.Any())
        {
            if (type.IsValueType)
            {
                Logger.LogWarning("AppSession.CreateType({Context}, {TypeName}): Using default value",
                    context, type.Name);
                instance = RuntimeHelpers.GetUninitializedObject(type);
            }
            else
            {
                Logger.LogError("AppSession.CreateType({Context}, {TypeName}): No public constructors",
                    context, type.Name);
            }
        }

        foreach (var constructor in typeConstructors)
        {
            var parameters = constructor.GetParameters();
            if (parameters.Length == 0)
            {
                instance = Activator.CreateInstance(type);
                break;
            }

            var paramValues = new object?[parameters.Length];

            for (var ix = 0; ix < parameters.Length; ix++)
            {
                var parameterInfo = parameters[ix];
                if (parameterInfo.ParameterType == typeof(AppSession))
                {
                    paramValues[ix] = this;
                }
                else
                {
                    paramValues[ix] = _resourceLoader.Services.GetService(parameterInfo.ParameterType)
                                      ?? CreateType($"{context}, CreateType({type.Name})",
                                          parameterInfo.ParameterType);
                }
            }

            try
            {
                instance = Activator.CreateInstance(type, paramValues);
                break;
            }
            catch (Exception ex)
            {
                Logger.LogError("AppSession.CreateType({Context}, {TypeName}): {Message}",
                    context, type.Name, ex.Message);
                Debugger.Break();
            }
        }

        return instance;
    }


    public string SubDomain
    {
        get
        {
            if (string.IsNullOrEmpty(HostDomain))
                return string.Empty;

            var port = HostDomain.Split(':');
            if (port.Length > 1)
            {
                if (IPAddress.TryParse(port[0], out _))
                    return string.Empty;
            }

            var parts = HostDomain.Split('.');
            if (parts.Length == 1) return string.Empty;

            var isNumeric = int.TryParse(parts[0], NumberStyles.Number, CultureInfo.InvariantCulture, out _);
            return isNumeric ? HostDomain : parts[0];
        }
    }

    private readonly Dictionary<string, object?> _userData;

    public object? this[string key]
    {
        get => _userData.ContainsKey(key) ? _userData[key] : null;
        set
        {
            if (this[key] == value)
                return;
            _userData[key] = value;
            NotifyPropertyChanged(key);
        }
    }

    public void Set<T>(string key, T value)
    {
        _userData[key] = value;
    }

    public T? Get<T>(string key)
    {
        if (!_userData.TryGetValue(key, out var value))
            return default;
        return (T?)value;
    }

    public void Remove(string key)
    {
        _userData.Remove(key);
    }

    public TimeSpan ConnectedDuration => DateTime.Now - ConnectedSince;

    public TimeSpan LastAccessDuration => DateTime.Now - LastAccess;

    // ReSharper disable once UnusedMember.Global
    public TimeSpan LastUserActionDuration => DateTime.Now - LastUserAction;

    public event Action? TimedOut;
    private Timer? _pollSessionTimeout;
    public TimeSpan SessionTimeout { get; private set; }
    public bool IsTimedOut => LastAccessDuration > SessionTimeout;

    // ReSharper disable once UnusedMember.Global
    public void SetTimeout(TimeSpan timeout)
    {
        _pollSessionTimeout?.Dispose();
        SessionTimeout = timeout;
        if (Math.Abs(timeout.TotalMilliseconds) > 0.1)
        {
            _pollSessionTimeout = new Timer(CheckSessionTimeout, null, TimeSpan.FromSeconds(30),
                TimeSpan.FromSeconds(30));
        }
    }

    private void CheckSessionTimeout(object? _)
    {
        if (LastAccessDuration > SessionTimeout)
        {
            _pollSessionTimeout?.Dispose();
            TimedOut?.Invoke();
        }

        NotifyPropertyChanged(nameof(ConnectedDuration));
        NotifyPropertyChanged(nameof(LastAccessDuration));
    }

#pragma warning disable IDISP008
    private readonly StonehengeResourceLoader _resourceLoader;
#pragma warning restore IDISP008


    public static readonly AppSession None = new();

    public AppSession()
        : this(null, new StonehengeHostOptions(), new AppSessions())
    {
    }

    public AppSession(StonehengeResourceLoader? resourceLoader, StonehengeHostOptions options, AppSessions appSessions)
    {
        _appSessions = appSessions;

        if (resourceLoader == null)
        {
            var assemblies = new List<Assembly?>
                {
                    Assembly.GetEntryAssembly(),
                    Assembly.GetExecutingAssembly(),
                    Assembly.GetAssembly(typeof(ResourceLoader))
                }
                .Where(a => a != null)
                .Distinct()
                .Cast<Assembly>()
                .ToList();

            Logger = StonehengeLogger.DefaultLogger;
            resourceLoader = new StonehengeResourceLoader(Logger,
            [
                new ResourceLoader(Logger, assemblies, Assembly.GetCallingAssembly())
            ]);
        }
        else
        {
            Logger = resourceLoader.Logger;
        }

        _resourceLoader = resourceLoader;
        _userData = new Dictionary<string, object?>(StringComparer.Ordinal);
        _id = Guid.NewGuid();
        SessionTimeout = TimeSpan.FromSeconds(10); //options.SessionTimeout;
        Cookies = new Dictionary<string, string>(StringComparer.Ordinal);
        Parameters = new Dictionary<string, string>(StringComparer.Ordinal);
        LastAccess = DateTime.Now;

        UseBasicAuth = options.UseBasicAuth;
        var htpasswd = Path.Combine(StonehengeApplication.BaseDirectory, ".htpasswd");
        if (File.Exists(htpasswd))
        {
            Passwords = new Passwords(htpasswd);
        }
        else if (UseBasicAuth)
        {
            Logger.LogError("Option UseBasicAuth requires .htpasswd file {Htpasswd}", htpasswd);
        }

        _eventTimeoutMs = options.GetEventTimeoutMs();

        try
        {
            if (Assembly.GetEntryAssembly() == null) return;
            var cfg = Path.Combine(StonehengeApplication.BaseDirectory, "Stonehenge.cfg"); // TODO: doc
            if (!File.Exists(cfg)) return;

            var settings = File.ReadAllLines(cfg);
            var secureCookies = settings.FirstOrDefault(s => s.Contains("SecureCookies", StringComparison.Ordinal));
            if (secureCookies == null) return;

            var set = secureCookies.Split('=');
            SecureCookies = set.Length > 1 && string.Equals(set[1].Trim(), "1", StringComparison.Ordinal);
        }
        catch
        {
            // ignore
        }
    }

    // ReSharper disable once UnusedMember.Global
    public bool IsInitialized => !string.IsNullOrEmpty(UserAgent);


    private bool IsAssemblyDebugBuild(Assembly assembly) => assembly
        .GetCustomAttributes(false)
        .OfType<DebuggableAttribute>()
        .Any(da => da.IsJITTrackingEnabled);

    public void Initialize(StonehengeHostOptions hostOptions, string hostUrl, string hostDomain,
        bool isLocal, string clientAddress, int clientPort, string userAgent)
    {
        HostOptions = hostOptions;
        HostUrl = hostUrl;
        HostDomain = hostDomain;
        IsLocal = isLocal;
        ClientAddress = clientAddress;
        ClientPort = clientPort;
        UserAgent = userAgent;
        ConnectedSince = DateTime.Now;

        DetectBrowser(userAgent);
        IsDebug = IsAssemblyDebugBuild(Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly());
    }

    // ReSharper disable once UnusedParameter.Local
    private void DetectBrowser(string userAgent)
    {
        var browserDecoder = new SimpleUserAgentDecoder(userAgent);

        Browser = browserDecoder.BrowserName;
        if (!string.IsNullOrEmpty(browserDecoder.BrowserVersion))
        {
            Browser += $" {browserDecoder.BrowserVersion}";
        }

        Platform = browserDecoder.ClientOsName;
        if (!string.IsNullOrEmpty(browserDecoder.ClientOsVersion))
        {
            Platform += $" {browserDecoder.ClientOsVersion}";
        }

        CookiesSupported = true;
    }

    public void Accessed(IDictionary<string, string> cookies, bool userAction)
    {
        foreach (var cookie in cookies)
        {
            Cookies[cookie.Key] = cookie.Value;
        }

        if (string.IsNullOrEmpty(PermanentSessionId) && cookies.TryGetValue("ss-pid", out var ssPid))
        {
            PermanentSessionId = ssPid;
        }

        LastAccess = DateTime.Now;
        NotifyPropertyChanged(nameof(LastAccess));
        if (userAction)
        {
            LastUserAction = DateTime.Now;
            NotifyPropertyChanged(nameof(LastUserAction));
        }

        StonehengeCookieSet = cookies.ContainsKey("stonehenge-id");
        NotifyPropertyChanged(nameof(StonehengeCookieSet));
    }

    public void SetContext(string context)
    {
        Context = context;
    }

    public void EventsClear(bool forceEnd)
    {
        Logger.LogTrace("Session({SessionId}).EventsClear({ForceEnd})",
            Id, forceEnd);
        lock (_events)
        {
            _events.Clear();
            if (forceEnd)
            {
                try
                {
                    _eventRelease?.Cancel();
                }
                catch
                {
                    // ignore disposed
                }
            }
        }
    }

    public void UpdatePropertyImmediately(string name)
    {
        UpdateProperty(name);
        UpdatePropertiesImmediately();
    }

    public void UpdatePropertiesImmediately()
    {
        _forceUpdate = true;
    }

    public void UpdateProperty(string name)
    {
        lock (_events)
        {
            if (!_events.Contains(name, StringComparer.Ordinal))
            {
                _events.Add(name);
            }

            try
            {
                _eventRelease?.Cancel();
            }
            catch
            {
                // ignore disposed
            }
        }
    }

    public static string GetResourceETag(string path) => AppInstanceId + StringComparer.Ordinal.GetHashCode(path).ToString("x8");

    public override string ToString()
    {
        // ReSharper disable once UseStringInterpolation
        return string.Format("[{0}] {1} {2}", Id,
            ConnectedSince.ToShortDateString() + " " + ConnectedSince.ToShortTimeString(), SubDomain);
    }

    public event PropertyChangedEventHandler? PropertyChanged;


    private void NotifyPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public void SetParameters(IDictionary<string, string> parameters)
    {
        foreach (var parameter in parameters)
        {
            Parameters[parameter.Key] = parameter.Value;
        }
        Parameters.Remove("stonehenge-id");
    }

    public void SetUser(JwtSecurityToken? jwtToken, string identityName, string identityId, string identityEMail)
    {
        SecurityToken = jwtToken;
        UserIdentity = identityName;
        UserIdentityId = identityId;
        UserIdentityEMail = identityEMail;
        RequestLogin = false;
    }

    public void SetSessionCulture(CultureInfo culture)
    {
        SessionCulture = culture;
        AppPages.UpdatePageTitles(this);
        if (ViewModel is ActiveViewModel avm)
        {
            avm.UpdateI18n();
            avm.EnableRoute(string.Empty, false);
        }
    }

    public void UserLogin(bool useBasicAuth = false)
    {
        SetUser(null, string.Empty, string.Empty, string.Empty);
        AuthorizeRedirectUrl = string.Empty;

        if (useBasicAuth)
        {
            UseBasicAuth = true;
            return;
        }

        var o = HostOptions.UseKeycloakAuthentication;
        if (o == null) return;

        RequestLogin = true;
        AuthorizeRedirectUrl = $"{HostUrl}/index.html?ts={DateTimeOffset.Now.ToUnixTimeMilliseconds()}";
        var query = new QueryBuilder
        {
            { "client_id", o.ClientId },
            { "redirect_uri", AuthorizeRedirectUrl },
            { "response_type", "code" },
            { "scope", "openid" },
            { "nonce", Id },
            { "state", Id }
        };
        (ViewModel as ActiveViewModel)?.NavigateTo($"{o.AuthUrl}/realms/{o.Realm}/protocol/openid-connect/auth{query}");
    }

    [SuppressMessage("IDisposableAnalyzers.Correctness", "IDISP014:Use a single instance of HttpClient")]
    public bool UserLogout()
    {
        if (HostOptions.UseBasicAuth || UseBasicAuth)
        {
            UseBasicAuth = HostOptions.UseBasicAuth;
            SetUser(null, string.Empty, string.Empty, string.Empty);
            if (ViewModel is ActiveViewModel avm)
            {
                UnauthorizeRedirect = true;
                avm.ExecuteClientScript("");
            }
            return true;
        }
        
        if (HostOptions.UseKeycloakAuthentication == null) return false;

        if (string.IsNullOrEmpty(AuthorizeRedirectUrl) || string.IsNullOrEmpty(RefreshToken)) return false;

        var o = HostOptions.UseKeycloakAuthentication;

        using var client = new HttpClient();
        var data = $"client_id={o.ClientId}&state={Id}&&refresh_token={RefreshToken}&redirect_uri={HttpUtility.UrlEncode(AuthorizeRedirectUrl)}";

        var logoutUrl = $"{o.AuthUrl}/realms/{o.Realm}/protocol/openid-connect/logout";
        using var content = new StringContent(data, Encoding.UTF8, "application/x-www-form-urlencoded");
        using var result = client.PostAsync(logoutUrl, content).Result;

        var text = result.Content.ReadAsStringAsync().Result;
        Debug.WriteLine($"UserLogout {result.StatusCode} : {text}");

        SetUser(null, string.Empty, string.Empty, string.Empty);
        AuthorizeRedirectUrl = string.Empty;

        return result.StatusCode == HttpStatusCode.NoContent;
    }

    public void Dispose()
    {
        _pollSessionTimeout?.Dispose();
        _pollSessionTimeout = null;

        _eventRelease?.Dispose();
        _eventRelease = null;

        OnNavigate = null;
    }
}