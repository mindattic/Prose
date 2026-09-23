using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Models.Canon;
using Prose.Core.Services;
using Prose.Core.Services.Factory;

namespace Prose.UnitTests;

/// <summary>The world as the factory relies on it (RFC 0015 §3.3–3.5): saves that change nothing
/// write nothing, every character field has a write path, and a record is verified against the book
/// only after the beats that mention it have been read.</summary>
public abstract class WorldFixture : RulingFixture
{
    protected CharacterRepository characters = null!;
    protected CharacterFieldWriter writer = null!;
    protected FactorySessionService sessions = null!;
    protected EntityVerificationService verifier = null!;

    [SetUp]
    public void SetUpWorld()
    {
        characters = new CharacterRepository(dbFactory);
        writer = new CharacterFieldWriter(characters, gate, dbFactory);
        sessions = new FactorySessionService(dbFactory);
        verifier = new EntityVerificationService(dbFactory, new BookSpineService(dbFactory), gate, sessions, rulings);
    }

    protected CharacterData NewCharacter(string name = "Kyle Test", string description = "A street samurai.")
    {
        var c = new CharacterData { Id = Guid.NewGuid().ToString("N"), Name = name, Description = description, Age = 27 };
        characters.Save(c);
        return characters.GetById(c.Id)!;
    }

    protected async Task<DateTime> ModifiedAtAsync(string id)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.Entities.IgnoreQueryFilters().Where(e => e.Id == Guid.Parse(id)).Select(e => e.ModifiedAt).SingleAsync();
    }

    protected async Task BackdateAsync(string id)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var e = await db.Entities.IgnoreQueryFilters().SingleAsync(x => x.Id == Guid.Parse(id));
        e.ModifiedAt = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        await db.SaveChangesAsync();
    }

    /// <summary>Tags a beat with the character, as a tagged save leaves it.</summary>
    protected async Task TagAsync(Guid beatId, CharacterData c)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var beat = await db.Beats.SingleAsync(b => b.Id == beatId);
        beat.Text = $"<entity repo=\"character\" guid=\"{Guid.Parse(c.Id)}\">{c.Name}</entity> {beat.Text}";
        beat.TextHash = NodeWorkbenchService.ComputeTextHash(beat.Text);
        await db.SaveChangesAsync();
    }

    protected async Task ReadBookAsync(Guid book)
    {
        var ordered = await workbench.GetOrderedBeatsAsync(book);
        await gate.MarkReadAsync(book, ordered.Select(o => (o.Beat.Id, o.Beat.TextHash ?? "")), "test");
    }

    protected async Task<List<Guid>> BeatIdsAsync(Guid book) =>
        (await workbench.GetOrderedBeatsAsync(book)).Select(o => o.Beat.Id).ToList();
}

[TestFixture]
public class SaveNoOpTests : WorldFixture
{
    [Test]
    public async Task A_character_save_that_changes_nothing_leaves_ModifiedAt_alone_and_a_real_change_moves_it()
    {
        var c = NewCharacter();
        await BackdateAsync(c.Id);
        var before = await ModifiedAtAsync(c.Id);

        characters.Save(characters.GetById(c.Id)!);
        Assert.That(await ModifiedAtAsync(c.Id), Is.EqualTo(before), "an idle round trip is not a change");

        var edited = characters.GetById(c.Id)!;
        edited.Description = "A street samurai with no family.";
        characters.Save(edited);
        Assert.That(await ModifiedAtAsync(c.Id), Is.GreaterThan(before));
        Assert.That(characters.GetById(c.Id)!.Description, Is.EqualTo("A street samurai with no family."));
    }

    [Test]
    public async Task A_tags_only_change_is_a_change()
    {
        var c = NewCharacter();
        await BackdateAsync(c.Id);
        var edited = characters.GetById(c.Id)!;
        edited.Tags = ["bcoda"];
        characters.Save(edited);
        Assert.That(await ModifiedAtAsync(c.Id), Is.GreaterThan(new DateTime(2020, 1, 2)));
        Assert.That(characters.GetById(c.Id)!.Tags, Is.EqualTo(new[] { "bcoda" }));
    }

