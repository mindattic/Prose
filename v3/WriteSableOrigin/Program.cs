using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StreetSamurai.Core.Extensions;
using StreetSamurai.Core.Interfaces;
using StreetSamurai.Core.Models;

// ── WriteSableOrigin: one-shot persistence of "The Voice You Trust" ────────
// Creates a 5-chapter Sable origin book in the StreetSamurai canon database
// using the same IBookRepository / IChapterRepository the Blazor UI and MCP
// server use. Idempotent on the deterministic book id below — re-running
// updates the existing record rather than spawning duplicates.

const string BookId = "5ab1e000000000000000000000000001"; // stable identifier
var chapterIds = new[]
{
    "5ab1ec01000000000000000000000001",
    "5ab1ec02000000000000000000000002",
    "5ab1ec03000000000000000000000003",
    "5ab1ec04000000000000000000000004",
    "5ab1ec05000000000000000000000005",
};

var services = new ServiceCollection();
services.AddLogging(b =>
{
    b.SetMinimumLevel(LogLevel.Information);
    b.AddSimpleConsole(o =>
    {
        o.SingleLine = true;
        o.TimestampFormat = "HH:mm:ss ";
    });
});
services.AddStreetSamuraiServices();

using var sp = services.BuildServiceProvider();
var books = sp.GetRequiredService<IBookRepository>();
var chapters = sp.GetRequiredService<IChapterRepository>();

Console.WriteLine("=== WriteSableOrigin: The Voice You Trust ===");

var book = new Book
{
    Id           = BookId,
    Title        = "The Voice You Trust",
    Tagline      = "Before she was Sable.",
    Premise      = "Before she was Sable — the fixer in the tan coat with her hands in the pockets — she was Rhea Adesanya-MacGregor, a Tier 3+ media personality on a five-year track to a Tier 4 anchor seat. Then her network was acquired, she was reassigned, she found something she should not have found, and the company arranged her exit. The exit took her eyes. The black-market clinic gave her new ones. The tan coat she bought on the walk home was not a costume. It was the closing entry in a five-year ledger of dignified mistakes.",
    ArcTarget    = "Ends with Sable standing across State and Madison from the building where Tessera Media Group used to be, hands in the pockets of a tan coat she had owned for forty minutes, counting one hour by the internal clock of paired ocular implants she had owned for two. Establishes the annual visit, the refusal of contact, and the operating posture that every later Sable chapter inherits.",
    Status       = "drafting",
    Protagonists = new() { "Sable" },
    ChapterIds   = new(chapterIds),
    StateAtEnd   = new BookState
    {
        InWorldTime    = "Late autumn 2217, approximately five years before A Restless Mind.",
        CharacterStatus = new()
        {
            ["Sable"] = "Newly named. Paired Aurum Spec-7 ocular implants. Tan coat purchased same day. Operating from the Circuit out of Yelena Chen-Okafor's network. No fixed residence. Φ180,000 paid off; first contract closed. Has read the OPTIC-7 case study and locked her copy in a Pacific Edge Mutual safe-deposit box in the Glooms (cipher-encoded thermal paper, 11-year prepaid).",
        },
        OpenThreads = new()
        {
            "The Pacific Edge Mutual safe-deposit box in the Glooms, paid through to 2228. Cipher-encoded thermal copies of the OPTIC-7 case study and Z3 deployment log.",
            "Esperanza Halberd-Iwu at Open Eyes received an anonymous tip with the OPTIC-7 lot number and four medical-record case identifiers. Sable has never confirmed receipt and has never made contact again.",
            "The Aurum-line implants Axiom expected her to take talk to Axiom Health Sciences' backend. Sable is carrying Helix Biosystems hardware that does not. The discrepancy is, by definition, the kind of thing Axiom does not yet know it has lost.",
            "The annual hour at State and Madison begins in 2217. She has not yet missed one.",
        },
        CanonChanges = new()
        {
            "Rhea Adesanya-MacGregor is now operating under the name Sable.",
            "Dr. Adaeze Kovalenko-Hassan's basement clinic in the Circuit is established as the black-market ocular surgery Sable will later return to on others' behalf.",
            "Yelena Chen-Okafor is established as the prior-generation fixer who first put Sable on the freelance roster. (Retires eleven years later and passes her network forward.)",
            "Tessera Media Group's State-and-Madison building has been converted to 'Forty-Seven North,' an upscale restaurant. This is the building Sable visits annually.",
        },
    },
};
books.SaveBook(book);
Console.WriteLine($"Saved book: {book.Title} ({book.Id})");

var ch1 = new Chapter
{
    Id         = chapterIds[0],
    BookId     = BookId,
    Number     = 1,
    Title      = "The Voice You Trust",
    Status     = "draft",
    Characters = new() { "Sable" },
    Synopsis   = "October 2216. Rhea Adesanya-MacGregor is twenty-five, a Tier 3+ feature voice at Tessera Media Group, two segments away from a Tier 4 anchor seat. She records a long-form piece on Axiom Industries' Z3 expansion at 0712. By 1015 she is in the eighth-floor conference room hearing the words 'wholly owned subsidiary.' By 1119 she is reading the same segment back into the booth with a new title penciled in by Axiom's brand transition liaison. The first thing she loses is not vision. It is the title she chose.",
    Html       = Html(Prose.Chapter1),
};
chapters.SaveChapter(ch1);
Console.WriteLine($"Saved chapter 1: {ch1.Title} ({ch1.Id})");

var ch2 = new Chapter
{
    Id         = chapterIds[1],
    BookId     = BookId,
    Number     = 2,
    Title      = "Floor 47",
    Status     = "draft",
    Characters = new() { "Sable" },
    Synopsis   = "Eight months in. Rhea is Senior Narrative Architect on Floor 47 of the Meridian Core — Axiom Industries Security Division, Communications Integration. Her apartment on Level 30 has a balcony Tier 4 hires do not get; her office has no window. The work is the conversion of operations reports into stakeholder communications. The cooking starts here. The first small revenge starts here — a plain-paper research memo left on a break-room counter, picked up by an independent outlet, lit a small fire and went out. She decides she can do it again.",
    Html       = Html(Prose.Chapter2),
};
chapters.SaveChapter(ch2);
Console.WriteLine($"Saved chapter 2: {ch2.Title} ({ch2.Id})");

