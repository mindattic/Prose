"""Create the prequel chapter 'Cold Chain' that earns the savagery of 'Without Hands'.
Outline only — Claude will write the prose against it later via the StoryDirector."""
import sys, json, os, secrets, datetime
sys.stdout.reconfigure(encoding='utf-8')

# Generate a UUID v7-style hex (timestamp + random) to match the existing chapter ID format
import uuid, time
chapter_id = uuid.uuid4().hex  # 32 hex chars

# Use a v7-style prefix for consistency: 019d / 019e / 019dd2... existing chapters
# Simpler: just generate fresh hex
chapter_id = secrets.token_hex(16)
print(f'New chapter ID: {chapter_id}')

bushido_book_id = 'eb91080d9c9c4f2b9b405fa5996bdea1'
without_hands_id = '019dd24feb047e9fb9c901450389a8b9'
chapter_dir = f'engine/data/stories/{chapter_id}'
os.makedirs(chapter_dir, exist_ok=True)

# ---------- 1. THE CHAPTER OUTLINE (StoryOutline schema) ----------
outline = {
    "title": "Cold Chain",
    "logline": "Kyle takes a forty-three-unit cold-chain pharmaceutical run for Vásquez Holdings — a contract that should have routed through the Lotus Syndicate's regular courier — and runs it clean. The complication is upstream of the delivery: a regulatory hold the buyer has to clear pushes the carrier-level settlement out of the Syndicate's collection window by exactly the length of one punishment cycle. By the time the payment clears, Kyle is already in Hua's chair.",
    "theme": "The bookkeeping nature of injustice — and the difference between completing the work and being credited for it. The discipline holds the work to a standard the system does not credit. Sometimes the math is right and the math is also too slow, and what falls into the gap between the right answer and the late answer is a man with both hands intact.",
    "premise": (
        "Kyle is offered a courier contract by Sable: forty-three units of Shen Pharmaceuticals cold-chain product, redirected at the last minute from a Lotus Syndicate route to a freelancer because the Vásquez Holdings buyer flagged the original courier as compromised. "
        "The fee is high — Φ 31,000 carrier — and the route is straightforward: pickup at a Bucktown bonded warehouse, eleven-hour cold-chain window, drop at a Vásquez-controlled receiving dock four districts east. "
        "Kyle takes the job. He runs the delivery clean: the cold chain holds, the receipts sign, the seals match, the consignment is delivered intact. "
        "Then the complication lands — not in the delivery, but in the *settlement layer*. A regulatory flag at the buyer's end (a routine Tier 4 customs review on pharmaceutical imports of this category) places a temporary hold on the carrier-level transaction. "
        "Vásquez Holdings cannot release the payment until the hold clears. The hold is twelve hours. Kyle does not know about the hold. He goes home. "
        "Meanwhile, the Lotus Syndicate's collection window — the narrow band where their accountants reconcile contract fees against routes — closes at the regular hour, with the Syndicate's expected fee from this contract conspicuously missing. "
        "Hua's lieutenants flag the discrepancy. The route was a Syndicate route. The fee did not arrive in the Syndicate's accounts. The freelancer Sable handed the contract to is named Kyle Ellen Corbin-Vasik. "
        "The Syndicate operates on the assumption that the freelancer pocketed the fee. They send a takedown crew. They take Kyle in the early morning while he is still sleeping off the run. "
        "The chapter ends as Kyle is bound to a steel chair in the south-arm chamber, jaw bruised from the takedown, NeoCortex bio-battery at 31%, watching a juvenile crocodile sleep on a heat rock and waiting for a woman he has not yet met to walk into the room."
    ),
    "characters": [
        "Kyle Ellen Corbin-Vasik",
        "Sable",
        "Mrs. Chen / Chen Wei-Lin"
    ],
    "acts": [
        {
            "act_number": 1,
            "name": "The Contract",
            "purpose": "Establish the job, the route, the fee, and the hidden upstream variable that will undo the bookkeeping. End the act with Kyle accepting the contract and beginning the run, while seeding the Syndicate's expectation that this fee belongs to them.",
            "beats": [
                {
                    "beat_index": 0,
                    "title": "The Offer",
                    "goal": "Sable routes the contract to Kyle through the cracked-screen terminal at the parts shop behind the sweating water recycler — the same terminal Kyle will use in 'Street Meat' for the Sasha extraction. The contract is forty-three units of Shen Pharmaceuticals cold-chain product, a Vásquez Holdings consignment moving from a Bucktown bonded warehouse to a receiving dock four districts east. Eleven-hour cold-chain window. Φ31,000 carrier. The job was originally a Lotus Syndicate route — Sable says this without inflection — but the buyer flagged the original courier as compromised and Vásquez asked for a freelancer with a clean reputation. Sable did not tell Vásquez who the freelancer was; she told Vásquez the freelancer was a *known quantity*, which is what Sable says about Kyle when she does not want to give his name to a buyer she has not vetted. Kyle reads the contract details. The fee is at the high end of his usual rate. The route is straightforward. The Syndicate routing is *information* — not a flag. He has worked routes the Syndicate considers theirs before. He took the job. Kyle accepts the contract. He does not ask Sable why Vásquez flagged the original courier. He should have. The chapter rests on the question he did not ask.",
                    "characters_present": ["Kyle Ellen Corbin-Vasik"],
                    "location": "Parts shop behind a water recycler, three blocks east of Mrs. Chen's noodle stall",
                    "emotional_arc": "The reader feels Kyle's professional ease — this is routine work for him. The unease the reader is supposed to feel is in the *background*: in Sable's omissions, in the casual mention of Syndicate routing, in the question Kyle does not ask.",
                    "stakes": "The fee. The route. The Syndicate's expectation that they get paid for routes that go through territory they consider theirs. Sable's reputation if Kyle blows the run.",
                    "seeds": [
                        "Φ31,000 carrier — the contract fee, the same number that will appear in the slate ping at the end of *Without Hands*",
                        "The Lotus Syndicate's original courier — flagged by the buyer as compromised, never investigated by Kyle",
                        "Sable's omission — she does not tell Kyle why the original courier was flagged, and Kyle does not ask",
                        "The eleven-hour cold-chain window — sets the delivery clock for act 2",
                        "Vásquez Holdings as the buyer — named here for the first time, will be named again as the slate ping that exonerates Kyle"
                    ],
                    "payoffs": [],
                    "facet_hint": "ideal",
                    "tension": 4,
                    "structure_role": "inciting_incident",
                    "scene_type": "scene"
                },
                {
                    "beat_index": 1,
                    "title": "Bucktown Pickup",
                    "goal": "Kyle arrives at the bonded warehouse in Bucktown at the contracted hour. The warehouse is exactly what bonded warehouses always are: a flat-roof composite structure on a side street with municipal seals on the doors and a clerk inside who has done this so many times she has stopped looking at the people doing it. The forty-three units are in a sealed cold-chain crate, the lid showing the Shen Pharmaceuticals label in two places and a Vásquez Holdings consignment number in three. The crate is heavier than its volume — the cooling unit accounts for thirty kilos. Kyle signs the chain-of-custody. He inspects the seals: intact. He inspects the cooling unit's diagnostic strip: green. He inspects the temperature reading: minus four degrees C, holding. He loads the crate onto a freelancer cart and exits the warehouse. The pickup is clean. He notes — out of habit, not suspicion — that the warehouse clerk did not ask for his name. Bonded warehouses do not ask names. He files this the way he files everything procedural and gets on the route.",
                    "characters_present": ["Kyle Ellen Corbin-Vasik"],
                    "location": "Bucktown bonded warehouse, district 47",
                    "emotional_arc": "The reader feels the weight of routine — this is work done so many times the body knows the steps. The chapter is using the routine to hide the variable.",
                    "stakes": "The seal integrity. The cold-chain temperature. Kyle's reputation if anything in the crate is wrong.",
                    "seeds": [
                        "The chain-of-custody signature — Kyle's name on the document, timestamped, the bookkeeping starting to assemble itself in the Syndicate's favor",
                        "The diagnostic strip green at pickup — establishes the cold chain held at origin, important for the exoneration",
                        "Forty-three units — the count Hua quotes in the accusation in *Without Hands*"
                    ],
                    "payoffs": [
                        "The contract from beat 0 — pickup confirms it is real and active"
                    ],
                    "facet_hint": "discipline",
                    "tension": 3,
                    "structure_role": "rising_action",
                    "scene_type": "scene"
                }
            ]
        },
        {
            "act_number": 2,
            "name": "The Run",
            "purpose": "Kyle runs the delivery clean across four districts on an eleven-hour cold-chain window, then hands the consignment to the Vásquez receiving dock with the seals intact and the temperature in spec. The chapter must let the reader watch a competent freelancer do competent work. Then — at the moment of completion, when the audience is settling into the relief of a clean job — the upstream variable arrives, off-camera, and changes everything without anyone in the chamber knowing it.",
            "beats": [
                {
                    "beat_index": 2,
                    "title": "The Pulse East",
                    "goal": "Kyle takes the cold-chain crate east on the Pulse — the GLMZ magnetic-vacuum-tube transit system, mach-6 capable, four-district hop in eleven minutes. The crate rides in the freelancer freight compartment. Kyle sits in the passenger module two cars forward and watches the cooling unit's diagnostic strip through the freight compartment camera feed. The strip stays green. He does not eat. He does not sleep. He runs the route the way he runs every route: paying attention to the variable that is most likely to fail. The Pulse arrives at the eastern terminus on schedule. The crate temperature is minus four point one degrees C. Within tolerance.",
                    "characters_present": ["Kyle Ellen Corbin-Vasik"],
                    "location": "Pulse magnetic-vacuum transit, central-to-eastern districts",
                    "emotional_arc": "The reader gets a moment of breathing room — Kyle riding a transit, the hum of the Pulse, the green diagnostic strip on a feed.",
                    "stakes": "The cold chain. The schedule.",
                    "seeds": [
                        "The Pulse — establishes Kyle has access to mach-6 transit, which is part of why the route was attractive to a freelancer",
                        "The temperature staying in tolerance — the cold chain is holding"
                    ],
                    "payoffs": [],
                    "facet_hint": "discipline",
                    "tension": 2,
                    "structure_role": "rising_action",
                    "scene_type": "scene"
                },
                {
                    "beat_index": 3,
                    "title": "The Drop",
                    "goal": "Kyle delivers the crate to the Vásquez Holdings receiving dock at the contracted hour. The receiving clerk is a Vásquez employee, mid-thirties, augmented at the eyes for barcode reading, polite in the way company employees are polite when they are being recorded. The clerk inspects the seals (intact), the diagnostic strip (green), the temperature (minus four point two degrees C), and the consignment number (matches the manifest). The clerk countersigns the chain-of-custody. The crate is unloaded onto a Vásquez internal cart and disappears through the warehouse interior doors. The clerk hands Kyle the receipt strip and the receipt's metadata flashes through Kyle's NeoCortex feed: *DELIVERY CONFIRMED — VÁSQUEZ HOLDINGS RECEIVING DOCK 4-EAST — CONSIGNMENT 0431-A — COLD CHAIN HELD — RELEASE PAYMENT PENDING REGULATORY CLEAR.* Kyle reads *RELEASE PAYMENT PENDING REGULATORY CLEAR* and pauses. The clerk says, mildly: 'Tier 4 customs review on the pharmaceutical category. Routine. They flag every shipment over a hundred units; ours got pulled despite the count being below threshold because the SKU shifted from Class B to Class A last month. Hold is usually twelve hours. Settlement releases on schedule once the hold clears.' Kyle nods. He has heard of this kind of hold. He has not encountered one personally. He files it. He does not ask whether the hold is going to delay his fee. He should have. The clerk does not volunteer the information. The chapter rests on the second question Kyle did not ask.",
                    "characters_present": ["Kyle Ellen Corbin-Vasik"],
                    "location": "Vásquez Holdings receiving dock, eastern district 4",
                    "emotional_arc": "The reader feels the surface relief — clean drop, clean receipt — and the underwater unease of the *PENDING REGULATORY CLEAR* note, which Kyle reads but does not interrogate.",
                    "stakes": "The chain-of-custody handoff. The receipt. The settlement timing.",
                    "seeds": [
                        "The receipt metadata — *RELEASE PAYMENT PENDING REGULATORY CLEAR* — the off-camera variable that will undo the bookkeeping",
                        "The Tier 4 customs hold — twelve hours, routine, normally invisible to the courier",
                        "The clerk's professional politeness — recorded, untraceable, will not remember Kyle was ever there"
                    ],
                    "payoffs": [
                        "The pickup from beat 1 — drop confirmed, cold chain held, the work is done"
                    ],
                    "facet_hint": "ledger",
                    "tension": 4,
                    "structure_role": "midpoint",
                    "scene_type": "scene"
                }
            ]
        },
        {
            "act_number": 3,
            "name": "The Hours",
            "purpose": "Kyle goes home thinking the job is done. The audience knows it is not. The Syndicate's collection window closes with their fee missing; Hua's lieutenants flag the discrepancy; the takedown crew is dispatched; Kyle is taken from his unit before the regulatory hold has cleared. The chapter ends as the chair of *Without Hands* arrives.",
            "beats": [
                {
                    "beat_index": 4,
                    "title": "Mrs. Chen's, 02:00",
                    "goal": "Kyle stops at Mrs. Chen's noodle stall on the way home — the same stall that anchors *Street Meat* and the bookkeeping coda of *Without Hands*. He orders pork bone broth and chili oil. Mrs. Chen does not ask where he has been; she has never asked. Kyle eats with both hands. The reader is supposed to register the eating-with-both-hands the way an obituary registers a smile in the last photograph. He pays. He does not say goodbye. He walks the four blocks to The Pivot. The hardware metabolism flags him as needing food and rest in equal measure. He files the flag. He climbs the stairs to 2F.",
                    "characters_present": ["Kyle Ellen Corbin-Vasik", "Mrs. Chen / Chen Wei-Lin"],
                    "location": "Mrs. Chen's noodle stall; the four blocks to The Pivot",
                    "emotional_arc": "The reader knows something is coming that Kyle does not. The eating-with-both-hands is the emotional weight of the beat. The chapter is using Kyle's normal as the loaded gun.",
                    "stakes": "Kyle's last unmolested meal. The hardware metabolism reserve he is going to need in nine hours.",
                    "seeds": [
                        "Eating with both hands — visual record the reader will carry into *Without Hands*",
                        "Mrs. Chen's silence — the bond established in *Teeth*, persistent, asks no questions",
                        "The Pivot stairs — the geography of Kyle's residence, established in earlier chapters"
                    ],
                    "payoffs": [
                        "The cold-chain run from beat 3 — Kyle treats it as complete, settles back into routine"
                    ],
                    "facet_hint": "discipline",
                    "tension": 3,
                    "structure_role": "rising_action",
                    "scene_type": "scene"
                },
                {
                    "beat_index": 5,
                    "title": "The Collection Window Closes",
                    "goal": "OFF-CAMERA from Kyle. The Lotus Syndicate's accounting window closes at the regular hour — sometime between 23:00 and 01:00, depending on the day's traffic. The accountant on duty (a Lotus middle-tier financial clerk, never named, present for one paragraph and then gone) reconciles the day's collected fees against the day's contracted routes. The Vásquez Holdings consignment is on the contracted-routes manifest. The fee — Φ31,000 — is not in the collected-fees ledger. The accountant flags the discrepancy. The flag goes up the chain to a lieutenant who hands it to Hua. Hua reads the flag. Hua reads the freelancer name on the chain-of-custody: *Kyle Ellen Corbin-Vasik.* The Lotus Syndicate operates on the assumption that the freelancer pocketed the fee. The Lotus Syndicate punishes thieves on a fixed protocol. Hua dispatches the takedown crew. The chapter must show the audience this happening at the institutional speed of bookkeeping — not as drama, but as procedure, the way a parking ticket gets written without anyone in the building knowing whose car it is.",
                    "characters_present": ["Hua (Lotus Syndicate)"],
                    "location": "Lotus Syndicate accounting room, south arm of the Cruciform Depot",
                    "emotional_arc": "The reader watches the bookkeeping issue Kyle's punishment as a clerical inevitability. The horror is institutional, not personal.",
                    "stakes": "The Syndicate's collection ledger. Hua's interpretation of the missing fee. Kyle's body, eight blocks away, asleep.",
                    "seeds": [
                        "The accountant — never named, the kind of person whose ordinary work issues a punishment that costs a man his hands",
                        "Hua reads Kyle's name — the first time she sees it; the chapter establishes that she did not know him before this",
                        "The takedown protocol — fixed, practiced, routine"
                    ],
                    "payoffs": [
                        "Sable's note in beat 0 about the route being a Syndicate route — pays off here as the reason the missing fee is flagged",
                        "Hua's accusation in *Without Hands* beat 0 — sourced here in the bookkeeping that called Kyle a thief"
                    ],
                    "facet_hint": "ledger",
                    "tension": 6,
                    "structure_role": "second_act_climax",
                    "scene_type": "scene"
                },
                {
                    "beat_index": 6,
                    "title": "The Takedown",
                    "goal": "COMBAT BEAT. The Lotus Syndicate takedown crew arrives at The Pivot at 04:17. Four men. Standard freelancer-grab kit: shock-baton, electromag pulse, sound suppressor, hood. They take the stairs quietly. They breach 2F's lock — Kyle's lock, not Pixel's — at 04:21. Kyle is asleep on the couch in his work clothes, the cold-chain receipt still in his jacket pocket, the hardware metabolism running on a recovery cycle that has not been topped off. The NeoCortex flags the breach in the half-second before it happens but does not have the bio-battery to drive the body to a defensive posture in time. The first shock-baton strike catches Kyle at the chrome bracket in his jaw — the same bracket that will be a handhold for the rest of the next twenty-four hours. The second strike puts him on the floor. The hood goes on. The takedown crew is professional. They are quiet. Pixel, across the hall in 2E, does not wake up. They carry Kyle down the stairs. The chapter ends with Kyle bound to a steel chair in the south-arm chamber of the Cruciform Depot, jaw bruised, NeoCortex bio-battery at 31%, watching a juvenile crocodile sleep on a heat rock — and the chapter's last sentence is the first sentence of *Without Hands*, exact, verbatim.",
                    "characters_present": ["Kyle Ellen Corbin-Vasik"],
                    "location": "The Pivot, Unit 2F → Cruciform Depot south-arm chamber",
                    "emotional_arc": "The reader watches the gun fire. The hand-amputation chapter is about to start. The audience now KNOWS Kyle was innocent, and they will carry that knowledge through every beat of *Without Hands*.",
                    "stakes": "Kyle's hands. Kyle's sword. The next twenty-four hours of his life.",
                    "seeds": [
                        "The chrome bracket strike — Kyle's jaw ache that becomes the handhold-of-the-night in *Without Hands*",
                        "Pixel sleeping across the hall — she does not wake up; the chapter does not need her to",
                        "Bio-battery at 31% on arrival in the chair — the exact figure that opens *Without Hands*"
                    ],
                    "payoffs": [
                        "The takedown order from beat 5 — executed",
                        "The savage scene in *Without Hands* — earned. The audience now knows Kyle did not steal. The audience now has to watch the punishment anyway."
                    ],
                    "facet_hint": "wound",
                    "tension": 8,
                    "structure_role": "third_act_climax",
                    "scene_type": "combat"
                },
                {
                    "beat_index": 7,
                    "title": "The Twelve-Hour Hold",
                    "goal": "OFF-CAMERA. CODA. While Kyle is being carried down The Pivot's stairs hooded and bleeding, somewhere in a Vásquez Holdings settlement office a Tier 4 customs reviewer at her terminal reaches the bottom of her queue, opens consignment 0431-A, finds the paperwork in order, and clicks the *clear* button. The regulatory hold lifts. The buyer's banking handshake initiates. The carrier-level transaction begins to propagate through the inter-bank settlement layer. The transaction takes nine hours to clear at full hops — by the time the clearing handshake completes, Kyle will be in a sluice trench under the Cruciform Depot integrating an automaton arm into his right wrist with the help of an entity that has been waiting eighteen years for a viable host. The chapter ends with the customs reviewer closing the case file, putting on her coat, and going home. She will never know who Kyle is. She will never know what her timely click cost him. The chapter's last image: the *clear* button, post-click, the queue advancing to the next case. The next case is the next case. The bookkeeping does not stop.",
                    "characters_present": [],
                    "location": "Vásquez Holdings settlement office, off-camera",
                    "emotional_arc": "The reader watches the second clock — the one that would have saved Kyle if it had ticked twelve hours faster — close on schedule. The cruelty of the chapter is the procedural innocence of every decision in the chain.",
                    "stakes": "The settlement Kyle is being punished for not delivering. The exoneration that will arrive at the worst possible moment in *Without Hands*.",
                    "seeds": [
                        "The customs reviewer's *clear* — the click that exonerates Kyle, completed too late",
                        "The nine-hour propagation delay — the reason the slate ping in *Without Hands* lands when it does",
                        "The bookkeeping does not stop — the chapter's thesis: institutional cruelty is the cumulative weight of timely decisions, none of them malicious"
                    ],
                    "payoffs": [
                        "The settlement layer from beat 3 — released here, will arrive in *Without Hands* beat 8 as the slate ping",
                        "The thematic argument — the punishment Kyle endures is the price of the institutional gap between *delivery confirmed* and *fee credited*"
                    ],
                    "facet_hint": "ledger",
                    "tension": 5,
                    "structure_role": "denouement",
                    "scene_type": "scene"
                }
            ]
        }
    ],
    "character_arcs": [
        {
            "character": "Kyle Ellen Corbin-Vasik",
            "want": "To complete the run, get paid, eat, sleep, take the next contract.",
            "need": "To be wrong about how much margin a clean job earns him in a system that runs on bookkeeping, not on whether the work was done.",
            "start_state": "A working freelancer with a contract on offer and a clean reputation. Believes — correctly, but not load-bearingly — that completing the work is what gets you credited for it.",
            "end_state": "Bound to a steel chair in the south-arm chamber of the Cruciform Depot, jaw bruised, NeoCortex at 31%, having completed the work and been credited as a thief. The events of *Without Hands* begin in the next breath.",
            "turning_point": "Beat 0 — accepting the contract without asking why the original courier was flagged. The chapter's thesis is in the questions Kyle did not ask.",
            "cost": "His hands. His sword. The next twenty-four hours. Whatever the rest of his life will become with Puppeteer in his head."
        },
        {
            "character": "Hua (Lotus Syndicate)",
            "want": "To collect the Syndicate's contracted fee for the Vásquez Holdings route.",
            "need": "To not act faster than the institutional bookkeeping can verify — but the Lotus Syndicate's punishment protocol does not have a 'wait twelve hours' clause, and Hua does not invent one.",
            "start_state": "A Lotus Syndicate principal running a south-arm cell, mid-fifties, real hands, no chrome, professionally competent and disinclined to wait.",
            "end_state": "Has dispatched a takedown crew to grab a freelancer she has not yet met for a fee that will arrive in her account within the next nine hours. The hubris of the bookkeeping is now load-bearing.",
            "turning_point": "Beat 5 — reading Kyle's name on the chain-of-custody and dispatching the takedown crew without authorizing the patience that would have saved her the debt she now carries.",
            "cost": "Eighty-five thousand and an apology she will never be able to make. The visible shaking of her real hands when she eventually says *I owe you*. The witness Kyle gives her, in *Without Hands*, of her own discipline failing her on the bookkeeping."
        }
    ],
    "seeds_and_payoffs": [
        {
            "seed": "Φ31,000 carrier fee on the Vásquez Holdings consignment",
            "planted_in_beat": 0,
            "payoff": "Same fee arrives in the carrier-level settlement during *Without Hands* beat 8 as the slate ping that exonerates Kyle",
            "payoff_in_beat": 7
        },
        {
            "seed": "The original Lotus Syndicate courier flagged as compromised by the buyer",
            "planted_in_beat": 0,
            "payoff": "Never investigated by Kyle; the flag was the warning the chapter rests on Kyle ignoring",
            "payoff_in_beat": 6
        },
        {
            "seed": "The Tier 4 customs hold on the pharmaceutical category — twelve hours, routine",
            "planted_in_beat": 3,
            "payoff": "The exact length of the hold is the exact length of one Lotus Syndicate punishment cycle — the institutional symmetry that lets the chapter argue cruelty is bookkeeping",
            "payoff_in_beat": 7
        },
        {
            "seed": "The diagnostic strip green at pickup, drop, and inspection",
            "planted_in_beat": 1,
            "payoff": "Establishes the cold chain held; Kyle did the work clean; the punishment is for a fee gap, not for malpractice",
            "payoff_in_beat": 3
        },
        {
            "seed": "Forty-three units of Shen Pharmaceuticals consignment",
            "planted_in_beat": 1,
            "payoff": "The exact figure Hua quotes in *Without Hands* beat 0 as the basis of the accusation",
            "payoff_in_beat": 6
        },
        {
            "seed": "Kyle eating with both hands at Mrs. Chen's stall, 02:00",
            "planted_in_beat": 4,
            "payoff": "The visual record the audience will carry through every amputation and integration beat of *Without Hands*",
            "payoff_in_beat": 6
        },
        {
            "seed": "The takedown crew breaching 2F at 04:21 with shock-batons",
            "planted_in_beat": 6,
            "payoff": "Sets up the chrome bracket ache in *Without Hands* — Kyle's handhold-of-the-night",
            "payoff_in_beat": 6
        },
        {
            "seed": "The customs reviewer's *clear* click",
            "planted_in_beat": 7,
            "payoff": "Initiates the nine-hour propagation that lands as the slate ping at the climax of *Without Hands*",
            "payoff_in_beat": 7
        },
        {
            "seed": "The bookkeeping does not stop — the chapter's thesis",
            "planted_in_beat": 7,
            "payoff": "Returns in *Without Hands* beat 8 as Hua's *I owe you*: the same bookkeeping that punished Kyle now binds Hua to him",
            "payoff_in_beat": 7
        }
    ]
}

