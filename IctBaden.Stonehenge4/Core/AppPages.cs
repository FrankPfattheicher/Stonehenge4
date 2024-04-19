using System.Linq;
using IctBaden.Stonehenge.Resources;

namespace IctBaden.Stonehenge.Core;

public static class AppPages
{
    internal static ViewModelInfo[] Pages = [];

    public static void SetPages(ViewModelInfo[] pages)
    {
        Pages = pages;
    }
    
    public static void EnableRoute(string route, bool enabled)
    {
        var vmInfo = Pages.FirstOrDefault(p => p.Route == route);
        if (vmInfo != null)
        {
            vmInfo.Visible = enabled;
        }
    }
    
    public static bool IsRouteEnabled(string route)
    {
        var vmInfo = Pages.FirstOrDefault(p => p.Route == route);
        return vmInfo is { Visible: true };
    }
    
}