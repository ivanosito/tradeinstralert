using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using tradeinstralert.Models;
using tradeinstralert.Services;
using tradeinstralert.Utils;

namespace tradeinstralert;

public sealed class TradeInstrumentAlertFunction
{
    private readonly ILogger<TradeInstrumentAlertFunction> _logger;
    private readonly BlobConfigStore _configStore;
    private readonly TwelveDataClient _twelve;
    private readonly VoiceTradingSmsSender _sms;

    public TradeInstrumentAlertFunction(
        ILogger<TradeInstrumentAlertFunction> logger,
        BlobConfigStore configStore,
        TwelveDataClient twelve,
        VoiceTradingSmsSender sms)
    {
        _logger = logger;
        _configStore = configStore;
        _twelve = twelve;
        _sms = sms;
    }

    // Every 5 minutes
    [Function("TradeInstrumentAlert")]
    public async Task RunAsync([TimerTrigger("0 */5 * * * *")] TimerInfo myTimer, CancellationToken ct)
    {
        var configContainer = Env.Get("CONFIG_CONTAINER", "config");
        var configBlob = Env.Get("CONFIG_BLOB", "tradeinstralert.json");
        var stateBlob = Env.Get("STATE_BLOB", "state.json");

        var apiKey = Env.Require("TWELVEDATA_API_KEY");

        // SMS settings (optional: you can disable SMS by leaving SMS_ENABLED=false)
        var smsEnabled = string.Equals(Env.Get("SMS_ENABLED", "true"), "true", StringComparison.OrdinalIgnoreCase);
        var smsBaseUrl = Env.Get("VOICETRADING_SMS_URL", "https://www.voicetrading.com/myaccount/sendsms.php");
        var smsUser = Env.Get("VOICETRADING_USERNAME", "");
        var smsPass = Env.Get("VOICETRADING_PASSWORD", "");
        var smsFrom = Env.Get("VOICETRADING_FROM", "");
        var smsTo = Env.Get("VOICETRADING_TO", "");

        _logger.LogInformation("Tick at {Now}. Reading config {Container}/{Blob}", DateTimeOffset.UtcNow, configContainer, configBlob);

        WatchConfig cfg;
        try
        {
            cfg = await _configStore.ReadWatchConfigAsync(configContainer, configBlob, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Could not load watch config.");
            return;
        }

        var state = await _configStore.ReadStateAsync(configContainer, stateBlob, ct);

        foreach (var w in cfg.Instruments.Where(i => i.Enabled))
        {
            if (string.IsNullOrWhiteSpace(w.Id) || string.IsNullOrWhiteSpace(w.Symbol))
            {
                _logger.LogWarning("Skipping instrument with missing Id/Symbol.");
                continue;
            }

            var candle = await _twelve.GetLatestCandleAsync(apiKey, w.Symbol, cfg.Interval, cfg.OutputSize, ct);
            if (candle is null)
            {
                _logger.LogWarning("No candle returned for {Id} ({Symbol})", w.Id, w.Symbol);
                continue;
            }

            var hitHigh = w.TargetHigh is not null && candle.High >= w.TargetHigh.Value;
            var hitLow = w.TargetLow is not null && candle.Low <= w.TargetLow.Value;
            var hit = hitHigh || hitLow;

            _logger.LogInformation(
                "{Id} {Symbol} candle {Dt} O:{O} H:{H} L:{L} C:{C} | Targets: high>={Th} low<={Tl} | Hit={Hit}",
                w.Id, w.Symbol, candle.DatetimeUtc, candle.Open, candle.High, candle.Low, candle.Close,
                w.TargetHigh, w.TargetLow, hit);

            if (!hit)
                continue;

            // De-dup: only alert once per candle datetime
            if (state.LastAlertedCandleUtc.TryGetValue(w.Id, out var lastDt) && lastDt == candle.DatetimeUtc)
            {
                _logger.LogInformation("Already alerted {Id} for candle {Dt}; skipping.", w.Id, candle.DatetimeUtc);
                continue;
            }

            var msg = BuildSmsMessage(w, candle, hitHigh, hitLow);

            if (smsEnabled)
            {
                if (string.IsNullOrWhiteSpace(smsUser) || string.IsNullOrWhiteSpace(smsPass) ||
                    string.IsNullOrWhiteSpace(smsFrom) || string.IsNullOrWhiteSpace(smsTo))
                {
                    _logger.LogWarning("SMS enabled but VOICETRADING_* env vars are missing; not sending.");
                }
                else
                {
                    var ok = await _sms.SendAsync(smsBaseUrl, smsUser, smsPass, smsFrom, smsTo, msg, ct);
                    if (!ok)
                        _logger.LogWarning("SMS send failed for {Id}", w.Id);
                }
            }

            state.LastAlertedCandleUtc[w.Id] = candle.DatetimeUtc;
            await _configStore.WriteStateAsync(configContainer, stateBlob, state, ct);
        }
    }

    private static string BuildSmsMessage(InstrumentWatch w, Candle c, bool hitHigh, bool hitLow)
    {
        // Keep it short (SMS-friendly)
        var parts = new List<string>
        {
            $"ALERT {w.Id} ({w.Symbol})",
            $"{c.DatetimeUtc:yyyy-MM-dd HH:mm} UTC",
            $"O{c.Open} H{c.High} L{c.Low} C{c.Close}"
        };

        if (hitHigh && w.TargetHigh is not null) parts.Add($"High >= {w.TargetHigh}");
        if (hitLow && w.TargetLow is not null) parts.Add($"Low <= {w.TargetLow}");

        return string.Join(" | ", parts);
    }
}
