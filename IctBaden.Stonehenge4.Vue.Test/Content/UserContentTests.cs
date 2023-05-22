using System;
using IctBaden.Stonehenge.Hosting;
using Microsoft.Extensions.Logging;
using Xunit;

namespace IctBaden.Stonehenge.Vue.Test.Content
{
    public class UserContentTests : IDisposable
    {
        private readonly ILogger _logger = StonehengeLogger.DefaultLogger;
        private readonly VueTestApp _app;

        public UserContentTests()
        {
            _app = new VueTestApp();
        }

        public void Dispose()
        {
            _app.Dispose();
        }

        [Fact]
        public void IndexShouldContainUserStylesRef()
        {
            var response = string.Empty;
            try
            {
                // ReSharper disable once ConvertToUsingDeclaration
                using (var client = new RedirectableHttpClient())
                {
                    response = client.DownloadStringWithSession(_app.BaseUrl).Result;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, nameof(IndexShouldContainUserStylesRef));
            }

            Assert.NotNull(response);
            Assert.Contains("'styles/userstyles.css'", response);
        }

        [Fact]
        public void IndexShouldContainUserScriptsRef()
        {
            var response = string.Empty;
            try
            {
                // ReSharper disable once ConvertToUsingDeclaration
                using (var client = new RedirectableHttpClient())
                {
                    response = client.DownloadString(_app.BaseUrl).Result;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, nameof(IndexShouldContainUserScriptsRef));
            }

            Assert.NotNull(response);
            Assert.Contains("'scripts/userscripts.js'", response);
        }

        [Fact]
        public void StartJsShouldContainStartUserScript()
        {
            var response = string.Empty;
            try
            {
                // ReSharper disable once ConvertToUsingDeclaration
                using (var client = new RedirectableHttpClient())
                {
                    response = client.DownloadStringWithSession(_app.BaseUrl + "/start.js").Result;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, nameof(StartJsShouldContainStartUserScript));
            }

            Assert.NotNull(response);
            Assert.Contains("'start_user_InitialLoaded'", response);
        }

    }
}
