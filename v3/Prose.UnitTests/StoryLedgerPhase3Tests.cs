using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Services;
using Prose.Core.Services.Audit;
using Prose.Core.Services.WriteGate;

namespace Prose.UnitTests;

/// <summary>
/// Story Ledger Phase 3 — provenance and quarantine.
///
/// <para>Each fixture below reconstructs a defect that actually happened, not a hypothetical:
/// the seven empty-target relationship rows written onto an unrelated book's character
/// (2026-09-02), the same-name resolution that a bare <c>FirstOrDefault</c> decided at random,
/// and new canon landing in whichever universe a caller happened to inherit. The standard is the
/// write-gate project's own: feed the known-bad input and confirm it is now REJECTED where it
/// previously silently succeeded.</para>
/// </summary>
[TestFixture]
public class StoryLedgerPhase3Tests
{
    private SqliteConnection conn = null!;
    private DbContextOptions<ProseDbContext> options = null!;
    private IReadOnlyList<IWriteGateSyncCheck> previousChecks = null!;
    private IUniverseContext? previousUniverse;

    [SetUp]
    public void SetUp()
    {
        conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        options = new DbContextOptionsBuilder<ProseDbContext>().UseSqlite(conn).Options;
        using var db = new ProseDbContext(options);
        db.Database.EnsureCreated();

        // Process-wide ambient statics — saved and restored so nothing bleeds between tests.
        previousChecks = WriteGateScope.SyncChecks;
        previousUniverse = UniverseScope.Current;
    }

    [TearDown]
    public void TearDown()
    {
        WriteGateScope.SyncChecks = previousChecks;
        UniverseScope.Current = previousUniverse;
        conn.Dispose();
    }

    private ProseDbContext Db() => new(options);

    private Guid SeedCharacter(string name, Guid universeId, Guid? originNodeId = null)
    {
        using var db = Db();
        var id = Guid.CreateVersion7();
        db.Entities.Add(new Entity
        {
            Id = id, UniverseId = universeId, EntityType = "character",
            Name = name, Slug = name.ToLowerInvariant().Replace(' ', '-') + "-" + id.ToString("N"),
            OriginNodeId = originNodeId,
        });
        db.Characters.Add(new Character { Id = id, Name = name });
        db.SaveChanges();
        return id;
    }

    private Guid SeedBook(Guid universeId)
    {
        using var db = Db();
        var id = Guid.CreateVersion7();
        db.Nodes.Add(new BookNode
        {
            Id = id, UniverseId = universeId,
            Title = "Book " + id.ToString("N")[..8], Slug = "book-" + id.ToString("N"),
        });
        db.SaveChanges();
        return id;
    }

    // ═══ CharacterRelationshipTargetCheck ═══════════════════════════════════
    // The WriteSubject that was declared and deliberately left unrouted "until a concrete problem
    // surfaces". These are that problem, in test form.

    [Test]
    public void RelationshipCheck_RejectsEmptyTarget_TheSeoJisunFingerprint()
    {
        WriteGateScope.SyncChecks = new IWriteGateSyncCheck[] { new CharacterRelationshipTargetCheck() };
        var universeId = Guid.CreateVersion7();
        var seoJisun = SeedCharacter("Seo Jisun", universeId);

        using var db = Db();
        // The exact shape CanonGroundingService's " of "-split parser produced: no target, and the
        // raw claim sentence duplicated into Type and Description.
        db.CharacterRelationships.Add(new CharacterRelationshipRow
        {
            CharacterId = seoJisun, TargetName = "",
            Type = "gave Kyle his katana", Description = "gave Kyle his katana",
        });

        var ex = Assert.Throws<WriteGateRejectedException>(() => db.SaveChanges());
        Assert.That(ex!.Message, Does.Contain("empty TargetName"));

        using var verify = Db();
        Assert.That(verify.CharacterRelationships.Count(r => r.CharacterId == seoJisun), Is.EqualTo(0),
            "a PRE-save rejection must leave nothing behind");
    }

