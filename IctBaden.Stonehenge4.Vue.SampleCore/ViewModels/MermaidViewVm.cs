using IctBaden.Stonehenge.Core;
using IctBaden.Stonehenge.ViewModel;
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace IctBaden.Stonehenge.Vue.SampleCore.ViewModels;

public class MermaidViewVm : ActiveViewModel
{
    public string MermaidGraph { get; private set; } = string.Empty;
    public string MermaidGraph2 { get; private set; } = string.Empty;
    public string MermaidGraph3 { get; private set; } = string.Empty;

    public bool ShowMermaidDlg { get; private set; }
    
    public MermaidViewVm(AppSession session)
        : base(session)
    {
        SetGraph(1);
    }

    [ActionMethod]
    public void SetGraph(int graph)
    {
        switch (graph)
        {
            case 1:
                MermaidGraph = "graph TD;  Start --> Stop;";
                MermaidGraph2 = "graph TD;  Begin --> End;";
                MermaidGraph3 = "graph TD;  Begin --> End;";
                break;
            case 2:
                MermaidGraph = """
                               graph LR;
                                   A--> B & C & D;
                                   B--> A & E;
                                   C--> A & E;
                                   D--> A & E;
                                   E--> B & C & D;
                               """;
                MermaidGraph2 = """
                                sequenceDiagram
                                    Alice ->> Bob: Hello Bob, how are you?
                                    Bob-->>John: How about you John?
                                    Bob--x Alice: I am good thanks!
                                    Bob-x John: I am good thanks!
                                    Note right of John: Bob thinks a long<br/>long time, so long<br/>that the text does<br/>not fit on a row.
                                
                                    Bob-->Alice: Checking with John...
                                    Alice->John: Yes... John, how are you?
                                """;
                MermaidGraph3 = """
                               flowchart TD        
                               
                               Solar((&nbsp;☀️ Solar &nbsp;))
                               Battery[(🔋 Battery<br>32%)]
                               Home[🏠 Haus]
                               Car[(🚗 Auto)]
                               
                               net{" "}
                               
                               Grid[⚡ Netz]
                               
                               Solar animated@-->|&nbsp;5330W&nbsp;| net
                               net -->|&nbsp;330W&nbsp;| Home
                               Battery -->|&nbsp;4000W&nbsp;| net
                               net -.- Car
                               net ----- Grid
                               
                               style Solar stroke:#ffff00,stroke-width:2px
                               style Grid stroke:#000000,stroke-width:2px
                               style Battery stroke:#8080ff,stroke-width:2px
                               style Home stroke:#228b22,stroke-width:2px
                               style Car stroke:#ff0000,stroke-width:2px
                               
                               animated@{ animate: true }
                               """;
                break;
            default:
                MermaidGraph = string.Empty;
                MermaidGraph2 = string.Empty;
                MermaidGraph3 = string.Empty;
                break;
        }
    }
    
    [ActionMethod]
    public void ShowDlg()
    {
        ShowMermaidDlg = true;
    }

    [ActionMethod]
    public void CloseDlg()
    {
        ShowMermaidDlg = false;
    }

}