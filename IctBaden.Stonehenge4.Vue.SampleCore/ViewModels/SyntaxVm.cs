using IctBaden.Stonehenge.Core;
using IctBaden.Stonehenge.ViewModel;

namespace IctBaden.Stonehenge.Vue.SampleCore.ViewModels;

public class SyntaxVm : ActiveViewModel
{
    public string Source { get; private set; }
    public string Language { get; private set; }

    public SyntaxVm(AppSession session) 
        : base(session)
    {
        Language = "csharp";
        Source = """
                 
                 public class SyntaxHighlight : IStonehengeExtension
                 {
                     public string Version => "11.11.1";
                 }
                 
                 """;
    }

    [ActionMethod]
    public void Update()
    {
        NotifyAllPropertiesChanged();
    }

}