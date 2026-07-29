$ErrorActionPreference = 'Stop'   # fail loudly: a SqlException is otherwise non-terminating
$OutputEncoding = [System.Text.UTF8Encoding]::new($false)
[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)

Add-Type -AssemblyName System.Data

$connStr = "Server=(localdb)\MSSQLLocalDB;Database=StreetSamurai;Trusted_Connection=True;TrustServerCertificate=True;"
$conn = New-Object System.Data.SqlClient.SqlConnection($connStr)
$conn.Open()

function Sha256Hex([string]$text) {
    $sha = [System.Security.Cryptography.SHA256]::Create()
    $bytes = [System.Text.Encoding]::UTF8.GetBytes($text.Trim())
    $hash = $sha.ComputeHash($bytes)
    return ([System.BitConverter]::ToString($hash) -replace '-', '').ToLower()
}

function Exec-NonQuery([string]$sql, [hashtable]$params) {
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = $sql
    foreach ($k in $params.Keys) {
        $cmd.Parameters.AddWithValue("@$k", $params[$k]) | Out-Null
    }
    $cmd.ExecuteNonQuery() | Out-Null
}

function Exec-Scalar([string]$sql) {
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = $sql
    return $cmd.ExecuteScalar()
}

function New-BeatRow([string]$text) {
    $script:MaxNumber = $script:MaxNumber + 1
    $id = [guid]::NewGuid()
    $hash = Sha256Hex $text
    $sql = "INSERT INTO Beats (Id, Text, TextHash, Act, SceneType, Kind, Number, Stale, WasCorrected, IsChapterStart, Version, EntityStale, CreatedAt, UpdatedAt) VALUES (@Id, @Text, @Hash, 0, 'scene', 'prose', @Number, 0, 0, 0, 0, 0, SYSUTCDATETIME(), SYSUTCDATETIME())"
    Exec-NonQuery $sql @{ Id = $id; Text = $text; Hash = $hash; Number = $script:MaxNumber }
    return $id
}

function Add-BeatNode([guid]$nodeId, [guid]$beatId, [double]$sortKey) {
    $sql = "INSERT INTO BeatNodes (NodeId, BeatId, SortKey, IsEnabled) VALUES (@NodeId, @BeatId, @SortKey, 1)"
    Exec-NonQuery $sql @{ NodeId = $nodeId; BeatId = $beatId; SortKey = $sortKey }
}

function Seed-Entity([string]$name, [string]$slug, [string]$type, [string]$desc) {
    $exists = Exec-Scalar "SELECT COUNT(*) FROM Entities WHERE UniverseId='0197E9C9-0003-7000-8000-000000000003' AND Slug='$slug'"
    if ($exists -gt 0) { Write-Host "  entity exists, skip: $name"; return }
    $id = [guid]::NewGuid()
    $sql = "INSERT INTO Entities (Id, EntityType, Name, Slug, Status, Description, CreatedAt, ModifiedAt, IsActive, UniverseId) VALUES (@Id, @Type, @Name, @Slug, 'canon', @Desc, SYSUTCDATETIME(), SYSUTCDATETIME(), 1, '0197E9C9-0003-7000-8000-000000000003')"
    Exec-NonQuery $sql @{ Id = $id; Type = $type; Name = $name; Slug = $slug; Desc = $desc }
    Write-Host "  seeded entity: $name"
}

# ---- Live state ----
$script:MaxNumber = [int](Exec-Scalar "SELECT MAX(Number) FROM Beats")
$NotesNodeId = [guid]"019FA96D-7D48-75E0-9BD9-2190171276DC"
$GlossaryNodeId = [guid]"019FA96D-8DD0-70E4-8D98-34AC48833B7E"
$Ch18NodeId = [guid]"019FA96D-3AF3-70C2-8D25-0CB23E7F4203"

$maxNoteSortKey = [double](Exec-Scalar "SELECT ISNULL(MAX(bn.SortKey),0) FROM BeatNodes bn WHERE bn.NodeId='019FA96D-7D48-75E0-9BD9-2190171276DC'")
$maxGlossarySortKey = [double](Exec-Scalar "SELECT ISNULL(MAX(bn.SortKey),0) FROM BeatNodes bn WHERE bn.NodeId='019FA96D-8DD0-70E4-8D98-34AC48833B7E'")
$maxNoteNumber = [int](Exec-Scalar "SELECT ISNULL(MAX(CAST(LEFT(b.Text, CHARINDEX(' ',b.Text)-1) AS INT)),0) FROM BeatNodes bn JOIN Beats b ON bn.BeatId=b.Id WHERE bn.NodeId='019FA96D-7D48-75E0-9BD9-2190171276DC' AND CHARINDEX(' ', b.Text) > 1 AND LEFT(b.Text, CHARINDEX(' ',b.Text)-1) NOT LIKE '%[^0-9]%'")