outline_path = f'{chapter_dir}/outline.json'
with open(outline_path, 'w', encoding='utf-8') as f:
    json.dump(outline, f, indent=2, ensure_ascii=False)
print(f'Wrote outline: {outline_path}')

# ---------- 2. STORY.JSON (placeholder, like a fresh chapter) ----------
story = {
    "id": chapter_id,
    "book_id": bushido_book_id,
    "number": 4,  # Inserted before *Without Hands*; we'll renumber Without Hands to 5
    "title": "Cold Chain",
    "synopsis": outline['logline'],
    "characters": outline['characters'],
    "status": "outlined",
    "html": "# Cold Chain\n\n*Protagonist: Kyle Ellen Corbin-Vasik*",
    "beats": [],
    "created": datetime.datetime.utcnow().isoformat() + 'Z',
    "modified": datetime.datetime.utcnow().isoformat() + 'Z'
}
story_path = f'{chapter_dir}/story.json'
with open(story_path, 'w', encoding='utf-8') as f:
    json.dump(story, f, indent=2, ensure_ascii=False)
print(f'Wrote story: {story_path}')

# ---------- 3. CHECKPOINT.JSON ----------
checkpoint = {
    "ProjectId": chapter_id,
    "Title": "Cold Chain",
    "Protagonist": "Kyle Ellen Corbin-Vasik",
    "Characters": outline['characters'],
    "Premise": outline['premise'],
    "Location": "GLMZ — Bucktown to eastern receiving dock; Cruciform Depot endpoint",
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
cp_path = f'{chapter_dir}/checkpoint.json'
with open(cp_path, 'w', encoding='utf-8') as f:
    json.dump(checkpoint, f, indent=2, ensure_ascii=False)
print(f'Wrote checkpoint: {cp_path}')

# ---------- 4. ADD TO BUSHIDO CODA BOOK + RENUMBER ----------
book_path = f'engine/data/books/{bushido_book_id}.json'
with open(book_path, encoding='utf-8') as f:
    book = json.load(f)
# Insert before Without Hands
ids = book['chapter_ids']
if chapter_id not in ids:
    if without_hands_id in ids:
        idx = ids.index(without_hands_id)
        ids.insert(idx, chapter_id)
    else:
        ids.append(chapter_id)
book['modified'] = datetime.datetime.utcnow().isoformat() + 'Z'
with open(book_path, 'w', encoding='utf-8') as f:
    json.dump(book, f, indent=2, ensure_ascii=False)
print(f'Bushido Coda book.json: chapter inserted; chapter_ids now {len(ids)} chapters')

# Renumber Without Hands story.json: number=4 → number=5
wh_story_path = f'engine/data/stories/{without_hands_id}/story.json'
with open(wh_story_path, encoding='utf-8') as f:
    wh = json.load(f)
old_num = wh.get('number')
wh['number'] = 5
with open(wh_story_path, 'w', encoding='utf-8') as f:
    json.dump(wh, f, indent=2, ensure_ascii=False)
print(f'Without Hands story.json: number {old_num} -> 5')

# ---------- 5. ADD TO BUSHIDO CODA OUTLINE ----------
bo_path = f'engine/data/books/{bushido_book_id}.outline.json'
with open(bo_path, encoding='utf-8') as f:
    bo = json.load(f)

ch4_entry = {
    "chapter_id": chapter_id,
    "number": 4,
    "title": "Cold Chain",
    "short_synopsis": "Kyle takes the Vásquez Holdings cold-chain run that earns the savagery of *Without Hands* — clean pickup, clean drop, a twelve-hour customs hold the courier never sees, and a Syndicate accountant who flags the missing fee at the wrong hour.",
    "long_synopsis": outline['logline'] + " The chapter is a procedural-cruelty study: Kyle does the work clean, the institutional bookkeeping issues a punishment as a clerical inevitability, the regulatory hold lifts while Kyle is being hooded on the stairs of The Pivot, and the chapter ends with the chair of *Without Hands* arriving — exonerating clear-button click happening off-camera at the exact moment Kyle's bio-battery reads 31%.",
    "key_beats": [
        "Sable routes a Vásquez Holdings cold-chain contract to Kyle through the cracked-screen terminal — Φ31,000 carrier, forty-three units Shen Pharmaceuticals, originally a Lotus Syndicate route, the original courier flagged as compromised by the buyer",
        "Bucktown bonded warehouse: pickup clean, seals intact, diagnostic strip green, chain-of-custody signed, Kyle's name now on the document",
        "Pulse magnetic-vacuum transit east, four districts, eleven minutes, cold chain holding at minus four point one",
        "Vásquez receiving dock 4-East: drop clean, countersigned, *RELEASE PAYMENT PENDING REGULATORY CLEAR* on the receipt — Tier 4 customs hold, twelve hours, routine, Kyle does not ask whether it delays his fee",
        "Mrs. Chen's noodle stall, 02:00 — Kyle eats pork bone broth with both hands; the audience records the image",
        "OFF-CAMERA: Lotus Syndicate accounting closes its collection window with the Φ31,000 missing; the accountant flags it; Hua reads Kyle's name; the takedown is dispatched as procedure",
        "The Pivot, 04:21 — four men breach Unit 2F with shock-batons, take Kyle in his sleep; chrome-bracket strike at the jaw (the handhold-of-the-night for *Without Hands*); Pixel does not wake",
        "Chapter ends as Kyle is bound to the steel chair in the Cruciform Depot south-arm chamber, jaw bruised, NeoCortex at 31%, watching the juvenile crocodile sleep — the first sentence of *Without Hands* arrives next",
        "OFF-CAMERA CODA: A Vásquez customs reviewer reaches the bottom of her queue and clicks *clear* on consignment 0431-A; the regulatory hold lifts; the carrier-level settlement begins its nine-hour propagation; she will never know who Kyle is"
    ],
    "opens_threads": [
        "Vásquez Holdings as a recurring buyer — established in this chapter, slate-ping in *Without Hands*, possible recurring contract source for later chapters",
        "Sable's selective omissions — the reason she did not tell Kyle why the original courier was flagged is unanswered; the chapter implies she may have known and chose not to say",
        "The original Lotus Syndicate courier the buyer flagged — never investigated, possibly relevant to the larger Syndicate politics",
        "The institutional-cruelty thesis — bookkeeping as the engine of injustice, will be a recurring theme in subsequent Kyle chapters"
    ],
    "closes_threads": [
        "How Kyle ended up in Hua's chair — fully accounted for"
    ],
    "state_changes": {
        "Kyle Ellen Corbin-Vasik": "Has completed a clean cold-chain run, been credited as a thief for the gap between completion and settlement, taken in his sleep by a Lotus Syndicate takedown crew, and arrived in the chair where *Without Hands* begins.",
        "Hua (Lotus Syndicate)": "Has dispatched a takedown crew on the assumption that a freelancer she has not met pocketed her fee. Has not yet learned the fee is in transit.",
        "Sable": "Has routed a contract to Kyle that was structurally a Lotus Syndicate route. The omissions are now load-bearing on Kyle's life.",
        "The unnamed customs reviewer": "Has clicked *clear* on consignment 0431-A and gone home. Will never know."
    },
    "pov_character": "Kyle Ellen Corbin-Vasik"
}

# Renumber existing Without Hands chapter entry to 5
for c in bo['chapters']:
    if c['chapter_id'] == without_hands_id:
        c['number'] = 5
        break

# Insert ch4 before Without Hands
new_chapters = []
inserted = False
for c in bo['chapters']:
    if c['chapter_id'] == without_hands_id and not inserted:
        new_chapters.append(ch4_entry)
        inserted = True
    new_chapters.append(c)
if not inserted:
    new_chapters.append(ch4_entry)
bo['chapters'] = new_chapters
bo['modified'] = datetime.datetime.utcnow().isoformat() + 'Z'

with open(bo_path, 'w', encoding='utf-8') as f:
    json.dump(bo, f, indent=2, ensure_ascii=False)
print(f'Bushido Coda outline: chapter 4 inserted, Without Hands renumbered to 5')

print('\n--- DONE ---')
print(f'New chapter at: engine/data/stories/{chapter_id}/')
print(f'  outline.json — 8 beats, 9 seeds-and-payoffs')
print(f'  story.json — placeholder, status=outlined')
print(f'  checkpoint.json — Complete=False, ready for StoryDirector')
print()
print(f'Bushido Coda chapters now:')
for i, cid in enumerate(book['chapter_ids'], 1):
    title = 'Cold Chain' if cid == chapter_id else ('Without Hands' if cid == without_hands_id else cid[:20])
    print(f'  {i}. {title} ({cid})')
