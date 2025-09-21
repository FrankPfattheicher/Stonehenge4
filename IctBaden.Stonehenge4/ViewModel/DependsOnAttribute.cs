using System;

namespace IctBaden.Stonehenge.ViewModel;

/// <summary>
/// Replacement for System.Windows.Markup.DependsOnAttribute
/// for non Windows systems
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
public class DependsOnAttribute : Attribute
{
    // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Local
    public string Name { get; private set; }

    public DependsOnAttribute(string name)
    {
        Name = name;
    }
}