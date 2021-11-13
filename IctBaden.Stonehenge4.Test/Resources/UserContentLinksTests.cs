using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using IctBaden.Stonehenge4.Core;
using IctBaden.Stonehenge4.Hosting;
using IctBaden.Stonehenge4.Resources;
using Xunit;

namespace IctBaden.Stonehenge4.Test.Resources
{
    public class UserContentLinksTests
    {
        private readonly AppSession _session = new AppSession();
        private readonly Resource _index;

        public UserContentLinksTests()
        {
            var assemblies = new List<Assembly>
                {
                    Assembly.GetAssembly(typeof(ResourceLoader)),
                    Assembly.GetExecutingAssembly(),
                    Assembly.GetCallingAssembly()
                }
                .Distinct()
                .ToList();
            var loader = new ResourceLoader(StonehengeLogger.DefaultLogger, assemblies, typeof(UserContentLinksTests).Assembly);
            _index = loader.Get(_session, "index.html", new Dictionary<string, string>());
        }

        [Fact]
        public void IndexShouldContainLinkToUserJs()
        {
            Assert.Contains("scripts/test.js", _index.Text);
        }
        
        [Fact]
        public void IndexShouldContainLinkToUserStyle()
        {
            Assert.Contains("styles/test.css", _index.Text);
        }

        [Fact]
        public void IndexShouldContainLinkToUserTheme()
        {
            Assert.Contains("themes/test-theme.css", _index.Text);
        }

    }
}