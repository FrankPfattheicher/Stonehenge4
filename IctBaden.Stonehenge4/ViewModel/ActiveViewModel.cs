// ActiveViewModel.cs
//
// Author:
//  Frank Pfattheicher <fpf@ict-baden.de>
//
// Copyright (C)2011-2025 ICT Baden GmbH
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Timers;
using IctBaden.Stonehenge.Core;
using IctBaden.Stonehenge.Resources;
using IctBaden.Stonehenge.Types;
using Microsoft.Extensions.Logging;
using Timer = System.Timers.Timer;

// ReSharper disable TemplateIsNotCompileTimeConstantProblem

// ReSharper disable MemberCanBeProtected.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable FieldCanBeMadeReadOnly.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedField.Global
// ReSharper disable VirtualMemberNeverOverridden.Global

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Local

namespace IctBaden.Stonehenge.ViewModel;

public class ActiveViewModel : DynamicObject, ICustomTypeDescriptor, INotifyPropertyChanged, IDisposable
{
    #region helper classes

    class GetMemberBinderEx(string name) : GetMemberBinder(name, false)
    {
        public override DynamicMetaObject FallbackGetMember(DynamicMetaObject target,
            DynamicMetaObject? errorSuggestion)
        {
            return null!;
        }
    }

    class SetMemberBinderEx(string name) : SetMemberBinder(name, false)
    {
        public override DynamicMetaObject FallbackSetMember(DynamicMetaObject target, DynamicMetaObject value,
            DynamicMetaObject? errorSuggestion)
        {
            return null!;
        }
    }

    class PropertyDescriptorEx : PropertyDescriptor
    {
        private readonly string _propertyName;
        private readonly PropertyDescriptor? _originalDescriptor;
        private readonly bool _readOnly;

        internal PropertyDescriptorEx(string name, PropertyInfo? info, bool readOnly)
            : base(name, null)
        {
            _propertyName = name;
            _originalDescriptor = FindOrigPropertyDescriptor(info);
            _readOnly = readOnly;
        }

        public override AttributeCollection Attributes => _originalDescriptor?.Attributes ?? base.Attributes;

        public override object? GetValue(object? component)
        {
            if (!(component is DynamicObject dynComponent))
                return _originalDescriptor?.GetValue(component);

            return dynComponent.TryGetMember(new GetMemberBinderEx(_propertyName), out var result)
                ? result
                : _originalDescriptor?.GetValue(component);
        }

        public override void SetValue(object? component, object? value)
        {
            if (component is DynamicObject dynComponent)
            {
                if (dynComponent.TrySetMember(new SetMemberBinderEx(_propertyName), value))
                    return;
            }

            _originalDescriptor?.SetValue(component, value);
        }

        public override bool IsReadOnly => _readOnly || (_originalDescriptor is { IsReadOnly: true });

        public override Type PropertyType
            => _originalDescriptor == null ? typeof(object) : _originalDescriptor.PropertyType;

        public override bool CanResetValue(object component)
            => _originalDescriptor != null && _originalDescriptor.CanResetValue(component);

        public override Type ComponentType
            => _originalDescriptor == null ? typeof(object) : _originalDescriptor.ComponentType;

        public override void ResetValue(object component)
            => _originalDescriptor?.ResetValue(component);

        public override bool ShouldSerializeValue(object component)
            => _originalDescriptor != null && _originalDescriptor.ShouldSerializeValue(component);

        private static PropertyDescriptor? FindOrigPropertyDescriptor(PropertyInfo? propertyInfo)
        {
            return propertyInfo == null || propertyInfo.DeclaringType == null
                ? null
                : TypeDescriptor.GetProperties(propertyInfo.DeclaringType)
                    .Cast<PropertyDescriptor>()
                    .FirstOrDefault(propertyDescriptor => propertyDescriptor.Name.Equals(propertyInfo.Name));
        }
    }

    class PropertyInfoEx(PropertyInfo pi, object obj, bool readOnly)
    {
        public PropertyInfo Info { get; private set; } = pi;
        public object Obj { get; private set; } = obj;
        public bool ReadOnly { get; private set; } = readOnly;
    }

    #endregion

    #region properties

    internal static readonly Type[] I18Types = AppDomain.CurrentDomain.GetAssemblies()
        .SelectMany(a => a.GetTypes())
        .Where(type => type.Name.EndsWith("I18n", StringComparison.OrdinalIgnoreCase))
        .ToArray();

    public string[] I18Names = [];

