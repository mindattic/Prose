$ErrorActionPreference = 'Stop'   # fail loudly: a SqlException is otherwise non-terminating
$OutputEncoding = [System.Text.UTF8Encoding]::new($false)
[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)

Add-Type -AssemblyName System.Data

$connStr = "Server=(localdb)\MSSQLLocalDB;Database=Prose;Trusted_Connection=True;TrustServerCertificate=True;"
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
$Ch23NodeId = [guid]"019FA072-2582-769C-A2B5-85E052E09347"

$maxNoteSortKey = [double](Exec-Scalar "SELECT ISNULL(MAX(bn.SortKey),0) FROM BeatNodes bn WHERE bn.NodeId='019FA01D-FA22-76C6-976C-3EA4F4D54A14' AND bn.IsEnabled=1")
$maxGlossarySortKey = [double](Exec-Scalar "SELECT ISNULL(MAX(bn.SortKey),0) FROM BeatNodes bn WHERE bn.NodeId='019FA026-3758-7A57-AF65-EC1DB4303EF3' AND bn.IsEnabled=1")
$maxNoteNumber = [int](Exec-Scalar "SELECT ISNULL(MAX(CAST(LEFT(b.Text, CHARINDEX(' ',b.Text)-1) AS INT)),0) FROM BeatNodes bn JOIN Beats b ON bn.BeatId=b.Id WHERE bn.NodeId='019FA01D-FA22-76C6-976C-3EA4F4D54A14' AND bn.IsEnabled=1 AND CHARINDEX(' ', b.Text) > 1 AND LEFT(b.Text, CHARINDEX(' ',b.Text)-1) NOT LIKE '%[^0-9]%'")
$maxChapterSortKey = [double](Exec-Scalar "SELECT ISNULL(MAX(bn.SortKey),0) FROM BeatNodes bn WHERE bn.NodeId='019FA072-2582-769C-A2B5-85E052E09347' AND bn.IsEnabled=1")

Write-Host "Starting: MaxNumber=$script:MaxNumber MaxNoteSortKey=$maxNoteSortKey MaxNoteNumber=$maxNoteNumber MaxGlossarySortKey=$maxGlossarySortKey MaxChapterSortKey=$maxChapterSortKey"

