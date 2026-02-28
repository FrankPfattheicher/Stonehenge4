using IctBaden.Stonehenge.Types;
using IctBaden.Stonehenge.ViewModel;

namespace IctBaden.Stonehenge.Forms.ViewModels;

public class Splitter : StonehengeComponent
{
    public enum Direction
    {
        Horizontal,
        Vertical
    }
    public Direction SplitDirection { get; set; } = Direction.Horizontal;

    public Splitter()
    {
        
    }
    
    [ActionMethod]
    public void Resize()
    {
        
    }
    
}