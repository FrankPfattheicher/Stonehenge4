using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using IctBaden.Stonehenge.Core;
using IctBaden.Stonehenge.Hosting;
using IctBaden.Stonehenge.Resources;
using Xunit;

namespace IctBaden.Stonehenge.Test.Resources
{
    public class UserContentLinksTests
    {
        private readonly AppSession _session = new();
        private readonly Resource? _index;

        public UserContentLinksTests()
        {
            var assemblies = new List<Assembly?>
                {
                    Assembly.GetAssembly(typeof(ResourceLoader)),
                    Assembly.GetExecutingAssembly(),
                    Assembly.GetCallingAssembly()
                }
                .Where(a => a != null)
                .Distinct()
                .Cast<Assembly>()
                .ToList();
#pragma warning disable IDISP001
            var loader = new ResourceLoader(StonehengeLogger.DefaultLogger, assemblies, typeof(UserContentLinksTests).Assembly);
#pragma warning restore IDISP001
            _index = loader.Get(_session, CancellationToken.None,"index.html", new Dictionary<string, string>()).Result;
        }

        [Fact]
        public void IndexShouldContainLinkToUserJs()
        {
            Assert.Contains("scripts/test.js", _index?.Text);
        }
        
        [Fact]
        public void IndexShouldContainLinkToUserStyle()
        {
            Assert.Contains("styles/test.css", _index?.Text);
        }

        [Fact]
        public void IndexShouldContainLinkToUserTheme()
        {
            Assert.Contains("themes/test-theme.css", _index?.Text);
        }

    }
}