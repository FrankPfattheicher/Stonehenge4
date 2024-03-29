using System;
using System.Net.Http;

namespace IctBaden.Stonehenge.Test.Hosting
{
    public sealed class RedirectableWebClient : IDisposable
    {
#pragma warning disable IDISP004
        private readonly HttpClient _client = new(new HttpClientHandler { AllowAutoRedirect = true });
#pragma warning restore IDISP004

        public void Dispose()
        {
            _client.Dispose();
        }
        
        
        public string DownloadString(string address)
        {
            for (var redirect = 0; redirect < 10; redirect++)
            {
                using var response = _client.GetAsync(address).Result;

                var redirectUrl = response.Headers.Location;
                if (redirectUrl == null)
                {
                    // address = response.Headers.ResponseUri.ToString();
                }

                if (redirectUrl == null)
                    break;

                var newAddress = new Uri(new Uri(address), redirectUrl).AbsoluteUri;
                if (newAddress == address)
                    break;

                address = newAddress;
            }

            return _client.GetStringAsync(address).Result;
        }

    }
}
