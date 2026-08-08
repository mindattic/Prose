using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Data.Entities;

namespace Prose.Core.Services;

// ─────────────────────────────────────────────────────────────────────────────
// SurveyService
//
// Persists canon-sync and contradiction-resolution surveys in the DB.
// Replaces hand-authored markdown survey files + hand-coded artifact HTML with
// a single DB-backed service that:
//   1. Creates surveys with typed questions (QuestionType drives apply logic)
//   2. Records answers from the artifact JSON export
//   3. Marks questions applied/skipped after fixes are made
//   4. Generates the interactive artifact HTML from DB data
//
// The apply step is still executed by Claude (SQL / MCP calls) — this service
// provides the question type hint and the answer record. Claude reads both and
// calls the appropriate fix mechanism.
//
// Invocable via:
//   MCP: create_survey, get_survey, list_surveys, answer_survey_question,
//        mark_survey_question_applied, complete_survey, get_survey_html
//   CLI: prose --list-surveys, prose --get-survey <slug>
// ─────────────────────────────────────────────────────────────────────────────

public class SurveyService(IDbContextFactory<ProseDbContext> dbFactory)
{
    static readonly JsonSerializerOptions JsonReadOpts  = new() { PropertyNameCaseInsensitive = true };
    static readonly JsonSerializerOptions JsonWriteOpts = new() { WriteIndented = false };

    // ── Create ────────────────────────────────────────────────────────────────

    public async Task<Survey> CreateSurveyAsync(
        string slug,
        string title,
        string? purpose,
        IReadOnlyList<SurveyQuestionInput> questions,
        Guid? universeId = null,
        CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        if (await db.Surveys.AnyAsync(s => s.Slug == slug, ct))
            throw new InvalidOperationException($"Survey with slug '{slug}' already exists.");

        var survey = new Survey
        {
            Id         = Guid.NewGuid(),
            UniverseId = universeId,
            Slug       = slug.Trim(),
            Title      = title.Trim(),
            Purpose    = purpose?.Trim(),
            Status     = "Open",
            CreatedAt  = DateTime.UtcNow,
        };

        for (int i = 0; i < questions.Count; i++)
        {
            var q = questions[i];
            survey.Questions.Add(new SurveyQuestion
            {
                Id           = Guid.NewGuid(),
                SurveyId     = survey.Id,
                QuestionKey  = q.QuestionKey.Trim(),
                QuestionType = q.QuestionType.Trim(),
                Title        = q.Title.Trim(),
                Context      = q.Context?.Trim(),
                OptionsJson  = JsonSerializer.Serialize(q.Options, JsonWriteOpts),
                SortOrder    = i,
            });
        }

        db.Surveys.Add(survey);
        await db.SaveChangesAsync(ct);
        return survey;
    }

    // ── Read ──────────────────────────────────────────────────────────────────

    public async Task<Survey?> GetSurveyAsync(string slug, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.Surveys
            .AsNoTracking()
            .Include(s => s.Questions.OrderBy(q => q.SortOrder))
            .FirstOrDefaultAsync(s => s.Slug == slug, ct);
    }

    public async Task<IReadOnlyList<Survey>> ListSurveysAsync(
        string? status = null, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var q = db.Surveys.AsNoTracking()
            .Include(s => s.Questions)
            .OrderByDescending(s => s.CreatedAt)
            .AsQueryable();
        if (!string.IsNullOrWhiteSpace(status))
            q = q.Where(s => s.Status == status);
        return await q.ToListAsync(ct);
    }

    // ── Answer ────────────────────────────────────────────────────────────────

    public async Task<bool> RecordAnswerAsync(
        string surveySlug,
        string questionKey,
        string selectedOption,
        CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var question = await db.SurveyQuestions
            .Include(q => q.Survey)
            .FirstOrDefaultAsync(q =>
                q.Survey.Slug == surveySlug &&
                q.QuestionKey == questionKey, ct);
        if (question is null) return false;

        question.SelectedOption = selectedOption.Trim().ToLowerInvariant();
        question.AnsweredAt     = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return true;
    }

