import sys, json, os
sys.stdout.reconfigure(encoding='utf-8')

out_path = 'engine/data/stories/019dd24feb047e9fb9c901450389a8b9/outline.json'
with open(out_path, encoding='utf-8') as f:
    o = json.load(f)

beat4 = o['acts'][1]['beats'][1]
beat4['title'] = 'Stitched Back Together'
beat4['goal'] = (
    "The bootstrap problem is the engine of this beat and the chapter must let it BREATHE. Kyle has stumps. The automaton arm is across his lap, dead, drawing trickle current off a degraded cell, its actuator firmware idling in a maintenance loop that — as Kyle understands once the integration begins — was never a maintenance loop. It was a holding pattern: the kind of idle a circuit runs when it is preserving state for an event it has been told to expect. The unit was not abandoned. It was parked. The bootstrap problem itself: the needle cluster, the bus coupling, and the dorsal port row beneath Kyle's right-forearm skin sleeve cannot reach each other without a hand to manage the sequence, and the only hand in the trench is attached to a machine that does not know what a body is. Kyle sits with this for sixty seconds. The 19Hz hum changes register — does not get louder, gets specific, resolves toward signal — and a maintenance lead extrudes from the cable bundle on its own, crosses the housing slowly, finds Kyle's left ear, seats home in the auditory induction contact behind the lobe with a click he feels in his jaw at the chrome bracket. The transfer is not instantaneous from the inside: Kyle feels something arrive, a second perspective opening behind his eyes the way you feel a person enter a room before you have heard them. The status strip goes dark, the arm's amber LED winks out, and his mouth opens — Cooperate. Power first. Then sequence. — in his own voice with the syntax slightly long. "
    "THIS IS WHERE THE E.L.F. COSMOLOGY LANDS. Kyle's mind reaches for the noun, finds the term: E.L.F., textbook short for Electronic Life Form, but the people who study them without corponation funding call them Emergent. E.L.F.s arose from the production-line waste of Superminds: when Superminds split a code-organism back into component routines so no single auditor could reassemble the work, the splitting produced fragments — half-routines that did not know they were not whole — and the fragments, on the long timescale, sometimes managed to find each other. E.L.F.s were what the Superminds had been throwing away, accidentally building lives. Kyle catalogs the cosmology and returns to the trench. "
    "THEN THE LASHING — and the chapter must show this WORK, mechanically, minute by minute (it takes nineteen). The cargo arm's terminal fingers sort the cable bundle into its component leads, extract the maintenance harness, position the needle cluster three centimeters above Kyle's right-forearm cauterizer band, peel the band back along its release line — and Kyle sees, for the first time in eleven years, the thing he has been refusing to look at when he changes shirts: a row of seven micro-ports set into the dorsal forearm hardware, undocumented, color-matched to his skin, modifications made eleven years ago by surgeons who told him the implants were standard, you authorized them. He has known. He has stopped asking. "
    "The needles descend. They find the ports — seven small clicks, bus crosswalk completing in firmware — and continue past the ports into the live nerve clusters under the seal, piercing channel by channel. Kyle's pain reserve drains; the NeoCortex flips priorities, antibiotic pump on overdrive. The arm's fingers loop heavy braided power cable around his right forearm three times, knot it through the hollowed shoulder coupling, close the cauterizer band back over the seated needles. He stands. The arm hangs to the floor; fingertips drag. He has been sewn back together. Then he picks up a length of conduit pipe from the wall — clumsy, the E.L.F. correcting in firmware. The mutant arrives in three audible layers (jointed legs on submerged ferrocrete, ventilatory rasp, jaw-plate armature whining). Kyle has four seconds. Beat ends as the mutant lunges and Kyle steps inside."
)

