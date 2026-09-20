using NUnit.Framework;
using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// The error-triage queue rests on one idea: a fault has a stable signature, so the same break
/// recurring a thousand times is one line of work, and "I fixed it" is a claim the log files get
/// to contradict.
///
/// <para>These tests pin the grouping rules. Get them wrong in one direction and every occurrence
/// becomes its own issue, which is just the log file again with extra steps. Get them wrong in the
/// other and two unrelated faults collapse into one, so fixing the first silently marks the second
/// done.</para>
/// </summary>
[TestFixture]
public class LogIssueServiceTests
{
    static LogEntry Entry(string message, string level = "Error", string? exception = null) =>
        new() { Timestamp = DateTime.Now, Level = level, Message = message, Exception = exception };

    // ── What must collapse together ─────────────────────────────────────────

    [Test]
    public void The_same_fault_about_different_data_is_one_signature()
    {
        var a = Entry("Beat 019d6143-a635-7e53-bc5a-16eb6cfe5941 failed to save after 812ms");
        var b = Entry("Beat 720b2787-ee18-4e33-a938-6446bc9d06c8 failed to save after 47ms");

        Assert.That(LogIssueService.SignatureOf(a), Is.EqualTo(LogIssueService.SignatureOf(b)),
            "One broken code path writes hundreds of these. If the guid and the timing split them " +
            "apart, the queue is just the log file again.");
    }

    [Test]
    public void Paths_and_timestamps_do_not_split_a_fault()
    {
        var a = Entry(@"Could not read D:\Projects\MindAttic\Prose\engine\data\a.txt at 2026-09-12 13:04:01");
        var b = Entry(@"Could not read D:\Projects\MindAttic\Prose\engine\data\zzz.txt at 2026-09-11 09:44:52");

        Assert.That(LogIssueService.SignatureOf(a), Is.EqualTo(LogIssueService.SignatureOf(b)));
    }

    [Test]
    public void A_bare_32_hex_id_normalises_like_a_guid()
    {
        // Node ids appear in this corpus in "N" format (no dashes) as often as dashed.
        var a = Entry("Chapter 019d6143a6357e53bc5a16eb6cfe5941 has no beats");
        var b = Entry("Chapter 720b2787ee184e33a9386446bc9d06c8 has no beats");

        Assert.That(LogIssueService.SignatureOf(a), Is.EqualTo(LogIssueService.SignatureOf(b)));
    }

    // ── What must stay apart ────────────────────────────────────────────────

    [Test]
    public void Genuinely_different_messages_stay_separate()
    {
        var a = Entry("Beat 019d6143-a635-7e53-bc5a-16eb6cfe5941 failed to save");
        var b = Entry("Entity 019d6143-a635-7e53-bc5a-16eb6cfe5941 has no record");

        Assert.That(LogIssueService.SignatureOf(a), Is.Not.EqualTo(LogIssueService.SignatureOf(b)),
            "Collapsing unrelated faults is the worse failure: fixing one would mark the other done.");
    }

    [Test]
    public void The_same_text_at_different_severities_stays_separate()
    {
        var warn  = Entry("Hub could not reach SQL Server", level: "Warning");
        var error = Entry("Hub could not reach SQL Server", level: "Error");

        Assert.That(LogIssueService.SignatureOf(warn), Is.Not.EqualTo(LogIssueService.SignatureOf(error)),
            "A degraded warning and a hard failure are different problems even when worded alike.");
    }

    [Test]
    public void The_same_message_from_different_exception_types_stays_separate()
    {
        var a = Entry("Save failed", exception: "System.TimeoutException: The operation timed out");
        var b = Entry("Save failed", exception: "Microsoft.Data.SqlClient.SqlException: Deadlock");

        Assert.That(LogIssueService.SignatureOf(a), Is.Not.EqualTo(LogIssueService.SignatureOf(b)),
            "A timeout and a deadlock need different fixes, however alike the log line reads.");
    }

    // ── Stability ───────────────────────────────────────────────────────────

