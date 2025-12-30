using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using tradeinstralert.Models;

namespace tradeinstralert.Services;

public sealed class TwelveDataClient
{
    private readonly HttpClient _http;
    private readonly ILogger<TwelveDataClient> _logger;

    public TwelveDataClient(HttpClient http, ILogger<TwelveDataClient> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<Candle?> GetLatestCandleAsync(string apiKey, string symbol, string interval, int outputSize, CancellationToken ct)
    {
        // Docs: https://twelvedata.com/docs#time-series
        // Example: /time_series?symbol=XAU/USD&interval=5min&outputsize=1&apikey=...
        var url = $"time_series?symbol={Uri.EscapeDataString(symbol)}&interval={Uri.EscapeDataString(interval)}&outputsize={outputSize}&apikey={Uri.EscapeDataString(apiKey)}";

        using var resp = await _http.GetAsync(url, ct);
        var body = await resp.Content.ReadAsStringAsync(ct);

        if (!resp.IsSuccessStatusCode)
        {
            _logger.LogWarning("TwelveData HTTP {Status}: {Body}", resp.StatusCode, body);
            return null;
        }

        using var doc = JsonDocument.Parse(body);

        if (doc.RootElement.TryGetProperty("status", out var statusEl) && statusEl.GetString() is string status && status != "ok")
        {
            _logger.LogWarning("TwelveData returned non-ok status: {Status}. Body: {Body}", status, body);
            return null;
        }

        if (!doc.RootElement.TryGetProperty("values", out var valuesEl) || valuesEl.ValueKind != JsonValueKind.Array || valuesEl.GetArrayLength() == 0)
            return null;

        var v0 = valuesEl[0];

        // TwelveData returns datetime in exchange local timezone; treat as UTC if it ends with Z, otherwise assume UTC for our de-dupe.
        // You can change this later if needed.
        var dtStr = v0.GetProperty("datetime").GetString() ?? "";
        if (!DateTime.TryParse(dtStr, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var dtUtc))
            dtUtc = DateTime.UtcNow;

        decimal ParseDec(string prop)
        {
            var s = v0.GetProperty(prop).GetString() ?? "0";
            return decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var d) ? d : 0m;
        }

        return new Candle
        {
            DatetimeUtc = dtUtc,
            Open = ParseDec("open"),
            High = ParseDec("high"),
            Low = ParseDec("low"),
            Close = ParseDec("close")
        };
    }
}
