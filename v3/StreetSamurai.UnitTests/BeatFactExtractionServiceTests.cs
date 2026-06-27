using Microsoft.Extensions.Logging.Abstractions;
using StreetSamurai.Core.Interfaces;
using StreetSamurai.Core.Models;
using StreetSamurai.Core.Services;

namespace StreetSamurai.UnitTests;

sealed class ConfigurableLlm : ILlmService
{
    public string Response { get; set; } = "{}";
    public int CallCount { get; private set; }
    public bool Throws { get; set; }

    public Task<bool> IsConfiguredAsync() => Task.FromResult(true);

    public Task<string> GenerateAsync(
        string system, string user, double temperature = 0.8,
        int maxTokens = 4096, string? model = null, CancellationToken ct = default)
    {
        CallCount++;
        if (Throws) throw new InvalidOperationException("LLM offline");
        return Task.FromResult(Response);
    }
}

[TestFixture]
public class BeatFactExtractionServiceTests
{
    static BeatFactExtractionService Make(ConfigurableLlm llm) =>
        new(llm, NullLogger<BeatFactExtractionService>.Instance);

    static Chapter AnyChapter(int? number = 1) => new()
    {
        Id = Guid.NewGuid().ToString("N"),
        Number = number,
        Title = "Test Chapter",
    };

    static ChapterBeat AnyBeat(string text = "He moved through the dark.", string synopsis = "Moving") => new()
    {
        Id = Guid.NewGuid().ToString("N"),
        Index = 1,
        Title = "Beat 1",
        Text = text,
        Synopsis = synopsis,
    };

    [Test]
    public async Task EmptyCharacterNames_ReturnsEmpty_LlmNeverCalled()
    {
        var llm = new ConfigurableLlm();
        var svc = Make(llm);

        var result = await svc.ExtractAsync(AnyChapter(), AnyBeat(), [], CancellationToken.None);

        Assert.That(result.Knowledge, Is.Empty);
        Assert.That(result.Conditions, Is.Empty);
        Assert.That(llm.CallCount, Is.EqualTo(0));
    }

    [Test]
    public async Task BothTextAndSynopsisEmpty_ReturnsEmpty_LlmNeverCalled()
    {
        var llm = new ConfigurableLlm();
        var svc = Make(llm);
        var beat = AnyBeat(text: "", synopsis: "");

        var result = await svc.ExtractAsync(AnyChapter(), beat, ["Kyle"], CancellationToken.None);

        Assert.That(result.Knowledge, Is.Empty);
        Assert.That(result.Conditions, Is.Empty);
        Assert.That(llm.CallCount, Is.EqualTo(0));
    }

    [Test]
    public async Task OnlyTextPresent_Proceeds_LlmCalled()
    {
        var llm = new ConfigurableLlm { Response = """{"knowledge":[],"conditions":[]}""" };
        var svc = Make(llm);
        var beat = AnyBeat(text: "Kyle stepped into the rain.", synopsis: "");

        await svc.ExtractAsync(AnyChapter(), beat, ["Kyle"], CancellationToken.None);

        Assert.That(llm.CallCount, Is.EqualTo(1));
    }

    [Test]
    public async Task OnlySynopsisPresent_Proceeds_LlmCalled()
    {
        var llm = new ConfigurableLlm { Response = """{"knowledge":[],"conditions":[]}""" };
        var svc = Make(llm);
        var beat = AnyBeat(text: "", synopsis: "Kyle opens the door.");

        await svc.ExtractAsync(AnyChapter(), beat, ["Kyle"], CancellationToken.None);

        Assert.That(llm.CallCount, Is.EqualTo(1));
    }

    [Test]
    public async Task LlmThrows_ReturnsEmpty_NoException()
    {
        var llm = new ConfigurableLlm { Throws = true };
        var svc = Make(llm);

        var result = await svc.ExtractAsync(AnyChapter(), AnyBeat(), ["Kyle"], CancellationToken.None);

        Assert.That(result.Knowledge, Is.Empty);
        Assert.That(result.Conditions, Is.Empty);
    }

    [Test]
    public async Task LlmReturnsBareJson_EmptyArrays_BothListsEmpty()
    {
        var llm = new ConfigurableLlm { Response = """{"knowledge":[],"conditions":[]}""" };
        var svc = Make(llm);

        var result = await svc.ExtractAsync(AnyChapter(), AnyBeat(), ["Kyle"], CancellationToken.None);

        Assert.That(result.Knowledge, Is.Empty);
        Assert.That(result.Conditions, Is.Empty);
    }

