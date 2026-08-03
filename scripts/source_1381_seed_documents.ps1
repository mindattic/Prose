. "$PSScriptRoot\gspl_db.ps1"
$conn = Open-SS

function New-SSEntity {
    param($Conn, [string]$EntityType, [string]$Name, [string]$Slug, [string]$Description, [string]$UniverseId)
    $now = [DateTime]::UtcNow
    $id = [Guid]::NewGuid().ToString()
    New-SSRow $Conn 'Entities' @{
        Id = $id
        EntityType = $EntityType
        Name = $Name
        Slug = $Slug
        Status = 'canon'
        Description = $Description
        CreatedAt = $now
        ModifiedAt = $now
        IsActive = 1
        UniverseId = $UniverseId
    } -Quiet
    Write-Host "  ok    $EntityType/$Slug"
}

$sourceUniverseId = Invoke-SSScalar $conn "SELECT Id FROM Universe WHERE Name='SOURCE'"

$docs = @(
    @{ Name = 'The Anonimalle Chronicle'; Slug = 'anonimalle-chronicle'; Description = "A Middle-English/Anglo-Norman chronicle, one of the two most detailed contemporary narrative sources for the 1381 revolt (the other being Henry Knighton's Chronicon). It is the source for the election of Wat Tyler at Maidstone on 7 June 1381, and for the fullest surviving account of the Smithfield meeting's dialogue between Tyler and Richard II, including Tyler's demands for a charter, the abolition of the episcopal hierarchy, redistribution of church wealth, and a single law of the land. Modern scholarship (per R.B. Dobson's sourcebook) treats it as a secondary compilation rather than an eyewitness account in the strictest sense, assembled close to the events but not a verbatim transcript." },
    @{ Name = "Thomas Walsingham's Historia Anglicana"; Slug = 'walsingham-historia-anglicana'; Description = "A Latin chronicle by the St Albans Benedictine monk Thomas Walsingham, hostile to the rebels and to John Ball in particular -- Walsingham is the chronicler chiefly responsible for linking Ball polemically to Wycliffe and the Lollards, a connection modern historians treat with skepticism given that Ball's radical preaching predates Wycliffe's own prominence by roughly a decade. Walsingham's account of the St Albans rising against his own abbey is a valuable, if partisan, local source." },
    @{ Name = "Henry Knighton's Chronicon"; Slug = 'knighton-chronicon'; Description = "A Latin chronicle by an Augustinian canon of Leicester Abbey, alongside the Anonimalle Chronicle one of the two fullest contemporary narrative sources for 1381. Knighton supplies the frequently quoted description of the Savoy Palace's looted plate -- 'such quantities of vessels and silver plate, without counting the parcel-gilt and solid gold, that five carts would hardly suffice to carry them' -- and the detail that the rebels deliberately destroyed rather than stole Gaunt's property, framing the Savoy's destruction as punishment rather than theft." },
    @{ Name = "Jean Froissart's Chroniques"; Slug = 'froissart-chroniques'; Description = "A French-language chronicle of fourteenth-century European affairs by the Hainaut chronicler Jean Froissart, written retrospectively and at one remove from the events (Froissart was not in England during the revolt). Valuable for its wider international framing of the revolt and its illuminated-manuscript illustrations of the Blackheath sermon and the Smithfield meeting, but historians treat its specific factual details with more caution than the English chronicles given its distance from the events and Froissart's own aristocratic sympathies." },
    @{ Name = 'The Statute of Labourers (1351)'; Slug = 'statute-of-labourers-1351'; Description = "An act of the English Parliament, passed in the first parliament held after the Black Death, elaborating and hardening the emergency royal Ordinance of Labourers (1349). It froze wages at pre-plague (1346) levels, compelled the able-bodied to accept work at those rates, and criminalized both employers who offered more and labourers who demanded it or left a master's service before an agreed term ended. Vigorously enforced through special justices for several years, it generated deep and lasting resentment among the wage-earning population it targeted and is named explicitly by historians as a direct grievance behind the 1381 revolt." },
    @{ Name = 'Poll tax return membranes (National Archives E 179 series)'; Slug = 'poll-tax-return-membranes-e179'; Description = "The surviving administrative returns recording assessment and collection of the 1377, 1379, and 1381 poll taxes, held today at The National Archives, Kew, under record series E 179. Comparison of the 1377 and 1381 returns for the same communities shows steep, geographically uneven undercounts in 1381 -- in some counties over half the expected taxpayers are simply missing from the rolls -- which historians read as evidence of organized evasion rather than population decline, and which explains why the Crown dispatched enforcement commissioners (the role Bampton held at Brentwood) in spring 1381." },
    @{ Name = "King's Bench indictment and gaol delivery rolls (1381-1382)"; Slug = 'kings-bench-indictment-rolls-1381'; Description = "The surviving legal record of the post-revolt trials -- indictments, gaol delivery rolls, and the special commissions' proceedings under justices including Robert Tresilian -- held today chiefly among the National Archives' King's Bench (KB 9) and Justices of the Peace records. These rolls are the source for named participants such as Johanna Ferrour who otherwise appear in no chronicle narrative, and for the documented scale of the post-revolt executions (at least 1,500 by contemporary estimate)." }
)

