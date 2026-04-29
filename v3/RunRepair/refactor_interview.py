"""Refactor 'West Town Cross-Street, 02:14' into 'The Interview':
- Lotus Syndicate ambush is a hiring audition, not a hit
- Remove debugging/stat-block language; let Silence be piezoelectric and graceful
- End with the freelance-roster deal that unfolds into Cold Chain → The Borrowed Hand
- Archive current prose; build new outline; reset checkpoint for regeneration"""
import sys, json, os, shutil, datetime
sys.stdout.reconfigure(encoding='utf-8')

CHAPTER_ID = '019dad5fdb77766b9d548fb43a11be18'
BUSHIDO_BOOK_ID = 'eb91080d9c9c4f2b9b405fa5996bdea1'
src = f'engine/data/stories/{CHAPTER_ID}'

# 1. Archive current prose
ts = datetime.datetime.now().strftime('%Y%m%dT%H%M%S')
arch_dir = f'engine/data/archives/{CHAPTER_ID}-{ts}-westtown-v1'
os.makedirs(arch_dir, exist_ok=True)
for f in os.listdir(src):
    shutil.copy2(os.path.join(src, f), os.path.join(arch_dir, f))
print(f'Archived current chapter to: {arch_dir}')

# 2. Build the new chapter outline
outline = {
    "title": "The Interview",
    "logline": (
        "Walking home from a clean job at 02:14 on a wet West Town cross-street, Kyle is ambushed by a six-piece Lotus Syndicate crew. "
        "Three minutes into the engagement Kyle reads what the audience does not yet: this is calibrated. The shooters are restraining. The dog drone's stun emitter is throttled below disable. Mira's calls are not panic — they are coordination cues to a spotter Kyle cannot see. "
        "He fights at full capability anyway, because the gracefully correct response to an interview is to do the work in front of you the way you would do it in private. "
        "When he is the only person standing, Mira tells him what the engagement was: an audition. Lotus Syndicate wants him on payroll. He says no. She offers a freelance roster instead — contracted routes through Lotus territory, carrier rates, no further crews on him — and Kyle, against his preference but in keeping with his arithmetic, accepts the verbal arrangement. "
        "He walks home the length of West Erie. The chapter ends with Kyle climbing the Pivot stairs and not sleeping, the Lotus freelance roster now the structural premise of every contract Sable will route to him next."
    ),
    "theme": (
        "Capability is appraised in private the way a horse is appraised at auction. The Lotus Syndicate has decided in advance that Kyle is for sale; the ambush is the negotiation. "
        "Kyle understands the shape of the offer by the third beat of the engagement and does not change his behavior — the only honest answer to *show me what you are* is *exactly what I always am*. "
        "The chapter is a study in dignified non-negotiation: Kyle does not pretend to be less than he is to escape the offer, and he does not perform more than he is to leverage the offer. He fights the way he fights. The Syndicate watches. The deal that gets struck at the end is not a victory — it is the smallest concession the encounter could end with, and Kyle accepts it because the math says accepting it costs less than refusing it. "
        "PROSE DIRECTIVE: This chapter must be GRACEFUL combat. Silence is a piezoelectric sword — the PZT shingane core harvests charge from every clean impact, every parry, every cut. Kyle does NOT drag the blade across asphalt, hoods, or surfaces; he does NOT use TENG/triboelectric film mechanics; he charges the bank by *fighting cleanly*. The corundum-strop draw at the start of engagement is canonical and stays. The hamon brightens when the work is good. The work is good when Kyle is in his discipline. The chapter must read as a samurai's interview — not as a debugging log of a video game encounter. "
        "REMOVE ENTIRELY: bracket-marked beat-state blocks ([BEAT N — TITLE], [BEAT N STATE], --- [BEAT N], statistical reports listing percentages and round counts as system output). Kyle's NeoCortex catalogs internally; the reader does not see the catalog as a stats sheet. "
        "PRESERVE THE CONTENT IN PROSE: every detail that lived in the original stat-block format must STILL appear in the chapter — the rain stopped forty minutes ago, the gang's exact roster (Mira/KT/Rook/Nines/Dex/the dog with their specific augmentation profiles and weapons), Kyle's Silence cold and Chorus loaded with four standard rounds, his bio-battery at 72% from a Mrs. Chen's broth meal three hours ago, Ballistic Precognition idling passive, the hamon's ascent through cyan-thread to bright-cyan to white-blue to sodium-white as the bank fills from clean impacts. These are not stats; these are the texture of a hardware-cataloging operator's awareness, and the prose must render them as Kyle's interior register, woven into the action paragraph by paragraph the way a hunter's scent-and-wind notes weave into a hunt scene. Bio-battery percentage, ammo counts, hamon charge, augmentation reads — all of these belong to Kyle's interiority and the reader's inference, not to a heads-up display, but the *information* must be there."
    ),
    "premise": (
        "0214 on a wet West Town cross-street near The Pivot. Six positions: Mira (chrome jaw, machine pistol) on the planter, KT (augmented arm, machete) between the panel van and a sedan, Rook (heavy, augmented legs) at the parking structure, Dex flanking from behind, Nines on a fire escape with a rifle, and a quadruped combat drone — Lotus calls it Ripper — held back near the dumpster. "
        "Kyle is walking home from a clean job; Silence's bank is empty (no engagement on the route), Chorus has four rounds, the rain stopped forty minutes ago. He reads six positions before he reaches them. He commits to ten more steps before forcing the trigger so he can see the spacing they have chosen. "
        "Mira opens the engagement on cue. Kyle draws Silence across the corundum strop on his left forearm in the same motion the saya clears, the piezoelectric core catching the impact-and-friction of three full passes, the hamon lighting from cold to a thin cyan thread. He fights. "
        "The fight is graceful by design. Silence brightens with each clean clash; KT's augmented arm goes dead at passive-disruption contact; Kyle scales the planter to the fire escape and disarms Nines through the cybernetic-wrist-brace interface; Rook's BCI crashes at the temple from a single edge-pass; the dog drops to two Chorus rounds; KT goes down clean in an arc against the planter; Dex runs. "
        "By the time Mira is the only one standing, Kyle has cataloged enough anomalies — the calibrated dog, the held-back rifle, Mira's coordination cadence — to know what he is in. He walks toward her at full hamon. She lets him. She does not flee; she has been waiting for this conversation. "
        "Mira makes the offer plainly: Lotus Syndicate wants him on payroll. He says no. She names the Bucktown engagement two months ago — fourteen-piece, eight casualties — as the first interview, and tells him he passed both. She asks once more. He says no again. "
        "She offers the freelance roster: contracted routes through Lotus territory, carrier rates, no further audition crews. Kyle thinks. The math is honest. He accepts the verbal arrangement. Mira tells him a fixer named Sable will route the contracts; Kyle does not flinch at the name. He turns south and takes the length of West Erie home. "
        "The chapter ends with Kyle climbing the Pivot stairs and not sleeping. The Lotus Syndicate freelance roster is the structural premise that the next chapter (Cold Chain) will pay off as a Vásquez Holdings cold-chain run, and that the chapter after (The Borrowed Hand) will pay off as the punishment that follows when a contracted fee fails to clear."
    ),
    "characters": [
        "Kyle Ellen Corbin-Vasik",
        "Mira (Lotus Syndicate captain — the recruiter)"
    ],
    "acts": [
        {
            "act_number": 1,
            "name": "The Cross-Street",
            "purpose": "Establish the route home, the read of six positions, and the moment Mira calls 'Now' — the engagement opens on cue.",
            "beats": [
                {
                    "beat_index": 0,
                    "title": "Six Positions Before The Block",
                    "goal": "Kyle is two blocks south of West Erie at 02:14, walking home from a clean job. The NeoCortex is at idle, on the catalog rhythm it runs when there is nothing to flag — and the catalog is full of small things the chapter must put into the prose not as a stat block but as the texture of his evening: Silence cold on his back (the hamon a cold blue thread, the bank empty because he walked the route home without engagement and no impact has fed the piezoelectric core); Chorus on his right hip (four 12-gauge rounds loaded in the cylinder, two empty chambers); the bio-battery at seventy-two percent because he ate at Mrs. Chen's three hours ago — protein-heavy broth, not a full meal; Ballistic Precognition idling passive, the way it always idles when he is moving through inhabited streets at hours people are not on them. The setting is neighborhood-wet in the specific register of West Town at this hour: narrow cross-street near The Pivot, wet asphalt holding streetlight in long ribbons, parked cars dark against the curbs, a concrete planter at the eastern corner with a dead tree he has noticed before, a dumpster at the mouth of the parking structure with a graffito he has not bothered to read in the months he has been walking past it. Rain stopped forty minutes ago — long enough that the runoff is no longer running, short enough that the air still carries the lake-water smell the wind brings off the eastern shore. He catalogs all of this without flagging any of it, because none of it is anomalous yet. Then the wrong heartbeats start. The NeoCortex flags six positions in the half-second between the first read and the sixth. One on the fire escape above the dry cleaner — elevated, patient. One behind the panel van — augmented-limb signature, mechanical-pivot register. One behind the concrete planter — human, breathing controlled, not a target Kyle has the angle on. One at the mouth of the parking structure — heavy, augmented-leg signature, body armor reading dense at the chest. One flanker behind him — unaugmented, two-weapon profile. And one *non-organic* signature near the dumpster — quadrupedal, the chassis profile reading like a combat drone but without the BCI signature drones in this district usually carry. The drone is purely mechanical: pit-bull-sized, armored chassis, jaw-mounted emitter cycling at low draw. He catalogs the read without acting on the read. He gives them ten more steps before forcing the trigger, so he can see the *spacing* they have chosen, because spacing is the difference between a hit and something else, and he has never been able to read which something else this is by the spacing alone. He gives them ten steps. They commit on the eighth.",
                    "characters_present": ["Kyle Ellen Corbin-Vasik"],
                    "location": "West Town cross-street, West Erie at Ashland",
                    "emotional_arc": "The reader catches the procedural attention of a man whose body has been catalogued ambushes for fourteen years; the noir register is restraint, not panic.",
                    "stakes": "Reading the spacing correctly. Buying himself the angle.",
                    "seeds": [
                        "Six positions named without panic — the chapter's procedural register",
                        "The wet asphalt — sets the visual",
                        "Mira's call cadence is *coming* — readers don't know yet, but Kyle's instincts will catch it",
                        "Silence's bank empty at engagement open — the hamon will brighten only with clean work"
                    ],
                    "payoffs": [],
                    "facet_hint": "discipline",
                    "tension": 5,
                    "structure_role": "inciting_incident",
                    "scene_type": "scene"
                },
                {
                    "beat_index": 1,
                    "title": "Now",
                    "goal": "Mira calls *Now.* Chrome jaw catching streetlight as she steps from behind the planter, machine pistol up — Lotus's south-arm cell captain, the BCI implant at her left temple a small visible bump under the close-cropped hair. KT comes out of the gap between the panel van and a sedan: augmented left arm fully extended elbow-to-fingertip in chrome, machete raised in her unaugmented right. Rook materializes from the mouth of the parking structure — heavy, augmented-leg signature reading hard through Kyle's array, body armor dense across the chest, a heavy pistol in a two-hand grip. Dex closes from behind, footfall light, both handguns already drawn — unaugmented, the flanker, the one whose presence the Syndicate uses precisely because his lack of signature is hard for arrays like Kyle's to read. Nines settles on the fire escape above the walk-up over the dry cleaner — rifle braced, the cybernetic wrist-brace the only augmentation his profile carries. And the dog rounds the dumpster: quadrupedal, low-slung, the chassis articulation precise enough that it is not slipping on the wet asphalt the way a less-engineered drone would. Pit-bull-sized. Armored across the back and haunch. Jaw-mounted emitter cycling, the ozone signature already in the air. *Purely mechanical.* The NeoCortex finds nothing to read in the chassis — no BCI, no cardiac regulator, no augment signatures at all. The array files it as an inhabited *thing* and moves on. Five humans plus the drone. Mira speaks first: *You're a long way from your contract territory.* Kyle does not answer. He draws Silence in the same motion that crosses his left forearm — the corundum strop mounted along the inner arm, three full passes against the mune in the four-tenths of a second the draw takes. The piezoelectric core wakes the way a capacitor wakes when its first impulse arrives, and the ferrocerium edge of the strop throws blue-white sparks across his sleeve and dies in the wet air. The hamon flickers from cold to a thin cyan thread. Not much. But a thread. He steps into the cross-street.",
                    "characters_present": ["Kyle Ellen Corbin-Vasik", "Mira (Lotus Syndicate captain — the recruiter)"],
                    "location": "West Town cross-street",
                    "emotional_arc": "The chapter pivots from observation to engagement; the corundum strop is the chapter's first piece of canon establishing Silence's piezoelectric register without needing to explain it.",
                    "stakes": "First exchange, first read.",
                    "seeds": [
                        "The corundum-strop draw — Kyle's signature opening, canonical",
                        "Hamon at cyan thread — establishes the bank reads visibly",
                        "Mira's *You're a long way from your contract territory* — heard now as taunt, will read later as recruiter's opener"
                    ],
                    "payoffs": ["The six-position read from beat 0"],
                    "facet_hint": "code",
                    "tension": 7,
                    "structure_role": "rising_action",
                    "scene_type": "combat"
                }
            ]
        },
        {
            "act_number": 2,
            "name": "The Engagement",
            "purpose": "The fight — graceful, piezoelectric, the hamon brightening with each clean clash. Kyle catalogs the anomalies of restraint without committing to the read until act 3. End the act with KT down, the dog destroyed, Nines disarmed, Rook blanked, Dex fled.",
            "beats": [
                {
                    "beat_index": 2,
                    "title": "First Clash",
                    "goal": "COMBAT BEAT. KT closes first. Her augmented arm swings horizontal, chrome catching streetlight; Kyle catches the blow on the flat of Silence — not the edge, the flat — and the impact rings through the steel into his wrists, into the shingane, and the piezoelectric core *takes* the impulse the way a capacitor takes a current. The hamon brightens. He stays in contact for one more exchange — KT brings the machete down from the right, he crosses the blade, takes the second hit clean, the hamon flickers up another increment — and then he moves the edge to her left wrist. Passive disruption. The augmented arm goes dead from the elbow down. KT makes a sound like a circuit breaker tripping. Mira fires once from the planter — a controlled burst — and Kyle reads it, steps out of the lane, hears the rounds clip the car door behind him. Mira's call to Nines is *late*. The rifle on the fire escape does not fire when the angle opens. Kyle catalogs the lateness without acting on it.",
                    "characters_present": ["Kyle Ellen Corbin-Vasik"],
                    "location": "West Town cross-street",
                    "emotional_arc": "The combat is musical, not mechanical: clash, brighten, disrupt, step. The reader feels the discipline.",
                    "stakes": "The hamon. The first read of restraint.",
                    "seeds": [
                        "Mira's call to Nines arriving late — the first concrete tell that this is calibrated, not committed",
                        "Passive-disruption to KT's arm — the technique Kyle will use again on Rook"
                    ],
                    "payoffs": ["The hamon's piezoelectric register from beat 1 — bank rises with clean work"],
                    "facet_hint": "code",
                    "tension": 8,
                    "structure_role": "rising_action",
                    "scene_type": "combat"
                },
                {
                    "beat_index": 3,
                    "title": "The Dog and the Crocodile",
                    "goal": "COMBAT BEAT. Rook closes from the structure entrance, heavy pistol in two-hand grip, looking for an inside angle where Silence cannot generate arc. Smart. Kyle gives him the approach. The hamon is still building. The dog rounds the front of the sedan low and fast, articulated legs finding grip on wet asphalt without slipping, jaw emitter active. Kyle hears the change in leg-cadence before he sees the lunge. He goes up onto the sedan's hood and the dog's stun emitter discharges into empty air under his boot — and Kyle *registers* what just happened: the emitter is calibrated to register but not disable. A real combat drone would have throttled the discharge to lock his nervous system; this one passed at threshold and continued. He files it. He does not act on it. Chorus comes off his hip in his left hand — bird's-head grip — and he puts the first round through the dog's rear actuator housing. The legs collapse. The dog drags itself forward on its front pair, jaw emitter still cycling, threat reduced. Kyle drops off the hood. Rook fires at three meters; Kyle reads the telegraph and lets the round pass between his arm and his ribs.",
                    "characters_present": ["Kyle Ellen Corbin-Vasik"],
                    "location": "West Town cross-street, sedan and parking structure entrance",
                    "emotional_arc": "The dog's calibrated emitter is the second concrete tell. Kyle has now registered enough anomalies to know what he is in. He fights the same way regardless.",
                    "stakes": "The dog. Rook. The integrity of Kyle's read.",
                    "seeds": [
                        "The dog's stun emitter at threshold — second concrete tell of restraint",
                        "Kyle's choice to file the read but fight the same way — the chapter's thesis"
                    ],
                    "payoffs": ["Mira's late call from beat 2 — confirmed pattern"],
                    "facet_hint": "discipline",
                    "tension": 8,
                    "structure_role": "rising_action",
                    "scene_type": "combat"
                },
                {
                    "beat_index": 4,
                    "title": "KT, Down",
                    "goal": "COMBAT BEAT. KT comes around the planter at a sprint, machete overhead, the dead arm useless at her side, screaming something that has stopped being words. Kyle reads her path. He steps inside the arc of the swing. The edge of Silence finds her throat at the point of closest contact. The cut is clean — the kind of clean the people who built him would have liked, and Kyle dislikes the thought the moment it lands and files it where he files things he does not want to remember thinking. The blade carries through the full motion and rings off the concrete planter on the follow-through, a hard clean note in the night air. The piezoelectric core takes the impulse. The hamon brightens. KT's machete hits the asphalt. Dex, somewhere behind the van, says something cracked: *He cut KT's head off, man. He just—* Mira: *Shut up.* Her voice is tight in a way that is not the tightness of a captain losing a fight. Kyle catalogs the tightness too.",
                    "characters_present": ["Kyle Ellen Corbin-Vasik"],
                    "location": "West Town cross-street, planter",
                    "emotional_arc": "The kill is clean, the discomfort is the cleanness. The reader feels Kyle's relationship with his own technique.",
                    "stakes": "KT's life. The night's first death. Mira's composure — registering as wrong.",
                    "seeds": [
                        "The cleanness of the cut — Kyle's discomfort with how his hands know what they know",
                        "Mira's tight voice — third concrete tell of recruitment, not war"
                    ],
                    "payoffs": ["KT's bricked arm from beat 2 — paid off in the kill"],
                    "facet_hint": "code",
                    "tension": 9,
                    "structure_role": "midpoint",
                    "scene_type": "combat"
                },
                {
                    "beat_index": 5,
                    "title": "The Spin and the Click",
                    "goal": "COMBAT BEAT. The fire escape is the problem. Nines has elevation and patience and Kyle is out of covered positions on this side of the street. He could try to close the angle — but climbing a planter to a fire escape three stories up under live fire is the kind of motion that looks like effort, and Kyle does not perform effort if there is a clean alternative. The clean alternative is in his right hand. Chorus: a shotgun-revolver, bird's-head grip, six-chamber cylinder, four 12-gauge standard rounds loaded, two empty chambers, single-action — the hammer must be cocked before each fire. The empty chambers are not empty by accident. They are reserved. From the small leather bandoleer along his right rib — three pouches, each closed with a thumb-snap — Kyle pulls one of two specialty rounds he carries: an explosive slug, twelve-gauge, manufactured by a man in West Town who has been fitting them by hand since before the Lotus Syndicate existed as an organization. He breaks Chorus open along the cylinder hinge. He drops the slug into one of the two empty chambers — the one second from the firing position. He closes the action. Then, with his thumb against the cylinder's textured edge, he *spins* it. The cylinder is balanced; it spins easily; Kyle spins it deliberately, neither fast nor slow, the way a man spins a thing whose timing he intends to control. The cylinder rotates through one full revolution and slows. Kyle counts the chamber-detents as they pass the lock-stop — one, two, three — and on the fourth detent his thumb arrests the motion at exactly the click he wants. The cylinder seats. The explosive slug is now in the firing chamber. He thumbs the hammer back. The single-action mechanism cocks with the small precise sound of a thing that has been engineered to make exactly that sound. He raises Chorus. Nines is three stories up, in profile, the rifle still tracking Kyle, the cybernetic wrist-brace catching streetlight at the inner forearm. Kyle puts the slug through Nines's right wrist. The explosive fragmentation takes the wrist apart at the brace junction; the rifle drops three stories to the alley below; Nines goes down on the grating, alive, screaming, the arterial bleeder he is now will need a tourniquet inside two minutes. Kyle does not climb. He thumbs the hammer back a second time on a standard chamber. He keeps the muzzle low. He looks at the street. Dex has broken from cover and is running. South. Fast. Kyle lets him run.",
                    "characters_present": ["Kyle Ellen Corbin-Vasik"],
                    "location": "West Town cross-street, the planter and the fire escape across",
                    "emotional_arc": "Graceful gunplay — the spin, the click, the hammer-pull, the shot. The chapter establishes Chorus as a weapon Kyle handles with the same precision he handles Silence. The cylinder click is the small theatrical beat that announces the kill before it happens.",
                    "stakes": "Nines's wrist. The rifle. The audience watching Kyle handle a complex weapon with one hand at full speed without performing speed.",
                    "seeds": [
                        "Chorus's six-chamber cylinder — four standard, two reserved chambers; specialty rounds carried in a rib-bandoleer",
                        "The hand-fit explosive slug — manufactured by a West Town gunsmith older than the Lotus Syndicate",
                        "Single-action cocking — the precise mechanical sound that will reappear in The Borrowed Hand if Chorus is used there",
                        "Dex running south — the surviving rumor-vector, will tell the rest of the city what Kyle is",
                        "Nines alive and bleeding — Kyle leaves another person alive by choice"
                    ],
                    "payoffs": [
                        "The fire escape's elevation problem — neutralized without Kyle leaving the ground",
                        "Chorus as a weapon of precision rather than spray — first full demonstration"
                    ],
                    "facet_hint": "discipline",
                    "tension": 9,
                    "structure_role": "rising_action",
                    "scene_type": "combat"
                },
                {
                    "beat_index": 6,
                    "title": "Rook, Blanked",
                    "goal": "COMBAT BEAT. Kyle drops back to street level. The dog drags itself to lunge from two meters; Kyle puts his second Chorus round through the jaw emitter housing at point-blank. The stun system dies. The dog's forward momentum carries it into his shin and he steps over it, one hand touching asphalt as he regains footing — and the piezoelectric core takes the brief contact-impulse, the hamon ticks up, and the cycle continues. Rook fires from the structure entrance — two rounds, tight group; Kyle reads them both, late at the lower bio-battery, but reads them, and moves through the gap between them in a way that looks, to anyone watching, like something that should not be physically possible. He files the watching. He has no time for the watching. He closes the distance to Rook at a run. Augmentation Signature Read has already mapped him: augmented legs (Tier 3), interface points at both hips, BCI at the right temple, no cardiac. Rook swings the pistol to track. Kyle gets inside the barrel with the last two steps; the slide presses against his ribs as it fires, the round going into concrete below his foot, the muzzle blast burning his shirt — and the edge of Silence comes in a precise arc to Rook's right temple. Passive disruption. BCI interface point. Rook's eyes go blank. The pistol drops from a hand that has forgotten it was holding something. He sits down on the concrete, breathing, every augmentation in his body simultaneously silent. He will wake up tomorrow with the worst headache of his life and no memory of the last six hours.",
                    "characters_present": ["Kyle Ellen Corbin-Vasik"],
                    "location": "West Town cross-street, parking structure entrance",
                    "emotional_arc": "Rook is left alive. The reader registers — the chapter is leaving people *alive*, by Kyle's choice. The discipline is not just technique; it is what Kyle does with the technique.",
                    "stakes": "Rook's life. The crocodile-tank-grade temptation to kill the people testing you.",
                    "seeds": ["Rook blanked, alive — will not remember tonight; will be debriefed by Mira; will be told only that the candidate cleared"],
                    "payoffs": ["The disruption-cascade technique from beats 2 and 5 — full demonstration to the watching audience"],
                    "facet_hint": "code",
                    "tension": 9,
                    "structure_role": "second_act_climax",
                    "scene_type": "combat"
                }
            ]
        },
        {
            "act_number": 3,
            "name": "The Offer",
            "purpose": "Mira left alone. The standoff. The recruitment offer made plainly. Kyle's refusal. The freelance roster counteroffer. The verbal arrangement that becomes the structural premise of Cold Chain.",
            "beats": [
                {
                    "beat_index": 7,
                    "title": "What She Was Waiting For",
                    "goal": "Mira has not run. Kyle gives her credit for that. She is standing in the open now, machine pistol at her side, pointed down. Chrome jaw catching streetlight from the west. She is looking at the dog, at Rook in the structure entrance, at the place where KT's machete fell. *Fourteen of us jumped you in the Bucktown stretch two months ago,* she says. *You walked out. We lost eight people.* Kyle says nothing. He lets her talk. *This wasn't a hit.* She is looking at his face now — or trying to; he is still in shadow. *This was a test. I needed to see if the stories were true.* The NeoCortex flags her micro-expressions. She is not lying. Kyle hates that the hardware is the thing that knows. *They're true,* he says. The machine pistol comes up. He reads the shot. He does not dodge it. The round hits the subdermal mesh on his left shoulder and deforms against the woven lattice, the impact like a punch from someone heavy. He keeps walking toward her. The hamon is sodium-white now. The full bank, taken legitimately from clean impacts and clean cuts. The light it casts across the wet asphalt between them is the chapter's only beautiful image. He stops two meters from her. *Drop the weapon.* She drops it.",
                    "characters_present": ["Kyle Ellen Corbin-Vasik", "Mira (Lotus Syndicate captain — the recruiter)"],
                    "location": "West Town cross-street, planter",
                    "emotional_arc": "The reader understands the chapter: the subdermal mesh hit was the *last interview question* — can he take a round and keep walking? The answer is the hamon at full bank, on capability, by his own discipline. The recruitment moment lands here.",
                    "stakes": "The shot to the mesh. Mira's read of his face.",
                    "seeds": [
                        "Bucktown stretch, two months ago — fourteen, eight casualties — the FIRST interview, named here for the first time and connected to tonight",
                        "Mira's *I needed to see if the stories were true* — chapter's title earned",
                        "The hamon at full bank from clean fighting — the chapter's first beautiful image"
                    ],
                    "payoffs": [
                        "The full chapter's combat — paid off in the bank reaching full at the right moment, by the right means"
                    ],
                    "facet_hint": "code",
                    "tension": 9,
                    "structure_role": "third_act_climax",
                    "scene_type": "scene"
                },
                {
                    "beat_index": 8,
                    "title": "The Roster",
                    "goal": "Mira makes the offer plain, no flourish: *Lotus wants you on payroll.* Top-tier protection, percentage of every contracted route through their territory, a salary the Pivot's rent would not feel. *You said no the first time. I'm asking again.* Kyle does not raise the blade. The hamon cools two notches. He says it level: *No.* She nods, exactly once, the way someone who expected the answer marks the answer received. She does not press. She offers the second thing, the one she came tonight to offer second: *Freelance roster, then.* Lotus contracts routes through their territory; Kyle takes first refusal on routes that need a clean reputation; carrier rates, no Lotus crew on him in the meantime. The arrangement is verbal; it lives in the cadence of two people talking in the rain rather than in any document. Kyle thinks. The math is honest. He has spent his life refusing to be anyone's man, and the freelance roster is the smallest concession this encounter could end with — his independence intact, the audition crews kept off him, and the fee structure plainly named. He takes it. *Fixer's name?* *Sable.* He files the name; she watches him file it. *She'll route the contracts.* He nods. He looks at the dog and at Rook and at the place where KT fell, the way you look at the shape of a deal you have just made with an organization that has been watching you for months. He turns. The hamon is at three-quarters. The bank he built honestly is going home with him.",
                    "characters_present": ["Kyle Ellen Corbin-Vasik", "Mira (Lotus Syndicate captain — the recruiter)"],
                    "location": "West Town cross-street, planter",
                    "emotional_arc": "The chapter's argument lands: dignified non-negotiation. Kyle does not pretend, does not perform, does not concede the larger position to escape the smaller. The deal is the smallest concession the encounter could end with, and accepting it is the right answer.",
                    "stakes": "The first offer. The second offer. The arrangement Kyle now has with the Lotus Syndicate as a freelance asset.",
                    "seeds": [
                        "The verbal arrangement — Kyle on Lotus freelance roster",
                        "Sable as the routing fixer — confirmed here by Mira; sets up Cold Chain's opening directly",
                        "Mira's *exactly once* nod — the recruiter's professional acceptance of a refusal"
                    ],
                    "payoffs": [
                        "The audition's purpose — paid off in the offer made and the deal struck",
                        "The chapter's title (The Interview) — earned"
                    ],
                    "facet_hint": "ledger",
                    "tension": 6,
                    "structure_role": "falling_action",
                    "scene_type": "scene"
                },
                {
                    "beat_index": 9,
                    "title": "West Erie Home",
                    "goal": "Kyle takes the length of West Erie south. The hamon cools by stages — sodium-white to blue-white to the cold cyan thread he started the night with. The shoulder where Mira's round hit is bruising under the subdermal mesh; the chrome bracket in his jaw aches from a takedown that did not land. Mrs. Chen's stall is dark this time of night — she opens at four, closes at midnight, the noodles are not for him tonight. He walks past anyway. The Pivot is four blocks ahead. His unit, 2F, is on the second floor with a working lock he never uses. Across the hall, 2E, Pixel is asleep — he can read her hardware signature through the wall the way he reads exits, the soft pulse of someone whose night is over. He climbs the stairs. He does not sleep. The chapter ends with Kyle sitting on the edge of the bed with Silence still in his right hand, the saya across his thighs, the hamon now the cold blue of a spent bank, and the freelance arrangement sitting in his head like a key he has just been handed to a door he has not yet opened. The next route Sable sends him will be a Lotus route. The audience reading this chapter knows what the next route is. The chapter does not say so. The chapter does not have to.",
                    "characters_present": ["Kyle Ellen Corbin-Vasik"],
                    "location": "West Erie south, then The Pivot, Unit 2F",
                    "emotional_arc": "The chapter ends quietly. The deal Kyle made is sitting in the room with him. The audience carries the deal forward into Cold Chain.",
                    "stakes": "Continuance. The next contract.",
                    "seeds": [
                        "Pixel asleep across the hall — establishes their proximity for The Borrowed Hand's takedown",
                        "Silence cooling to spent bank — chapter's closing image",
                        "The unopened door — the freelance arrangement Kyle does not yet know will cost him his hands"
                    ],
                    "payoffs": [
                        "The full bank from beat 7 — spent walking home, the chapter respecting its own arithmetic"
                    ],
                    "facet_hint": "continuance",
                    "tension": 4,
                    "structure_role": "denouement",
                    "scene_type": "scene"
                }
            ]
        }
    ],
    "character_arcs": [
        {
            "character": "Kyle Ellen Corbin-Vasik",
            "want": "To walk home from a clean job without engaging anyone.",
            "need": "To pass the interview without recognizing it as one — to be the kind of operator whose normal performance is appraisal-grade.",
            "start_state": "A working freelancer two blocks from his bed, NeoCortex at idle, expecting nothing.",
            "end_state": "On the Lotus Syndicate freelance roster by verbal arrangement; the next contract Sable routes him will be a Lotus-territory route, and the chapter does not say so.",
            "turning_point": "Beat 3 — registering the dog's calibrated emitter and choosing not to change his behavior. The chapter's thesis lives in that choice.",
            "cost": "His independence stays formally intact. His operational independence does not. The next move is no longer purely his."
        },
        {
            "character": "Mira (Lotus Syndicate captain — the recruiter)",
            "want": "To put Kyle on Lotus payroll.",
            "need": "To recognize and accept his refusal without breaking the recruiter's pose; to leave with the second-best deal cleanly executed.",
            "start_state": "A captain running a six-piece audition crew under the assumption Kyle will accept the offer or fail the test.",
            "end_state": "Has signed Kyle to the freelance roster verbally. Has lost three of her crew (KT dead, Nines disarmed, Rook blanked) to a candidate who fought like the candidate she had been told he was.",
            "turning_point": "Beat 7 — the moment she fires the round into the subdermal mesh and Kyle keeps walking. The interview is over; the offer has to be made.",
            "cost": "Three crew. The carrier-rate margin she has just given up on every Lotus route Kyle takes."
        }
    ],
    "seeds_and_payoffs": [
        {
            "seed": "Mira's late call to Nines (beat 2)",
            "planted_in_beat": 2,
            "payoff": "Pattern of restraint Kyle catalogs through beats 3-4, lands in beat 7 as Mira's confession that this was an audition",
            "payoff_in_beat": 7
        },
        {
            "seed": "The dog's calibrated stun emitter (beat 3)",
            "planted_in_beat": 3,
            "payoff": "Concrete second tell of audition; informs Kyle's decision to fight the same way regardless",
            "payoff_in_beat": 3
        },
        {
            "seed": "Mira's tight voice after KT's kill (beat 4)",
            "planted_in_beat": 4,
            "payoff": "Lands in beat 7 as her professionalism cracking briefly during the offer",
            "payoff_in_beat": 7
        },
        {
            "seed": "The Bucktown stretch two months ago — first interview, fourteen-piece, eight casualties",
            "planted_in_beat": 7,
            "payoff": "Reframes the entire chapter: tonight was the second interview, not the first, and the eight casualties were Lotus's first read of the answer",
            "payoff_in_beat": 7
        },
        {
            "seed": "The piezoelectric hamon brightening with clean impacts only",
            "planted_in_beat": 1,
            "payoff": "The full bank in beat 7 is the chapter's most beautiful image because Kyle built it cleanly — no drag-charging, no awkward TENG mechanics, just the work",
            "payoff_in_beat": 7
        },
        {
            "seed": "The freelance roster verbal arrangement (beat 8)",
            "planted_in_beat": 8,
            "payoff": "Direct setup for Cold Chain — Sable will route a Lotus-territory contract to Kyle in the next chapter, which Kyle will take because of the deal he made tonight",
            "payoff_in_beat": 9
        },
        {
            "seed": "Sable named as the routing fixer (beat 8)",
            "planted_in_beat": 8,
            "payoff": "Cold Chain opens with Sable routing the Vásquez contract through the cracked-screen terminal — Kyle takes it because of the verbal arrangement he made in this chapter",
            "payoff_in_beat": 8
        },
        {
            "seed": "Pixel asleep across the hall (beat 9)",
            "planted_in_beat": 9,
            "payoff": "The Borrowed Hand's beat 7 (the takedown) — Pixel does not wake when Lotus carries Kyle out of 2F",
            "payoff_in_beat": 9
        },
        {
            "seed": "Rook blanked, will not remember tonight",
            "planted_in_beat": 6,
            "payoff": "Mira will be debriefed alone; the Lotus Syndicate's institutional memory of Kyle is now controlled by Mira, who has signed him to the freelance roster",
            "payoff_in_beat": 8
        },
        {
            "seed": "Dex running south, surviving rumor-vector",
            "planted_in_beat": 5,
            "payoff": "The wider city now knows what Kyle is at street level; Mira's freelance roster offer is harder to refuse because the alternative is more crews like Dex's",
            "payoff_in_beat": 7
        }
    ]
}

