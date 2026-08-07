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
$Ch18NodeId = [guid]"019FA06E-E541-76D7-8867-57D1241C3DDC"

$maxNoteSortKey = [double](Exec-Scalar "SELECT ISNULL(MAX(bn.SortKey),0) FROM BeatNodes bn WHERE bn.NodeId='019FA01D-FA22-76C6-976C-3EA4F4D54A14' AND bn.IsEnabled=1")
$maxGlossarySortKey = [double](Exec-Scalar "SELECT ISNULL(MAX(bn.SortKey),0) FROM BeatNodes bn WHERE bn.NodeId='019FA026-3758-7A57-AF65-EC1DB4303EF3' AND bn.IsEnabled=1")
$maxNoteNumber = [int](Exec-Scalar "SELECT ISNULL(MAX(CAST(LEFT(b.Text, CHARINDEX(' ',b.Text)-1) AS INT)),0) FROM BeatNodes bn JOIN Beats b ON bn.BeatId=b.Id WHERE bn.NodeId='019FA01D-FA22-76C6-976C-3EA4F4D54A14' AND bn.IsEnabled=1 AND CHARINDEX(' ', b.Text) > 1 AND LEFT(b.Text, CHARINDEX(' ',b.Text)-1) NOT LIKE '%[^0-9]%'")
$maxCh18SortKey = [double](Exec-Scalar "SELECT ISNULL(MAX(bn.SortKey),0) FROM BeatNodes bn WHERE bn.NodeId='019FA06E-E541-76D7-8867-57D1241C3DDC' AND bn.IsEnabled=1")

Write-Host "Starting: MaxNumber=$script:MaxNumber MaxNoteSortKey=$maxNoteSortKey MaxNoteNumber=$maxNoteNumber MaxGlossarySortKey=$maxGlossarySortKey MaxCh18SortKey=$maxCh18SortKey"

