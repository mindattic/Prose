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
$NotesNodeId = [guid]"019FA01D-FA22-76C6-976C-3EA4F4D54A14"
$GlossaryNodeId = [guid]"019FA026-3758-7A57-AF65-EC1DB4303EF3"
$Ch26NodeId = [guid]"019FA074-4A7C-7119-AF4B-1F2DB39E5FC8"

$maxNoteSortKey = [double](Exec-Scalar "SELECT ISNULL(MAX(bn.SortKey),0) FROM BeatNodes bn WHERE bn.NodeId='019FA01D-FA22-76C6-976C-3EA4F4D54A14' AND bn.IsEnabled=1")
$maxGlossarySortKey = [double](Exec-Scalar "SELECT ISNULL(MAX(bn.SortKey),0) FROM BeatNodes bn WHERE bn.NodeId='019FA026-3758-7A57-AF65-EC1DB4303EF3' AND bn.IsEnabled=1")
$maxChapterSortKey = [double](Exec-Scalar "SELECT ISNULL(MAX(bn.SortKey),0) FROM BeatNodes bn WHERE bn.NodeId='019FA074-4A7C-7119-AF4B-1F2DB39E5FC8' AND bn.IsEnabled=1")
$maxNoteNumber = [int](Exec-Scalar "SELECT ISNULL(MAX(CAST(LEFT(b.Text, CHARINDEX(' ',b.Text)-1) AS INT)),0) FROM BeatNodes bn JOIN Beats b ON bn.BeatId=b.Id WHERE bn.NodeId='019FA01D-FA22-76C6-976C-3EA4F4D54A14' AND bn.IsEnabled=1 AND CHARINDEX(' ', b.Text) > 1 AND LEFT(b.Text, CHARINDEX(' ',b.Text)-1) NOT LIKE '%[^0-9]%'")

Write-Host "Starting: MaxNumber=$script:MaxNumber MaxNoteSortKey=$maxNoteSortKey MaxNoteNumber=$maxNoteNumber MaxGlossarySortKey=$maxGlossarySortKey MaxChapterSortKey=$maxChapterSortKey"

