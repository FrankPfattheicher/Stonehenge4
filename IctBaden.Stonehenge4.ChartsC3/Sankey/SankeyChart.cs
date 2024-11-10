// ReSharper disable PropertyCanBeMadeInitOnly.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global

using IctBaden.Stonehenge.Types;

namespace IctBaden.Stonehenge.Extension.Sankey;

public class SankeyChart
{
    /// <summary>
    /// Id of sankey chart element
    /// </summary>
    public string Id { get; private set; } = Element.NewId();

    public int NodeWidth { get; set; } = 20;


    public SankeyNode[] Nodes { get; set; } = [];
    public SankeyLink[] Links { get; set; } = [];
    
    
    public void UpdateId() => Id = Element.NewId();

}