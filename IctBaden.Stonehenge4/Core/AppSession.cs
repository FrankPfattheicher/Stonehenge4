using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using IctBaden.Stonehenge.Hosting;
using IctBaden.Stonehenge.Resources;
using IctBaden.Stonehenge.ViewModel;
using Microsoft.Extensions.Logging;

// ReSharper disable TemplateIsNotCompileTimeConstantProblem

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable EventNeverSubscribedTo.Global

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Local

[assembly: InternalsVisibleTo("IctBaden.Stonehenge.Test")]

namespace IctBaden.Stonehenge.Core
{
    public class AppSession : INotifyPropertyChanged
    {
        public static string AppInstanceId { get; private set; } = Guid.NewGuid().ToString("N");

        public StonehengeHostOptions HostOptions { get; private set; }
        public string HostDomain { get; private set; }
        public bool IsLocal { get; private set; }
        public bool IsDebug { get; private set; }
        public string ClientAddress { get; private set; }
        public int ClientPort { get; private set; }
        public string UserAgent { get; private set; }
        public string Platform { get; private set; }
        public string Browser { get; private set; }

        public bool CookiesSupported { get; private set; }
        public bool StonehengeCookieSet { get; private set; }
        public Dictionary<string, string> Cookies { get; private set; }
        public Dictionary<string, string> Parameters { get; private set; }

        public DateTime ConnectedSince { get; private set; }
        public DateTime LastAccess { get; private set; }
        public string CurrentRoute => _history.FirstOrDefault();
        public string Context { get; private set; }

        
        /// Name of user identity 
        public string UserIdentity { get; private set; }
        /// Name of user identity 
        public string UserIdentityId { get; private set; }
        /// Name of user identity 
        public string UserIdentityEMail { get; private set; }
        public DateTime LastUserAction { get; private set; }

        private readonly Guid _id;
        public string Id => $"{_id:N}";

        public string PermanentSessionId { get; private set; }

        public readonly bool UseBasicAuth;
        public readonly Passwords Passwords;
        public string VerifiedBasicAuth;

        private readonly int _eventTimeoutMs;

        private readonly List<string> _events = new();

        private CancellationTokenSource _eventRelease;
        private bool _forceUpdate;
        private readonly List<string> _history = new();

        public string GetBackRoute()
        {
            var route = "";
            if (_history.Count > 1)
            {
                route = _history.Skip(1).First();
                _history.RemoveAt(0);
            }

            return route;
        }

        public bool IsWaitingForEvents { get; private set; }

        public bool SecureCookies { get; private set; }

        public readonly ILogger Logger;

        // ReSharper disable once ReturnTypeCanBeEnumerable.Global
        public async Task<string[]> CollectEvents()
        {
            IsWaitingForEvents = true;
            var eventVm = ViewModel;

            _eventRelease = new CancellationTokenSource();

            // wait _eventTimeoutMs for events - if there is one - continue
            var max = _eventTimeoutMs / 100;

            async Task Wait(CancellationTokenSource cts, int milliseconds)
            {
                await Task.Delay(milliseconds, cts.Token)
                    .ContinueWith(_ =>
                    {
                        if (cts is { IsCancellationRequested: true })
                        {
                            _forceUpdate = true;
                        }
                    }, TaskContinuationOptions.None)
                    .ConfigureAwait(false);
            }

            while (!_forceUpdate && max > 0)
            {
                await Wait( _eventRelease, 100);
                max--;
            }

            if (ViewModel == eventVm)
            {
                // wait for maximum 500ms for more events - if there is none within - continue
                max = 50;
                while (!_forceUpdate && max > 0)
                {
                    await Wait(_eventRelease, 10);
                    max--;
                }
            }
            else
            {
                // VM has changed
                EventsClear(false);
            }

            _forceUpdate = false;
            IsWaitingForEvents = false;

            lock (_events)
            {
                var events = _events.ToArray();
                _events.Clear();
                return events;
            }
        }

        private object _viewModel;

        public object ViewModel
        {
            get => _viewModel;
            set
            {
                (_viewModel as IDisposable)?.Dispose();

                _viewModel = value;
                if (value is INotifyPropertyChanged npc)
                {
                    npc.PropertyChanged += (sender, args) =>
                    {
                        if (!(sender is ActiveViewModel avm))
                        {
                            return;
                        }

                        lock (avm.Session._events)
                        {
                            avm.Session.UpdateProperty(args.PropertyName);
                        }
                    };
                }
            }
        }