Write-Host "Starting: MaxNumber=$script:MaxNumber MaxNoteSortKey=$maxNoteSortKey MaxNoteNumber=$maxNoteNumber MaxGlossarySortKey=$maxGlossarySortKey"

# ---- Notes (slug -> title/body) in order ----
$notes = [ordered]@{
'speira-roman-detachment-size' = @{ title="A cohort at the arrest? The size of John's speira"; body="Raymond E. Brown, The Death of the Messiah: From Gethsemane to the Grave, vol. 1, Anchor Bible Reference Library (New York: Doubleday, 1994), commentary ad loc. John 18:3. Brown surveys the range of the Greek term speira, which in contemporary usage could denote anything from a small maniple of a few dozen men up to a full cohort of several hundred, and lays out the historical-plausibility question directly: would Rome commit any of its own soldiers, let alone hundreds of them, to arrest one unindicted Galilean teacher before a single formal charge existed? Brown's own reading treats the detail as likely inflated for narrative effect rather than a precise troop count, while cataloguing scholars on both sides of the question." }
'antonia-fortress-garrison-crowd-control' = @{ title="Real troops, real festival, uncertain deployment"; body="Flavius Josephus, The Jewish War, Book 2, section 224, and Book 5, section 244 (Loeb Classical Library, trans. H. St. J. Thackeray, Cambridge, MA: Harvard University Press). Josephus describes a permanent Roman garrison cohort stationed at the Antonia Fortress overlooking the Temple, reinforced during pilgrimage festivals specifically because of the crowd-control risk large festival gatherings posed; this establishes that some troop presence in Jerusalem at Passover is historically well attested, without confirming that a detachment of that garrison was in fact the specific body John describes accompanying Judas." }
'philo-pilate-character' = @{ title="Pilate's reputation, from a hostile contemporary"; body="Philo of Alexandria, On the Embassy to Gaius (Legatio ad Gaium), sections 299-305 (Loeb Classical Library, trans. F. H. Colson, Cambridge, MA: Harvard University Press, 1962). Philo, writing within Pilate's own lifetime, describes him as inflexible, corrupt, and given to gratuitous cruelty and frequent executions without trial, a portrait historians weigh against the comparatively restrained, almost reluctant Pilate of John's trial narrative when assessing how much of the Gospel's characterization reflects Johannine theological interest in Pilate as an ambivalent judge." }
'ego-eimi-theophany-flourish' = @{ title="Soldiers on the ground: a Johannine flourish"; body="C. K. Barrett, The Gospel According to St John: An Introduction with Commentary and Notes on the Greek Text, 2nd ed. (Philadelphia: Westminster Press, 1978), commentary ad loc. John 18:5-6; compare Rudolf Bultmann, The Gospel of John: A Commentary, trans. G. R. Beasley-Murray (Philadelphia: Westminster Press, 1971), ad loc. Both commentators read the arresting party's recoil and collapse at Jesus's ego eimi as a deliberate theophany-style flourish, drawing on the divine-name resonance already at work across John's earlier I-am sayings, and note its complete absence from all three Synoptic arrest accounts, where Jesus is simply seized." }
'palatial-mansion-avigad-excavation' = @{ title="A mansion in the Upper City"; body="Nahman Avigad, Discovering Jerusalem (Nashville: Thomas Nelson, 1983), chapter on the Herodian Quarter excavations. Avigad's excavations of Jerusalem's Jewish Quarter uncovered a large, lavishly appointed Herodian-period residence, popularly named the Palatial Mansion, along with several other opulent houses in the same district, giving real archaeological texture to the kind of priestly-aristocratic residence implied by the high priest's house and courtyard in this chapter, though no inscription ties any specific excavated house to Annas or Caiaphas by name." }
'annas-high-priestly-dynasty-josephus' = @{ title="One family, five high priests"; body="Flavius Josephus, Jewish Antiquities, Book 20, section 198 (Loeb Classical Library, trans. Louis H. Feldman, Cambridge, MA: Harvard University Press). Josephus records that Annas (Ananus son of Seth), high priest roughly 6-15 CE, saw five of his own sons and his son-in-law Caiaphas hold the high priesthood in succession over the following decades, a family monopoly on the office that helps explain why a deposed former high priest could retain the real influence John's narrative assumes even without the formal Roman-recognized title." }
'annas-caiaphas-two-stage-hearing' = @{ title="Why Annas first? Three ways to read it"; body="Raymond E. Brown, The Death of the Messiah: From Gethsemane to the Grave, vol. 1 (New York: Doubleday, 1994), commentary ad loc. John 18:12-14, 19-24. Brown lays out the range of scholarly proposals for John's uniquely two-stage hearing, absent from all three Synoptics' single Caiaphas/Sanhedrin session: that it preserves a genuine informal-then-formal sequence; that it is a source-critical seam left by combining two originally separate traditions; or that it simply dramatizes Annas's well-attested ongoing influence as a kind of high-priest-emeritus power broker. Brown does not adjudicate definitively among the three." }
'john-18-13-24-textual-rearrangement' = @{ title="A manuscript fix for a confusing sequence"; body="Bruce M. Metzger, A Textual Commentary on the Greek New Testament, 2nd ed. (Stuttgart: Deutsche Bibelgesellschaft / United Bible Societies, 1994), ad loc. John 18:13-24. Metzger notes that some ancient versions, including forms of the Old Syriac tradition, relocate verse 24 ('Annas sent him bound to Caiaphas') to a position earlier in the sequence, an ancient harmonizing attempt to resolve the same ambiguity modern readers notice: as the Greek text stands, it is not immediately clear whether the interrogation described in verses 19-23 is being conducted by Annas or by Caiaphas." }
'peter-denial-bracketing-technique' = @{ title="A denial wrapped around a trial"; body="R. Alan Culpepper, Anatomy of the Fourth Gospel: A Study in Literary Design (Philadelphia: Fortress Press, 1983), chapter on Johannine narrative technique. Culpepper identifies John's interweaving of Peter's three denials around the Annas interrogation (18:15-18, then 18:25-27, bracketing 18:19-24) as a deliberate intercalation technique, structurally forcing the reader to hold Peter's collapsing courage and Jesus's steady testimony in view at the same time rather than reading them as two separate, sequential episodes." }
'passover-purity-chronology-discrepancy' = @{ title="A meal not yet eaten: John's Passover problem"; body="Raymond E. Brown, The Death of the Messiah: From Gethsemane to the Grave, vol. 1 (New York: Doubleday, 1994), commentary ad loc. John 18:28. Brown treats the ritual-purity detail here (the Jewish leaders avoiding the Gentile-occupied Praetorium so as not to be defiled before eating the Passover) as a genuine, substantive chronological conflict with the Synoptics, where the Last Supper the night before is explicitly presented as the Passover meal itself; in John's timeline, by contrast, the priests in this scene have not yet eaten the Passover meal on the very morning Jesus is being tried. Brown's own conclusion, shared by much mainstream critical scholarship, is that this is a real discrepancy rather than one fully reconcilable by harmonization." }
'sanders-passover-dating-debate' = @{ title="Two calendars, or one theological edit?"; body="E. P. Sanders, The Historical Figure of Jesus (London: Penguin, 1993), chapter on the death of Jesus. Sanders surveys harmonization proposals, including competing calendars in use among different Jewish groups or a private earlier Passover observance by Jesus's own group, alongside the simpler critical conclusion that John has likely shifted the date for theological reasons, and judges the harmonizing solutions each to carry their own unresolved difficulties." }
'ehrman-passover-discrepancy-view' = @{ title="A contradiction the Gospels do not resolve"; body="Bart D. Ehrman, Jesus, Interrupted: Revealing the Hidden Contradictions in the Bible (And Why We Don't Know About Them) (New York: HarperOne, 2009), chapter on contradictions in the Passion narratives. Ehrman presents the John-versus-Synoptic Passover dating conflict as one of the clearest examples of an unresolved internal contradiction in the Gospels, arguing that John's dating serves the Gospel's own theological interest in aligning Jesus's death with the slaughter of the Passover lambs rather than preserving an independently more accurate chronology." }
'lamb-slaughter-symbolic-dating' = @{ title="Dying when the lambs die"; body="Raymond E. Brown, The Death of the Messiah: From Gethsemane to the Grave, vol. 1 (New York: Doubleday, 1994), commentary ad loc. John 18:28 and John 19:14. Brown connects John's dating of the crucifixion to the afternoon of preparation day, when Passover lambs were slaughtered in the Temple, arguing the chronology serves the Gospel's Lamb-of-God theology, already introduced at John 1:29, rather than functioning as an independently attested historical corrective to the Synoptic timeline." }
'ius-gladii-roman-capital-authority' = @{ title="Whose authority to execute?"; body="Raymond E. Brown, The Death of the Messiah: From Gethsemane to the Grave, vol. 1 (New York: Doubleday, 1994), commentary ad loc. John 18:31. Brown surveys the long-running debate over whether the Sanhedrin under Roman rule genuinely lacked the legal authority to carry out executions, the Roman ius gladii, or right of the sword, being reserved to the governor, noting the tension with other evidence, including the stoning of Stephen in Acts 7 and rabbinic sources, that some capital process may still have operated under Jewish authority in some circumstances, while concluding the Gospel's own claim here fits its larger theological interest in Jesus dying specifically by Roman crucifixion rather than Jewish stoning." }
'pilate-caesarea-normal-residence' = @{ title="A governor who commuted for festivals"; body="Flavius Josephus, Jewish Antiquities, Book 18, section 55 (Loeb Classical Library, trans. Louis H. Feldman, Cambridge, MA: Harvard University Press). Josephus records that Judea's Roman prefects, including Pilate, normally resided at Caesarea Maritima on the coast and came up to Jerusalem chiefly for festivals such as Passover, when pilgrim crowds required a stronger security presence, which is why Pilate himself is available in the city to hear this case at all." }
'what-is-truth-johannine-irony' = @{ title="The question already answered"; body="D. A. Carson, The Gospel According to John, Pillar New Testament Commentary (Grand Rapids: Eerdmans, 1991), commentary ad loc. John 18:38; compare Craig S. Keener, The Gospel of John: A Commentary, vol. 2 (Peabody, MA: Hendrickson, 2003), ad loc. Both commentators read Pilate's 'What is truth?' as deliberate Johannine irony: Truth (aletheia) has already been established across the Gospel as a central theological term bound up with Jesus's own identity (John 14:6), so Pilate's dismissive question is answered, unrecognized, by the very person standing in front of him." }
'barabbas-pardon-custom-historicity' = @{ title="A custom no outside source confirms"; body="Raymond E. Brown, The Death of the Messiah: From Gethsemane to the Grave, vol. 1 (New York: Doubleday, 1994), commentary ad loc. John 18:39-40. Brown notes that the alleged Roman governor's custom of releasing one prisoner chosen by the crowd at Passover is not independently attested in Josephus, Philo, or any other first-century non-Christian source, and that no clear parallel Roman amnesty practice of this specific form is documented elsewhere in the empire, making its historicity a genuinely open and much-debated question rather than a documented fact external evidence can confirm." }
'barabbas-name-meaning-textual-variant' = @{ title="Son of the father, in more ways than one"; body="Textual apparatus of the Nestle-Aland Novum Testamentum Graece, ad loc. Matthew 27:16-17, where a minority of manuscripts read the prisoner's full name as 'Jesus Barabbas' rather than simply 'Barabbas.' Bart D. Ehrman, Jesus, Interrupted: Revealing the Hidden Contradictions in the Bible (New York: HarperOne, 2009), notes the name Bar-abba itself means 'son of the father' in Aramaic, an irony many scholars read as deliberate: the crowd is offered a choice between two men who could each be called a son of the father." }
'beloved-disciple-known-to-high-priest' = @{ title="A disciple with priestly connections"; body="Urban C. von Wahlde, The Gospel and Letters of John, vol. 2, Eerdmans Critical Commentary (Grand Rapids: Eerdmans, 2010), commentary ad loc. John 18:15-16. Von Wahlde surveys the source-critical puzzle of the unnamed 'other disciple... known to the high priest,' who gains Peter entry to the courtyard, a detail found only in John and traditionally, though not universally, identified with the Beloved Disciple already introduced earlier in the Gospel; von Wahlde treats the identification as plausible but not textually certain." }
'malchus-named-only-in-john' = @{ title="A name only one Gospel bothers to give"; body="Craig S. Keener, The Gospel of John: A Commentary, vol. 2 (Peabody, MA: Hendrickson, 2003), commentary ad loc. John 18:10. Keener observes that while all four Gospels report a sword-strike on the high priest's servant's ear during the arrest, only John names both the servant, Malchus, and the disciple who strikes him, Peter; the Synoptics leave both anonymous, a detail Keener reads as consistent with John's general tendency toward naming minor figures the Synoptics leave unnamed, though it cannot be independently verified beyond the Gospel's own report." }
}

