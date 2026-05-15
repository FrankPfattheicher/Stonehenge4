using System.Linq;
using IctBaden.Stonehenge.Core;
using IctBaden.Stonehenge.ViewModel;
using IctBaden.Stonehenge4.SyntaxHighlight.ViewModels;
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace IctBaden.Stonehenge.Vue.SampleCore.ViewModels;

public class SyntaxVm : ActiveViewModel
{
    public CSharpScriptEditor ScEdit { get; set; } = new();
    public string Result { get; set; } = string.Empty;

    public string[] Errors => ScEdit.Diagnostics
        .Select(diagnostic => diagnostic.GetMessage())
        .ToArray();


    public SyntaxVm(AppSession session)
        : base(session)
    {
        ScEdit.Source = """

                        public class SyntaxHighlight : IStonehengeExtension
                        {
                            public string Version => "11.11.1";
                        }

                        """;
    }

    [ActionMethod]
    public void Compile()
    {
        Result = string.Empty;
        ScEdit.Compile();
        NotifyAllPropertiesChanged();
    }
    
    [ActionMethod]
    public void Evaluate()
    {
        Result = $"{ScEdit.Evaluate()}";
        NotifyAllPropertiesChanged();
    }
    
}