# ---- Notes (slug -> title/body) in order ----
$notes = [ordered]@{
'phylacteries-tefillin-qumran-yadin' = @{ title='Tefillin recovered from the Qumran caves'; body="Yigael Yadin, Tefillin from Qumran (XQ Phyl 1-4) (Jerusalem: Israel Exploration Society, 1969). Yadin's publication of tefillin recovered from the Qumran caves documents small leather boxes containing the same four scriptural passages (Exodus 13:1-10, 13:11-16, Deuteronomy 6:4-9, 11:13-21) still used in tefillin today, physically dating the practice Matthew's Jesus criticizes as ostentatious display to the first century BCE/CE, the same period as the Pharisees he addresses." }
'deuteronomy-tefillin-literal-reading' = @{ title="Binding it 'as a sign': a literal reading with a long pedigree"; body="Jeffrey H. Tigay, The JPS Torah Commentary: Deuteronomy (Philadelphia: Jewish Publication Society, 1996), excursus on tefillin and mezuzot, commentary ad loc. Deuteronomy 6:8 and 11:18. Tigay traces how the instruction to 'bind them as a sign on your hand, and let them be as frontlets between your eyes' came to be read literally rather than metaphorically, and how that literal reading — producing the physical tefillin object — was already well established before the Mishnah's later codification of the practice's details." }
'tzitzit-numbers-command-milgrom' = @{ title="A blue cord at every corner"; body="Jacob Milgrom, The JPS Torah Commentary: Numbers (Philadelphia: Jewish Publication Society, 1990), commentary ad loc. Numbers 15:38-39. Milgrom works through the fringe (tzitzit) command's construction (a blue-dyed cord attached to a corner tassel) and situates it within the wider ancient Near Eastern convention that an elaborately fringed hem signaled an important wearer, directly relevant to Matthew 23:5's charge that the scribes and Pharisees lengthen their fringes for status display." }
'bar-kokhba-cave-letters-textiles-yadin' = @{ title="Fringed garments from the Judean Desert caves"; body="Yigael Yadin, The Finds from the Bar Kokhba Period in the Cave of Letters (Jerusalem: Israel Exploration Society, 1963). Yadin's excavation of the Cave of Letters recovered a substantial corpus of first- and second-century CE textiles, including tunics with decorative corner and border treatment consistent with fringed, tzitzit-style garments, extending the material record for the practice across the same Judean Desert region and into the two generations following Jesus's own lifetime." }
'isaiah-five-woes-westermann' = @{ title="The woe oracle as a recognized prophetic form"; body="Claus Westermann, Basic Forms of Prophetic Speech, trans. Hugh Clayton White (Philadelphia: Westminster Press, 1967), chapter on prophetic judgment speech and the woe oracle (Wehe-Wort). Westermann's form-critical study treats the woe oracle — a formulaic opening exclamation followed by indictment and threatened consequence — as a distinct, recurring unit of Hebrew prophetic speech with roots in older funerary lament and covenant-curse language, not a device invented for any single passage; Isaiah's own extended run of six such woes against social sins (Isaiah 5:8-23) is a standard example of the form." }
'habakkuk-five-woes-andersen' = @{ title="Five woes, one taunt-song"; body="Francis I. Andersen, Habakkuk: A New Translation with Introduction and Commentary, Anchor Bible vol. 25 (New York: Doubleday, 2001), commentary ad loc. Habakkuk 2:6-19. Andersen reads Habakkuk's five stacked woe oracles against a rapacious oppressor as a single, tightly unified compositional unit — a taunt-song built from the same recognized woe form Isaiah uses — rather than a loose gathering of independent sayings." }
'davies-allison-woe-genre' = @{ title="Matthew's sevenfold woe inside Israel's own prophetic tradition"; body="W. D. Davies and Dale C. Allison Jr., Matthew: A Commentary, vol. 3: Matthew XIX-XXVIII, International Critical Commentary (Edinburgh: T&T Clark, 1997), commentary ad loc. Matthew 23:13-36. Davies and Allison situate the chapter's sevenfold 'woe to you, scribes and Pharisees, hypocrites' structure explicitly within the inherited Hebrew prophetic woe-oracle tradition exemplified by Isaiah and Habakkuk, reading Matthew's Jesus as deliberately adopting Israel's own prophetic voice against contemporary religious leadership rather than inventing a new rhetorical form." }
'danby-mishnah-shekalim' = @{ title="Whitewashing tombs, one month before Passover"; body="Herbert Danby, trans., The Mishnah (Oxford: Oxford University Press, 1933), tractate Shekalim 1:1. Danby's standard English translation renders the Mishnah's instruction that from the first of Adar — roughly a month before Passover — public announcements went out and roads and tombs around Jerusalem were repaired and marked, specifically so pilgrims arriving for the festival would not unknowingly walk over an unmarked grave." }
'klawans-corpse-impurity' = @{ title="Why an unmarked grave mattered so much"; body="Jonathan Klawans, Impurity and Sin in Ancient Judaism (Oxford: Oxford University Press, 2000), chapters on ritual impurity in biblical and Second Temple law. Klawans traces how corpse impurity (tumat met), requiring purification with the ashes of a red heifer (Numbers 19) over a week-long process, functioned as the most severe category of ritual impurity in the biblical system, applied mechanically regardless of intent — the underlying reason an unmarked grave along a pilgrimage road warranted an annual, citywide maintenance effort rather than occasional courtesy." }
'luz-wirkungsgeschichte-matthew-23' = @{ title="Tracking a chapter's later, weaponized reading"; body="Ulrich Luz, Matthew 21-28: A Commentary, Hermeneia series (Minneapolis: Fortress Press, 2005), Wirkungsgeschichte (history-of-influence) discussion accompanying the commentary on Matthew 23. Luz's commentary is unusual among critical commentaries for tracking a passage's later reception alongside its original historical meaning, and he traces in detail how this chapter's polemic was lifted from its first-century argument and weaponized by later Christian preachers and polemicists into a blanket verdict on Jewish people and Judaism, a use its first-century author neither intended nor lived to see." }
'saldarini-matthew-intra-jewish' = @{ title="A Gospel written from inside a Jewish argument"; body="Anthony J. Saldarini, Matthew's Christian-Jewish Community, Chicago Studies in the History of Judaism (Chicago: University of Chicago Press, 1994). Saldarini argues at length that Matthew's Gospel was composed by Jewish followers of Jesus, for a community that still understood itself as Jewish, competing with a rival, Pharisaic-led form of Judaism for authority over what faithful Israel looked like in the decades following the Temple's destruction in 70 CE — reframing this chapter's polemic as an intra-Jewish sectarian argument rather than a gentile church's condemnation of an outside religion." }
'overman-formative-judaism' = @{ title="Formative Judaism at Yavneh, and Matthew's rival claim"; body="J. Andrew Overman, Matthew's Gospel and Formative Judaism: The Social World of the Matthean Community (Minneapolis: Fortress Press, 1990). Overman situates Matthew's conflict with 'the scribes and Pharisees' against the specific, contemporaneous rise of a formative rabbinic Judaism reorganizing itself around the surviving Pharisaic movement associated with Yavneh after 70 CE — the same institutional reorganization and synagogue-boundary process already discussed in this book's treatment of the Birkat ha-Minim material in John's Gospel." }
'gale-jant-matthew-23' = @{ title="A reference work states the caution directly"; body="Aaron M. Gale, 'The Gospel According to Matthew,' introduction and annotations, in The Jewish Annotated New Testament, 2nd ed., ed. Amy-Jill Levine and Marc Zvi Brettler (Oxford: Oxford University Press, 2017). Gale's annotation on Matthew 23 explicitly cautions readers against extending Matthew's specific, period-bound polemic against specific first-century religious rivals into a claim about Jewish people or Judaism as such, a use the chapter's own historical circumstances do not support." }
'levine-misunderstood-jew' = @{ title="A chapter's argument, and what later readers did with it"; body="Amy-Jill Levine, The Misunderstood Jew: The Church and the Scandal of the Jewish Jesus (San Francisco: HarperOne, 2006), chapters on Jesus and the Pharisees. Levine traces how centuries of Christian preaching lifted this chapter's specific, period-bound invective against particular first-century religious rivals out of its original argument and re-purposed it as a timeless verdict on Jewish people and Judaism as a religion, a use the chapter's own historical circumstances do not support." }
'davies-allison-lament-bridge' = @{ title="One literary unit, not two adjacent scenes"; body="W. D. Davies and Dale C. Allison Jr., Matthew: A Commentary, vol. 3: Matthew XIX-XXVIII, International Critical Commentary (Edinburgh: T&T Clark, 1997), commentary ad loc. Matthew 23:37-24:2. Davies and Allison read the lament's 'desolate house' (23:38) and the immediately following prediction that the Temple's stones would be thrown down (24:1-2) as a single deliberate literary unit, most plausibly composed with the Temple's actual destruction in 70 CE already in view, rather than two loosely adjacent scenes." }
}