    [Test]
    public void A_signature_is_stable_across_calls_and_short_enough_to_type()
    {
        var e = Entry("Beat 019d6143-a635-7e53-bc5a-16eb6cfe5941 failed");
        var sig = LogIssueService.SignatureOf(e);

        Assert.That(LogIssueService.SignatureOf(e), Is.EqualTo(sig), "Must be deterministic — it is a database key.");
        Assert.That(sig, Has.Length.EqualTo(10), "Short enough to retype from a terminal.");
        Assert.That(sig, Does.Match("^[0-9a-f]+$"));
    }

    [Test]
    public void Whitespace_and_line_endings_do_not_change_a_signature()
    {
        var a = Entry("Save failed:   the beat\r\nwas locked");
        var b = Entry("Save failed: the beat was locked");

        Assert.That(LogIssueService.SignatureOf(a), Is.EqualTo(LogIssueService.SignatureOf(b)));
    }

    [Test]
    public void Normalise_replaces_volatile_parts_with_placeholders()
    {
        var t = LogIssueService.Normalise(
            "Beat 019d6143-a635-7e53-bc5a-16eb6cfe5941 failed after 812ms");

        Assert.That(t, Is.EqualTo("Beat {guid} failed after {n}ms"),
            "The template is what a human reads when deciding whether two faults are the same.");
    }

    [Test]
    public void Blazor_circuit_ids_collapse_to_one_fault()
    {
        // Taken verbatim from engine\data\logs\log-20260912.txt — the most common error in the
        // real corpus. The circuit id is opaque base64, not a guid, so without a rule for
        // identifier-shaped tokens every crash became its own "distinct fault" and the grouping
        // bought nothing on the one error that actually matters.
        var a = Entry("Unhandled exception in circuit 'z-OMai2TwVd_wLJLaAS2uIV2yYw0tMfznr7d7PZKC9A'.");
        var b = Entry("Unhandled exception in circuit 'afOSprYcdo5xUTHrYV9EXIEypJmIFzIGzTwIkcNe5PE'.");
        var c = Entry("Unhandled exception in circuit 'QJZ-L3E3UmxV98suZkjrZsGw1VxktMDDvpng--nCH5Y'.");

        var sig = LogIssueService.SignatureOf(a);
        Assert.That(LogIssueService.SignatureOf(b), Is.EqualTo(sig));
        Assert.That(LogIssueService.SignatureOf(c), Is.EqualTo(sig));
        Assert.That(LogIssueService.Normalise(a.Message),
            Is.EqualTo("Unhandled exception in circuit '{id}'."));
    }

    [Test]
    public void Long_type_names_are_not_mistaken_for_opaque_ids()
    {
        // The {id} rule requires a digit precisely so ordinary long identifiers survive — a
        // namespaced type name is how you tell two faults apart, not noise to erase.
        const string msg = "Prose.Core.Services.TrinityReconciliationService threw";
        Assert.That(LogIssueService.Normalise(msg), Is.EqualTo(msg));
    }

    [Test]
    public void A_number_that_continues_an_identifier_is_left_alone()
    {
        // Regression: the first rule was \b\d+\b, which cannot match "812" in "812ms" because a
        // digit and a letter share no word boundary — so timings survived and every occurrence
        // became its own fault. The fix must not over-correct and eat the digits in SHA256 either,
        // or unrelated hash/encoding faults collapse into one.
        Assert.That(LogIssueService.Normalise("failed after 812ms"), Is.EqualTo("failed after {n}ms"));
        Assert.That(LogIssueService.Normalise("SHA256 mismatch"), Is.EqualTo("SHA256 mismatch"));
        Assert.That(LogIssueService.Normalise("UTF8 decode failed"), Is.EqualTo("UTF8 decode failed"));
    }

    [Test]
    public void An_empty_message_does_not_throw()
    {
        Assert.DoesNotThrow(() => LogIssueService.SignatureOf(Entry("")));
        Assert.That(LogIssueService.Normalise(null!), Is.Empty);
    }
}