    [Test]
    public async Task A_weapon_save_that_changes_nothing_leaves_ModifiedAt_alone()
    {
        var weapons = new WeaponryRepository(dbFactory);
        var w = new WeaponryData { Id = Guid.NewGuid().ToString("N"), Name = "Silence", Description = "A katana." };
        weapons.Save(w);
        await BackdateAsync(w.Id);
        var before = await ModifiedAtAsync(w.Id);

        weapons.Save(weapons.GetById(w.Id)!);
        Assert.That(await ModifiedAtAsync(w.Id), Is.EqualTo(before));

        var edited = weapons.GetById(w.Id)!;
        edited.Description = "A katana that does not glow.";
        weapons.Save(edited);
        Assert.That(await ModifiedAtAsync(w.Id), Is.GreaterThan(before));
    }

    private static string ThisFile([CallerFilePath] string p = "") => p;

    [Test]
    public void Every_relational_repository_save_skips_a_no_op_before_it_writes()
    {
        var src = Path.Combine(Directory.GetParent(ThisFile())!.Parent!.FullName, "Prose.Core", "Services", "Repositories.cs");
        var text = File.ReadAllText(src);
        var saves = Regex.Matches(text, @"public override void Save\(\w+ item\)(?<body>.*?)Mapper\.PersistAsync\(db, id, item\)", RegexOptions.Singleline);
        Assert.That(saves, Has.Count.GreaterThanOrEqualTo(29));
        var missing = saves.Where(m => !m.Groups["body"].Value.Contains("SaveGuard.IsUnchanged(")).Select(m => m.Value[..60]).ToList();
        Assert.That(missing, Is.Empty, "a save that bumps ModifiedAt on a no-op un-reads every beat that mentions the entity");
    }
}

[TestFixture]
public class CharacterFieldWriterTests : WorldFixture
{
    [Test]
    public void Every_key_get_character_returns_is_writable_or_refused_with_a_reason()
    {
        var all = CharacterFieldWriter.AllKeys.ToHashSet();
        Assert.That(CharacterFieldWriter.Refused.Keys, Is.SubsetOf(all));
        Assert.That(CharacterFieldWriter.WritableKeys.Concat(CharacterFieldWriter.Refused.Keys), Is.EquivalentTo(all));
        foreach (var k in new[] { "behavioral", "timeline", "cyberware_inventory", "neural_abilities", "genetic_ancestry",
                                  "ancestry_detail", "knowledge", "conditions", "psychology", "relationships", "story_hooks",
                                  "aliases", "tags", "belongings", "operating_territory", "physical_description", "stats" })
            Assert.That(CharacterFieldWriter.WritableKeys, Does.Contain(k));
    }

    [Test]
    public async Task Deep_fields_round_trip_and_commas_are_never_split()
    {
        var c = NewCharacter();
        var fields = new JsonObject
        {
            ["story_hooks"] = new JsonArray("Mrs. Chen, who is 79, always takes his money", "The ten who came before"),
            ["behavioral"] = new JsonObject
            {
                ["decision_rules"] = new JsonArray("Will always pay Mrs. Chen"),
                ["escalation_ladder"] = new JsonArray("Watch", "Warn", "Draw Silence"),
                ["habits"] = new JsonArray("Counts exits"),
            },
            ["timeline"] = new JsonArray(new JsonObject { ["date"] = "2210", ["event"] = "Silence comes to him at sixteen", ["consequences"] = "He believes Seito gave it" }),
            ["cyberware_inventory"] = new JsonArray(new JsonObject { ["name"] = "Neural lace", ["body_location"] = "skull" }),
            ["neural_abilities"] = new JsonArray(new JsonObject { ["name"] = "Overclock", ["cost_percent"] = 20, ["passive"] = false }),
            ["genetic_ancestry"] = new JsonObject { ["East Asian"] = 50.0, ["European"] = 50.0 },
            ["knowledge"] = new JsonArray(new JsonObject { ["topic"] = "Sift", ["summary"] = "Met her on the Broken Glass night" }),
            ["age"] = 27,
        };
        var r = await writer.SetFieldsAsync(c.Id, fields.ToJsonString());
        Assert.That(r.Ok, Is.True, r.Error);
        Assert.That(r.NotLanded, Is.Empty);
        Assert.That(r.Changed, Is.SupersetOf(new[] { "story_hooks", "behavioral", "timeline", "cyberware_inventory", "neural_abilities", "genetic_ancestry", "knowledge" }));

        var back = characters.GetById(c.Id)!;
        Assert.That(back.StoryHooks, Has.Count.EqualTo(2));
        Assert.That(back.StoryHooks[0], Does.Contain("who is 79, always"));
        Assert.That(back.Behavioral.EscalationLadder, Is.EqualTo(new[] { "Watch", "Warn", "Draw Silence" }));
        Assert.That(back.Timeline.Single().Event, Is.EqualTo("Silence comes to him at sixteen"));
        Assert.That(back.CyberwareInventory.Single().BodyLocation, Is.EqualTo("skull"));
        Assert.That(back.NeuralAbilities.Single().CostPercent, Is.EqualTo(20));
        Assert.That(back.GeneticAncestry["European"], Is.EqualTo(50.0));
        Assert.That(back.Knowledge.Single().Summary, Does.Contain("Broken Glass"));
    }