outline_path = f'{src}/outline.json'
with open(outline_path, 'w', encoding='utf-8') as f:
    json.dump(outline, f, indent=2, ensure_ascii=False)
print(f'Wrote new outline: {outline_path}')

# 3. Update story.json — title, status, html placeholder, characters
story_path = f'{src}/story.json'
with open(story_path, encoding='utf-8-sig') as f:
    s = json.load(f)
s['title'] = 'The Interview'
s['characters'] = ['Kyle Ellen Corbin-Vasik', 'Mira (Lotus Syndicate captain — the recruiter)']
s['status'] = 'outlined'
s['html'] = "# The Interview\n\n*Protagonist: Kyle Ellen Corbin-Vasik*"
s['beats'] = []
s['modified'] = datetime.datetime.utcnow().isoformat() + 'Z'
with open(story_path, 'w', encoding='utf-8') as f:
    json.dump(s, f, indent=2, ensure_ascii=False)
print(f'story.json: title -> "The Interview", html reset to placeholder')

# 4. Reset / build the checkpoint
checkpoint_path = f'{src}/checkpoint.json'
checkpoint = {
    "ProjectId": CHAPTER_ID,
    "Title": "The Interview",
    "Protagonist": "Kyle Ellen Corbin-Vasik",
    "Characters": s['characters'],
    "Premise": outline['premise'],
    "Location": "West Town cross-street, West Erie at Ashland; The Pivot",
    "Outline": outline,
    "OutlineReview": None,
    "QualityReport": None,
    "CanonGrounding": None,
    "Beats": [],
    "FullText": "",
    "Complete": False,
    "FailureReason": None,
    "Created": datetime.datetime.utcnow().isoformat() + 'Z',
    "LastModified": datetime.datetime.utcnow().isoformat() + 'Z'
}
with open(checkpoint_path, 'w', encoding='utf-8') as f:
    json.dump(checkpoint, f, indent=2, ensure_ascii=False)