    [Test]
    public void RelationshipCheck_RejectsWhitespaceOnlyTarget()
    {
        WriteGateScope.SyncChecks = new IWriteGateSyncCheck[] { new CharacterRelationshipTargetCheck() };
        var universeId = Guid.CreateVersion7();
        var who = SeedCharacter("Bear", universeId);

        using var db = Db();
        db.CharacterRelationships.Add(new CharacterRelationshipRow
        {
            CharacterId = who, TargetName = "   ", Type = "his funeral", Description = "his funeral",
        });

        Assert.Throws<WriteGateRejectedException>(() => db.SaveChanges());
    }

    [Test]
    public void RelationshipCheck_RejectsTargetScopedToADifferentBook()
    {
        WriteGateScope.SyncChecks = new IWriteGateSyncCheck[] { new CharacterRelationshipTargetCheck() };
        var universeId = Guid.CreateVersion7();
        var testament = SeedBook(universeId);
        var bcoda = SeedBook(universeId);
        var seoJisun = SeedCharacter("Seo Jisun", universeId, originNodeId: testament);
        var bookScopedKyle = SeedCharacter("Kyle Ellen Corbin", universeId, originNodeId: bcoda);

        using var db = Db();
        db.CharacterRelationships.Add(new CharacterRelationshipRow
        {
            CharacterId = seoJisun, TargetName = "Kyle Ellen Corbin", TargetEntityId = bookScopedKyle,
            Type = "mentor", Description = "gave him the katana",
        });

        var ex = Assert.Throws<WriteGateRejectedException>(() => db.SaveChanges());
        Assert.That(ex!.Message, Does.Contain("different books"));
    }

    [Test]
    public void RelationshipCheck_AllowsAUniverseWideTarget()
    {
        // GLMZ's Kyle is universe-wide (OriginNodeId null) by design, referenced by every book.
        // This is the common legitimate case and must never be mistaken for contamination.
        WriteGateScope.SyncChecks = new IWriteGateSyncCheck[] { new CharacterRelationshipTargetCheck() };
        var universeId = Guid.CreateVersion7();
        var testament = SeedBook(universeId);
        var bookScoped = SeedCharacter("Seo Jisun", universeId, originNodeId: testament);
        var sharedKyle = SeedCharacter("Kyle Ellen Corbin", universeId, originNodeId: null);

        using var db = Db();
        db.CharacterRelationships.Add(new CharacterRelationshipRow
        {
            CharacterId = bookScoped, TargetName = "Kyle Ellen Corbin", TargetEntityId = sharedKyle,
            Type = "client", Description = "hired him once",
        });
        db.SaveChanges();

        using var verify = Db();
        Assert.That(verify.CharacterRelationships.Count(r => r.CharacterId == bookScoped), Is.EqualTo(1));
    }

    [Test]
    public void RelationshipCheck_AllowsBothEndsInTheSameBook()
    {
        WriteGateScope.SyncChecks = new IWriteGateSyncCheck[] { new CharacterRelationshipTargetCheck() };
        var universeId = Guid.CreateVersion7();
        var book = SeedBook(universeId);
        var a = SeedCharacter("Faith Larson", universeId, originNodeId: book);
        var b = SeedCharacter("Ethan Wolfe", universeId, originNodeId: book);

        using var db = Db();
        db.CharacterRelationships.Add(new CharacterRelationshipRow
        {
            CharacterId = a, TargetName = "Ethan Wolfe", TargetEntityId = b,
            Type = "bandmate", Description = "plays in Fenris with him",
        });
        db.SaveChanges();

        using var verify = Db();
        Assert.That(verify.CharacterRelationships.Count(r => r.CharacterId == a), Is.EqualTo(1));
    }