# ---- Chapter beats (with [[NOTE:slug]] placeholders) ----
$beat1 = @'
John places the arrest in a garden across the Kidron valley, a spot Jesus and his disciples had used often enough that Judas knows exactly where to find him (18:1-2). Judas arrives leading not just Temple police but, in John's telling, a full military detachment [[NOTE:speira-roman-detachment-size]] — a speira, together with officers from the chief priests and Pharisees, carrying lanterns, torches, and weapons (18:3). Jesus, going out to meet them, asks whom they seek, and when they answer "Jesus of Nazareth," replies "ego eimi" — "I am he" — a phrase John has spent seventeen chapters loading with divine-name resonance (see this study's earlier discussion of the I-am sayings). At the words, the whole arresting party draws back and falls to the ground (18:4-6), a detail found nowhere in the Synoptic arrest accounts and read by mainstream commentary as a deliberate Johannine theophany flourish rather than reported combat choreography [[NOTE:ego-eimi-theophany-flourish]]. Jesus asks the question again, secures his disciples' safety, and only then does Simon Peter draw a sword and strike off the right ear of the high priest's servant, a man John alone names as Malchus [[NOTE:malchus-named-only-in-john]]. Jesus orders the sword put away and submits to being bound (18:10-12).

Whether a Roman military unit was actually involved before any formal charge existed is a real historical-plausibility question, not a settled fact either way. The term speira itself is elastic in contemporary Greek usage, capable of meaning anything from a small maniple of a few dozen men to a full cohort of several hundred, and scholars read the detail differently: some treat it as later narrative amplification, emphasizing Jesus's foreknowledge and total command of the scene rather than reporting an actual troop count; others point out that Rome did station a garrison cohort at the Antonia Fortress overlooking the Temple, reinforced specifically during pilgrim festivals for crowd-control reasons, which makes some small detachment plausible without confirming this particular one [[NOTE:antonia-fortress-garrison-crowd-control]].
'@

