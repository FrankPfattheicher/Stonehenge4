using System;
using System.Linq;
using System.Reflection;
using IctBaden.Stonehenge.ViewModel;

namespace IctBaden.Stonehenge.Types;

public class StonehengeComponent : ActiveViewModel
{
    public string ComponentId { get; private set; } = Guid.NewGuid().ToString("N");

    internal string[] GetI18Names()
    {
        var componentResources = I18Types.FirstOrDefault(ty => string.Equals(ty.Name, $"{GetType().Name}I18n", StringComparison.OrdinalIgnoreCase));
        if(componentResources == null) return [];
        
        var texts = componentResources
            .GetProperties(BindingFlags.Static | BindingFlags.NonPublic)
            .Where(property => property.PropertyType == typeof(string))
            .ToArray();

        return texts.Select(propertyInfo => propertyInfo.Name).ToArray();
    }
}