    [Test]
    public void RelationshipCheck_AllowsAnUnresolvedButNamedTarget()
    {
        // The documented deviation from the Phase 3 spec, pinned by a test so it cannot be
        // "tidied up" later into a rejection: CLAUDE.md's Stage 2 gate permits an intentional
        // off-page reference, the table carries no field distinguishing that from an unseeded
        // target, and CharacterMapper reinserts EVERY row on every character Save — so rejecting
        // this would make an unrelated edit to any character with a legacy unresolved row fail.
        // These are reported by --entity-relationships --orphans and CHAR-REL-UNRESOLVED instead.
        WriteGateScope.SyncChecks = new IWriteGateSyncCheck[] { new CharacterRelationshipTargetCheck() };
        var universeId = Guid.CreateVersion7();
        var who = SeedCharacter("Kyle Ellen Corbin", universeId);

        using var db = Db();
        db.CharacterRelationships.Add(new CharacterRelationshipRow
        {
            CharacterId = who, TargetName = "an aunt never otherwise named", TargetEntityId = null,
            Type = "aunt", Description = "sent him money once",
        });
        db.SaveChanges();

        using var verify = Db();
        Assert.That(verify.CharacterRelationships.Count(r => r.CharacterId == who), Is.EqualTo(1));
    }

    // ═══ UnscopedUniverseWriteCheck ═════════════════════════════════════════

    [Test]
    public void UnscopedUniverseWrite_RejectsNewEntityWhenTheUniverseWasOnlyInherited()
    {
        WriteGateScope.SyncChecks = new IWriteGateSyncCheck[] { new UnscopedUniverseWriteCheck() };
        UniverseScope.Current = new InheritedScopeContext(Guid.CreateVersion7());

        using var db = Db();
        // No UniverseId set by the caller — this is what an MCP create_character does today, and
        // the row would be stamped with whatever the Hub process happened to default to.
        db.Entities.Add(new Entity
        {
            Id = Guid.CreateVersion7(), EntityType = "character",
            Name = "Someone New", Slug = "someone-new",
        });

        var ex = Assert.Throws<WriteGateRejectedException>(() => db.SaveChanges());
        Assert.That(ex!.Message, Does.Contain("inherited from the persisted default"));
        Assert.That(ex.Message, Does.Contain("switch_universe"), "the message must name the fix");
    }

    [Test]
    public void UnscopedUniverseWrite_RejectsNewNodeToo()
    {
        WriteGateScope.SyncChecks = new IWriteGateSyncCheck[] { new UnscopedUniverseWriteCheck() };
        UniverseScope.Current = new InheritedScopeContext(Guid.CreateVersion7());

        using var db = Db();
        db.Nodes.Add(new BookNode { Id = Guid.CreateVersion7(), Title = "Untitled", Slug = "untitled" });

        Assert.Throws<WriteGateRejectedException>(() => db.SaveChanges());
    }

    [Test]
    public void UnscopedUniverseWrite_AllowsCodeThatSetsUniverseIdItself()
    {
        // The interchange importer and --restore-entity do exactly this: they know which universe
        // the row belongs to and say so. They must keep working under an inherited ambient scope.
        WriteGateScope.SyncChecks = new IWriteGateSyncCheck[] { new UnscopedUniverseWriteCheck() };
        var inherited = Guid.CreateVersion7();
        var deliberate = Guid.CreateVersion7();
        UniverseScope.Current = new InheritedScopeContext(inherited);

        using var db = Db();
        var id = Guid.CreateVersion7();
        db.Entities.Add(new Entity
        {
            Id = id, UniverseId = deliberate, EntityType = "place",
            Name = "Port Gadriket", Slug = "port-gadriket",
        });
        db.SaveChanges();

        using var verify = Db();
        Assert.That(verify.Entities.IgnoreQueryFilters().First(e => e.Id == id).UniverseId, Is.EqualTo(deliberate),
            "the caller's explicit universe must survive, not be replaced by the ambient one");
    }

    [Test]
    public void UnscopedUniverseWrite_AllowsWhenTheCallerNamedItsUniverse()
    {
        WriteGateScope.SyncChecks = new IWriteGateSyncCheck[] { new UnscopedUniverseWriteCheck() };
        UniverseScope.Current = new ExplicitScopeContext(Guid.CreateVersion7());

        using var db = Db();
        var id = Guid.CreateVersion7();
        db.Entities.Add(new Entity { Id = id, EntityType = "character", Name = "Pixel", Slug = "pixel" });
        db.SaveChanges();

        using var verify = Db();
        Assert.That(verify.Entities.IgnoreQueryFilters().Any(e => e.Id == id), Is.True);
    }