    [Test]
    public async Task Objects_merge_so_changing_one_list_leaves_its_siblings_alone()
    {
        var c = NewCharacter();
        Assert.That((await writer.SetFieldsAsync(c.Id, """{"behavioral":{"habits":["Counts exits"],"decision_rules":["Always pays"]}}""")).Ok, Is.True);
        var r = await writer.SetFieldsAsync(c.Id, """{"behavioral":{"habits":["Counts exits twice"]},"genetic_ancestry":{"European":60.0,"Martian":40.0}}""");
        Assert.That(r.Ok, Is.True, r.Error);
        var back = characters.GetById(c.Id)!;
        Assert.That(back.Behavioral.DecisionRules, Is.EqualTo(new[] { "Always pays" }), "a sibling not named is untouched");
        Assert.That(back.Behavioral.Habits, Is.EqualTo(new[] { "Counts exits twice" }));

        r = await writer.SetFieldsAsync(c.Id, """{"genetic_ancestry":{"Martian":null}}""");
        Assert.That(r.Ok, Is.True, r.Error);
        Assert.That(characters.GetById(c.Id)!.GeneticAncestry.Keys, Is.EqualTo(new[] { "European" }), "null on a dictionary key removes it");
    }

    [Test]
    public async Task Tags_replace_so_a_tag_taken_out_of_the_list_is_gone()
    {
        // Found live 2026-09-23 on Sable: the repository's tag sync only adds, so the write
        // read back with the removed tags still attached — and said so instead of answering ok.
        var c = NewCharacter();
        Assert.That((await writer.SetFieldsAsync(c.Id, """{"tags":["fixer","doctor-safekeeper"]}""")).Ok, Is.True);
        var r = await writer.SetFieldsAsync(c.Id, """{"tags":["fixer"]}""");
        Assert.That(r.Ok, Is.True, r.Error);
        Assert.That(characters.GetById(c.Id)!.Tags, Is.EqualTo(new[] { "fixer" }));
    }

    [Test]
    public async Task Null_clears_a_field_and_absent_keys_are_untouched()
    {
        var c = NewCharacter();
        Assert.That((await writer.SetFieldsAsync(c.Id, """{"story_hooks":["one","two"]}""")).Ok, Is.True);
        var r = await writer.SetFieldsAsync(c.Id, """{"story_hooks":null}""");
        Assert.That(r.Ok, Is.True, r.Error);
        var back = characters.GetById(c.Id)!;
        Assert.That(back.StoryHooks, Is.Empty);
        Assert.That(back.Description, Is.EqualTo("A street samurai."), "a key not given is not touched");
        Assert.That(back.Age, Is.EqualTo(27));
    }

    [TestCase("""{"location":"Halsted"}""", "location")]
    [TestCase("""{"id":"abc"}""", "identity")]
    [TestCase("""{"mother":"none"}""", "unknown field")]
    [TestCase("""{"age":"twenty-seven"}""", "wrong shape")]
    [TestCase("""{"name":null}""", "cannot be cleared")]
    [TestCase("""["not","an","object"]""", "one JSON object")]
    public async Task Bad_writes_are_refused_and_write_nothing(string fields, string reason)
    {
        var c = NewCharacter();
        await BackdateAsync(c.Id);
        var r = await writer.SetFieldsAsync(c.Id, fields);
        Assert.That(r.Ok, Is.False);
        Assert.That(r.Error, Does.Contain(reason));
        Assert.That(await ModifiedAtAsync(c.Id), Is.EqualTo(new DateTime(2020, 1, 1)));
    }