$beat2 = @'
Jesus is led first not to Caiaphas, the sitting high priest that year, but to Annas, his father-in-law (18:13) — a hearing found only in John and absent from all three Synoptics' single Caiaphas/Sanhedrin session. Annas had himself been high priest a decade earlier before Roman-appointed succession moved the office along, and formally held no legal standing to try anyone by the time of this scene; his continuing influence, though, was real and independently attested elsewhere — Josephus records that five of Annas's own sons, plus his son-in-law Caiaphas, would go on to hold the high priesthood in the following decades, a family monopoly on the office that explains why a retired high priest could still function as a genuine power broker [[NOTE:annas-high-priestly-dynasty-josephus]]. Scholars propose several ways to read John's unique two-stage sequence: a real informal-then-formal legal process; a seam left by combining two separate underlying traditions; or simply a dramatization of Annas's ongoing influence as a kind of high-priest-emeritus figure [[NOTE:annas-caiaphas-two-stage-hearing]]. The high priestly household itself would have resembled the kind of large, lavishly appointed residence excavated in Jerusalem's Upper City, giving the scene real archaeological texture even without a name on the door [[NOTE:palatial-mansion-avigad-excavation]].

While Jesus is inside, Peter waits at the courtyard door, let in only because another disciple known to the high priest, plausibly though not certainly the Beloved Disciple already introduced earlier in this Gospel [[NOTE:beloved-disciple-known-to-high-priest]], vouches for him. A servant girl keeping the door asks Peter directly whether he too is one of this man's disciples, and he answers, "I am not" (18:15-18) — the first of three denials John deliberately wraps around the Annas interrogation rather than reporting in one unbroken sequence [[NOTE:peter-denial-bracketing-technique]]. Annas questions Jesus about his disciples and his teaching; Jesus replies that he has spoken openly, in synagogues and the Temple, and has said nothing in secret, then invites Annas to ask those who actually heard him. An officer standing by strikes Jesus for the answer, and Jesus presses back on the fairness of being struck for speaking truthfully (18:19-23). This same household and its ossuary evidence were already discussed earlier in this study in connection with Caiaphas's own tomb find, and that ground is not worth retreading here.

