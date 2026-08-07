using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Prose.Core.Data;
using Prose.Core.Models.Canon;

namespace Prose.Core.Services;

/// <summary>
/// Proposes a plausible immediate family for a subject character — parents,
/// siblings, spouse, children — without writing anything until the proposal
/// is explicitly applied. This is the staged growth path the cast scaling
/// concern requires: each proposal call returns ~3-8 new characters that the
/// user reviews before committing.
///
/// <para><b>Scope (iteration 1).</b> Immediate family only. Cousins are NOT
/// generated here because they require generating subject's parents'
/// siblings first (a recursive second-tier pass). Run the proposer on each
/// new aunt/uncle to grow the cousin layer if you want it.</para>
///
/// <para><b>Names.</b> First and last names are sourced from the existing
/// canon character pool, filtered by genetic-ancestry overlap with the
/// subject. So a subject with German + East Asian heritage gets parents
/// whose names already exist on canon characters with German or East Asian
/// ancestry. Keeps the Ubiquitous-Diaspora aesthetic consistent and avoids
/// inventing new etymologies the world hasn't seen.</para>
///
/// <para><b>Surnames.</b></para>
/// <list type="bullet">
///   <item>Subject's parents: each gets ONE chunk of subject's last name (if
///         the subject is "Smith-Jones", father gets "Smith", mother gets
///         "Jones"). For single-chunk subjects, parents share the surname.</item>
///   <item>Siblings: same surname as subject.</item>
///   <item>Spouse: drawn from name pool, NOT subject's surname.</item>
///   <item>Children: subject's surname (no auto-hyphenation — the project
///         just trimmed the cast clean of triple-barreled names).</item>
/// </list>
///
/// <para><b>Genetics.</b> Parents are seeded with subject's own
/// genetic_ancestry as a starting point (since their genes had to come from
/// somewhere). Siblings, spouse-children, and any later descendants get
/// blended via <see cref="GeneticsInheritanceService"/> on apply.</para>
///
/// <para><b>Demographics.</b> Ages computed by simple rules — parents 25–40
/// years older, siblings ±5, spouse ±10, children 18–40 younger. Subject
/// must have a non-zero age for child generation to fire.</para>
/// </summary>
public class FamilyGeneratorService
{
    private readonly IDbContextFactory<ProseDbContext> dbFactory;
    private readonly FamilyTieService                          family;
    private readonly GeneticsInheritanceService                genetics;
    private readonly ILogger<FamilyGeneratorService>           log;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = false,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

    public FamilyGeneratorService(
        IDbContextFactory<ProseDbContext> dbFactory,
        FamilyTieService                          family,
        GeneticsInheritanceService                genetics,
        ILogger<FamilyGeneratorService>           log)
    {
        this.dbFactory = dbFactory;
        this.family    = family;
        this.genetics  = genetics;
        this.log       = log;
    }

    public sealed record ProposedCharacter(
        Guid     Id,
        string   FirstName,
        string?  MiddleName,
        string   LastName,
        int      Age,
        string   Gender,
        string   Heritage,
        string   Role,
        Dictionary<string, double> GeneticAncestry);

    public sealed record FamilyProposal(
        Guid                         SubjectId,
        string                       SubjectName,
        List<ProposedCharacter>      Parents,
        List<ProposedCharacter>      Siblings,
        List<ProposedCharacter>      Spouses,
        List<ProposedCharacter>      Children,
        List<AuntUncleLink>          AuntsUncles,
        List<AuntUncleSpouseLink>    AuntUncleSpouses,
        List<CousinLink>             Cousins)
    {
        public IEnumerable<ProposedCharacter> All =>
            Parents.Concat(Siblings).Concat(Spouses).Concat(Children)
                .Concat(AuntsUncles     .Select(au  => au.Person))
                .Concat(AuntUncleSpouses.Select(aus => aus.Person))
                .Concat(Cousins         .Select(c   => c.Person));
        public int Total =>
            Parents.Count + Siblings.Count + Spouses.Count + Children.Count
            + AuntsUncles.Count + AuntUncleSpouses.Count + Cousins.Count;
    }

    /// <summary>An aunt/uncle is a sibling of one of subject's parents.</summary>
    public sealed record AuntUncleLink(ProposedCharacter Person, Guid ParentId);

