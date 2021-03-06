using System.Collections.Generic;
using System.IO;
using IctBaden.Stonehenge.Core;
using IctBaden.Stonehenge.Hosting;
using IctBaden.Stonehenge.Resources;

namespace IctBaden.Stonehenge.Test.Tools
{
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

        public Resource Post(AppSession session, string resourceName, Dictionary<string, string> parameters, Dictionary<string, string> formData)
        {
            return null;
        }

        public Resource Get(AppSession session, string resourceName, Dictionary<string, string> parameters)
        {
            var resourceExtension = Path.GetExtension(resourceName);
            return new Resource(resourceName, "test://TestResourceLoader.content", ResourceType.GetByExtension(resourceExtension), _content, Resource.Cache.None);
        }
    }
}