The manuscript tradition itself preserves a trace of how confusing this sequence already felt to ancient readers: some early versions relocate verse 24, "Annas sent him bound to Caiaphas," to an earlier point in the passage, an ancient harmonizing attempt to clarify whether Annas or Caiaphas is doing the questioning in the verses just before it, exactly the same ambiguity a modern reader notices on a first pass [[NOTE:john-18-13-24-textual-rearrangement]].
'@

$beat3 = @'
Peter's second and third denials come at the charcoal fire the servants and officers have built against the night cold, where Peter has been standing and warming himself; asked again whether he is one of Jesus's disciples, and then specifically whether he was seen in the garden with him, Peter denies it both times, and immediately, a rooster crows (18:25-27). John does not pause to record Peter's reaction the way Luke does; the crow simply lands and the narrative moves on, letting the fulfillment of Jesus's own prediction speak for itself. From Caiaphas, Jesus is led early in the morning to the Praetorium, the Roman governor's headquarters, but the Jewish leaders themselves stop at the threshold and refuse to go in, unwilling to be ritually defiled before eating the Passover (18:28).

That single clause creates one of the most genuinely disputed chronological problems anywhere in the Gospels, and it deserves an honest, even-handed look rather than a quick harmonization. In the Synoptics, the meal Jesus shares with his disciples the night before is explicitly the Passover meal itself. In John's timeline, by contrast, the priests in this very scene have not yet eaten the Passover meal on the morning of Jesus's trial, meaning John's Last Supper the night before was not, in his chronology, a Passover meal at all. Mainstream critical scholarship, including Raymond Brown's detailed treatment, concludes this is a real discrepancy between the two chronologies, not one current evidence can fully reconcile [[NOTE:passover-purity-chronology-discrepancy]]. E. P. Sanders surveys the harmonization attempts on offer, competing calendars among different Jewish groups, an early private Passover observance by Jesus's own group, and finds each carries its own unresolved difficulties [[NOTE:sanders-passover-dating-debate]]; Bart Ehrman treats the conflict more bluntly, as one of the Gospels' clearest unresolved internal contradictions [[NOTE:ehrman-passover-discrepancy-view]]. The likeliest explanation on offer is theological rather than a simple reporting error: John appears to have shifted his dating so that Jesus's death falls at the exact hour the Passover lambs are being slaughtered in the Temple, extending the Lamb of God language this Gospel introduced back at its opening [[NOTE:lamb-slaughter-symbolic-dating]].
'@

