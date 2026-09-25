using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Prose.Core.Services.Discussion;

/// <summary>Whether anything can transcribe right now, and why not.</summary>
/// <param name="Engine">Which provider would be used — named so the UI never has to guess.</param>
public sealed record SpeechAvailability(bool Ready, string Engine, string? Blocker);

/// <summary>What was heard, what heard it, and what that cost.</summary>
/// <param name="RecordingPath">Where the audio was kept, relative to
/// <see cref="SpeechService.RecordingsRoot"/>. Null when nothing was kept.</param>
public sealed record Transcript(
    string Text, string Engine, string Model, double Cost, string? RecordingPath);

/// <summary>
/// Turning a recording into words.
///
/// <para><b>Hosted, not local.</b> The rest of the voice loop is local-first because replaying a
/// line is something you do thirty times in an afternoon; transcription is not that. A local
/// whisper.cpp build buys offline operation at a large cost in setup and in latency, and latency
/// is the thing most likely to make this loop unusable. Groq's <c>whisper-large-v3-turbo</c> is
/// the fastest hosted transcription available and costs about four cents an hour of audio, which
/// at one author's scale is indistinguishable from free.</para>
///
/// <para><b>The recording is kept.</b> A transcription error reads exactly like a change of mind
/// six weeks later, and <see cref="Data.Entities.DiscussionTurn.InputMode"/> alone only says that
/// a turn was spoken. The audio is the only thing that can settle what was actually said, so it
/// is written beside the turn rather than discarded once the text comes back.</para>
/// </summary>
public sealed class SpeechService(
    HttpClient http,
    SettingsService settings,
    ILogger<SpeechService> log)
{
    private const string GroqEndpoint = "https://api.groq.com/openai/v1/audio/transcriptions";
    private const string GroqModel = "whisper-large-v3-turbo";

    private const string OpenAiEndpoint = "https://api.openai.com/v1/audio/transcriptions";
    private const string OpenAiModel = "whisper-1";

    /// <summary>Published rates per second of audio, 2026-09: Groq turbo at $0.04/hour,
    /// OpenAI whisper-1 at $0.006/minute. Billed on audio duration, not on tokens, so the client's
    /// own measured length is the right multiplicand and no usage block comes back to reconcile
    /// against — which is why these are constants here and not read off a response.</summary>
    private const double GroqCostPerSecond = 0.04 / 3600.0;
    private const double OpenAiCostPerSecond = 0.006 / 60.0;

    /// <summary>
    /// Whisper's <c>prompt</c> is capped at 224 tokens and silently truncates past it — from the
    /// END, so an overlong vocabulary quietly loses its tail rather than failing. Kept well under
    /// in characters so the cut never happens where we cannot see it.
    /// </summary>
    public const int MaxVocabularyChars = 700;

    /// <summary>Where recordings live. Beside settings and the Piper voices, not in the install
    /// folder, which a redeploy wipes.</summary>
    public static string RecordingsRoot { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "MindAttic", "Prose", "voice-notes");

    public SpeechAvailability Probe()
    {
        if (!string.IsNullOrWhiteSpace(settings.GroqApiKey))
            return new SpeechAvailability(true, $"groq/{GroqModel}", null);
        if (!string.IsNullOrWhiteSpace(settings.OpenAiApiKey))
            return new SpeechAvailability(true, $"openai/{OpenAiModel}", null);

        return new SpeechAvailability(false, "none",
            "No transcription key. Add a Groq key in Settings — it is the fastest and costs about "
            + "four cents an hour of audio. An OpenAI key also works.");
    }

    /// <summary>
    /// Transcribe a recording.
    /// </summary>
    /// <param name="vocabulary">Proper nouns to prime the model with — see
    /// <see cref="SpeechVocabularyService"/>. Whisper treats <c>prompt</c> as the text preceding
    /// the audio, so a list of this book's invented names is what makes "Togishi" come back as
    /// "Togishi" rather than "to gishi".</param>
    /// <param name="durationMs">The recording's length as the browser measured it. Used only to
    /// price the call; a zero or missing value costs nothing rather than guessing.</param>
    public async Task<Transcript> TranscribeAsync(
        byte[] audio,
        string mimeType,
        string? vocabulary = null,
        int durationMs = 0,
        bool keepRecording = true,
        CancellationToken ct = default)
    {
        if (audio.Length == 0)
            throw new InvalidOperationException("The recording was empty — nothing was captured.");

        var availability = Probe();
        if (!availability.Ready)
            throw new InvalidOperationException(availability.Blocker);

        // Written before the network call, not after. A transcription that fails or comes back
        // garbled is exactly the case where the author wants the audio, and a save that only
        // happens on success would throw it away in precisely that case.
        var path = keepRecording ? await SaveRecordingAsync(audio, mimeType, ct) : null;

        var seconds = Math.Max(0, durationMs) / 1000.0;

        if (!string.IsNullOrWhiteSpace(settings.GroqApiKey))
        {
            try
            {
                var text = await PostAsync(
                    GroqEndpoint, settings.GroqApiKey, GroqModel, audio, mimeType, vocabulary, ct);
                return new Transcript(text, "groq", GroqModel, seconds * GroqCostPerSecond, path);
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex) when (!string.IsNullOrWhiteSpace(settings.OpenAiApiKey))
            {
                // Unlike Claude — which has no fallback below a BYO key on purpose — transcription
                // has no ruling against one, and a dropped utterance is lost work the author has
                // to say again.
                log.LogWarning(ex, "Groq transcription failed; falling back to OpenAI.");
            }
        }

        var fallback = await PostAsync(
            OpenAiEndpoint, settings.OpenAiApiKey, OpenAiModel, audio, mimeType, vocabulary, ct);
        return new Transcript(fallback, "openai", OpenAiModel, seconds * OpenAiCostPerSecond, path);
    }

    /// <summary>Both providers speak the OpenAI audio-transcriptions shape, so one call covers
    /// them — only the host and the bearer differ.</summary>
    private async Task<string> PostAsync(
        string endpoint, string apiKey, string model,
        byte[] audio, string mimeType, string? vocabulary, CancellationToken ct)
    {
        var (mime, extension) = NormalizeMime(mimeType);

        using var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(audio);
        file.Headers.ContentType = new MediaTypeHeaderValue(mime);
        form.Add(file, "file", $"utterance.{extension}");
        form.Add(new StringContent(model), "model");
        form.Add(new StringContent("json"), "response_format");
        // The author is dictating, not being creative: take the likeliest reading every time.
        form.Add(new StringContent("0"), "temperature");
        // Stated rather than detected. Language detection on a two-second utterance is a coin
        // toss, and a wrong guess returns a translation instead of a transcript.
        form.Add(new StringContent("en"), "language");

        if (!string.IsNullOrWhiteSpace(vocabulary))
            form.Add(new StringContent(Truncate(vocabulary, MaxVocabularyChars)), "prompt");

        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint) { Content = form };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        using var response = await http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            // The body says which of the several possible things went wrong — unsupported
            // container, file too large, bad key — and EnsureSuccessStatusCode would discard it.
            string body;
            try { body = await response.Content.ReadAsStringAsync(ct); }
            catch { body = "<unreadable response body>"; }
            if (body.Length > 600) body = body[..600] + "…";
            throw new HttpRequestException(
                $"Transcription {(int)response.StatusCode} {response.StatusCode} " +
                $"(model={model}, {audio.Length} bytes of {mime}): {body}",
                inner: null, statusCode: response.StatusCode);
        }

        var json = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.TryGetProperty("text", out var t) ? (t.GetString() ?? "").Trim() : "";
    }

    /// <summary>
    /// Keep the audio, and return the path to record against the turn.
    /// </summary>
    /// <returns>A path relative to <see cref="RecordingsRoot"/>, or null when the write failed —
    /// losing a recording must never lose the transcription with it.</returns>
    public async Task<string?> SaveRecordingAsync(byte[] audio, string mimeType, CancellationToken ct = default)
    {
        try
        {
            var (_, extension) = NormalizeMime(mimeType);
            // Foldered by month so the directory stays listable after a year of dictation.
            var folder = DateTime.UtcNow.ToString("yyyy-MM");
            var name = $"{Guid.CreateVersion7():N}.{extension}";

            var directory = Path.Combine(RecordingsRoot, folder);
            Directory.CreateDirectory(directory);
            await File.WriteAllBytesAsync(Path.Combine(directory, name), audio, ct);

            // Forward slashes: this is a stored identifier, and it should read the same whichever
            // machine later resolves it.
            return $"{folder}/{name}";
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            log.LogWarning(ex, "Could not keep the recording; the transcript is unaffected.");
            return null;
        }
    }

    /// <summary>The absolute path of a stored recording, or null when it is no longer there.</summary>
    public static string? ResolveRecording(string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath)) return null;
        var full = Path.GetFullPath(Path.Combine(RecordingsRoot, relativePath));
        // A stored path is data, and data that escapes its root is how a read turns into an
        // arbitrary-file read.
        // With the separator: a bare prefix test let "..\voice-notes-x\…" through, since a
        // sibling folder named "voice-notes-x" starts with "voice-notes".
        var root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(RecordingsRoot)) + Path.DirectorySeparatorChar;
        if (!full.StartsWith(root, StringComparison.OrdinalIgnoreCase)) return null;
        return File.Exists(full) ? full : null;
    }

    /// <summary>
    /// <c>MediaRecorder</c> reports its type with codec parameters attached
    /// (<c>audio/webm;codecs=opus</c>), which <see cref="MediaTypeHeaderValue"/> rejects and the
    /// APIs do not want either. Strip to the bare type and derive the filename extension, which
    /// is what both providers actually sniff the container from.
    /// </summary>
    public static (string Mime, string Extension) NormalizeMime(string? mimeType)
    {
        var mime = (mimeType ?? "").Split(';')[0].Trim().ToLowerInvariant();
        return mime switch
        {
            "audio/webm" => ("audio/webm", "webm"),
            "audio/ogg" => ("audio/ogg", "ogg"),
            "audio/mp4" => ("audio/mp4", "mp4"),
            "audio/mpeg" => ("audio/mpeg", "mp3"),
            "audio/wav" or "audio/wave" or "audio/x-wav" => ("audio/wav", "wav"),
            // Chromium's MediaRecorder gives webm/opus everywhere WebView2 runs; anything else is
            // worth sending under its own name rather than refusing to try.
            _ => ("audio/webm", "webm"),
        };
    }

    private static string Truncate(string s, int max) => s.Length <= max ? s : s[..max];
}
