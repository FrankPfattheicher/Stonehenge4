using System.Drawing;
using IctBaden.Stonehenge.Types;

// ReSharper disable UnusedMember.Global

namespace IctBaden.Stonehenge.Extension.Pie;

public class PieChart
{
    /// <summary>
    /// Id of pie chart element
    /// </summary>
    public string Id { get; private set; } = Element.NewId();

    public IDictionary<string, object> Data =>
        new(StringComparer.Ordinal)
        {
            ["type"] = "pie",
            ["columns"] = Sectors
                .Select(s => new object[] { s.Label, s.Value })
                .ToArray(),
            ["colors"] = GetSectorColors()
        };

    private Dictionary<string, string> GetSectorColors()
    {
        var sc = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var sector in Sectors)
        {
            if (sector.Color != null)
                sc.Add(sector.Label, ColorRgb((Color)sector.Color));
        }

        return sc;
    }

    private string ColorRgb(Color color) => $"#{color.R:X2}{color.G:X2}{color.B:X2}";

    public PieSector[] Sectors = [];

    
    public void UpdateId() => Id = Element.NewId();

}