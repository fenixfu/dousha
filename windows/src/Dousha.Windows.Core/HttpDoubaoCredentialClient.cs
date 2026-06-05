using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;

namespace Dousha.Windows.Core;

public sealed class HttpDoubaoCredentialClient : IDoubaoCredentialClient, IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly bool _ownsHttpClient;

    public HttpDoubaoCredentialClient()
        : this(new HttpClient(), ownsHttpClient: true)
    {
    }

    public HttpDoubaoCredentialClient(HttpClient httpClient, bool ownsHttpClient = false)
    {
        _httpClient = httpClient;
        _ownsHttpClient = ownsHttpClient;
    }

    public async Task<RegisteredDoubaoDevice> RegisterDeviceAsync(CancellationToken cancellationToken = default)
    {
        var cdid = Guid.NewGuid().ToString().ToLowerInvariant();
        var openudid = RandomHex(bytes: 8);
        var clientudid = Guid.NewGuid().ToString().ToLowerInvariant();
        var request = DoubaoProtocol.BuildRegistrationRequest(cdid, openudid, clientudid, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        var response = await SendAsync(request, cancellationToken);
        return DoubaoProtocol.ParseRegistrationResponse(response, cdid, openudid, clientudid);
    }

    public async Task<string> FetchTokenAsync(RegisteredDoubaoDevice device, CancellationToken cancellationToken = default)
    {
        var request = DoubaoProtocol.BuildTokenRequest(device.DeviceId, device.Cdid, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        var response = await SendAsync(request, cancellationToken);
        return DoubaoProtocol.ParseTokenResponse(response);
    }

    public void Dispose()
    {
        if (_ownsHttpClient)
        {
            _httpClient.Dispose();
        }
    }

    private async Task<string> SendAsync(DoubaoHttpRequest request, CancellationToken cancellationToken)
    {
        using var message = new HttpRequestMessage(new HttpMethod(request.Method), request.Url)
        {
            Content = new StringContent(request.Body, Encoding.UTF8)
        };
        foreach (var header in request.Headers)
        {
            if (string.Equals(header.Key, "Content-Type", StringComparison.OrdinalIgnoreCase))
            {
                message.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(header.Value);
                continue;
            }

            if (!message.Headers.TryAddWithoutValidation(header.Key, header.Value))
            {
                message.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        using var response = await _httpClient.SendAsync(message, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    private static string RandomHex(int bytes)
    {
        return Convert.ToHexString(RandomNumberGenerator.GetBytes(bytes)).ToLowerInvariant();
    }
}
