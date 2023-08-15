using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using IctBaden.Stonehenge.Core;
using IctBaden.Stonehenge.Hosting;
using IctBaden.Stonehenge.Resources;

namespace IctBaden.Stonehenge.Test.Tools;

public class TestResourceLoader : IStonehengeResourceProvider
{
    private readonly string _content;

    public TestResourceLoader(string content)
    {
        _content = content;
    }

    public void InitProvider(StonehengeResourceLoader loader, StonehengeHostOptions options)
    {
    }

    public List<ViewModelInfo> GetViewModelInfos() => new List<ViewModelInfo>();

    public void Dispose()
    {
    }

    public Task<Resource> Post(AppSession session, string resourceName, Dictionary<string, string> parameters, Dictionary<string, string> formData) => null;
    public Task<Resource> Put(AppSession session, string resourceName, Dictionary<string, string> parameters, Dictionary<string, string> formData) => null;
    public Task<Resource> Delete(AppSession session, string resourceName, Dictionary<string, string> parameters, Dictionary<string, string> formData) => null;

    public Task<Resource> Get(AppSession session, string resourceName, Dictionary<string, string> parameters)
    {
        var resourceExtension = Path.GetExtension(resourceName);
        return Task.FromResult(new Resource(resourceName, "test://TestResourceLoader.content", ResourceType.GetByExtension(resourceExtension), _content, Resource.Cache.None));
    }
}