    /// <summary>A cousin is a child of one of subject's aunts/uncles. Both
    /// parent ids land here so the apply path can wire <c>parent_of</c> from
    /// each — gives cousins a proper two-parent genetics blend.</summary>
    public sealed record CousinLink(ProposedCharacter Person, Guid AuntUncleId, Guid? AuntUncleSpouseId);

    /// <summary>An aunt/uncle's spouse — provides the second genetic parent
    /// for any cousins under that aunt/uncle.</summary>
    public sealed record AuntUncleSpouseLink(ProposedCharacter Person, Guid AuntUncleId);

    /// <summary>
    /// Build a proposal for the subject's immediate family. No DB writes.
    /// Pass a seeded <see cref="Random"/> for reproducible proposals. Set
    /// <paramref name="includeCousins"/> to also generate aunts/uncles
    /// (subject's parents' siblings) and cousins (their children).
    /// </summary>
    public async Task<FamilyProposal> ProposeAsync(Guid subjectId, Random? rng = null,
        bool includeCousins = false, CancellationToken ct = default)
    {
        rng ??= Random.Shared;
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var subjectRec = await db.Records.AsNoTracking()
            .Where(r => r.EntityId == subjectId)
            .Select(r => new { r.Json, r.Entity!.Name })
            .FirstOrDefaultAsync(ct);
        if (subjectRec == null)
            throw new InvalidOperationException($"Subject {subjectId} not found.");

        var subject = JsonSerializer.Deserialize<CharacterData>(subjectRec.Json, JsonOpts)
            ?? throw new InvalidOperationException($"Could not deserialize CharacterData for {subjectId}.");

        // Pull a name pool of OTHER characters that share an ancestry component.
        var subjectAncestries = subject.GeneticAncestry.Keys
            .Where(k => subject.GeneticAncestry[k] >= 5.0)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var pool = await BuildNamePoolAsync(db, subjectId, subjectAncestries, ct);

        // Subject's last-name chunks (handles "Smith-Jones" splitting cleanly)
        var lastChunks = string.IsNullOrWhiteSpace(subject.PhysicalDescription?.LastNameOrEmpty())
            ? SplitLastName(ExtractLastNameFromFullName(subject.Name))
            : SplitLastName(subject.PhysicalDescription!.LastNameOrEmpty());
        if (lastChunks.Count == 0) lastChunks = new List<string> { "Unknown" };

        // Spouse heritage source: a canon character whose ancestry is
        // *different* from subject's, to keep blended kids interesting per
        // the project's Ubiquitous-Diaspora rule.
        var spouseDonor = await PickSpouseDonorAsync(db, subjectId, subjectAncestries, ct);

        var parents = BuildParents(subject, lastChunks, pool, rng);

        var auntsUncles      = new List<AuntUncleLink>();
        var auntUncleSpouses = new List<AuntUncleSpouseLink>();
        var cousins          = new List<CousinLink>();
        if (includeCousins)
        {
            // For each generated parent, generate 0-3 of THEIR siblings.
            // Surname matches the parent's (siblings share family surname).
            // Ancestry seeded from the parent (will be re-blended on apply
            // if/when grandparents are also generated, which is out of scope).
            foreach (var parent in parents)
            {
                var auCount = rng.Next(0, 4);
                for (int i = 0; i < auCount; i++)
                {
                    var gender = RandomGender(rng);
                    var au = new ProposedCharacter(
                        Id: Guid.CreateVersion7(),
                        FirstName: pool.PickFirstName(gender, rng) ?? $"AuntUncle{i + 1}",
                        MiddleName: null,
                        LastName: parent.LastName,
                        Age: Math.Max(18, parent.Age + rng.Next(-5, 6)),
                        Gender: gender,
                        Heritage: parent.Heritage,
                        Role: "aunt_or_uncle",
                        GeneticAncestry: NoisyCopy(parent.GeneticAncestry, rng));
                    auntsUncles.Add(new AuntUncleLink(au, parent.Id));

                    // Generate the aunt/uncle's spouse so cousins get a proper
                    // two-parent genetics blend. Spouse heritage drawn from a
                    // canon character whose top ancestry diverges from the
                    // aunt/uncle's — mirrors the subject-spouse rule above.
                    Guid? spouseId = null;
                    if (au.Age >= 22 && rng.NextDouble() >= 0.5)
                    {
                        var spGender = RandomGender(rng);
                        var spDonor  = await PickSpouseDonorAsync(db, subjectId,
                            au.GeneticAncestry.Where(kv => kv.Value >= 5.0)
                                .Select(kv => kv.Key)
                                .ToHashSet(StringComparer.OrdinalIgnoreCase),
                            ct);
                        var spouse = new ProposedCharacter(
                            Id: Guid.CreateVersion7(),
                            FirstName: pool.PickFirstName(spGender, rng) ?? "Spouse",
                            MiddleName: null,
                            LastName: pool.PickSingleChunkLastName(rng) ?? "Stone",
                            Age: Math.Max(18, au.Age + rng.Next(-10, 11)),
                            Gender: spGender,
                            Heritage: spDonor?.Heritage ?? "",
                            Role: "aunt_uncle_spouse",
                            GeneticAncestry: spDonor?.Ancestry ?? new());
                        auntUncleSpouses.Add(new AuntUncleSpouseLink(spouse, au.Id));
                        spouseId = spouse.Id;
                    }

                    // Each aunt/uncle ≥ 35 gets 0-3 cousins (children of theirs)
                    if (au.Age >= 35)
                    {
                        var cCount = rng.Next(0, 4);
                        for (int j = 0; j < cCount; j++)
                        {
                            var cgender = RandomGender(rng);
                            var minGap = 18;
                            var maxGap = Math.Max(minGap + 1, au.Age - 1);
                            var cousin = new ProposedCharacter(
                                Id: Guid.CreateVersion7(),
                                FirstName: pool.PickFirstName(cgender, rng) ?? $"Cousin{j + 1}",
                                MiddleName: null,
                                LastName: au.LastName,
                                Age: Math.Max(1, au.Age - rng.Next(minGap, maxGap + 1)),
                                Gender: cgender,
                                Heritage: au.Heritage,
                                Role: "cousin",
                                GeneticAncestry: NoisyCopy(au.GeneticAncestry, rng));
                            cousins.Add(new CousinLink(cousin, au.Id, spouseId));
                        }
                    }
                }
            }
        }

        var proposal = new FamilyProposal(
            subjectId,
            subject.Name,
            parents,
            BuildSiblings(subject, lastChunks, pool, rng),
            BuildSpouses(subject, pool, spouseDonor, rng),
            BuildChildren(subject, lastChunks, pool, rng),
            auntsUncles,
            auntUncleSpouses,
            cousins);

        log.LogInformation("Proposal for {Subject}: {N} new characters (includeCousins={IC})",
            subject.Name, proposal.Total, includeCousins);
        return proposal;
    }

