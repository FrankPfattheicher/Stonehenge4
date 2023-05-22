namespace IctBaden.Stonehenge.Extension.Pie;

public class PieChart
{
    public string Id { get; } = Guid.NewGuid().ToString("N");

    public Dictionary<string, object> Data
    {
        get =>
            new Dictionary<string, object>
            {
                ["columns"] = Sectors
                    .Select(s => new object[] { s.Label, s.Value })
                    .ToArray(),
                ["type"] = "pie"
            };
    }

    public PieSector[] Sectors = Array.Empty<PieSector>();
}