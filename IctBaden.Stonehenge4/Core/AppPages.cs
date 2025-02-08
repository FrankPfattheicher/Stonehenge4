using System.Globalization;
using System.Linq;
using System.Reflection;
using IctBaden.Stonehenge.Resources;
using IctBaden.Stonehenge.ViewModel;

namespace IctBaden.Stonehenge.Core;

public static class AppPages
{
    internal static ViewModelInfo[] Pages = [];

    public static void SetPages(ViewModelInfo[] pages)
    {
        Pages = pages;
    }
    
    public static void UpdatePageTitles(AppSession appSession)
    {
        foreach (var viewModelInfo in Pages)
        {
            var id = viewModelInfo.TitleId;
            if(string.IsNullOrEmpty(id)) continue;

            var propertyInfo = ActiveViewModel.I18Types
                .SelectMany(i18Type => i18Type
                    .GetProperties(BindingFlags.Static | BindingFlags.NonPublic)
                    .Where(property => property.PropertyType == typeof(string)))
                .FirstOrDefault(property => string.Equals(property.Name, id, System.StringComparison.OrdinalIgnoreCase));
            if (propertyInfo == null) continue;
            
            var cult = propertyInfo.DeclaringType?
                .GetProperties(BindingFlags.Static | BindingFlags.NonPublic)
                .FirstOrDefault(property => property.PropertyType == typeof(CultureInfo));
            cult?.SetValue(propertyInfo.DeclaringType, appSession.SessionCulture);

            viewModelInfo.Title = propertyInfo.GetValue(null)?.ToString() ?? viewModelInfo.Title;
        }
    }
    
    public static void EnableRoute(string route, bool enabled)
    {
        var vmInfo = Pages.FirstOrDefault(p => string.Equals(p.Route, route, System.StringComparison.OrdinalIgnoreCase));
        if (vmInfo != null)
        {
            vmInfo.Visible = enabled;
        }
    }
    
    public static bool IsRouteEnabled(string route)
    {
        var vmInfo = Pages.FirstOrDefault(p => string.Equals(p.Route, route, System.StringComparison.OrdinalIgnoreCase));
        return vmInfo is { Visible: true };
    }
    
}