# ---- Chapter beats (with [[NOTE:slug]] placeholders) ----
$beat1 = @'
The two ritual objects Jesus singles out for being made ostentatiously oversized — "they make their phylacteries broad and their fringes long" (23:5) — are not caricatures invented for this scene. Both are real, textually commanded, and physically attested from Jesus's own century.

"Phylacteries" translates tefillin: small leather boxes worn bound to the forehead and the arm during prayer, each containing four handwritten scriptural passages (Exodus 13:1-10, 13:11-16, Deuteronomy 6:4-9, and 11:13-21) on tiny parchment scrolls. The practice reads Deuteronomy 6:8 and 11:18 — "bind them as a sign on your hand, and let them be as frontlets between your eyes" — literally rather than metaphorically, and Jeffrey Tigay's commentary on Deuteronomy traces how that literal reading, producing the physical object, was already established well before the Mishnah's later codification of its details [[NOTE:deuteronomy-tefillin-literal-reading]]. The archaeological confirmation is direct: Yigael Yadin's excavation and publication of tefillin recovered from the Qumran caves, matching the same four-passage contents Jewish practice still uses today, places the custom squarely in the first century BCE/CE, physically close in both form and date to the Pharisees Matthew's Jesus is addressing [[NOTE:phylacteries-tefillin-qumran-yadin]].

"Fringes" (tzitzit) rest on an older, separate command: Numbers 15:38-39 instructs Israelites to attach fringes with a blue cord to the corners of their garments as a visual reminder to keep the commandments. Jacob Milgrom's standard commentary on Numbers works through the fringe's construction and situates it within a wider ancient Near Eastern convention in which an elaborately fringed hem signaled an important wearer — precisely the vanity Matthew's Jesus is naming [[NOTE:tzitzit-numbers-command-milgrom]]. Fringed garments from Jesus's own decade have not surfaced, but the broader Judean Desert textile record extends the picture forward: Yigael Yadin's excavation of the Bar Kokhba-period Cave of Letters recovered first- and second-century CE tunics with decorative corner treatment consistent with tzitzit-style fringing, showing the same wardrobe convention persisting across the region for generations after Jesus [[NOTE:bar-kokhba-cave-letters-textiles-yadin]]. None of this proves any specific first-century Pharisee wore either item oversized out of vanity — that charge is Matthew's polemical framing, not an archaeological finding — but the underlying objects themselves are real, dateable, and precisely the ones this verse names.
'@