    /// <summary>
    /// Persist every proposed character + family edges + run genetics
    /// propagation. Returns the new EntityIds in proposal order.
    /// </summary>
    public async Task<List<Guid>> ApplyProposalAsync(FamilyProposal proposal,
        Random? rng = null, CancellationToken ct = default)
    {
        rng ??= Random.Shared;
        var newIds = new List<Guid>();

        // 1) Write every new character record. Each new character is a
        //    minimal CharacterData — name, age, gender, heritage, blank-but-
        //    present sub-objects so the deserializer round-trips cleanly.
        foreach (var p in proposal.All)
        {
            await WriteStubCharacterAsync(p, ct);
            newIds.Add(p.Id);
        }

        // 2) Wire family edges.
        foreach (var parent in proposal.Parents)
            await family.AddParentAsync(parent.Id, proposal.SubjectId, "generator", ct: ct);
        foreach (var sibling in proposal.Siblings)
        {
            await family.AddSiblingAsync(proposal.SubjectId, sibling.Id, "generator", ct);
            // Siblings inherit subject's parents — link them too if any were proposed
            foreach (var parent in proposal.Parents)
                await family.AddParentAsync(parent.Id, sibling.Id, "generator", ct: ct);
        }
        foreach (var spouse in proposal.Spouses)
            await family.AddSpouseAsync(proposal.SubjectId, spouse.Id, "generator", ct);
        foreach (var child in proposal.Children)
        {
            await family.AddParentAsync(proposal.SubjectId, child.Id, "generator", ct: ct);
            // If a spouse was proposed, the children are theirs too
            foreach (var spouse in proposal.Spouses)
                await family.AddParentAsync(spouse.Id, child.Id, "generator", ct: ct);
        }
        // Aunts/uncles are siblings of one of subject's parents
        foreach (var au in proposal.AuntsUncles)
            await family.AddSiblingAsync(au.ParentId, au.Person.Id, "generator", ct);
        // Aunt/uncle spouses — wired as spouse_of to the matching aunt/uncle
        foreach (var aus in proposal.AuntUncleSpouses)
            await family.AddSpouseAsync(aus.AuntUncleId, aus.Person.Id, "generator", ct);
        // Cousins are children of an aunt/uncle (and optionally the aunt/uncle's spouse)
        foreach (var c in proposal.Cousins)
        {
            await family.AddParentAsync(c.AuntUncleId, c.Person.Id, "generator", ct: ct);
            if (c.AuntUncleSpouseId.HasValue)
                await family.AddParentAsync(c.AuntUncleSpouseId.Value, c.Person.Id, "generator", ct: ct);
        }

        // 3) Propagate genetics for every new descendant (siblings, children, cousins).
        //    Parents, spouses, aunts/uncles, and aunt/uncle spouses don't blend
        //    because they're roots in this sub-graph (we didn't generate THEIR parents).
        foreach (var sibling in proposal.Siblings)
            await genetics.PropagateForAsync(sibling.Id, rng, ct);
        foreach (var child in proposal.Children)
            await genetics.PropagateForAsync(child.Id, rng, ct);
        foreach (var c in proposal.Cousins)
            await genetics.PropagateForAsync(c.Person.Id, rng, ct);

        log.LogInformation("Applied family proposal for {Subject}: {N} new characters wired",
            proposal.SubjectName, proposal.Total);
        return newIds;
    }

