using System;
using System.Threading;
using System.Threading.Tasks;

using iLoveVideoEditor.Sdk.Api;
using iLoveVideoEditor.Sdk.Client;
using iLoveVideoEditor.Sdk.Model;

namespace iLoveVideoEditor.Sdk;

/// <summary>
/// Facade over the generated API client: API-key auth, render polling,
/// download URLs and template helpers. Mirrors the Node/Python SDKs.
/// </summary>
public sealed class ILoveVideoEditorClient
{
    private static readonly TimeSpan DefaultPollInterval = TimeSpan.FromSeconds(5);

    private readonly RenderApi _renderApi;
    private readonly TemplatesApi _templatesApi;

    /// <summary>
    /// Create a client authenticated with an API key from the iLoveVideoEditor dashboard.
    /// </summary>
    public ILoveVideoEditorClient(string apiKey, string baseUrl = "https://api.ilovevideoeditor.com")
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new ArgumentException("API key is required", nameof(apiKey));

        var configuration = new Configuration { BasePath = baseUrl };
        configuration.AddApiKey("x-api-key", apiKey);
        _renderApi = new RenderApi(configuration);
        _templatesApi = new TemplatesApi(configuration);
    }

    /// <summary>Queue a render and return immediately with the job id.</summary>
    public async Task<RenderResult> QueueRenderAsync(
        VideoJSON videoJson,
        CancellationToken cancellationToken = default)
    {
        var response = await _renderApi
            .QueueRenderAsync(new QueueRenderRequest(videoJson), cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        return new RenderResult(response.JobId, response.Status) { Stage = response.Stage };
    }

    /// <summary>Queue a render and poll until it reaches a terminal state.</summary>
    public async Task<RenderResult> RenderAsync(
        VideoJSON videoJson,
        IProgress<int>? progress = null,
        TimeSpan? pollInterval = null,
        CancellationToken cancellationToken = default)
    {
        var queued = await QueueRenderAsync(videoJson, cancellationToken).ConfigureAwait(false);
        var interval = pollInterval ?? DefaultPollInterval;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var result = await GetRenderAsync(queued.JobId, cancellationToken).ConfigureAwait(false);
            if (result.ProgressPercent is { } percent)
                progress?.Report(percent);
            if (result.IsTerminal)
                return result;

            await Task.Delay(interval, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>Fetch the current state of a render job.</summary>
    public async Task<RenderResult> GetRenderAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        var job = await _renderApi.GetRenderStatusAsync(jobId, cancellationToken).ConfigureAwait(false);
        return new RenderResult(job.JobId, job.Status)
        {
            Stage = job.Stage,
            ProgressPercent = job.Progress?.Percent,
            DownloadUrl = job.Url,
            Error = job.Error,
        };
    }

    /// <summary>Get a fresh signed download URL for a completed render.</summary>
    public async Task<string> GetDownloadUrlAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        var response = await _renderApi
            .GetRenderDownloadUrlAsync(jobId, cancellationToken)
            .ConfigureAwait(false);
        return response.DownloadUrl;
    }

    /// <summary>List the public templates.</summary>
    public Task<ListTemplates200Response> ListTemplatesAsync(CancellationToken cancellationToken = default) =>
        _templatesApi.ListTemplatesAsync(cancellationToken);

    /// <summary>Get one template by id or slug.</summary>
    public Task<GetTemplate200Response> GetTemplateAsync(string id, CancellationToken cancellationToken = default) =>
        _templatesApi.GetTemplateAsync(id, cancellationToken);
}

/// <summary>Flattened render-job state returned by <see cref="ILoveVideoEditorClient"/>.</summary>
public sealed class RenderResult
{
    /// <summary>Create a result for a job in a known state.</summary>
    public RenderResult(Guid jobId, string status)
    {
        JobId = jobId;
        Status = status;
    }

    /// <summary>The render job id.</summary>
    public Guid JobId { get; }

    /// <summary>Job state: pending, rendering, completed, failed or cancelled.</summary>
    public string Status { get; }

    /// <summary>Pipeline stage reported by the renderer, when available.</summary>
    public string? Stage { get; init; }

    /// <summary>Progress in percent (0–100) while the job is running.</summary>
    public int? ProgressPercent { get; init; }

    /// <summary>Signed download URL, present once the job completed.</summary>
    public string? DownloadUrl { get; init; }

    /// <summary>Error message when the job failed.</summary>
    public string? Error { get; init; }

    /// <summary>True when the job reached a terminal state (completed, failed or cancelled).</summary>
    public bool IsTerminal => Status is "completed" or "failed" or "cancelled";

    /// <inheritdoc />
    public override string ToString() => $"{JobId}: {Status}";
}
