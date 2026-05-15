using System.Text.Json.Serialization;
using IctBaden.Stonehenge.Types;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;

namespace IctBaden.Stonehenge4.SyntaxHighlight.ViewModels;

public class ScriptEditor : StonehengeComponent
{
    public string Source { get; set; } = string.Empty;

    [JsonIgnore]
    public Diagnostic[] Diagnostics { get; private set; } = [];

    public override void OnLoad()
    {
        base.OnLoad();
        Task.Run(Compile);
    }

    public void Compile()
    {
        var script = CSharpScript.Create(Source);
        Diagnostics = script.Compile().ToArray();
    }
}
