# iLoveVideoEditor.Sdk

[![NuGet](https://img.shields.io/nuget/v/iLoveVideoEditor.Sdk.svg)](https://www.nuget.org/packages/iLoveVideoEditor.Sdk)
[![license](https://img.shields.io/badge/license-MIT-blue.svg)](https://github.com/ilovevideoeditor/sdk-dotnet/blob/main/LICENSE)

Official .NET SDK for the [iLoveVideoEditor](https://ilovevideoeditor.com) API —
render videos from VideoJSON specs in C#, F# or VB.

- Auto-generated client for the full REST API (`iLoveVideoEditor.Sdk.Api`) from our OpenAPI spec
- Hand-written facade `ILoveVideoEditorClient` with API-key auth, render polling and download helpers

## Install

```bash
dotnet add package iLoveVideoEditor.Sdk
```

Requires .NET 8+. Get an API key from the
[dashboard](https://ilovevideoeditor.com/dashboard).

## Quickstart

```csharp
using iLoveVideoEditor.Sdk;
using iLoveVideoEditor.Sdk.Model;

var client = new ILoveVideoEditorClient(
    apiKey: Environment.GetEnvironmentVariable("ILOVEVIDEOEDITOR_API_KEY")!);

// Queue a render and wait for the MP4
var result = await client.RenderAsync(
    new VideoJSON(
        name: "Promo",
        layers: new List<object> { /* see VideoJSON docs */ }),
    progress: new Progress<int>(p => Console.Write($"\r{p}%   ")));

if (result.Status == "completed")
{
    var url = await client.GetDownloadUrlAsync(result.JobId);
    Console.WriteLine($"\nDownload: {url}");
}
else
{
    Console.WriteLine($"Render {result.Status}: {result.Error}");
}
```

Fire-and-forget (queue only):

```csharp
var queued = await client.QueueRenderAsync(videoJson);
Console.WriteLine(queued.JobId);

// later
var status = await client.GetRenderAsync(queued.JobId);
```

List the 249+ public templates:

```csharp
var templates = await client.ListTemplatesAsync();
```

## Full API surface

Everything else (projects, assets, API keys, billing, renditions, tools,
webhooks, workflows) is available through the generated API classes in
`iLoveVideoEditor.Sdk.Api` — see [`docs` in the generated client](https://github.com/ilovevideoeditor/sdk-dotnet/tree/main/docs)
or the [API reference](https://ilovevideoeditor.com/docs/api/).

## VideoJSON

A VideoJSON spec describes the scene: layers, text, images, animations, and
timing. Generate one with the [fluent API](https://www.npmjs.com/package/@ilovevideoeditor/core):

```ts
import ILoveVideoEditor from '@ilovevideoeditor/core';

const $ = new ILoveVideoEditor({ name: 'Promo', width: 1920, height: 1080, fps: 30 });
$.addText({ text: 'Hello', fontSize: 12, color: '#fff' });
$.wait('3s');
console.log(JSON.stringify(await $.compile()));
```

## Related SDKs

- [Node.js](https://www.npmjs.com/package/@ilovevideoeditor/sdk-node) · [Python](https://pypi.org/project/ilovevideoeditor-sdk/) · [PHP](https://packagist.org/packages/ilovevideoeditor/sdk) · [Ruby](https://rubygems.org/gems/ilovevideoeditor-sdk) · [Go](https://pkg.go.dev/github.com/ilovevideoeditor/sdk-go)
- CLI: [`ilovevideoeditor`](https://www.npmjs.com/package/ilovevideoeditor) — also on Homebrew, Scoop and winget

## License

MIT
