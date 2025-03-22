using System;

namespace IctBaden.Stonehenge.Types;

public class StonehengeComponent
{
    public string ComponentId { get; private set; } = Guid.NewGuid().ToString("N");
}