    [Test]
    public async Task KnowledgeItem_NullCharacter_Skipped()
    {
        var json = """
            {"knowledge":[{"character":null,"topic":"The door is unlocked","summary":"found out"}],"conditions":[]}
            """;
        var llm = new ConfigurableLlm { Response = json };
        var svc = Make(llm);

        var result = await svc.ExtractAsync(AnyChapter(), AnyBeat(), ["Kyle"], CancellationToken.None);

        Assert.That(result.Knowledge, Is.Empty);
    }

    [Test]
    public async Task KnowledgeItem_NullTopic_Skipped()
    {
        var json = """
            {"knowledge":[{"character":"Kyle","topic":null,"summary":"found out"}],"conditions":[]}
            """;
        var llm = new ConfigurableLlm { Response = json };
        var svc = Make(llm);

        var result = await svc.ExtractAsync(AnyChapter(), AnyBeat(), ["Kyle"], CancellationToken.None);

        Assert.That(result.Knowledge, Is.Empty);
    }

    [Test]
    public async Task ConditionItem_NullKind_Skipped()
    {
        var json = """
            {"knowledge":[],"conditions":[{"character":"Kyle","kind":null,"name":"Methylin","severity":"moderate"}]}
            """;
        var llm = new ConfigurableLlm { Response = json };
        var svc = Make(llm);

        var result = await svc.ExtractAsync(AnyChapter(), AnyBeat(), ["Kyle"], CancellationToken.None);

        Assert.That(result.Conditions, Is.Empty);
    }

    [Test]
    public async Task LlmReturnsJsonFenced_FencesStripped_ParsedCorrectly()
    {
        var json = "```json\n{\"knowledge\":[],\"conditions\":[]}\n```";
        var llm = new ConfigurableLlm { Response = json };
        var svc = Make(llm);

        var result = await svc.ExtractAsync(AnyChapter(), AnyBeat(), ["Kyle"], CancellationToken.None);

        Assert.That(result.Knowledge, Is.Empty);
        Assert.That(result.Conditions, Is.Empty);
    }

    [Test]
    public async Task ValidKnowledgeItem_AppearsInResult()
    {
        var json = """
            {
              "knowledge":[{"character":"Kyle","topic":"Hua's tab","summary":"24 hours overdue","entities":["Hua"]}],
              "conditions":[]
            }
            """;
        var llm = new ConfigurableLlm { Response = json };
        var svc = Make(llm);

        var result = await svc.ExtractAsync(AnyChapter(), AnyBeat(), ["Kyle"], CancellationToken.None);

        Assert.That(result.Knowledge, Has.Count.EqualTo(1));
        var (charName, item) = result.Knowledge[0];
        Assert.That(charName, Is.EqualTo("Kyle"));
        Assert.That(item.Topic, Is.EqualTo("Hua's tab"));
        Assert.That(item.Summary, Is.EqualTo("24 hours overdue"));
        Assert.That(item.Entities, Contains.Item("Hua"));
    }

    [Test]
    public async Task ValidConditionItem_AppearsInResult()
    {
        var json = """
            {
              "knowledge":[],
              "conditions":[{"character":"Sasha","kind":"addiction","name":"Methylin","severity":"moderate","notes":"since the Shelf job"}]
            }
            """;
        var llm = new ConfigurableLlm { Response = json };
        var svc = Make(llm);

        var result = await svc.ExtractAsync(AnyChapter(), AnyBeat(), ["Sasha"], CancellationToken.None);

        Assert.That(result.Conditions, Has.Count.EqualTo(1));
        var (charName, item) = result.Conditions[0];
        Assert.That(charName, Is.EqualTo("Sasha"));
        Assert.That(item.Kind, Is.EqualTo("addiction"));
        Assert.That(item.Name, Is.EqualTo("Methylin"));
        Assert.That(item.Severity, Is.EqualTo("moderate"));
        Assert.That(item.Notes, Is.EqualTo("since the Shelf job"));
    }

    [Test]
    public async Task ChapterNumberNull_NoCrash_UsesQuestionMark()
    {
        var llm = new ConfigurableLlm { Response = """{"knowledge":[],"conditions":[]}""" };
        var svc = Make(llm);
        var chapter = AnyChapter(number: null);

        Assert.DoesNotThrowAsync(() =>
            svc.ExtractAsync(chapter, AnyBeat(), ["Kyle"], CancellationToken.None));
    }

    [Test]
    public async Task ValidKnowledgeItem_ChapterNumberRecorded()
    {
        var json = """
            {"knowledge":[{"character":"Kyle","topic":"The key","summary":"it opens the vault"}],"conditions":[]}
            """;
        var llm = new ConfigurableLlm { Response = json };
        var svc = Make(llm);
        var chapter = AnyChapter(number: 7);

        var result = await svc.ExtractAsync(chapter, AnyBeat(), ["Kyle"], CancellationToken.None);

        Assert.That(result.Knowledge[0].Item.LearnedChapter, Is.EqualTo(7));
    }
}