        // ReSharper disable once UnusedMember.Global
        public void ClientAddressChanged(string address)
        {
            ClientAddress = address;
            NotifyPropertyChanged(nameof(ClientAddress));
        }

        internal object SetViewModelType(string typeName)
        {
            var oldViewModel = ViewModel;
            if (oldViewModel != null)
            {
                if ((oldViewModel.GetType().FullName == typeName))
                {
                    // no change
                    return oldViewModel;
                }

                EventsClear(true);
                var disposable = oldViewModel as IDisposable;
                disposable?.Dispose();
            }

            var resourceLoader =
                _resourceLoader.Providers.First(ld => ld.GetType() == typeof(ResourceLoader)) as ResourceLoader;
            if (resourceLoader == null)
            {
                ViewModel = null;
                Logger.LogError("Could not create ViewModel - No resourceLoader specified:" + typeName);
                return null;
            }

            var newViewModelType = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .FirstOrDefault(type => type.FullName?.EndsWith($".{typeName}") ?? false);

            if (newViewModelType == null)
            {
                ViewModel = null;
                Logger.LogError("Could not create ViewModel:" + typeName);
                return null;
            }

            ViewModel = CreateType($"ViewModel({typeName})", newViewModelType);

            var viewModelInfo = _resourceLoader.Providers
                .SelectMany(p => p.GetViewModelInfos())
                .FirstOrDefault(vmi => vmi.VmName == typeName);

            _history.Insert(0, viewModelInfo?.Route ?? "");

            return ViewModel;
        }

        public object CreateType(string context, Type type)
        {
            object instance = null;
            foreach (var constructor in type.GetConstructors())
            {
                var parameters = constructor.GetParameters();
                if (parameters.Length == 0)
                {
                    instance = Activator.CreateInstance(type);
                    break;
                }

                var paramValues = new object[parameters.Length];

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
                    Logger.LogError($"AppSession.CreateType({context}, {type.Name}): " + ex.Message);
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

                var parts = HostDomain.Split('.');
                if (parts.Length == 1) return string.Empty;

                var isNumeric = int.TryParse(parts[0], out _);
                return isNumeric ? HostDomain : parts[0];
            }
        }

        private readonly Dictionary<string, object> _userData;

        public object this[string key]
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

        public T Get<T>(string key)
        {
            if (!_userData.ContainsKey(key))
                return default;

            return (T)_userData[key];
        }

        public void Remove(string key)
        {
            _userData.Remove(key);
        }

        public TimeSpan ConnectedDuration => DateTime.Now - ConnectedSince;

        public TimeSpan LastAccessDuration => DateTime.Now - LastAccess;

        // ReSharper disable once UnusedMember.Global
        public TimeSpan LastUserActionDuration => DateTime.Now - LastUserAction;

        public event Action TimedOut;
        private Timer _pollSessionTimeout;
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

        private void CheckSessionTimeout(object _)
        {
            if ((LastAccessDuration > SessionTimeout) && (_terminator != null))
            {
                _pollSessionTimeout.Dispose();
                _terminator.Dispose();
                TimedOut?.Invoke();
            }

            NotifyPropertyChanged(nameof(ConnectedDuration));
            NotifyPropertyChanged(nameof(LastAccessDuration));
        }

        private IDisposable _terminator;


        // ReSharper disable once UnusedMember.Global
        public void SetTerminator(IDisposable disposable)
        {
            _terminator = disposable;
        }

        private readonly StonehengeResourceLoader _resourceLoader;

        public AppSession()
            : this(null, new StonehengeHostOptions())
        {
        }