$beat4 = @'
Pilate comes out to the leaders rather than have them come in, asking what accusation they bring. Their answer is circular, "if this man were not doing evil, we would not have delivered him over to you," and Pilate's response, "take him yourselves and judge him by your own law," draws out their real admission: "it is not lawful for us to put anyone to death." John reads this exchange as fulfilling Jesus's own earlier prediction of the manner of his death (18:29-32). Whether the Sanhedrin genuinely lacked the legal authority to execute under Roman rule, or retained some narrower capital process of its own, is itself a live scholarly question — Rome's governors are usually understood to have reserved the ius gladii, the right of the sword, to themselves, though other evidence, including Stephen's stoning in Acts 7, complicates a completely clean reading of Jewish authorities as powerless in every case [[NOTE:ius-gladii-roman-capital-authority]]. Pilate himself is available to hear any of this only because Judea's Roman governors normally resided down the coast at Caesarea and came up to Jerusalem specifically for festivals like this one, when the pilgrim crowds required a stronger hand [[NOTE:pilate-caesarea-normal-residence]].

The Pilate we meet across this scene, patient enough to shuttle back and forth between an angry crowd and a private prisoner and to ask philosophical questions, sits somewhat uneasily against the historical portrait handed down by his contemporaries. Philo of Alexandria, writing within Pilate's own lifetime and no admirer of his, describes an inflexible, corrupt governor prone to gratuitous cruelty and executions without trial, a real tension worth noting between the reluctant, almost sympathetic judge of John's narrative and the far harsher administrator glimpsed elsewhere [[NOTE:philo-pilate-character]].

Pilate then goes back inside and questions Jesus privately about kingship. Jesus answers that his kingdom is not of this world, if it were, his own people would be fighting to keep him from being handed over, and when Pilate presses further, Jesus answers that he was born and came into the world for one purpose: to bear witness to the truth, and that everyone of the truth hears his voice. Pilate's reply is two words: "What is truth?" (18:33-38a). It is one of the most quoted and most interpreted lines in the entire Gospel, and mainstream commentary reads it as deliberate Johannine irony rather than idle philosophy: Truth has already been built up across this Gospel as one of its central theological terms, tied directly to Jesus's own identity, so Pilate is asking, unknowingly, the very question Jesus has just finished answering by standing in front of him [[NOTE:what-is-truth-johannine-irony]].
'@

$beat5 = @'
Pilate goes back out and tells the assembled leaders he finds no guilt in Jesus at all, then offers to invoke a custom of releasing one prisoner to them at Passover. The crowd shouts back not for Jesus but for Barabbas, whom John identifies simply as a robber (18:38b-40).