$beat2 = @'
The sevenfold structure of this chapter — each accusation launched with "woe to you, scribes and Pharisees, hypocrites" (23:13, 15, 16, 23, 25, 27, 29) — is itself a recognized prophetic form, not a rhetorical device invented for this scene. The Hebrew woe oracle (hoy), built from a formulaic opening exclamation followed by an indictment and often a threatened consequence, is a well-documented unit of prophetic speech. Claus Westermann's form-critical study of prophetic speech treats the woe oracle as a distinct, recurring genre with roots in older funerary lament and covenant-curse language, not a device invented for any single passage [[NOTE:isaiah-five-woes-westermann]]. Isaiah stacks the form into an extended run of six back-to-back woes against specific social sins — greed, drunkenness, moral inversion, self-conceit, corrupt judgment (Isaiah 5:8-23) — and Habakkuk gathers five more woes into a single taunt-song against a rapacious oppressor, each introduced the same way (Habakkuk 2:6-19); Francis Andersen's commentary on Habakkuk reads that five-woe sequence as a tightly unified compositional unit rather than loosely gathered sayings [[NOTE:habakkuk-five-woes-andersen]].

Matthew's sevenfold woe against the scribes and Pharisees sits recognizably inside that same prophetic tradition. The standard critical commentary on Matthew makes the point explicitly when working through this chapter's structure: Jesus is deliberately adopting Israel's own prophetic voice — the same form Isaiah and Habakkuk use — and turning it against contemporary religious leadership, rather than improvising a new mode of attack [[NOTE:davies-allison-woe-genre]]. Whether the historical Jesus delivered seven woes in one sitting or Matthew has gathered scattered sayings into this shape for rhetorical effect, the form itself is a genuine, checkable piece of Israelite prophetic convention, not an invention of the Gospel.
'@

$beat3 = @'
The whitewashing custom behind "whitewashed tombs, which on the outside look beautiful, but inside are full of the bones of the dead" (23:27) is precisely dateable and textually documented, not a folk custom reconstructed by inference. Herbert Danby's standard English translation of the Mishnah renders tractate Shekalim 1:1 as instructing that from the first of Adar — roughly a month before Passover — public announcements went out and roads and tombs around Jerusalem were repaired and marked, specifically so pilgrims arriving for the festival would not unknowingly walk over an unmarked grave [[NOTE:danby-mishnah-shekalim]]. Danby's translation of the same tractate makes clear this was pilgrimage-season civic infrastructure applied specifically to the roads travelers used entering the city, not routine cemetery upkeep undertaken on some other schedule [[NOTE:danby-mishnah-shekalim]].

The stakes of an accidental stumble were not trivial. Corpse impurity (tumat met) was the most severe category of ritual impurity in the biblical purity system, requiring purification with the ashes of a red heifer (Numbers 19) over a full week-long process before a person could re-enter the Temple precincts. Jonathan Klawans's study of the ancient Jewish purity system traces how seriously — and how mechanically, independent of any moral fault on the impure person's own part — this particular impurity was treated, which is exactly why an unmarked grave along a pilgrimage road warranted an annual, citywide maintenance effort rather than occasional courtesy [[NOTE:klawans-corpse-impurity]]. Matthew's image trades on that precise seasonal detail: a beautifully whitewashed exterior that exists for the specific purpose of warning strangers away from what is unclean inside, aimed at scribes and Pharisees whose external religious presentation, Jesus charges, performs exactly the opposite function of what the whitewash itself is for.
'@

$beat4 = @'
This is the point in the Gospel where how a reader reads matters as much as what the text says, and mainstream biblical scholarship is unusually explicit about it. The sustained, personal ferocity of this chapter's language — "hypocrites," "blind guides," "serpents, brood of vipers" (23:33) — has a long, documented history of being read by later Christian interpreters as a timeless verdict on Judaism as a religion, and that later reading did real historical damage, feeding centuries of Christian antisemitism. Ulrich Luz's commentary on Matthew is unusual among critical commentaries for tracking that reception history (Wirkungsgeschichte) alongside the text itself, and he traces in detail how this chapter's rhetoric was lifted from its first-century argument and weaponized by later Christian preachers and polemicists in ways its own author could not have intended and did not live to see [[NOTE:luz-wirkungsgeschichte-matthew-23]].

