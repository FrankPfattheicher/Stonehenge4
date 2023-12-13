using System.Drawing;

namespace IctBaden.Stonehenge.Extension.Pie;

public class PieChart
{
    public string Id { get; } = Guid.NewGuid().ToString("N");

    public Dictionary<string, object> Data
    {
        get =>
            new Dictionary<string, object>
            {
                ["type"] = "pie",
                ["columns"] = Sectors
                    .Select(s => new object[] { s.Label, s.Value })
                    .ToArray(),
                ["colors"] = SectorColors
            };
    }

    private Dictionary<string, string> SectorColors
    {
        get
        {
            var sc = new Dictionary<string, string>();
            
            foreach (var sector in Sectors)
            {
                if (sector.Color != null) 
                    sc.Add(sector.Label, ColorRgb((Color)sector.Color));
            }
            return sc;
        }
    }
    
    private string ColorRgb(Color color) => $"#{color.R:X2}{color.G:X2}{color.B:X2}";

    public PieSector[] Sectors = Array.Empty<PieSector>();
}