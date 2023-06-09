using IctBaden.Stonehenge.Core;
using IctBaden.Stonehenge.ViewModel;

namespace IctBaden.Stonehenge.Vue.SampleCore.ViewModels;

public class MermaidViewVm : ActiveViewModel
{
    public string MermaidGraph { get; private set; }

    public MermaidViewVm(AppSession session)
        : base(session)
    {
        MermaidGraph = @"graph LR;
    A--> B & C & D;
    B--> A & E;
    C--> A & E;
    D--> A & E;
    E--> B & C & D;";
    }
}