    [Test]
    public async Task A_write_that_changes_nothing_writes_nothing()
    {
        var c = NewCharacter();
        await BackdateAsync(c.Id);
        var r = await writer.SetFieldsAsync(c.Id, """{"age":27,"description":"A street samurai."}""");
        Assert.That(r.Ok, Is.True);
        Assert.That(r.Changed, Is.Empty);
        Assert.That(await ModifiedAtAsync(c.Id), Is.EqualTo(new DateTime(2020, 1, 1)));
    }

    [Test]
    public async Task A_write_that_would_unread_read_beats_says_so_and_waits_for_confirmation()
    {
        var c = NewCharacter();
        await BackdateAsync(c.Id);
        var (book, _) = await BookAsync("He paid.", "She took it.");
        var ids = await BeatIdsAsync(book);
        await TagAsync(ids[0], c);
        await ReadBookAsync(book);

        var refused = await writer.SetFieldsAsync(c.Id, """{"age":28}""");
        Assert.That(refused.Ok, Is.False);
        Assert.That(refused.UnreadCost, Is.EqualTo(1));
        Assert.That(refused.Error, Does.Contain("un-reads 1 beat"));
        Assert.That(characters.GetById(c.Id)!.Age, Is.EqualTo(27), "refused means not written");

        var confirmed = await writer.SetFieldsAsync(c.Id, """{"age":28}""", confirmUnread: true);
        Assert.That(confirmed.Ok, Is.True, confirmed.Error);
        var status = await gate.GetStatusAsync(book);
        Assert.That(status.Unread.Single().BeatId, Is.EqualTo(ids[0]));
        Assert.That(status.Unread.Single().Reason, Is.EqualTo(UnreadReason.EntityChanged));
    }
}

[TestFixture]
public class EntityVerificationTests : WorldFixture
{
    private async Task<(Guid Book, CharacterData C, List<Guid> Ids)> TaggedBookAsync()
    {
        var c = NewCharacter();
        await BackdateAsync(c.Id);
        var (book, _) = await BookAsync("He walked in.", "The rain.", "He paid.");
        var ids = await BeatIdsAsync(book);
        await TagAsync(ids[0], c);
        await TagAsync(ids[2], c);
        return (book, c, ids);
    }

    [Test]
    public async Task Begin_delivers_the_record_and_its_mentions_and_commit_records_the_verification()
    {
        var (book, c, ids) = await TaggedBookAsync();
        await ReadBookAsync(book);

        var p = await verifier.BeginAsync(Guid.Parse(c.Id), book);
        Assert.That(p.MentionCount, Is.EqualTo(2));
        Assert.That(p.Mentions.Select(m => m.BeatId), Is.EqualTo(new[] { ids[0], ids[2] }));
        Assert.That(p.UnreadMentions, Is.Zero);
        Assert.That(p.Mentions[0].Text, Does.StartWith("Kyle Test He walked in."), "mention text is delivered tag-stripped");
        Assert.That(p.Record?["description"]?.GetValue<string>(), Is.EqualTo("A street samurai."));

        var before = await ModifiedAtAsync(c.Id);
        var row = await verifier.CommitAsync(p.Nonce, "test");
        Assert.That(row.RecordModifiedAt, Is.EqualTo(before));
        Assert.That(await ModifiedAtAsync(c.Id), Is.EqualTo(before), "verifying never writes the entity, so it never un-reads anything");
    }

    [Test]
    public async Task Commit_is_refused_while_a_mention_beat_is_unread()
    {
        var (book, c, _) = await TaggedBookAsync();
        var p = await verifier.BeginAsync(Guid.Parse(c.Id), book);
        Assert.That(p.UnreadMentions, Is.EqualTo(2));
        var ex = Assert.ThrowsAsync<InvalidOperationException>(() => verifier.CommitAsync(p.Nonce, "test"));
        Assert.That(ex!.Message, Does.Contain("unread"));
    }