        public AppSession(StonehengeResourceLoader resourceLoader, StonehengeHostOptions options)
        {
            if (resourceLoader == null)
            {
                var assemblies = new List<Assembly>
                    {
                        Assembly.GetEntryAssembly(),
                        Assembly.GetExecutingAssembly(),
                        Assembly.GetAssembly(typeof(ResourceLoader))
                    }
                    .Distinct()
                    .ToList();

                Logger = StonehengeLogger.DefaultLogger;
                var loader = new ResourceLoader(Logger, assemblies, Assembly.GetCallingAssembly());
                resourceLoader = new StonehengeResourceLoader(Logger, new List<IStonehengeResourceProvider> { loader });
            }
            else
            {
                Logger = resourceLoader.Logger;
            }

            _resourceLoader = resourceLoader;
            _userData = new Dictionary<string, object>();
            _id = Guid.NewGuid();
            SessionTimeout = TimeSpan.FromMinutes(15);
            Cookies = new Dictionary<string, string>();
            Parameters = new Dictionary<string, string>();
            LastAccess = DateTime.Now;

            UseBasicAuth = options.UseBasicAuth;
            if (UseBasicAuth)
            {
                var htpasswd = Path.Combine(StonehengeApplication.BaseDirectory, ".htpasswd");
                if (File.Exists(htpasswd))
                {
                    Passwords = new Passwords(htpasswd);
                }
                else
                {
                    Logger.LogError("Option UseBasicAuth requires .htpasswd file " + htpasswd);
                }
            }

            _eventTimeoutMs = options.GetEventTimeoutMs();

            try
            {
                if (Assembly.GetEntryAssembly() == null) return;
                var cfg = Path.Combine(StonehengeApplication.BaseDirectory, "Stonehenge.cfg"); // TODO: doc
                if (!File.Exists(cfg)) return;

                var settings = File.ReadAllLines(cfg);
                var secureCookies = settings.FirstOrDefault(s => s.Contains("SecureCookies"));
                if (secureCookies != null)
                {
                    var set = secureCookies.Split('=');
                    SecureCookies = (set.Length > 1) && (set[1].Trim() == "1");
                }
            }
            catch
            {
                // ignore
            }
        }

        // ReSharper disable once UnusedMember.Global
        public bool IsInitialized => UserAgent != null;


        private bool IsAssemblyDebugBuild(Assembly assembly)
        {
            return assembly.GetCustomAttributes(false).OfType<DebuggableAttribute>().Any(da => da.IsJITTrackingEnabled);
        }

        public void Initialize(StonehengeHostOptions hostOptions, string hostDomain,
            bool isLocal, string clientAddress, int clientPort, string userAgent)
        {
            HostOptions = hostOptions;
            HostDomain = hostDomain;
            IsLocal = isLocal;
            ClientAddress = clientAddress;
            ClientPort = clientPort;
            UserAgent = userAgent;
            ConnectedSince = DateTime.Now;

            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                DetectBrowser(userAgent);
            }

            IsDebug = IsAssemblyDebugBuild(Assembly.GetEntryAssembly());
        }

        // ReSharper disable once UnusedParameter.Local
        private void DetectBrowser(string userAgent)
        {
            //TODO: Decoder
            Browser = "";
            CookiesSupported = true;
            Platform = "OS";
        }

        public void Accessed(IDictionary<string, string> cookies, bool userAction)
        {
            foreach (var cookie in cookies)
            {
                if (Cookies.ContainsKey(cookie.Key))
                {
                    Cookies[cookie.Key] = cookie.Value;
                }
                else
                {
                    Cookies.Add(cookie.Key, cookie.Value);
                }
            }


            if ((PermanentSessionId == null) && cookies.ContainsKey("ss-pid"))
            {
                PermanentSessionId = cookies["ss-pid"];
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
            Logger.LogTrace($"Session({Id}).EventsClear({forceEnd})");
            lock (_events)
            {
                //var privateEvents = Events.Where(e => e.StartsWith(AppService.PropertyNameId)).ToList();
                _events.Clear();
                //Events.AddRange(privateEvents);
                if (forceEnd)
                {
                    _eventRelease?.Cancel();
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
                if (!_events.Contains(name))
                {
                    _events.Add(name);
                }

                _eventRelease?.Cancel();
            }
        }

        public static string GetResourceETag(string path) => AppInstanceId + path.GetHashCode().ToString("x8");

        public override string ToString()
        {
            // ReSharper disable once UseStringInterpolation
            return string.Format("[{0}] {1} {2}", Id,
                ConnectedSince.ToShortDateString() + " " + ConnectedSince.ToShortTimeString(), SubDomain);
        }

        public event PropertyChangedEventHandler PropertyChanged;


        private void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void SetParameters(Dictionary<string, string> parameters)
        {
            foreach (var parameter in parameters)
            {
                if (Parameters.ContainsKey(parameter.Key))
                {
                    Parameters[parameter.Key] = parameter.Value;
                }
                else
                {
                    Parameters.Add(parameter.Key, parameter.Value);
                }
            }
        }

        public void SetUser(string identityName, string identityId, string identityEMail)
        {
            UserIdentity = identityName;
            UserIdentityId = identityId;
            UserIdentityEMail = identityEMail;
        }
    }
}