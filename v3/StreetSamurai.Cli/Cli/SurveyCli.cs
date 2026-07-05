using Microsoft.Extensions.DependencyInjection;
using StreetSamurai.Core.Services;

namespace StreetSamurai.Cli;

/// <summary>
/// Survey management CLI commands.
///
/// <code>
/// ss --list-surveys [--status Open|Completed]
/// ss --get-survey --slug &lt;slug&gt;
/// </code>
/// </summary>
public static class SurveyCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        var svc = services.GetRequiredService<SurveyService>();

        if (args.Contains("--list-surveys"))
            return await ListAsync(args, svc);

        if (args.Contains("--get-survey"))
            return await GetAsync(args, svc);

        Console.Error.WriteLine("[survey] Unknown command. Use --list-surveys or --get-survey --slug <slug>.");
        return 1;
    }

    // ── --list-surveys ──────────────────────────────────────────────────────

    static async Task<int> ListAsync(string[] args, SurveyService svc)
    {
        var status = Flag(args, "--status");
        var surveys = await svc.ListSurveysAsync(status);

        if (surveys.Count == 0)
        {
            Console.WriteLine(status is null
                ? "[surveys] No surveys found."
                : $"[surveys] No {status.ToLower()} surveys found.");
            return 0;
        }

        Console.WriteLine($"{"Slug",-40} {"Status",-12} {"Q",-4} {"Ans",-4} {"Applied",-8} Created");
        Console.WriteLine(new string('-', 90));
        foreach (var s in surveys)
        {
            int total   = s.Questions.Count;
            int ans     = s.Questions.Count(q => q.SelectedOption != null);
            int applied = s.Questions.Count(q => q.ApplyStatus == "Applied");
            Console.WriteLine($"{s.Slug,-40} {s.Status,-12} {total,-4} {ans,-4} {applied,-8} {s.CreatedAt:yyyy-MM-dd}");
        }
        return 0;
    }

    // ── --get-survey --slug <slug> ──────────────────────────────────────────

    static async Task<int> GetAsync(string[] args, SurveyService svc)
    {
        var slug = Flag(args, "--slug");
        if (string.IsNullOrWhiteSpace(slug))
        {
            Console.Error.WriteLine("[survey] --slug <slug> is required.");
            return 1;
        }

        var survey = await svc.GetSurveyAsync(slug);
        if (survey is null)
        {
            Console.Error.WriteLine($"[survey] Survey '{slug}' not found.");
            return 1;
        }

        Console.WriteLine($"Survey: {survey.Title}");
        Console.WriteLine($"Slug:   {survey.Slug}  |  Status: {survey.Status}  |  Created: {survey.CreatedAt:yyyy-MM-dd}");
        if (!string.IsNullOrWhiteSpace(survey.Purpose))
            Console.WriteLine($"Purpose: {survey.Purpose}");
        Console.WriteLine();

        foreach (var q in survey.Questions.OrderBy(x => x.SortOrder))
        {
            var answered = q.SelectedOption is not null ? $"→ {q.SelectedOption}" : "(unanswered)";
            var applied  = q.ApplyStatus != "Pending" ? $"[{q.ApplyStatus}]" : "";
            Console.WriteLine($"  {q.QuestionKey}  {answered}  {applied}");
            Console.WriteLine($"      {q.Title}");
            if (!string.IsNullOrWhiteSpace(q.ApplyNotes))
                Console.WriteLine($"      Applied: {q.ApplyNotes}");
        }
        return 0;
    }

    static string? Flag(string[] args, string name)
    {
        var idx = Array.IndexOf(args, name);
        return idx >= 0 && idx + 1 < args.Length ? args[idx + 1] : null;
    }
}