print(f'checkpoint.json: reset for regeneration')

# 5. Update Bushido Coda outline.json — replace chapter 3 entry
bo_path = f'engine/data/books/{BUSHIDO_BOOK_ID}.outline.json'
with open(bo_path, encoding='utf-8-sig') as f:
    bo = json.load(f)

new_ch3 = {
    "chapter_id": CHAPTER_ID,
    "number": 3,
    "title": "The Interview",
    "short_synopsis": "Walking home at 02:14 in the wet, Kyle is ambushed by a six-piece Lotus Syndicate crew — and three minutes in, reads what the audience does not yet: this is a hiring audition. He fights at full capability anyway, refuses Lotus's offer of payroll, and accepts a freelance roster instead — the verbal arrangement that becomes the premise of the next chapter.",
    "long_synopsis": outline['logline'] + " The chapter is graceful piezoelectric combat — Silence brightens with each clean impact, no surface-drag charging — and ends with Kyle climbing the Pivot stairs with the Lotus freelance roster sitting in his head like a key to a door he has not yet opened. The next route Sable sends him will be a Lotus route. The audience knows. The chapter does not say so.",
    "key_beats": [
        "02:14 on a wet West Town cross-street; Kyle reads six positions before he reaches them and commits to ten more steps to assess spacing",
        "Mira calls *Now*; Kyle draws Silence across the corundum strop in the same motion the saya clears; the piezoelectric hamon catches a thin cyan thread",
        "First clash with KT — passive-disruption to her augmented arm — and Mira's call to Nines arrives late: first concrete tell of restraint",
        "Rook closes; the dog's stun emitter discharges at threshold but never lands — second tell of calibrated audition; Kyle catalogs but does not change his behavior",
        "KT down clean against the planter; Mira's voice tight in a way that is not the tightness of a captain losing a fight — third tell",
        "Kyle scales the planter to the fire escape; disarms Nines through cybernetic-wrist-brace interface; Dex flees south",
        "Rook's BCI crashed at the temple — left alive; the chapter is leaving people alive by Kyle's choice",
        "Mira left at the planter; she fires once into Kyle's subdermal mesh and he keeps walking; the hamon is sodium-white, the bank built on clean fighting; she drops the weapon",
        "Mira's confession: this was an audition; the Bucktown stretch two months ago was the first; she makes the recruitment offer plain — Lotus payroll. Kyle says no",
        "She offers the freelance roster instead — Lotus contracts routed to him through Sable, carrier rates, no further audition crews. Kyle accepts the verbal arrangement",
        "Kyle takes the length of West Erie home; hamon cools through the spent-bank stages; The Pivot is four blocks ahead; Pixel asleep across the hall in 2E",
        "Chapter ends with Kyle sitting on his bed in 2F with Silence across his thighs, the freelance arrangement sitting in his head like a key to an unopened door — the next chapter (Cold Chain) is the door"
    ],
    "opens_threads": [
        "The Lotus Syndicate freelance roster — Kyle's verbal arrangement with Mira; structural premise of Cold Chain",
        "Sable as the routing fixer — confirmed here by Mira; opens Cold Chain directly",
        "Mira's professional refusal-acceptance — the chapter's argument that there are recruiters who can take *no* for an answer in the short term and not in the long term",
        "Dex running south — the surviving rumor-vector that confirms Kyle's reputation at street level",
        "The Bucktown stretch first interview — eight casualties two months ago — Lotus's prior data on Kyle"
    ],
    "closes_threads": [
        "The audition itself — Kyle has passed; the encounter has resolved into a deal",
        "The crew's KT (dead), Nines (disarmed), Rook (blanked), the dog (destroyed), Dex (fled), Mira (released by deal)"
    ],
    "state_changes": {
        "Kyle Ellen Corbin-Vasik": "On the Lotus Syndicate freelance roster by verbal arrangement. Bruised left shoulder under the subdermal mesh from Mira's calibrating round. Hamon cooling through spent-bank stages. The Pivot stairs ahead, the next contract from Sable already in shape if not yet in name.",
        "Mira (Lotus Syndicate captain)": "Has signed a candidate to the freelance roster after a two-month audition. Has lost KT, Nines's effectiveness, Rook's tonight, and the dog. Will route the next Lotus-territory contract through Sable to Kyle.",
        "The Lotus Syndicate (south-arm cell, recruitment)": "Has acquired a freelance asset on terms more expensive than payroll would have been but cheaper than another audition. The institutional memory of Kyle's capability is now Mira's alone — Rook will not remember tonight."
    },
    "pov_character": "Kyle Ellen Corbin-Vasik"
}