The custom itself is worth pausing on honestly rather than assuming it as settled fact, because no independent first-century source, not Josephus, not Philo, not any Roman administrative record, confirms that Roman governors of Judea, or of anywhere else in the empire, observed any such festival-amnesty practice. That silence makes the custom's historicity a genuinely open and actively debated question among scholars, not a documented detail external evidence corroborates [[NOTE:barabbas-pardon-custom-historicity]]. There is also a curious irony buried in the name itself: a minority of ancient manuscripts of Matthew's parallel account give the prisoner's full name as "Jesus Barabbas," and Bar-abba means, in Aramaic, simply "son of the father," so that, on that reading, the crowd is offered a choice between two men each of whom could be called a son of the father, one bearing the name openly, the other only by what his title had come to mean [[NOTE:barabbas-name-meaning-textual-variant]]. The chapter ends there, its final beat a plain outcome with no comment from the narrator: the crowd's choice made, Jesus still bound, still uncondemned by Rome, still awaiting whatever Pilate does next.
'@

$beats = @($beat1, $beat2, $beat3, $beat4, $beat5)

# ---- Glossary additions (heading -> body) ----
$glossary = [ordered]@{
'SPEIRA (ROMAN MILITARY DETACHMENT)' = "The Greek term John uses for the armed body accompanying Judas at the arrest (18:3), elastic enough in contemporary usage to mean anything from a small maniple of a few dozen soldiers up to a full cohort of several hundred. Its presence alongside the Temple police raises a genuine historical-plausibility question about whether Rome would commit troops to arrest one Galilean teacher before any formal charge existed [[NOTE:speira-roman-detachment-size]], though a Roman garrison cohort is independently attested at the nearby Antonia Fortress, reinforced during festival crowds [[NOTE:antonia-fortress-garrison-crowd-control]]."
'KIDRON VALLEY' = "The valley east of Jerusalem's Temple Mount that Jesus and his disciples cross to reach the garden where he is arrested (18:1). John does not name the garden itself, unlike the Synoptics' Gethsemane, locating it only by this crossing."
'THE PALATIAL MANSION (ARCHAEOLOGICAL SITE)' = "A large, lavishly appointed Herodian-period residence excavated in Jerusalem's Upper City Jewish Quarter, among several opulent houses of the same district plausibly connected to the priestly aristocracy of Jesus's day. Cited here as archaeological texture for the kind of high priestly household implied by Annas's and Caiaphas's residences in this chapter, though no excavated structure is inscribed with either man's name [[NOTE:palatial-mansion-avigad-excavation]]."
'MALCHUS' = "The high priest's servant whose right ear Peter cuts off with a sword during the arrest in the garden (18:10); Jesus heals the wound in Luke's parallel account (Luke 22:51), though John does not record a healing. Only John names both the servant and the disciple who strikes him; the Synoptics leave both anonymous [[NOTE:malchus-named-only-in-john]]."
'ANNAS' = "Former high priest, in office roughly 6-15 CE, and father-in-law of the sitting high priest Caiaphas, to whom Jesus is taken first for questioning (18:13, 19-24) despite Annas holding no formal legal standing under Roman-appointed succession. His continuing influence is independently attested: Josephus records that five of his sons and his son-in-law Caiaphas all later held the high priesthood in turn [[NOTE:annas-high-priestly-dynasty-josephus]]. Scholars read the unique Annas-then-Caiaphas sequence, found only in John, as reflecting either a real two-stage process, a source-critical seam, or simply Annas's ongoing influence as a power broker [[NOTE:annas-caiaphas-two-stage-hearing]]."
'CHARCOAL FIRE (JOHN 18:18)' = "The fire of coals the servants and officers build in the high priest's courtyard against the night cold, where Peter stands warming himself when he is asked for the second and third time whether he is one of Jesus's disciples (18:18, 25). The specific Greek word for this fire, anthrakia, recurs only once more in the New Testament, in John 21:9, where the risen Jesus has built one on the shore of the Sea of Galilee for the reunion breakfast at which Peter is restored, an echo many literary readers of the Gospel connect back to this scene of denial."
'BARABBAS' = "The prisoner John identifies simply as 'a robber' (18:40), whom the crowd chooses for release over Jesus under the alleged Passover amnesty custom. His name, Bar-abba, means 'son of the father' in Aramaic, and a minority of ancient manuscripts of Matthew's parallel account give his full name as 'Jesus Barabbas' [[NOTE:barabbas-name-meaning-textual-variant]]."
'PONTIUS PILATE' = "The Roman prefect of Judea who presides over Jesus's trial in this chapter, questioning him privately about kingship and declaring he finds no guilt in him (18:29-38). Judea's governors normally resided at Caesarea Maritima on the coast, coming to Jerusalem chiefly for festivals such as this one [[NOTE:pilate-caesarea-normal-residence]]; the comparatively patient, philosophically curious Pilate of this scene sits in some tension with the harsher portrait of the man given by his contemporary Philo of Alexandria [[NOTE:philo-pilate-character]]."
'PRAETORIUM (PILATE''S HEADQUARTERS)' = "Herod the Great's former palace in Jerusalem, used as the Roman governor's residence and headquarters during his visits to the city, where Jesus's trial before Pilate takes place (18:28-38). The Jewish leaders themselves refuse to enter it, to avoid ritual defilement before eating the Passover meal [[NOTE:passover-purity-chronology-discrepancy]], so Pilate conducts much of the exchange by stepping outside to them."
'RITUAL PURITY BEFORE PASSOVER (JOHN 18:28)' = "The stated reason the Jewish leaders will not enter the Praetorium: entering a Gentile residence would render them ritually unclean and unable to eat the Passover meal. The detail creates one of the most substantive, honestly disputed chronological tensions between John and the Synoptics, since it implies the priests in this scene have not yet eaten the Passover meal on the very morning of Jesus's trial, in contrast to the Synoptics' explicit Passover Last Supper the night before [[NOTE:passover-purity-chronology-discrepancy]]."
'PASSOVER AMNESTY CUSTOM (UNATTESTED OUTSIDE THE GOSPELS)' = "The alleged practice, invoked by Pilate here (18:39), of a Roman governor releasing one prisoner chosen by the crowd at Passover. No independent first-century source, Jewish or Roman, confirms any such custom, making its historicity a genuinely open scholarly question rather than an established fact [[NOTE:barabbas-pardon-custom-historicity]]."
'"WHAT IS TRUTH?" (JOHN 18:38)' = "Pilate's two-word reply to Jesus's statement that he came into the world to bear witness to the truth (18:37-38), among the most quoted lines in the Gospel. Mainstream commentary reads it as deliberate authorial irony: Truth has already been established across John as a central theological term tied to Jesus's own identity, so Pilate unknowingly asks the very question Jesus has just answered by standing in front of him [[NOTE:what-is-truth-johannine-irony]]."
}