    [Test]
    public void UnscopedUniverseWrite_IsANoOpWhenUniverseScopingIsInactive()
    {
        // Guid.Empty means no universes are configured at all (tests, a fresh DB, design-time).
        // There is no wrong universe to land in, so there is nothing to enforce — and gating here
        // would break every test in this suite and every fresh-machine bootstrap.
        WriteGateScope.SyncChecks = new IWriteGateSyncCheck[] { new UnscopedUniverseWriteCheck() };
        UniverseScope.Current = null;

        using var db = Db();
        var id = Guid.CreateVersion7();
        db.Entities.Add(new Entity { Id = id, EntityType = "weapon", Name = "Cacophony", Slug = "cacophony" });
        db.SaveChanges();

        using var verify = Db();
        Assert.That(verify.Entities.IgnoreQueryFilters().Any(e => e.Id == id), Is.True);
    }

    [Test]
    public void UnscopedUniverseWrite_DoesNotGateModificationsOfExistingRows()
    {
        // --merge-entity, --archive-book, --tag-entities and every other cross-universe repair
        // command MODIFY rows whose UniverseId is their own. Only ambient-stamped INSERTS are the
        // hazard; gating updates would break the legitimately universe-agnostic commands.
        var universeId = Guid.CreateVersion7();
        var id = SeedCharacter("Yuki", universeId);

        WriteGateScope.SyncChecks = new IWriteGateSyncCheck[] { new UnscopedUniverseWriteCheck() };
        UniverseScope.Current = new InheritedScopeContext(universeId);

        using var db = Db();
        var entity = db.Entities.First(e => e.Id == id);
        entity.Description = "edited under an inherited scope";
        db.SaveChanges();

        using var verify = Db();
        Assert.That(verify.Entities.IgnoreQueryFilters().First(e => e.Id == id).Description,
            Is.EqualTo("edited under an inherited scope"));
    }

    // ═══ EntityResolver book-scope tiebreaker ═══════════════════════════════

    [Test]
    public void Resolver_SingleMatchStillResolves()
    {
        var universeId = Guid.CreateVersion7();
        var id = SeedCharacter("Maeve", universeId);

        using var db = Db();
        Assert.That(EntityResolver.ResolveEntityIdAny(db, "Maeve"), Is.EqualTo(id));
    }

    [Test]
    public void Resolver_BookScopedCandidateWinsForItsOwnBook()
    {
        // The Farai case EntityMentionScanner was already hardened against: two same-named
        // entities in one universe, one scoped to the book doing the asking.
        var universeId = Guid.CreateVersion7();
        var thisBook = SeedBook(universeId);
        var otherBook = SeedBook(universeId);
        var mine = SeedCharacter("Farai", universeId, originNodeId: thisBook);
        SeedCharacter("Farai", universeId, originNodeId: otherBook);

        using var db = Db();
        Assert.That(EntityResolver.ResolveEntityIdAny(db, "Farai", thisBook), Is.EqualTo(mine));
    }

    [Test]
    public void Resolver_FallsBackToTheUniverseWideCandidate()
    {
        var universeId = Guid.CreateVersion7();
        var thisBook = SeedBook(universeId);
        var otherBook = SeedBook(universeId);
        var shared = SeedCharacter("Raphael", universeId, originNodeId: null);
        SeedCharacter("Raphael", universeId, originNodeId: otherBook);

        using var db = Db();
        Assert.That(EntityResolver.ResolveEntityIdAny(db, "Raphael", thisBook), Is.EqualTo(shared),
            "no book-scoped contender for THIS book, so the shared entity is the only honest answer");
    }

    [Test]
    public void Resolver_ReturnsNullWhenTheCollisionCannotBeBroken()
    {
        // The behaviour change that matters: this used to return whichever row the query
        // enumerated first. Two universe-wide claimants on one name is a duplicate-entity defect,
        // and picking one at random is how a relationship ends up on the wrong character.
        var universeId = Guid.CreateVersion7();
        SeedCharacter("Kofi", universeId, originNodeId: null);
        SeedCharacter("Kofi", universeId, originNodeId: null);

        using var db = Db();
        Assert.That(EntityResolver.ResolveEntityIdAny(db, "Kofi"), Is.Null);
    }

