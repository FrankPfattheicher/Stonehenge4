using System;

namespace IctBaden.Stonehenge.ViewModel;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class SessionVariableAttribute : Attribute
{
    // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Local
    /// <summary>
    /// Custom name used for session variable.
    /// Empty - use property name. 
    /// </summary>
    public string Name { get; private set; }

    public SessionVariableAttribute(string name = "")
    {
        Name = name;
    }
}
