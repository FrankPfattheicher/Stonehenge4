﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using IctBaden.Stonehenge.Core;
using IctBaden.Stonehenge.Hosting;
using IctBaden.Stonehenge.Resources;
using IctBaden.Stonehenge.ViewModel;
using Microsoft.Extensions.Logging;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable TemplateIsNotCompileTimeConstantProblem

namespace IctBaden.Stonehenge.Vue.Client;

[SuppressMessage("Usage", "CA2254:Vorlage muss ein statischer Ausdruck sein")]
[SuppressMessage("Usage", "MA0011:IFormatProvider is missing")]
internal class VueAppCreator
{
    private readonly ILogger _logger;
    private readonly StonehengeResourceLoader _loader;
    private readonly StonehengeHostOptions _options;
    private readonly Assembly _appAssembly;
    private readonly Assembly _vueAssembly;
    private readonly Dictionary<string, Resource> _vueContent;

    private readonly string _controllerTemplate;
    private readonly string _elementTemplate;

    public VueAppCreator(ILogger logger, StonehengeResourceLoader loader, StonehengeHostOptions options,
        Assembly appAssembly, Dictionary<string, Resource> vueContent)
    {
        _logger = logger;
        _loader = loader;
        _options = options;
        _appAssembly = appAssembly;
        _vueContent = vueContent;
        _vueAssembly = Assembly.GetAssembly(typeof(VueAppCreator))!;

        _controllerTemplate = LoadResourceText(_vueAssembly, "IctBaden.Stonehenge.Vue.Client.stonehengeComponent.js");
        _elementTemplate = LoadResourceText(_vueAssembly, "IctBaden.Stonehenge.Vue.Client.stonehengeElement.js");
    }

    private async Task<string> LoadResourceText(string resourceName)
    {
        var resource = await _loader.Get(AppSession.None, CancellationToken.None, resourceName, new Dictionary<string, string>(StringComparer.Ordinal)).ConfigureAwait(false);
        return resource?.Text ?? LoadResourceText(_appAssembly, resourceName);
    }

    private string LoadResourceText(Assembly assembly, string resourceName)
    {
        var resourceText = string.Empty;

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null) return resourceText;
        using var reader = new StreamReader(stream);
        resourceText = reader.ReadToEnd();