What mainstream critical scholarship reconstructs instead is a narrower, more specific, and more historically grounded picture: this chapter's harshness reflects an intra-Jewish sectarian argument, not a Christian-versus-Jewish one. Anthony Saldarini's study of Matthew's community argues at length that the Gospel was written by Jewish followers of Jesus, for a community that still understood itself as Jewish, competing with a rival, Pharisaic-led form of Judaism for authority over what faithful Israel looked like after the Temple's destruction in 70 CE [[NOTE:saldarini-matthew-intra-jewish]]. J. Andrew Overman's parallel study of the social world behind Matthew reaches a similar conclusion from a different angle, situating the conflict against the specific, contemporaneous rise of a formative rabbinic Judaism reorganizing itself around the surviving Pharisaic movement at Yavneh in the decades after 70 CE — the same institutional reorganization and synagogue-boundary process already discussed in this book's treatment of the Birkat ha-Minim material in John's Gospel (chapter 9) [[NOTE:overman-formative-judaism]]. Read that way, Matthew 23 is a family argument at its most bitter — two groups of Jews, one aligned with Jesus and one with the Pharisaic movement that would become rabbinic Judaism, each claiming to be the authentic voice of Israel in the vacuum the Temple's fall had left — rather than a gentile church's blanket condemnation of a religion outside itself.

Amy-Jill Levine's widely read study of Matthew's Jewish context states the same caution for a general audience: she traces how centuries of Christian preaching lifted this chapter's specific, period-bound invective out of its first-century argument and re-purposed it as a timeless verdict on Jewish people and Judaism as a religion, a use the chapter's own historical circumstances do not support [[NOTE:levine-misunderstood-jew]]. The Jewish Annotated New Testament states the same caution in reference-work form: its annotation on this chapter, written by Aaron Gale, warns explicitly against extending Matthew's specific, period-bound polemic against specific first-century religious rivals into a claim about Jewish people or Judaism as such [[NOTE:gale-jant-matthew-23]]. That distinction — intra-Jewish sectarian polemic, sharpened by a specific late-first-century institutional conflict, against a timeless theological verdict on an entire religion — is not a modern political gloss laid over the text. It is the same authorship-and-audience question this book has asked of every chapter, applied honestly to the one chapter where getting the answer right matters most.
'@

$beat5 = @'
The chapter's closing lament over Jerusalem does more than end chapter 23; it hands directly into chapter 24. "See, your house is left to you desolate" (23:38) is immediately followed by Jesus leaving the Temple, his disciples pointing out its buildings, and Jesus predicting that "there will not be left here one stone upon another" (24:1-2) — the opening move of the Olivet Discourse. The standard critical commentary on this section of Matthew treats 23:37-24:2 as a single deliberate literary unit rather than two loosely adjacent scenes: the lament's desolate house and the following chapter's prediction of the Temple's stone-by-stone destruction are read together as one sustained meditation on Jerusalem's and the Temple's fate, most plausibly composed with the Temple's actual destruction in 70 CE already in view [[NOTE:davies-allison-lament-bridge]]. That placement is worth flagging briefly here rather than deferring it entirely: whatever the historical Jesus said about Jerusalem's future, the specific literary hinge — grief giving way immediately to a structural prediction of the building's collapse — is Matthew's own compositional choice, and it is the same authorship-and-hindsight question this book has raised about the chapter's polemic more broadly, now aimed forward at the discourse the next chapter develops at length.
'@

$beats = @($beat1, $beat2, $beat3, $beat4, $beat5)