# Replace the existing chapter 3 entry
for i, c in enumerate(bo['chapters']):
    if c['chapter_id'] == CHAPTER_ID:
        bo['chapters'][i] = new_ch3
        break

bo['modified'] = datetime.datetime.utcnow().isoformat() + 'Z'
with open(bo_path, 'w', encoding='utf-8') as f:
    json.dump(bo, f, indent=2, ensure_ascii=False)
print(f'Bushido Coda outline.json: chapter 3 entry replaced (West Town -> The Interview)')

# 6. Update Bushido Coda book.json — premise/arc_target if they reference chapter 3 by old name
book_path = f'engine/data/books/{BUSHIDO_BOOK_ID}.json'
with open(book_path, encoding='utf-8-sig') as f:
    book = json.load(f)
# Premise references "A gang ambushes him in a wet West Town cross-street" — update phrasing
old_premise_frag = 'A gang ambushes him in a wet West Town cross-street because someone wants him made an example of.'
new_premise_frag = 'A six-piece Lotus Syndicate crew interviews him with violence on a wet West Town cross-street to see whether the stories about him are true.'
if old_premise_frag in book['premise']:
    book['premise'] = book['premise'].replace(old_premise_frag, new_premise_frag)
    print('Bushido Coda book.json: premise updated to reflect The Interview reframe')
book['modified'] = datetime.datetime.utcnow().isoformat() + 'Z'
with open(book_path, 'w', encoding='utf-8') as f:
    json.dump(book, f, indent=2, ensure_ascii=False)

print('\n--- READY FOR REGENERATION ---')
print(f'New outline: 10 beats, 10 seeds-and-payoffs, prose directive in theme/premise to remove debugging language and use piezoelectric mechanics')