# ---- Notes (slug -> title/body) in order ----
$notes = [ordered]@{
'qumran-graduated-discipline-parallel' = @{ title='A graduated discipline procedure, attested twice'; body="Geza Vermes, trans., The Complete Dead Sea Scrolls in English, rev. ed. (London: Penguin Books, 2004), Community Rule (1QS), column 5, lines 24-6:1, and the Damascus Document (CD), column 9, lines 2-4; see also W. D. Davies and Dale C. Allison Jr., Matthew VIII-XVIII, International Critical Commentary (Edinburgh: T&T Clark, 1991), commentary ad loc. Matthew 18:15-17. The Qumran community's own Community Rule prescribes a documented, graduated sequence for handling a member's wrongdoing: private rebuke first (1QS 5:24-6:1), escalating to rebuke before witnesses if the offender does not respond, a structure the separate Damascus Document echoes in its own discipline rule (CD 9:2-4), which likewise requires a matter to be raised before witnesses before it can be brought to the full congregation. Matthew 18:15-17's own private-rebuke, then two-or-three-witnesses, then whole-church sequence is a genuinely close structural match to this already-attested Second Temple sectarian model of graduated communal discipline — a specific parallel of organizational procedure distinct from, and considerably more detailed than, this book's earlier discussion of the same Qumran document's washing-and-repentance provisions [94]." }
'minyan-ten-vs-two-or-three' = @{ title='Two or three, not ten'; body="Herbert Danby, trans., The Mishnah (Oxford: Oxford University Press, 1933), tractate Avot 3:2-3 and tractate Sanhedrin 1:6; see also Lee I. Levine, The Ancient Synagogue: The First Thousand Years, 2nd ed. (New Haven: Yale University Press, 2005), chapter on the origins and development of synagogue liturgical practice. The Mishnah preserves two separate traditions worth distinguishing carefully. Avot 3:2-3 attributes to Rabbi Hananiah ben Teradion and Rabbi Simeon ben Yohai the idea that a small gathering of two or three occupied with Torah study brings the divine presence to rest among them, a saying strikingly close in shape to Jesus's own promise in Matthew 18:20. Sanhedrin 1:6 separately specifies ten adult men (a minyan) as the quorum required for certain public liturgical acts, including the communal recitation of the Shema with its leader's repetition. Both passages are preserved in a document compiled around 200 CE, considerably later than Matthew's own composition, and Levine's history of the synagogue documents that a fixed, numerically specific minyan requirement of ten is attested with confidence only in this later rabbinic material, not demonstrably already fixed at ten in Jesus's own lifetime. The genuine, checkable point of comparison is that Matthew's saying specifies a markedly smaller quorum, two or three, than the number later codified for the minyan — not that Jesus is here quoting or contradicting an already-fixed rabbinic institution." }
'talent-denarii-exchange-rate' = @{ title='Working out the exchange rate'; body="Walter Bauer, Frederick W. Danker, William F. Arndt, and F. Wilbur Gingrich, A Greek-English Lexicon of the New Testament and Other Early Christian Literature (BDAG), 3rd ed. (Chicago: University of Chicago Press, 2000), entries on talanton and denarion; Richard Duncan-Jones, The Economy of the Roman Empire: Quantitative Studies, 2nd ed. (Cambridge: Cambridge University Press, 1982), chapter on wages and prices. The standard ancient monetary equivalency behind the parable's numbers — a talent reckoned at roughly six thousand denarii, and a denarius as the widely attested baseline daily wage for an ordinary hired laborer in the early Roman Empire — is not the Gospel writer's own invention; it reflects coinage and pay conventions independently documented across the wider Greco-Roman economic record that Duncan-Jones's study surveys in detail. Doing the arithmetic on those two independently attested figures is what turns ``ten thousand talents'' from an abstractly large number into a concrete, checkable absurdity: tens of thousands of years of an ordinary laborer's wages, an amount no household servant of the period could plausibly be entrusted with, let alone owe outright." }
'archelaus-tribute-benchmark' = @{ title="A real-world benchmark: one ethnarch's entire tribute bill"; body="Flavius Josephus, Jewish Antiquities, Book 17, sections 318-320 (Loeb Classical Library, trans. Ralph Marcus and Allen Wikgren, Cambridge, MA: Harvard University Press). After Herod the Great's death, Josephus records the annual tribute income assigned to each of his surviving sons' territories: Archelaus's ethnarchy, comprising Judea, Samaria, and Idumea together, was assessed at six hundred talents a year — a real, independently documented figure for an entire regional government's yearly revenue, not a round number invented for comparison. Set beside that figure, the unforgiving servant's ten-thousand-talent debt is not simply large; it comes to more than sixteen times the annual tribute of the very territories the parable's own first hearers lived under, a scale calibrated to register as flatly impossible rather than merely enormous." }
'millstone-donkey-vs-handmill' = @{ title='The donkey millstone, not the hand mill'; body="Walter Bauer, Frederick W. Danker, William F. Arndt, and F. Wilbur Gingrich, A Greek-English Lexicon of the New Testament and Other Early Christian Literature (BDAG), 3rd ed. (Chicago: University of Chicago Press, 2000), entry on onikos; K. C. Hanson and Douglas E. Oakman, Palestine in the Time of Jesus: Social Structures and Social Conflicts, 2nd ed. (Minneapolis: Fortress Press, 2008), discussion of ancient grinding technology and household versus animal-powered milling. The Greek text behind Matthew 18:6 specifies not just any millstone but a mylos onikos, literally a ``donkey millstone'' — the large, heavy upper stone of a rotary mill too big to be turned by hand, requiring an animal yoked to it, distinguished in the same period's household economy from the small hand-mill or quern a woman could operate alone each morning to grind a family's daily grain (the smaller hand-mill Matthew's own Gospel references elsewhere, at 24:41, ``two women grinding at the mill''). Reaching specifically for the animal-powered class of millstone, rather than the ordinary domestic one already familiar to the same audience from daily life, sharpens the saying's image: the largest, heaviest millstone available, not a merely inconvenient household object." }
'little-ones-children-or-community-debate' = @{ title='Children, or the whole vulnerable community?'; body="W. D. Davies and Dale C. Allison Jr., Matthew VIII-XVIII, International Critical Commentary (Edinburgh: T&T Clark, 1991), commentary ad loc. Matthew 18:6, 18:10, 18:14; Ulrich Luz, Matthew 8-20: A Commentary, Hermeneia series, trans. James E. Crouch (Minneapolis: Fortress Press, 2001), commentary on the same verses. Matthew's Greek shifts between two related but not identical terms across this discourse: paidion, ``child,'' used of the literal child Jesus places among the disciples at 18:2-5, and hoi mikroi, ``the little ones,'' used of the potential victims of causing-to-sin at 18:6, the object of the angels' care at 18:10, and the subject of the Father's will at 18:14. Whether ``the little ones'' in these three later verses still means literal children, carrying the image of 18:2 forward, or has broadened to mean vulnerable or lowly members of the Christian community generally — a reading consistent with the chapter's overall community-discipline focus, and the direction Davies and Allison lean by reading ``the least'' inclusively — is a genuinely open question on which the mainstream commentary tradition remains divided; the text's own wording does not settle it either way." }
}

