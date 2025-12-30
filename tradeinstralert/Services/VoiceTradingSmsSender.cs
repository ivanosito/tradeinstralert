using Microsoft.Extensions.Logging;
using System.Net.Http;

namespace tradeinstralert.Services;

public sealed class VoiceTradingSmsSender
{
    private readonly HttpClient _http;
    private readonly ILogger<VoiceTradingSmsSender> _logger;

    public VoiceTradingSmsSender(HttpClient http, ILogger<VoiceTradingSmsSender> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<bool> SendAsync(string baseUrl, string username, string password, string from, string to, string text, CancellationToken ct)
    {
        // User-provided endpoint:
        // https://www.voicetrading.com/myaccount/sendsms.php?username=...&password=...&from=...&to=...&text=...
        var url =
            $"{baseUrl}?username={Uri.EscapeDataString(username)}" +
            $"&password={Uri.EscapeDataString(password)}" +
            $"&from={Uri.EscapeDataString(from)}" +
            $"&to={Uri.EscapeDataString(to)}" +
            $"&text={Uri.EscapeDataString(text)}";

        try
        {
            using var resp = await _http.GetAsync(url, ct);
            var body = await resp.Content.ReadAsStringAsync(ct);

            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogWarning("SMS send failed HTTP {Status}: {Body}", resp.StatusCode, body);
                return false;
            }

            _logger.LogInformation("SMS sent ok. Response: {Body}", body);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SMS send threw exception.");
            return false;
        }
    }
}