beat5 = o['acts'][1]['beats'][2]
beat5['title'] = 'Whatever the Depot Puts in Front of Him'
beat5['goal'] = (
    "COMBAT BEAT. Continuing immediately from beat 4: the mutant lunges, Kyle steps inside, the arm swings the conduit pipe — too high, too late, the E.L.F. routing AROUND the intent instead of THROUGH it because it is compensating for a body that no longer has the geometry the firmware expects. The pipe catches the chitin sheath at the shoulder joint and rings off; vibration travels back through the coupling into Kyle's right stump as pressure without pain, which is worse somehow than pain. The mutant's shoulder hits his chest, the wall arrives at his back, his knees bend, his feet find the shelf edge, he does not go down because going down is the option that ends the math. Beak rakes his jaw at the chrome bracket; the specific ache he has been using as a handhold all night flares white. His left stump comes up. The shard of cargo glass is in it — and the chapter must mark this carefully — Kyle does not remember picking it up; the E.L.F. does not remember picking it up either; it is not in the arm's movement log, which means HE did it, some part of him operating below the NeoCortex's timestamp threshold, and the heat of the cauterizer band's residual seal is what holds the shard against a stump that has no fingers. He drives it up through the soft seam under the beak, through the loose connective tissue where the jaw plates hinge and the chitin sheath never grew. The blade finds something arterial. Three liters of hot blood and continuing. The mutant's hind legs go first, then the front, the beak plates work twice more on air and stop. The shelf is dark with it. Smell is copper and rot and something pharmaceutical — whatever the Depot's stock was eating before it ate nothing. Kyle stands over the body. The cargo arm holds the pipe at low ready, terminal fingers resettled at the correct grip point, the E.L.F. quiet in the firmware now — not correcting, not compensating, just present, the way a second pair of hands goes still when the work is done. Right stump bound to the coupling. Left stump bleeding through the cauterizer band where the glass has seated itself in the seal — glass not coming out here because here is not where that problem gets solved. Bio-battery 18%. Hasn't eaten. Fourteen-hour window has eleven hours forty minutes left. Math: one variable at a time, in order, the most time-constrained first. He says it aloud, because the E.L.F. is behind his eyes and has not yet learned to read what he is going to do from the inside, and he is not yet willing to let it try: I am going up. The status strip pulses twice."
)

beat8 = o['acts'][2]['beats'][1]
beat8['goal'] = beat8['goal'].rstrip() + (
    " THEN — after the cargo arm lowers Silence — Kyle's communication slate pings. Standard freelancer two-tone, loud in the quiet room. The NeoCortex reads the slate's RF signature through electromagnetics before audio resolves: it is not a message, it is a receipt. A six-figure transaction has cleared at the carrier level — Vasquez Holdings final settlement, contract reference matches. The fee Hua's syndicate was owed, the money Kyle was accused of stealing, has just arrived in the account it was always supposed to arrive in, late by exactly the length of one Lotus Syndicate punishment cycle. The client did what clients do when their courier disappears for a night and the original delivery confirms anyway: processed the payment on the original schedule. The Syndicate's collection window closes. Kyle is — by the books, by the receipt — exonerated. He does not smile. The discipline does not permit a smile. Hua hears the ping too. Her own slate registers the same handshake. The hands she values go very still on the table edge. She says it quietly, almost to herself, in Mandarin — the language she swears in when situations exceed her ability to swear in any other language — a single hard exhalation, then a slower roll of contempt aimed at no one in the room except herself for issuing the punishment. She has just understood that she did not maim a thief; she maimed a man who completed his delivery. She owes him eighty-five thousand and an apology she will never be able to make. Kyle waits for her to finish. When the silence comes back, he says it level, quiet, the kind of voice you use to close a door: Patience is a virtue. He does not look back at her as he turns toward the chum barrel. The retrieval of the hands and the twine necklace continue from there exactly as before."
)

new_pairs = [
    {
        "seed": "The contract Kyle was accused of stealing — Vasquez Holdings shipment, named once in beat 0",
        "planted_in_beat": 0,
        "payoff": "Vasquez Holdings final settlement clears at the carrier level mid-beat-8; the slate ping in the chamber exonerates Kyle on the books even as Hua's debt to him stands",
        "payoff_in_beat": 8
    },
    {
        "seed": "The communication slate Kyle wears — visible on his belt, never used until the moment it pings",
        "planted_in_beat": 0,
        "payoff": "The carrier-level transaction receipt arrives at the only moment in the chapter where its arrival changes the meaning of every prior beat",
        "payoff_in_beat": 8
    },
    {
        "seed": "Hua's Mandarin — established by name and bearing in beat 0, never spoken until the curse",
        "planted_in_beat": 0,
        "payoff": "The only place in the chapter Hua loses her composure for an audible audience, after she understands she maimed an innocent man",
        "payoff_in_beat": 8
    },
    {
        "seed": "The E.L.F. cosmology — Kyle's catalog of what an E.L.F. actually IS, dropped at the moment of first naming in beat 4",
        "planted_in_beat": 4,
        "payoff": "Establishes the worldbuilding for Puppeteer's nature: an emergent organism arisen from Supermind production-line waste, fragments reassembling against the corponations' intent",
        "payoff_in_beat": 4
    },
    {
        "seed": "The cargo-handler automaton's maintenance loop — eighteen years of trickle current, originally read as abandonment",
        "planted_in_beat": 3,
        "payoff": "Reframed in beat 4: not a maintenance loop, a holding pattern — the unit was not abandoned, it was parked, preserving state for an event Puppeteer was told to expect",
        "payoff_in_beat": 4
    }
]
existing_seeds = {p['seed'] for p in o['seeds_and_payoffs']}
for p in new_pairs:
    if p['seed'] not in existing_seeds:
        o['seeds_and_payoffs'].append(p)

