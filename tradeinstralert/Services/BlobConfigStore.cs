using System.Text.Json;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Logging;
using tradeinstralert.Models;

namespace tradeinstralert.Services;

public sealed class BlobConfigStore
{
    private readonly BlobServiceClient _blobService;
    private readonly ILogger<BlobConfigStore> _logger;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    public BlobConfigStore(BlobServiceClient blobService, ILogger<BlobConfigStore> logger)
    {
        _blobService = blobService;
        _logger = logger;
    }

    public async Task<WatchConfig> ReadWatchConfigAsync(string containerName, string blobName, CancellationToken ct)
    {
        var container = _blobService.GetBlobContainerClient(containerName);
        var blob = container.GetBlobClient(blobName);

        if (!await blob.ExistsAsync(ct))
            throw new InvalidOperationException($"Config blob not found: {containerName}/{blobName}");

        var content = await blob.DownloadContentAsync(ct);
        var json = content.Value.Content.ToString();
        var cfg = JsonSerializer.Deserialize<WatchConfig>(json, _jsonOptions);

        if (cfg is null)
            throw new InvalidOperationException("Could not deserialize watch config JSON.");

        // Defensive defaults
        cfg.Interval = string.IsNullOrWhiteSpace(cfg.Interval) ? "5min" : cfg.Interval;
        cfg.OutputSize = cfg.OutputSize <= 0 ? 1 : cfg.OutputSize;
        cfg.Instruments ??= new();

        return cfg;
    }

    public async Task<AlertState> ReadStateAsync(string containerName, string blobName, CancellationToken ct)
    {
        var container = _blobService.GetBlobContainerClient(containerName);
        var blob = container.GetBlobClient(blobName);

        if (!await blob.ExistsAsync(ct))
            return new AlertState();

        var content = await blob.DownloadContentAsync(ct);
        var json = content.Value.Content.ToString();

        try
        {
            return JsonSerializer.Deserialize<AlertState>(json, _jsonOptions) ?? new AlertState();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "State blob was unreadable; starting fresh.");
            return new AlertState();
        }
    }

    public async Task WriteStateAsync(string containerName, string blobName, AlertState state, CancellationToken ct)
    {
        var container = _blobService.GetBlobContainerClient(containerName);
        await container.CreateIfNotExistsAsync(cancellationToken: ct);

        var blob = container.GetBlobClient(blobName);
        var json = JsonSerializer.Serialize(state, _jsonOptions);

        using var ms = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));
        await blob.UploadAsync(ms, overwrite: true, cancellationToken: ct);
    }
}