        return resourceText;
    }

    public void CreateApplication(ViewModelInfo[] contentPages)
    {
        var applicationJs = LoadResourceText(_vueAssembly, "IctBaden.Stonehenge.Vue.Client.stonehengeApp.js");
        applicationJs = InsertRoutes(applicationJs, contentPages);
        applicationJs = InsertElements(applicationJs);

        var resource = new Resource("app.js", "VueResourceProvider", ResourceType.Js, applicationJs,
            Resource.Cache.Revalidate);
        _vueContent.Add("app.js", resource);
    }

    private string InsertRoutes(string pageText, ViewModelInfo[] contentPages)
    {
        const string routesInsertPoint = "//stonehengeAppRoutes";
        const string stonehengeAppTitleInsertPoint = "stonehengeAppTitle";
        const string stonehengeRootPageInsertPoint = "stonehengeRootPage";
        const string stonehengeAppHandleWindowResized = "stonehengeAppHandleWindowResized";
        const string pageTemplate =
            "{{ path: '{0}', name: '{1}', title: '{2}', component: () => Promise.resolve(stonehengeLoadComponent('{3}')), visible: {4} }}";

        var pages = contentPages
            .Select(vmInfo => string.Format(pageTemplate,
                "/" + vmInfo.Route,
                vmInfo.Route,
                vmInfo.Title,
                vmInfo.Route,
                vmInfo.Visible ? "true" : "false"))
            .ToList();

        var startPageName = _options.StartPage;
        if (contentPages.Length == 0)
        {
            _logger.LogError("VueAppCreator: No content pages found");
        }
        else if (string.IsNullOrEmpty(startPageName))
        {
            startPageName = contentPages.FirstOrDefault(vmInfo => vmInfo.Visible)?.Route ?? string.Empty;
        }

        if (string.IsNullOrEmpty(startPageName))
        {
            _logger.LogError("VueAppCreator: No content start page found");
        }
        else
        {
            startPageName = startPageName.Replace('-', '_');
        }

        var startPage = contentPages.FirstOrDefault(page => string.Equals(page.Route, startPageName, StringComparison.Ordinal));
        if (startPage != null)
        {
            pages.Insert(0, string.Format(pageTemplate, string.Empty, string.Empty, startPage.Title, startPage.Route, "false"));
        }

        var routes = string.Join("," + Environment.NewLine, pages);
        pageText = pageText
            .Replace(routesInsertPoint, routes)
            .Replace(stonehengeAppTitleInsertPoint, _options.Title)
            .Replace(stonehengeRootPageInsertPoint, startPageName)
            .Replace(stonehengeAppHandleWindowResized, _options.HandleWindowResized.ToString().ToLower());

        return pageText;
    }


    public void CreateComponents(StonehengeResourceLoader resourceLoader)
    {
        var viewModels = _vueContent
            .Where(res => string.IsNullOrEmpty(res.Value.ViewModel?.ElementName) && res.Value.ViewModel?.VmName != null)
            .Select(res => res.Value)
            .Distinct()
            .ToList();

        foreach (var viewModel in viewModels)
        {
            var vmName = viewModel.ViewModel?.VmName;
            if (string.IsNullOrEmpty(vmName)) continue;
            
            var controllerJs = GetController(vmName, resourceLoader);
            if (string.IsNullOrEmpty(controllerJs)) continue;
            
            try
            {
                _logger.LogInformation("VueAppCreator.CreateComponents: {VmName} => src.{ViewModelName}.js",
                    vmName, viewModel.Name);

                var name = _appAssembly?.GetManifestResourceNames()
                    .FirstOrDefault(rn => rn.EndsWith($".app.{viewModel.Name}_user.js", StringComparison.OrdinalIgnoreCase));
                if (!string.IsNullOrEmpty(name) && _appAssembly != null)
                {
                    var userJs = LoadResourceText(_appAssembly, name);
                    if (!string.IsNullOrWhiteSpace(userJs))
                    {
                        controllerJs += userJs;
                    }
                }

                var resource = new Resource($"{viewModel.Name}.js", "ViewModel", ResourceType.Js, controllerJs,
                    Resource.Cache.Revalidate);
                _vueContent.Add(resource.Name, resource);
            }
            catch (Exception ex)
            {
                _logger.LogError("VueAppCreator.CreateComponents: {VmName} EXCEPTION: {Message}",
                    vmName, ex.Message);
                Debugger.Break();
            }
        }
    }

    private static Type[] GetAssemblyTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (Exception)
        {
            return Type.EmptyTypes;
        }
    }

    private static Type? GetVmType(string name)
    {
        return AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(GetAssemblyTypes)
            .FirstOrDefault(type => string.Compare(type.Name, name, StringComparison.InvariantCultureIgnoreCase) == 0);
    }

    private static readonly bool DebugBuild = Assembly.GetEntryAssembly()?.GetCustomAttributes(false)
        .OfType<DebuggableAttribute>().Any(da => da.IsJITTrackingEnabled) ?? true;

    private string? GetController(string vmName, StonehengeResourceLoader resourceLoader)
    {
        var vmType = GetVmType(vmName);
        if (vmType == null)
        {
            _logger.LogError("No VM for type {VmName} defined", vmName);
            return null;
        }

        var viewModel = CreateViewModel(vmType, resourceLoader, new AppSessions());

        var text = _controllerTemplate
            .Replace("stonehengeDebugBuild", DebugBuild ? "true" : "false")
            .Replace("stonehengeViewModelName", vmName)
            .Replace("stonehengePollDelay", _options.GetPollDelayMs().ToString())
            .Replace("stonehengePollRetries", _options.PollRetries.ToString());

        var propertyNames = GetPropNames(viewModel);
        if (propertyNames.Count > 0)
        {
            var propDefinitions = propertyNames.Select(pn => pn + " : ''\r\n");
            text = text.Replace("//stonehengeProperties", "," + string.Join(",", propDefinitions));
        }

        var postBackPropNames = GetPostBackPropNames(viewModel, propertyNames)
            .Select(name => "'" + name + "'");
        text = text.Replace("'propNames'", string.Join(",", postBackPropNames));

        // supply functions for action methods
        const string methodTemplate =
            "stonehengeMethodName: function({paramNames}) { app.stonehengeViewModelName.StonehengePost('ViewModel/stonehengeViewModelName/stonehengeMethodName{paramValues}'); }";

        var actionMethods = new List<string>();
        foreach (var methodInfo in vmType.GetMethods().Where(methodInfo =>
                     methodInfo.GetCustomAttributes(false).OfType<ActionMethodAttribute>().Any()))
        {
            //var method = (methodInfo.GetParameters().Length > 0)
            //  ? "%method%: function (data, event, param) { if(!IsLoading()) post_ViewModelName_Data(self, event.currentTarget, '%method%', param); },".Replace("%method%", methodInfo.Name)
            //  : "%method%: function (data, event) { if(!IsLoading()) post_ViewModelName_Data(self, event.currentTarget, '%method%', null); },".Replace("%method%", methodInfo.Name);

            var paramNames = methodInfo.GetParameters().Select(p => p.Name).ToArray();
            var paramValues = paramNames.Any()
                ? "?" + string.Join("&", paramNames.Select(n => string.Format("{0}='+encodeURIComponent({0})+'", n)))
                : string.Empty;

            var method = methodTemplate
                .Replace("stonehengeViewModelName", vmName)
                .Replace("stonehengeMethodName", methodInfo.Name)
                .Replace("stonehengePollDelay", _options.GetPollDelayMs().ToString())
                .Replace("{paramNames}", string.Join(",", paramNames))
                .Replace("{paramValues}", paramValues)
                .Replace("+''", string.Empty);

            actionMethods.Add(method);
        }

        var disposeVm = viewModel as IDisposable;
        disposeVm?.Dispose();

        return text.Replace("/*commands*/", string.Join("," + Environment.NewLine, actionMethods));
    }

    private object? CreateViewModel(Type vmType, StonehengeResourceLoader resourceLoader, AppSessions appSessions)
    {
        try
        {
            using var session = new AppSession(resourceLoader, _options, appSessions);
            var viewModel = session.CreateType("CreateViewModel", vmType);
            if (viewModel == null)
            {
                _logger.LogCritical("Failed to create ViewModel '{VmTypeName}' : Missing public ctor?", vmType.Name);
            }
            return viewModel;
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to create ViewModel '{VmTypeName}' : {Message}", 
                vmType.Name, ex.Message);
            Debugger.Break();
            return null;
        }
    }


    private static List<string> GetPropNames(object? viewModel)
    {
        // properties
        if (viewModel == null) return [];

        var vmProps = new List<PropertyDescriptor>();
        if (viewModel is ActiveViewModel activeVm)
        {
            vmProps.AddRange(from PropertyDescriptor prop in activeVm.GetProperties() select prop);
        }
        else
        {
            vmProps.AddRange(TypeDescriptor.GetProperties(viewModel, true).Cast<PropertyDescriptor>());
        }

        // ReSharper disable once IdentifierTypo
        var propertyNames = (from prop in vmProps
            let bindable = prop.Attributes.OfType<BindableAttribute>().ToArray()
            where (bindable.Length <= 0) || bindable[0].Bindable
            select prop.Name).ToList();

        return propertyNames;
    }

    private static List<string> GetPostBackPropNames(object? viewModel, IEnumerable<string> propertyNames)
    {
        if (viewModel == null) return [];

        var postBackPropNames = new List<string>();
        var activeVm = viewModel as ActiveViewModel;
        var vmType = viewModel.GetType();
        foreach (var propName in propertyNames)
        {
            // do not send ReadOnly or OneWay bound properties back
            var prop = vmType.GetProperty(propName);
            if (prop == null)
            {
                if (activeVm == null)
                    continue;
                prop = activeVm.GetPropertyInfo(propName);
                if (activeVm.IsPropertyReadOnly(propName))
                    continue;
            }

            if (prop?.GetSetMethod(false) == null) // not public writable
                continue;
            // ReSharper disable once IdentifierTypo
            var bindable = prop.GetCustomAttributes(typeof(BindableAttribute), true);
            if ((bindable.Length > 0) && ((BindableAttribute)bindable[0]).Direction == BindingDirection.OneWay)
                continue;
            postBackPropNames.Add(propName);
        }

        return postBackPropNames;
    }

    private string InsertElements(string pageText)
    {
        const string elementsInsertPoint = "//stonehengeElements";
        var elements = CreateElements().Result;

        return pageText.Replace(elementsInsertPoint, string.Join(Environment.NewLine, elements));
    }

    public async Task<List<string>> CreateElements()
    {
        var customElements = _vueContent
            .Where(res => !string.IsNullOrEmpty(res.Value.ViewModel?.ElementName))
            .Select(res => res.Value)
            .Distinct()
            .ToList();

        var elements = new List<string>();
        foreach (var element in customElements)
        {
            try
            {
                var elementJs = _elementTemplate.Replace("stonehengeCustomElementName", element.ViewModel!.ElementName, StringComparison.Ordinal);

                var source = Path.GetFileNameWithoutExtension(ResourceLoader.RemoveResourceProtocol(element.Source));
                elementJs = elementJs.Replace("stonehengeViewModelName", source, StringComparison.Ordinal);

                var bindings = new List<string>();
                if (element.ViewModel != null)
                {
                    bindings.AddRange(element.ViewModel.Bindings.Select(b => $"'{b}'"));
                }
                elementJs = elementJs.Replace("stonehengeCustomElementProps", string.Join(',', bindings), StringComparison.Ordinal);

                var template = await LoadResourceText($"{source}.html").ConfigureAwait(false);
                template = JsonSerializer.Serialize(template);
                elementJs = elementJs.Replace("'stonehengeElementTemplate'", template, StringComparison.Ordinal);

                var methods = await LoadResourceText($"{source}.js").ConfigureAwait(false);
                if (!string.IsNullOrEmpty(methods)) methods = "," + methods;
                elementJs = elementJs.Replace("//stonehengeElementMethods", methods, StringComparison.Ordinal);

                if (!string.IsNullOrEmpty(element.ViewModel?.VmName))
                {
                    var actionMethods = GetActionMethods(element.ViewModel.VmName);
                    if (!string.IsNullOrEmpty(actionMethods))
                    {
                        methods = ", methods: {" + actionMethods + "}";
                        elementJs = elementJs.Replace("//stonehengeElementActions", methods, StringComparison.Ordinal);
                    }
                }
                
                elements.Add(elementJs);

                var resource = new Resource($"{element.Name}.js", "VueResourceProvider", ResourceType.Js, elementJs,
                    Resource.Cache.Revalidate);
                _vueContent.Add(resource.Name, resource);
            }
            catch (Exception ex)
            {
                _logger.LogError("VueAppCreator.CreateComponents: {ElementName} EXCEPTION: {Message}",
                    element.Name, ex.Message);
                Debugger.Break();
            }
        }

        return elements;
    }
    
    private static string GetActionMethods(string vmName)
    {
        var vmType = GetVmType(vmName);
        if (vmType == null) return string.Empty;
 
        const string methodTemplate =
            "stonehengeMethodName: function({paramNames}) { app[app['activeViewModelName']]\n.StonehengePost('ViewModel/' + app['activeViewModelName'] + '/' + this.model.ComponentId + '/stonehengeMethodName{paramValues}'); }";

        var actionMethods = new List<string>();
        foreach (var methodInfo in vmType.GetMethods().Where(methodInfo =>
                     methodInfo.GetCustomAttributes(false).OfType<ActionMethodAttribute>().Any()))
        {
            var paramNames = methodInfo.GetParameters().Select(p => p.Name).ToArray();
            var paramValues = paramNames.Any()
                ? "?" + string.Join("&", paramNames.Select(n => string.Format("{0}='+encodeURIComponent({0})+'", n)))
                : string.Empty;

            var method = methodTemplate
                .Replace("stonehengeMethodName", methodInfo.Name)
                .Replace("{paramNames}", string.Join(",", paramNames))
                .Replace("{paramValues}", paramValues)
                .Replace("+''", string.Empty);

            actionMethods.Add(method);
        }
       
        return string.Join(", ", actionMethods);
    }
    
}