    [Test]
    public void Resolver_ReturnsNullWhenTwoBookScopedCandidatesShareTheAskingBook()
    {
        var universeId = Guid.CreateVersion7();
        var book = SeedBook(universeId);
        SeedCharacter("Wren", universeId, originNodeId: book);
        SeedCharacter("Wren", universeId, originNodeId: book);

        using var db = Db();
        Assert.That(EntityResolver.ResolveEntityIdAny(db, "Wren", book), Is.Null);
    }

    [Test]
    public void Resolver_ResolvesAnAliasAndRespectsBookScope()
    {
        var universeId = Guid.CreateVersion7();
        var thisBook = SeedBook(universeId);
        var otherBook = SeedBook(universeId);
        var mine = SeedCharacter("Kyle Ellen Corbin", universeId, originNodeId: thisBook);
        var theirs = SeedCharacter("Kyle Somebody Else", universeId, originNodeId: otherBook);

        using (var seed = Db())
        {
            seed.CharacterAliases.Add(new CharacterAlias { CharacterId = mine, Value = "Kyle" });
            seed.CharacterAliases.Add(new CharacterAlias { CharacterId = theirs, Value = "Kyle" });
            seed.SaveChanges();
        }

        using var db = Db();
        Assert.That(EntityResolver.ResolveEntityIdAny(db, "Kyle", thisBook), Is.EqualTo(mine));
        Assert.That(EntityResolver.ResolveEntityIdAny(db, "Kyle", otherBook), Is.EqualTo(theirs));
        Assert.That(EntityResolver.ResolveEntityIdAny(db, "Kyle"), Is.Null,
            "with no asking book, an alias claimed by two entities is genuinely ambiguous");
    }

    // ═══ Provenance grades and the audit ════════════════════════════════════

    [Test]
    public void ClaimProvenance_IsValid_AcceptsEveryGradeAndRefusesTypos()
    {
        Assert.Multiple(() =>
        {
            foreach (var g in ClaimProvenance.All)
                Assert.That(ClaimProvenance.IsValid(g), Is.True, g);
            Assert.That(ClaimProvenance.IsValid("Authored"), Is.False, "grades are case-sensitive constants");
            Assert.That(ClaimProvenance.IsValid("approved"), Is.False);
            Assert.That(ClaimProvenance.IsValid(null), Is.False);
            Assert.That(ClaimProvenance.IsValid(""), Is.False);
        });
    }

    [Test]
    public void NewRowsDefaultToInferredNotAuthored()
    {
        // "inferred" is the honest default: believable, never authoritative. A default of
        // "authored" would silently claim human approval for everything the engine writes.
        Assert.That(new Entity().Provenance, Is.EqualTo(ClaimProvenance.Inferred));
        Assert.That(new CharacterRelationshipRow().Provenance, Is.EqualTo(ClaimProvenance.Inferred));
        Assert.That(new Prose.Core.Models.Canon.CharacterRelationship().Provenance, Is.EqualTo(ClaimProvenance.Inferred));
    }

    [Test]
    public void ScaffoldedIsNotTrustworthy()
    {
        Assert.Multiple(() =>
        {
            Assert.That(ClaimProvenance.IsTrustworthy(ClaimProvenance.Authored), Is.True);
            Assert.That(ClaimProvenance.IsTrustworthy(ClaimProvenance.Observed), Is.True);
            Assert.That(ClaimProvenance.IsTrustworthy(ClaimProvenance.Inferred), Is.False);
            Assert.That(ClaimProvenance.IsTrustworthy(ClaimProvenance.Scaffolded), Is.False,
                "scaffolded is never canon — that is the whole point of the grade");
            Assert.That(ClaimProvenance.IsTrustworthy(ClaimProvenance.LegacyUnknown), Is.False);
        });
    }