    // ── Apply / Complete ──────────────────────────────────────────────────────

    public async Task<bool> MarkQuestionAppliedAsync(
        string surveySlug,
        string questionKey,
        string applyNotes,
        string applyStatus = "Applied",
        CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var question = await db.SurveyQuestions
            .Include(q => q.Survey)
            .FirstOrDefaultAsync(q =>
                q.Survey.Slug == surveySlug &&
                q.QuestionKey == questionKey, ct);
        if (question is null) return false;

        question.ApplyStatus = applyStatus;
        question.ApplyNotes  = applyNotes.Trim();
        question.AppliedAt   = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> CompleteSurveyAsync(string slug, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var survey = await db.Surveys.FirstOrDefaultAsync(s => s.Slug == slug, ct);
        if (survey is null) return false;

        survey.Status      = "Completed";
        survey.CompletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return true;
    }

    // ── HTML generation ───────────────────────────────────────────────────────

    public async Task<string> GenerateHtmlAsync(string slug, CancellationToken ct = default)
    {
        var survey = await GetSurveyAsync(slug, ct)
            ?? throw new InvalidOperationException($"Survey '{slug}' not found.");
        return GenerateHtml(survey);
    }

    public string GenerateHtml(Survey survey)
    {
        var questions = survey.Questions.OrderBy(q => q.SortOrder).ToList();
        int n = questions.Count;
        var sb = new StringBuilder();

        // Head + CSS
        sb.Append("<title>").Append(He(survey.Title)).AppendLine("</title>");
        sb.AppendLine(SurveyCss);

        // Wrapper + header
        sb.AppendLine("<div class=\"container\">");
        sb.AppendLine("  <header class=\"header\">");
        sb.AppendLine("    <div class=\"eyebrow\">Prose · Canon Sync</div>");
        sb.Append("    <h1>").Append(He(survey.Title)).AppendLine("</h1>");
        sb.AppendLine("    <div class=\"header-meta\">");
        sb.Append("      <span>").Append(DateTime.UtcNow.ToString("yyyy-MM-dd")).AppendLine("</span>");
        sb.Append("      <span>").Append(n).Append(" question").Append(n != 1 ? "s" : "").AppendLine("</span>");
        sb.AppendLine("    </div>");
        sb.AppendLine("    <div class=\"progress-bar\"><div class=\"progress-fill\" id=\"pf\"></div></div>");
        sb.Append("    <div class=\"progress-label\"><span id=\"pc\">0</span> of ").Append(n).AppendLine(" answered</div>");
        sb.AppendLine("  </header>");

        // Question cards
        foreach (var q in questions)
            RenderQuestion(sb, q, ParseOptions(q.OptionsJson));

        // Export section
        sb.AppendLine("  <div class=\"export-section\">");
        sb.AppendLine("    <button class=\"export-btn\" id=\"ebtn\" onclick=\"doExport()\" disabled>Export Answers</button>");
        sb.Append("    <div class=\"export-status\" id=\"estat\">Answer all ").Append(n)
          .Append(" question").Append(n != 1 ? "s" : "").AppendLine(" to export</div>");
        sb.AppendLine("    <div id=\"json-out\" style=\"display:none; width:100%; margin-top:12px;\">");
        sb.AppendLine("      <div style=\"font-size:12px; color:var(--text-muted); margin-bottom:6px; letter-spacing:0.04em; text-transform:uppercase; font-weight:600;\">Copy and paste this back:</div>");
        sb.AppendLine("      <textarea id=\"json-text\" readonly onclick=\"this.select()\" style=\"width:100%; height:160px; font-family:'Courier New',monospace; font-size:12px; background:var(--surface-alt); color:var(--text-primary); border:1px solid var(--border); border-radius:4px; padding:10px 12px; resize:vertical; line-height:1.5;\"></textarea>");
        sb.AppendLine("      <button onclick=\"copyJson()\" style=\"margin-top:8px; background:var(--surface-alt); color:var(--accent-text); border:1px solid var(--border); padding:6px 16px; border-radius:4px; font-size:13px; font-weight:600; cursor:pointer; font-family:inherit;\" id=\"copybtn\">Copy to clipboard</button>");
        sb.AppendLine("    </div>");
        sb.AppendLine("  </div>");
        sb.AppendLine("</div>");

        // JavaScript
        sb.AppendLine("<script>");
        sb.AppendLine("  const ans = {};");
        sb.Append("  const N = ").Append(n).AppendLine(";");
        sb.Append("  const SURVEY_SLUG = '").Append(He(survey.Slug)).AppendLine("';");
        sb.AppendLine(@"  function pick(q, v, el) {
    ans['q-' + q] = v;
    document.querySelectorAll('[name=""q' + q + '""]').forEach(r => r.closest('.opt').classList.remove('selected'));
    el.closest('.opt').classList.add('selected');
    const done = Object.keys(ans).length;
    document.getElementById('pc').textContent = done;
    document.getElementById('pf').style.width = (done / N * 100) + '%';
    const btn = document.getElementById('ebtn');
    const st = document.getElementById('estat');
    if (done === N) {
      btn.disabled = false;
      st.textContent = 'All questions answered — ready to export';
    } else {
      btn.disabled = true;
      const rem = N - done;
      st.textContent = rem + ' question' + (rem !== 1 ? 's' : '') + ' remaining';
    }
  }
  function doExport() {
    const json = JSON.stringify({ survey: SURVEY_SLUG, answers: ans }, null, 2);
    document.getElementById('json-text').value = json;
    document.getElementById('json-out').style.display = 'block';
    document.getElementById('estat').textContent = 'Copy the JSON below and paste it back:';
  }
  function copyJson() {
    const ta = document.getElementById('json-text');
    ta.select();
    document.execCommand('copy');
    document.getElementById('copybtn').textContent = 'Copied!';
    setTimeout(() => { document.getElementById('copybtn').textContent = 'Copy to clipboard'; }, 2000);
  }");
        sb.AppendLine("</script>");

        return sb.ToString();
    }

    static void RenderQuestion(StringBuilder sb, SurveyQuestion q, List<SurveyOptionDto> options)
    {
        sb.AppendLine("  <div class=\"question\">");
        sb.AppendLine("    <div class=\"q-header\">");
        sb.Append("      <div class=\"q-tag\">").Append(He(q.QuestionKey))
          .Append(" · ").Append(He(q.QuestionType)).AppendLine("</div>");
        sb.Append("      <div class=\"q-title\">").Append(He(q.Title)).AppendLine("</div>");
        sb.AppendLine("    </div>");

        if (!string.IsNullOrWhiteSpace(q.Context))
        {
            sb.AppendLine("    <div class=\"q-body\">");
            sb.Append("      <p class=\"q-context\">").Append(He(q.Context)).AppendLine("</p>");
            sb.AppendLine("    </div>");
        }

        var qKey = q.QuestionKey.Replace("-", "").Replace(" ", "").ToLowerInvariant();
        var qNum = q.QuestionKey.Replace("Q-", "").Replace("q-", "");

        sb.AppendLine("    <div class=\"options\">");
        foreach (var opt in options)
        {
            sb.Append("      <label class=\"opt\" id=\"o").Append(qKey).Append(He(opt.Key)).AppendLine("\">");
            sb.Append("        <input type=\"radio\" name=\"").Append(qKey)
              .Append("\" value=\"").Append(He(opt.Key))
              .Append("\" onchange=\"pick('").Append(He(qNum))
              .Append("','").Append(He(opt.Key)).AppendLine("',this)\">");
            sb.AppendLine("        <span class=\"opt-dot\"></span>");
            sb.Append("        <span class=\"opt-key\">").Append(He(opt.Key)).AppendLine("</span>");
            sb.Append("        <span class=\"opt-text\">").Append(He(opt.Label));
            if (!string.IsNullOrWhiteSpace(opt.Description))
                sb.Append(" <em>").Append(He(opt.Description)).Append("</em>");
            sb.AppendLine("</span>");
            sb.AppendLine("      </label>");
        }
        sb.AppendLine("    </div>");
        sb.AppendLine("  </div>");
    }

    static List<SurveyOptionDto> ParseOptions(string json)
    {
        try { return JsonSerializer.Deserialize<List<SurveyOptionDto>>(json, JsonReadOpts) ?? []; }
        catch { return []; }
    }

    static string He(string? s) =>
        s is null ? "" : s
            .Replace("&", "&amp;").Replace("<", "&lt;")
            .Replace(">", "&gt;").Replace("\"", "&quot;");

    // CSS is a plain verbatim string — no interpolation, no brace-escaping issues.
    const string SurveyCss = @"<style>
  :root {
    --bg: #EDEEF2; --surface: #FFFFFF; --surface-alt: #F4F5F9;
    --border: #D0D3DC; --border-strong: #B0B5C3;
    --text-primary: #1B202E; --text-secondary: #5C6275; --text-muted: #8A90A0;
    --accent: #3355CC; --accent-light: #E8EDF9; --accent-text: #2244AA;
    --selected-bg: #EEF2FC; --selected-border: #3355CC;
    --mono: 'Courier New', 'Lucida Console', monospace;
  }
  @media (prefers-color-scheme: dark) {
    :root {
      --bg: #0D1019; --surface: #171C2E; --surface-alt: #1E2438;
      --border: #2C3248; --border-strong: #3D4560;
      --text-primary: #E2E6F0; --text-secondary: #8A93AA; --text-muted: #5C6480;
      --accent: #6B8FFF; --accent-light: #1C254A; --accent-text: #8AABFF;
      --selected-bg: #1A2444; --selected-border: #6B8FFF;
    }
  }
  :root[data-theme=""dark""] {
    --bg: #0D1019; --surface: #171C2E; --surface-alt: #1E2438;
    --border: #2C3248; --border-strong: #3D4560;
    --text-primary: #E2E6F0; --text-secondary: #8A93AA; --text-muted: #5C6480;
    --accent: #6B8FFF; --accent-light: #1C254A; --accent-text: #8AABFF;
    --selected-bg: #1A2444; --selected-border: #6B8FFF;
  }
  :root[data-theme=""light""] {
    --bg: #EDEEF2; --surface: #FFFFFF; --surface-alt: #F4F5F9;
    --border: #D0D3DC; --border-strong: #B0B5C3;
    --text-primary: #1B202E; --text-secondary: #5C6275; --text-muted: #8A90A0;
    --accent: #3355CC; --accent-light: #E8EDF9; --accent-text: #2244AA;
    --selected-bg: #EEF2FC; --selected-border: #3355CC;
  }
  *, *::before, *::after { box-sizing: border-box; margin: 0; padding: 0; }
  body { background: var(--bg); color: var(--text-primary); font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', system-ui, Helvetica, Arial, sans-serif; font-size: 15px; line-height: 1.6; padding: 40px 20px 80px; }
  .container { max-width: 720px; margin: 0 auto; }
  .header { margin-bottom: 36px; }
  .eyebrow { font-size: 11px; font-weight: 700; letter-spacing: 0.12em; text-transform: uppercase; color: var(--accent); margin-bottom: 10px; }
  .header h1 { font-family: Georgia, 'Times New Roman', serif; font-size: 26px; font-weight: normal; line-height: 1.25; text-wrap: balance; margin-bottom: 10px; }
  .header-meta { font-size: 13px; color: var(--text-muted); display: flex; flex-wrap: wrap; gap: 14px; margin-bottom: 18px; }
  .progress-bar { height: 3px; background: var(--border); border-radius: 2px; overflow: hidden; }
  .progress-fill { height: 100%; background: var(--accent); border-radius: 2px; width: 0%; transition: width 0.3s ease; }
  .progress-label { margin-top: 6px; font-size: 12px; color: var(--text-muted); font-variant-numeric: tabular-nums; }
  .question { background: var(--surface); border: 1px solid var(--border); border-radius: 6px; margin-bottom: 18px; overflow: hidden; }
  .q-header { padding: 18px 22px 16px; border-bottom: 1px solid var(--border); }
  .q-tag { font-size: 10.5px; font-weight: 700; letter-spacing: 0.1em; text-transform: uppercase; color: var(--accent-text); background: var(--accent-light); padding: 2px 8px; border-radius: 3px; display: inline-block; margin-bottom: 9px; font-family: var(--mono); }
  .q-title { font-family: Georgia, 'Times New Roman', serif; font-size: 16.5px; font-weight: normal; line-height: 1.35; text-wrap: balance; }
  .q-body { padding: 16px 22px 0; }
  .q-context { font-size: 13.5px; color: var(--text-secondary); line-height: 1.65; margin-bottom: 12px; }
  .options { display: flex; flex-direction: column; gap: 7px; padding: 14px 22px 20px; }
  .opt { display: flex; align-items: flex-start; gap: 10px; padding: 10px 13px; border: 1px solid var(--border); border-radius: 4px; cursor: pointer; transition: border-color 0.12s, background 0.12s; border-left-width: 3px; border-left-color: transparent; }
  .opt:hover { border-color: var(--border-strong); background: var(--surface-alt); border-left-color: transparent; }
  .opt.selected { border-color: var(--selected-border); background: var(--selected-bg); border-left-color: var(--selected-border); }
  .opt input[type=""radio""] { position: absolute; opacity: 0; width: 0; height: 0; }
  .opt-dot { flex-shrink: 0; width: 15px; height: 15px; border: 2px solid var(--border-strong); border-radius: 50%; margin-top: 3px; display: flex; align-items: center; justify-content: center; transition: border-color 0.12s; }
  .opt.selected .opt-dot { border-color: var(--accent); background: var(--accent); }
  .opt.selected .opt-dot::after { content: ''; width: 5px; height: 5px; border-radius: 50%; background: #fff; }
  .opt-key { flex-shrink: 0; font-family: var(--mono); font-size: 11.5px; font-weight: 700; color: var(--accent-text); width: 14px; margin-top: 2px; }
  .opt-text { font-size: 13.5px; color: var(--text-primary); line-height: 1.5; }
  .opt-text em { font-style: normal; color: var(--text-secondary); font-size: 13px; }
  .export-section { margin-top: 28px; display: flex; flex-direction: column; align-items: center; gap: 10px; }
  .export-btn { background: var(--accent); color: #fff; border: none; padding: 10px 26px; font-size: 14px; font-weight: 600; border-radius: 5px; cursor: pointer; letter-spacing: 0.02em; transition: opacity 0.12s; font-family: inherit; }
  .export-btn:hover:not(:disabled) { opacity: 0.85; }
  .export-btn:disabled { opacity: 0.35; cursor: not-allowed; }
  .export-btn:focus-visible { outline: 2px solid var(--accent); outline-offset: 3px; }
  .export-status { font-size: 13px; color: var(--text-muted); }
  @media (prefers-reduced-motion: reduce) { * { transition: none !important; } }
</style>";
}

// ── DTOs ──────────────────────────────────────────────────────────────────────

public sealed record SurveyOptionDto(string Key, string Label, string? Description = null);

public sealed record SurveyQuestionInput(
    string QuestionKey,
    string Title,
    IReadOnlyList<SurveyOptionDto> Options,
    string? Context = null,
    string QuestionType = "Custom");
