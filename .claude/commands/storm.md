---
description: Resume the GOSPEL glossary + corpus-wide publish-readiness pass (saved mid-session, MCP restart pending).
allowed-tools: mcp__prose__switch_universe, mcp__prose__list_universes, mcp__prose__current_universe, mcp__prose__upsert_glossary_term, mcp__prose__list_glossary_terms, Bash, PowerShell, Read, Edit, Write, Grep, Glob, TaskCreate, TaskUpdate, TaskList
---

# STORM — resume point

Saved mid-session because the MCP server needed a restart to see a newly-created "gospel"
Universe row. On invocation, treat this file's content as the current task state and continue
directly — do not re-derive or re-ask anything already settled below.

## Current task

Finish the GOSPEL universe glossary rollout, then run the deferred corpus-wide re-export and
final logic sweep. The user's explicit instruction: **only re-export once ALL of prose, glossary,
and description work is done** — do not export prematurely.

## Decisions locked this session

- **GOSPEL universe created**: Id `0197e9c9-0007-7000-8000-000000000007`, slug `gospel`, seed
  script `v3\Prose.Core\Data\Sql\add_universe_gospel_20260812.sql`, registered in
  `SqlSeedService.Seeds["universe_gospel"]`. Already applied (seed ran successfully).
- **Matthew/Mark/Luke/John moved into GOSPEL**: all 117 rows (4 books + 113 chapter descendants)
  had `Nodes.UniverseId` updated from `nonfiction` to `gospel` via a recursive-CTE UPDATE. Verified
  — confirmed 4 books + 113 chapters now show `gospel`.
- **Glossary detection is plural-insensitive**: `GlossaryService.AppearsInText` (in
  `v3\Prose.Core\Services\GlossaryService.cs`) strips a trailing "s" from the headword before
  matching, with an optional trailing "s" back on the match — so "neuretic"/"neuretics" (or any
  singular/plural pair) are one entry regardless of which form was authored. Shipped and built.
- **Glossary detection is now recursive/cross-referencing**: `GetUsedTermsCoreAsync` (same file)
  expands the "used" set to include any OTHER glossary term that appears inside an already-used
  term's own Definition/FullForm (e.g. SR's definition mentions DataEast → DataEast gets pulled in
  too), repeating until no new terms are found. Shipped and built.
- **GlossaryTerms is already universe-scoped** (FK `UniverseId`, unique index on
  `(UniverseId, Term)`) — confirmed no cross-universe term sharing is possible; this was already
  correct, no code change needed.
- **Epub/PDF now get a Glossary back-matter section** (previously only .docx did) — added
  `BuildGlossaryChapter` to `v3\Prose.Core\Services\ManuscriptExportService.cs`, wired into
  `ExportEpubAsync`/`ExportPdfAsync` only (NOT `ExportAudioTxtAsync` — that's the TTS narration
  script, glossary entries read aloud would be wrong; NOT `ExportMarkdownAsync` — editing aid,
  not a KDP deliverable). Verified end-to-end on RTR (GLMZ): epub TOC + chapter + PDF page all
  showed the Glossary correctly.
- **GLMZ master glossary expanded 33 → 67 terms; SCRY expanded 31 → 65 terms** — audited against
  regenerated `docs/BIBLE.md`/`docs/WORLD.md`/`docs/GLMZ.md` (GLMZ) and
  `docs/universes/ENTOS.md`/`docs/SCRY.md` (SCRY). Also fixed a mojibake bug on the Φ (Quanta
  currency) GLMZ term — was corrupted to "I�", fixed to the real Φ (U+03A6) with correct
  Q/Qreds aliasing in the definition.
- **All 22 GLMZ books got a standardized subtitle**: "A GLMZ Novel" (BCODA, DWIACE, IxS — all
  ≥40k words), "A GLMZ Short Story" (TWD — 6,086 words, <7,500), "A GLMZ Novella" (everyone else,
  7,500–40k words). Rule: SFWA-style thresholds with "novelette" merged into novella since only 3
  tiers were wanted. Fixed 4 books that were previously mislabeled "Short Story" despite being
  well above 7,500 words (CRIT, ICFI, PXL, TWU).
