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
        // Live entities belong to a universe; the book fixtures use the first one. Without this the
        // save path's scan cannot see the character, so its tagged name reads as an unknown name.
        using (var db = dbFactory.CreateDbContext())
        {
            var universe = db.Universes.Select(u => u.Id).FirstOrDefault();
            if (universe == Guid.Empty)
            {
                universe = Guid.CreateVersion7();
                db.Universes.Add(new Universe { Id = universe, Slug = "u-" + universe.ToString("N")[..6], Name = "U" });
            }
            var e = db.Entities.IgnoreQueryFilters().Single(x => x.Id == Guid.Parse(c.Id));
            e.UniverseId = universe;
            db.SaveChanges();
        }
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

        // Tags are a set: a new tag written first reads back last, and that is not a failed write.
        var reordered = await writer.SetFieldsAsync(c.Id, """{"tags":["researcher","fixer"]}""");
        Assert.That(reordered.Ok, Is.True, reordered.Error);
        Assert.That(characters.GetById(c.Id)!.Tags, Is.EquivalentTo(new[] { "fixer", "researcher" }));
    }

    [Test]
    public async Task Relationships_are_a_set_so_the_store_sorting_them_is_not_a_failed_write()
    {
        // Found live 2026-09-23 on Mrs. Chen: the store loads relationships sorted by name, so a
        // correct write whose list ran in any other order read back "different" and answered not-ok.
        var c = NewCharacter();
        var first = await writer.SetFieldsAsync(c.Id,
            """{"relationships":[{"name":"Kyle","type":"customer"},{"name":"Pixel","type":"neighbor"},{"name":"West Town","type":"block"}]}""");
        Assert.That(first.Ok, Is.True, first.Error);

        var stored = (JsonArray)first.Record!["relationships"]!;
        var reversed = new JsonArray(stored.Reverse().Select(n => n!.DeepClone()).ToArray());
        ((JsonObject)reversed[0]!)["description"] = "Half the block passes through her counter.";
        var r = await writer.SetFieldsAsync(c.Id, new JsonObject { ["relationships"] = reversed }.ToJsonString());
        Assert.That(r.Ok, Is.True, r.Error);
        Assert.That(r.NotLanded, Is.Empty);
        Assert.That(r.Changed, Does.Contain("relationships"));
        Assert.That(characters.GetById(c.Id)!.Relationships.Single(x => x.Name == "West Town").Description,
            Is.EqualTo("Half the block passes through her counter."));
    }

    [Test]
    public void Relationships_compare_without_order_but_every_other_list_keeps_its_order()
    {
        var a = JsonNode.Parse("""[{"name":"A","type":"x"},{"name":"B","type":"y"}]""");
        var sameSet = JsonNode.Parse("""[{"type":"y","name":"B"},{"name":"A","type":"x"}]""");
        var changed = JsonNode.Parse("""[{"name":"A","type":"x"},{"name":"B","type":"z"}]""");
        Assert.That(FieldPatch.Same("relationships", a, sameSet), Is.True);
        Assert.That(FieldPatch.Same("relationships", a, changed), Is.False, "a changed relationship is still a difference");
        Assert.That(FieldPatch.Same("timeline", a, sameSet), Is.False, "other lists are ordered");
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

[TestFixture]
public class CaptureScannerTests : WorldFixture
{
    [Test]
    public async Task A_name_used_in_two_beats_that_nothing_accounts_for_is_unresolved_until_a_ruling_names_it()
    {
        var (book, _) = await BookAsync("He met Halvorsen at the dock.", "Later that night, Halvorsen left.", "Only once did he see Quillfeather.");
        var r = await capture.ScanAsync(book);
        Assert.That(r.Unresolved.Select(n => n.Name), Is.EqualTo(new[] { "Halvorsen" }), "a hapax is not a finding");
        Assert.That((await factory.StatusAsync(book)).Units.Single().Stations["F4"].Pass, Is.False);

        await rulings.RecordAsync(new RulingDraft("incidental", "Halvorsen is a one-off dock foreman with no entity", book, Pattern: "Halvorsen", Source: "session:test"));
        Assert.That((await capture.ScanAsync(book)).Unresolved, Is.Empty, "the memo is keyed on the incidental rulings");
        Assert.That((await factory.StatusAsync(book)).Units.Single().Stations["F4"].Pass, Is.True);
    }

    [TestCase("It hit Mach six.", "They flew at Mach three.")]
    [TestCase("A cheap Tier 3 implant.", "Nothing above Tier 2 here.")]
    [TestCase("Meet me on Thursdays.", "He came by on Thursdays.")]
    public async Task A_format_word_capitalized_by_convention_is_not_a_missing_entity(string first, string second)
    {
        var (book, _) = await BookAsync(first, second);
        Assert.That((await capture.ScanAsync(book)).Unresolved, Is.Empty);
    }

    [Test]
    public async Task A_known_entity_named_without_its_tag_is_untagged_until_the_beat_is_saved_again()
    {
        var c = NewCharacter("Renko Moss", "A crew chief.");
        var (book, _) = await BookAsync("drove.", "Nobody spoke to Renko Moss on the way back.");
        var ids = await BeatIdsAsync(book);
        await TagAsync(ids[0], c);   // tagged in the first beat, named untagged in the second

        var r = await capture.ScanAsync(book);
        var miss = r.Untagged.Single();
        Assert.That(miss.BeatId, Is.EqualTo(ids[1]));
        Assert.That(miss.EntityName, Is.EqualTo("Renko Moss"));

        string text;
        await using (var db = await dbFactory.CreateDbContextAsync())
            text = await db.Beats.Where(b => b.Id == ids[1]).Select(b => b.Text).SingleAsync();
        await workbench.UpdateBeatTextAsync(ids[1], text, BeatWriteReason.TagMaintenance, deferAnalysis: true);

        var after = await capture.ScanAsync(book);
        Assert.That(after.Untagged, Is.Empty, "the scan is the save path's own, so a re-save clears exactly what it reports");
        await using (var db = await dbFactory.CreateDbContextAsync())
            Assert.That(BeatMarkup.StripEntityTags(await db.Beats.Where(b => b.Id == ids[1]).Select(b => b.Text).SingleAsync()),
                Is.EqualTo(BeatMarkup.StripEntityTags(text)), "tags only; no word changed");
    }
}

[TestFixture]
public class SaveMentionsTests : WorldFixture
{
    [Test]
    public async Task A_save_writes_its_mention_rows_before_it_returns()
    {
        // Baseline B11: the rows were written by a fire-and-forget task, so the next read raced it
        // ("database is locked" under SQLite, stale mentions on SQL Server).
        var c = NewCharacter("Renko Moss", "A crew chief.");
        var (book, _) = await BookAsync("drove.");
        var beat = (await BeatIdsAsync(book))[0];
        await workbench.UpdateBeatTextAsync(beat, "Renko Moss drove.", BeatWriteReason.AuthorEdit, deferAnalysis: true);
        await using var db = await dbFactory.CreateDbContextAsync();
        var rows = await db.BeatEntityMentions.Where(m => m.BeatId == beat).Select(m => m.EntityId).ToListAsync();
        Assert.That(rows, Is.EqualTo(new[] { Guid.Parse(c.Id) }), "nothing to wait for: the rows exist when the save returns");
    }
}

[TestFixture]
public class CaptureRetagTests : WorldFixture
{
    [Test]
    public void RetagName_moves_or_removes_only_that_surface_for_that_entity_and_changes_no_words()
    {
        var five = Guid.CreateVersion7();
        var corp = Guid.CreateVersion7();
        var other = Guid.CreateVersion7();
        var stored = $"<entity repo=\"character\" guid=\"{five}\">Five</entity> shells. <entity repo=\"character\" guid=\"{five}\">Praxis</entity> soldiers. " +
                     $"<entity repo=\"character\" guid=\"{other}\">Five</entity> is someone else's.";

        var off = CaptureScanner.RetagName(stored, "Five", five);
        Assert.That(BeatMarkup.StripEntityTags(off), Is.EqualTo(BeatMarkup.StripEntityTags(stored)), "no word changes");
        Assert.That(BeatMarkup.CountTagsByEntity(off).GetValueOrDefault(five), Is.EqualTo(1), "only the Five surface came off; his Praxis tag stays");
        Assert.That(BeatMarkup.CountTagsByEntity(off)[other], Is.EqualTo(1), "another entity's tag on the same word is untouched");

        var moved = CaptureScanner.RetagName(stored, "Praxis", five, corp, "corponation");
        Assert.That(moved, Does.Contain($"<entity repo=\"corponation\" guid=\"{corp}\">Praxis</entity>"));
        Assert.That(BeatMarkup.CountTagsByEntity(moved).GetValueOrDefault(five), Is.EqualTo(1));
        Assert.That(BeatMarkup.StripEntityTags(moved), Is.EqualTo(BeatMarkup.StripEntityTags(stored)));
    }

    [Test]
    public async Task A_numeral_tag_taken_off_stays_off_through_the_save()
    {
        var op = NewCharacter("Praxis Operator Five", "An operator.");
        var opId = Guid.Parse(op.Id);
        var (book, _) = await BookAsync("drove.");
        var beat = (await BeatIdsAsync(book))[0];
        async Task<string> TextAsync()
        {
            await using var db = await dbFactory.CreateDbContextAsync();
            return await db.Beats.Where(b => b.Id == beat).Select(b => b.Text).SingleAsync();
        }

        await workbench.UpdateBeatTextAsync(beat, $"<entity repo=\"character\" guid=\"{opId}\">Five</entity> shells meant five chances.",
            BeatWriteReason.TagMaintenance, deferAnalysis: true);
        Assert.That(BeatMarkup.CountTagsByEntity(await TextAsync()).GetValueOrDefault(opId), Is.EqualTo(1), "a tag placed by hand is pinned");

        await workbench.UpdateBeatTextAsync(beat, CaptureScanner.RetagName(await TextAsync(), "Five", opId), BeatWriteReason.TagMaintenance, deferAnalysis: true);
        var text = await TextAsync();
        Assert.That(BeatMarkup.CountTagsByEntity(text).GetValueOrDefault(opId), Is.EqualTo(0), "the scan no longer derives a numeral, so the tag stays off");
        Assert.That(BeatMarkup.StripEntityTags(text), Is.EqualTo("Five shells meant five chances."));
    }
}

[TestFixture]
public class CapturePinTests : WorldFixture
{
    [Test]
    public void PinName_wraps_only_untagged_whole_word_uses_and_changes_no_words()
    {
        var id = Guid.CreateVersion7();
        var stored = $"<entity repo=\"character\" guid=\"{id}\">Sable</entity> and Sable's coat, not Sables or sable.";
        var pinned = CaptureScanner.PinName(stored, "Sable", id, "character");
        Assert.That(BeatMarkup.CountTagsByEntity(pinned)[id], Is.EqualTo(2), "the tagged use stays one tag; the untagged one is wrapped");
        Assert.That(BeatMarkup.StripEntityTags(pinned), Is.EqualTo(BeatMarkup.StripEntityTags(stored)));
    }

    [Test]
    public async Task A_name_the_universe_makes_ambiguous_is_resolved_from_what_the_book_already_tags()
    {
        // "Sable" is Sable's whole name and also a curated alias of "Sable Whitfield": two whole
        // claims, so the save's scan drops it as ambiguous — but the book already tags it, once, as
        // Sable. (A name that is only DERIVED from "Sable Whitfield" is no longer a contest: the whole
        // name wins it outright. Found live on BCODA.)
        var sable = NewCharacter("Sable", "A fixer.");
        var whitfield = NewCharacter("Sable Whitfield", "Someone else entirely.");
        whitfield.Aliases = ["Sable"];
        characters.Save(whitfield);
        var (book, _) = await BookAsync("waited.", "Later, Sable left.", "Nobody saw Sable go.");
        var ids = await BeatIdsAsync(book);
        await TagAsync(ids[0], sable);

        var name = (await capture.ScanAsync(book)).Unresolved.Single();
        Assert.That(name.Name, Is.EqualTo("Sable"));
        Assert.That(name.BookSays, Is.EqualTo(Guid.Parse(sable.Id)), "the book has said who it is");

        foreach (var beatId in name.BeatIds)
        {
            string text;
            await using (var db = await dbFactory.CreateDbContextAsync())
                text = await db.Beats.Where(b => b.Id == beatId).Select(b => b.Text).SingleAsync();
            await workbench.UpdateBeatTextAsync(beatId, CaptureScanner.PinName(text, "Sable", name.BookSays!.Value, "character"),
                BeatWriteReason.TagMaintenance, deferAnalysis: true);
        }
        var after = await capture.ScanAsync(book);
        Assert.That(after.Unresolved, Is.Empty);
        Assert.That(after.Untagged, Is.Empty, "the save kept the pinned tags");
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
