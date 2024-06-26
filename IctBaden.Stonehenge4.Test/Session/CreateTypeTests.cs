using IctBaden.Stonehenge.Core;
using Xunit;

namespace IctBaden.Stonehenge.Test.Session;

public class CreateTypeTests
{
    [Fact]
    public void SetViewModelTypeShouldMatchTypeNameExactly()
    {
        using var session = new AppSession();
        var instance = session.SetViewModelType("TestVm");
        
        Assert.Equal(nameof(TestVm), instance?.GetType().Name);

        instance = session.SetViewModelType("ExtraTestVm");
        
        Assert.Equal(nameof(ExtraTestVm), instance?.GetType().Name);
    }

}