    [Test]
    public async Task ProvenanceAudit_CountsByGradeAndTotalsTheUnapproved()
    {
        var universeId = Guid.CreateVersion7();
        var a = SeedCharacter("Approved Person", universeId);
        SeedCharacter("Scaffolded Person", universeId);
        SeedCharacter("Another Scaffolded", universeId);

        var svc = Provenance();
        await svc.SetEntityProvenanceAsync(a, ClaimProvenance.Authored);
        foreach (var id in AllEntityIdsExcept(a))
            await svc.SetEntityProvenanceAsync(id, ClaimProvenance.Scaffolded);

        var report = await svc.AuditAsync();

        var entityCounts = report.Counts.Where(c => c.Table == "Entities").ToDictionary(c => c.Grade, c => c.Count);
        Assert.Multiple(() =>
        {
            Assert.That(entityCounts[ClaimProvenance.Authored], Is.EqualTo(1));
            Assert.That(entityCounts[ClaimProvenance.Scaffolded], Is.EqualTo(2));
            Assert.That(report.UnapprovedRows, Is.EqualTo(2), "only authored/observed count as approved");
            Assert.That(report.Samples.Any(s => s.Label.Contains("Scaffolded Person")), Is.True);
            Assert.That(report.Samples.Any(s => s.Label.Contains("Approved Person")), Is.False,
                "an approved row is not a finding");
        });
    }

    [Test]
    public async Task ProvenanceAudit_ScopedToABookCountsOnlyThatBooksOwnEntities()
    {
        var universeId = Guid.CreateVersion7();
        var book = SeedBook(universeId);
        SeedCharacter("Book Scoped", universeId, originNodeId: book);
        SeedCharacter("Universe Wide", universeId, originNodeId: null);

        var report = await Provenance().AuditAsync(bookNodeId: book);

        var total = report.Counts.Where(c => c.Table == "Entities").Sum(c => c.Count);
        Assert.That(total, Is.EqualTo(1),
            "a universe-wide entity belongs to every book and must not be attributed to one");
    }

    [Test]
    public async Task SetEntityProvenance_RefusesAnUnknownGradeAndReportsAMissingRow()
    {
        var universeId = Guid.CreateVersion7();
        var id = SeedCharacter("Sumi", universeId);
        var svc = Provenance();

        Assert.ThrowsAsync<ArgumentException>(() => svc.SetEntityProvenanceAsync(id, "approved"));
        Assert.That(await svc.SetEntityProvenanceAsync(Guid.CreateVersion7(), ClaimProvenance.Authored), Is.False);
        Assert.That(await svc.SetEntityProvenanceAsync(id, ClaimProvenance.Authored), Is.True);

        using var verify = Db();
        Assert.That(verify.Entities.IgnoreQueryFilters().First(e => e.Id == id).Provenance,
            Is.EqualTo(ClaimProvenance.Authored));
    }

    [Test]
    public async Task SetRelationshipProvenance_PromotesOneRow()
    {
        var universeId = Guid.CreateVersion7();
        var who = SeedCharacter("Shroud", universeId);
        long rowId;
        using (var seed = Db())
        {
            var row = new CharacterRelationshipRow
            {
                CharacterId = who, TargetName = "Doc Stash", Type = "fixer",
                Provenance = ClaimProvenance.Scaffolded,
            };
            seed.CharacterRelationships.Add(row);
            seed.SaveChanges();
            rowId = row.Id;
        }

        Assert.That(await Provenance().SetRelationshipProvenanceAsync(rowId, ClaimProvenance.Authored), Is.True);

        using var verify = Db();
        Assert.That(verify.CharacterRelationships.First(r => r.Id == rowId).Provenance,
            Is.EqualTo(ClaimProvenance.Authored));
    }