    // ── proposal builders ────────────────────────────────────────────────

    private List<ProposedCharacter> BuildParents(CharacterData subject, List<string> lastChunks,
        NamePool pool, Random rng)
    {
        var parents = new List<ProposedCharacter>();
        if (subject.Age <= 0) return parents;

        var genders = RandomGenderPair(rng);
        var motherSurname = lastChunks.Count > 1 ? lastChunks[1] : lastChunks[0];
        var fatherSurname = lastChunks[0];
        var heritage = subject.PhysicalDescription?.Heritage ?? "";

        // Each parent contributes ~50% of subject's genome, so seed each
        // parent's genetic_ancestry as a noised copy of subject's. Genetics
        // walker will blend descendants from this baseline.
        var ancestryA = NoisyCopy(subject.GeneticAncestry, rng);
        var ancestryB = NoisyCopy(subject.GeneticAncestry, rng);

        parents.Add(new ProposedCharacter(
            Id: Guid.CreateVersion7(),
            FirstName: pool.PickFirstName(genders.A, rng) ?? "Parent",
            MiddleName: null,
            LastName: fatherSurname,
            Age: subject.Age + rng.Next(25, 41),
            Gender: genders.A,
            Heritage: heritage,
            Role: "parent",
            GeneticAncestry: ancestryA));

        parents.Add(new ProposedCharacter(
            Id: Guid.CreateVersion7(),
            FirstName: pool.PickFirstName(genders.B, rng) ?? "Parent",
            MiddleName: null,
            LastName: motherSurname,
            Age: subject.Age + rng.Next(25, 41),
            Gender: genders.B,
            Heritage: heritage,
            Role: "parent",
            GeneticAncestry: ancestryB));

        return parents;
    }

    private List<ProposedCharacter> BuildSiblings(CharacterData subject, List<string> lastChunks,
        NamePool pool, Random rng)
    {
        var siblings = new List<ProposedCharacter>();
        var count    = rng.Next(0, 4);   // 0-3 siblings
        var surname  = string.Join("-", lastChunks);
        for (int i = 0; i < count; i++)
        {
            var gender = RandomGender(rng);
            siblings.Add(new ProposedCharacter(
                Id: Guid.CreateVersion7(),
                FirstName: pool.PickFirstName(gender, rng) ?? $"Sibling{i + 1}",
                MiddleName: null,
                LastName: surname,
                Age: Math.Max(1, subject.Age + rng.Next(-5, 6)),
                Gender: gender,
                Heritage: subject.PhysicalDescription?.Heritage ?? "",
                Role: "sibling",
                // Sibling ancestry will be re-blended on apply via
                // GeneticsInheritanceService once parent edges land. This is
                // just a placeholder that's already close to subject's so
                // mid-pipeline reads get a reasonable answer.
                GeneticAncestry: NoisyCopy(subject.GeneticAncestry, rng)));
        }
        return siblings;
    }

