namespace IctBaden.Stonehenge4.SyntaxHighlight.ViewModels;

public sealed class ScriptDiagnostic
{
    public int Start { get; set; }

    public int Length { get; set; }

    public string Message { get; set; } = string.Empty;

    public string Severity { get; set; } = "Error";
}