foreach ($d in $docs) {
    $slug = $d.Slug
    New-SSEntity $conn 'document' $d.Name $slug $d.Description $sourceUniverseId
}

$terms = @(
    @{ Name = 'Villeinage'; Slug = 'villeinage'; Description = "The unfree tenurial status binding a large share of the medieval English peasantry to a lord's manor -- villeins owed labour services (working the lord's own land a set number of days per week), could not leave the manor or marry off it without the lord's licence, and were subject to the manorial court rather than the royal common-law courts for most disputes. The 1381 rebels' central demand at Mile End was its abolition; the institution was not formally ended by the revolt and instead declined over the following century primarily for economic reasons -- labour scarcity after the Black Death let tenants negotiate better terms, and lords increasingly found cash rents and leasehold more profitable than unfree labour service." },
    @{ Name = 'The Great Rumour'; Slug = 'great-rumour'; Description = "A wave of tenant resistance across roughly forty villages in south-east and south-west England in 1377, in which villagers refused labour services and argued -- via the legal doctrine of ancient demesne -- that Domesday Book proved their manors had once been royal land whose tenants owed no such services. Petitions and lawsuits pressing the claim were unsuccessful, but the Great Rumour is treated by historians as a direct legal and organizational dress rehearsal for 1381: the same grievance (unfree labour obligation), the same instinct to seek remedy through a documentary/legal argument rather than simple refusal, four years earlier." },
    @{ Name = 'Ancient demesne'; Slug = 'ancient-demesne'; Description = "A legal status attaching to manors recorded in Domesday Book (1086) as having been held directly by the Crown at that date; tenants of such manors enjoyed certain protections against arbitrary increases in labour service and access to distinct legal remedies. The Great Rumour's petitioners argued their manors qualified for this status and its protections; the claim was rejected in the courts, but the underlying grievance carried directly into 1381." },
    @{ Name = 'Poll tax'; Slug = 'poll-tax-term'; Description = "A tax levied as a flat or banded charge per head (per 'poll') rather than on land, income, or property. England levied three in rapid succession -- 1377 (a flat groat, 4 pence, per adult, raising roughly £22,000), 1379 (a graduated scale across seven social bands, raising only about £18,600 against a hoped-for £50,000 amid widespread evasion), and 1381 (nominally a flat shilling, 12 pence, per adult, with local averaging rules meant to shift more of the burden onto the wealthy within a community, projected to raise roughly £66,666). The 1381 tax's enforcement -- commissioners sent out from March 1381 to chase communities whose returns fell short of expectation -- directly triggered the Essex and Kent risings." },
    @{ Name = 'Hedge-priest'; Slug = 'hedge-priest'; Description = "An unbeneficed, itinerant priest without a fixed parish living -- John Ball's status for most of his known preaching career. Operating outside the normal parish structure gave such priests both less institutional protection (Ball was repeatedly imprisoned and excommunicated by Archbishop Sudbury) and, in Ball's case, a wider and more socially mixed audience than a settled parish incumbent would typically reach." },
    @{ Name = '"Peasants'' Revolt" (naming dispute)'; Slug = 'peasants-revolt-naming-dispute'; Description = "The event has no fixed name in contemporary sources -- fourteenth-century chroniclers did not give it one collective title. Eighteenth- and nineteenth-century writers called it 'the Insurrection of Wat Tyler'; 'Peasants' Revolt' is a later, likely nineteenth-century coinage (the historian Paul Strohm traced an early use to J.R. Green's 1874 Short History of the English People but could not confirm an original coiner). Modern historians (Strohm, Miri Rubin, and others) have criticized the name on two grounds: many participants -- clergy, urban artisans, former parliamentary representatives, London tradesmen -- were not peasants in any technical sense, and 'revolt' overstates the coherence and duration of what several recent historians prefer to call 'the Great Revolt', 'the English Rising of 1381', or simply '1381'. This book uses 'the revolt' and '1381' interchangeably and flags the naming question directly rather than adopting one term as if it were neutral." }
)

foreach ($t in $terms) {
    New-SSEntity $conn 'vocabulary' $t.Name $t.Slug $t.Description $sourceUniverseId
}

$conn.Close()
Write-Host "done: $($docs.Count) documents, $($terms.Count) terms"