# ---- Notes (slug -> title/body) in order ----
$notes = [ordered]@{
'zechariah-11-thirty-pieces' = @{ title='Thirty pieces of silver, straight from Zechariah'; body="W. D. Davies and Dale C. Allison Jr., Matthew 19-28, International Critical Commentary (Edinburgh: T&T Clark, 1997), commentary ad loc. Matthew 26:14-16. Judas's price — thirty pieces of silver, agreed with the chief priests before the Passover meal even begins — matches, closely enough that the correspondence is treated as deliberate rather than coincidental, Zechariah's account of a rejected shepherd paid off and casting the money into the Temple treasury: `"they weighed out as my wages thirty pieces of silver... So I took the thirty pieces of silver and threw them into the house of the LORD, to the potter`" (Zechariah 11:12-13). Matthew doesn't cite that text explicitly here; the explicit citation, misattributed instead to `"Jeremiah,`" comes three chapters later when the money itself resurfaces (27:9-10) — a separate, well-documented textual puzzle [51]. What belongs at this earlier point is only the narrative fact that the price itself already echoes a specific, checkable prophetic passage before the text ever announces the connection out loud." }
'exodus-21-32-slave-price' = @{ title="A second echo: the price of a gored slave"; body="R. T. France, The Gospel of Matthew, New International Commentary on the New Testament (Grand Rapids, MI: Eerdmans, 2007), commentary ad loc. Matthew 26:15. Thirty shekels of silver is also, independently of Zechariah, the compensation Exodus 21:32 fixes for an ox that gores a slave to death — the Torah's own legal valuation of a slave's life. Commentators read the coincidence, or deliberate echo, as adding a second scriptural layer under Judas's price: not only a rejected shepherd's wages but a slave's replacement value, both landing on the same number. Whether Matthew's first audience was expected to hear both echoes at once isn't settled by the text itself." }
'eph-ho-parei-crux' = @{ title='"Friend, why have you come" — a phrase built not to quite answer'; body="Ulrich Luz, Matthew 21-28, Hermeneia series, trans. James E. Crouch (Minneapolis: Fortress Press, 2005), commentary ad loc. Matthew 26:50. The Greek behind Jesus's reply to Judas at the kiss — `"hetaire, eph' ho parei`" — is genuinely difficult to render cleanly. Hetaire is a cool, formal address (`"comrade`" or `"friend`" in a distancing sense, used elsewhere in this Gospel only for people being confronted — a landowner to a grumbling laborer, a king to a guest without a wedding garment) and eph' ho parei is compressed enough that translators disagree over whether it's a question (`"why have you come?`"), a command (`"do what you are here to do`"), or something left deliberately unfinished. Luz surveys the major options without declaring the ambiguity solved; modern translations visibly split on the point, which is itself the honest state of the underlying Greek." }
'matthew-only-sword-saying' = @{ title="`"All who take the sword`" — Matthew's own line, not the other Synoptics'"; body="W. D. Davies and Dale C. Allison Jr., Matthew 19-28, International Critical Commentary (Edinburgh: T&T Clark, 1997), commentary ad loc. Matthew 26:51-54. All three Synoptic Gospels report an unnamed follower drawing a sword and striking off the high priest's servant's ear (Mark 14:47; Luke 22:49-50; Matthew 26:51), but what each writer has Jesus say next differs. Mark records no rebuke at all. Matthew alone gives Jesus the extended reply — `"Put your sword back into its place. For all who take the sword will perish by the sword`" (26:52) — followed by the claim that Jesus could summon `"more than twelve legions of angels`" and chooses not to (26:53). Neither line appears in Mark's or Luke's version of the same scene, making both Matthew's own distinctive theological addition to a shared arrest tradition rather than a detail independently multiply attested." }
'luke-heals-severed-ear' = @{ title="Luke's own addition, in the opposite direction"; body="Joseph A. Fitzmyer, The Gospel According to Luke X-XXIV, Anchor Bible vol. 28A (Garden City, NY: Doubleday & Company, 1985), commentary ad loc. Luke 22:51. Where Matthew adds a saying condemning the sword-strike, Luke's parallel account adds the opposite kind of detail: Jesus touches the severed ear and heals it (22:51), a healing found in no other Gospel's telling of the arrest. Each Synoptic writer, working from what looks like the same underlying tradition — an unnamed disciple, a sword, a severed ear — supplies his own distinct elaboration on top of it, pulling in a different direction." }
'high-priest-tearing-garments-tension' = @{ title="The high priest tears his robe — against his own law"; body="Jacob Neusner, trans., The Mishnah: A New Translation (New Haven: Yale University Press, 1988), Sanhedrin 7:5; Raymond E. Brown, The Death of the Messiah: A Commentary on the Passion Narratives in the Four Gospels, vol. 1 (New York: Doubleday, 1994), commentary on Matthew 26:65. Leviticus 21:10 specifically forbids the high priest, uniquely among priests, from tearing his garments in mourning or distress. Caiaphas does exactly that on hearing what he judges blasphemy (26:65). Rabbinic legal tradition preserved in the Mishnah treats hearing blasphemy as its own separate category, licensing the tear regardless of the general prohibition (Sanhedrin 7:5) — a real legal tension the Gospel narrative doesn't pause to resolve, and one whose documented rabbinic exception postdates the scene by well over a century, the same kind of methodological gap this chapter has already flagged once for the Sanhedrin's other procedural rules [19]." }
'two-or-three-witnesses-deuteronomy' = @{ title="Witnesses enough, on paper"; body="W. D. Davies and Dale C. Allison Jr., Matthew 19-28, International Critical Commentary (Edinburgh: T&T Clark, 1997), commentary ad loc. Matthew 26:59-61; Raymond E. Brown, The Death of the Messiah, vol. 1 (New York: Doubleday, 1994), discussion of the false-witness scene. Deuteronomy 19:15 requires at least two or three witnesses to establish any charge. Matthew's account has the council actively seeking usable testimony, coming up short more than once, before `"at last two came forward`" with a garbled version of a Temple-destruction saying (26:60-61) — technically clearing the numerical bar the law sets. Mark's parallel account of the identical scene goes further, stating outright that the witnesses' testimony `"did not agree`" (Mark 14:56, 59) — an explicit admission of procedural failure that Matthew's retelling doesn't repeat, presenting a comparatively cleaner, if still evidently engineered, proceeding." }
'criterion-embarrassment-peter-denial' = @{ title="Peter's denial across all four Gospels: embarrassment, multiply attested"; body="Bart D. Ehrman, Did Jesus Exist? The Historical Argument for Jesus of Nazareth (New York: HarperOne, 2012), discussion of the criterion of embarrassment applied to Peter's denial. This book has already applied the criterion of embarrassment once, to Jesus's own baptism [16]; Peter's threefold denial and bitter weeping (26:69-75) is historical-Jesus scholarship's other standard textbook case of the same reasoning, and by most readings a stronger one, because it clears a second bar as well: all four Gospels include it independently, each naming Peter specifically rather than leaving the failure to an anonymous disciple (Mark 14:66-72; Luke 22:54-62; John 18:15-18, 25-27). A movement inventing its own founding story had every reason to omit or soften a scene in which its most prominent named leader publicly and repeatedly denies knowing its central figure hours before that figure's execution. That all four independent traditions kept the scene anyway, with matching essential shape despite real differences in staging detail, is read as evidence the core memory predates any of the four written Gospels." }
}

# ---- Chapter beats (with [[NOTE:slug]] placeholders) ----
$beat1 = @'
Two details in Judas's bargain reward a closer look than the narrative itself gives them. The price — thirty pieces of silver, agreed with the chief priests before the Passover meal even begins (26:14-16) — isn't a neutral number. It matches, closely enough that the correspondence is treated as deliberate rather than coincidental, Zechariah's account of a rejected shepherd paid off and casting the money into the Temple treasury (Zechariah 11:12-13) [[NOTE:zechariah-11-thirty-pieces]]. Matthew doesn't cite that text explicitly here; the explicit citation — misattributed instead to "Jeremiah" — comes three chapters later, when the money itself resurfaces at 27:9-10, a separate, well-documented textual puzzle this book takes up in its own place at that later point. What belongs here is only the narrative fact that the price itself already echoes a specific, checkable prophetic passage before the text ever announces the connection out loud.

A second, independent echo sits underneath the same number. Exodus 21:32 fixes thirty shekels of silver as the compensation owed when an ox gores a slave to death — the Torah's own legal valuation of a slave's life [[NOTE:exodus-21-32-slave-price]]. Whether Matthew's audience was expected to hear both echoes at once, or whether the second is a modern commentator's overlay onto a number that was simply the going rate for something cheap, isn't something the text settles either way; both readings are argued in the literature, and neither requires the other to be true.
'@

$beat2 = @'
The arrest itself turns on two small, close-together moments worth isolating. Judas approaches, says "Greetings, Rabbi," and kisses Jesus (26:49) — the agreed signal (26:48) — and Jesus's reply back to him is genuinely hard to translate cleanly: hetaire, eph' ho parei, rendered variously as "Friend, why have you come?", "Friend, do what you came for," or left closer to its compressed, unfinished Greek shape entirely [[NOTE:eph-ho-parei-crux]]. The word Matthew's Jesus uses for "friend" elsewhere in this Gospel is reserved for morally loaded confrontations — a landowner to a grumbling laborer, a king to a wedding guest without the right garment — which colors the address as cool rather than warm, whatever the exact sense of the second half turns out to be.

Then a disciple — unnamed in this Gospel — draws a sword and cuts off the ear of the high priest's servant (26:51), and here the three Synoptic accounts genuinely diverge in what each writer adds to what looks like the same base scene. Mark records the strike and moves on without a word of rebuke. Luke keeps any rebuke brief but adds a detail found nowhere else: Jesus reaches out and heals the severed ear (Luke 22:51) [[NOTE:luke-heals-severed-ear]]. Matthew takes the opposite path — no healing gesture, but an extended reply instead: "Put your sword back into its place. For all who take the sword will perish by the sword" (26:52), followed by the claim that Jesus could call down "more than twelve legions of angels" and chooses not to (26:53), a saying and a claim belonging to Matthew alone among the three Synoptic versions of this moment [[NOTE:matthew-only-sword-saying]]. Only the Gospel of John, telling this same night yet again, supplies the two names this Gospel withholds — the disciple as Peter, the servant as Malchus — a naming this campaign's discussion of John's own account of that night takes up directly rather than repeating here. What Matthew keeps anonymous, and adds instead, is its own distinctive moral: a rejection of violent resistance placed in Jesus's mouth at the exact moment resistance was offered on his behalf.
'@

$beat3 = @'
The trial's evidentiary shape is worth a closer look than the narrative gives it credit for needing. Deuteronomy 19:15 sets the baseline for any capital charge: at least two or three witnesses. Matthew's account has the council actively hunting for testimony it can use, coming up empty more than once, before "at last two" step forward with a garbled version of a Temple-destruction saying (26:59-61) — a proceeding that clears the numerical bar the law sets [[NOTE:two-or-three-witnesses-deuteronomy]]. Mark's version of the identical scene goes a step further and states outright that the witnesses' accounts "did not agree" (Mark 14:56, 59) — an explicit admission of the very failure Matthew's retelling quietly doesn't repeat, whether by compression, by a cleaner source, or by a writer smoothing over an awkward detail already present in his tradition.

The verdict scene adds its own small legal wrinkle. Caiaphas, on hearing what he judges blasphemy, tears his own robe (26:65) — a gesture Leviticus 21:10 specifically forbids the high priest, uniquely among priests, from performing. Rabbinic legal tradition later carved out an exception for exactly this situation, treating a report of blasphemy as its own separate category that licensed the tear regardless of the general mourning prohibition [[NOTE:high-priest-tearing-garments-tension]]. That exception is only clearly documented centuries after Caiaphas's own lifetime — the same methodological caveat this chapter has already raised once about the Mishnah's other, procedural rules for capital trials [19] — real evidence of later practice, not proof the same allowance already existed in the 30s CE, but a detail the narrative happens to get right regardless of exactly when the rule crystallized.
'@

$beat4 = @'
Peter's collapse at the end of the chapter — three denials, a stranger's accent recognized and disowned each time, a rooster crowing exactly on schedule, and Peter going out to weep bitterly (26:69-75) — deserves to be named for what it is methodologically, not only narratively. This book has already applied the criterion of embarrassment once, to Jesus's own baptism [16]; Peter's denial is historical-Jesus scholarship's other standard example of the same reasoning, and by most readings a stronger one, because it clears a second bar as well as the first: all four Gospels include it independently, each naming Peter specifically rather than leaving the failure to an anonymous disciple (Mark 14:66-72; Luke 22:54-62; John 18:15-18, 25-27) [[NOTE:criterion-embarrassment-peter-denial]]. A movement inventing its own founding story had every reason to omit or soften a scene in which its most prominent named leader publicly and repeatedly denies knowing its central figure, hours before that figure's execution. That all four independent traditions kept the scene anyway, with the same essential shape despite real differences in staging detail, is read as evidence the core memory — not necessarily every line of dialogue, but the fact and shape of the failure itself — predates any of the four written Gospels.
'@

$beats = @($beat1, $beat2, $beat3, $beat4)

# ---- Glossary additions (heading -> body) ----
$glossary = [ordered]@{
'THIRTY PIECES OF SILVER (ZECHARIAH 11:12-13 CITATION)' = "The price Judas accepts from the chief priests to betray Jesus (26:15), matching Zechariah 11:12-13's account of a rejected shepherd's wages cast into the Temple treasury [[NOTE:zechariah-11-thirty-pieces]], and independently echoing Exodus 21:32's legal price for a slave gored to death by an ox [[NOTE:exodus-21-32-slave-price]]. Matthew does not cite Zechariah by name at this point in the narrative; the Gospel's explicit citation of the related material, misattributed to `"Jeremiah,`" comes later, when the money resurfaces at 27:9-10."
'CRITERION OF EMBARRASSMENT' = "A tool of historical-Jesus scholarship, formalized in John P. Meier's A Marginal Jew project: a detail that would have been awkward or theologically inconvenient for the early church to invent is judged more likely to be historically genuine, on the reasoning that a movement shaping its own founding story has every incentive to omit or soften such details rather than manufacture them. First applied in this book to Jesus's baptism by John [16]; applied a second time to Peter's threefold denial of Jesus, a scene independently preserved across all four canonical Gospels despite naming the movement's most prominent later leader as its failure [[NOTE:criterion-embarrassment-peter-denial]]."
}

# ---- Insert Notes ----
$slugToNumber = @{}
foreach ($slug in $notes.Keys) {
    $n = $notes[$slug]
    $noteNum = $maxNoteNumber + 1
    $maxNoteNumber = $noteNum
    $text = "$noteNum - $($n.title)`n`n$($n.body)"
    $id = New-BeatRow $text
    $maxNoteSortKey += 50
    Add-BeatNode $NotesNodeId $id $maxNoteSortKey
    $slugToNumber[$slug] = $noteNum
}
Write-Host "Inserted $($notes.Count) notes, ending at [$maxNoteNumber]"

# ---- Insert chapter beats with placeholder replacement ----
$sortKey = $maxChapterSortKey
foreach ($beatText in $beats) {
    $resolved = $beatText
    foreach ($slug in $slugToNumber.Keys) {
        $resolved = $resolved.Replace("[[NOTE:$slug]]", "[$($slugToNumber[$slug])]")
    }
    $sortKey += 100
    $id = New-BeatRow $resolved
    Add-BeatNode $Ch26NodeId $id $sortKey
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
Seed-Entity "Thirty Pieces of Silver (Zechariah 11:12-13)" "thirty-pieces-of-silver-zechariah-11-12-13" "vocabulary" "Judas's payment for betraying Jesus (Matthew 26:15), matching Zechariah 11:12-13's rejected-shepherd wages and Exodus 21:32's slave-gored-by-ox price."

$conn.Close()
Write-Host "DONE Chapter 26 depth pass."