    [Test]
    public async Task Commit_is_refused_when_the_record_changed_after_begin()
    {
        var (book, c, _) = await TaggedBookAsync();
        await ReadBookAsync(book);
        var p = await verifier.BeginAsync(Guid.Parse(c.Id), book);
        var edited = characters.GetById(c.Id)!;
        edited.Description = "Changed while being examined.";
        characters.Save(edited);
        var ex = Assert.ThrowsAsync<InvalidOperationException>(() => verifier.CommitAsync(p.Nonce, "test"));
        Assert.That(ex!.Message, Does.Contain("record changed"));
    }

    [Test]
    public async Task Commit_is_refused_when_a_mention_beat_changed_after_begin()
    {
        var (book, c, ids) = await TaggedBookAsync();
        await ReadBookAsync(book);
        var p = await verifier.BeginAsync(Guid.Parse(c.Id), book);
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            var beat = await db.Beats.SingleAsync(b => b.Id == ids[2]);
            beat.Text += " And left.";
            beat.TextHash = NodeWorkbenchService.ComputeTextHash(beat.Text);
            await db.SaveChangesAsync();
        }
        var ex = Assert.ThrowsAsync<InvalidOperationException>(() => verifier.CommitAsync(p.Nonce, "test"));
        Assert.That(ex!.Message, Does.Contain("changed"));
    }

    [TestCase("not-a-nonce")]
    [TestCase("eyJ4Ijoi.forged")]
    public async Task A_nonce_this_Hub_did_not_issue_is_refused(string nonce)
    {
        await TaggedBookAsync();
        Assert.ThrowsAsync<ArgumentException>(() => verifier.CommitAsync(nonce, "test"));
    }

    [Test]
    public async Task A_tampered_nonce_is_refused()
    {
        var (book, c, _) = await TaggedBookAsync();
        await ReadBookAsync(book);
        var p = await verifier.BeginAsync(Guid.Parse(c.Id), book);
        var parts = p.Nonce.Split('.');
        var forged = (parts[0][0] == 'A' ? "B" : "A") + parts[0][1..] + "." + parts[1];
        Assert.ThrowsAsync<ArgumentException>(() => verifier.CommitAsync(forged, "test"));
    }

    [Test]
    public async Task F1_waits_for_the_full_read_then_fails_until_verified_then_goes_red_when_the_record_changes()
    {
        var (book, c, _) = await TaggedBookAsync();
        var f1 = async () => (await factory.StatusAsync(book)).Units.Single().Stations["F1"];

        Assert.That((await f1()).State, Is.EqualTo("waiting"), "verification follows the full read");
        await ReadBookAsync(book);
        Assert.That((await f1()).State, Is.EqualTo("fail"));
        Assert.That((await f1()).Detail, Does.Contain(c.Name));

        var p = await verifier.BeginAsync(Guid.Parse(c.Id), book);
        await verifier.CommitAsync(p.Nonce, "test");
        Assert.That((await f1()).Pass, Is.True);

        var r = await writer.SetFieldsAsync(c.Id, """{"age":28}""", confirmUnread: true);
        Assert.That(r.Ok, Is.True, r.Error);
        Assert.That((await f1()).Pass, Is.False, "a record change voids its verification");
    }

    [Test]
    public async Task Factory_next_skips_a_waiting_F1_and_sends_the_line_to_the_reading()
    {
        var (book, _, _) = await TaggedBookAsync();
        var next = await factory.NextAsync(book);
        Assert.That(next.Station, Is.EqualTo("F5"));
    }

    [Test]
    public async Task Verification_is_refused_while_the_record_breaks_a_law_and_allowed_once_it_is_fixed()
    {
        var (book, c, _) = await TaggedBookAsync();
        await rulings.RecordAsync(new RulingDraft("law", "Silence is not piezoelectric", book, Pattern: @"\bpiezo\w*"));
        Assert.That((await writer.SetFieldsAsync(c.Id, """{"behavioral":{"escalation_ladder":["Draw Silence","Piezo spark"]}}""")).Ok, Is.True);
        await ReadBookAsync(book);

        var p = await verifier.BeginAsync(Guid.Parse(c.Id), book);
        var ex = Assert.ThrowsAsync<InvalidOperationException>(() => verifier.CommitAsync(p.Nonce, "test"));
        Assert.That(ex!.Message, Does.Contain("behavioral.escalation_ladder[1]"));

        Assert.That((await writer.SetFieldsAsync(c.Id, """{"behavioral":{"escalation_ladder":["Draw Silence"]}}""", confirmUnread: true)).Ok, Is.True);
        await ReadBookAsync(book);
        p = await verifier.BeginAsync(Guid.Parse(c.Id), book);
        Assert.DoesNotThrowAsync(() => verifier.CommitAsync(p.Nonce, "test"));
    }
}