    [Test]
    public async Task SetClaimProvenance_PromotesOneLedgerClaim()
    {
        using (var seed = Db())
        {
            seed.ContinuityClaims.Add(new ContinuityClaim
            {
                ClaimUid = "claim-1", EntityId = "e1", EntityName = "Kyle", EntityKind = "character",
                Predicate = "origin", Object = "constructed, no prior life",
                SourceType = "chapter", Status = "CONFIRMED",
                Provenance = ClaimProvenance.Inferred,
            });
            seed.SaveChanges();
        }

        var svc = Provenance();
        Assert.That(await svc.SetClaimProvenanceAsync("claim-1", ClaimProvenance.Authored), Is.True);
        Assert.That(await svc.SetClaimProvenanceAsync("nope", ClaimProvenance.Authored), Is.False);

        using var verify = Db();
        Assert.That(verify.ContinuityClaims.First(c => c.ClaimUid == "claim-1").Provenance,
            Is.EqualTo(ClaimProvenance.Authored));
    }

    [Test]
    public void RelationshipGradeSurvivesTheMapperRoundTrip()
    {
        // CharacterMapper.PersistAsync deletes and reinserts EVERY relationship row on each Save.
        // A grade that lived only on the row would be reset to the default by the next unrelated
        // edit to that character — which would make the whole column meaningless within a week.
        var universeId = Guid.CreateVersion7();
        var who = SeedCharacter("Felix", universeId);
        using (var seed = Db())
        {
            seed.CharacterRelationships.Add(new CharacterRelationshipRow
            {
                CharacterId = who, TargetName = "Maeve", Type = "partner",
                Provenance = ClaimProvenance.Authored,
            });
            seed.SaveChanges();
        }

        // The read side (LoadOne → Materialize) must carry the grade out to the DTO, or the
        // write side has nothing to carry back in.
        using var db = Db();
        var data = CharacterMapper.LoadOne(db, who);
        Assert.That(data, Is.Not.Null);
        Assert.That(data!.Relationships, Has.Count.EqualTo(1));
        Assert.That(data.Relationships[0].Provenance, Is.EqualTo(ClaimProvenance.Authored),
            "the read side must carry the grade out");
    }

    // ═══ Ledger free-text search ════════════════════════════════════════════
    // The instrument the author's family purge needed and the ledger did not have. Every read was
    // by entity id, by status, or by applied-ness — so a fact recorded in the OBJECT string was
    // unfindable, which is how four claims survived Phase 2's father_* predicate sweep.

    private ContinuityService Continuity() => new(new SingleContextFactory(options));

    private void SeedClaim(string uid, string entityId, string entityName, string predicate, string obj, string status = "NEW")
    {
        using var db = Db();
        db.ContinuityClaims.Add(new ContinuityClaim
        {
            ClaimUid = uid, EntityId = entityId, EntityName = entityName, EntityKind = "character",
            Predicate = predicate, Object = obj, SourceType = "chapter", Status = status,
        });
        db.SaveChanges();
    }

    [Test]
    public void LedgerSearch_FindsAFactHiddenInTheObjectString()
    {
        // The exact defect: a father fact recorded under a sword predicate. No predicate-name
        // search can reach it, and this is why the "purged" fabrication kept resurfacing.
        SeedClaim("c1", "kyle", "Kyle Ellen Corbin", "second_sword_possession",
            "old sword wrapped in oilcloth, made by father", "CONFIRMED");
        SeedClaim("c2", "kyle", "Kyle Ellen Corbin", "second_sword_tsuka", "wrapped in dark cord");

        var hits = Continuity().Search("father");

        Assert.That(hits, Has.Count.EqualTo(1));
        Assert.That(hits[0].ClaimUid, Is.EqualTo("c1"), "the uid is what makes the hit actionable");
    }

    [Test]
    public void LedgerSearch_MatchesEntityNameAndPredicateToo()
    {
        SeedClaim("c1", "kyle", "Kyle Ellen Corbin", "father_name", "Dae-jung Seo");
        SeedClaim("c2", "chen", "Mrs. Chen", "role", "noodle shop owner");

        Assert.Multiple(() =>
        {
            Assert.That(Continuity().Search("Dae-jung").Select(c => c.ClaimUid), Is.EqualTo(new[] { "c1" }));
            Assert.That(Continuity().Search("father_name").Select(c => c.ClaimUid), Is.EqualTo(new[] { "c1" }));
            Assert.That(Continuity().Search("Mrs. Chen").Select(c => c.ClaimUid), Is.EqualTo(new[] { "c2" }));
        });
    }

