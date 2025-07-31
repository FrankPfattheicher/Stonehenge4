// ReSharper disable UnusedAutoPropertyAccessor.Global

using IctBaden.Stonehenge.Types;

namespace IctBaden.Stonehenge.Extension;

public class Gauge
{
    /// <summary>
    /// Id of gauge chart element
    /// </summary>
    public string Id { get; private set; } = Element.NewId();
    
    public string? Label { get; set; }

    public bool MinMaxLabels { get; set; } = true;
    
    public int Min { get; set; }
    public int Max { get; set; }
    public int Value { get; set; }
    
    public string[] ColorPatterns { get; set; } = { "blue" };
    public int[] ColorThresholds { get; set; } = { 0 };
    
    /// <summary>
    /// For adjusting arc thickness
    /// </summary>
    public int Thickness { get; set; } = 32;
    public string? Units { get; set; }

    public void UpdateId() => Id = Element.NewId();
    public void Regenerate() => Id = Element.NewId();

}