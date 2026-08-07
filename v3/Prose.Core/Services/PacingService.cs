namespace Prose.Core.Services;

/// <summary>
/// Manages narrative pacing — the rhythm of storytelling that creative writing
/// programs teach but most AI-generated fiction lacks.
///
/// Modeled on the principles of:
/// - Scene vs. Summary (showing action in real-time vs. compressing time)
/// - Sentence rhythm (short sentences = tension, long = reflection)
/// - The "beat and breathe" pattern (action → pause → sensory → advance)
/// - Exposition budgeting (never dump, always earn)
/// - The sensory anchor (every scene needs a physical detail that grounds it)
/// - Rising/falling tension across a scene arc
/// </summary>
public class PacingService
{
    /// <summary>Pacing modes that shape how the LLM writes.</summary>
    public enum PaceMode
    {
        /// <summary>Slow, sensory, immersive. Long sentences. Rich detail. The reader should feel the space.</summary>
        Breathe,
        /// <summary>Normal conversational pace. Dialogue-driven. Character interaction.</summary>
        Flow,
        /// <summary>Tension building. Shorter sentences. Details that feel wrong. Something is coming.</summary>
        Tighten,
        /// <summary>Action. Short declarative sentences. No interior monologue. Movement and consequence.</summary>
        Strike,
        /// <summary>Aftermath. The cost of what just happened. Emotional processing. Quiet.</summary>
        Settle
    }

    /// <summary>Get the pacing instruction for a beat based on its position and context.</summary>
    public static PacingInstruction GetPacing(int beatIndex, int totalBeats, string? beatGoal = null)
    {
        // Default arc: Breathe → Flow → Tighten → Strike → Settle
        var position = (float)beatIndex / Math.Max(totalBeats - 1, 1);
        var mode = position switch
        {
            < 0.15f => PaceMode.Breathe,    // Opening — establish the world
            < 0.4f => PaceMode.Flow,         // Development — character and dialogue
            < 0.7f => PaceMode.Tighten,      // Rising tension
            < 0.85f => PaceMode.Strike,      // Climax
            _ => PaceMode.Settle              // Resolution
        };

        // Override based on beat goal keywords
        if (beatGoal != null)
        {
            var goal = beatGoal.ToLowerInvariant();
            if (goal.Contains("fight") || goal.Contains("chase") || goal.Contains("escape") || goal.Contains("attack"))
                mode = PaceMode.Strike;
            else if (goal.Contains("discover") || goal.Contains("arrive") || goal.Contains("enter") || goal.Contains("explore"))
                mode = PaceMode.Breathe;
            else if (goal.Contains("confront") || goal.Contains("tension") || goal.Contains("threaten"))
                mode = PaceMode.Tighten;
            else if (goal.Contains("aftermath") || goal.Contains("grief") || goal.Contains("reflect") || goal.Contains("cost"))
                mode = PaceMode.Settle;
        }

        return new PacingInstruction(mode);
    }
}

/// <summary>Pacing instruction injected into the generation prompt.</summary>
public record PacingInstruction(PacingService.PaceMode Mode)
{
    /// <summary>Prose style guidance for the LLM.</summary>
    public string ProseGuidance => Mode switch
    {
        PacingService.PaceMode.Breathe => """
            PACING: BREATHE — This beat is slow and immersive.
            - Long, flowing sentences. Let the reader inhabit the space.
            - Lead with sensory detail: what does this place smell like, sound like, feel against skin?
            - No exposition dumps. Ground every fact in a physical sensation.
            - If there's worldbuilding to convey, embed it in what the character notices, not what the narrator explains.
            - The reader should feel like they're standing there.
            """,
        PacingService.PaceMode.Flow => """
            PACING: FLOW — This beat is conversational and character-driven.
            - Mix dialogue with action. Characters reveal themselves through what they say and how they move.
            - Vary sentence length naturally — some short, some long, matching the rhythm of conversation.
            - Show relationships through behavior, not description. Don't tell the reader how characters feel about each other.
            - Advance the plot through interaction, not narration.
            """,
        PacingService.PaceMode.Tighten => """
            PACING: TIGHTEN — Tension is building. Something is wrong or about to go wrong.
            - Sentences get shorter as the beat progresses.
            - Details become specific and sharp — a sound that shouldn't be there, a glance that lasts too long.
            - The character's body knows before their mind does. Write the physical response first: stomach drops, neck hair rises.
            - Don't name the threat. Let the reader feel it through the character's unease.
            - End the beat on an unresolved tension.
            """,
        PacingService.PaceMode.Strike => """
            PACING: STRIKE — Action. This beat moves fast.
            - Short declarative sentences. Subject-verb-object. No ornamentation.
            - No interior monologue during action — the character is reacting, not thinking.
            - Every sentence advances the situation. Cut anything that doesn't move.
            - Physical consequences are immediate and specific: not "he was hurt" but "the round punched through his shoulder and spun him into the wall."
            - Sound, impact, movement. The reader should feel the velocity.
            """,
        PacingService.PaceMode.Settle => """
            PACING: SETTLE — The aftermath. What it cost.
            - Return to long, quiet sentences. The urgency is gone.
            - Focus on what's changed — the room looks different now, the character's hands are shaking, someone is missing.
            - Sensory details return but they're different: the same place feels altered by what happened in it.
            - Let the character process. Interior monologue is welcome here. Let them sit with it.
            - This is where the reader feels the weight of the story.
            - HARD STOP: do NOT have the narrator summarize what the scene or story meant. No 'and in that moment she understood that...' — no moral gloss, no retrospective explanation of significance. The images and the cost are the meaning. Naming it kills it.
            """,
        _ => ""
    };
}
