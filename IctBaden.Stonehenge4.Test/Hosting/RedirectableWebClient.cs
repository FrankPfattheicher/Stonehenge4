using System;
using System.Net.Http;

namespace IctBaden.Stonehenge4.Test.Hosting
{
    public class RedirectableWebClient : IDisposable
    {
        private HttpClient client;
        public RedirectableWebClient()
        {
            client = new HttpClient(new HttpClientHandler { AllowAutoRedirect = true });
        }
        
        public void Dispose()
        {
            client?.Dispose();
        }
        
        
        public string DownloadString(string address)
        {
            for (var redirect = 0; redirect < 10; redirect++)
            {
                var response = client.GetAsync(address).Result;

                var redirectUrl = response.Headers.Location;
                if (redirectUrl == null)
                {
                    //address = response.Headers .ResponseUri.ToString();
                }

                response.Dispose();

                if (redirectUrl == null)
                    break;

                var newAddress = new Uri(new Uri(address), redirectUrl).AbsoluteUri;
                if (newAddress == address)
                    break;

                address = newAddress;
            }

            return client.GetStringAsync(address).Result;
        }

    }
}
