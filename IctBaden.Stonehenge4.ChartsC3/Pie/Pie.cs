using System.Text.Json.Serialization;

namespace IctBaden.Stonehenge.Extension.Pie;

public class Pie
{
    public string Id { get; } = Guid.NewGuid().ToString("N");

    public Dictionary<string, object> Data
    {
        get =>
            new Dictionary<string, object>
            {
                ["columns"] = Sectors
                    .Select(s => s.Date)
                    .ToArray(),
                ["type"] = "pie"
            };
    }

    public PieSector[] Sectors = Array.Empty<PieSector>();
}