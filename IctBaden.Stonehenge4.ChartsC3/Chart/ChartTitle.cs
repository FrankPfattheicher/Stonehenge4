using System.Text.Json.Serialization;

namespace IctBaden.Stonehenge4.ChartsC3;

public class ChartTitle
{
    [JsonPropertyName("text")]
    public string Text { get; set; }
    
    public ChartTitle(string text)
    {
        Text = text;
    }


}