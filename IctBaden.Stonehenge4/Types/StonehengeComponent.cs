using System;
using IctBaden.Stonehenge.ViewModel;

namespace IctBaden.Stonehenge.Types;

public class StonehengeComponent : ActiveViewModel
{
    public string ComponentId { get; private set; } = Guid.NewGuid().ToString("N");
}