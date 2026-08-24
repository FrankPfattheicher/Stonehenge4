// ReSharper disable MemberCanBePrivate.Global
namespace IctBaden.Stonehenge.Core;

/// <summary>
/// Event used to send server changes to client
/// </summary>
public record ClientEvent
{
    public readonly string Name;
    public readonly ClientEventSource Source;

    public string GetEventArg() => Source + ":" + Name;
        
    public ClientEvent(string name, ClientEventSource source)
    {
        Name = name;
        Source = source;
    }
}