# ---- Chapter beats (with [[NOTE:slug]] placeholders) ----
$beat1 = @'
Jesus lays out a specific, step-by-step procedure for a "brother" who sins against another: go to him privately first; if he won't listen, bring one or two others so that "every word may be established by the mouth of two or three witnesses"; if he still won't listen, tell it to the whole church; and if he won't listen even to the church, treat him "as a Gentile and a tax collector" — outside the community's normal fellowship (18:15-17). Jesus then repeats, to the disciples collectively this time, the binding-and-loosing formula he had earlier given to Peter alone: "whatever you bind on earth shall be bound in heaven, and whatever you loose on earth shall be loosed in heaven" (18:18), the same specific pair of Second Temple legal verbs already discussed in this book's account of Peter's confession [61].

The escalating structure of 18:15-17 is not a procedure Matthew's community invented from nothing. The Qumran sectarian community, already introduced in this book's account of John the Baptist, wrote its own graduated discipline sequence into its foundational rulebook: private rebuke first, then rebuke before witnesses if the private approach fails, before a matter could be raised with the wider group [[NOTE:qumran-graduated-discipline-parallel]]. The real, checkable point here is narrow but genuine: two independent Second Temple Jewish communities, writing within a few generations of each other and both organizing themselves as bounded fellowships governed by internal discipline rather than by the Temple courts or civil law, arrived at structurally similar graduated procedures — private confrontation before public exposure, and public exposure before the harshest sanction. That is a real point of comparative religious-community organization, not proof that either group copied the other; nothing in Matthew's text or in the Qumran material claims direct dependence, and the two communities' theological reasons for the procedure differ considerably even where the shape overlaps [[NOTE:qumran-graduated-discipline-parallel]].

What the sanction itself amounts to is also worth noting precisely: being treated "as a Gentile and a tax collector" does not mean formal excommunication in any developed later-church sense so much as social and religious distancing — treatment as an outsider to the community's own internal fellowship, a real and serious social consequence in a small, tightly bound first-century group, but a narrower and more informal sanction than later ecclesiastical procedures that would eventually draw on this passage as their proof text. The graduated procedure's whole point, worth stressing given how the chapter ends, is that it is built to stop short of that outcome if at all possible — three separate off-ramps (private, small-witness, whole-church) before the harshest sanction, the same structural patience toward an erring member that the Qumran community's own rule likewise built in before its own harshest sanctions were applied [[NOTE:qumran-graduated-discipline-parallel]].
'@

$beat2 = @'
Jesus closes the discipline instructions with a promise: "again I say to you, that if two of you shall agree on earth as touching any thing that they shall ask, it shall be done for them... For where two or three are gathered together in my name, there am I in the midst of them" (18:19-20).

The number is worth pausing on, because Jewish tradition attaches real significance to specific gathering-sizes for exactly this kind of claim about the divine presence resting on a group. The Mishnah preserves a saying, attributed to early rabbinic teachers, that when two people sit together occupied with words of Torah, the divine presence rests between them — a saying remarkably close in shape to Jesus's own promise here [[NOTE:minyan-ten-vs-two-or-three]]. But the same rabbinic tradition, in a separate passage, requires a much larger quorum — ten adult men, a minyan — for certain other communal acts of worship, including the leader's public repetition of the Shema [[NOTE:minyan-ten-vs-two-or-three]]. Matthew's Jesus specifies two or three: a real, worth-noting contrast with the substantially larger number later codified for the minyan, though not, on the evidence available, a direct rebuttal of an institution not yet demonstrably fixed at that number in his own lifetime. What can be said with more confidence is that first-century Judaism already had a live, native conceptual category — small gatherings devoted to sacred purposes drawing the divine presence to them — that Jesus's promise here draws on and specifies with its own particular number.
'@

