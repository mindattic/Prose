using Prose.Core.Models;
using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// Regression cover for the 2026-08-09 ammo-clamp fix. The [RESOURCE LEDGER] block is the
/// model's own self-report of ammo/grenade/neural state appended to its prose; nothing
/// previously validated it against the weapon's actual capacity or the prior beat's count, so
/// a hallucinated refill or an over-capacity count was accepted verbatim and persisted as
/// ground truth for the NEXT exchange — silently breaking "ammo is finite." Fix: clamp each
/// weapon's reported ammo to [0, declared /max] when the model reports a capacity, or to
/// [0, prior count] when it doesn't (ammo can only deplete without an explicit reload elsewhere
/// in scene logic).
/// </summary>
[TestFixture]
public class CombatSceneWriterAmmoClampTests
{
    static Dictionary<string, CombatantResources> OneCombatant(string name, int ammo, string weapon = "Chorus") =>
        new(StringComparer.OrdinalIgnoreCase)
        {
            [name] = new CombatantResources { AmmoByWeapon = new Dictionary<string, int> { [weapon] = ammo } },
        };

    static string Ledger(string name, string ammoField) =>
        $"Some prose describing the exchange.\n[RESOURCE LEDGER]\n{name}: AMMO {ammoField}\n[/RESOURCE LEDGER]\nMore prose after.";

    [Test]
    public void NormalDepletion_IsAccepted()
    {
        var current = OneCombatant("Kyle", ammo: 6);
        var beatText = Ledger("Kyle", "Chorus=4");

        var (clean, updated) = CombatSceneWriter.ParseResourceLedger(beatText, current);

        Assert.That(updated["Kyle"].AmmoByWeapon["Chorus"], Is.EqualTo(4));
        Assert.That(clean, Does.Not.Contain("RESOURCE LEDGER"));
    }

    [Test]
    public void ReportedAmmoExceedingDeclaredCapacity_IsClampedToCapacity()
    {
        var current = OneCombatant("Kyle", ammo: 4);
        // Model hallucinates a refill AND declares a capacity of 5 — reported 12 must clamp to 5.
        var beatText = Ledger("Kyle", "Chorus=12/5");

        var (_, updated) = CombatSceneWriter.ParseResourceLedger(beatText, current);

        Assert.That(updated["Kyle"].AmmoByWeapon["Chorus"], Is.EqualTo(5),
            "ammo must never exceed the model's own declared magazine capacity");
    }

    [Test]
    public void ReportedAmmoIncreaseWithNoDeclaredCapacity_IsClampedToPriorCount()
    {
        var current = OneCombatant("Kyle", ammo: 3);
        // No /max given, but the model reports MORE ammo than the prior beat had — a
        // hallucinated refill with no reload event. Must clamp to the prior count (3), since
        // ammo can only deplete without an explicit reload elsewhere in scene logic.
        var beatText = Ledger("Kyle", "Chorus=9");

        var (_, updated) = CombatSceneWriter.ParseResourceLedger(beatText, current);

        Assert.That(updated["Kyle"].AmmoByWeapon["Chorus"], Is.EqualTo(3),
            "an undeclared-capacity increase must clamp to the prior count, not accept the hallucinated value");
    }

    [Test]
    public void ReportedAmmoNegative_ClampsToZero()
    {
        var current = OneCombatant("Kyle", ammo: 2);
        var beatText = Ledger("Kyle", "Chorus=0");

        var (_, updated) = CombatSceneWriter.ParseResourceLedger(beatText, current);

        Assert.That(updated["Kyle"].AmmoByWeapon["Chorus"], Is.EqualTo(0));
    }

    [Test]
    public void NoLedgerBlock_ReturnsOriginalStateUnchanged()
    {
        var current = OneCombatant("Kyle", ammo: 4);
        var beatText = "Just prose, no ledger block at all.";

        var (clean, updated) = CombatSceneWriter.ParseResourceLedger(beatText, current);

        Assert.That(clean, Is.EqualTo(beatText));
        Assert.That(updated["Kyle"].AmmoByWeapon["Chorus"], Is.EqualTo(4));
    }
}