# ---- Glossary additions (heading -> body) ----
$glossary = [ordered]@{
'PHYLACTERIES AND FRINGES (TEFILLIN AND TZITZIT)' = "Two real, textually commanded Jewish ritual items named in Jesus's charge that the scribes and Pharisees 'make their phylacteries broad and their fringes long' (23:5). Phylacteries (tefillin) are small leather boxes bound to the forehead and arm during prayer, containing four scriptural passages, per a literal reading of Deuteronomy 6:8 and 11:18 [[NOTE:deuteronomy-tefillin-literal-reading]]; actual first-century examples have been recovered from the Qumran caves [[NOTE:phylacteries-tefillin-qumran-yadin]]. Fringes (tzitzit) are corner tassels with a blue cord, commanded in Numbers 15:38-39 [[NOTE:tzitzit-numbers-command-milgrom]], with fringed garments recovered from the later Bar Kokhba-period Cave of Letters extending the material record for the practice across the region [[NOTE:bar-kokhba-cave-letters-textiles-yadin]]. See also PHARISEES AND SADDUCEES (FIRST-CENTURY JEWISH SECTS)."
'WOE ORACLE (PROPHETIC RHETORICAL FORM)' = "A recognized unit of Hebrew prophetic speech (Hebrew hoy), built from a formulaic opening exclamation followed by an indictment and often a threatened consequence [[NOTE:isaiah-five-woes-westermann]]. Isaiah stacks six such woes against specific social sins in a single sequence (Isaiah 5:8-23), and Habakkuk gathers five more into one taunt-song (Habakkuk 2:6-19) [[NOTE:habakkuk-five-woes-andersen]]. Matthew's sevenfold 'woe to you, scribes and Pharisees, hypocrites' (23:13-36) is read by mainstream critical commentary as a deliberate adoption of this same inherited prophetic form, not an invented mode of attack [[NOTE:davies-allison-woe-genre]]."
"MATTHEW'S ANTI-PHARISAIC POLEMIC (INTRA-JEWISH CONTEXT, NOT ANTI-JUDAISM)" = "The scholarly consensus reading of this chapter's harsh, sustained rhetoric against 'the scribes and Pharisees' as reflecting a specific intra-Jewish sectarian conflict between Matthew's own community and a Pharisaic-led Judaism reorganizing itself after the Temple's destruction in 70 CE [[NOTE:saldarini-matthew-intra-jewish]] [[NOTE:overman-formative-judaism]], rather than a timeless theological claim about Jewish people or Judaism as a religion — a distinction mainstream scholarship states explicitly given the chapter's long, documented history of being weaponized in later Christian antisemitism [[NOTE:luz-wirkungsgeschichte-matthew-23]] [[NOTE:levine-misunderstood-jew]] [[NOTE:gale-jant-matthew-23]]. Compare the same reorganizing-Judaism backdrop behind the Birkat ha-Minim and synagogue-expulsion material in John's Gospel (chapter 9). See also PHARISEES AND SADDUCEES (FIRST-CENTURY JEWISH SECTS) and JERUSALEM."
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
    Add-BeatNode $Ch23NodeId $id $sortKey
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
Seed-Entity "Yigael Yadin" "yigael-yadin" "character" "Israeli archaeologist and scholar; excavated and published the Qumran tefillin finds and the Bar Kokhba-period Cave of Letters textiles."
Seed-Entity "Anthony J. Saldarini" "anthony-j-saldarini" "character" "New Testament scholar; author of Matthew's Christian-Jewish Community, arguing Matthew's Gospel reflects an intra-Jewish sectarian conflict."
Seed-Entity "Aaron M. Gale" "aaron-m-gale" "character" "New Testament scholar; author of the Gospel of Matthew introduction and annotations in The Jewish Annotated New Testament."
Seed-Entity "Amy-Jill Levine" "amy-jill-levine" "character" "New Testament scholar; co-editor of The Jewish Annotated New Testament and author of The Misunderstood Jew, on Matthew's Jewish context."
Seed-Entity "Ulrich Luz" "ulrich-luz" "character" "New Testament scholar; author of the Hermeneia commentary on Matthew, noted for tracking the Wirkungsgeschichte (history of influence) of Matthew's text, including chapter 23's later antisemitic misuse."
Seed-Entity "Claus Westermann" "claus-westermann" "character" "Old Testament scholar; author of Basic Forms of Prophetic Speech, the standard form-critical treatment of the prophetic woe oracle."
Seed-Entity "Jonathan Klawans" "jonathan-klawans" "character" "Scholar of ancient Judaism; author of Impurity and Sin in Ancient Judaism, on the biblical ritual-purity system including corpse impurity."
Seed-Entity "Jeffrey H. Tigay" "jeffrey-h-tigay" "character" "Biblical scholar; author of the JPS Torah Commentary on Deuteronomy, including its excursus on tefillin and mezuzot."

$conn.Close()
Write-Host "DONE Chapter 23 depth pass."
