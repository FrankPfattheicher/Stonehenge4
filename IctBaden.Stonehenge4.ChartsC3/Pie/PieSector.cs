// ReSharper disable PropertyCanBeMadeInitOnly.Global
namespace IctBaden.Stonehenge.Extension.Pie;

public class PieSector
{
    public string Label { get; set; } = "";
    public int Value { get; set; }

    internal object[] Date => new object[] { Label, Value };
}