    private List<ProposedCharacter> BuildSpouses(CharacterData subject, NamePool pool,
        SpouseDonor? donor, Random rng)
    {
        var spouses = new List<ProposedCharacter>();
        if (subject.Age < 22) return spouses;
        // 50% chance of having a current spouse — keeps the cast from
        // ballooning by always pairing every adult.
        if (rng.NextDouble() < 0.5) return spouses;
        var gender  = RandomGender(rng);
        var surname = pool.PickSingleChunkLastName(rng) ?? "Stone";
        spouses.Add(new ProposedCharacter(
            Id: Guid.CreateVersion7(),
            FirstName: pool.PickFirstName(gender, rng) ?? "Spouse",
            MiddleName: null,
            LastName: surname,
            Age: Math.Max(18, subject.Age + rng.Next(-10, 11)),
            Gender: gender,
            Heritage: donor?.Heritage ?? "",
            Role: "spouse",
            GeneticAncestry: donor?.Ancestry ?? new()));
        return spouses;
    }

    private List<ProposedCharacter> BuildChildren(CharacterData subject, List<string> lastChunks,
        NamePool pool, Random rng)
    {
        var children = new List<ProposedCharacter>();
        // Children gated to subject age >= 35 — under that, the math collapses
        // to mostly newborns since child age = subject - random(18, 40). Past
        // 35, the range produces realistic child ages 1..22.
        if (subject.Age < 35) return children;
        var count   = rng.Next(0, 4);   // 0-3 children
        var surname = string.Join("-", lastChunks);
        var minGap  = 18;
        var maxGap  = Math.Max(minGap + 1, subject.Age - 1);   // can't be older than 1-yr below subject
        for (int i = 0; i < count; i++)
        {
            var gender = RandomGender(rng);
            var age    = Math.Max(1, subject.Age - rng.Next(minGap, maxGap + 1));
            children.Add(new ProposedCharacter(
                Id: Guid.CreateVersion7(),
                FirstName: pool.PickFirstName(gender, rng) ?? $"Child{i + 1}",
                MiddleName: null,
                LastName: surname,
                Age: age,
                Gender: gender,
                Heritage: subject.PhysicalDescription?.Heritage ?? "",
                Role: "child",
                // Re-blended on apply; placeholder uses subject's noised copy
                GeneticAncestry: NoisyCopy(subject.GeneticAncestry, rng)));
        }
        return children;
    }

    private static Dictionary<string, double> NoisyCopy(Dictionary<string, double> src, Random rng)
    {
        var copy = new Dictionary<string, double>(src, StringComparer.OrdinalIgnoreCase);
        if (copy.Count == 0) return copy;
        // Same noise model as GeneticsInheritanceService — keeps sub-graph
        // siblings non-identical without drifting too far from subject.
        foreach (var k in copy.Keys.ToList())
        {
            var noise = (rng.NextDouble() * 2.0 - 1.0) * 5.0;
            copy[k] = Math.Max(0, copy[k] + noise);
        }
        var total = copy.Values.Sum();
        if (total > 0)
            foreach (var k in copy.Keys.ToList())
                copy[k] = Math.Round(copy[k] * 100.0 / total, 1);
        return copy;
    }

    public sealed record SpouseDonor(string Heritage, Dictionary<string, double> Ancestry);