var ch3 = new Chapter
{
    Id         = chapterIds[2],
    BookId     = BookId,
    Number     = 3,
    Title      = "Discrepancies in the Margin",
    Status     = "draft",
    Characters = new() { "Sable" },
    Synopsis   = "Twelve months in. While writing an Axiom 'thought-leadership' white paper on Z3, Rhea reads — through an unattended analyst's clipboard — the OPTIC-7 case study. The Z3 deployment was a controlled trial. Mira Quintero-Bekele, the community organizer, was the priority weighting. Rhea copies the document onto thermal paper in a cipher she has memorized since fourteen, walks it to Pacific Edge Mutual in the Glooms, locks it in a safe-deposit box paid eleven years forward. She drops an anonymous letter to Open Eyes. On Monday her badge declines at the building entrance.",
    Html       = Html(Prose.Chapter3),
};
chapters.SaveChapter(ch3);
Console.WriteLine($"Saved chapter 3: {ch3.Title} ({ch3.Id})");

var ch4 = new Chapter
{
    Id         = chapterIds[3],
    BookId     = BookId,
    Number     = 4,
    Title      = "The Restructuring",
    Status     = "draft",
    Characters = new() { "Sable" },
    Synopsis   = "The voluntary separation conversation. Beatrix Ngalula-Vance from Internal Affairs, a Security Division colleague who does not speak unless Rhea picks the harder version. The package: full severance, sealed NDA, biometric purge — retina, palm, gait, voiceprint, vascular — and a Φ440,000 bearer credit for an Axiom-approved Aurum Tier 3 ocular implant. Rhea reads the third page twice and asks the question on the record. She walks out, sleeps in a Z6 transient hotel, comes back at 0941 the next morning because the contract has a deadline. The procedure is not fifteen minutes. The agent is a derivative of OPTIC-7. She does not scream.",
    Html       = Html(Prose.Chapter4),
};
chapters.SaveChapter(ch4);
Console.WriteLine($"Saved chapter 4: {ch4.Title} ({ch4.Id})");

var ch5 = new Chapter
{
    Id         = chapterIds[4],
    BookId     = BookId,
    Number     = 5,
    Title      = "Sable",
    Status     = "draft",
    Characters = new() { "Sable" },
    Synopsis   = "The basement of a hat-repair shop in the Circuit. Dr. Adaeze Kovalenko-Hassan, sixty-something, tells Rhea exactly what the Axiom-approved Aurum line is: a leash. She offers the Spec-7 instead — paired telescopic ocular, no networked firmware, no biometric handshake, Helix Biosystems tolerance — and holds the procedure open for six weeks while Rhea works off the Φ180,000 difference under Yelena Chen-Okafor. The implants log the city. The tan coat goes on three corridors over. The hour at State and Madison begins. She picks the name at the noodle stall on the corner of Damen and Augusta. The hands stay in the pockets.",
    Html       = Html(Prose.Chapter5),
};
chapters.SaveChapter(ch5);
Console.WriteLine($"Saved chapter 5: {ch5.Title} ({ch5.Id})");

// Verify round-trip.
var reloaded = books.LoadBook(BookId);
Console.WriteLine();
Console.WriteLine($"=== Verification ===");
Console.WriteLine($"Book: {reloaded?.Title}");
Console.WriteLine($"Chapters in order: {reloaded?.ChapterIds.Count}");
foreach (var cid in reloaded?.ChapterIds ?? new())
{
    var c = chapters.LoadChapter(cid);
    Console.WriteLine($"  #{c?.Number}: {c?.Title} ({c?.Html.Length} chars)");
}

return 0;

// Wraps the raw multi-paragraph prose into <p> tags so the chapter renderer
// does not see a single text blob. Empty lines separate paragraphs.
static string Html(string raw)
{
    var paragraphs = raw.Replace("\r\n", "\n").Trim().Split("\n\n", StringSplitOptions.RemoveEmptyEntries);
    return string.Join("\n", paragraphs.Select(p => "<p>" + p.Trim() + "</p>"));
}

