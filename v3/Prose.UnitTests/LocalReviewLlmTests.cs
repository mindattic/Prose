using System.Net;
using System.Text;
using System.Text.Json;
using NUnit.Framework;
using Prose.Core.Services;
using Prose.Core.Services.Local;

namespace Prose.UnitTests;

/// <summary>
/// Proves the local review transport (<see cref="LocalReviewLlm"/>) is correctly wired
/// and stays segregated from the cloud panel: it POSTs to the configured local endpoint
/// with the local model tag, parses the OpenAI-compatible response, and never reaches a
/// cloud provider. No Ollama / network required — a stub handler stands in.
/// </summary>
[TestFixture]
public class LocalReviewLlmTests
{
    private string storageDir = null!;
    private SettingsService settings = null!;

    [SetUp]
    public void SetUp()
    {
        storageDir = Path.Combine(Path.GetTempPath(), $"ss_localllm_{Guid.NewGuid():N}");
        settings = new SettingsService(storageDir)
        {
            LocalReviewBaseUrl = "http://localhost:11434/v1/chat/completions",
            LocalReviewModel   = "qwen2.5-14b-rev",
        };
    }

    [TearDown]
    public void TearDown()
    {
        settings.Dispose();
        try { Directory.Delete(storageDir, recursive: true); } catch { /* best effort */ }
    }

    [Test]
    public async Task PostsToLocalEndpoint_WithLocalModel_AndParsesContent()
    {
        HttpRequestMessage? captured = null;
        string? capturedBody = null;
        var handler = new StubHandler(async (req, ct) =>
        {
            captured = req;
            capturedBody = req.Content == null ? null : await req.Content.ReadAsStringAsync(ct);
            var json = """{"choices":[{"message":{"role":"assistant","content":"{\"score\":73}"}}]}""";
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
        });
        var http = new HttpClient(handler);
        var sut = new LocalReviewLlm(http, settings);

        // Cloud providerId/apiKey are intentionally ignored by the local transport;
        // pass deliberately bogus cloud values to prove they're not used.
        var result = await sut.CallAsync(
            providerId: "claude-api", apiKey: "sk-should-be-ignored", model: "",
            systemPrompt: "you are a reviewer", userMessage: "rate this node",
            maxTokens: 256, temperature: 0.85);

        Assert.That(result, Is.EqualTo("{\"score\":73}"), "returns choices[0].message.content verbatim");
        Assert.That(captured, Is.Not.Null);
        Assert.That(captured!.RequestUri!.ToString(), Is.EqualTo(settings.LocalReviewBaseUrl),
            "must POST to the configured local endpoint, never a cloud URL");
        Assert.That(capturedBody, Does.Contain("qwen2.5-14b-rev"),
            "falls back to the configured local model tag when model arg is blank");
        Assert.That(capturedBody, Does.Not.Contain("claude-api").And.Not.Contain("sk-should-be-ignored"),
            "cloud provider id / key must not leak into the local request");
    }

    [Test]
    public void Unreachable_ThrowsActionableError_WithoutCloudFallback()
    {
        var handler = new StubHandler((_, _) => throw new HttpRequestException("connection refused"));
        var http = new HttpClient(handler);
        var sut = new LocalReviewLlm(http, settings);

        var ex = Assert.ThrowsAsync<HttpRequestException>(async () =>
            await sut.CallAsync("local", "local", "qwen2.5-14b-rev", "sys", "user"));
        Assert.That(ex!.Message, Does.Contain("ollama").IgnoreCase,
            "a down local server must surface an actionable Ollama hint, not silently fall back to cloud");
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> impl;
        public StubHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> impl) => this.impl = impl;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => impl(request, cancellationToken);
    }
}