    [Test]
    public void LedgerSearch_ShowsRejectedRowsByDefaultSoAPurgeCanBeVerified()
    {
        SeedClaim("c1", "kyle", "Kyle Ellen Corbin", "father_name", "Dae-jung Seo", "REJECTED");

        Assert.That(Continuity().Search("Dae-jung"), Has.Count.EqualTo(1),
            "confirming a purge landed means being able to see the rejected row");
        Assert.That(Continuity().Search("Dae-jung", liveOnly: true), Is.Empty,
            "--live is what answers 'is it still asserted anywhere?'");
    }

    [Test]
    public void LedgerSearch_ScopesToOneEntityAndOnePredicateFamily()
    {
        SeedClaim("c1", "kyle", "Kyle Ellen Corbin", "father_name", "Dae-jung Seo");
        SeedClaim("c2", "celeste", "Celeste Hartley", "father_name", "Douglas Hartley");

        Assert.Multiple(() =>
        {
            Assert.That(Continuity().Search("father", entityId: "kyle").Select(c => c.ClaimUid),
                Is.EqualTo(new[] { "c1" }), "another character's real father must not be swept up");
            Assert.That(Continuity().Search("Hartley", predicatePrefix: "father").Select(c => c.ClaimUid),
                Is.EqualTo(new[] { "c2" }));
            Assert.That(Continuity().Search("father"), Has.Count.EqualTo(2));
        });
    }

    [Test]
    public void LedgerSearch_IsCaseInsensitiveAndRefusesAnEmptyNeedle()
    {
        SeedClaim("c1", "kyle", "Kyle Ellen Corbin", "father_name", "Dae-jung Seo");

        Assert.That(Continuity().Search("DAE-JUNG"), Has.Count.EqualTo(1));
        Assert.That(Continuity().Search("   "), Is.Empty,
            "an empty needle must not degenerate into 'match everything'");
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private ProvenanceService Provenance() =>
        new(new SingleContextFactory(options), NullLogger<ProvenanceService>.Instance);

    private List<Guid> AllEntityIdsExcept(Guid exclude)
    {
        using var db = Db();
        return db.Entities.IgnoreQueryFilters().Where(e => e.Id != exclude).Select(e => e.Id).ToList();
    }

    /// <summary>Hands out contexts over the one in-memory SQLite connection this fixture owns.</summary>
    private sealed class SingleContextFactory(DbContextOptions<ProseDbContext> options)
        : IDbContextFactory<ProseDbContext>
    {
        public ProseDbContext CreateDbContext() => new(options);
    }

    /// <summary>
    /// A universe context whose scope came from the persisted default — nobody named it. This is
    /// the live Hub's state for any MCP tool call that never issued switch_universe.
    /// </summary>
    private sealed class InheritedScopeContext(Guid id) : StubUniverseContext(id)
    {
        public override bool IsExplicitlyScoped => false;
    }

    /// <summary>A universe context the caller chose (--universe / PROSE_UNIVERSE / switch_universe).</summary>
    private sealed class ExplicitScopeContext(Guid id) : StubUniverseContext(id)
    {
        public override bool IsExplicitlyScoped => true;
    }

    private abstract class StubUniverseContext(Guid id) : IUniverseContext
    {
        public Guid CurrentId { get; private set; } = id;
        public string CurrentSlug => "stub";
        public UniverseInfo? CurrentUniverse => new(CurrentId, "stub", "Stub", null, null, true, 100);
        public IReadOnlyList<UniverseInfo> ListUniverses() => Array.Empty<UniverseInfo>();
        public bool IsGlmz => false;
        public abstract bool IsExplicitlyScoped { get; }
        public string UniverseGroundingOr(string glmzFallback) => glmzFallback;
        public void UseUniverse(Guid newId) { CurrentId = newId; UniverseScope.BumpEpoch(); }
        public bool UseUniverseBySlug(string slug) => false;
        public void SetFlowUniverse(Guid? newId) { }
        public void PersistAsDefault(Guid newId) { }
        public void Refresh() { }
    }
}
