using System.Diagnostics.CodeAnalysis;
using IctBaden.Stonehenge.Types;
using IctBaden.Stonehenge.ViewModel;

namespace IctBaden.Stonehenge.Forms.ViewModels;

[SuppressMessage("Design", "MA0046:Use EventHandler<T> to declare events")]
public class Splitter : StonehengeComponent
{
    public enum Direction
    {
        Horizontal,
        Vertical
    }
    public Direction SplitDirection { get; set; } = Direction.Horizontal;

    public event Action<int, int>? SplitterMoved;
    
    [ActionMethod]
    public void Moved(int first, int second)
    {
        SplitterMoved?.Invoke(first, second);
    }
    
}