[TestFixture]
public class EntityFieldWriterTests : WorldFixture
{
    private FactionRepository factions = null!;
    private DistrictRepository places = null!;
    private EntityFieldWriter entityWriter = null!;

    [SetUp]
    public void SetUpEntityWriter()
    {
        factions = new FactionRepository(dbFactory);
        places = new DistrictRepository(dbFactory);
        var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddSingleton(services, factions);
        Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddSingleton(services, places);
        var sp = Microsoft.Extensions.DependencyInjection.ServiceCollectionContainerBuilderExtensions.BuildServiceProvider(services);
        entityWriter = new EntityFieldWriter(sp, gate, dbFactory, writer);
    }

    [Test]
    public void Every_repository_type_has_a_field_writer_and_a_record_loader()
    {
        var repoTypes = typeof(CharacterRepository).Assembly.GetTypes()
            .Where(t => !t.IsAbstract && t.BaseType is { IsGenericType: true } b && b.GetGenericTypeDefinition() == typeof(EfRepository<>))
            .ToList();
        Assert.That(repoTypes, Has.Count.GreaterThanOrEqualTo(29));
        var covered = EntityFieldWriter.Repositories.Values.Append(typeof(CharacterRepository)).ToHashSet();
        Assert.That(repoTypes.Where(t => !covered.Contains(t)).Select(t => t.Name), Is.Empty,
            "a repository type with no field writer is a record that can be read but not corrected");
        Assert.That(EntityFieldWriter.Repositories.Keys.Append("character"), Is.SubsetOf(CanonRecordLoader.MappedTypes));
    }

    [Test]
    public async Task A_faction_list_item_with_commas_stays_one_item_and_siblings_are_untouched()
    {
        var f = new FactionData { Id = Guid.NewGuid().ToString("N"), Name = "The Vultures", Description = "Body pickup.",
            Methods = ["Body pickup", "Discreet delivery, used once by Hua to insert a False Death Protocol subject"], StoryHooks = ["Doorstep"] };
        factions.Save(f);

        var r = await entityWriter.SetFieldsAsync(f.Id, """{"methods":["Body pickup","Discreet contract delivery, to a non-standard destination, no questions asked"]}""");
        Assert.That(r.Ok, Is.True, r.Error);
        Assert.That(r.Changed, Is.EqualTo(new[] { "methods" }));
        var back = factions.GetById(f.Id)!;
        Assert.That(back.Methods, Has.Count.EqualTo(2));
        Assert.That(back.Methods[1], Does.Contain("no questions asked"));
        Assert.That(back.StoryHooks, Is.EqualTo(new[] { "Doorstep" }), "a key not given is untouched");
    }

    [Test]
    public async Task A_place_takes_tags_away_and_refuses_unknown_keys()
    {
        var p = new DistrictData { Id = Guid.NewGuid().ToString("N"), Name = "Northpoint", Description = "A body farm.", Tags = ["carrion", "street-meat"] };
        places.Save(p);
        var r = await entityWriter.SetFieldsAsync(p.Id, """{"tags":["carrion"]}""");
        Assert.That(r.Ok, Is.True, r.Error);
        Assert.That(places.GetById(p.Id)!.Tags, Is.EqualTo(new[] { "carrion" }));

        var bad = await entityWriter.SetFieldsAsync(p.Id, """{"methods":["x"]}""");
        Assert.That(bad.Ok, Is.False);
        Assert.That(bad.Error, Does.Contain("unknown field"));
    }