# ---- Insert Notes ----
$slugToNumber = @{}
foreach ($slug in $notes.Keys) {
    $n = $notes[$slug]
    $noteNum = $maxNoteNumber + 1
    $maxNoteNumber = $noteNum
    $text = "$noteNum — $($n.title)`n`n$($n.body)"
    $id = New-BeatRow $text
    $maxNoteSortKey += 50
    Add-BeatNode $NotesNodeId $id $maxNoteSortKey
    $slugToNumber[$slug] = $noteNum
}
Write-Host "Inserted $($notes.Count) notes, ending at [$maxNoteNumber]"

# ---- Insert chapter beats with placeholder replacement ----
$sortKey = 0.0
foreach ($beatText in $beats) {
    $resolved = $beatText
    foreach ($slug in $slugToNumber.Keys) {
        $resolved = $resolved.Replace("[[NOTE:$slug]]", "[$($slugToNumber[$slug])]")
    }
    $sortKey += 100
    $id = New-BeatRow $resolved
    Add-BeatNode $Ch18NodeId $id $sortKey
}
Write-Host "Inserted $($beats.Count) chapter beats"

# ---- Insert glossary entries ----
foreach ($heading in $glossary.Keys) {
    $body = $glossary[$heading]
    foreach ($slug in $slugToNumber.Keys) {
        $body = $body.Replace("[[NOTE:$slug]]", "[$($slugToNumber[$slug])]")
    }
    $text = "$heading`n`n$body"
    $id = New-BeatRow $text
    $maxGlossarySortKey += 50
    Add-BeatNode $GlossaryNodeId $id $maxGlossarySortKey
}
Write-Host "Inserted $($glossary.Count) glossary entries"

# ---- Seed new entities ----
Seed-Entity "The Palatial Mansion" "the-palatial-mansion" "place" "Herodian-period aristocratic residence excavated in Jerusalem's Upper City Jewish Quarter; archaeological analogue for a high priestly household of Jesus's day, discovered by Nahman Avigad."
Seed-Entity "Speira (Roman Military Cohort)" "speira-roman-military-cohort" "vocabulary" "Greek term for the armed detachment accompanying Judas at Jesus's arrest (John 18:3); ranges in meaning from a small maniple to a full cohort of several hundred soldiers."

$conn.Close()
Write-Host "DONE Chapter 18."
