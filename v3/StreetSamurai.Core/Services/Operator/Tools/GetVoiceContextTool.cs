using System.Text.Json;

namespace StreetSamurai.Core.Services.Operator.Tools;

/// <summary>
/// Pulls per-character voice profiles + active conversation goals so the
/// operator can draft dialogue that sounds like the actual character. Returns
/// the same context blocks the autonomous pipeline injects — vocabulary,
/// cadence, tells, what each character will and won't say.
/// </summary>
public class GetVoiceContextTool : IWriterTool
{
    private readonly DialogueService dialogue;
    public GetVoiceContextTool(DialogueService dialogue) { this.dialogue = dialogue; }

    public string Name => "get_voice_context";

    public string Description =>
        "Fetch character voice profiles and conversation goals before drafting " +
        "dialogue. Returns the same per-character voice context the autonomous " +
        "pipeline uses — vocabulary register, sentence shape, tells, refusals — " +
        "plus what each character is trying to get from the conversation given " +
        "the beat goal and tension. CALL THIS BEFORE writing dialogue so each " +
        "voice stays distinct.";

    public string ParametersJsonSchema => """
    {
      "type": "object",
      "properties": {
        "characters": {
          "type": "array",
          "items": { "type": "string" },
          "description": "Canonical names of speakers in the scene."
        },
        "beat_goal": {
          "type": "string",
          "description": "What this conversation is trying to accomplish narratively."
        },
        "tension_level": {
          "type": "integer", "minimum": 1, "maximum": 10, "default": 5
        }
      },
      "required": ["characters"]
    }
    """;

    public Task<string> InvokeAsync(JsonElement args, OperatorContext ctx, CancellationToken ct)
    {
        var chars = new List<string>();
        if (args.TryGetProperty("characters", out var arr) && arr.ValueKind == JsonValueKind.Array)
            foreach (var v in arr.EnumerateArray())
                if (v.ValueKind == JsonValueKind.String) chars.Add(v.GetString() ?? "");

        if (chars.Count == 0)
            return Task.FromResult(JsonSerializer.Serialize(new { error = "characters[] is required." }));

        var goal = args.TryGetProperty("beat_goal", out var g) ? g.GetString() ?? "" : "";
        var tension = args.TryGetProperty("tension_level", out var t) && t.ValueKind == JsonValueKind.Number
            ? t.GetInt32() : 5;

        var voiceContext = dialogue.BuildDialogueContext(chars);
        var convoGoals = dialogue.BuildConversationGoals(chars, goal, tension);

        return Task.FromResult(JsonSerializer.Serialize(new
        {
            characters = chars,
            voice_context = voiceContext,
            conversation_goals = convoGoals,
        }));
    }
}