internal static class Prose
{

public const string Chapter1 = """
The booth's red light came on at 0712. Rhea had been in the chair since 0654. Eighteen minutes of breath work, a glass of warm water, a black coffee she had not drunk because the floor manager liked her voice better when she was thirsty. The script in front of her was forty-one pages, double-spaced, marked in the producer's blue pencil. The segment was scheduled to run nineteen minutes.

The producer's voice came through the cans. "From the top, when you're ready."

She read the slug. *Axiom Industries — A City Within The City.* The title was hers. She had pushed for it through three rewrites because "A City Within The City" did the work two paragraphs of throat-clearing would otherwise need to do. The corporate-relations liaison had wanted *Axiom Industries — Tomorrow's Civic Architecture.* Rhea had told the producer she would read either, but only one of them would make the audience finish their coffee. The producer had taken the note.

She read.

She read the way Tessera Media Group had trained her to read for five years: the breath landing on the noun, the consonant softened at the end of the clause so the listener wasn't hearing a sentence end, they were hearing the next one start. The Tessera grading system called it *forward lean.* Listener attention spans, modeled and rewarded. *Forward lean,* nineteen minutes uninterrupted.

The piece walked the audience from State and Madison north along the Spine to the Meridian Core, into the Axiom service corridor that had been quietly absorbed from municipal control four years prior, and through the demographic recompositions in the two zones bracketing the new corporate residential block. Rhea did not say *absorbed.* She did not say *displacement.* She said *integration,* and she said it the way her grandmother had said *grace,* with a slight pause before the noun so the listener could hear it land.

At the eleven-minute mark she leaned into the script's only number — *seventeen thousand four hundred residential units, brought to market in eighteen months* — and felt the small physiological adjustment in her sternum that the producer called *the trust.* When she hit a number cleanly the audience trusted her. When she hit the next one cleanly the audience would still be there at minute nineteen. This was the work.

The red light went off at 0747.

The producer keyed the talkback. "Beautiful. We're done."

She did not move. She had learned not to move until the engineer cleared the room. Two of her colleagues had been fined for sneezing into a hot mic.

The director came in. He was wearing the steel-gray suit Tessera had bought him for the network rebrand last spring, with the lapel pin that was just a small circle and not a logo because Tessera had been quiet about its own branding ever since the audience-trust metric began to react negatively to logos. He had a coffee in each hand. He handed her one of them. She took it with her right.

"You have the voice they trust," he said. He said it the way he always said it. He had said it the first time the morning he had hired her. He had said it on the morning her segment had been picked up by the international pool. He was saying it now because he could not get through a recording session without saying it, and Rhea suspected he believed the saying was part of what made it true.

"Thank you, Marcus."

"They're going to call you in this week. Tier four. The desk."

She took a careful sip of the coffee. It was bad coffee. He brought her bad coffee whenever he was about to tell her something she should not yet be told. The good coffee was in the upstairs lounge, and the upstairs lounge was for people who already had the meetings he was hinting at.

"I'm not on the list yet," she said.

"No. But they're going to put you on it."

"Marcus —"

"Don't say no before they ask."

She did not. She drank the bad coffee and read the next slug — a four-minute filler segment on the Pulse's new direct service to Reykjavík. The numbers were boring, the script was boring, the engineering was a marvel, nobody would remember it by 0900. She made the engineering sound like the marvel it was. The red light went off.

At 1015 the floor manager came into the booth and said the morning meeting had been moved to the eighth-floor conference room, and could she stop by. Rhea said she could. The floor manager did not meet her eyes. The floor manager was a woman named Imani who had two daughters, both in the West Lakeshore school district, and who would in fourteen years have lost the school district to a private CorpoNation-academy contract Axiom would write the day before bid lock; on this Tuesday in October, Imani did not look at Rhea because she had been told what the eighth-floor meeting was about, and Imani was not good at small lies.

The eighth-floor conference room held forty people. Tessera had thirty-eight employees on that floor. The two additional chairs were taken by men Rhea did not recognize, and one of them was wearing a watch she had described in a segment four months ago. The segment had been about Axiom Industries' executive culture. The watch was real and worth, she remembered cleanly, four hundred and twenty thousand quanta — Φ420,000 — and the man wearing it was smiling at her as if she had complimented him personally.

The director general of Tessera stood up at the head of the table.

"Good morning, everyone."

He said the words *strategic alignment,* and *long-term stewardship,* and *capital partnership.* He said the words *Axiom Industries* twice, and the second time he said them he was looking at the man with the watch, and the man with the watch was looking at Rhea.

The director general said the words *full acquisition* very carefully, with the *full* landing on the breath, the way Rhea read the noun. The room was silent in the specific way a room is silent when forty people are simultaneously calculating their severance packages. Rhea did the math too. Tessera had been independent for nineteen years. Her contract had four years and seven months left on it.

"Effective immediately," the director general said, "Tessera Media Group is a wholly owned subsidiary of Axiom Industries Communications and Brand. Your contracts roll over without alteration. Your benefits roll over without alteration. Your editorial independence —" and here he paused, the kind of pause Rhea would have used on a noun like *grace,* "— is fully protected under the new affiliation."

The man with the watch was no longer smiling. He was reading Rhea's face. His eyes did the thing eyes do when someone is reading you for a tell and trying to read you for two tells at once and failing at both because the second tell would have required him to know the first one was there.

Rhea reached for her water glass. She drank a small careful sip. She set the glass down at exactly the angle she had set it down at the previous twenty meetings.

She said nothing.

The director general thanked them. The man with the watch stood up to introduce himself as *Axiom's brand transition liaison,* and said that the integration would be *seamless and respectful of the editorial heritage that has made Tessera what it is.* He said *editorial heritage* the way Marcus said *the voice they trust,* with the conviction of a man who had rehearsed a sentence often enough to forget the difference between belief and reflex.

In the elevator back down to the booth level, Marcus stood next to her. He did not look at her either.

"They'll still put you on the list," he said.

"I know," she said.

He did not say *I'm sorry.* He did not say *we should have warned you.* He did not say *I should have warned you.* What he did say, very softly, as the elevator passed the fourth floor:

"You have the voice they trust. They wouldn't have bought us if you didn't."

The elevator stopped. The doors opened.

Rhea walked back to the booth. The script for the next segment was on the chair. It was a six-minute piece on Axiom Industries' Z3 expansion — the same piece she had read at 0712, repurposed into a stand-alone evening slot. The blue pencil had become a red pencil. *Integration* was now *partnership.* *A City Within The City* had been crossed out. The new title, in the man with the watch's slanted hand:

*A Future Worth Building.*

Rhea read the slug. The red light came on. She read the words the way she had been trained to read them, with the breath on the noun and the consonants soft, because she had been trained to read them this way and she had not yet decided to read them any other way.

The red light went off at 1119.

The director said *beautiful* on the talkback.

The man with the watch was standing in the booth control room, watching her through the glass, and when she met his eyes through the glass he smiled at her the way an investor smiles at a holding.
""";

public const string Chapter2 = """
The corporate residential block had a balcony. Most of the units on Level 30 did not. Hers did because she was Tier 3+, and Tier 3+ was the level at which Axiom Industries acknowledged the existence of weather. From the balcony she could see the eight blocks of the Meridian Core that Axiom had quietly absorbed in the four years before her hire, and the two blocks they were absorbing now. The Pulse station roof was visible to the southwest, a slab of brushed graphite the size of a city block, and below it the cross-street where the slugs surfaced and dipped at thirty-second intervals. The thrumline was perceptible through the balcony floor — an eight-hertz vibration that Tier 4 residents were told was *infrastructure,* that Tier 1 residents in the Glooms ten kilometers east called *the city talking to itself.*

Floor 47 of the Meridian Core was Axiom Security Division's Communications Integration department. Rhea's office did not have a window. Tier 3+ at Axiom got the apartment with the balcony. Tier 4 got the office with the window. She understood, the third week, that the apartment had been chosen for her on purpose. The view was the inducement. The office was the work. The work was where the trade was being made.

Her title, in full: Senior Narrative Architect, Communications Integration, Security Division.

The first three weeks of the job had been onboarding. Compliance modules, badge calibration, biometric enrolment — retina, palm, gait, voiceprint, vascular. The biometric tech had been very gentle with her. *We're sorry it's so thorough,* he had said. *It's a security-division standard. The integration runs deep.* She had smiled. She had not asked why a media-side hire needed gait analysis on file. She had drawn the conclusion she was meant to draw.

In the fourth week the work began. The brief was a forty-eight-page operations report from the Z3 expansion. Casualties — *transient morbidity events,* the language read — had been logged at twenty-seven. Equipment expended, four orders of magnitude more. Local sentiment indices, six points down from baseline. Press coverage, two pieces from independent outlets, both flagged for *narrative-pre-emption.*

Her job, as Senior Narrative Architect, was to take this forty-eight-page operations report and produce a three-paragraph stakeholder communication that turned twenty-seven transient morbidity events into *a measured public-health response in a complex environmental context.* The phrase *transient morbidity event* would not appear in the communication. Neither would the phrase *public-health response.* The communication would use the word *resilience* twice, and the word *partnership* once, and would not contain a single number larger than nineteen, because Axiom's internal modeling had established that numbers larger than nineteen produced negative sentiment in non-finance audiences.

She wrote the communication in eleven minutes. The blue-pencil markups came back from her director in nine. She had used the word *complex* twice. The director's note: *We use complex once per page. Twice is permission.*

She rewrote the second *complex* as *layered.* The communication shipped.

She went home and cooked. The corporate apartment had a kitchen larger than her parents' bedroom had been in Glasgow. She had learned to cook before she had learned to read, from a Yoruba grandmother and a Scottish mother who had each insisted the other's tradition would not win at the table. The truce had been *both, in sequence,* and Rhea had inherited the truce. She made a beef stew over rice, with smoked paprika and a finishing splash of palm oil because the grandmother had won on aromatics, and bread on the side because the mother had won on starch. She ate it standing at the counter. She did not turn on the news.

The director's note on her work, after the third communication: *You have the voice they trust.* He said it warmly. He said it the way Marcus had said it.

She began, in the fifth week, to do small things.

She read the operations reports more carefully than she needed to. The reports referenced source documents — chemical inventories, deployment logs, transit movement orders — and the source documents lived on a parallel directory she was not credentialed to read. She read them anyway. The credential system was rigorous about logging *access denied;* it was less rigorous about logging *access by proximity,* which was the term for what happened when an analyst's clipboard, left open on the shared workstation in the seventh-floor break room, contained an inherited token from a directory she would not otherwise have seen. The break room workstation was, she had noticed her second week, the most-trafficked machine on the floor and the least-watched.

She read about the Z3 deployment. *Twenty-seven transient morbidity events* meant twenty-seven people had walked into a Sunday-evening community assembly in Pilsen and walked out, three hours later, into an emergency-medical intake at the Aurochs trauma complex with chemical exposure to the eyes. Twelve of them recovered fully within nineteen hours. Eleven of them required eighteen weeks of cornea-regen therapy at out-of-pocket cost. Four of them did not recover. The official record, generated by a third-party medical contractor whose contract was held by Axiom's Health and Safety Division, listed the event as a *stress-related amnestic episode with associated transient visual disturbance.* The community organizer who had called the assembly, a fifty-two-year-old retired transit dispatcher named Mira Quintero-Bekele, had been one of the four who did not recover.

The chemical agent, identified by lot number in the deployment log, was a Tier-restricted ophthalmic disruptor. It was not in commercial production. It was manufactured for one client. The client's name was on the lot label. The client was Axiom Industries Security Division.

Rhea did not write any of this down. She read it the way a journalist reads a document, which is to say she read it once, fully, then closed the directory, then walked back to her office, then sat in her chair for nine minutes, then went home and cooked yam pottage because the grandmother had wanted yam pottage that night.

In the seventh week she did the first thing.

She had a contact at one of the two independent outlets that had filed coverage on the Z3 expansion. The contact's name was Olu Ferrara. He had been a colleague at Tessera, before the acquisition, before he had walked into the director general's office on the morning of the announcement and resigned in writing, with cause, citing editorial independence. He had been one of the four people Rhea had envied for a single afternoon. She had not contacted him since.

She drafted an internal memo, on her own workstation, on her own time, using a research template that was a permitted format for narrative-architecture work. The memo summarized — at the level a Senior Narrative Architect would summarize, for a Senior Narrative Architect's own files — the operational sequencing of the Z3 expansion. Sequencing only. No source documents. No chemical inventories. No casualty figures. The kind of memo that would, if read by an outside journalist with a competent map of the Meridian Core's deployment patterns, raise three specific questions about three specific timestamps.

She printed the memo on plain paper. She walked it to the seventh-floor break room. She left it on the counter beside the coffee station, face-up, in the section of counter where colleagues routinely left documents they were too lazy to file. She walked back to her office. She did not look back.

Forty-eight hours later, the independent outlet ran a six-hundred-word piece. The piece was careful. The piece named no source. The piece was titled *Three Questions Axiom Has Not Answered About Z3.* The third question was correct enough to be dangerous and vague enough to be deniable.

The director called her into his office on Monday morning. He had the piece on his screen.

"Have you seen this?"

"I have."

"What's your read?"

"It's careful work. They don't have the documents. They're guessing on the sequencing and they're guessing well. Whoever they have inside isn't an analyst — the questions aren't technical enough. It's someone in operations, mid-level, with access to scheduling."

The director nodded. "That's my read too."

"Do you want me to do a counter-piece?"

"Not yet. We're going to let them ask. The right response right now is silence and a slightly improved community-relations outreach in Z3. Draft me three paragraphs for the local liaison."

"Yes."

She walked back to her office. She drafted the three paragraphs in fourteen minutes. The director's blue pencil came back in nineteen. The communication shipped. The independent outlet ran a follow-up on Friday, less careful than the first piece, citing a source the outlet did not have. The piece died in three news cycles.

Rhea cooked that night. Yam pottage again. The grandmother had wanted it twice in one week.

She did not feel proud. She did not feel guilty. She felt, for the first time since the acquisition, that she had not lost something she had not already chosen to lose. The voice they trusted was still in her throat. It was still working. It was now working slightly more for her than for them. The trade was small. The trade was real. The trade was, she understood at the kitchen counter with the spoon in her hand, the only trade Floor 47 was going to offer her.

She would learn to take it.
""";

public const string Chapter3 = """
The white paper was forty-eight pages. It was due Friday.

Rhea had been writing it for eleven days. The first three days had been the easy part — the framing, the *executive context,* the careful repurposing of the operations report's least dangerous numbers into a piece of Axiom-published *thought leadership* that would be quoted by three universities and one corporate-policy journal in the eighteen months following its release. She had written the executive context in four hours. The director had blue-pencilled it twice in three hours. The framing was, by the end of day three, finalized.

Days four through eleven were the body. The body required source verification. Source verification meant directory access. Directory access, for the white paper, was supposed to come through the analyst on her team — Joaquim, a year younger than her, two tiers junior, with a clipboard he was very careless about and a habit of leaving his workstation unlocked when he went to the seventh-floor break room.

She had not asked Joaquim for access. She had read his clipboard in his absences. This was, she had decided in the seventh week, a Senior Narrative Architect's reasonable use of available materials.

On day nine she found the agent specification.

The specification was a twelve-page technical document on Axiom Health Sciences letterhead. It was titled *OPTIC-7: Targeted Ophthalmic Disruptor for Crowd-Density Disengagement Operations.* The author's name had been redacted in the standard fourteen-pixel black bar. The document's body had not been redacted. The document's body included a dosing chart, a tissue-recovery curve, and a two-page case-study appendix.

The case study was the Z3 deployment.

The case study used the same phrase Axiom's external medical contractor had used in the public record: *stress-related amnestic episode.* The case study referred to this phrase as the *post-event reporting framework.* The case study noted, in a footnote, that the framework had been *pre-coordinated with regional clinical partners in the eight weeks prior to the operation,* with the partners — Aurochs Trauma Complex among them — *briefed on the expected symptomology, the appropriate intake categorization, and the appropriate billing classification.*

The case study identified the operation's *target population* as *the attendee cohort at the community assembly convened by M. Quintero-Bekele on 11 March 2217, with priority weighting applied to M. Quintero-Bekele herself.*

Priority weighting.

Rhea read the footnote three times. The case study did not say *kill Mira Quintero-Bekele.* The case study said *priority weighting,* which meant the agent had been dosed, in the target zone, in a concentration calibrated to produce maximum tissue damage at the location of one specific attendee. Mira had been standing, the deployment log noted, two and a half meters from the assembly's improvised podium. The dosing concentration at two and a half meters from the podium had been four-point-eight times the median concentration in the rest of the room.

Mira had been one of the four who did not recover.

Rhea closed the document. She closed Joaquim's clipboard. She locked his workstation, the way she had learned to lock it for him, so he would not return from the break room and find his terminal compromised by someone other than herself. She walked back to her office. She did not sit down. She stood at the closed door and counted her breaths, the way she had been taught to count them at Tessera before a long-form segment, eighteen counts in, eighteen counts out, until the sternum did the small physiological adjustment the producer had called *the trust.*

The trust did not arrive.

She sat down anyway. She opened the white paper draft. She wrote, with the cursor steady, four paragraphs of executive context that would not have shamed her at Tessera. The director would blue-pencil them gently. The white paper would ship Friday on time.

She went home. She did not cook. She drank a glass of cold water from the tap and stood at the balcony and watched the Pulse station's slugs surface and dip at thirty-second intervals. The thrumline was perceptible through the balcony floor. *The city talking to itself.* The Glooms ten kilometers east. The Tier 1 bank she would, in four days, walk into for the first time.

The bank was called Pacific Edge Mutual. It did not advertise. It served the Tier 1 residents the CorpoNations did not. Its lobby was three meters wide and lit by a single yellowed fixture. Its safe-deposit boxes were the smallest size the city's deposit-box industry made. She rented a box for eleven years' prepaid, paid in cash, and gave a name that was not Rhea Adesanya-MacGregor and was not yet Sable.

She had spent two days, before walking to the bank, copying the OPTIC-7 document onto thermal paper in a cipher of her own design. The cipher was simple — a transposition with a seed she had memorized at fourteen, from a children's book her grandmother had loved. The transposition would not survive a serious cryptanalytic attack. It would survive a casual one. It would survive long enough for her to be elsewhere by the time anyone found the box.

She put the thermal sheets in the box. She locked the box. She walked back to the Meridian Core through three transit transfers, two of which were unnecessary, both of which would have appeared to a tail as the small inefficiencies of an unconfident commuter rather than the surveillance-shedding pattern they were. She had read about this pattern in a Tessera segment four years ago, on freelance journalists in the Andean Remnants. She had remembered the pattern. She had not expected to be using it for herself.

The advocacy group's name was Open Eyes. It operated out of a converted bakery in the Glooms. Its lawyer was a woman named Esperanza Halberd-Iwu, who had been disbarred by the Axiom-affiliated bar association nineteen years ago for representing a Z3 plaintiff in a class action that had ended in a six-figure settlement and a permanent injunction Axiom had quietly violated within eight months. Esperanza was a person Rhea had interviewed once, at Tessera, for a piece that had not aired because Marcus had taken the piece off the schedule two days before broadcast.

Rhea did not contact Esperanza directly. She wrote a letter, on plain paper, in a handwriting she had practiced for an hour at the kitchen counter to make non-attributable, and dropped the letter in a public mailbox in a Glooms neighborhood her transit history would not show her visiting. The letter contained the OPTIC-7 lot number, the four medical-record case identifiers from Aurochs, and a single sentence: *The 11 March stress-related amnestic episode at the Pilsen community assembly was a controlled deployment of OPTIC-7, Axiom Industries Security Division, with priority weighting applied to the convener.*

She mailed the letter on Wednesday.

On Friday morning, the white paper shipped. The director thanked her. He said the words *exceptional work,* and the words *the kind of analysis we hire for.* He gave her bad coffee. Marcus, she remembered briefly, had given her bad coffee the morning he had told her she would be put on the Tier 4 list. Marcus had not, in fourteen months at Axiom, called her once.

On Monday morning her badge declined at the building entrance.

It declined twice. The guard — a young man whose nameplate read *Hartfield,* whose grandmother, Rhea would learn nine years later in an unrelated context, had also lived in the Glooms — keyed the override on the third attempt, smiled apologetically, and said the system was running slow. He waved her through.

She got to her office. Her workstation was on. Her email was on. Her email had one new message. The message was from Internal Affairs, Communications Integration, Security Division. The subject line was a routing code she had not seen before. The body was four sentences. The four sentences invited her to a meeting in the eighth-floor conference room at 1415.

She did not eat lunch. She did not cook the next night. She went to the meeting at 1414.
""";

public const string Chapter4 = """
The eighth-floor conference room held forty people. The morning the acquisition had been announced, fourteen months earlier, it had held thirty-eight Tessera employees and the man with the watch. This afternoon it held two: a woman named Beatrix Ngalula-Vance from Internal Affairs, and a Security Division colleague whose face Rhea remembered from the biometric enrolment in week three. He had not been gentle that day. He was not gentle today. He was wearing a different watch.

Beatrix offered Rhea coffee. Rhea declined. Beatrix asked her to sit. Rhea sat.

"Rhea," Beatrix said. "Thank you for coming. This is going to be a short conversation."

"All right."

"We have a discrepancy in our access logs."

Rhea did not move. She had counted her breaths in the elevator. The trust had not arrived in the elevator either, but the breath count had held.

"I see."

"On the afternoon of October 22 of last year, you accessed an OPTIC-7 case-study document from a workstation in the seventh-floor break room. You don't have credentials for that document. The document was open on the workstation because the credentialed user — analyst Joaquim Da Silva-Lerner — had stepped away with his clipboard token unlocked. You read the document for, by our logs, nineteen minutes. You re-locked Joaquim's workstation before you left."

"All right."

"We've been watching for a while."

"All right."

Beatrix folded her hands. The Security Division colleague did not move. He had not yet spoken. Rhea understood that he would not speak unless the conversation went a different direction than it was going, and that the conversation going a different direction was the thing the room was designed to make her not do.

"Rhea, we are not going to bring legal action."

"All right."

"Axiom Industries values the work you have done in Communications Integration. The work is exceptional. We do not want a public separation. We do not want a contested separation. We want a clean separation, with full severance, with a sealed non-disclosure, with continued benefit eligibility through the end of the calendar year. The compensation package is on the table in front of you. Take a moment to read it."

Rhea read it. Three pages. Severance was generous. The NDA was wide. The continued benefits were a courtesy. The third page listed the *data-hygiene requirements* — the standard biometric purge for separations involving sensitive-access histories. Retina, palm, gait, voiceprint, vascular. The retina purge was specified as a *fifteen-minute outpatient procedure, conducted under local anaesthesia, with a recovery window of less than ninety minutes.* The procedure was not optional. The procedure was contractually consequent to severance.

Rhea read the third page twice.

She looked up.

"What's the procedure."

"It's standard."

"Is it OPTIC-7?"

Beatrix did not flinch. The Security Division colleague did not flinch. Rhea had given them, in the question, the only piece of leverage they had not yet confirmed they had, and she had given it to them on purpose. She had decided in the elevator that the elevator was not the place for a strategic question, and the seventh-floor break room was not the place for a strategic question, and the eighth-floor conference room — surveilled, recorded, witnessed by two — was the only place she could ask the question and have the answer be, by procedure, a recorded one.

Beatrix said, "It's a derivative."

"A derivative of OPTIC-7."

"It is calibrated to produce permanent retinal disengagement without the surrounding tissue effects. Fifteen minutes. Local anaesthesia. The recovery window is short."

"It blinds me."

"It produces permanent retinal disengagement. Yes."

"And I sign this document and walk out of here blind."

"You sign this document and you do not walk out of here blind today. The procedure is scheduled at a time of mutual convenience. The package includes a bearer credit for a Tier 3 ocular implant from an Axiom-approved provider. The implant restores functional vision. Most separations of this type are imperceptible to outside observers within three weeks."

"Most."

"Most."

Rhea sat with that for a count. She did not count breaths. She counted the document's third page, line by line.

"And if I refuse."

The Security Division colleague spoke for the first time. His voice was unkind. It was unkind in the way the watch on his wrist was expensive — not as a display, but as a baseline assumption about who he was talking to.

"Then we have a different conversation," he said.

Beatrix raised a hand a half-inch off the table. The Security Division colleague stopped talking.

"Rhea," Beatrix said. "Please understand. We do not want to do this any differently than this. You have been a valued contributor. We are offering the cleanest available exit. The package is generous. The continued benefits are real. The Axiom-approved implant provider is one of the best in the city — the Aurum line, paired ocular, Tier 3 spec. You will see again. You will see well. The provider is in the corporate residential block. The procedure with them is also fifteen minutes. The implants are state of the art."

Rhea looked at the third page of the package. The bearer credit was specified: *AURUM OCULAR, paired ocular implant, Tier 3 spec, retail value Φ440,000, redeemable through provider network only.* The provider list contained six clinics, all in Axiom-zoned residential blocks. The Aurum implant was a closed-firmware product. The firmware was maintained by Axiom Health Sciences. The biometric handshake — Rhea would learn this from the black-market technician on Friday night, the technician who would, in fourteen years, train Pixel on the same intake protocols — was identical to the biometric handshake that had been stripped from her in the procedure.

She would, in other words, walk out with new eyes that talked to the same backend as the old ones.

"I'd like a day to read the package," she said.

"You can have until end of business tomorrow," Beatrix said.

"Thank you."

"Rhea." Beatrix folded her hands again. The folding was the same physiological gesture Marcus made on the talkback at the moment the segment was done. "We don't want to do this any other way."

Rhea did not say *I know.* She stood up. She walked to the door. The Security Division colleague did not move.

She did not return to the office. She went down to Level 30. She packed nothing. She left the apartment with the keys on the kitchen counter, the rice cooker plugged in, the cipher seed memorized, the safe-deposit box in the Glooms paid through to 2228. She took the elevator to the lobby. She did not look back at the man with the watch in the lobby, who was standing by the fountain in the same suit he had been wearing fourteen months ago. She walked through the lobby and out onto State Street and turned south, away from the Spine, away from the Pulse station, away from the Glooms — the wrong direction on purpose, the surveillance-shedding pattern she had read about in the Andean Remnants segment, the pattern she had told herself she was unlikely ever to need.

She slept eight hours in a transient hotel in Z6. She woke at 0700. She ate a piece of bread and drank tap water. She walked back to State Street at 0830 because the contract had a deadline of end of business and she had decided, the night before, that the deadline could not be missed.

She walked into the Meridian Core at 0941. Her badge declined. The guard — not Hartfield this time — keyed the override the way the building had decided guards would key it for a Senior Narrative Architect in transition. She was escorted to a sub-level she had never been to. The corridor was black-walled. The procedure room was small. The Security Division colleague was there. He was not in his suit. He was wearing the kind of clean cotton scrubs medical personnel wear when they have been told to look like medical personnel. He was not medical personnel. The actual technician was an older woman in a Tier 4 lab coat who did not introduce herself. The chair was bolted to the floor.

The local anaesthesia was administered to the periorbital tissue at 0959. The procedure began at 1003. It was not fifteen minutes. The Security Division colleague did not speak. The technician did not speak. The chemical agent was administered via a curved applicator at 1006, and Rhea felt the dosing — at the concentration Mira Quintero-Bekele had stood in at two and a half meters from the podium — for a fraction of a second before the local anaesthesia caught up with the pain receptors that the local anaesthesia did not, in fact, fully cover.

The pain was the data. The pain was the unredacted footnote. The pain was Axiom's case study in its native medium, applied to her face, and she did not scream because she had decided in the chair, while the anaesthesia was being administered, that she would not scream, and she did not.

The procedure ended at 1041.

The recovery window was not fifteen minutes. She was held in the procedure chair for two hours. Her vision did not return. Her vision had not been intended to return. The bearer credit was placed in her left hand by a courier she did not see.

She was escorted out of the Meridian Core through a service corridor at 0427 the next morning, blind and severed, with the credit in her hand and a transit token in her coat pocket that would take her, by the route a courier in Axiom's employ had calculated for her, to the Circuit.

The escort left her at the corner of Armitage and Milwaukee. He did not say goodbye. She heard him walk back the way they had come. She stood at the corner for ninety-one seconds. She counted them, because counting was the one thing she had left, and the count held the way the breath had held in the elevator, and the count was the first thing that came with her into what would come next.

At second ninety-two, she walked.
""";

public const string Chapter5 = """
The technician in the Circuit was older than she expected.

She had imagined, walking from the corner of Armitage and Milwaukee at 0432 with the credit voucher in her left hand and her right arm hooked through the elbow of a stranger she had paid four hundred quanta to walk her three blocks east, that the technician would be a man in his thirties, that the clinic would smell of solvents, that the procedure would happen on a folding chair under a kitchen lamp. None of those things was true. The technician was a woman in her sixties whose name was Dr. Adaeze Kovalenko-Hassan, who introduced herself once, calmly, and did not introduce herself again. The clinic occupied the basement of a hat-repair shop. It smelled of nothing. The chair was surgical-grade and very old and had been re-upholstered four times.

Dr. Kovalenko-Hassan examined what was left of Rhea's eyes for nine minutes. She did not narrate the examination. She drew a small chart on a paper pad, in pencil, with a steady hand. She set the pad down.

"You are not the first one I have seen this week," she said. Her voice was level, clean, unhurried. The voice of a woman who had never needed to raise it and did not intend to start now. "You are the seventh. The compound is a derivative of OPTIC-7. It was deployed in Z3 in March. You worked at Axiom."

Rhea did not answer. Dr. Kovalenko-Hassan did not need her to answer.

"The bearer credit you are carrying is for the Aurum paired ocular, Tier 3 spec, redeemable at six Axiom-zoned clinics. Do you understand what the credit is for."

"It's a leash."

"Yes."

"The biometric handshake is unchanged from the procedure."

"Yes. The implant talks to the same backend that your stripped retina talked to. They will know where you are, what you see, when you sleep. They will know the moment you decide to stop being useful to them. They are giving you back vision on terms they choose. That is the offering."

"What do you offer."

Dr. Kovalenko-Hassan smiled. The smile was small and brief and not warm.

"I offer the Aurum Spec-7. Same housing. Telescopic optics. Manual aperture. No networked firmware. No biometric handshake. The optics are slightly better than the Tier 3 retail unit because the housings are machined to a Helix Biosystems tolerance the retail line does not bother with. The retail price would be Φ620,000. Your voucher is worth Φ440,000 here, because I take vouchers at a discount on the assumption that you are not in a position to recover par value. The difference is Φ180,000, payable in cash, before I begin."

"I don't have a hundred and eighty thousand in cash."

"No. You will. You will work for the difference. I will hold the procedure open for six weeks. You will report to a fixer named Yelena Chen-Okafor at the noodle stall on the corner of Damen and Augusta. She will give you small work that does not require sight. You will accumulate the Φ180,000 in approximately five and a half weeks. At the end of the fifth week you will return here. I will install the Spec-7 over fourteen hours across two days. You will recover, walk out, and never be billed again by anyone whose backend has access to the inside of your skull. Do you understand."

"Yes."

"There is no version of this conversation where you take the Aurum Tier 3 and the Spec-7. You can have one. You picked the harder one. I am telling you the price so you can pick again if you want to pick again."

"I'm picking the harder one."

"Good."

Dr. Kovalenko-Hassan made one note on the pencil pad. The note was a number and a date. She tore the page off and handed it to Rhea, who took it in her right hand because the left was still holding the voucher. The handwriting was very small and very precise. Rhea would, in three weeks, recognize the same hand on a separate piece of paper that arrived at the noodle stall with her second payment, and the recognition would be one of the four things that began to constitute the trust she had not known how to ask for.

Five weeks and four days later, she walked back into the basement of the hat-repair shop with one hundred and eighty thousand quanta in a folded envelope in her coat pocket. She handed Dr. Kovalenko-Hassan the envelope. Dr. Kovalenko-Hassan did not count the envelope. The procedure began at 0900 on a Tuesday and ended at 1415 on the following Wednesday, with a fourteen-hour interregnum during which Rhea slept on a cot in a back room, woke at intervals, and listened to the eight-hertz thrumline of a Pulse corridor three meters below the basement floor. She had heard the thrumline from the balcony on Level 30. It had sounded the same. It had meant something different.

At 1422 on Wednesday she sat up in the surgical chair. The aperture mechanism in her left ocular adjusted for the lamp on the wall. The aperture mechanism in her right ocular adjusted with a sound the left had not made. Dr. Kovalenko-Hassan corrected the right aperture mechanism with a small tool. The right ocular settled. The two implants logged the room.

She saw the room.

It was the first information she had received through any optical channel in forty-two days. The room was beige, low-lit, three by four meters, surgical chair in the center, instrument trolley to the right, doorway with a heavy gasket to the left. The doorway gasket was brass. The brass was recently oiled. The recently-oiled hinges were a Faraday-shielded Tier 4 spec. The room was, she understood, a room that did not appear on any Axiom map.

She did not cry. She had decided in the chair on Tuesday morning, while the anaesthesia was being administered, that she would not cry, and she did not.

She walked out of the basement at 1611. It was raining. The GLMZ had scheduled rain for atmospheric processing reasons. The rain fell at the angle the atmospheric processors preferred. The rain was, she noticed with the new optics, slightly easier to see at the level of individual droplets than rain had ever been with the eyes she had been born with.

She walked three corridors over and bought a coat.

The coat was tan. It was knee-length. It was well-cut. It had deep pockets. The stall-keeper, an old man who had not asked her name and had not commented on the implants, had it on a rack of three identical coats, all in tan, all knee-length, all well-cut, all with deep pockets. He had been a tailor at a Tier 3 firm in his thirties. The firm had gone under in a CorpoNation consolidation in his fifties. He had moved to the Circuit and sold coats. The coats were the work he had done at the firm; the prices were the only thing the consolidation had changed.

She paid in cash. She put the coat on. She put her hands in the pockets. She left the stall.

She walked five blocks west, and three blocks north, and stood at the corner of State and Madison, across the street from the Tessera Media Group building.

The Tessera Media Group building was no longer the Tessera Media Group building. It was, as of nineteen days ago, a restaurant. The restaurant was called *Forty-Seven North.* The restaurant served *deconstructed regional menus* on slate. The corner table — which Rhea, in the booth in the basement five years and three months ago, had not yet known to think of as the corner table — was visible through the window. A man was sitting at the corner table. He was eating a deconstructed mole on a piece of slate.

She stood across the street. She did not cross. She did not look away. The aperture mechanisms adjusted, and adjusted again, and a man twenty meters down the block turned at the sound, which only he heard, because the rain at this distance covered the implants for any normal listener.

She stood for one hour.

The hour was her decision. She had decided it on Wednesday morning, in the chair, while the anaesthesia was being administered, and she had not changed her mind in the seven hours between the decision and the moment she stood at the corner of State and Madison and began to count. She would stand for one hour. She would stand for one hour every year. She would not approach the building. She would not enter the restaurant. She would not, at any point in the hour, take her hands out of the pockets of the tan coat.

The hour ended at 1716 by the implants' internal clock, which was accurate to seventeen parts per billion and would, over the following thirteen years, drift not at all.

She walked back to the Circuit.

She did not call herself Rhea on the walk. She did not call herself Sable yet. She would call herself Sable for the first time, three days later, at the noodle stall on the corner of Damen and Augusta, when the fixer who had given her the work for the Φ180,000 — Yelena Chen-Okafor, who would, in eleven years, retire and pass her network to a younger broker — asked the new client at the counter what to call her.

"Sable," she said.

Yelena nodded. She did not ask why.

The new client paid in cash. Sable took the contract. She did not take her hands out of her pockets to accept it. The cash arrived later, by courier, to a dead drop she did not own and would not own and would never need to own, because the cash arrived, and the work began, and the work continued, and the implants logged the city, and the tan coat did not come off, and the hands stayed in the pockets, and the breath, on the noun, no longer needed to land soft, because she was no longer the voice anyone trusted.

She was the voice that did not need to be trusted to be paid.

She walked.
""";

}
