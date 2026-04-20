using MindAttic.LLMVoting;
using StreetSamurai.Core.Interfaces;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Resolves flagged fact inconsistencies by submitting both candidate values
/// to all configured LLM providers as a choice vote.
///
/// When two sources disagree on a fact (e.g., zone = "Z3" vs "Z4"), the
/// majority-vote consensus from the frequency-counting phase isn't always
/// semantically correct. This service asks multiple LLMs to weigh in, using
/// world context to decide which value is actually right.
///
/// If the LLM vote agrees with the frequency consensus → dismiss the flag.
/// If the LLM vote overturns the consensus → the source file likely has an
/// error; flag as "minority correct" so the author can fix it.
/// </summary>
public class FactResolutionService
{
    private readonly LLMVotingService voting;
    private readonly FactDiscoveryService factDb;

    public FactResolutionService(LLMVotingService voting, FactDiscoveryService factDb)
    {
        this.voting = voting;
        this.factDb = factDb;
    }

    public bool HasProviders => voting.GetActiveProviderIds().Count > 0;

    /// <summary>
    /// Submit a flagged inconsistency to all configured LLMs as a binary choice vote.
    /// Marks the flag as dismissed regardless of outcome; returns the vote result for display.
    /// If the minority value won, the caller should prompt the author to review the source file.
    /// </summary>
    public async Task<ResolutionResult> ResolveAsync(FlaggedFact fact, CancellationToken ct = default)
    {
        var request = new VoteRequest
        {
            Question  = $"For the entity \"{fact.EntityName}\", what is the correct value for the attribute \"{fact.Predicate}\"?",
            Context   = BuildContext(fact),
            Options   = [fact.IncorrectValue, fact.CorrectValue],
            MaxTokens = 256,
            Temperature      = 0.1,
            SynthesizeNarrative = false,
        };

        var result = await voting.VoteAsync(request, Quorum.SimpleMajority, ct);

        // Did the LLM panel side with the minority (the flagged file's value)?
        var minorityWon = result.QuorumReached &&
            result.Consensus.Equals(fact.IncorrectValue, StringComparison.OrdinalIgnoreCase);

        factDb.MarkDismissed(fact.Id);

        return new ResolutionResult
        {
            FlagId            = fact.Id,
            WinningValue      = result.Consensus,
            MinorityWon       = minorityWon,
            QuorumReached     = result.QuorumReached,
            ConsensusStrength = result.ConsensusStrength,
            VoterCount        = result.IndividualVotes.Count(v => !v.IsError),
            Votes             = result.IndividualVotes
                .Where(v => !v.IsError)
                .Select(v => new VoteDetail(v.VoterName, v.Decision, v.Confidence))
                .ToList(),
            SourceFile        = fact.SourceFile,
        };
    }

    private static string BuildContext(FlaggedFact fact)
    {
        var fileName = Path.GetFileName(fact.SourceFile);
        var repo     = Path.GetFileName(Path.GetDirectoryName(fact.SourceFile) ?? "");
        return $"""
            SETTING: GLMZ (Great Lakes Mega Zone) — post-collapse cyberpunk city, year 2250.
            Corponations have replaced governments. The world is partitioned into 12 geographic zones (Z1–Z12 and Z∞).
            Currency symbol is Φ (Quanta). Iowan Behemoths are autonomous machines, not alive.

            ENTITY: {fact.EntityName}
            TYPE: {repo} entity
            ATTRIBUTE: {fact.Predicate}

            TWO CONFLICTING VALUES WERE FOUND ACROSS MULTIPLE SOURCE FILES:

            Value A (minority — {(1.0 - fact.Confidence):P0} of sources, including {fileName}):
              {fact.IncorrectValue}

            Value B (majority — {fact.Confidence:P0} of sources):
              {fact.CorrectValue}

            Based on the entity name, attribute, and world context, which value is more likely correct?
            """;
    }
}

public record ResolutionResult
{
    public int               FlagId            { get; init; }
    public string            WinningValue      { get; init; } = "";
    public bool              MinorityWon       { get; init; }
    public bool              QuorumReached     { get; init; }
    public double            ConsensusStrength { get; init; }
    public int               VoterCount        { get; init; }
    public List<VoteDetail>  Votes             { get; init; } = [];
    public string            SourceFile        { get; init; } = "";
}

public record VoteDetail(string Model, string Decision, int Confidence);
