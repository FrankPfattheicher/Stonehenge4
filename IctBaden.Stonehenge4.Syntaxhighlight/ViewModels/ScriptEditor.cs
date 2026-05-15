using IctBaden.Stonehenge.Types;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.Text;

// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace IctBaden.Stonehenge4.SyntaxHighlight.ViewModels;

public class ScriptEditor : StonehengeComponent
{
    public string Source { get; set; } = string.Empty;

    public ScriptDiagnostic[] DiagnosticMarkers { get; private set; } = [];
    public Diagnostic[] Diagnostics = [];

    private Script<object>? _script;

    public override void OnLoad()
    {
        base.OnLoad();
        Task.Run(Compile);
    }

    public object Evaluate()
    {
        if (!Compile() || _script == null) return string.Empty;
        return _script
            .RunAsync()
            .Result
            .ReturnValue;
    }

    public bool Compile()
    {
        var options = ScriptOptions.Default
            .AddReferences(
                typeof(object).Assembly,
                typeof(Enumerable).Assembly   // System.Linq
            )
            .WithImports("System", "System.Collections.Generic", "System.Linq");
        _script = CSharpScript.Create(Source, options);
        Diagnostics = _script.Compile().ToArray();
        DiagnosticMarkers = CreateDiagnosticMarkers(Source, Diagnostics);
        return Diagnostics.Length == 0;
    }

    private static ScriptDiagnostic[] CreateDiagnosticMarkers(string source, IEnumerable<Diagnostic> diagnostics)
    {
        var text = SourceText.From(source);
        return diagnostics
            .Where(diagnostic => diagnostic.Severity >= DiagnosticSeverity.Warning)
            .Select(diagnostic => ToMarker(diagnostic, text))
            .Where(marker => marker.Length > 0)
            .OrderBy(marker => marker.Start)
            .ToArray();
    }

    private static ScriptDiagnostic ToMarker(Diagnostic diagnostic, SourceText text)
    {
        var span = diagnostic.Location.SourceSpan;
        var start = Math.Clamp(span.Start, 0, text.Length);
        var end = Math.Clamp(span.End, start, text.Length);
        if (end <= start)
        {
            end = Math.Min(start + 1, text.Length);
        }

        return new ScriptDiagnostic
        {
            Start = start,
            Length = end - start,
            Message = diagnostic.GetMessage(),
            Severity = diagnostic.Severity.ToString()
        };
    }
}