namespace IctBaden.Stonehenge.Extension.Sankey;

public class Sankey
{
    public string Id { get; } = Guid.NewGuid().ToString("N");
    
    public SankeyNode[] Nodes { get; set; }
    public SankeyLink[] Links { get; set; }
}