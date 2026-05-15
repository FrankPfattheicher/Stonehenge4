using System.Diagnostics.CodeAnalysis;

namespace IctBaden.Stonehenge;

[SuppressMessage("Design", "MA0069:Non-constant static fields should not be visible")]
public static class StonehengeGlobal
{
    // ReSharper disable once FieldCanBeMadeReadOnly.Global
    /// Global setting to enable/disable ConfigureAwait. 
    public static bool ConfigureAwait = false;
}