namespace tradeinstralert.Models;

public sealed class AlertState
{
    // instrumentId -> last alerted candle datetime (UTC, ISO string)
    public Dictionary<string, DateTime> LastAlertedCandleUtc { get; set; } = new();
}
