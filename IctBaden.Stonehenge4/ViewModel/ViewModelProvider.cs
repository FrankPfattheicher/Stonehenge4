using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using IctBaden.Stonehenge.Core;
using IctBaden.Stonehenge.Hosting;
using IctBaden.Stonehenge.Resources;
using Microsoft.Extensions.Logging;

// ReSharper disable TemplateIsNotCompileTimeConstantProblem

namespace IctBaden.Stonehenge.ViewModel;

[SuppressMessage("Design", "MA0051:Method is too long")]
[SuppressMessage("ReSharper", "ReplaceSubstringWithRangeIndexer")]
public sealed class ViewModelProvider(ILogger logger) : IStonehengeResourceProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new DoubleConverter() }
    };

    public void Dispose()
    {
    }

    public void InitProvider(StonehengeResourceLoader loader, StonehengeHostOptions options)
    {
    }

    public IList<ViewModelInfo> GetViewModelInfos() => [];

    public Task<Resource?> Put(AppSession? session, string resourceName, IDictionary<string, string> parameters,
        IDictionary<string, string> formData) =>
        Task.FromResult<Resource?>(null);

    public Task<Resource?> Delete(AppSession? session, string resourceName, IDictionary<string, string> parameters,
        IDictionary<string, string> formData) =>
        Task.FromResult<Resource?>(null);

    public Task<Resource?> Post(AppSession? session, string resourceName,
        IDictionary<string, string> parameters, IDictionary<string, string> formData)
    {
        if (resourceName.StartsWith("Command/", StringComparison.OrdinalIgnoreCase))
        {
            var commandName = resourceName.Substring(8);
            var appCommandsType = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .FirstOrDefault(type => type.GetInterfaces().Contains(typeof(IStonehengeAppCommands)));
            if (appCommandsType != null)
            {
                var appCommands = session?.CreateType("AppCommands", appCommandsType);

                var commandHandler = appCommands?.GetType().GetMethod(commandName);
                if (commandHandler != null)
                {
                    var cmdParameters = commandHandler.GetParameters()
                        .Select(parameter => parameter.ParameterType == typeof(AppSession)
                            ? session
                            : Convert.ChangeType(
                                parameters.FirstOrDefault(kv =>
                                    string.Equals(kv.Key, parameter.Name, StringComparison.OrdinalIgnoreCase)).Value,
                                parameter.ParameterType, CultureInfo.InvariantCulture));

                    commandHandler.Invoke(appCommands, cmdParameters.ToArray());

                    return Task.FromResult<Resource?>(new Resource(commandName, "Command", ResourceType.Json,
                        "{ 'executed': true }",
                        Resource.Cache.None));
                }

                return Task.FromResult<Resource?>(new Resource(commandName, "Command", ResourceType.Json,
                    "{ 'executed': false }",
                    Resource.Cache.None));
            }

            return Task.FromResult<Resource?>(new Resource(commandName, "Command", ResourceType.Json,
                "{ 'executed': false }",
                Resource.Cache.None));
        }

        if (resourceName.StartsWith("Data/", StringComparison.OrdinalIgnoreCase))
        {
            return PostDataResource(session, resourceName.Substring(5), parameters, formData);
        }

        if (!resourceName.StartsWith("ViewModel/", StringComparison.OrdinalIgnoreCase)) return Task.FromResult<Resource?>(null);

        var parts = resourceName.Split('/');
        if (parts.Length != 3) return Task.FromResult<Resource?>(null);

        var vmTypeName = parts[1];
        var methodName = parts[2];

        if (session?.ViewModel == null)
        {
            logger.LogWarning("ViewModelProvider: Set VM={VmTypeName}, no current VM", vmTypeName);
            session?.SetViewModelType(vmTypeName);
        }

        foreach (var (key, value) in formData)
        {
            logger.LogDebug("ViewModelProvider: Set {Key}={Value}", key, value);
            SetPropertyValue(logger, session?.ViewModel, key, value);
        }

        var vmType = session?.ViewModel?.GetType();
        if (!string.Equals(vmType?.Name, vmTypeName, StringComparison.Ordinal))
        {
            logger.LogWarning("ViewModelProvider: Request for VM={VmTypeName}, current VM={CurrentVmTypeName}",
                vmTypeName, vmType?.Name);
            return Task.FromResult<Resource?>(new Resource(resourceName, "ViewModelProvider", ResourceType.Json,
                "{ \"StonehengeContinuePolling\":false }", Resource.Cache.None));
        }

        var method = vmType?.GetMethod(methodName);
        if (method == null)
        {
            logger.LogWarning("ViewModelProvider: ActionMethod {MethodName} not found", methodName);
            Debugger.Break();
            return Task.FromResult<Resource?>(null);
        }

        try
        {
            var attribute = method
                .GetCustomAttributes(typeof(ActionMethodAttribute), true)
                .FirstOrDefault() as ActionMethodAttribute;
            var executeAsync = attribute?.ExecuteAsync ?? false;
            var methodParams = method.GetParameters()
                .Zip(
                    parameters.Values,
                    (parameterInfo, postParam) =>
                        new KeyValuePair<Type, object>(parameterInfo.ParameterType, postParam))
                .Select(parameterPair =>
                    Convert.ChangeType(parameterPair.Value, parameterPair.Key, CultureInfo.InvariantCulture))
                .ToArray();
            if (executeAsync)
            {
                var task = Task.Run(() => method.Invoke(session?.ViewModel, methodParams));
#pragma warning disable MA0042
                task.Wait(1000);
#pragma warning restore MA0042
                return GetEvents(session, CancellationToken.None, resourceName);
            }

            method.Invoke(session?.ViewModel, methodParams);
        }
        catch (Exception ex)
        {
            if (ex.InnerException != null) ex = ex.InnerException;

            logger.LogError("ViewModelProvider: ActionMethod {MethodName} has {Count} params",
                methodName, method.GetParameters().Length);
            logger.LogError("ViewModelProvider: Called with {Count} params: {Message}\r\n{StackTrace}",
                parameters.Count, ex.Message, ex.StackTrace);

            Debugger.Break();

            var exResource = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                { "Message", ex.Message },
                { "StackTrace", ex.StackTrace ?? string.Empty }
            };
            return Task.FromResult<Resource?>(new Resource(resourceName, "ViewModelProvider", ResourceType.Json,
                GetViewModelJson(exResource),
                Resource.Cache.None));
        }

        return Task.FromResult<Resource?>(new Resource(resourceName, "ViewModelProvider", ResourceType.Json,
            GetViewModelJson(session?.ViewModel), Resource.Cache.None));
    }

    public Task<Resource?> Get(AppSession? session, CancellationToken requestAborted, string resourceName,
        IDictionary<string, string> parameters)
    {
        if (resourceName.StartsWith("ViewModel/", StringComparison.OrdinalIgnoreCase))
        {
            if (session != null && SetViewModel(session, resourceName))
            {
                if (session.ViewModel is ActiveViewModel activeViewModel)
                {
                    activeViewModel.OnLoad();
                    activeViewModel.UpdateI18n();
                }

                return GetViewModel(session, resourceName);
            }
        }
        else if (resourceName.StartsWith("Events/", StringComparison.OrdinalIgnoreCase))
        {
            return GetEvents(session, requestAborted, resourceName);
        }
        else if (session != null && resourceName.StartsWith("Data/", StringComparison.OrdinalIgnoreCase))
        {
            return GetDataResource(session, resourceName.Substring(5), parameters);
        }

        return Task.FromResult<Resource?>(null);
    }

    private bool SetViewModel(AppSession? session, string resourceName)
    {
        if (session == null) return false;
        var vmTypeName = Path.GetFileNameWithoutExtension(resourceName);
        if (session.ViewModel != null)
        {
            ClearStonehengeInternalProperties(session.ViewModel as ActiveViewModel);
            if (string.Equals(session.ViewModel.GetType().Name, vmTypeName, StringComparison.Ordinal))
            {
                return true;
            }
        }

        if (session.SetViewModelType(vmTypeName) != null)
        {
            return true;
        }

        logger.LogError("Could not set ViewModel type to {VmTypeName}", vmTypeName);
        return false;
    }

    private Task<Resource?> GetViewModel(AppSession session, string resourceName)
    {
        session.EventsClear(true);

        return Task.FromResult<Resource?>(new Resource(resourceName, "ViewModelProvider", ResourceType.Json,
            GetViewModelJson(session.ViewModel),
            Resource.Cache.None));
    }

    private static async Task<Resource?> GetEvents(AppSession? session, CancellationToken requestAborted,
        string resourceName)
    {
        var parts = resourceName.Split('/');
        if (parts.Length < 2) return null;

        var vmTypeName = parts[1];
        var vmType = session?.ViewModel?.GetType();

        string json;
        if (!string.Equals(vmTypeName, vmType?.Name, StringComparison.Ordinal))
        {
            // view model changed !
            json = "{ \"StonehengeContinuePolling\":false";
            if (session == null)
            {
                json += ", \"StonehengeEval\":\"window.location.reload();\"";
            }

            json += " }";
            return new Resource(resourceName, "ViewModelProvider", ResourceType.Json, json, Resource.Cache.None);
        }

        var data = new List<string> { "\"StonehengeContinuePolling\":true" };
        var events = session == null
            ? []
            : await session.CollectEvents(requestAborted).ConfigureAwait(false);
        if (session?.ViewModel is ActiveViewModel activeVm)
        {
            try
            {
                // ReSharper disable once LoopCanBeConvertedToQuery
                foreach (var property in events)
                {
                    var value = activeVm.TryGetMember(property);
                    data.Add(
                        $"\"{property}\":{Encoding.UTF8.GetString(JsonSerializer.SerializeToUtf8Bytes(value, JsonOptions))}");
                }

                AddStonehengeInternalProperties(data, activeVm);
            }
            catch
            {
                // ignore for events
            }
        }

        json = "{" + string.Join(',', data) + "}";
        return new Resource(resourceName, "ViewModelProvider", ResourceType.Json, json, Resource.Cache.None);
    }

    private static void ClearStonehengeInternalProperties(ActiveViewModel? activeVm)
    {
        if (activeVm == null) return;

        // clear only if navigation happens
        activeVm.NavigateToRoute = string.Empty;
    }

    private static void AddStonehengeInternalProperties(ICollection<string> data, ActiveViewModel activeVm)
    {
        if (!string.IsNullOrEmpty(activeVm.MessageBoxTitle) || !string.IsNullOrEmpty(activeVm.MessageBoxText))
        {
            var title = activeVm.MessageBoxTitle;
            var text = activeVm.MessageBoxText;
            var script =
                $@"alert('{HttpUtility.JavaScriptStringEncode(title)}\r\n{HttpUtility.JavaScriptStringEncode(text)}');";
            data.Add(
                $"\"StonehengeEval\":{Encoding.UTF8.GetString(JsonSerializer.SerializeToUtf8Bytes(script, JsonOptions))}");
            activeVm.MessageBoxTitle = string.Empty;
            activeVm.MessageBoxText = string.Empty;
        }

        if (!string.IsNullOrEmpty(activeVm.ClientScript))
        {
            var script = activeVm.ClientScript;
            data.Add(
                $"\"StonehengeEval\":{Encoding.UTF8.GetString(JsonSerializer.SerializeToUtf8Bytes(script, JsonOptions))}");
            activeVm.ClientScript = string.Empty;
        }

        if (activeVm.UpdateRoutes)
        {
            var routes = AppPages.Pages
                .Select(page => $"\"{page.Route}\": {page.Visible.ToString().ToLower(CultureInfo.InvariantCulture)}")
                .ToArray();
            var json = "{ " + string.Join(", ", routes) + " }";
            data.Add($"\"StonehengeRoutes\":{json}");
            activeVm.UpdateRoutes = false;
        }

        if (!string.IsNullOrEmpty(activeVm.NavigateToRoute))
        {
            var route = activeVm.NavigateToRoute;
            data.Add(
                $"\"StonehengeNavigate\":{Encoding.UTF8.GetString(JsonSerializer.SerializeToUtf8Bytes(route, JsonOptions))}");
        }
    }

    private static Task<Resource?> GetDataResource(AppSession session, string resourceName,
        IDictionary<string, string> parameters)
    {
        var vm = session.ViewModel as ActiveViewModel;
        var method = vm?.GetType()
            .GetMethods()
            .FirstOrDefault(m =>
                string.Compare(m.Name, "GetDataResource", StringComparison.InvariantCultureIgnoreCase) == 0);
        if (method == null || method.ReturnType != typeof(Resource)) return Task.FromResult<Resource?>(null);

        Resource? data;
        if (method.GetParameters().Length == 2)
        {
            data = (Resource?)method.Invoke(vm, [resourceName, parameters]);
        }
        else
        {
            data = (Resource?)method.Invoke(vm, [resourceName]);
        }

        return Task.FromResult(data);
    }

    private static Task<Resource?> PostDataResource(AppSession? session, string resourceName,
        IDictionary<string, string> parameters, IDictionary<string, string> formData)
    {
        var vm = session?.ViewModel as ActiveViewModel;
        var method = vm?.GetType()
            .GetMethods()
            .FirstOrDefault(m =>
                string.Compare(m.Name, "PostDataResource", StringComparison.InvariantCultureIgnoreCase) == 0);
        if (method == null || method.ReturnType != typeof(Resource)) return Task.FromResult<Resource?>(null);

        Resource? data;
        if (method.GetParameters().Length == 3)
        {
            data = (Resource?)method.Invoke(vm, [resourceName, parameters, formData]);
        }
        else if (method.GetParameters().Length == 2)
        {
            data = (Resource?)method.Invoke(vm, [resourceName, parameters]);
        }
        else
        {
            data = (Resource?)method.Invoke(vm, [resourceName]);
        }

        return Task.FromResult(data);
    }

    // ReSharper disable once MemberCanBePrivate.Global
    public static void DeserializeStructValue(ILogger logger, string propName, ref object? structObj,
        string? structValue, Type structType)
    {
        try
        {
            if (string.IsNullOrEmpty(structValue))
            {
                structObj = null;
                return;
            }

            if (structValue.StartsWith("[", StringComparison.Ordinal))
            {
                var arrayObjects = JsonSerializer.Deserialize<JsonObject[]>(structValue);
                if (arrayObjects == null)
                {
                    structObj = null;
                    return;
                }

                var elementType = structType.GenericTypeArguments.FirstOrDefault();
                if (elementType == null)
                {
                    structObj = null;
                    return;
                }

                var addMethod = structObj?.GetType().GetMethod("Add");
                if (addMethod == null)
                {
                    structObj = null;
                    return;
                }

                foreach (var member in arrayObjects)
                {
                    var element = Activator.CreateInstance(elementType);
                    if (element == null) continue;

                    if (member is { } objMembers)
                    {
                        SetMembers(logger, ref element, elementType, objMembers);
                    }

                    addMethod.Invoke(structObj, [element]);
                }

                return;
            }

            if (JsonSerializer.Deserialize<JsonObject>(structValue) is { } members)
            {
                SetMembers(logger, ref structObj, structType, members);
            }
        }
        catch (Exception ex)
        {
            logger.LogError("ViewModelProvider.DeserializeStructValue({StructName}): {Message}", structType.Name,
                ex.Message);
            Debugger.Break();
        }
    }

    private static void SetMembers(ILogger logger, ref object? structObj, Type structType, JsonObject members)
    {
        foreach (var member in members)
        {
            var mProp = structType.GetProperty(member.Key);
            if (mProp != null && member.Value != null)
            {
                try
                {
                    var val = DeserializePropertyValue(logger, mProp.Name, member.Value.ToString(), mProp.PropertyType);
                    mProp.SetValue(structObj, val, null);
                }
                catch (Exception ex)
                {
                    logger.LogError("ViewModelProvider.SetMembers.SetValue({StructName}.{PropertyName}): {Message}",
                        structType.Name, mProp.Name, ex.Message);
                }
            }
        }
    }

    // ReSharper disable once MemberCanBePrivate.Global
    internal static object? DeserializePropertyValue(ILogger logger, string propName, string? propValue, Type propType)
    {
        try
        {
            if (propType == typeof(string))
                return propValue;
            if (propType == typeof(bool) && !string.IsNullOrEmpty(propValue))
                return bool.Parse(propValue);
            if (propType == typeof(float))
            {
                if (float.TryParse(propValue, NumberStyles.Float, CultureInfo.CurrentCulture, out var fVal))
                    return fVal;
                if (float.TryParse(propValue, NumberStyles.Float, CultureInfo.InvariantCulture, out fVal))
                    return fVal;
            }

            if (propType == typeof(double))
            {
                if (double.TryParse(propValue, NumberStyles.Float, CultureInfo.CurrentCulture, out var dVal))
                    return dVal;
                if (double.TryParse(propValue, NumberStyles.Float, CultureInfo.InvariantCulture, out dVal))
                    return dVal;
            }

            if (propType == typeof(DateTime))
            {
                if (DateTime.TryParse(propValue, CultureInfo.CurrentUICulture, out var dt))
                    return dt;
                if (DateTime.TryParse(propValue, CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
                    return dt;
            }

            if (propType == typeof(DateTimeOffset))
            {
                if (DateTimeOffset.TryParse(propValue, CultureInfo.CurrentUICulture, out var dt))
                    return dt;
                if (DateTimeOffset.TryParse(propValue, CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
                    return dt;
            }

            if (propType is { IsClass: true, IsArray: false })
            {
                var structObj = Activator.CreateInstance(propType);
                if (structObj != null)
                {
                    DeserializeStructValue(logger, propName, ref structObj, propValue, propType);
                }

                return structObj;
            }

            if (propValue != null)
            {
                return JsonSerializer.Deserialize(propValue, propType);
            }
        }
        catch (Exception ex)
        {
            logger.LogError("DeserializePropertyValue({Name}) = '{Value}': {Message}", propName, propValue, ex.Message);
            Debugger.Break();
        }

        return null;
    }

    private static void SetPropertyValue(ILogger logger, object? vm, string propName, string newValue)
    {
        try
        {
            if (vm is ActiveViewModel activeVm)
            {
                var pi = activeVm.GetPropertyInfo(propName);
                if (pi == null || !pi.CanWrite)
                    return;

                if (pi.PropertyType is { IsValueType: true, IsPrimitive: false } &&
                    !string.Equals(pi.PropertyType.Namespace, "System", StringComparison.Ordinal)) // struct
                {
                    var structObj = activeVm.TryGetMember(propName);
                    if (structObj != null && !string.IsNullOrEmpty(newValue) && newValue.Trim().StartsWith('{'))
                    {
                        DeserializeStructValue(logger, pi.Name, ref structObj, newValue, pi.PropertyType);
                        activeVm.TrySetMember(propName, structObj);
                    }
                }
                else if (pi.PropertyType.IsGenericType &&
                         pi.PropertyType.Name.StartsWith("Notify`", StringComparison.OrdinalIgnoreCase))
                {
                    var val = DeserializePropertyValue(logger, pi.Name, newValue,
                        pi.PropertyType.GenericTypeArguments[0]);
                    var type = typeof(Notify<>).MakeGenericType(pi.PropertyType.GenericTypeArguments[0]);
                    var notify = Activator.CreateInstance(type, new[] { activeVm, pi.Name, val });
                    var valueField = type.GetField("_value", BindingFlags.Instance | BindingFlags.NonPublic);
                    valueField?.SetValue(notify, val);
                    activeVm.TrySetMember(propName, notify);
                }
                else
                {
                    var val = DeserializePropertyValue(logger, pi.Name, newValue, pi.PropertyType);
                    activeVm.TrySetMember(propName, val);
                }
            }
            else
            {
                var pi = vm?.GetType().GetProperty(propName);
                if (pi == null || !pi.CanWrite)
                    return;

                var val = DeserializePropertyValue(logger, pi.Name, newValue, pi.PropertyType);
                pi.SetValue(vm, val, null);
            }
        }
        catch (Exception ex)
        {
            logger.LogError("SetPropertyValue({PropName}): {Message}", propName, ex.Message);
            Debugger.Break();
        }
    }

    private string GetViewModelJson(object? viewModel)
    {
        if (viewModel == null) return string.Empty;

        var watch = new Stopwatch();
        watch.Start();

        var ty = viewModel.GetType();
        logger.LogDebug("ViewModelProvider: ViewModel={VmTypeName}", ty.Name);

        var data = new List<string>();
        var context = string.Empty;
        try
        {
            // ensure view model data available before executing client scripts
            context = "view model";
            using var jsonDocument = JsonSerializer.SerializeToDocument(viewModel, JsonOptions);
#pragma warning disable IDISP004
            data.AddRange(jsonDocument.RootElement.EnumerateObject()
                .Select(jsonElement => jsonElement.ToString()));
#pragma warning restore IDISP004

            if (viewModel is ActiveViewModel activeVm)
            {
                context = "internal properties";
                AddStonehengeInternalProperties(data, activeVm);

                context = "dictionary names";
                // ReSharper disable once LoopCanBeConvertedToQuery
                foreach (var name in activeVm.GetDictionaryNames())
                {
                    // ReSharper disable once UseStringInterpolation
                    data.Add(
                        $"\"{name}\":{JsonSerializer.SerializeToElement(activeVm.TryGetMember(name), JsonOptions)}");
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError("Exception serializing ViewModel({VmTypeName}) {Context} : {Message}\r\n{StackTrace}",
                ty.Name, context, ex.Message, ex.StackTrace);

            Debugger.Break();

            var exResource = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                { "Message", ex.Message },
                { "StackTrace", ex.StackTrace ?? string.Empty }
            };
            return JsonSerializer.SerializeToElement(exResource, JsonOptions).ToString();
        }

        var json = "{" + string.Join(",", data) + "}";

        watch.Stop();
        logger.LogTrace("GetViewModelJson: {ElapsedMilliseconds}ms", watch.ElapsedMilliseconds);
        return json;
    }
}