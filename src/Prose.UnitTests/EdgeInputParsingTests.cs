using Prose.Core.Models;
using Prose.Core.Services;
using Prose.Core.Services.Factory;

namespace Prose.UnitTests;

/// <summary>
/// Edge-case input that used to throw instead of answering: a model-written resource ledger with
/// an overflowing count, and a journal window reaching back before year 1.
/// </summary>
[TestFixture]
public class EdgeInputParsingTests
{
    static Dictionary<string, CombatantResources> Kyle(int ammo = 4, int grenades = 2, int neural = 70) =>
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["Kyle"] = new CombatantResources
            {
                AmmoByWeapon = new Dictionary<string, int> { ["Chorus"] = ammo },
                Grenades = [new GrenadeStock { Type = "frag", Effect = "blast", Count = grenades }],
                BioBatteryPercent = neural,
            },
        };

    static string Ledger(string fields) =>
        $"Prose.\n[RESOURCE LEDGER]\nKyle: {fields}\n[/RESOURCE LEDGER]\nMore prose.";

    [Test]
    public void OverflowingAmmoCount_ClampsInsteadOfThrowing()
    {
        var (_, updated) = CombatSceneWriter.ParseResourceLedger(Ledger("AMMO Chorus=99999999999"), Kyle(ammo: 4));
        Assert.That(updated["Kyle"].AmmoByWeapon["Chorus"], Is.EqualTo(4), "no declared capacity: clamps to the prior count");
    }

    [Test]
    public void OverflowingNeuralAndGrenades_DoNotThrow()
    {
        var (_, updated) = CombatSceneWriter.ParseResourceLedger(
            Ledger("NEURAL=99999999999% | GRENADES frag x99999999999"), Kyle(grenades: 2));
        Assert.That(updated["Kyle"].BioBatteryPercent, Is.EqualTo(100));
        Assert.That(updated["Kyle"].Grenades.Single().Count, Is.EqualTo(2), "an overflowing count keeps the prior stock");
    }

    [Test]
    public void NonAsciiDigits_AreIgnored_NotAFormatException()
    {
        // U+0663 ARABIC-INDIC DIGIT THREE matched \d, and int.Parse rejects it.
        var (_, updated) = CombatSceneWriter.ParseResourceLedger(Ledger("AMMO Chorus=٣ | NEURAL=٥٠%"), Kyle(ammo: 4, neural: 70));
        Assert.That(updated["Kyle"].AmmoByWeapon["Chorus"], Is.EqualTo(4));
        Assert.That(updated["Kyle"].BioBatteryPercent, Is.EqualTo(70));
    }

    [TestCase("99999999d")]
    [TestCase("Infinityd")]
    [TestCase("99999999999999h")]
    public void JournalWindowBeforeYearOne_IsRejected_NotThrown(string since)
    {
        Assert.That(FactoryJournal.TryParseInstant(since, out _), Is.False);
    }

    [Test]
    public void JournalWindow_OrdinarySpansStillParse()
    {
        Assert.That(FactoryJournal.TryParseInstant("2d", out var at), Is.True);
        Assert.That((DateTime.UtcNow - at).TotalDays, Is.EqualTo(2).Within(0.01));
        Assert.That(FactoryJournal.TryParseInstant("90m", out var m), Is.True);
        Assert.That((DateTime.UtcNow - m).TotalMinutes, Is.EqualTo(90).Within(1));
    }
}