$beat3 = @'
Peter asks how many times he must forgive a brother who sins against him — as many as seven times? — and Jesus answers "seventy times seven" (or, in some translations and manuscripts, "seventy-seven"), then illustrates the point with a parable: a king settles accounts with his servants, and one is found to owe ten thousand talents, a debt he cannot possibly repay. The king orders him, his wife, his children, and all he has to be sold, but relents and forgives the entire debt when the servant begs for patience. That same servant then finds a fellow servant who owes him a hundred denarii, refuses the identical plea for patience, and has the man thrown into debtors' prison; when the king hears of it, he hands the unforgiving servant over to the torturers until he should pay all that was owed (18:21-35).

The economics of that first debt are worth working out precisely rather than skimming past as generic hyperbole. A talent was reckoned at roughly six thousand denarii, and a denarius was the standard attested daily wage for an ordinary hired laborer in the early Roman world [[NOTE:talent-denarii-exchange-rate]]; ten thousand talents therefore comes to something in the neighborhood of tens of thousands of years of an average worker's wages — a sum no household servant, however senior, could ever have plausibly accumulated as a personal debt [[NOTE:talent-denarii-exchange-rate]]. A real-world benchmark sharpens the point further: Josephus records that the entire annual tribute owed to Rome by Judea, Samaria, and Idumea combined, under Herod's son Archelaus, came to six hundred talents a year [[NOTE:archelaus-tribute-benchmark]] — meaning the parable hangs a debt worth more than sixteen years of an entire regional government's total tax revenue on a single household servant [[NOTE:archelaus-tribute-benchmark]]. Ancient audiences, who had no need to do this arithmetic themselves to feel the absurdity, would have heard "ten thousand talents" as a number specifically chosen to be impossible.

Against that stands the fellow servant's debt of a hundred denarii — a real, entirely ordinary sum, roughly three months' wages for a laborer, the kind of debt that plausibly did pass between working people in this period. The parable's entire argument rests on that deliberate mismatch: a forgiven debt of impossible, hyperbolic scale set directly beside a refused debt of modest, realistic scale, so that the unforgiving servant's cruelty reads not as a subtle moral failing but as an arithmetic outrage the first hearers could do in their heads.
'@

$beat4 = @'
Before any of that, Jesus has already set the discourse's terms with two hard warnings. Whoever causes one of "these little ones which believe in me" to sin, he says, would be better off with "a millstone hanged about his neck" and drowned "in the depth of the sea" (18:6); whoever causes his own hand, foot, or eye to sin should cut it off or pluck it out rather than be cast, whole, into everlasting fire (18:7-9).

The millstone image is worth being precise about, because the Greek text specifies a particular class of object, not a generic heavy stone. The word behind "millstone" here is a mylos onikos — literally a "donkey millstone," the large upper stone of a rotary mill too big and heavy to be turned by hand, requiring an animal yoked to it to grind grain at any real scale [[NOTE:millstone-donkey-vs-handmill]]. That is a specifically different, and specifically larger, object than the small hand-mill or quern a household could operate without an animal at all — the kind of hand-mill Matthew's own Gospel elsewhere pictures two women turning together to grind a family's daily flour [[NOTE:millstone-donkey-vs-handmill]]. Reaching for the animal-scale millstone rather than the household one is a deliberate escalation of the image: not an inconvenient object round someone's neck, but the largest, heaviest class of millstone available in a first-century agricultural village, chosen precisely because drowning with it attached would be immediate and irreversible.
'@

$beat5 = @'
The phrase "these little ones" recurs three times across this discourse — as the potential victims of causing-to-sin (18:6), as the ones whose angels "always behold the face of my Father" (18:10), and as the ones the Father does not wish to see perish (18:14) — immediately after Jesus has set an actual child among the disciples as the model for greatness in the kingdom (18:1-5).