    // ReSharper disable once InconsistentNaming
    // ReSharper disable once CollectionNeverQueried.Global
    public IDictionary<string, string> I18n { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    private readonly Dictionary<string, List<string>> _dependencies = new(StringComparer.Ordinal);
    private readonly Dictionary<string, object?> _dictionary = new(StringComparer.Ordinal);

    [Browsable(false)] internal int Count => GetProperties().Count;

#pragma warning disable IDISP008
    [Browsable(false)] public AppSession Session;
#pragma warning restore IDISP008
    [Browsable(false)] public bool SupportsEvents;

    // ReSharper disable InconsistentNaming
    [Bindable(false)] public string _stonehenge_CommandSenderName_ { get; set; } = string.Empty;

    private Timer? _updateTimer;

    public string GetCommandSenderName()
    {
        return _stonehenge_CommandSenderName_;
    }

    #endregion

    public ActiveViewModel()
        : this(null)
    {
    }

    public ActiveViewModel(AppSession? session)
    {
        SupportsEvents = session != null;
        Session = session ?? new AppSession();

        foreach (var component in GetComponents())
        {
            component.Session = Session;
            component.SupportsEvents = SupportsEvents;
            component.SetParent(this);
        }

        foreach (var prop in GetType().GetProperties())
        {
            if (prop.PropertyType.IsGenericType &&
                prop.PropertyType.Name.StartsWith("Notify`", StringComparison.Ordinal))
            {
                var type = typeof(Notify<>).MakeGenericType(prop.PropertyType.GenericTypeArguments[0]);
                var property = prop.GetValue(this);
                if (property != null) continue;
                var ctor = type.GetConstructors(BindingFlags.Instance | BindingFlags.Public)
                    .First(c => c.GetParameters().Length == 2);
                property = ctor.Invoke([this, prop.Name]);
                prop.SetValue(this, property);
            }
        }

        GetProperties();
    }


    public void UpdateI18n()
    {
        I18n.Clear();
        foreach (var i18Type in I18Types)
        {
            var cult = i18Type
                .GetProperties(BindingFlags.Static | BindingFlags.NonPublic)
                .FirstOrDefault(property => property.PropertyType == typeof(CultureInfo));
            cult?.SetValue(this, Session.SessionCulture);
            
            var texts = i18Type
                .GetProperties(BindingFlags.Static | BindingFlags.NonPublic)
                .Where(property => property.PropertyType == typeof(string))
                .ToArray();

            foreach (var propertyInfo in texts)
            {
                if(!I18Names.Contains(propertyInfo.Name, StringComparer.OrdinalIgnoreCase)) continue;
                I18n.Add(propertyInfo.Name, propertyInfo.GetValue(null)?.ToString() ?? string.Empty);
            }
        }
        foreach (var component in GetComponents())
        {
            component.UpdateI18n();
        }

        NotifyPropertyChanged(nameof(I18n));
    }

    internal StonehengeComponent[] GetComponents()
    {
        var componentProperties = GetType()
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(property => property.PropertyType.IsSubclassOf(typeof(StonehengeComponent)))
            .ToArray();
        
        var components = new List<StonehengeComponent>();
        foreach (var property in componentProperties)
        {
            if (property.GetValue(this) is StonehengeComponent component)
            {
                components.Add(component);
            }
        }
        return components.ToArray();
    }

    internal void ActiveViewModelOnLoad()
    {
        if (!SupportsEvents)
        {
            Session.EventsClear(forceEnd: true);
        }

        foreach (PropertyDescriptorEx sp in sessionProperties)
        {
            var name = sp.Name;
            var sv = Session.Get<object?>(name);
            if(sv == null) continue;

            Session.Logger.LogDebug("Restore session property {Name} = {Value}", name, sv);
            sp.SetValue(this, sv); 
        }
        foreach (var sf in sessionFields)
        {
            var name = sf.Name;
            var sv = Session.Get<object?>(name);
            if(sv == null) continue;

            Session.Logger.LogDebug("Restore session field {Name} = {Value}", name, sv);
            sf.SetValue(this, sv);
        }
        
        foreach (var component in GetComponents())
        {
            component.I18Names = component.GetI18Names();
            component.OnLoad();
        }
    }
    
    /// <summary>
    /// Called when application navigates to this view model.
    /// This is an equivalent to a client site onload event.
    /// </summary>
    public virtual void OnLoad()
    {
    }

    protected void SetParent(ActiveViewModel parent)
    {
        PropertyChanged += (_, args) =>
        {
            if (!string.IsNullOrEmpty(args.PropertyName))
            {
                parent.NotifyPropertyChanged(args.PropertyName);
            }
        };
    }

    public object? TryGetMember(string name)
    {
        TryGetMember(new GetMemberBinderEx(name), out var result);
        return result;
    }

    public void TrySetMember(string name, object? value)
    {
        TrySetMember(new SetMemberBinderEx(name), value);
    }

    [Browsable(false)]
    protected object? this[string name]
    {
        get => TryGetMember(name);
        set => TrySetMember(name, value);
    }

    #region DynamicObject

    public override IEnumerable<string> GetDynamicMemberNames()
    {
        var names = new List<string>();
        names.AddRange(from elem in _dictionary select elem.Key);
        return names;
    }

    public IEnumerable<string> GetDictionaryNames()
    {
        return _dictionary.Select(e => e.Key);
    }

    private PropertyInfoEx? GetPropertyInfoEx(string name)
    {
        var pi = GetType().GetProperty(name);
        return pi != null ? new PropertyInfoEx(pi, this, false) : null;
    }

    public PropertyInfo? GetPropertyInfo(string name)
    {
        var infoEx = GetPropertyInfoEx(name);
        return infoEx?.Info;
    }

    public bool IsPropertyReadOnly(string name)
    {
        var infoEx = GetPropertyInfoEx(name);
        return (infoEx == null) || infoEx.ReadOnly;
    }

    public override bool TryGetMember(GetMemberBinder binder, out object? result)
    {
        var pi = GetPropertyInfoEx(binder.Name);
        if (pi != null)
        {
            var val = pi.Info.GetValue(pi.Obj, null);
            result = val;
            return true;
        }

        return _dictionary.TryGetValue(binder.Name, out result);
    }

    public override bool TrySetMember(SetMemberBinder binder, object? value)
    {
        var pi = GetPropertyInfoEx(binder.Name);
        if (pi != null)
        {
            pi.Info.SetValue(pi.Obj, value, null);
            NotifyPropertyChanged(binder.Name);
            return true;
        }

        _dictionary[binder.Name] = value;
        NotifyPropertyChanged(binder.Name);
        return true;
    }

    #endregion

    #region ICustomTypeDescriptor

    public AttributeCollection GetAttributes()
    {
        return TypeDescriptor.GetAttributes(this, true);
    }

    public string GetClassName()
    {
        return TypeDescriptor.GetClassName(this, true) ?? string.Empty;
    }

    public string GetComponentName()
    {
        return TypeDescriptor.GetComponentName(this, true) ?? string.Empty;
    }

    public TypeConverter GetConverter()
    {
        return TypeDescriptor.GetConverter(this, true);
    }

    public EventDescriptor? GetDefaultEvent()
    {
        return TypeDescriptor.GetDefaultEvent(this, true);
    }

    public PropertyDescriptor? GetDefaultProperty()
    {
        return TypeDescriptor.GetDefaultProperty(this, true);
    }

    public object? GetEditor(Type editorBaseType)
    {
        return TypeDescriptor.GetEditor(this, editorBaseType, true);
    }

    public EventDescriptorCollection GetEvents(Attribute[]? attributes)
    {
        return TypeDescriptor.GetEvents(this, attributes, true);
    }

    public EventDescriptorCollection GetEvents()
    {
        return TypeDescriptor.GetEvents(this, true);
    }

    private PropertyDescriptorCollection? properties;
    private readonly PropertyDescriptorCollection sessionProperties = new ([]);
    private readonly List<FieldInfo> sessionFields = [];

    public PropertyDescriptorCollection GetProperties()
    {
        if (properties != null) return properties;

        properties = new PropertyDescriptorCollection([]);
        foreach (var prop in GetType().GetProperties())
        {
            var pi = GetType().GetProperty(prop.Name);
            var desc = new PropertyDescriptorEx(prop.Name, pi, false);
            properties.Add(desc);

            var sessionVariableAttribute = prop.GetCustomAttribute<SessionVariableAttribute>();
            if (sessionVariableAttribute != null)
            {
                Session.Logger.LogDebug("Adding session property {Name}", prop.Name);
                sessionProperties.Add(desc);
            }
        }
        foreach (var field in GetAllFields(GetType()))
        {
            var sessionVariableAttribute = field.GetCustomAttribute<SessionVariableAttribute>();
            if (sessionVariableAttribute != null)
            {
                Session.Logger.LogDebug("Adding session field {Name}", field.Name);
                sessionFields.Add(field);
            }
        }
        foreach (var elem in _dictionary)
        {
            var desc = new PropertyDescriptorEx(elem.Key, null, false);
            properties.Add(desc);
        }
        foreach (PropertyDescriptorEx prop in properties)
        {
            foreach (Attribute attribute in prop.Attributes)
            {
                if (attribute.GetType() != typeof(DependsOnAttribute))
                    continue;
                var da = (DependsOnAttribute)attribute;
                if (!_dependencies.ContainsKey(da.Name))
                    _dependencies[da.Name] = new List<string>();

                _dependencies[da.Name].Add(prop.Name);
            }
        }
        var myMethods = GetType()
            .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
        foreach (var method in myMethods)
        {
            var dependsOnAttributes = method.GetCustomAttributes(typeof(DependsOnAttribute), true);
            foreach (DependsOnAttribute attribute in dependsOnAttributes)
            {
                if (!_dependencies.ContainsKey(attribute.Name))
                    _dependencies[attribute.Name] = [];

                _dependencies[attribute.Name].Add(method.Name);
            }
        }
        return properties;
    }

    public static IEnumerable<FieldInfo> GetAllFields(Type? type)
    {
        if (type == null)
            yield break;

        const BindingFlags flags =
            BindingFlags.Instance |
            BindingFlags.Static |
            BindingFlags.Public |
            BindingFlags.NonPublic |
            BindingFlags.DeclaredOnly;

        while (type != null)
        {
            foreach (var field in type.GetFields(flags))
                yield return field;

            type = type.BaseType;
        }
    }
    
    private PropertyDescriptorCollection? propertiesAttribute;

    public PropertyDescriptorCollection GetProperties(Attribute[]? attributes)
    {
        if (propertiesAttribute != null)
            return propertiesAttribute;

        propertiesAttribute = new PropertyDescriptorCollection([]);
        foreach (PropertyDescriptor prop in TypeDescriptor.GetProperties(this, true))
            propertiesAttribute.Add(prop);
        return propertiesAttribute;
    }

    public object GetPropertyOwner(PropertyDescriptor? pd)
    {
        return this;
    }

    #endregion

    #region INotifyPropertyChanged

    public event PropertyChangedEventHandler? PropertyChanged;

    private void ExecuteHandler(PropertyChangedEventHandler handler, string name)
    {
        var args = new PropertyChangedEventArgs(name);
        //var dispatcherObject = handler.Target as DispatcherObject;
        //// If the subscriber is a DispatcherObject and different thread
        //if (dispatcherObject != null && dispatcherObject.CheckAccess() == false)
        //{
        //    // Invoke handler in the target dispatcher's thread
        //    dispatcherObject.Dispatcher.BeginInvoke(handler, DispatcherPriority.DataBind, this, args);
        //}
        //else // Execute handler as is
        //{
        //    handler(this, args);
        //}
        handler(this, args);
    }

    protected internal void NotifyPropertyChanged(string name)
    {
#if DEBUG
        //TODO: AppService.PropertyNameId
        Debug.Assert(name.StartsWith("_stonehenge_", StringComparison.Ordinal)
                     || GetPropertyInfo(name) != null
                     || _dictionary.ContainsKey(name)
            , "NotifyPropertyChanged for unknown property " + name);
#endif
        var handler = PropertyChanged;
        if (handler != null)
        {
            ExecuteHandler(handler, name);
        }

        if (!_dependencies.TryGetValue(name, out var dependency))
            return;

        foreach (var dependentName in dependency)
        {
            if (handler != null)
            {
                ExecuteHandler(handler, dependentName);
            }
        }
    }

    protected void NotifyPropertiesChanged(string[] names)
    {
        foreach (var name in names)
        {
            NotifyPropertyChanged(name);
        }
    }

    public void NotifyAllPropertiesChanged()
    {
        if (properties != null)
        {
            foreach (PropertyDescriptorEx prop in properties)
            {
                NotifyPropertyChanged(prop.Name);
            }
        }

        Session.UpdatePropertiesImmediately();
    }

    #endregion

    #region MessageBox

    public string MessageBoxTitle = string.Empty;
    public string MessageBoxText = string.Empty;

    public void MessageBox(string title, string text)
    {
        MessageBoxTitle = title;
        MessageBoxText = text;
        NotifyPropertyChanged("_stonehenge_StonehengeEval");
    }

    #endregion

    #region Server side page enabling

    public bool UpdateRoutes;

    public void EnableRoute(string route, bool enabled)
    {
        route = route.Replace("-", "_");
        Session.Logger.LogInformation("ActiveViewModel.EnableRoute({Route}) = {Enabled}", route, enabled);
        AppPages.EnableRoute(route, enabled);
        UpdateRoutes = true;
    }

    public bool IsRouteEnabled(string route) =>
        AppPages.IsRouteEnabled(route.Replace("-", "_"));

    #endregion

    #region Server side navigation

    public string NavigateToRoute = string.Empty;

    public void NavigateTo(string route)
    {
        if (string.Equals(Session.CurrentRoute, route, StringComparison.OrdinalIgnoreCase)) return;
        Session.Logger.LogInformation("ActiveViewModel.NavigateTo: {Route}", route);
        NavigateToRoute = route.StartsWith("http", StringComparison.OrdinalIgnoreCase)
            ? route
            : route.Replace('-', '_');
    }

    public void NavigateBack()
    {
        var route = Session.GetBackRoute();
        if (string.IsNullOrEmpty(route))
        {
            Session.Logger.LogWarning("ActiveViewModel.NavigateBack: No back route");
            return;
        }

        Session.Logger.LogInformation("ActiveViewModel.NavigateBack: {Route}", route);
        NavigateToRoute = route.Replace('-', '_');
    }

    public void ReloadPage() => ExecuteClientScript("window.location.reload();");

    #endregion

    #region Client site scripting

    public string ClientScript = string.Empty;

    public void ExecuteClientScript(string script)
    {
        script = script.Trim();
        if (!script.EndsWith(';'))
        {
            script += "; ";
        }

        if (string.IsNullOrEmpty(ClientScript))
        {
            ClientScript = script;
            return;
        }

        ClientScript += script;
    }

    #endregion

    #region Clipboard support

    public void CopyToClipboard(string text)
    {
        text = text
            .Replace("\\", @"\\", StringComparison.Ordinal)
            .Replace("'", "\\'", StringComparison.Ordinal)
            .Replace("\r", "\\r", StringComparison.Ordinal)
            .Replace("\n", "\\n", StringComparison.Ordinal);
        ExecuteClientScript($"stonehengeCopyToClipboard('{text}')");
    }

    #endregion

    public virtual Resource? GetDataResource(string resourceName)
    {
        return null;
    }

    public virtual Resource? GetDataResource(string resourceName, IDictionary<string, string> parameters)
    {
        return null;
    }

    public virtual Resource? PostDataResource(string resourceName, IDictionary<string, string> parameters,
        IDictionary<string, string> formData)
    {
        return null;
    }

    protected void SetUpdateTimer(int updateMs) => SetUpdateTimer(TimeSpan.FromMilliseconds(updateMs));

    protected void SetUpdateTimer(TimeSpan update)
    {
        if (_updateTimer == null)
        {
            _updateTimer = new Timer(update.TotalMilliseconds);
            _updateTimer.Elapsed += UpdateTimerOnElapsed;
            _updateTimer.Enabled = true;
        }
        else
        {
            _updateTimer.Interval = update.TotalMilliseconds;
        }
    }

    protected void StopUpdateTimer()
    {
        if (_updateTimer == null) return;

        _updateTimer.Stop();
        _updateTimer.Dispose();
        _updateTimer = null;
    }

    private void UpdateTimerOnElapsed(object? sender, ElapsedEventArgs e)
    {
        Thread.CurrentThread.CurrentCulture = Session.SessionCulture;
        Thread.CurrentThread.CurrentUICulture = Session.SessionCulture;
        OnUpdateTimer();
    }

    public virtual void OnUpdateTimer()
    {
    }

    public virtual void Dispose()
    {
        foreach (PropertyDescriptorEx sp in sessionProperties)
        {
            var name = sp.Name;
            var sv = sp.GetValue(this); 
            try
            {
                Session.Set(name, sv);
                Session.Logger.LogDebug("Save session property {Name} = {Value}", name, sv);
            }
            catch (Exception ex)
            {
                Session.Logger.LogError(ex, "Failed to save session property {Name} = {Value}", name, sv);
            }
        }
        foreach (var sf in sessionFields)
        {
            var name = sf.Name;
            var sv = sf.GetValue(this); 
            try
            {
                Session.Set(name, sv);
                Session.Logger.LogDebug("Save session field {Name} = {Value}", name, sv);
            }
            catch (Exception ex)
            {
                Session.Logger.LogError(ex, "Failed to save session field {Name} = {Value}", name, sv);
            }
        }
        
        StopUpdateTimer();
        OnDispose();
    }

    public virtual void OnDispose()
    {
    }
    
    public virtual void OnWindowResized(int width, int height)
    {
    }

}