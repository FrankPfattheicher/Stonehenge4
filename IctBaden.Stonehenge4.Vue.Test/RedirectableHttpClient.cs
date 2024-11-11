using System;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

// ReSharper disable ConvertToUsingDeclaration

namespace IctBaden.Stonehenge.Vue.Test;

public partial class RedirectableHttpClient : HttpClient
{
    // ReSharper disable once MemberCanBePrivate.Global
    public string? SessionId { get; set; }

    public async Task<string> DownloadStringWithSession(string address)
    {
        if (SessionId == null)
        {
            await DownloadString(address).ConfigureAwait(false);
        }

        var url = new UriBuilder(address);
        var query = HttpUtility.ParseQueryString(url.Query);
        query["stonehenge-id"] = SessionId;
        url.Query = query.ToString() ?? string.Empty;
        return await DownloadString(url.ToString()).ConfigureAwait(false);
    }

    public async Task<string> DownloadString(string address)
    {
        for (var redirect = 0; redirect < 10; redirect++)
        {
            using var response = await GetAsync(address).ConfigureAwait(false);

            var redirectUrl = response.Headers.Location;
            string? redirectAddr = null;
            if (redirectUrl == null)
            {
                redirectAddr = response.RequestMessage?.RequestUri?.ToString();
            }
            if (redirectAddr != null)
            {
                var match = StonehengeIdRegex()
                    .Match(redirectAddr);
                if (match.Success)
                {
                    SessionId = match.Groups[1].Value;
                }
            }

            var body = response.Content.ReadAsStringAsync().Result;
            if (redirectUrl == null)
            {
                return body;
            }

            var newAddress = new Uri(response.RequestMessage!.RequestUri!, redirectUrl).AbsoluteUri;
            if (string.Equals(newAddress, address, StringComparison.OrdinalIgnoreCase))
                break;

            address = newAddress;
        }

        return string.Empty;
    }

    public async Task<string> Post(string address, string data)
    {
        DefaultRequestHeaders.Add("X-Stonehenge-Id", SessionId);
        using var content = new StringContent(data);
        using var response = await PostAsync(address, content).ConfigureAwait(false);
        var body = response.Content.ReadAsStringAsync().Result;
        return body;
    }

    public async Task<string> Put(string address, string data)
    {
        using var content = new StringContent(data);
        using var response = await PutAsync(address, content).ConfigureAwait(false);
        var body = response.Content.ReadAsStringAsync().Result;
        return body;
    }
    
    public async Task<string> Delete(string address)
    {
        using var response = await DeleteAsync(address).ConfigureAwait(false);
        var body = response.Content.ReadAsStringAsync().Result;
        return body;
    }

#pragma warning disable MA0009
    [GeneratedRegex("stonehenge-id=([a-f0-9A-F]+)", RegexOptions.RightToLeft | RegexOptions.ExplicitCapture)]
#pragma warning restore MA0009
    private static partial Regex StonehengeIdRegex();
}