Whether "the little ones" in these three later verses is still meant literally — carrying forward the specific image of the child physically standing among the Twelve — or has broadened into a figurative label for vulnerable, lowly, or easily-overlooked members of the Christian community generally is a genuinely open question in the mainstream commentary tradition, not one this book's method can settle by appeal to any external check [[NOTE:little-ones-children-or-community-debate]]. Matthew's own Greek shifts vocabulary as the discourse moves: the literal "child" (paidion) of 18:2-5 gives way to "the little ones" (hoi mikroi) of 18:6 onward, and commentators read that shift both ways — some treating it as a widening of scope to match the chapter's overall turn toward community discipline and care for straying members, others treating the child of 18:2-5 as still firmly in view throughout [[NOTE:little-ones-children-or-community-debate]]. Given how directly the entire discourse's opening image is a literal child, and how directly its closing sections concern grown "brothers" who sin, dispute, and need forgiving, both readings have real textual footing, and the honest position is that Matthew's own wording does not force a choice between them [[NOTE:little-ones-children-or-community-debate]].
'@

$beats = @($beat1, $beat2, $beat3, $beat4, $beat5)

# ---- Glossary additions (heading -> body) ----
$glossary = [ordered]@{
'MINYAN (PRAYER QUORUM)' = "The traditional quorum of ten adult Jewish men required, per later rabbinic law, for certain communal acts of worship, including the leader's public repetition of the Shema. The specific number ten is documented with confidence in the Mishnah (compiled around 200 CE, tractate Sanhedrin 1:6) rather than demonstrably fixed already in Jesus's own lifetime. Matthew 18:20's promise that Jesus is present ``where two or three are gathered'' in his name specifies a markedly smaller number than the minyan's ten, a real and worth-noting contrast rather than a direct citation or rebuttal of an institution not yet clearly codified at that number in the first century [[NOTE:minyan-ten-vs-two-or-three]]. A separate, earlier Mishnah tradition (Avot 3:2-3) already associates a much smaller gathering of two or three people studying Torah with the resting of the divine presence, a saying structurally close to Jesus's own [[NOTE:minyan-ten-vs-two-or-three]]."
'TALENT (ANCIENT CURRENCY UNIT)' = "The largest unit of ancient Mediterranean currency reckoning, equivalent to roughly six thousand denarii — and a denarius was the standard attested daily wage for an ordinary hired laborer in the early Roman world [[NOTE:talent-denarii-exchange-rate]]. The parable of the unforgiving servant (18:23-35) hinges its entire argument on this real exchange rate: a debt of ten thousand talents comes to tens of thousands of years of ordinary wages, more than sixteen times the six-hundred-talent annual tribute Judea, Samaria, and Idumea together owed Rome under Herod's son Archelaus [[NOTE:archelaus-tribute-benchmark]] — a deliberately impossible sum set beside the fellow servant's realistic, modest debt of a hundred denarii, about three months' wages."
'MILLSTONE (FIRST-CENTURY AGRICULTURAL TECHNOLOGY)' = "The grinding implement invoked in Jesus's warning that it would be better for an offender of ``these little ones'' to be drowned with ``a millstone... hanged about his neck'' (18:6). The underlying Greek specifies a mylos onikos, a ``donkey millstone'' — the large upper stone of a rotary mill too heavy to turn by hand, requiring an animal yoked to it — a specifically larger and heavier class of object than the small hand-mill or quern a household could operate alone, the kind pictured elsewhere in Matthew's Gospel as two women grinding together (24:41) [[NOTE:millstone-donkey-vs-handmill]]. Reaching for the largest available class of millstone sharpens the image of a punishment meant to be immediate and irreversible."
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
$sortKey = $maxCh18SortKey + 900.0
foreach ($beatText in $beats) {
    $resolved = $beatText
    foreach ($slug in $slugToNumber.Keys) {
        $resolved = $resolved.Replace("[[NOTE:$slug]]", "[$($slugToNumber[$slug])]")
    }
    $id = New-BeatRow $resolved
    Add-BeatNode $Ch18NodeId $id $sortKey
    $sortKey += 100
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
# No new entities required: the Qumran Community (Dead Sea Scrolls) entity referenced above
# already exists in the catalog (slug: qumran-community-dead-sea-scrolls). Minyan, Talent, and
# Millstone are vocabulary/glossary concepts, not named characters or places, so no Seed-Entity
# calls are made in this script.

$conn.Close()
Write-Host "DONE Chapter 18."
