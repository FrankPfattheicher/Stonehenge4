using System.Collections.Generic;
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace IctBaden.Stonehenge.Vue.SampleCore.ViewModels;

public class Continent
{
    public string Name { get; init; } = string.Empty;
    public string Icon { get; init; } = string.Empty;
    public int Countries { get; init; }
    /// <summary>
    /// 1000 square km
    /// </summary>
    public int Area { get; init; }
    public bool IsChild { get; init; }

    public IList<Continent> Children = new List<Continent>();
}