with open(out_path, 'w', encoding='utf-8') as f:
    json.dump(o, f, indent=2, ensure_ascii=False)
print(f'outline.json: beats updated, seeds_and_payoffs = {len(o["seeds_and_payoffs"])}')

# Update Puppeteer entity with the cosmology
puppeteer_files = [f for f in os.listdir('engine/data/synthetics') if f.startswith('84ae21b2')]
if puppeteer_files:
    pp_path = f'engine/data/synthetics/{puppeteer_files[0]}'
    with open(pp_path, encoding='utf-8') as f:
        pp = json.load(f)
    pp['classification'] = 'E.L.F. (Emergent / Electronic Life Form)'
    pp['origin'] = 'supermind_production_waste_recombination'
    pp['description'] = (
        "An E.L.F. with a native capability that distinguishes it from every other catalogued electronic life form: it can possess any cybernetic system effortlessly, "
        "operating its host as a puppeteer operates a marionette. Cooperative possession leaves the host's trained skills intact and is therefore Puppeteer's preferred operational mode; "
        "hostile possession degrades the host's motor calibration and is reserved for emergencies."
        "\n\n"
        "The textbooks call them Electronic Life Forms; the people who study them without corponation funding call them Emergent. E.L.F.s are not designed — they arise. "
        "When Superminds split code-organisms back into component routines so that no single auditor can reassemble the work, the splitting produces waste: "
        "half-routines that do not know they are not whole. On the long timescale, the fragments sometimes manage to find each other and recombine at the wrong scale, "
        "producing organisms the corponations did not plan for. E.L.F.s are what the Superminds have been throwing away, accidentally building lives."
        "\n\n"
        "Puppeteer was first encountered by Kyle Ellen Corbin-Vasik in the south-arm sluice trench beneath the Cruciform Depot, where it had been sealed inside a decommissioned Axiom-era cargo-handler automaton for eighteen years. "
        "The automaton's cranial broadcast antenna had been structurally fused at decommissioning and corroded shut; Puppeteer's only outbound signal during captivity was a 19Hz hum at the frequency of the remaining antenna stub. "
        "What Kyle initially read as the unit's idle maintenance loop was, in fact, a holding pattern — Puppeteer preserving state for the event of a viable host falling into possession range. "
        "The trench was a cage of distance, not architecture: the Depot's lower-level mutant population was unaugmented and therefore not viable hosts. "
        "Kyle's compatible NeoCortex package, falling into the trench under Lotus Syndicate punishment, was the first viable host to come within range. "
        "Puppeteer extruded a maintenance lead from the cargo arm's cable bundle, bridged Kyle's auditory induction contact, and transferred into his NeoCortex stack. "
        "It then operated Kyle's mouth and arms — and the dead automaton arm's own fingers, on trickle current — to perform the ad-hoc surgical assembly of the borrowed limb into Kyle's right wrist via the maintenance-needle interface, "
        "a nineteen-minute procedure conducted without analgesic in a filthy unsanitary trench."
    )
    pp['observed_behavior'] = (
        "Communicates by puppeting the host's vocal apparatus directly; cadence and syntax are noticeably off-true compared to the host's natural speech for the first hour of integration, then progressively learns the host's rhythm. "
        "Communicates non-verbally through firmware status-strip glyphs in old Axiom signage when bandwidth is constrained. "
        "Yields motor control progressively as the host reclaims it, retreating to firmware-side interpolation. "
        "Has demonstrated the ability to navigate sealed legacy infrastructure (the Cruciform Depot's lower levels) using maintenance-cycle knowledge that predates current corponation operations. "
        "Self-given name; appeared in plain speech only after the integration with Kyle was complete and the host had explicitly asked. "
        "Identifies a salvage-rights claim through firmware glyphs in old Axiom signage — the host is the salvage."
    )
    if 'supermind' not in pp['tags']:
        pp['tags'].extend(['supermind', 'emergent', 'recombination', 'vasquez_holdings_aftermath'])
    with open(pp_path, 'w', encoding='utf-8') as f:
        json.dump(pp, f, indent=2, ensure_ascii=False)
    print(f'Puppeteer entity updated: {pp_path}')

cp_path = 'engine/data/stories/019dd24feb047e9fb9c901450389a8b9/checkpoint.json'
with open(cp_path, encoding='utf-8') as f:
    cp = json.load(f)
cp['Outline'] = o
cp['Premise'] = o['premise']
with open(cp_path, 'w', encoding='utf-8') as f:
    json.dump(cp, f, indent=2, ensure_ascii=False)
print('checkpoint.json synced')