    /// <summary>
    /// Pick a canon character whose top ancestry components don't overlap
    /// with the subject's, then steal their heritage + genetic_ancestry
    /// fields as the spouse's seed values. If no divergent canon character
    /// exists, returns null and the spouse stays heritage-blank.
    /// </summary>
    private async Task<SpouseDonor?> PickSpouseDonorAsync(ProseDbContext db,
        Guid subjectId, HashSet<string> subjectAncestries, CancellationToken ct)
    {
        var sample = await db.Records.AsNoTracking()
            .Where(r => r.Entity!.EntityType == "character"
                     && r.Entity.IsActive
                     && r.EntityId != subjectId)
            .Select(r => r.Json)
            .Take(2000)
            .ToListAsync(ct);

        var candidates = new List<SpouseDonor>();
        foreach (var json in sample)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                var heritage = doc.RootElement.TryGetProperty("physical_description", out var pd)
                    && pd.TryGetProperty("heritage", out var h)
                        ? h.GetString() ?? "" : "";
                if (!doc.RootElement.TryGetProperty("genetic_ancestry", out var ga)
                    || ga.ValueKind != JsonValueKind.Object) continue;
                var anc = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
                foreach (var p in ga.EnumerateObject())
                    if (p.Value.ValueKind == JsonValueKind.Number) anc[p.Name] = p.Value.GetDouble();
                if (anc.Count == 0) continue;
                var top = anc.OrderByDescending(kv => kv.Value).FirstOrDefault().Key ?? "";
                if (string.IsNullOrEmpty(top)) continue;
                if (subjectAncestries.Contains(top)) continue;
                candidates.Add(new SpouseDonor(heritage, anc));
            }
            catch { /* malformed records ignored */ }
        }
        if (candidates.Count == 0) return null;
        return candidates[Random.Shared.Next(candidates.Count)];
    }

    // ── name pool ────────────────────────────────────────────────────────

    private sealed class NamePool
    {
        public List<string> MaleFirsts    { get; set; } = new();
        public List<string> FemaleFirsts  { get; set; } = new();
        public List<string> NeutralFirsts { get; set; } = new();
        public List<string> LastNames     { get; set; } = new();

        public string? PickFirstName(string gender, Random rng)
        {
            var pool = gender switch
            {
                "male"   => MaleFirsts.Count   > 0 ? MaleFirsts   : NeutralFirsts,
                "female" => FemaleFirsts.Count > 0 ? FemaleFirsts : NeutralFirsts,
                _        => NeutralFirsts.Count > 0 ? NeutralFirsts : MaleFirsts.Concat(FemaleFirsts).ToList(),
            };
            return pool.Count == 0 ? null : pool[rng.Next(pool.Count)];
        }

        /// <summary>Single-chunk surnames only — no hyphens — so the spouse
        /// path doesn't reintroduce the density we just removed.</summary>
        public string? PickSingleChunkLastName(Random rng)
        {
            var single = LastNames.Where(s => !s.Contains('-')).ToList();
            return single.Count == 0 ? null : single[rng.Next(single.Count)];
        }
    }

    private async Task<NamePool> BuildNamePoolAsync(ProseDbContext db, Guid excludeId,
        HashSet<string> ancestries, CancellationToken ct)
    {
        // Pull every active character. Cheap (~1216 rows). We bucket each
        // first name by its DOMINANT gender across canon — a name is "male"
        // when ≥80% of its canon usages are male, "female" by the same rule,
        // and "neutral" otherwise. That makes the pool resilient to per-row
        // gender data-quality holes (a single mislabeled "Belinda male" no
        // longer pollutes the male bucket).
        var rows = await db.Characters.AsNoTracking()
            .Where(c => c.Id != excludeId
                     && c.FirstName != null && c.FirstName != ""
                     && c.LastName  != null)
            .Select(c => new { c.FirstName, c.LastName, c.Gender })
            .ToListAsync(ct);

        var byName = new Dictionary<string, (int Male, int Female, int Other)>(StringComparer.OrdinalIgnoreCase);
        var lastNameSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var r in rows)
        {
            if (!byName.TryGetValue(r.FirstName, out var c)) c = (0, 0, 0);
            switch ((r.Gender ?? "").ToLowerInvariant())
            {
                case "male":   c = (c.Male + 1, c.Female,     c.Other); break;
                case "female": c = (c.Male,     c.Female + 1, c.Other); break;
                default:       c = (c.Male,     c.Female,     c.Other + 1); break;
            }
            byName[r.FirstName] = c;
            if (!string.IsNullOrWhiteSpace(r.LastName)) lastNameSet.Add(r.LastName);
        }

        var pool = new NamePool();
        foreach (var (name, counts) in byName)
        {
            var total = counts.Male + counts.Female + counts.Other;
            if (total == 0) continue;
            var maleRatio   = counts.Male   / (double)total;
            var femaleRatio = counts.Female / (double)total;
            if (maleRatio   >= 0.8) pool.MaleFirsts.Add(name);
            else if (femaleRatio >= 0.8) pool.FemaleFirsts.Add(name);
            else pool.NeutralFirsts.Add(name);
        }
        pool.LastNames = lastNameSet.ToList();
        return pool;
    }

    // ── apply: write a stub character ────────────────────────────────────

    private async Task WriteStubCharacterAsync(ProposedCharacter p, CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var fullName = string.IsNullOrWhiteSpace(p.MiddleName)
            ? $"{p.FirstName} {p.LastName}"
            : $"{p.FirstName} {p.MiddleName} {p.LastName}";

        var data = new CharacterData
        {
            Id = p.Id.ToString("N"),
            Type = "character",
            Name = fullName,
            Gender = p.Gender,
            Pronouns = p.Gender switch { "male" => "he/him", "female" => "she/her", _ => "they/them" },
            Age = p.Age,
            Status = "alive",
            Description = $"Generated family member ({p.Role}).",
            PhysicalDescription = new PhysicalDescription { Heritage = p.Heritage },
            GeneticAncestry = new Dictionary<string, double>(p.GeneticAncestry, StringComparer.OrdinalIgnoreCase),
        };
        var json = JsonSerializer.Serialize(data, JsonOpts);

        // Slug + collision check (mirror EfRepository's approach)
        var plainSlug = Prose.Core.Services.WorldGraphService.Slugify(fullName);
        var slug = plainSlug;
        var collision = await db.Entities.AnyAsync(e =>
            e.EntityType == "character" && e.Slug == slug && e.Id != p.Id, ct);
        if (collision) slug = $"{plainSlug}_{p.Id:N}";

        db.Entities.Add(new Data.Entities.Entity
        {
            Id          = p.Id,
            EntityType  = "character",
            Name        = fullName,
            Slug        = slug,
            Status      = "canon",
            CreatedAt   = DateTime.UtcNow,
            ModifiedAt  = DateTime.UtcNow,
            IsActive    = true,
        });
        db.Records.Add(new Data.Entities.Record
        {
            EntityId  = p.Id,
            Json      = json,
            UpdatedAt = DateTime.UtcNow,
        });
        db.Characters.Add(new Data.Entities.Character
        {
            Id         = p.Id,
            Name       = fullName,
            FirstName  = p.FirstName,
            MiddleName = p.MiddleName,
            LastName   = p.LastName,
            Gender     = p.Gender,
            Age        = p.Age,
            Heritage   = p.Heritage,
        });
        await db.SaveChangesAsync(ct);

        // Slug is a denormalized column on Characters not exposed via the EF
        // model; mirror the rename script's pattern and stamp it via raw SQL.
        await db.Database.ExecuteSqlRawAsync(
            "UPDATE Characters SET Slug = {0} WHERE Id = {1}", slug, p.Id);
    }

    // ── helpers ──────────────────────────────────────────────────────────

    private static List<string> SplitLastName(string lastName) =>
        string.IsNullOrWhiteSpace(lastName)
            ? new List<string>()
            : lastName.Split('-').Where(s => !string.IsNullOrWhiteSpace(s)).ToList();

    private static string ExtractLastNameFromFullName(string fullName)
    {
        var parts = (fullName ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length > 0 ? parts[^1] : "";
    }

    private static string RandomGender(Random rng) =>
        rng.NextDouble() < 0.5 ? "male" : "female";

    private static (string A, string B) RandomGenderPair(Random rng)
    {
        // 80% mixed, 20% same-gender — keeps the cast feeling like the real
        // demographic mix the project's worldbuilding implies, without
        // mechanically forcing one of each.
        if (rng.NextDouble() < 0.8) return ("male", "female");
        return rng.NextDouble() < 0.5 ? ("male", "male") : ("female", "female");
    }
}

internal static class PhysicalDescriptionExtensions
{
    /// <summary>
    /// Defensive: <c>PhysicalDescription</c> doesn't expose a separate
    /// LastName, so this just returns empty so the caller falls through to
    /// extracting from the full <c>Name</c>. Extension point in case a
    /// future schema split adds a typed last-name field.
    /// </summary>
    public static string LastNameOrEmpty(this Models.Canon.PhysicalDescription _) => "";
}
