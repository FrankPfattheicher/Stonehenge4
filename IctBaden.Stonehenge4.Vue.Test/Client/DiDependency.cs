using System.Collections.Generic;

// ReSharper disable UnusedMember.Global

namespace IctBaden.Stonehenge.Vue.Test.Client;

public class DiDependency
{
    public int VmPropInteger { get; set; }
    public string VmPropText { get; set; } = string.Empty;
    public IList<string> VmPropList { get; set; } = new List<string>();
}