    [Test]
    public async Task A_character_id_gets_the_character_rules()
    {
        var c = NewCharacter();
        var r = await entityWriter.SetFieldsAsync(c.Id, """{"location":"Halsted"}""");
        Assert.That(r.Ok, Is.False);
        Assert.That(r.Error, Does.Contain("location"));
    }
}

[TestFixture, NonParallelizable]
public class BannedNameForwardOnlyTests : WorldFixture
{
    [Test]
    public async Task A_name_that_predates_a_ban_can_still_be_edited_but_nobody_new_can_take_it()
    {
        // Found live 2026-09-23: every save bumps the Entities row, so the ban fired on the one
        // character the author let keep "Nadia" and her record could not be corrected at all.
        var nadia = NewCharacter("Dr. Nadia Park", "A researcher.");
        var other = NewCharacter("Someone Else", "x");
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            db.BannedNames.Add(new BannedName { Name = "Nadia" });
            await db.SaveChangesAsync();
        }
        var saved = Prose.Core.Services.WriteGate.WriteGateScope.SyncChecks;
        Prose.Core.Services.WriteGate.WriteGateScope.SyncChecks = [new Prose.Core.Services.WriteGate.BannedNameSyncCheck()];
        try
        {
            var edit = await writer.SetFieldsAsync(nadia.Id, """{"description":"She worked the architecture."}""");
            Assert.That(edit.Ok, Is.True, edit.Error);

            var taken = await writer.SetFieldsAsync(other.Id, """{"name":"Nadia Smith"}""");
            Assert.That(taken.Ok, Is.False);
            Assert.That(taken.Error, Does.Contain("banned name"));
        }
        finally { Prose.Core.Services.WriteGate.WriteGateScope.SyncChecks = saved; }
    }
}

[TestFixture]
public class RecordLawTests : WorldFixture
{
    [Test]
    public async Task A_search_reads_the_tagged_records_for_one_pattern_and_records_nothing()
    {
        var c = NewCharacter("Hua Test", "She runs the south-arm chamber.");
        var (book, _) = await BookAsync("Nothing here.");
        await TagAsync((await BeatIdsAsync(book))[0], c);
        var hits = await rulings.FindRecordViolationsAsync(book, searchPattern: @"south-arm");
        Assert.That(hits.Single().Field, Is.EqualTo("description"));
        Assert.That(await rulings.ListAsync(book), Is.Empty, "a search is not a ruling");
        Assert.That(await rulings.FindRecordViolationsAsync(book), Is.Empty, "and the laws, of which there are none, find nothing");
    }

    [Test]
    public async Task A_law_binds_the_records_a_book_tags_and_a_page_law_binds_only_the_page()
    {
        var c = NewCharacter("Silence Smith", "Made by Seo. Her blade has a piezoelectric edge.");
        var (book, _) = await BookAsync("Seo made it.", "Nothing here.");
        var ids = await BeatIdsAsync(book);
        await TagAsync(ids[1], c);
        await rulings.RecordAsync(new RulingDraft("law", "not piezoelectric", book, Pattern: @"\bpiezo\w*"));
        await rulings.RecordAsync(new RulingDraft("page-law", "Seo made Silence: canon only, never on the page", book, Pattern: @"(?-i)\bSeo\b"));

        var records = await rulings.FindRecordViolationsAsync(book);
        Assert.That(records.Select(h => (h.Field, h.Match)), Is.EqualTo(new[] { ("description", "piezoelectric") }),
            "the record may hold Seo (a page-law), but not piezo (a law)");

        var page = await rulings.FindLawViolationsAsync(book);
        Assert.That(page.Select(h => h.Match), Is.EqualTo(new[] { "Seo" }), "the page may not say Seo");
    }

    [Test]
    public async Task A_record_the_book_does_not_tag_is_not_its_business()
    {
        NewCharacter("Elsewhere", "A piezoelectric toaster from another book.");
        var (book, _) = await BookAsync("Nothing here.");
        await rulings.RecordAsync(new RulingDraft("law", "not piezoelectric", book, Pattern: @"\bpiezo\w*"));
        Assert.That(await rulings.FindRecordViolationsAsync(book), Is.Empty);
    }
}