- **Kindle pages + reading time is now a permanent schema field, computed on every export** —
  added `Node.KindlePages` (int?) and `Node.ReadingMinutes` (int?) columns (migration
  `20260812152501_AddKindlePagesAndReadingMinutes`, already applied to the DB). Formula: pages =
  round(words / 250) — the commonly-cited Kindle-page convention, distinct from the existing
  print-trim `KdpPageCount` (306 wpm + chapter overhead); reading time = round(words / 200wpm) —
  commonly-cited average adult silent-reading speed. Wired into
  `NodeFullExportService.ExportAllAsync`: recomputes from live prose every export, strips any
  previously-appended "Approximately N pages and X to read." trailing line from Description via
  regex before appending a freshly computed one (idempotent — never piles up duplicates across
  re-exports). Already manually back-filled once for all 36 then-existing books via a one-off
  PowerShell/SqlClient pass (before the schema/code path existed) — the next real export of each
  book will supersede that manual value with the properly-computed one, which is expected and fine.
- **KdpPublish app fully restyled to match the real KDP portal** — light theme (#EAEDED bg, teal
  #007185 links, Amazon yellow #FFD814 CTA, Squid Ink #131A22 log), Amazon Ember font family
  downloaded locally to `v3\Prose.KdpPublish\wwwroot\fonts\` (28 files, all weights/styles/both
  "Modern Display" and "Modern Text" variants) with `@font-face` rules pointing at local paths —
  no more CDN dependency. Sidebar widened +150px (410px), code-column ellipsis removed, redundant
  header branding removed, notes (▤) and cover (🖼) preview-modal icons added per row, Select All
  skips WorkInProgress books, status badges got icons (✓ Published, ⟳ Outdated, ⚠ WorkInProgress),
  row background coloring removed (badge-only signal), only one real scrollbar remains (the book
  list), log panel fixed at 25vh with a spinner (not "Loading manifest…" text) on first load.
- **Word-count/pages/reading-time convention documented in `NodeFullExportService.cs` doc
  comments** — 250 wpm pages, 200 wpm reading time, both distinct from the print `KdpPageCount`.

## State / where we are

**Blocked, now possibly unblocked by the MCP restart the user just did**: `mcp__prose__switch_universe`
could not resolve `slug: "gospel"` because the MCP server's universe list was stale (built before
the `gospel` row existed in the DB). The user has now restarted the MCP server/session. **First
action on resuming: call `mcp__prose__list_universes` — if `gospel` now appears, proceed
immediately with the upserts below. If it still doesn't appear, that's a real bug (an agent was
mid-investigation into `list_universes`'s exact data source/DI lifetime — check for that agent's
findings if available, otherwise re-investigate `v3\Prose.Mcp\Tools.Universe.cs` or wherever
`list_universes`/`switch_universe` are implemented, and `IUniverseContext`'s DI registration).**

**Everything else is done and built** (GLMZ/SCRY glossary expansion, epub/pdf glossary wiring,
GLMZ subtitles, Kindle pages/reading-time schema+wiring, KdpPublish restyle+fonts+UI fixes). The
26-book GLMZ+SCRY re-export from earlier in the session is already stale relative to the newer
changes (subtitles, pages/reading-time, further glossary edits) — **do not treat that earlier
re-export as sufficient; a fresh full corpus re-export is still needed** (task below).

## Next concrete steps

1. `mcp__prose__list_universes` — confirm `gospel` is visible. If yes:
2. `mcp__prose__switch_universe({slug: "gospel"})`.
3. Upsert all 87 deduplicated GOSPEL glossary terms below via `mcp__prose__upsert_glossary_term`
   (batch in parallel tool calls, same pattern used for the 34 GLMZ + 34 SCRY additions earlier
   this session). These were merged down from 127 raw candidates independently proposed by four
   research agents (one per Matthew/Mark/Luke/John, each reading that book's
   `story-synopsis.txt`) — duplicates across books (Sanhedrin, Pharisees, Denarius, Titulus, etc.
   appeared in 2-4 of the four lists) were merged into single canonical entries.
4. `dotnet run --project v3/Prose.Cli -- --generate-glossary --universe gospel` — regenerate the
   master glossary docs.
5. `dotnet run --project v3/Prose.Cli -- --universe gospel --generate-book-glossary --all` —
   regenerate each Gospel book's per-book glossary subset, sanity-check term-hit counts.
6. **Full corpus re-export** (deferred task #11): every book with live prose, every universe
   (GLMZ's 22 + SCRY's 4 already done once earlier but now stale — redo; GOSPEL's 4; plus
   fiction/TFAH, horror/QRT, nonfiction/1381+IREOUT+JOAN+NEPH — 36 books total per the earlier
   corpus-wide word-count query). Only run this once steps 1-5 are confirmed clean.
7. **Full logic sweep** (deferred task #12, per `docs/LOGIC.md` / `/logic-sweep`): across every
   book, confirming (a) general readability/continuity per the standard six-dimension sweep, and
   (b) each book's back-matter glossary lists ONLY terms actually used in that specific book,
   correctly pulled from that book's own universe's master glossary (not cross-contaminated
   between GLMZ/SCRY/GOSPEL/etc.).
8. Report final status to the user.

## The 87 GOSPEL glossary terms to upsert (term | fullForm | definition | category)

1. Sanhedrin | "" | The Jewish high council of chief priests, elders, and scribes holding religious and limited judicial authority in first-century Jerusalem under Roman oversight; it interrogates and tries Jesus before he is handed to Pilate. | Office/Institution
2. Pharisees | "" | A first-century Jewish religious movement emphasizing strict observance of Torah law and oral tradition; recurring critics of Jesus over Sabbath, purity, fasting, and divorce law. | Sect
3. Sadducees | "" | A Jewish priestly-aristocratic party aligned with Temple authority that accepted only the written Torah and denied bodily resurrection; challenges Jesus with a resurrection riddle in all three Synoptic Gospels. | Sect
4. Scribes | "" | Professional experts in Jewish law and scripture responsible for copying, interpreting, and teaching the Torah; frequently paired with Pharisees and priests as Jesus's interlocutors and opponents. | Office
5. Herodians | "" | Political supporters of Herodian dynastic rule rather than a religious sect; join Pharisees in schemes to trap Jesus over the imperial tax question. | Faction
6. High Priest | "" | The chief officiant of the Jerusalem Temple and highest Jewish religious authority under Rome; Caiaphas (with his father-in-law and predecessor Annas) holds the office at Jesus's trial. | Office
7. Tetrarch | ruler of a quarter-province | A Roman-appointed ruler over a subdivided former kingdom, ranked below a full king; Herod Antipas held this title over Galilee, his brother Philip over Ituraea/Trachonitis, and Lysanias over Abilene. | Office
8. Centurion | "" | A Roman (or, in Herodian territory, Herodian-appointed) army officer commanding roughly a hundred soldiers; one asks Jesus to heal his servant, another declares Jesus "God's Son" at the crucifixion. | Office
9. Synagogue | "" | A local Jewish assembly hall for scripture reading, prayer, and teaching, distinct from the Jerusalem Temple; Jesus repeatedly teaches and heals in synagogues throughout Galilee. | Place/Institution
10. Praetorium | "" | The residence and headquarters of a Roman governor or commander in a provincial city; the setting of Jesus's Roman trial, mockery, and flogging in the Passion narratives. | Place
11. Golgotha | place of the skull | The execution site outside Jerusalem's walls where Jesus was crucified; its exact modern location is disputed between two candidate sites. | Place
12. Gehenna | "" | A term for post-mortem punishment drawn from the historical Valley of Hinnom outside Jerusalem, once the site of a child-sacrifice cult; the popular claim it was a burning garbage dump traces only to a medieval rabbinic commentary with no ancient support. | Place/Concept
13. Machaerus | "" | A Herodian fortress east of the Dead Sea where, per the historian Josephus, John the Baptist was imprisoned and executed by Herod Antipas. | Place
14. Decapolis | the Ten Cities | A league of largely Greek-speaking, Gentile-influenced cities near Galilee; used as evidence of a mixed Gentile/Jewish population in the regions Jesus visits east of the Jordan. | Place/Region
15. Denarius | "" | A standard Roman silver coin, roughly a laborer's day's wage; the reference coin for the "render unto Caesar" tribute question and the flat wage in the vineyard-workers parable. | Currency
16. Lepton (plural: Lepta) | "" | The smallest-denomination Jewish coin, of negligible value; the poor widow's Temple-treasury donation of two of them is presented as a token but sincere sum. | Currency
17. Mina | "" | A unit of currency worth considerably more than a denarius, used for the sums a departing nobleman entrusts to his servants in one of Jesus's parables. | Currency
18. Talent | "" | An ancient unit of weight used to denote an enormous sum of money, far exceeding a denarius; the modern sense of "talent" as personal ability derives entirely from later allegorical readings of the parable where it appears. | Currency
19. Tyrian Shekel | "" | A silver coin minted in Tyre, required for the Jerusalem Temple tax because of its reliable silver content despite bearing pagan imagery — the reason money-changers operated in the Temple courts. | Currency
20. Tyrian Purple | "" | An extremely costly dye extracted from murex sea snails, associated with royalty and wealth across the ancient Mediterranean. | Trade Good
21. Corban | "" | A vow dedicating money or property to God or the Temple treasury, used by some to legally withhold financial support from aging parents — a loophole Jesus condemns as overriding the commandment to honor one's parents. | Practice/Legal
22. Shema | "" | The core Jewish declaration of monotheistic faith, recited daily; Jesus cites it alongside Leviticus 19:18 in naming the greatest commandment. | Practice/Prayer
23. Levirate Marriage | "" | A biblical law obligating a man to marry his deceased, childless brother's widow to continue the family line; the Sadducees build a hypothetical on it to mock belief in resurrection. | Practice/Law
24. Phylacteries (Tefillin) | Tefillin | Small leather boxes containing scripture verses, bound to the arm and forehead during prayer per Torah command; Jesus criticizes wearing oversized ones for public show, not the practice itself. | Practice/Object
25. Tzitzit | ritual fringes | Knotted fringes worn on garment corners per Torah command; like phylacteries, criticized by Jesus for ostentatious length, not for existing. | Practice/Object
26. Sukkot (Feast of Tabernacles) | Feast of Tabernacles/Booths | An autumn Jewish pilgrimage festival with water-libation and Temple illumination ceremonies, commemorating temporary wilderness dwellings; background for both the Transfiguration's "tabernacles" offer and John's "living water"/"light of the world" sayings. | Practice/Festival
27. Feast of Dedication | Hanukkah | An eight-day winter Jewish festival commemorating the Temple's rededication after the Maccabean revolt; the setting for a Jerusalem discourse at Solomon's Portico. | Practice/Festival
28. Temple Tax | "" | An annual levy on Jewish adult males supporting the Jerusalem Temple, independently corroborated by the historians Josephus, Philo, and Cicero. | Practice/Institution
29. Binding and Loosing | "" | A rabbinic idiom for the authority to declare something forbidden or permitted, or to include or exclude someone from the community; granted first to Peter, then to all the disciples. | Practice/Authority
30. Mishnah | "" | The foundational written compilation of Jewish oral legal tradition, codified around 200 CE; cited for burial customs, tithing practices, and a differing date for the Temple's destruction. | Historiography/Text
31. Talmud | "" | The large body of rabbinic commentary built on the Mishnah; cited on anonymous charity and on burial as the legally safest way to safeguard money. | Historiography/Text
32. Septuagint | the ancient Greek translation of the Hebrew Bible | The pre-Christian Greek translation of Jewish scripture; central to the debate over Isaiah's "almah" being rendered "parthenos" (virgin) in the nativity narrative. | Historiography/Text
33. Almah and Parthenos | Hebrew 'almah' / Greek 'parthenos' | Isaiah's original Hebrew word for "young woman" (not necessarily a virgin), rendered as the Greek word for "virgin" in the Septuagint — the translation shift at the center of the virgin-birth debate. | Historiography/Translation
34. Q Source | Quelle (German: 'source') | A hypothetical lost sayings-document scholars propose Matthew and Luke drew on independently of Mark, reconstructed by comparing their shared material. | Historiography
35. Signs Source | "" | A historical-critical theory that John's Gospel drew on an earlier, now-lost written collection of Jesus's miracle stories, proposed to explain stylistic seams in the finished text. | Historiography
36. Messianic Secret | "" | A modern scholarly theory (William Wrede) describing Mark's recurring motif of Jesus silencing demons and witnesses about his identity. | Historiography
37. Criterion of Embarrassment | "" | A historical-critical method judging details likely authentic because they would have been awkward for the early community to invent, such as John the Baptist's doubt about Jesus. | Historiography/Method
38. Vaticinium ex Eventu | prophecy from the outcome (Latin) | A historical-critical term for a "prediction" written after an event occurred but presented as foreknowledge; applied to the Temple-destruction discourse. | Historiography
39. Abomination of Desolation | "" | A phrase from Daniel originally describing Antiochus IV's 167 BCE altar desecration, later reapplied by successive interpreters — including Caligula's statue order and the Gospel authors — to new crises. | Historiography/Concept
40. Josephus | "" | A first-century Jewish historian whose independent, non-Christian writings are repeatedly used to corroborate, complicate, or contradict Gospel details, from John the Baptist's death to the Temple's destruction. | Historiography/Source
41. Papias | "" | An early second-century Christian bishop whose fragmentary writings are among the earliest external sources on the Gospels' origins; his ambiguous description of "John" underlies a two-author authorship debate. | Historiography/Source
42. Irenaeus | "" | A late second-century bishop of Lyon whose writings are the earliest surviving source explicitly identifying the Fourth Gospel with the apostle John, son of Zebedee. | Historiography/Source
43. Eusebius | "" | An early fourth-century Christian historian whose Ecclesiastical History preserves and interprets earlier lost sources such as Papias, cited repeatedly in disputed authorship and textual debates. | Historiography/Source
44. Codex Sinaiticus | "" | One of the oldest surviving near-complete Greek New Testament manuscripts (4th century CE); its original hand is missing later-added verses in more than one Gospel, making it a key witness for textual-addition debates. | Historiography/Manuscript
45. Pilate Stone | "" | A first-century inscription discovered at Caesarea bearing Pontius Pilate's name and title, providing rare independent epigraphic confirmation of a Gospel figure and his real title of prefect. | Artifact/Historiography
46. Theodotos Inscription | "" | A first-century synagogue dedication inscription discovered in Jerusalem, used as independent archaeological evidence for the synagogue officials and practices the Gospels describe. | Artifact/Historiography
47. Nazareth Inscription | "" | An ancient marble inscription bearing an imperial edict against grave-tampering, once cited as corroborating Gospel tomb-guard claims; isotope analysis later traced its stone to a Kos quarry, undermining that link. | Historiography/Object
48. Census of Quirinius | "" | The Roman population count Luke ties to Jesus's birth, attributed to governor Quirinius; historically contested because Quirinius's attested governorship (6 CE) falls a decade after Herod the Great's death (4 BCE). | Historiography
49. Prefect | "" | The Roman administrative title for governors of minor provinces like Judea, confirmed for Pontius Pilate by the Pilate Stone; Gospel-era popular usage calls him "governor" rather than this formal title. | Office
50. Ossuary | "" | A small stone box used in first-century Judea for secondary burial of bones after the flesh decayed; named examples (Caiaphas, James) are cited in debates over Gospel-adjacent burial customs. | Archaeology
51. Nard | spikenard | A costly aromatic oil imported from the Himalayan region, used as luxury perfume or burial ointment; a woman anoints Jesus with a pound of it shortly before his death. | Practice/Object
52. Titulus | titulus crucis | A placard stating the charge against a crucified prisoner, affixed to a Roman cross; a claimed relic fragment of Jesus's titulus, venerated at Santa Croce, is radiocarbon-dated centuries too late to be genuine. | Object/Practice
53. Sudarium (of Oviedo) | Latin: cloth/face-cloth | A bloodstained relic in Oviedo, Spain, claimed to be Jesus's resurrection face-covering; radiocarbon dating places it centuries after his death. | Object/Relic
54. Holy Lance | also called the Spear of Longinus | A relic claimed to be the spear that pierced Jesus's side; rival versions exist in Rome, Vienna, and Echmiadzin, and metallurgical dating rules out first-century origin for at least one. | Object/Relic
55. True Cross | "" | The traditionally venerated wood believed to be from Jesus's cross; a nineteenth-century survey disproved the popular claim that surviving fragments could fill a ship. | Object/Relic
56. Crown of Thorns | "" | A relic purchased by King Louis IX of France in 1238, claimed to be Jesus's mocking crown; the surviving object now contains no thorns, all having been distributed as separate relics. | Object/Relic
57. Longinus | "" | The traditional name given to the soldier who pierced Jesus's side, invented from the Greek word for spear and first appearing in later apocryphal and Syriac sources, not the Gospel text itself. | Historiography/Legend
58. Logos | Greek: 'word'/'reason' | The philosophical-theological term opening the Fourth Gospel, drawing on Greek philosophy and Hellenistic Jewish thought to describe a divine ordering principle identified with Jesus. | Theology
59. Paraclete | Greek 'parakletos': advocate/helper/comforter | A title for the Holy Spirit in Jesus's farewell teaching — a promised advocate who comes after Jesus's departure to guide, defend, and testify for the disciples. | Theology
60. Ego Eimi ("I Am") Sayings | Greek: 'I am' | Jesus's recurring self-declarations, linked by scholars to the divine self-naming formula in Exodus. | Theology
61. Anōthen | Greek, meaning both 'again' and 'from above' | The deliberately ambiguous Greek word Jesus uses with Nicodemus, rendered "born again" in English translation, whose double meaning drives the scene's central misunderstanding. | Theology/Linguistics
62. High Priestly Prayer | "" | The traditional title for Jesus's extended prayer before his arrest; the title itself has a documented history in later theological tradition rather than being original to the text. | Theology
63. Farewell Discourse | "" | The scholarly designation for Jesus's extended final teaching to his disciples, echoing the ancient genre of testamentary speeches given by the departing before their followers. | Historiography/Genre
64. Beloved Disciple | "" | The unnamed figure the Fourth Gospel repeatedly calls "the disciple whom Jesus loved," present at the Last Supper, the cross, and the empty tomb, and credited by the text as the Gospel's source. | Historiography
65. Pericope Adulterae | Latin: 'the passage about the adulteress' | The scholarly name for the story of the woman caught in adultery, regarded as a later insertion since it is absent from the earliest manuscripts and its location shifts across manuscript traditions. | Historiography
66. Aposynagogos | Greek: 'expelled from the synagogue' | Formal expulsion from the synagogue community; used as evidence of the social pressures shaping the Fourth Gospel's later setting. | Historiography/Practice
67. Birkat Ha-Minim | Hebrew: 'Blessing/Curse of the Heretics' | A Jewish liturgical formula sometimes proposed as a mechanism for excluding early Jewish-Christians from synagogues, though its direct link to Gospel expulsion language is debated. | Historiography
68. Solomon's Portico | "" | A covered colonnade on the Jerusalem Temple's outer court used for gathering and teaching; the setting for a winter Temple discourse. | Place
69. Gabbatha (Stone Pavement) | Gabbatha; the Stone Pavement | The elevated platform where Pilate presents Jesus to the crowd before crucifixion; its exact archaeological location is disputed. | Place
70. Kataluma | Greek word rendered 'inn' in some translations | The Greek term behind the nativity "inn"; scholarship reads it as a private home's guest room, not a commercial inn. | Term/Linguistic
71. Magnificat | "" | The hymn of praise Mary speaks upon greeting Elizabeth, traditionally named for its opening Latin word ("magnifies"); unique to the infancy narrative it appears in. | Text/Liturgical
72. Benedictus | "" | The prophetic canticle Zechariah delivers once his speech returns after John's birth, traditionally named for its opening word ("blessed"). | Text/Liturgical
73. Infancy Gospel of Thomas | "" | A later, non-canonical text containing invented childhood-miracle stories about Jesus, cited as a contrast to the Gospels' restraint and as an example of legendary accretion over time. | Text/Historiography
74. Epitropos | Greek: steward/manager | A Greek administrative title for a steward managing an estate or royal household; one of the women who funded Jesus's ministry was married to a man holding this office. | Office
75. Archisynagogos | Greek: 'ruler of the synagogue' | The title for a synagogue's presiding official, responsible for its administration and services; independently attested by the Theodotos Inscription. | Office
76. Legion | "" | The name a possessing demon gives itself in an exorcism narrative, evoking a Roman military unit of several thousand soldiers; scholars debate whether the name carries deliberate anti-Roman resonance. | Term/Historiography
77. Beelzebul | "" | A name applied to a chief demon/Satan figure, traced as a satirical corruption of a genuine ancient Ugaritic divine epithet — distinct from later folk claims about an insect-god cult. | Religious Term
78. Kaddish | "" | A Jewish liturgical prayer sanctifying God's name; scholars caution against treating it as a direct structural model for the Lord's Prayer. | Practice
79. Amidah | "" | The central standing prayer of Jewish liturgy, recited multiple times daily; its fixed, standardized form postdates Jesus, making a direct comparison to his prayer anachronistic. | Practice
80. Mammon | "" | An Aramaic term for wealth or material riches, personified in Jesus's teaching as a rival object of devotion to God. | Religious Term
81. Architelones | Greek: 'chief tax collector' | A Greek compound term implying authority over other tax collectors in a district; applied to one specific wealthy tax collector to distinguish him from ordinary toll collectors elsewhere. | Office
82. Levite | "" | A member of the tribe of Levi assigned to assist in Temple service, ranking below the priesthood proper; a Levite passes by the injured traveler in the parable of the Good Samaritan. | Office
83. Hillel and Shammai | "" | Two rival first-century rabbinic schools of legal interpretation — Hillel broader and more lenient, Shammai narrower and stricter — invoked as the live contemporary debate a divorce ruling engages. | Historiography/Legal Schools
84. Porneia | Greek, usually rendered 'sexual immorality' | The Greek term underlying a divorce exception clause, whose precise scope is debated and compared to the Hillel/Shammai dispute. | Historiography/Legal Term
85. Boanerges | 'sons of thunder' | An Aramaic-derived nickname Jesus gave to two of his disciples, James and John; its precise etymology is debated. | Language/Epithet
86. Talitha koum | 'Little girl, arise' | An Aramaic phrase preserved untranslated in the Greek text, spoken by Jesus while raising a synagogue leader's dead daughter. | Language
87. Ephphatha | 'be opened' | An Aramaic word retained in the Greek text, spoken while healing a deaf man with a speech impediment. | Language

## Anchors

- Task list: #11 (full corpus re-export, pending), #12 (full logic sweep, pending) — both still open.
- Key files touched this session: `v3\Prose.Core\Services\GlossaryService.cs`,
  `v3\Prose.Core\Services\ManuscriptExportService.cs`, `v3\Prose.Core\Services\NodeFullExportService.cs`,
  `v3\Prose.Core\Data\Entities\Node.cs`, `v3\Prose.Core\Data\Sql\add_universe_gospel_20260812.sql`,
  `v3\Prose.Core\Services\SqlSeedService.cs`, `v3\Prose.KdpPublish\wwwroot\app.css`,
  `v3\Prose.KdpPublish\wwwroot\panel.html`, `v3\Prose.KdpPublish\MainWindow.xaml(.cs)`.
- Migration: `20260812152501_AddKindlePagesAndReadingMinutes` (applied). Note it originally also
  tried to re-add `Findings.SourceRuleVersion`/its index (already existed from an earlier raw-SQL
  seed, RFC 0011 brick B2) — that part was manually stripped from the migration's Up/Down before
  applying; don't be confused if you see that column already exists.
- GOSPEL universe Id: `0197e9c9-0007-7000-8000-000000000007`.
