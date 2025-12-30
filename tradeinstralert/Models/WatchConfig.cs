namespace tradeinstralert.Models;

public sealed class WatchConfig
{
    public int Version { get; set; } = 1;
    public string Interval { get; set; } = "5min";
    public int OutputSize { get; set; } = 1;
    public List<InstrumentWatch> Instruments { get; set; } = new();
}

public sealed class InstrumentWatch
{
    // Unique id used for state de-duplication (e.g., "GOLD")
    public string Id { get; set; } = string.Empty;

    // Twelve Data symbol (e.g., "XAU/USD")
    public string Symbol { get; set; } = string.Empty;

    // Price thresholds
    public decimal? TargetHigh { get; set; }
    public decimal? TargetLow { get; set; }

    public bool Enabled { get; set; } = true;
}

public sealed class Candle
{
    public DateTime DatetimeUtc { get; set; }
    public decimal Open { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public decimal Close { get; set; }
}
