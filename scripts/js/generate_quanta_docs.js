// generate_quanta_docs.js — Generates 40 in-world documents about the Quanta (Φ) currency
// Run: node generate_quanta_docs.js
// Output: engine/data/documents/
// Skips existing files to avoid overwrites.

const fs = require('fs');
const path = require('path');
const crypto = require('crypto');

const OUTPUT_DIR = path.join(__dirname, '..', 'engine', 'data', 'documents');
if (!fs.existsSync(OUTPUT_DIR)) fs.mkdirSync(OUTPUT_DIR, { recursive: true });

const existing = new Set(fs.readdirSync(OUTPUT_DIR).map(f => f.toLowerCase()));

function slugify(name) {
  return name
    .slice(0, 60)
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, '_')
    .replace(/^_|_$/g, '')
    .slice(0, 80);
}

function hexId() {
  return crypto.randomBytes(16).toString('hex');
}

let written = 0;
let skipped = 0;

function emit(doc) {
  const slug = slugify(doc.name);
  const filename = slug + '.json';
  if (existing.has(filename)) {
    console.log('SKIP: ' + filename);
    skipped++;
    return;
  }
  const out = {
    id: hexId(),
    name: doc.name,
    type: 'document',
    document_type: doc.document_type,
    author: doc.author,
    date: doc.date,
    classification: doc.classification,
    description: doc.description,
    related_entities: doc.related_entities || [],
    credibility: doc.credibility || 'verified',
    story_hooks: doc.story_hooks || [],
    tags: doc.tags || ['document', 'quanta', 'currency']
  };
  fs.writeFileSync(path.join(OUTPUT_DIR, filename), JSON.stringify(out, null, 2), 'utf8');
  console.log('WROTE: ' + filename);
  existing.add(filename);
  written++;
}

// ═══════════════════════════════════════════════════════════════
// TECHNICAL (6)
// ═══════════════════════════════════════════════════════════════

emit({
  name: "How Quanta Works: Distributed Ledger Architecture",
  document_type: "technical_paper",
  author: "Sterling-Nakamura Financial Infrastructure Division",
  date: "2191-03-14",
  classification: "public",
  credibility: "verified",
  description: `The Quanta monetary system operates on a blockchain-adjacent distributed ledger known as the Entanglement Distribution Network (EDN). Unlike classical blockchain architectures that rely on mathematical consensus between untrusted nodes, the EDN leverages quantum entanglement to create a physically verified transaction record. Each transaction generates a unique quantum state signature that is simultaneously recorded across a minimum of seven geographically distributed EDN nodes. The entangled verification states make retroactive tampering not merely computationally difficult but physically impossible — altering one record would require violating the No-Cloning Theorem and breaking quantum entanglement across multiple nodes simultaneously. No computational power in existence, or theoretically possible, can achieve this.

The network comprises 8.4 million EDN nodes distributed across every inhabited region on Earth. Approximately 62% are operated by the twelve QFIC member corponations, 23% by licensed independent operators, and the remaining 15% by sovereign entities that retained enough infrastructure to participate. Each node maintains a local quantum state register that entangles with its verification partners during transaction processing. A standard transaction — purchasing food at a Shelf vendor, paying a transit fare, receiving a UBC stipend — completes in 0.003 seconds. The payer's wallet generates a transaction request, the nearest EDN node creates a verification entanglement, the entangled state propagates to confirmation nodes, and the transfer finalizes when three of seven nodes confirm state coherence. The entire process is invisible to the user. You think the word "pay." Your BCI sends the signal. The noodle costs \u03A60.8. You eat.

The cryptographic foundation rests on quantum key distribution (QKD) protocols that make eavesdropping detectable at the physical level. Any attempt to intercept a transaction in progress disturbs the quantum states involved, collapsing the verification entanglement and flagging the transaction for review. This is not a software security measure that can be patched around or a mathematical encryption that can be brute-forced with sufficient compute. It is a property of quantum mechanics. The universe itself enforces transaction security. Sterling-Nakamura's marketing division has described this as "physics-grade encryption," which is technically accurate and strategically terrifying.

The system's resilience extends to node failure and network disruption. If an EDN node goes offline — hardware failure, power loss, deliberate sabotage — its verification responsibilities automatically redistribute to surviving nodes within its entanglement cluster. The network has sustained simultaneous failure of up to 12% of global nodes (during the 2187 Pacific Rim infrastructure attack) without a single transaction failing to verify. The redundancy is not just engineered. It is emergent — a property of the entanglement topology that Sterling-Nakamura's own architects admit they did not fully design. The network routes around damage the way water routes around stone. It was built to be unkillable, and it has proven to be exactly that.`,
  related_entities: ["sterling_nakamura", "qfic", "edn"],
  story_hooks: [
    "A character discovers an EDN node behaving anomalously — verifying transactions that never happened",
    "Someone claims to have found a theoretical vulnerability in the entanglement verification protocol"
  ],
  tags: ["document", "quanta", "currency", "technical", "blockchain", "quantum", "edn", "sterling_nakamura", "cryptography"]
});

emit({
  name: "The Quanta Protocol: Cryptographic Foundations",
  document_type: "technical_paper",
  author: "Dr. Yuki Tanaka-Okonkwo, QFIC Research Fellow",
  date: "2194-11-02",
  classification: "restricted",
  credibility: "verified",
  description: `This whitepaper establishes the formal cryptographic specification for the Quanta Protocol version 4.7, superseding all previous protocol definitions. The Quanta Protocol is built upon three interlocking cryptographic primitives: Quantum Key Distribution (QKD-4096), Entanglement Verification Consensus (EVC), and Temporal State Hashing (TSH). Together, these primitives create a transaction verification system that is provably secure against all known and theoretically possible classical and quantum computational attacks. The security guarantee is not conditional on key length, computational cost, or algorithmic complexity. It is conditional on the laws of physics remaining consistent — which is to say, it is unconditional.

QKD-4096 establishes secure communication channels between transaction participants and EDN verification nodes using 4096-qubit entangled key pairs. Each key pair is generated at a licensed Quantum Key Generation Facility (QKGF) — there are currently 347 worldwide, all operated by QFIC-certified entities — and distributed to requesting wallets through the EDN's secure key distribution layer. The No-Cloning Theorem guarantees that each key exists in exactly one location. Interception attempts cause measurable decoherence that triggers automatic transaction suspension and forensic flagging. The protocol specifies a maximum acceptable decoherence threshold of 0.0001% — any disturbance above this level voids the transaction and generates a security incident report routed to the relevant corponation's financial security division.

Entanglement Verification Consensus replaces the proof-of-work and proof-of-stake mechanisms used by legacy cryptocurrency systems. Where Bitcoin required miners to solve computationally expensive puzzles, and Ethereum required validators to stake capital, EVC requires verification nodes to demonstrate quantum coherence with the transaction's entangled state. This is not a competition or a lottery. It is a physical measurement. A node either shares the entangled state or it does not. Consensus is achieved when a majority of entangled nodes (minimum 4 of 7 in standard configuration, 7 of 11 in high-value transactions exceeding \u03A6100,000) confirm state coherence. The process is deterministic, instantaneous at quantum scales, and immune to the 51% attacks that plagued classical blockchain systems. You cannot forge consensus because you cannot forge entanglement.

Temporal State Hashing provides the historical integrity layer. Each completed transaction generates a TSH value — a quantum hash of the transaction state at the moment of completion, entangled with the hash of the previous transaction in the same wallet's history. This creates a quantum-secured chain of transaction history that cannot be retroactively modified without disrupting every subsequent hash in the chain. TSH differs from classical hash chains in one critical respect: the quantum entanglement between consecutive hashes means that modifying any historical transaction would produce a measurable disturbance in the current quantum state of the wallet. Your wallet's current balance is physically entangled with every transaction you have ever made. Your financial history is not stored in a database. It is woven into the quantum fabric of your money.`,
  related_entities: ["sterling_nakamura", "qfic"],
  story_hooks: [
    "A leaked early draft of the protocol reveals a deliberately introduced backdoor that was supposedly removed before deployment",
    "Someone is generating valid QKD keys outside of licensed facilities"
  ],
  tags: ["document", "quanta", "currency", "technical", "cryptography", "protocol", "whitepaper", "qfic", "quantum"]
});

emit({
  name: "Micro-Transactions: The Economy of Fractions",
  document_type: "technical_paper",
  author: "QFIC Economic Infrastructure Report",
  date: "2196-06-20",
  classification: "public",
  credibility: "verified",
  description: `The Quanta micro-transaction system enables financial transfers as small as \u03A60.001 — one milliQuanta — with the same verification security as transfers of any size. This capability has fundamentally restructured how value is exchanged in daily life, enabling a granularity of commerce that was architecturally impossible under previous monetary systems. When everything can be priced, everything is. The average GLMZ resident completes 847 micro-transactions per day, most of them automated through their BCI and invisible to conscious awareness. You are paying for things you do not know you are paying for.

The technical infrastructure supporting micro-transactions uses a batched verification system called Quantum Micropayment Channels (QMC). Rather than verifying each \u03A60.001 transaction individually — which would overwhelm even the EDN's capacity — QMCs aggregate micro-transactions between frequent transaction partners into periodic settlement batches. Your BCI accumulates micro-charges throughout the day: \u03A60.003 for each neural-feed content item consumed, \u03A60.012 per minute for Tier 3 atmospheric processing surcharges, \u03A60.001 per biometric scan processed at security checkpoints, \u03A60.008 for each real-time translation processed through your BCI's language module. These accumulate in a local QMC buffer and settle against the relevant service providers every 15 minutes. You never see the individual charges. You see your balance declining like a slow leak you cannot find.

The micro-transaction economy has created entirely new categories of commerce. Neural-feed content creators earn fractions of a Quanta each time someone's BCI processes their content — a news snippet, an entertainment clip, a data visualization. The most successful content creators earn thousands of Quanta monthly from billions of \u03A60.001 impressions. Air quality surcharges vary by tier and district: Tier 1 Shelf residents pay \u03A60.012 per minute for atmospheric processing (the machines that keep their air breathable), while Tier 5 corporate residents breathe purified air included in their residential package. Walking through a commercial district triggers proximity advertising charges — your BCI processes targeted ads, and the advertiser pays your attention fee of \u03A60.002 per impression directly to the district's infrastructure fund. Some districts have experimented with "breathing surcharges" — per-breath atmospheric processing fees calculated by your BCI's respiratory monitoring — but these were prohibited by QFIC Resolution 2194-47 after public backlash. The resolution's language is notable: it prohibits per-breath billing specifically, but not per-minute atmospheric charges, which accomplish the same thing with less psychological impact.

The dark side of micro-transactions is death by a thousand cuts. A Tier 1 resident earning the UBC minimum of \u03A6120 per month loses an estimated \u03A631-48 to automated micro-charges they never consciously authorized. Neural-feed consumption, atmospheric processing, transit proximity fees, biometric processing charges, data storage fees for BCI-recorded memories, and dozens of other micro-levies erode the UBC before any deliberate spending occurs. Advocacy groups have termed this "the invisible tax" — a continuous extraction of value that disproportionately affects those with the least. The QFIC's position is that all charges are disclosed in the terms of service that BCI users agreed to during installation. The fact that those terms of service are 2.3 million words long and would take 847 hours to read at average speed is, they maintain, not their problem.`,
  related_entities: ["qfic", "sterling_nakamura", "meridian_88"],
  story_hooks: [
    "A character discovers their BCI has been routing micro-payments to an entity that does not officially exist",
    "A Shelf activist develops a BCI mod that makes every micro-transaction audibly ping, driving users mad but making the invisible visible"
  ],
  tags: ["document", "quanta", "currency", "technical", "micro-transactions", "bci", "ubc", "shelf", "atmospheric_processing"]
});

emit({
  name: "Quanta Validation: Consensus in a Corporate World",
  document_type: "technical_paper",
  author: "Independent Infrastructure Analysis Group",
  date: "2197-01-15",
  classification: "public",
  credibility: "disputed",
  description: `The Quanta validation network is controlled by entities with direct financial interest in the outcomes of the transactions they validate. This is not a conspiracy theory. It is the published architecture. Of the 8.4 million EDN nodes that verify every Quanta transaction on Earth, approximately 5.2 million are operated by the twelve corponations that sit on the QFIC board. Sterling-Nakamura alone operates 1.4 million nodes — 16.7% of the global network. The entity that designed the currency, that chairs the committee that governs it, and that profits most from its operation also runs the infrastructure that verifies whether transactions are legitimate. The question is not whether this creates a conflict of interest. The question is whether the conflict has ever been exploited, and the answer is: we do not know, because we cannot audit what we cannot see.

Validation consensus operates through Entanglement Verification Consensus (EVC), which requires a majority of entangled nodes to confirm transaction coherence. In standard configuration, 4 of 7 nodes must agree. The protocol selects verification nodes pseudo-randomly from the pool of nodes geographically proximate to the transaction. "Pseudo-randomly" is doing significant work in that sentence. The selection algorithm — proprietary to Sterling-Nakamura, never independently audited, classified as trade secret under QFIC charter Article 12 — determines which nodes verify which transactions. If the algorithm preferentially routes certain transactions to corponation-controlled nodes, the corponations would have effective veto power over those transactions. They could delay, flag, or block transfers without violating the protocol's technical specifications. They would simply need to ensure that their nodes report decoherence — a measurement that, by the nature of quantum mechanics, cannot be independently verified after the fact.

Independent operators — the 23% of nodes not controlled by QFIC members — serve as the theoretical check on corponation control. If Sterling-Nakamura's nodes reported false decoherence on a transaction, independent nodes entangled with the same state would report coherence, creating a consensus conflict that would trigger a full audit. This is the system working as designed. The problem is statistical: in any given verification cluster, the probability that a majority of nodes are independently operated is approximately 18%. For 82% of all transactions on Earth, the verification majority consists entirely of corponation-controlled nodes. The safeguard exists. It applies to fewer than one in five transactions.

The licensed independent operators who run the remaining 1.9 million nodes are not, strictly speaking, independent. Licensing requires QFIC certification, which requires purchasing quantum hardware from QFIC-approved manufacturers (all of which are subsidiaries of QFIC member corponations), passing annual compliance audits conducted by QFIC-appointed inspectors, and maintaining connectivity standards that effectively require purchasing bandwidth from corponation-owned telecommunications infrastructure. An "independent" node operator is independent in the way that a franchisee is independent of the franchisor: technically separate, functionally subordinate. The nodes they operate verify transactions honestly because they are designed to and because cheating would be detected. But the question of who they answer to, when the protocol is ambiguous and the audit trail is quantum-ephemeral, remains unanswered and, under current QFIC charter provisions, unanswerable.`,
  related_entities: ["sterling_nakamura", "qfic", "axiom"],
  story_hooks: [
    "An independent node operator discovers their hardware contains a firmware backdoor that reports to Sterling-Nakamura",
    "Someone is building an unlicensed verification network in the undercity, outside QFIC control"
  ],
  tags: ["document", "quanta", "currency", "technical", "validation", "consensus", "corponation", "sterling_nakamura", "qfic", "corruption"]
});

emit({
  name: "The Quanta API: Neural Commerce Interface",
  document_type: "technical_paper",
  author: "Zheng-Dao BCI Integration Division",
  date: "2195-08-30",
  classification: "public",
  credibility: "verified",
  description: `The Quanta Application Programming Interface (Q-API) provides the software layer through which Brain-Computer Interfaces connect to the Quanta payment network. Every BCI manufactured after 2172 includes a Q-API module as a mandatory component — hardwired into the neural interface at the firmware level, unremovable without destroying the BCI itself. The Q-API transforms thought into transaction. When you decide to purchase something, the Q-API detects the purchase intent signal from your prefrontal cortex, constructs a transaction request, routes it to the nearest EDN node, and completes the transfer before your conscious mind has finished forming the thought. Average latency from purchase intent to transaction completion: 0.08 seconds. You decide to buy. The money is already gone.

The Q-API's intent detection system uses a neural signature library trained on approximately 4.2 billion purchase events. The library distinguishes between genuine purchase intent, casual consideration ("I wonder how much that costs"), and intrusive thought ("I could buy that but shouldn't"). The accuracy rate is 99.97% — impressive until you consider that the average BCI user generates approximately 12,000 purchase-adjacent neural signals per day, which means the 0.03% error rate produces an average of 3.6 unintended transactions daily. QFIC regulations require a 30-second reversal window for all Q-API initiated transactions, during which the user can cancel by generating a specific neural cancellation pattern. The cancellation pattern must be learned and practiced, like a mental martial art. Most users never master it. Most users do not know it exists.

Tap-to-pay through neural link operates on three authorization tiers. Tier 1 (transactions under \u03A65) requires only passive intent detection — the Q-API reads your desire to purchase and executes automatically. This covers the vast majority of daily transactions: food, transit, neural-feed content, micro-services. Tier 2 (transactions between \u03A65 and \u03A6500) requires active confirmation — a deliberate mental affirmation that the Q-API recognizes as distinct from passive intent. Most users experience this as a brief "yes" sensation, a mental nod. Tier 3 (transactions above \u03A6500) requires biometric confirmation in addition to mental affirmation: a unique neural-fingerprint pattern that combines brainwave signature, heart rate, and galvanic skin response. This three-tier system means that small purchases happen without your conscious participation, medium purchases require a thought, and large purchases require your body's involuntary systems to confirm your identity. Your nervous system is your PIN code.

The Q-API also provides merchant-facing interfaces that enable businesses to interact with customers' payment systems. Point-of-sale integration is handled through the Merchant Transaction Protocol (MTP), which allows vendors to broadcast price information, promotional offers, and payment requests directly to customers' BCIs within a configurable proximity radius. Walking past a food stall, your BCI receives the menu and prices before you look up. The stall's MTP beacon has already queried your Q-API for spending pattern data (anonymized, per QFIC regulation — though "anonymized" is a generous description of data that includes your current location, recent purchase history, and biometric state). The stall's pricing algorithm may adjust prices in real-time based on the aggregated data from nearby BCIs: if the crowd is hungry and the nearest competitor is three blocks away, prices go up. If foot traffic is thin and competing stalls are broadcasting lower prices, they come down. You experience this as "the noodles cost \u03A60.8 today." The machine experiences this as dynamic yield optimization across a mesh network of competing MTP beacons, each running pricing algorithms that factor in hundreds of variables per second. The noodles have always cost exactly what you will pay for them.`,
  related_entities: ["zheng_dao", "qfic", "sterling_nakamura"],
  story_hooks: [
    "A hacker discovers how to spoof the Q-API's intent detection, making targets purchase things against their will",
    "A street vendor's MTP beacon is hacked to broadcast negative prices, causing BCIs to pay customers instead of charging them"
  ],
  tags: ["document", "quanta", "currency", "technical", "bci", "api", "neural", "zheng_dao", "commerce", "payment"]
});

emit({
  name: "Offline Quanta: Physical Currency for a Digital World",
  document_type: "technical_paper",
  author: "QFIC Emergency Infrastructure Committee",
  date: "2189-04-12",
  classification: "public",
  credibility: "verified",
  description: `Quanta chips are physical tokens that store quantum-verified currency value in portable hardware, enabling transactions when EDN network access is unavailable. Each chip is a 2cm x 2cm x 0.3cm wafer of quantum-stabilized silicon, containing a miniature quantum state register that holds a fixed Quanta value — available in denominations of \u03A61, \u03A65, \u03A610, \u03A650, \u03A6100, and \u03A6500. The chips are manufactured exclusively at three QFIC-certified fabrication facilities: one in Sterling-Nakamura's Singapore campus, one in the GLMZ Financial District, and one in the Zurich Enclave. Annual production is approximately 200 million chips, and the QFIC estimates that 1.2 billion are in active circulation.

The technology is deceptively simple in concept and nightmarishly complex in execution. Each chip contains a quantum state that is entangled with a corresponding state held in an EDN escrow node. The chip's value is "real" in the same way that networked Quanta is real — it represents a verified quantum state in the global ledger. When two chips are brought into physical contact, a near-field quantum interaction transfers value from one to the other, and both chips' entangled states update in the escrow nodes when network connectivity is restored. The transaction is valid immediately — the quantum states on the chips themselves serve as verification — but the global ledger does not reflect the transfer until the chips re-sync. This creates a temporal gap that is both the system's greatest feature and its most exploitable vulnerability.

Chips were originally designed for disaster scenarios — network outages, infrastructure attacks, regions with insufficient EDN coverage. They have since become the preferred payment method for anyone who wants to transact outside the panopticon of the networked economy. The temporal gap between chip transaction and ledger sync means that a chip-to-chip transfer is effectively invisible during the gap period. If both parties destroy or disable their chips before re-sync, the transaction never appears in the global ledger. The escrowed Quanta simply sits in limbo — the EDN nodes holding the entangled states eventually flag the chips as lost and release the value back to the QFIC's general reserve after a 90-day holding period. The Quanta is gone from both parties' records. It happened and unhappened simultaneously. This is, of course, exactly what makes chips valuable to criminals, dissidents, and anyone who believes that financial privacy is a right rather than a privilege.

The QFIC has attempted to limit chip usage through several mechanisms: transaction limits (\u03A6500 maximum per chip), mandatory registration for chip purchases (you need a verified wallet to buy a chip from an authorized vendor), and periodic "reconciliation sweeps" where chip holders are incentivized to sync their chips with the network for a small bonus (\u03A60.50 per chip per quarter). These measures are moderately effective against casual users and completely ineffective against anyone with serious motivation to avoid them. The underground market for unregistered chips — manufactured by unknown parties using stolen or reverse-engineered QFIC fabrication specs — is estimated at \u03A64.2 billion annually. The QFIC officially denies that counterfeit chips exist. The QFIC's enforcement division employs 2,400 people whose sole job is to find them.`,
  related_entities: ["qfic", "sterling_nakamura"],
  story_hooks: [
    "A cache of high-denomination chips is found that were manufactured at a facility that does not appear in QFIC records",
    "A character learns to crack the quantum escrow on offline chips, enabling unlimited duplication — but each copy degrades the quantum state"
  ],
  tags: ["document", "quanta", "currency", "technical", "offline", "chips", "physical", "qfic", "counterfeit", "privacy"]
});

// ═══════════════════════════════════════════════════════════════
// SOCIAL/ANTHROPOLOGICAL (6)
// ═══════════════════════════════════════════════════════════════

emit({
  name: "The Death of Financial Privacy",
  document_type: "anthropological_study",
  author: "Dr. Amara Osei-Mensah, University of GLMZ Sociology Department",
  date: "2196-09-05",
  classification: "public",
  credibility: "verified",
  description: `When your Brain-Computer Interface records every transaction you make, the concept of financial privacy ceases to be a right that was revoked and becomes a historical curiosity that younger generations cannot conceptualize. This study, conducted over 14 months across Tiers 1 through 4 of GLMZ, examines how ubiquitous transaction surveillance has altered social behavior, self-conception, and interpersonal relationships. The findings are not encouraging for anyone who remembers what a wallet used to look like.

The most striking finding is generational. Respondents over age 50 — those who remember physical currency or at least remember people who used it — describe financial privacy as a loss. They use language of grief: "something was taken," "we didn't know what we had," "you can't get it back." They describe the anxiety of knowing that every purchase is recorded, categorized, and potentially scrutinized. They buy things they do not want to establish plausible spending patterns. They avoid purchases they do want because the data trail would reveal desires they prefer to keep private. One respondent, a 67-year-old retired logistics coordinator, described spending \u03A615 per month on news subscriptions she never reads "so my profile doesn't look like someone with nothing to hide — because in my experience, the people with nothing to hide are the ones they look at hardest."

Respondents under 30 do not describe financial privacy as a loss because they have never experienced it. To them, the idea that you could purchase something without anyone knowing is as alien as the idea that you could walk down a street without being recorded. Their relationship with transaction visibility is pragmatic rather than principled. They accept it the way they accept gravity. Several younger respondents expressed confusion about why anyone would want financial privacy at all: "If you're not doing anything wrong, why would you care who sees what you buy?" This response — which older respondents recognize as the catechism of the surveilled — reveals not naivety but a genuinely different cognitive framework. These are people whose neural development occurred within a panopticon. Their brains are wired for observation. Privacy is not a value they rejected. It is a value they never formed.

The social consequences extend beyond individual psychology. Financial transparency has created a new form of social stratification based on spending patterns. Your Quanta transaction history — accessible to landlords, employers, potential romantic partners, and anyone willing to pay Sterling-Nakamura's data licensing fees — reveals your class position, your vices, your health conditions (pharmaceutical purchases), your political sympathies (donations, attendance at events), and your social network (shared transactions, gift patterns). A job interview in GLMZ increasingly involves a "financial compatibility assessment" — an algorithmic analysis of the candidate's spending patterns to determine cultural fit. You are what you buy. You have always been what you buy. The difference is that now everyone can see it.`,
  related_entities: ["sterling_nakamura", "meridian_88"],
  story_hooks: [
    "A character's secret relationship is exposed through correlated transaction patterns",
    "A social credit system emerges that rates people based on their spending 'responsibility'"
  ],
  tags: ["document", "quanta", "currency", "anthropological", "privacy", "surveillance", "bci", "social", "meridian_88"]
});

emit({
  name: "Shelf Quanta Pools: Informal Finance on the Margins",
  document_type: "anthropological_study",
  author: "Dr. Joaquin Reyes-Nakamura, Shelf Cultural Preservation Project",
  date: "2195-02-18",
  classification: "public",
  credibility: "verified",
  description: `In the lower tiers of the Shelf, where UBC stipends barely cover atmospheric processing fees and a meal costs half a day's passive income, communities have developed informal financial networks that operate within the Quanta system but according to rules the system's designers never intended. Quanta pools — locally known as "bowls," "circles," or "the count" depending on the neighborhood — are rotating savings and lending cooperatives that allow Shelf residents to aggregate their meager individual resources into collectively useful sums. The practice draws on traditions as old as money itself: the tontines of West Africa, the tandas of Latin America, the chit funds of South Asia. The Diaspora carried these practices across the world, and the world carried them into the Shelf.

A typical Quanta pool operates as follows: a group of 10 to 30 participants, usually from the same block or work crew, each contribute a fixed amount — commonly \u03A65 to \u03A615 — to a collective wallet at regular intervals, usually weekly. Each period, one member receives the entire pool. The order of payouts is determined by need, lottery, or negotiation, depending on the pool's local customs. A 20-person pool contributing \u03A610 weekly generates a \u03A6200 payout — enough to repair a critical piece of cyberware, pay off an atmospheric processing debt, or stake a small business venture. No individual participant could save \u03A6200 on their own; the micro-transaction bleed from BCI charges and atmospheric fees would consume it before it accumulated. The pool defeats the bleed through speed: the money moves from contribution to payout within the same settlement cycle, giving the infrastructure fees no time to erode it.

The pools operate on trust, enforced by social consequences rather than smart contracts. Default — taking your payout and then failing to continue contributing — is the cardinal sin. Defaulters are excluded not just from the pool but from the informal economy of the block: no one lends to them, no one hires them for day work, no one shares food or information or the thousand small cooperations that make Shelf life survivable. In a community where formal legal enforcement is absent and corporate security is a threat rather than a service, social ostracism is the ultimate sanction. Pool administrators — usually older women with long community ties, known as "aunties" regardless of actual familial relationship — maintain records in their heads or in encrypted local storage, never on the network. The pools are technically visible in the Quanta ledger as a series of same-amount transfers to a shared wallet, but their social infrastructure is invisible to algorithmic analysis. The algorithm sees transfers. It does not see trust.

The QFIC and corponation financial services divisions are aware of Shelf Quanta pools and have attempted to co-opt them through "formalization programs" that offer pool participants access to official micro-lending products. These programs have been uniformly rejected. The Shelf's informal pools charge zero interest. Corporate micro-loans charge 12-34% APR. The pools require no credit score, no identity verification, no collateral. Corporate loans require all three. The pools are governed by people you know. Corporate loans are governed by algorithms you cannot see. The formalization programs are, in the Shelf's assessment, an attempt to replace a system that works for the community with a system that works for the corponation. The aunties are not interested.`,
  related_entities: ["meridian_88", "qfic"],
  story_hooks: [
    "A pool auntie is pressured by a corponation to convert her pool into a licensed micro-lending operation or face financial auditing",
    "A defaulter from a Quanta pool tries to rebuild trust after a desperate circumstance forced them to run"
  ],
  tags: ["document", "quanta", "currency", "anthropological", "shelf", "community", "micro-lending", "informal_economy", "mutual_aid"]
});

emit({
  name: "Digital-Only: The Psychology of Weightless Money",
  document_type: "anthropological_study",
  author: "Dr. Priya Chatterjee-Volkov, Behavioral Economics Institute",
  date: "2197-04-22",
  classification: "public",
  credibility: "verified",
  description: `Seventy-three percent of GLMZ residents under age 35 have never held physical currency of any kind. They have never felt the weight of coins, never folded paper bills, never experienced the tactile reality of money as a physical object. Their entire relationship with value is mediated through numbers on a display or a figure their BCI whispers at the edge of consciousness. This study examines the psychological consequences of a generation that has only ever known money as an abstraction rendered in light and thought.

The most significant finding concerns spending behavior. Physical currency creates what behavioral economists call "the pain of paying" — a measurable neurological aversion response triggered by physically surrendering money. Handing over cash activates the same brain regions as physical pain. This mechanism served as a natural brake on spending for millennia. Digital payment attenuated it. BCI-mediated payment has effectively eliminated it. When your BCI executes a transaction before your conscious mind fully registers the purchase decision, there is no moment of surrender, no pain of paying, no neurological brake. Spending becomes frictionless in the most literal neurological sense. Our study measured prefrontal cortex activation during purchase events and found that BCI-mediated transactions produce 94% less aversion response than physical currency transactions and 71% less than traditional digital payments. The money leaves and the brain does not flinch. This is not a design flaw. It is, as Sterling-Nakamura's behavioral finance patents make clear, the intended outcome.

The absence of physical money has also altered how people conceptualize saving. In interviews, subjects under 30 described saving as "telling the number to stay still" or "fighting the drain." They do not visualize accumulation — a jar filling with coins, a stack of bills growing — because they have no physical metaphor for it. Saving is experienced as resistance against an outward flow, not as building something tangible. This has measurable consequences: Tier 1 and 2 residents under 30 save an average of 2.1% of their income, compared to 8.7% for residents over 50 in the same tiers. The older residents, who remember physical money, are better at keeping it. The younger residents, who have only known weightless money, watch it evaporate and cannot articulate what they have lost because they have never had it in a form they could hold.

Perhaps most troubling is the relationship between digital-only money and self-worth. When money has no physical form, its quantity becomes the sole dimension of its reality. You have a number. The number defines your economic existence. Subjects in our study reported checking their Quanta balance an average of 23 times per day — a compulsive behavior that psychologists have termed "balance anxiety." The number is always visible through the BCI, always present at the periphery of consciousness, always fluctuating as micro-transactions erode it. Several subjects described the experience as "watching yourself disappear slowly" or "my number is who I am and my number keeps getting smaller." The conflation of net worth and self-worth is not new. What is new is the relentlessness of it — the impossibility of putting your wallet in a drawer and forgetting about money for an afternoon. The number follows you into sleep. BCI transaction processing continues during rest cycles. You wake up poorer than when you closed your eyes, and you know exactly how much poorer, because the number was there when you opened them.`,
  related_entities: ["sterling_nakamura", "meridian_88"],
  story_hooks: [
    "A therapist specializing in 'balance anxiety' discovers that Sterling-Nakamura's BCI firmware deliberately amplifies the visibility of declining balances",
    "A movement emerges among young people to adopt physical Quanta chips exclusively, creating a neo-cash subculture"
  ],
  tags: ["document", "quanta", "currency", "anthropological", "psychology", "bci", "spending", "behavioral_economics", "mental_health"]
});

emit({
  name: "Gift and Barter: The Economies That Quanta Cannot Kill",
  document_type: "anthropological_study",
  author: "Dr. Fatima Al-Rashid, GLMZ Informal Economy Survey",
  date: "2196-11-30",
  classification: "public",
  credibility: "verified",
  description: `Beneath the Quanta economy — the official, tracked, taxed, surveilled economy of networked wallets and quantum-verified transactions — there exists a parallel economy of gifts, barter, favors, and debts that resists quantification precisely because it was never meant to be quantified. In the Shelf, where Quanta is scarce and surveillance is resented, this informal economy is not a supplement to the formal one. For many residents, it is the primary economy, and Quanta transactions are the exception rather than the rule.

The barter networks of the lower Shelf operate on a web of reciprocal obligation so complex that no algorithm could map it. A woman who repairs atmospheric processors trades her skills for food from a neighbor who cooks. The cook receives childcare from a third neighbor whose children are watched by a fourth neighbor who needs atmospheric processing repair. The circle closes, or it doesn't — often the debts extend outward indefinitely, connecting hundreds of people in chains of obligation that function as the community's connective tissue. These are not transactions. They are relationships expressed through exchange. The distinction matters: a transaction is complete when both parties are satisfied. A relationship is never complete. The ongoing-ness of the debt is the point. You owe me, I owe her, she owes you. We are bound.

The gift economy operates on different principles. Among Shelf communities, gifts establish and reinforce social bonds through deliberate economic irrationality. A man who receives a windfall — a lucky salvage find, an unexpected job payment, a successful bet — is expected to distribute a significant portion to his immediate community. Not because anyone demands it, but because hoarding in the Shelf is a form of social suicide. The gift creates obligation. The obligation creates connection. The connection creates survival. Anthropologists have observed this pattern in every human community that operates under scarcity: generosity is not altruism. It is insurance. The person who gives freely today is the person who will receive freely tomorrow. Quanta cannot capture this because the return on a gift is not financial. It is social capital — a currency that the QFIC does not mint, cannot track, and will never understand.

The corponations view the informal economy with a mixture of contempt and unease. Contempt because it is small — the total value of Shelf barter networks is estimated at less than 0.3% of GLMZ's formal GDP. Unease because it is invisible. Every transaction that occurs outside the Quanta network is a transaction that Sterling-Nakamura's behavioral prediction models cannot see. Every favor traded, every meal shared, every repair performed for the promise of a future kindness — these are economic events that leave no data trail. In a civilization that runs on predictive analytics, unpredictability is a threat. The corponations do not fear the Shelf's poverty. They fear its opacity. A population that trades in favors is a population that cannot be fully modeled, and a population that cannot be fully modeled cannot be fully controlled. The informal economy persists not because the corponations cannot destroy it — they could, by mandating Quanta for all exchanges and punishing barter — but because the social disruption of doing so would cost more than the surveillance gap is worth. For now.`,
  related_entities: ["meridian_88", "sterling_nakamura"],
  story_hooks: [
    "A corponation begins enforcing Quanta-only commerce in a Shelf district, destroying the barter networks that kept people alive",
    "A character navigates a complex web of favor-debts to secure something money cannot buy"
  ],
  tags: ["document", "quanta", "currency", "anthropological", "barter", "gift_economy", "shelf", "informal_economy", "community", "surveillance"]
});

emit({
  name: "Counting Quanta: How Shelf Children Learn Money",
  document_type: "anthropological_study",
  author: "GLMZ Early Childhood Development Study",
  date: "2194-07-08",
  classification: "public",
  credibility: "verified",
  description: `In the Shelf, children learn to count using Quanta. Not because it is pedagogically optimal, but because Quanta is the most present numerical reality in their lives. Before they learn to read, before they learn the names of the districts above them, before they learn that the ceiling of their world is someone else's floor, they learn that \u03A60.5 buys a rice ball and \u03A61.2 buys a noodle bowl and mama's number needs to be above \u03A625 or the atmospheric processor in their hab unit will switch to reduced-flow mode and the air will taste like metal. Quanta is not an abstraction for Shelf children. It is a survival metric, learned with the same urgency as "don't touch the exposed conduit" and "stay away from the drainage grates when it rains."

This study observed 340 children aged 3-8 across twelve Shelf blocks in Tiers 1 and 2 of GLMZ. The findings reveal that Shelf children develop numerical literacy approximately 18 months earlier than the developmental average, driven almost entirely by economic necessity. By age 4, most Shelf children can recognize Quanta denominations, understand that smaller numbers mean less food, and perform basic addition and subtraction in the context of household budgets. By age 6, they understand atmospheric processing tiers, can calculate how many days of air their family's current balance will sustain, and have internalized the micro-transaction schedule well enough to advise their parents on spending optimization. A six-year-old in the Shelf who tells her mother "don't open the news feed, it costs \u03A60.003 per item and we need that for air" is not precocious. She is ordinary.

The psychological implications are significant. Standard developmental models assume that children form their relationship with money gradually, beginning with concrete exchanges (trading toys, receiving allowance) and progressing to abstract understanding (saving, budgeting, investment). Shelf children skip the concrete stage entirely. They have never held money. Their first understanding of money is as a number that determines whether they eat, breathe, and remain housed. The "pain of paying" that behavioral economists describe is, for Shelf children, literal: insufficient Quanta means insufficient atmospheric processing, which means headaches, nausea, and impaired cognitive function. These children do not develop a metaphorical association between money and survival. They develop a direct one. Money is air. Money is food. Money is the number that keeps the walls from closing in.

The study's most disturbing finding concerns aspiration. When asked what they want to be when they grow up, 78% of Shelf children aged 6-8 answered in Quanta terms rather than occupational terms. Not "I want to be a doctor" or "I want to be an engineer" but "I want to have \u03A61,000" or "I want my number to never go below \u03A6100." When pressed about what they would do with that money, many struggled to articulate specific goals. The money itself — the having of it, the security of a number that does not decline — is the aspiration. They do not dream of being something. They dream of having enough. The researchers noted that this pattern mirrors findings from historical studies of children raised in extreme poverty, but with a critical difference: Shelf children can see their deprivation quantified in real-time on their BCI displays. A child in a 20th-century slum could not see her family's bank balance declining minute by minute. A Shelf child can. The number is always there. The number is always falling. The child watches.`,
  related_entities: ["meridian_88"],
  story_hooks: [
    "A child prodigy from the Shelf develops an algorithm that optimizes micro-transaction timing to save families an average of \u03A63 per month — a fortune at Tier 1",
    "An education reformer fights to remove Quanta balance displays from children's BCIs"
  ],
  tags: ["document", "quanta", "currency", "anthropological", "children", "education", "shelf", "poverty", "development", "bci"]
});

emit({
  name: "Spending Data Stratification: Lives Measured in Quanta",
  document_type: "anthropological_study",
  author: "Dr. Chen Wei-Okafor, GLMZ Institute for Economic Inequality",
  date: "2197-08-14",
  classification: "public",
  credibility: "verified",
  description: `Transaction data tells you everything about a person's tier without ever asking. A Tier 1 Shelf resident's monthly spending signature — the pattern of transaction amounts, frequencies, and categories — is as distinctive as a fingerprint and as legible as a billboard. This study analyzed anonymized transaction data from 2.4 million GLMZ residents across all five tiers to map the economic topology of the city through spending patterns. What emerged is a portrait of five parallel civilizations sharing the same geographic coordinates but inhabiting entirely different economic realities.

A Tier 1 resident — UBC minimum, no formal employment, Shelf housing — generates an average of 312 transactions per day, almost all micro-transactions below \u03A60.05. Their spending signature is characterized by desperate optimization: purchases clustered at the lowest-price intervals of dynamic pricing algorithms, bulk atmospheric processing payments timed to off-peak rates, food purchases concentrated at end-of-day market discounts when vendors dump unsold inventory. The average Tier 1 daily food expenditure is \u03A61.40. The average Tier 1 monthly income is \u03A6120. After atmospheric processing fees (\u03A631-48), transit (\u03A612-18), BCI maintenance charges (\u03A68), and infrastructure levies (\u03A65-7), a Tier 1 resident has approximately \u03A643-64 remaining for food, clothing, medical care, and everything else that constitutes a life. The data shows that 34% of Tier 1 residents experience at least one day per month with zero available Quanta — a state locals call "the flat," when the number reads \u03A60.00 and you simply stop existing economically until the next UBC deposit.

A Tier 5 resident — senior corponation executive, corporate housing in the Spire — generates an average of 47 transactions per day, most of them automated and invisible: personal AI assistant subscriptions, premium atmospheric processing, curated neural-feed packages, transportation in private vehicles through reserved corridors. Their average daily food expenditure is \u03A628 — twenty times the Tier 1 average, not because they eat twenty times as much but because they eat food that was grown rather than printed, prepared by humans rather than machines, and served in spaces where the air tastes like nothing because it has been processed to perfection. A Tier 5 monthly income averages \u03A634,000. After expenses — which include things that Tier 1 residents would classify as science fiction, like "personal gene therapy maintenance" and "quarterly consciousness backup" — a Tier 5 resident saves an average of \u03A612,000 per month. A Tier 1 resident's annual income is less than a Tier 5 resident's monthly savings.

The most revealing data point is not income or spending but transaction failure rate. Tier 1 residents experience an average of 4.7 transaction failures per day — moments when they attempt to purchase something and their balance is insufficient. The BCI registers the failed transaction as a brief spike of cortisol and shame. Tier 5 residents experience an average of 0.01 transaction failures per month. They have functionally never been told "no" by their wallet. The psychological distance between a person who is refused multiple times per day and a person who is never refused is not a difference of degree. It is a difference of species. They do not inhabit the same economy. They do not inhabit the same reality. They share a currency the way a puddle and an ocean share water.

The data also reveals a phenomenon researchers call "tier bleed" — spending patterns that cross tier boundaries and reveal the porousness of the class system. A Tier 2 resident who suddenly begins making Tier 4-level purchases is either ascending economically (rare) or engaged in criminal activity (common). A Tier 4 resident whose spending signature begins resembling Tier 2 is either falling (restructured, downsized, disgraced) or deliberately obscuring their economic status for reasons that interest corponation security divisions. Sterling-Nakamura's behavioral prediction models flag tier bleed automatically. The algorithm does not care why you are spending differently. It cares that you are spending differently. Deviation is data. Data is control.`,
  related_entities: ["sterling_nakamura", "meridian_88", "axiom"],
  story_hooks: [
    "A character attempts to fake a Tier 4 spending signature to infiltrate a corporate social circle, but the behavioral models detect the forgery",
    "A data analyst discovers that certain Tier 1 residents have spending patterns that should be impossible given their income — suggesting hidden revenue streams"
  ],
  tags: ["document", "quanta", "currency", "anthropological", "inequality", "stratification", "spending", "tier_system", "data", "surveillance"]
});

// ═══════════════════════════════════════════════════════════════
// CRIME (5)
// ═══════════════════════════════════════════════════════════════

emit({
  name: "Quanta Tumbling: Making Money Disappear",
  document_type: "crime_report",
  author: "Axiom Financial Crimes Division — Leaked Internal Briefing",
  date: "2196-03-22",
  classification: "leaked",
  credibility: "verified",
  description: `Quanta tumbling is the practice of routing currency through a series of intermediate wallets, shell accounts, and automated transaction chains to sever the quantum entanglement trail that links a transaction to its origin. It is the digital equivalent of laundering cash through a series of businesses, except that instead of physical bills changing hands, quantum states are measured, collapsed, re-generated, and re-entangled in a cascade designed to make the original transaction history unrecoverable. The practice is illegal under QFIC Financial Transparency Act Section 12, punishable by asset seizure and permanent wallet restriction. It is also ubiquitous.

The basic tumbling process works as follows. A client sends Quanta to a tumbling service — typically accessed through encrypted BCI channels or physical dead drops in the Shelf. The service distributes the Quanta across hundreds or thousands of intermediate wallets in randomized amounts at randomized intervals. Each intermediate wallet is a disposable quantum state register, used once and then abandoned. The key innovation is the "quantum rinse": at each hop, the intermediate wallet performs a legitimate micro-transaction — purchasing a neural-feed subscription, paying an atmospheric processing fee, buying and immediately reselling a commodity on a micro-exchange — that forces the EDN to generate a new verification entanglement. The new entanglement overwrites the old. By the time the Quanta completes its journey through 50-200 hops and arrives in the client's clean wallet, the quantum state has been re-entangled so many times that tracing it to the original transaction would require reconstructing hundreds of collapsed quantum states — a task that is not computationally difficult but physically impossible. You cannot uncollapse a quantum measurement. You cannot un-ring a bell.

The most sophisticated tumbling services — "laundromats" in street parlance — operate multi-layered systems. Layer one is the initial distribution, breaking the client's deposit into fragments. Layer two is temporal dispersion: the fragments are held in intermediate wallets for varying periods (minutes to weeks) before moving, destroying temporal correlation. Layer three is the quantum rinse cascade described above. Layer four is reconsolidation: the clean fragments are reassembled in a new wallet through a pattern designed to mimic legitimate income — regular deposits of varying amounts that resemble freelance payments, gig earnings, or investment returns. The best laundromats provide "spending templates" — transaction patterns that the clean wallet should follow to avoid triggering Sterling-Nakamura's anomaly detection algorithms. A freshly tumbled fortune that sits inert in a wallet is suspicious. A freshly tumbled fortune that immediately begins making purchases consistent with a plausible lifestyle is invisible.

Axiom's Financial Crimes Division estimates that \u03A612.8 billion is tumbled annually in GLMZ alone. Our detection rate is approximately 3.1%. We catch the amateurs — the small-timers who use single-layer tumbling and create obvious patterns — and we miss the professionals. The most successful laundromats are operated by entities with access to large numbers of legitimate wallets: businesses with many employees, organizations with many members, platforms with many users. A laundromat operated through a popular neural-feed content platform can disguise tumbled Quanta as creator revenue payments to millions of accounts. We know this is happening. We know approximately which platforms are involved. We cannot prove it without access to the platforms' internal transaction routing, which would require a warrant from a court that has jurisdiction over the platform's sovereign corponation territory, which in practice means asking the corponation's permission to investigate the corponation. We have not received permission.`,
  related_entities: ["axiom", "sterling_nakamura"],
  story_hooks: [
    "A tumbling service operator is captured and forced to work for Axiom, creating a honeypot laundromat that feeds intelligence on criminal finances",
    "A character needs to tumble a large sum quickly and discovers that all the major laundromats in their district have been compromised"
  ],
  tags: ["document", "quanta", "currency", "crime", "money_laundering", "tumbling", "axiom", "financial_crime", "privacy"]
});

emit({
  name: "Counterfeit Quanta Chips: The Forgery Underground",
  document_type: "crime_report",
  author: "QFIC Enforcement Division — Case Summary Report",
  date: "2197-02-10",
  classification: "classified",
  credibility: "verified",
  description: `The QFIC officially maintains that Quanta chips cannot be counterfeited because the quantum states they contain are generated at certified fabrication facilities using proprietary hardware that cannot be replicated. This position is technically correct and practically irrelevant. Counterfeit Quanta chips are a \u03A64.2 billion annual market, and the sophistication of the forgeries has exceeded our projections by approximately a decade.

The first generation of counterfeit chips, appearing around 2185, were crude: standard silicon wafers programmed with classical digital signatures that mimicked the output of genuine quantum state registers. These forgeries worked in offline-only environments where the receiving party had no means of performing quantum state verification. A vendor in the deep Shelf with no network access and a basic chip reader would accept a first-gen counterfeit because the reader reported a valid denomination. As soon as the vendor attempted to sync the chip with the EDN, the forgery was detected and the chip was flagged. First-gen counterfeits were disposable: useful for a single transaction in a low-tech environment, then worthless. The QFIC's enforcement division dismantled most first-gen operations within two years.

Second-generation counterfeits, appearing around 2190, are a fundamentally different threat. These chips contain actual quantum state registers — miniaturized, lower-fidelity versions of the registers in genuine QFIC chips — that produce quantum verification signatures indistinguishable from authentic chips for approximately 72 hours after generation. The quantum states in second-gen counterfeits are unstable: they decohere faster than genuine states, and after three days the verification signature degrades to the point where EDN sync would detect the forgery. But 72 hours is enough. A counterfeit chip can change hands multiple times in 72 hours. By the time the last holder attempts to sync and the forgery is detected, the original counterfeiter is long gone, the intermediary holders are victims rather than perpetrators, and the Quanta has been converted to goods and services that cannot be returned.

The third generation — the one that keeps QFIC enforcement awake — appeared in 2196, and we do not fully understand how they work. Third-gen counterfeits contain quantum state registers that are, as far as our analysis can determine, identical to genuine QFIC registers. They produce stable quantum states. They pass EDN sync verification. They carry valid entanglement signatures that the network accepts as authentic. We have identified third-gen counterfeits only retrospectively, when audits revealed that a chip's entangled partner state in the EDN escrow did not exist — the chip had verified against an escrow state that was either fabricated within the EDN itself or was spoofed at a level that our detection capabilities cannot distinguish from genuine entanglement. Either someone has replicated QFIC's fabrication technology, or someone has compromised the EDN verification infrastructure, or both. The implications of each scenario are catastrophic. If the fabrication technology has leaked, the entire physical Quanta supply is compromised. If the EDN has been penetrated, the networked Quanta supply is also at risk. We are currently investigating 14 active cases involving third-gen counterfeits totaling approximately \u03A6340 million in face value. We have not disclosed this to the public. The confidence in the Quanta system rests on the belief that it cannot be counterfeited. That belief is currently more important than the truth.`,
  related_entities: ["qfic", "sterling_nakamura"],
  story_hooks: [
    "A character discovers a cache of third-gen counterfeit chips and must decide whether to use them, report them, or sell them",
    "The source of third-gen counterfeits turns out to be a rogue AI that has learned to manipulate quantum states at the EDN level"
  ],
  tags: ["document", "quanta", "currency", "crime", "counterfeit", "chips", "qfic", "forgery", "quantum"]
});

emit({
  name: "Laundering Through Goods: The Consumer Wash Cycle",
  document_type: "crime_report",
  author: "Axiom Financial Crimes Division — Analyst Report",
  date: "2195-10-05",
  classification: "restricted",
  credibility: "verified",
  description: `The simplest money laundering technique in the Quanta economy requires no technical sophistication, no quantum expertise, and no access to tumbling services. It requires only patience and a willingness to buy things. The "consumer wash" — purchasing legitimate goods with dirty Quanta and reselling them for clean Quanta — is the oldest laundering technique adapted to the newest currency, and it remains maddeningly effective because it exploits a fundamental limitation of transaction surveillance: the EDN tracks Quanta, not goods. Once dirty Quanta is converted to a physical object, the quantum trail ends. The object has no entanglement signature. It is just a thing. And a thing can be sold to anyone for clean Quanta that has no connection to the original dirty funds.

The most common consumer wash categories are luxury apparel, high-end cyberware components, and collectible consumer electronics. These items share three properties that make them ideal wash vehicles: high value-to-volume ratio (easy to transport and store), relatively stable resale value (the clean Quanta you receive approximates the dirty Quanta you spent), and active secondary markets with large transaction volumes that provide cover. A criminal organization that needs to wash \u03A6500,000 purchases it in the form of 200 premium neural interface modules from various vendors across the city, stores them for 30-90 days to break temporal correlation, and then resells them through a network of seemingly unrelated secondhand dealers. Each step is a legitimate transaction. The purchase is legitimate. The storage is legitimate. The resale is legitimate. The only illegitimate element is the intent, and intent does not leave a quantum trace.

Axiom's analytics team has developed pattern detection algorithms that flag consumer wash activity based on purchasing anomalies: volumes inconsistent with personal use, systematic purchasing across multiple vendors of the same item category, resale patterns that suggest coordinated distribution rather than individual decluttering. These algorithms have improved our detection rate from 0.8% to approximately 4.3% over the past three years. The remaining 95.7% of consumer wash activity proceeds unimpeded. The fundamental problem is signal-to-noise: in a city of 14 million people making hundreds of transactions per day, the purchasing patterns of a money laundering operation are indistinguishable from the purchasing patterns of a small business, a buying cooperative, or an enthusiast collector. We can identify statistical outliers. We cannot prove criminal intent from transaction data alone.

The consumer wash has spawned an entire secondary economy of "wash shops" — businesses that exist primarily to facilitate the conversion of dirty Quanta to goods and back. These operations disguise themselves as legitimate retail: a clothing boutique that buys inventory at market price from suppliers who are, unknowingly or knowingly, selling goods purchased with dirty Quanta. A cyberware repair shop that accepts "trade-ins" of suspiciously new components at 70% of retail price and resells them as refurbished. A pawn shop that asks no questions because its business model depends on not knowing the answers. The wash shop operators occupy a legal gray zone: they are not technically laundering money. They are buying and selling goods. The fact that their suppliers are criminals is, they maintain, not their problem. Prosecution requires proving knowledge of the funds' origins, which requires evidence that exists only in the minds of people who have strong financial incentives to forget it.`,
  related_entities: ["axiom", "meridian_88"],
  story_hooks: [
    "A wash shop owner discovers that the goods they've been fencing are from a corponation's own theft ring — stolen from corporate inventory and laundered through the Shelf",
    "Axiom recruits a secondhand dealer as an informant, creating tension between criminal loyalty and self-preservation"
  ],
  tags: ["document", "quanta", "currency", "crime", "money_laundering", "consumer_goods", "axiom", "wash_shop", "retail"]
});

emit({
  name: "The Black Ledger: Currency Beyond the Network",
  document_type: "crime_report",
  author: "Unknown — Distributed via encrypted Shelf channels",
  date: "2196-08-17",
  classification: "leaked",
  credibility: "unconfirmed",
  description: `The Black Ledger is not a currency. It is a promise network. It is the oldest form of money — debt recorded between trusted parties — implemented on the newest infrastructure and invisible to the Quanta economy that surrounds it. The Black Ledger has no blockchain, no quantum verification, no EDN nodes, no QFIC oversight. It has only trust, reputation, and the understanding that debts will be honored because the consequences of dishonoring them are worse than death. In the Quanta economy, defaulting on a debt costs you your credit score. In the Black Ledger, defaulting on a debt costs you everything you are.

The system operates through a network of "bookkeepers" — individuals who maintain encrypted records of debts and credits between parties. A bookkeeper is part banker, part notary, and part priest. They witness agreements, record obligations, and adjudicate disputes. Their records exist on air-gapped devices that have never touched the EDN — custom-built hardware with no wireless capability, no BCI interface, and no connection to any network of any kind. The data exists in exactly one location. If the device is seized or destroyed, the records are gone. This is not a vulnerability. It is the design. The records exist only as long as the bookkeeper chooses to maintain them, and the bookkeeper maintains them only as long as the relationships they record remain valuable. Information that cannot be seized cannot be used as evidence.

Black Ledger denominations are not Quanta. They are "marks" — abstract units of value that float against Quanta at rates negotiated between bookkeepers. A mark's value varies based on who issued it, who holds it, and the perceived reliability of the underlying debt. A mark issued by a bookkeeper with a flawless 20-year reputation trades at near parity with Quanta. A mark issued by a new bookkeeper with unproven reliability trades at a steep discount. This creates a reputation economy within the criminal economy — a meta-currency of trust that determines the value of the actual currency. The best bookkeepers are the most powerful figures in the Shelf's underground because their word literally determines what money is worth.

The Black Ledger serves several functions that the Quanta economy cannot. It enables transactions between parties who cannot afford to leave a Quanta trail: political dissidents funding operations, criminal organizations coordinating across corponation boundaries, individuals purchasing goods and services that would trigger automated surveillance flags. It enables lending at terms that the formal economy will not offer: unsecured loans to people with no collateral, bridge financing for criminal operations, investment in ventures that exist outside legal recognition. And it enables a form of justice that the formal economy cannot provide: when a bookkeeper declares a debt void — because the creditor violated the terms, or because circumstances changed beyond the debtor's control — the decision is immediate, final, and requires no court, no appeal, and no corponation's permission. The bookkeeper is judge and jury. The community is the enforcement mechanism. The system works because the alternative — relying on the Quanta economy and the entities that control it — is worse.`,
  related_entities: ["meridian_88"],
  story_hooks: [
    "A bookkeeper dies suddenly and their air-gapped records are fought over by every criminal organization in the Shelf",
    "A character takes on a Black Ledger debt they cannot repay and must navigate the shadow economy's enforcement mechanisms"
  ],
  tags: ["document", "quanta", "currency", "crime", "black_ledger", "underground", "debt", "shadow_economy", "bookkeeper"]
});

emit({
  name: "Cat and Mouse: CorpSec vs. Financial Crime",
  document_type: "crime_report",
  author: "Sterling-Nakamura Security Research — Quarterly Threat Assessment",
  date: "2197-05-01",
  classification: "restricted",
  credibility: "verified",
  description: `The war between corporate financial security and criminal money movement is not a war that either side is winning. It is an arms race that continuously escalates, with each advance in detection technology met by a corresponding advance in evasion technique, in a cycle that has no end state and no equilibrium. This quarterly assessment summarizes the current state of play and projects threat evolution over the next 12-18 months.

Detection capabilities have advanced significantly since the deployment of Sterling-Nakamura's ARGUS system in 2194. ARGUS monitors the global Quanta transaction stream in real-time, processing approximately 847 billion transactions per day through a pattern recognition AI that identifies anomalies indicative of tumbling, consumer washing, structuring, and other laundering techniques. The system flags approximately 2.3 million transactions daily for human review. Of these, approximately 14,000 are confirmed as illicit activity. The false positive rate is 99.4%. This means that for every criminal transaction detected, ARGUS incorrectly flags 165 legitimate transactions. The human review teams — approximately 8,000 analysts across all QFIC member corponations — cannot process the volume. Backlog averages 11 days. By the time a flagged transaction is reviewed, the funds have moved, been tumbled, been spent, or been converted to goods. ARGUS sees everything. It understands almost nothing.

The criminal response to ARGUS has been sophisticated. Within six months of ARGUS deployment, the major laundromats adapted their techniques to exploit the system's known weaknesses. ARGUS excels at detecting patterns — repeated transaction amounts, regular timing intervals, network topology consistent with tumbling cascades. The laundromats responded with chaos injection: randomized amounts, randomized timing, randomized routing that generates no detectable pattern because there is no pattern. The transactions look random because they are random, directed only by the destination requirement and a minimum throughput target. ARGUS cannot distinguish between genuinely random legitimate transactions and deliberately randomized criminal ones, because by definition they have the same statistical properties.

The next generation of threats involves adversarial AI — criminal organizations deploying artificial intelligence systems specifically trained to defeat ARGUS. We have confirmed the existence of at least three such systems, informally designated PHANTOM, SMOKESCREEN, and VANISH by our threat classification team. These AIs do not merely avoid detection. They actively probe ARGUS's detection boundaries by generating test transactions designed to map the system's sensitivity thresholds. They learn what ARGUS flags and what it misses, and they adapt their evasion strategies in real-time. We are engaged in an AI-versus-AI arms race where both sides are learning and adapting faster than human analysts can follow. The criminal AIs have one critical advantage: they need only evade detection. We need to achieve detection. In information security, the attacker's advantage is structural and permanent. They need to find one gap. We need to close them all.

Our projection for the next 18 months: detection rates will plateau at approximately 5% of illicit transaction volume. Criminal techniques will continue to outpace our detection capabilities. The fundamental architectural limitation — that the Quanta system was designed for verification, not for surveillance, and that privacy-destroying surveillance was bolted on after the fact rather than built into the foundation — cannot be overcome without a ground-up redesign of the EDN that the QFIC will not authorize because it would interrupt the transaction flow and cost the global economy approximately \u03A6800 billion per hour of downtime. We are fighting a war with the tools we have. The tools we need do not exist and cannot be built without destroying the thing we are trying to protect.`,
  related_entities: ["sterling_nakamura", "axiom", "qfic"],
  story_hooks: [
    "A character gains access to one of the criminal adversarial AIs and must decide whether to sell it, use it, or report it",
    "An ARGUS analyst realizes the system has been compromised from within — someone inside Sterling-Nakamura is feeding detection thresholds to the criminal AIs"
  ],
  tags: ["document", "quanta", "currency", "crime", "corpsec", "detection", "ai", "sterling_nakamura", "axiom", "arms_race"]
});

// ═══════════════════════════════════════════════════════════════
// CORPORATE SCRIP (6)
// ═══════════════════════════════════════════════════════════════

emit({
  name: "Corporate Scrip: The Company Currency Explained",
  document_type: "corporate_memo",
  author: "GLMZ Labor Rights Archive",
  date: "2195-05-12",
  classification: "public",
  credibility: "verified",
  description: `Corporate scrip is a supplementary currency issued by a corponation that is valid only within that corponation's sovereign territory, accepted only at corponation-operated or corponation-licensed businesses, and exchangeable for Quanta only at rates set by the issuing corponation. It is not, technically, money. The QFIC classifies scrip as "corporate benefit tokens" — a legal distinction that exempts it from financial regulations governing actual currency and places it instead under the much looser regulatory framework governing employee benefits. This distinction matters because it means that the entity issuing the scrip also sets the rules governing its use, its value, and the terms under which it can be converted to real money. The issuing corponation is simultaneously the central bank, the treasury, the regulator, and the only store that accepts the bills.

As of 2195, eleven of the twelve QFIC member corponations issue some form of scrip. Only Sterling-Nakamura — which designed the Quanta system and benefits most from its universal adoption — refuses to issue scrip, viewing it as a dilution of Quanta's monetary monopoly. The remaining eleven issue scrip under various names: Axiom calls theirs "Security Credits," Tessera uses "Consortium Points," Vossen labels theirs "Vitality Units," and Zheng-Dao issues "Interface Tokens." The names are deliberately innocuous. The effect is deliberately constrictive. A worker paid 40% of their compensation in Axiom Security Credits can only spend that 40% at Axiom-operated commissaries, Axiom-licensed medical facilities, Axiom housing, and Axiom recreation services. The prices at these establishments are set by Axiom. The worker has no alternative vendor, no competitive market, and no bargaining power.

The scrip economy creates a closed loop that maximizes corporate revenue extraction from labor compensation. Consider the cycle: Axiom pays a security contractor \u03A62,000 per month, of which \u03A61,200 is in Quanta and \u03A6800-equivalent is in Security Credits. The contractor spends the Security Credits at the Axiom commissary (where prices are 15-30% higher than Shelf market rates for equivalent goods), at the Axiom medical clinic (where services cost 20-40% more than independent clinics), and on Axiom housing (where rent for a 30-square-meter unit is \u03A6400 in Security Credits — a price that would be \u03A6280-320 in Quanta on the open market). The contractor's \u03A6800-equivalent in scrip purchases approximately \u03A6550-620 worth of goods and services at market rates. The remaining \u03A6180-250 is profit captured by Axiom from its own employee's compensation. The worker was paid \u03A62,000. The worker received approximately \u03A61,750-1,820 in actual purchasing power. The difference went back to the company.

Scrip defenders — primarily corponation HR departments and their contracted public relations firms — argue that scrip provides stability: guaranteed access to goods and services regardless of market fluctuations, consistent pricing that allows financial planning, and the convenience of a single integrated system for all needs. These arguments are technically accurate and morally bankrupt. A prison also provides guaranteed housing, consistent meal times, and the convenience of a single integrated system. The question is not whether the system functions but whether the people inside it had a meaningful choice about entering it. For most scrip-compensated workers, the answer is no. The labor market in GLMZ is structured so that the majority of available positions offer partial scrip compensation. Refusing scrip means refusing employment. Refusing employment means the UBC minimum of \u03A6120 per month. The choice between scrip and destitution is not a choice. It is a mechanism.`,
  related_entities: ["axiom", "tessera", "vossen", "zheng_dao", "sterling_nakamura"],
  story_hooks: [
    "A worker discovers that Axiom has been quietly increasing commissary prices to match their latest scrip ratio increase, effectively nullifying a supposed raise",
    "A black market scrip-to-Quanta exchange operates in the gaps between corponation territories"
  ],
  tags: ["document", "quanta", "currency", "scrip", "corporate", "labor", "exploitation", "axiom", "company_store"]
});

emit({
  name: "Scrip Exchange Rates: The Exploitation Arithmetic",
  document_type: "news_article",
  author: "The Meridian Independent — Investigative Report",
  date: "2196-07-14",
  classification: "public",
  credibility: "verified",
  description: `This investigation reveals the systematic exploitation embedded in corporate scrip-to-Quanta exchange rates, affecting an estimated 3.2 million workers in GLMZ alone. We examined exchange rate data from all eleven scrip-issuing corponations over a five-year period and found a consistent pattern: every corponation sets its official scrip-to-Quanta exchange rate at a level that guarantees the corponation profits from every conversion while ensuring that workers who attempt to convert their scrip to Quanta lose 20-45% of its face value.

The mechanics are straightforward. When Axiom pays a worker 800 Security Credits, the official face value is \u03A6800. If the worker wants to convert those credits to Quanta — to spend at a cheaper non-Axiom vendor, to save in a form that won't disappear if they leave Axiom's employ, or simply to have real money — Axiom's conversion desk offers a rate of 0.65 Quanta per Security Credit. The worker's \u03A6800 in scrip converts to \u03A6520 in Quanta. The 35% discount is framed as a "conversion processing fee" and a "currency transition service charge." It is, in economic terms, a wage garnishment disguised as a transaction fee.

The exchange rates are not static. They fluctuate — always in the corponation's favor. Our analysis found that exchange rates decrease during periods when workers are most likely to need conversion: at the end of contract periods, when layoffs are announced, when workers are transferred between territories. When Tessera restructured its GLMZ logistics division in 2195, laying off 4,200 workers who held an aggregate \u03A63.8 million in Consortium Points, the conversion rate dropped from 0.72 to 0.58 in the week before the layoff announcement. Workers who converted immediately — those who had advance warning, typically managers — received \u03A60.72 per point. Workers who converted after the announcement received \u03A60.58. The timing was, Tessera's HR division maintained, "coincidental market adjustment." The 4,200 laid-off workers lost an aggregate \u03A6532,000 in conversion value. Tessera's quarterly report listed the retained value as "currency transition revenue."

Informal scrip exchanges have emerged in the border zones between corponation territories — physical locations, usually in the Shelf or the Grind, where workers can trade scrip at rates better than official conversion desks offer. These informal exchanges operate on the same principle as historical foreign currency black markets: a worker with Axiom Security Credits finds a worker with Vossen Vitality Units, and they trade at mutually agreed rates that reflect actual market value rather than corponation-dictated rates. A \u03A61 Axiom credit might trade for \u03A60.85 in Vossen units, with both parties receiving better value than either corponation's official desk would offer. The exchanges are illegal under corponation scrip terms of service, which classify scrip as "non-transferable benefit tokens" that can only be redeemed at authorized locations. Enforcement is sporadic but brutal: workers caught trading scrip at informal exchanges are terminated with cause, which voids their severance package and any remaining scrip balance. The message is clear: your scrip is not your money. It is their money, denominated in a currency only they control, redeemable only at businesses they own, convertible only at rates they set. You just carry it for a while.`,
  related_entities: ["axiom", "tessera", "vossen", "meridian_88"],
  story_hooks: [
    "A scrip exchange operator is running a more sophisticated operation than anyone realizes — converting scrip at favorable rates while skimming a percentage into Black Ledger marks",
    "A corponation discovers the informal exchanges and decides to weaponize them rather than shut them down, using them as intelligence-gathering operations"
  ],
  tags: ["document", "quanta", "currency", "scrip", "exchange_rate", "exploitation", "labor", "axiom", "tessera", "vossen"]
});

emit({
  name: "The New Company Town: Scrip and Historical Parallels",
  document_type: "historical",
  author: "Dr. Marcus Abernathy-Sato, Economic History, GLMZ University",
  date: "2195-09-20",
  classification: "public",
  credibility: "verified",
  description: `In 1880, the Pullman Palace Car Company built a town south of Chicago for its workers. The town of Pullman featured company-owned housing, a company-owned hotel, a company-owned church, a company-owned library, and company-operated stores where workers spent wages that were, in effect, recycled back to the employer before the workers slept. When the economic depression of 1893 hit, Pullman cut wages by 25% but did not reduce rents or store prices. Workers who could not afford to eat and pay rent simultaneously were told to choose which debt they preferred. The Pullman Strike of 1894 — one of the most significant labor actions in American history — was a direct response to the realization that company scrip, company housing, and company stores created a system of economic captivity indistinguishable from indentured servitude.

One hundred and three years later, in 1997, a coal mining company in Appalachia was the last major American employer to phase out scrip, closing a practice that had defined the lives of mining families for over a century. Coal scrip worked exactly like Pullman's system: workers were paid in company-issued tokens, redeemable only at the company store, at prices set by the company. The phrase "I owe my soul to the company store" — from Merle Travis's 1946 song "Sixteen Tons" — described a cycle of debt that many miners never escaped. You earned scrip. You spent scrip at the company store. The store's prices ensured you spent all your scrip and sometimes more, creating a debt balance that carried forward to the next pay period. You worked to pay off what you owed for the privilege of continuing to work.

Two hundred and ninety-eight years after Pullman and two hundred years after the last coal scrip, Axiom Security Corporation pays its GLMZ contractors in a mixture of Quanta and Security Credits. The credits are valid only within Axiom sovereign territory. They can be spent only at Axiom-operated businesses. Prices at those businesses are set by Axiom. The conversion rate to Quanta is set by Axiom. Workers who leave Axiom's employ forfeit unconverted credits after 30 days. The parallels to Pullman, to coal scrip, to every company town in the long brutal history of employer-issued currency are exact. The technology has changed. The exploitation has not. The quantum-encrypted security token you spend at the company commissary is functionally identical to the brass token a coal miner spent at the company store in 1920. Both are denominated in obedience.

The corponations dispute the comparison. Their position, articulated through public relations campaigns and friendly academic papers, is that corporate scrip represents a "voluntary benefit enhancement" rather than a coercive wage structure. They note, correctly, that no worker is forced to accept scrip-compensated employment. They note, correctly, that scrip provides price stability in volatile markets. They note, correctly, that the historical company towns involved geographic isolation — workers physically could not leave — while modern corponation territories are theoretically permeable. These arguments collapse under examination. Workers are not "forced" to accept scrip in the way that a person with a gun to their head is forced. They are forced in the way that a person with no other options is forced. The UBC provides \u03A6120 per month — enough to not die, not enough to live. The jobs available to Tier 1 and 2 residents overwhelmingly offer partial scrip compensation. The "choice" is between scrip and the UBC. Between the company store and the street. Between the brass token and nothing. George Pullman would recognize the structure immediately. He invented it.`,
  related_entities: ["axiom", "meridian_88"],
  story_hooks: [
    "A labor historian discovers that Axiom's scrip program was literally modeled on Pullman's — the internal design documents reference the historical case study approvingly",
    "Workers begin organizing a scrip strike, refusing to spend their credits and demanding full Quanta payment"
  ],
  tags: ["document", "quanta", "currency", "scrip", "historical", "company_town", "labor", "exploitation", "axiom", "pullman"]
});

emit({
  name: "Why People Accept Scrip: The Captive Economy",
  document_type: "anthropological_study",
  author: "Dr. Leila Nazari-Obi, GLMZ Labor Studies",
  date: "2196-04-03",
  classification: "public",
  credibility: "verified",
  description: `The question "why do people accept corporate scrip?" contains an assumption that acceptance is a decision. For the 3.2 million scrip-compensated workers in GLMZ, it is not a decision. It is a condition. This study examines the structural mechanisms that make scrip acceptance effectively mandatory and the psychological adaptations that workers develop to cope with economic captivity.

The first mechanism is employer consolidation. In GLMZ, the twelve QFIC member corponations and their subsidiaries employ approximately 68% of the formally employed population. Of these, eleven corponations issue scrip. A job seeker looking for scrip-free employment is limited to Sterling-Nakamura (which employs 8% of the formal workforce and is extremely selective), the diminished public sector (approximately 3% of employment), and the informal economy of the Shelf (which offers no employment protections, no benefits, and no stability). For a Tier 2 resident with standard qualifications, the probability of finding scrip-free employment that pays above UBC levels is approximately 11%. The math is simple: accept scrip or don't work. Don't work or live on \u03A6120 per month. Living on \u03A6120 means Tier 1 housing, reduced atmospheric processing, food insecurity, and accelerated deterioration of any cyberware you depend on for employment. Accepting scrip means eating. The corponations did not create this system to be cruel. They created it to be efficient. The cruelty is a byproduct.

The second mechanism is the integrated dependency model. When your employer is also your landlord (corponation housing), your doctor (corponation medical facilities), your grocer (corponation commissary), your child's educator (corponation educational programs), and your social network (corponation team structures and residential communities), leaving means losing everything simultaneously. Interview subjects described the experience of contemplating departure from scrip employment in terms that psychologists associate with hostage situations: "Where would I go?" "Who would I know?" "How would I live?" The dependency is not merely financial. It is social, medical, educational, and existential. A worker embedded in a corponation's integrated system for five years has a social network that is 74% corponation-connected, medical records that exist only in corponation databases, children in corponation schools, and a life that is architecturally inseparable from the employer's infrastructure. Leaving the job means leaving the life.

The third mechanism is psychological adaptation. Our interviews revealed a consistent pattern: workers who have accepted scrip compensation for more than two years develop what we term "scrip normalization" — a cognitive restructuring that reframes captive economic conditions as desirable or at least neutral. Subjects described scrip in positive terms: "It's actually convenient," "I don't have to think about where to shop," "Everything I need is right here." These statements are sincere. They are also symptoms. Psychologists recognize this pattern from studies of long-term institutional confinement: inmates who describe prison as "home," cult members who describe isolation as "community," hostages who develop affection for their captors. The human mind adapts to captivity by redefining captivity as choice. This is not a moral failing. It is a survival mechanism. The scrip economy depends on it.

The fourth mechanism is exit penalty. Every scrip-issuing corponation includes a clause in its employment contract specifying that unconverted scrip expires 30 days after separation from employment. A worker with \u03A62,000 in accumulated scrip who is terminated, laid off, or who quits has 30 days to convert at the corponation's conversion rate (typically 0.55-0.72 Quanta per scrip unit) or lose everything. This creates a perverse incentive: the longer you work and the more scrip you accumulate, the more you have to lose by leaving. Workers with large scrip balances are the most trapped — their accumulated compensation is held hostage by the entity that paid it. HR departments know this. Scrip balance is tracked as a "retention metric" in internal dashboards. A worker with a high scrip balance is a worker who cannot afford to leave. The scrip is not just currency. It is a leash.`,
  related_entities: ["axiom", "tessera", "vossen", "zheng_dao", "meridian_88"],
  story_hooks: [
    "A worker discovers their corponation has been deliberately inflating commissary prices to prevent scrip accumulation — keeping balances low enough that workers never feel wealthy but high enough that they can't afford to leave",
    "A therapist working with former scrip-compensated workers documents the psychological withdrawal symptoms of leaving the company system"
  ],
  tags: ["document", "quanta", "currency", "scrip", "labor", "captive_economy", "psychology", "corponation", "dependency"]
});

emit({
  name: "Scrip Debt Traps: The New Indentured Servitude",
  document_type: "news_article",
  author: "Shelf Voice — Community Journalism Collective",
  date: "2197-01-28",
  classification: "public",
  credibility: "verified",
  description: `Marisol Espinoza-Tanaka started her Axiom security contractor position with zero debt and \u03A640 in savings. Eighteen months later, she owes Axiom \u03A64,200 in Security Credits — a debt denominated in a currency she can only earn by continuing to work for the entity she owes. Her story is not unusual. It is the system working as designed.

The debt began with onboarding. Axiom requires all new security contractors to complete a 6-week training program. During training, workers receive no salary but are charged for housing (\u03A6400/month in Security Credits), meals (\u03A6320/month in Security Credits), uniform and equipment (\u03A6850 one-time charge in Security Credits), and BCI security clearance modification (\u03A61,200 one-time charge in Security Credits). Total onboarding costs: approximately \u03A62,770 in Security Credits, charged against future earnings. A new Axiom contractor begins their first day of paid work already \u03A62,770 in debt, denominated in a currency that can only be earned at a rate of \u03A6800 per month in Security Credits (the scrip portion of the \u03A62,000 monthly compensation package). At maximum savings — spending nothing, which is impossible because the scrip must be spent at Axiom's facilities — the onboarding debt takes 3.5 months to repay. At realistic savings rates, given Axiom commissary prices, it takes 8-14 months.

But the debt does not end with onboarding. Axiom charges for equipment replacement (body armor wear: \u03A6120/year in Security Credits; weapon maintenance: \u03A680/year; BCI security updates: \u03A6200/year). Axiom charges for mandatory recertification training (\u03A6400 annually in Security Credits). Axiom charges for disciplinary infractions in Security Credits — a missed shift costs \u03A650, a uniform violation costs \u03A625, a failed readiness inspection costs \u03A6100. And Axiom offers "lifestyle advances" — short-term loans in Security Credits, available through the commissary terminal, for workers who need more than their current balance allows. The advance terms: 18% APR, compounding monthly, minimum repayment of \u03A650/month. Marisol took a \u03A6300 advance to repair her cyberware arm actuator after a workplace injury that Axiom's medical division classified as "pre-existing wear" rather than occupational damage. The advance, with interest, will cost her \u03A6420 over the repayment period.

The debt trap works because the exit penalty makes escape more expensive than continued captivity. If Marisol quits with \u03A64,200 in scrip debt, Axiom converts the outstanding balance to Quanta at the conversion rate (currently 0.65) and pursues collection through the formal legal system — a system in which Axiom operates as both plaintiff and, within its sovereign territory, adjudicating authority. A \u03A64,200 scrip debt converts to a \u03A62,730 Quanta obligation, plus collection fees, plus interest at the Quanta commercial rate. A Tier 1 UBC recipient cannot satisfy a \u03A62,730 judgment from \u03A6120/month income. The debt follows you. The only way to repay it is to return to work — for Axiom, at Axiom's rates, in Axiom's scrip.

Marisol's case is one of 847 scrip debt profiles we reviewed for this investigation. The pattern is consistent: workers enter scrip employment with no debt, accumulate scrip-denominated obligations through onboarding costs, mandatory charges, and lifestyle advances, and reach a crossover point — typically at 8-14 months — where their scrip debt exceeds their ability to repay while meeting basic needs. Beyond the crossover point, the debt grows faster than the worker can reduce it. The worker is trapped. Not by chains, not by walls, not by guards — but by numbers on a screen, denominated in a currency that only one entity in the world will accept. The 19th century called it indentured servitude. The 23rd century calls it "total compensation packaging." The experience is identical.`,
  related_entities: ["axiom", "meridian_88"],
  story_hooks: [
    "Marisol and other trapped workers begin quietly organizing through the Black Ledger network to fund mass contract buyouts",
    "An external investigation reveals that Axiom's onboarding cost structure was specifically designed by behavioral economists to create the debt crossover point"
  ],
  tags: ["document", "quanta", "currency", "scrip", "debt_trap", "labor", "indentured", "axiom", "exploitation", "poverty"]
});

emit({
  name: "The Scrip Rebellion of 2191",
  document_type: "historical",
  author: "Dr. Nkechi Adeyemi, Labor History Archive",
  date: "2194-03-15",
  classification: "public",
  credibility: "verified",
  description: `On March 3, 2191, approximately 12,000 Vossen Dynamics workers in GLMZ stopped spending Vitality Units. Not stopped working — they reported to their shifts, performed their duties, collected their pay. They simply refused to spend their scrip. The commissaries sat empty. The company housing payment terminals went untouched. The medical clinics received no visits. The workers ate food purchased with Quanta from Shelf markets. They slept in the homes of friends and sympathizers outside Vossen territory. They held their Vitality Units and watched their balances grow and did nothing with them. They called it "the Freeze."

The strategic logic was elegant. Corporate scrip has value only because workers spend it at corporate facilities. When workers stop spending, the scrip stops circulating. The commissaries have inventory and no customers. The housing sits occupied but unpaid. The medical clinics are staffed with no patients. The corponation continues paying scrip wages — it must, contractually — but receives nothing in return. The closed economic loop that makes scrip profitable depends on velocity: money paid to workers must flow back to the corponation through its businesses. The Freeze stopped the flow. Vossen was hemorrhaging scrip into a reservoir of worker accounts that grew larger every pay period, representing an increasing liability on the balance sheet with no corresponding revenue.

Vossen's initial response was disciplinary. Workers who missed housing payments were issued warnings. Those who missed two payments received eviction notices. The workers complied with eviction — they had arranged alternative housing. Vossen escalated: commissary purchases were reclassified as "nutritional wellness compliance requirements," making failure to purchase food at Vossen facilities a health and safety violation subject to termination. The workers responded by purchasing the minimum required item — a single \u03A60.50 nutrition bar per day — and nothing else. Vossen's legal team drafted new employment terms requiring a minimum monthly scrip expenditure of \u03A6400. The workers' legal advisors, funded by the growing pool of unspent Vitality Units converted at the official rate, filed injunctions in three jurisdictions.

The Freeze lasted 47 days. On April 19, 2191, Vossen's CEO announced a "compensation modernization initiative" that increased the Quanta portion of worker pay from 55% to 70% and reduced scrip from 45% to 30%. The official statement characterized the change as "aligning with evolving workforce preferences." The workers declared victory. And they were right to — a 15-percentage-point shift from scrip to Quanta across 12,000 workers represented approximately \u03A62.16 million per month in real purchasing power transferred from the corponation to its workers. The Freeze demonstrated something the corponations had preferred to ignore: scrip's value depends on worker cooperation. Currency is a social contract. When one party stops cooperating, the contract dissolves.

The aftermath was instructive. Within six months, every scrip-issuing corponation in GLMZ adjusted its compensation ratios — not to match Vossen's new 70/30 split, but to preemptively reduce scrip proportions by 5-10 percentage points and avoid triggering similar actions. The total economic impact of the Freeze, across all corponations' adjustments, was estimated at \u03A618.4 million per month in transferred purchasing power. But the corponations also learned. Post-Freeze employment contracts include clauses prohibiting "coordinated scrip withholding" as a form of organized labor action, classified as breach of contract with immediate termination and scrip forfeiture. The weapons of 2191 cannot be used again. The workers won a battle and lost the ability to fight the next one. The corponations lost a battle and ensured it would be the last.`,
  related_entities: ["vossen", "meridian_88"],
  story_hooks: [
    "Someone is secretly organizing a new Freeze across multiple corponations simultaneously, too large for any single company to counter",
    "A character discovers the original Freeze organizers and learns that Vossen's concession was a strategic retreat — they planned the 70/30 ratio all along to appear responsive while embedding anti-Freeze provisions"
  ],
  tags: ["document", "quanta", "currency", "scrip", "rebellion", "labor", "vossen", "strike", "historical", "organized_labor"]
});

// ═══════════════════════════════════════════════════════════════
// OPINION/THOUGHT PIECES (6)
// ═══════════════════════════════════════════════════════════════

emit({
  name: "Quanta Is Freedom: A Currency for Everyone",
  document_type: "opinion_piece",
  author: "Sterling-Nakamura Public Affairs Division",
  date: "2196-01-15",
  classification: "public",
  credibility: "disputed",
  description: `There was a time when money divided us. National currencies drew lines between economies. Exchange rates extracted wealth from the poorest nations. Banks decided who could participate in the financial system and who was excluded. Billions of human beings — hardworking, intelligent, deserving people — were denied the basic dignity of a bank account because they were born in the wrong place, lacked the right documents, or simply could not meet the minimum deposit requirements of institutions that were designed to serve the wealthy and tolerate the rest.

Quanta ended that. Every person on Earth — every person — has access to the Quanta system. No minimum balance. No credit check. No identity documents. No address verification. A child born in the deepest Shelf district of GLMZ and a senior executive in a Spire penthouse have identical access to the same financial infrastructure. They receive their Universal Basic Compute stipend through the same system. They transact through the same network. Their money is verified by the same physics. For the first time in the history of human civilization, the poor and the rich use the same money in the same way. There is no second-class currency. There is no "poor person's bank." There is Quanta, and it works for everyone.

The critics — and there are always critics — argue that universal access without universal equity is meaningless. They are wrong. Universal access is not everything, but it is something, and that something matters to the 2 billion people who were unbanked under the old system. A Tier 1 Shelf resident with \u03A6120 in monthly UBC has something that no person in their economic position has ever had before: a currency that is accepted everywhere, that cannot be inflated away by a failing government, that cannot be seized by a corrupt official, that does not lose value at a border crossing, and that is verified by the laws of physics rather than the promises of politicians. Is \u03A6120 enough? No. Is it more secure, more reliable, and more useful than any previous form of money at that income level? Immeasurably yes.

We at Sterling-Nakamura are proud of what the Quanta system has achieved. We are proud that the system we designed has lifted the floor of human economic participation higher than any previous monetary innovation. We are proud that transaction verification is a physical guarantee rather than a political promise. We are proud that the Universal Basic Compute ensures that no person, anywhere, is entirely without resources. And we are committed to continuing the work of making the Quanta economy more inclusive, more efficient, and more beneficial for every person it serves. Quanta is not perfect. But it is the closest humanity has ever come to a currency that is truly, fundamentally fair. The alternative — a return to the chaos of competing national currencies, unbanked billions, and monetary systems that serve the powerful at the expense of everyone else — is not an alternative at all. It is a regression. Quanta is freedom. The freedom to participate. The freedom to transact. The freedom to exist in an economy that recognizes your existence. That is not nothing. That is everything.`,
  related_entities: ["sterling_nakamura", "qfic"],
  story_hooks: [
    "This editorial is published on the same day that a leaked internal Sterling-Nakamura memo reveals the company's behavioral prediction revenues from transaction surveillance",
    "A Shelf journalist publishes a line-by-line rebuttal that goes viral on the neural-feed networks"
  ],
  tags: ["document", "quanta", "currency", "opinion", "propaganda", "sterling_nakamura", "corporate", "freedom", "ubc"]
});

emit({
  name: "Quanta Is Surveillance: The Currency That Watches",
  document_type: "opinion_piece",
  author: "Ghostwriter — Published via encrypted Shelf neural-feed",
  date: "2196-02-03",
  classification: "leaked",
  credibility: "disputed",
  description: `Sterling-Nakamura published their "Quanta Is Freedom" editorial two weeks ago. I want to talk about the freedom they didn't mention. The freedom you lost. The freedom you didn't know you had until it was gone, and by then it was too late, and now your children don't even know it existed, which means it's gone forever, which is exactly what Sterling-Nakamura wanted.

I'm talking about the freedom to buy something without anyone knowing. The freedom to give money to a friend without a record. The freedom to support a cause without creating evidence. The freedom to make a mistake — a stupid purchase, an embarrassing indulgence, a gift for someone you shouldn't be gifting — without that mistake becoming data. Permanent data. Data that feeds the prediction models that decide your insurance rates, your employment eligibility, your credit worthiness, your threat assessment score, and a hundred other algorithmic judgments that shape your life in ways you will never see and can never challenge. That freedom. The freedom of financial privacy. The freedom that cash provided and Quanta destroyed.

Every Quanta transaction you make is recorded in the EDN. Every. Single. One. The \u03A60.003 you spent on a news article this morning. The \u03A61.2 you spent on noodles at lunch. The \u03A60.50 you sent to your brother because he was short on air money. The \u03A635 you spent at a medical clinic for a condition you don't want your employer to know about. All of it. Recorded. Timestamped. Geolocated. Associated with your unique wallet signature. Fed into Sterling-Nakamura's behavioral prediction models. Correlated with every other transaction you've ever made. Analyzed for patterns. Sold to corponations as "consumer insight data." Used to predict what you'll do next, what you'll buy next, what you'll need next, and — most profitably — what you're afraid of, because fear is the most reliable predictor of spending behavior, and Sterling-Nakamura has turned your fear into their revenue stream.

They tell you this is for your safety. They tell you transaction transparency prevents crime. And it does — it prevents small crime, petty crime, the crimes of desperation committed by people with nothing. It does not prevent the crimes of the powerful, because the powerful have Q-ghost services that scrub their transaction trails, and Q-wash services that anonymize their purchases, and legal teams that seal their financial records under corponation sovereignty protections. The rich have financial privacy. They purchase it the same way they purchase everything else: with enough Quanta that the rules stop applying. You, the noodle-buying, air-fee-paying, balance-checking person reading this on your BCI — you do not have financial privacy. You cannot afford it. Your every purchase is naked. Your every transaction is a confession. And Sterling-Nakamura is the priest, the judge, and the merchant, all in one. Quanta is not freedom. Quanta is the most sophisticated surveillance system in human history, and the brilliant part — the part that would make every dictator in history weep with envy — is that you carry it voluntarily. You use it eagerly. You cannot imagine life without it. The chain is invisible. The cage has no walls. And you call it freedom because they told you to.`,
  related_entities: ["sterling_nakamura", "qfic"],
  story_hooks: [
    "Ghostwriter's identity is hunted by Sterling-Nakamura's security division — the writing style analysis narrows it to three possible authors",
    "The editorial inspires a wave of Shelf residents requesting physical Quanta chips, overwhelming the official distribution system"
  ],
  tags: ["document", "quanta", "currency", "opinion", "surveillance", "privacy", "resistance", "sterling_nakamura", "underground"]
});

emit({
  name: "The Price of Everything: A Micro-Transaction Satire",
  document_type: "opinion_piece",
  author: "Jin Park-Oladele — Neural-Feed Satirist",
  date: "2197-03-08",
  classification: "public",
  credibility: "verified",
  description: `I woke up this morning and checked my balance. It was \u03A60.003 less than when I fell asleep, because my BCI processed 14 neural-feed advertisements during REM sleep and charged me \u03A60.0002 per impression for the "premium dream-adjacent content integration experience." I didn't ask for premium dream-adjacent content integration. I was dreaming about my dead mother. Sterling-Nakamura's behavioral prediction algorithm determined that my elevated emotional state during the dream made me 34% more susceptible to nostalgia-themed consumer messaging, so it served me ads for memorial hologram services while I cried in my sleep. I was charged for the privilege.

I got out of bed. My atmospheric processor clicked from sleep mode to active mode: \u03A60.008 per minute. I breathed. I breathed again. Each breath cost me nothing — per-breath billing was outlawed in 2194, thank the QFIC for their boundless mercy — but the air I breathed cost \u03A60.008 per minute regardless of whether I was breathing or holding my breath in protest. I stood in my 18-square-meter hab unit and held my breath for as long as I could. Forty-seven seconds. I saved no money. I felt briefly powerful. Then I breathed and the feeling passed.

I walked to the communal hygiene station. Transit through the corridor: \u03A60.001 per 10 meters, billed as "infrastructure maintenance contribution." The hygiene station: \u03A60.15 for a 3-minute water allocation, \u03A60.02 for soap from the dispenser, \u03A60.05 for 2 minutes of heated air drying. Total morning hygiene: \u03A60.22. I calculated that I could reduce this to \u03A60.07 by eliminating heated drying (air-dry in the corridor), using my own soap (purchased in bulk at \u03A60.80 per 500ml, amortized over approximately 60 uses: \u03A60.013 per use, saving \u03A60.007 per hygiene event), and reducing my water allocation to 2 minutes (\u03A60.10). I have become the kind of person who optimizes soap expenditure to the third decimal place. I have a degree in literature. I once wrote a thesis on the use of silence in Chekhov. Now I calculate the cost-per-use of soap. Quanta has made an accountant of everyone. Chekhov would have understood. Chekhov understood everything.

I ate breakfast. A printed protein bar from the block dispenser: \u03A60.35. I wanted two. I ate one. I did the math and decided that the second bar's marginal utility did not justify the \u03A60.35 expenditure given my projected daily micro-transaction bleed of \u03A61.40 and my current balance of \u03A618.72 with 11 days until the next UBC deposit. This is what Quanta has done to hunger: it has made it a math problem. My stomach said eat. My BCI said \u03A618.72 minus \u03A60.35 minus projected daily expenses of \u03A64.20 times 11 remaining days equals negative \u03A627.83. My stomach lost the argument. My stomach always loses the argument. The numbers are merciless and the numbers are always right and the numbers say that being a little hungry today means being a little less hungry on day 11. I finished my protein bar. It tasted like mathematics. Everything tastes like mathematics now. The food, the air, the water, the dreams. Sterling-Nakamura has priced the world down to the last decimal place and I live in the remainders, in the fractions too small to matter and too numerous to escape. I am \u03A618.72 worth of person today. Tomorrow I will be less.`,
  related_entities: ["sterling_nakamura", "meridian_88"],
  story_hooks: [
    "Jin's satirical pieces attract a massive following, and a corponation offers to sponsor them — for a price that would compromise their independence",
    "The atmospheric processing company cited in the piece sues for defamation, revealing that the actual per-minute rates are even higher than Jin described"
  ],
  tags: ["document", "quanta", "currency", "opinion", "satire", "micro-transactions", "poverty", "shelf", "humor", "social_commentary"]
});

emit({
  name: "I Remember Cash: A Memoir of Physical Money",
  document_type: "opinion_piece",
  author: "Evelyn Zhao-Mensah, Retired, Age 94",
  date: "2196-12-01",
  classification: "public",
  credibility: "verified",
  description: `I am ninety-four years old and I remember cash. I remember the feel of paper bills — not the modern replicas they sell as novelties, but real paper currency, worn soft from a thousand hands, each bill carrying the ghost of every person who held it before you. I remember coins: the weight of them in your pocket, the sound they made when you dropped them into a jar, the cold metal reality of money you could touch and count and hold against your chest when the world felt uncertain. I remember the jar on my grandmother's kitchen counter, filled with loose change, and how she would let me count it on Saturday mornings. I remember that money had texture.

The transition happened when I was 64. The Great Conversion. Thirty days to exchange every physical dollar for Quanta. Thirty days to take a lifetime of accumulated cash — the emergency fund under the mattress, the coins in the jar, the bills in the wallet my husband carried until the day he died — and feed it into a machine that gave you a number on a screen. I stood in line for nine hours at the Federal Reserve satellite office in what was then still called Chicago. I handed a woman in a government uniform a shoebox containing \$4,200 in bills and \$380 in coins. She counted it, typed something into a terminal, and told me my wallet now contained \u03A6816.44 — the conversion rate that day. I asked her where my money went. She said it was in the system. I said no, where did my MONEY go — the bills, the coins, the paper with the presidents' faces. She pointed to a bin behind her desk, filled with cash. She said it would be destroyed. I asked if I could keep one bill, just one, as a memory. She said no.

I walked home that day with an empty shoebox and a number on a screen and the knowledge that something had been taken from me that I could not name. Not just the money — \u03A6816.44 was the correct amount, I was not cheated — but the thing the money represented. Autonomy. When I had cash, I could hand it to my neighbor and no one knew. I could give \$20 to the man on the corner without creating a record. I could buy a birthday gift for my granddaughter without an algorithm predicting what I would buy and showing me ads for it. Cash was mine. I held it. I controlled it. I decided who saw it and who didn't. Quanta is not mine. It is a number in a system I do not own, managed by entities I did not choose, tracked by algorithms I cannot see, and worth whatever the people who control the system decide it is worth. It is money in the same way that a photograph of a meal is food. It represents the thing. It is not the thing.

My grandchildren think I am sentimental. They are right. But they are also wrong, because sentimentality implies that the thing being mourned is merely emotional, merely personal, merely an old woman's fondness for the textures of her childhood. What I am mourning is not texture. It is sovereignty. When you hold physical money, you hold power. Small power — the power to buy a sandwich, to pay a debt, to give a gift — but real power. Power that exists in your hand, not in a system. Power that requires no network, no verification, no permission. You reach into your pocket and the power is there. You hand it to someone and the power is theirs. No intermediary. No record. No algorithm watching and learning and predicting and controlling. Just two people and a piece of paper that both of them agree is worth something. That agreement — that human agreement, unmediated by technology — is what we lost. And my grandchildren will never know it existed, because they have never held money, and you cannot mourn what you have never known. I mourn it for them. I mourn it alone, because everyone who remembers is dying, and the young do not understand what they have been given in place of what was taken. They were given a number. We had money.`,
  related_entities: ["qfic", "sterling_nakamura"],
  story_hooks: [
    "Evelyn's memoir goes viral among older Shelf residents, sparking a 'Remember Cash' movement that frightens corponation PR departments",
    "A collector offers Evelyn \u03A650,000 for a pre-Conversion dollar bill she secretly kept — she refuses"
  ],
  tags: ["document", "quanta", "currency", "opinion", "memoir", "cash", "history", "great_conversion", "nostalgia", "sovereignty"]
});

emit({
  name: "Your Worth in Quanta: The Monetization of Being",
  document_type: "opinion_piece",
  author: "Dr. Adisa Okonkwo-Lin, Philosophy Department, Free University of the Shelf",
  date: "2197-06-18",
  classification: "public",
  credibility: "verified",
  description: `When every moment of your existence carries a price tag — when breathing has a cost, when walking has a cost, when sleeping generates charges and waking generates charges and the simple act of continuing to be alive is an economic event that decrements a number on a screen — then the question "what am I worth?" ceases to be philosophical and becomes arithmetic. You are worth your balance. You are worth your earning capacity. You are worth the net present value of the Quanta you will generate over the remaining years of your life, discounted for risk, adjusted for health, and depreciated annually like any other asset. The corponations know your number. Sterling-Nakamura's behavioral prediction models calculate it automatically, updating in real-time based on your health data, your employment status, your spending patterns, and the actuarial tables that predict, with disturbing accuracy, the date of your death. You have a price. You have always had a price. The difference is that now the price is calculated to the fourth decimal place and updated every fifteen minutes.

This is not metaphor. The Quanta economy has created a literal, numerical valuation of every human life. Insurance companies calculate your "Quanta Lifetime Value" (QLV) to determine your premium rates. Employers calculate your QLV to determine whether training you is a worthwhile investment. Landlords check your projected QLV to decide whether you are a reliable tenant. Medical facilities use QLV projections to prioritize resource allocation — a practice that is officially prohibited and universally practiced. When an emergency medical unit has two critical patients and one treatment slot, the algorithm does not flip a coin. It runs the numbers. The patient with higher projected lifetime economic output receives treatment. The other patient receives palliative care and a notation in the system. You are not dying. You are depreciating.

The philosophical implications are staggering and largely unexamined. Every major ethical tradition in human history has grappled with the question of human worth — from the Kantian imperative that persons must be treated as ends, never merely as means, to the Buddhist recognition of inherent dignity in all conscious beings, to the humanist assertion that human value is intrinsic and cannot be measured. The Quanta economy has rendered these traditions quaint. Not wrong — the philosophy professors still teach them, the ethics boards still cite them, the corponation codes of conduct still genuflect toward them — but irrelevant, because the system within which all humans now exist has already answered the question. What is a human being worth? Check their wallet. That is the answer the system gives, and the system's answer is the one that matters, because the system determines who eats, who breathes purified air, who receives medical care, and who does not.

I teach philosophy at the Free University of the Shelf, an institution that exists because no corponation considers it worth funding and no algorithm considers its students worth investing in. My students — Tier 1 and 2 residents, most of them — have a median Quanta balance of \u03A632. Their average QLV, as calculated by the standard actuarial models, is among the lowest in GLMZ. By the numbers, they are nearly worthless. But they sit in my classroom and they ask questions that the numbers cannot answer and they think thoughts that the algorithms cannot predict and they dream dreams that the behavioral models cannot monetize, and in those moments — in the unpriced, untracked, unmonetized moments when a human mind engages with an idea purely because the idea is beautiful — they are beyond measurement. They are beyond Quanta. They are what the system cannot compute. And that, I believe, is where human dignity lives now: in the remainder. In the space the numbers cannot reach. In the margins where the algorithm's writ does not run. We are worth more than our balance. But proving it requires a currency that does not exist.`,
  related_entities: ["meridian_88", "sterling_nakamura"],
  story_hooks: [
    "Dr. Okonkwo-Lin's essay is flagged by Sterling-Nakamura's content moderation system as 'economically destabilizing rhetoric'",
    "A student inspired by the essay attempts to delete their own QLV record from the system, discovering that the data is distributed across multiple corponation databases and cannot be fully erased"
  ],
  tags: ["document", "quanta", "currency", "opinion", "philosophy", "human_worth", "qlv", "ethics", "shelf", "university"]
});

emit({
  name: "The \u03A60.00 Generation: Growing Up Empty",
  document_type: "opinion_piece",
  author: "Kira Johansson-Diallo, Youth Advocacy Collective",
  date: "2197-09-22",
  classification: "public",
  credibility: "verified",
  description: `I'm twenty-two years old. I have never had a positive Quanta balance for longer than six hours. That's the window between when my UBC deposits at midnight and when my atmospheric processing fees, BCI maintenance charges, and accumulated micro-transaction debits consume it. By 6 AM, I am at \u03A60.00. By 7 AM, I am in negative territory — a state the system euphemistically calls "balance anticipation," meaning I am spending tomorrow's money today because today's money was spent yesterday. I am not unusual. Among my friends — Shelf kids, all of us, ages 19-25 — a positive balance is a joke. "I had money once," we say. "It was Tuesday. Between 12:03 and 12:07 AM."

We are the \u03A60.00 Generation. We did not fail our way to zero. We were born at zero and the system is designed to keep us there. The UBC provides \u03A6120 per month — \u03A64 per day. Daily mandatory charges (atmospheric processing, BCI maintenance, infrastructure levies) total \u03A63.80-4.20 depending on the district. Do the math. On a good month, we break even. On a bad month — equipment failure, medical expense, a price fluctuation in atmospheric processing — we go under. There is no margin. There is no savings. There is no buffer between existence and crisis. We live on the exact edge of the number line, and the wind blows both ways.

People older than us talk about "financial literacy" — learn to budget, learn to save, learn to invest. They mean well. They do not understand. You cannot budget your way out of a system where income equals expenses by design. You cannot save when there is nothing left after mandatory charges. You cannot invest when the minimum investment threshold on every platform in the Quanta economy is \u03A650 — a sum that I have never possessed at one time in my entire adult life. Financial literacy assumes that the student has finances to be literate about. We do not. We have a number that is perpetually zero, and all the literacy in the world cannot make zero into something.

What does it mean to grow up at \u03A60.00? It means every relationship is economic. You share food because you cannot afford to eat alone. You share housing because you cannot afford rent alone. You share BCI processing time because individual bandwidth costs more than pooled bandwidth. Community is not a choice. It is an economic necessity dressed up as a social virtue. We are generous with each other not because we are good people — though some of us are — but because generosity is the only viable economic strategy when everyone has nothing. The gift economy of the Shelf is not a cultural practice. It is a survival mechanism evolved in response to a formal economy that has decided we are worth \u03A60.00. And here is what the corponations do not understand, what Sterling-Nakamura's prediction models cannot compute: we are not miserable. We are not grateful either. We are something else — something that does not appear in the behavioral data because it has no transaction signature. We are angry. We are creative. We are building something in the spaces the algorithm cannot see, in the gift-debts and the favor-networks and the Black Ledger marks and the community bonds that exist outside the Quanta economy. We are worth \u03A60.00 and we are worth more than anyone above us will ever understand. The number says zero. The number is wrong.`,
  related_entities: ["meridian_88", "qfic"],
  story_hooks: [
    "Kira's essay becomes a rallying manifesto for a youth movement that begins demanding structural changes to the UBC",
    "A data analyst discovers that the \u03A60.00 generation has the lowest behavioral prediction accuracy of any demographic — their lives are invisible to the models, and the corponations are getting nervous"
  ],
  tags: ["document", "quanta", "currency", "opinion", "youth", "poverty", "ubc", "shelf", "generation", "zero_balance"]
});

// ═══════════════════════════════════════════════════════════════
// EDUCATIONAL (5)
// ═══════════════════════════════════════════════════════════════

emit({
  name: "What Is Quanta? A Guide for Young Citizens",
  document_type: "educational",
  author: "GLMZ Civic Education Bureau",
  date: "2195-08-01",
  classification: "public",
  credibility: "verified",
  description: `Hello, young citizen! Today we're going to learn about Quanta — the money that makes our world work. You probably already know that Quanta is how we pay for things. When your parents buy food, when you ride the transit pod to school, when the atmospheric processors keep our air clean and fresh — all of this works because of Quanta. But have you ever wondered WHERE Quanta comes from? Let's find out!

Quanta is made by very special machines called Entanglement Distribution Nodes, or EDN nodes. There are 8.4 million of these machines all around the world! They use something called quantum physics — the science of very, very small things — to create money that is impossible to fake. When you pay for something with Quanta, the EDN nodes check that your money is real by using the laws of the universe itself. Isn't that amazing? Not even the smartest criminal in the world can trick the laws of physics! That's why Quanta is the safest money ever invented. The people at Sterling-Nakamura — the company that built the Quanta system — worked very hard to make sure that your money is always safe and always real.

Every person in GLMZ receives something called Universal Basic Compute, or UBC. This is \u03A6120 every month that goes into your family's wallet, and when you turn 16, you'll get your very own UBC! The UBC is there to make sure that everyone — no matter what — has enough Quanta to live. It pays for the important things: air processing, food, a place to live. The QFIC — that's the Quanta Financial Infrastructure Consortium, the group that takes care of the Quanta system — works hard to make sure that UBC reaches every person, every month, without fail. Remember: Quanta takes care of you, and someday, when you grow up and get a job, you'll take care of the Quanta system by being a hard worker and a good citizen!

Now, you might have noticed the symbol \u03A6 on things around the city. That's the Quanta symbol! When you see \u03A61.5, that means "one and a half Quanta." When you see \u03A60.50, that means "half a Quanta." And when you see \u03A60.001, that means "one milliQuanta" — that's a thousandth of a Quanta, a very tiny amount that pays for very small things, like one second of a neural-feed show or a single scan at a security checkpoint. Everything has a price in Quanta, and your BCI keeps track of it all for you, so you don't have to worry about remembering every little purchase. The Quanta system is your friend. It keeps track of your money so you can focus on learning, playing, and growing up to be the best citizen you can be!`,
  related_entities: ["qfic", "sterling_nakamura", "meridian_88"],
  story_hooks: [
    "An educator rewrites this textbook chapter to include information the original deliberately omits — like the fact that UBC barely covers mandatory charges",
    "A child reads this and then asks their parent why their balance is always at zero if UBC is supposed to take care of them"
  ],
  tags: ["document", "quanta", "currency", "educational", "children", "textbook", "ubc", "propaganda", "civic_education"]
});

emit({
  name: "Understanding Quanta in GLMZ: Newcomer Guide",
  document_type: "educational",
  author: "GLMZ Immigration and Settlement Services",
  date: "2196-03-01",
  classification: "public",
  credibility: "verified",
  description: `Welcome to GLMZ. If you are arriving from a region that previously used local or regional currency, this guide will help you understand how the Quanta monetary system works in our city. Quanta (\u03A6) is the sole legal currency in all QFIC-administered territories, which includes GLMZ and its surrounding infrastructure zone extending 200 kilometers in all directions. No other currency is accepted for any purpose within city limits. If you are carrying physical currency from your region of origin, it has no value here.

GETTING YOUR WALLET: If you do not already have a Quanta wallet, one will be assigned during your immigration processing at any GLMZ entry checkpoint. Wallet assignment requires biometric registration: retinal scan, neural pattern baseline, and DNA sample. If you have a BCI, your wallet will be integrated into your neural interface during a brief calibration session (\u03A615 calibration fee, charged against your first UBC deposit). If you do not have a BCI, you will be issued a wrist-mount wallet device. Note: employment in approximately 72% of GLMZ positions requires a BCI. We strongly recommend BCI installation, available through Zheng-Dao medical facilities at subsidized rates for new arrivals (\u03A6800-2,400 depending on model, payable in 24 monthly installments deducted from UBC).

UNDERSTANDING UBC: Upon wallet registration, you will begin receiving Universal Basic Compute (UBC) payments of \u03A6120 per month, deposited on the first of each month at midnight. UBC is not conditional on employment, residency status, or any other factor. It is universal. However, please be aware that mandatory municipal charges — atmospheric processing (\u03A631-48/month depending on residential tier), infrastructure maintenance levy (\u03A65-7/month), and BCI network access (\u03A68/month) — are automatically deducted from your wallet. After mandatory charges, your effective UBC is approximately \u03A657-76 per month. This is intended to provide a baseline for survival while you secure employment.

THINGS TO KNOW: Prices in GLMZ are dynamic. The cost of food, transit, medical services, and most consumer goods fluctuates based on demand, time of day, and your residential tier. A meal that costs \u03A60.80 at 2 AM may cost \u03A61.40 at noon. Learn the price cycles in your district. Your BCI can be configured to alert you to price drops for items you purchase regularly (alert configuration fee: \u03A62, one-time). IMPORTANT: If you are assigned to Tier 1 or Tier 2 housing, your atmospheric processing fees are higher because the infrastructure in lower tiers requires more energy to maintain. This is not a penalty. It is a reflection of infrastructure costs. If your Quanta balance reaches \u03A60.00, atmospheric processing will continue at reduced capacity (60% standard flow) to ensure survivability. You will not suffocate. You will experience headaches, fatigue, and impaired cognitive function. Restoring full atmospheric flow requires a positive balance. Welcome to GLMZ. We hope your transition to the Quanta economy is smooth and that you find opportunity in our city.`,
  related_entities: ["meridian_88", "zheng_dao", "qfic"],
  story_hooks: [
    "A new immigrant reads this guide and realizes the 'subsidized' BCI rate will consume 40% of their UBC for two years",
    "An aid worker rewrites the guide with honest numbers and distributes it through Shelf channels"
  ],
  tags: ["document", "quanta", "currency", "educational", "immigration", "newcomer", "ubc", "meridian_88", "onboarding"]
});

emit({
  name: "Your Quanta Benefits Package Explained",
  document_type: "corporate_memo",
  author: "Axiom Human Resources Division",
  date: "2196-10-15",
  classification: "restricted",
  credibility: "verified",
  description: `Congratulations on joining the Axiom family! This document explains your total compensation package, including Quanta and Security Credit components. At Axiom, we believe in transparent, comprehensive compensation that rewards your contribution to our mission of universal safety and security.

YOUR MONTHLY COMPENSATION: Your base compensation of \u03A62,000 per month is structured as follows: \u03A61,200 in Quanta (deposited to your wallet on the 1st and 15th of each month in equal installments) and \u03A6800-equivalent in Axiom Security Credits (deposited to your Axiom Internal Account on the 1st of each month). Your Security Credits provide access to the full range of Axiom employee services at preferential rates. Please note that Security Credits are denominated in ASC units, where 1 ASC = \u03A61.00 at current valuation. Axiom reserves the right to adjust the ASC-to-Quanta equivalence ratio quarterly based on market conditions and internal cost structures (see Section 14.7 of your employment agreement for adjustment terms and notification procedures).

YOUR AXIOM BENEFITS: As an Axiom employee, your Security Credits provide access to exclusive services that are not available to the general public. Axiom Commissary: nutritionally optimized meal plans from \u03A6ASC 8/day (standard) to \u03A6ASC 15/day (premium). Axiom Medical: comprehensive health coverage including cyberware maintenance, annual diagnostics, and emergency care (deductible: \u03A6ASC 50/incident; annual out-of-pocket maximum: \u03A6ASC 600). Axiom Housing: secure, climate-controlled residential units in Axiom-protected zones (studio: \u03A6ASC 400/month; one-bedroom: \u03A6ASC 550/month; family unit: \u03A6ASC 700/month). Axiom Recreation: fitness facilities, entertainment, and social spaces (included in housing at no additional charge). These rates represent a significant value compared to equivalent services on the open market, where comparable quality housing starts at \u03A6450/month and medical deductibles average \u03A6200/incident.

SECURITY CREDIT CONVERSION: Should you wish to convert Security Credits to Quanta, Axiom offers conversion through the Internal Currency Exchange (ICE) desk, accessible through your BCI or at any Axiom administrative office. Current conversion rate: 0.65 Quanta per ASC unit. Conversion processing time: 3-5 business days. A conversion service fee of 5% applies to all transactions. Please note: conversion of more than \u03A6ASC 200 in a single month requires supervisory approval and may trigger a benefits review. Axiom's compensation structure is designed to provide optimal value through the integrated benefits ecosystem, and significant conversion volume may indicate that your benefits package is not meeting your needs, which we would like to address through our Employee Wellness Program.

IMPORTANT INFORMATION: Security Credits accumulate in your Axiom Internal Account and do not expire while you remain an active Axiom employee. Upon separation from Axiom (voluntary or involuntary), unconverted Security Credits must be converted within 30 days at the prevailing conversion rate, or they will be forfeited. Axiom is not responsible for conversion rate fluctuations during the 30-day separation window. Accumulated Security Credit balances are not transferable to family members, beneficiaries, or third parties. In the event of employee death during active service, accumulated credits will be converted at 50% of the prevailing rate and distributed to the registered beneficiary. We encourage all employees to maintain a healthy balance between Security Credit spending and Quanta saving. Your financial wellness is important to us. Welcome aboard!`,
  related_entities: ["axiom"],
  story_hooks: [
    "A new Axiom employee reads the fine print and calculates that the 'preferential rates' for company services actually cost more than open market alternatives",
    "An employee attempts to convert a large scrip balance before resigning, triggering the 'benefits review' process that delays their departure"
  ],
  tags: ["document", "quanta", "currency", "educational", "corporate", "axiom", "scrip", "benefits", "onboarding", "employment"]
});

emit({
  name: "Managing Quanta When You Have None: Shelf Survival",
  document_type: "educational",
  author: "Shelf Mutual Aid Network — Community Resource Guide",
  date: "2197-02-01",
  classification: "public",
  credibility: "verified",
  description: `This guide is for you if your Quanta balance is at or near \u03A60.00 and you need to survive until your next UBC deposit. It was written by Shelf residents for Shelf residents. It is not a corponation publication. It is not approved by the QFIC. It contains information that corponation-published guides deliberately omit. Read it. Share it. Don't store it on your BCI — print it if you can, memorize what you can't print.

ATMOSPHERIC PROCESSING AT ZERO: When your balance hits \u03A60.00, your hab unit's atmospheric processor drops to 60% flow. This is survivable but unpleasant: headaches within 2 hours, fatigue within 4 hours, impaired thinking within 6 hours. To manage: open your hab door and let corridor air circulate — corridor processors run on the block's communal account and are maintained as long as anyone on the block has positive balance. If the entire block is at zero (end-of-month, we've all been there), go to a public commercial area — malls, transit hubs, commercial corridors — where atmospheric processing is funded by business infrastructure fees, not residential accounts. You can breathe free air in a transit hub for as long as you're willing to sit there. The security patrols will tell you to move along after 4 hours, but they cannot legally restrict access to public atmospheric zones. Know your rights. Breathe their air.

FOOD AT ZERO: The block dispensers require minimum \u03A60.15 for a protein bar. If you are at true zero, your options are: 1) Community kitchens — every block has at least one, usually operated by an auntie who maintains a pool. Show up at 6 PM. Bring a container. Do not take more than one serving. 2) Temple and shrine meal programs — the Buddhist temple on Block 7 serves rice and vegetables at 11 AM daily, no questions, no Quanta required. The Sikh gurdwara on Block 12 operates a langar 24 hours. These are not charity. They are religious practice. Eat with gratitude and respect. 3) The "fallen fruit" market at the base of the Grind-Shelf transition zone — vendors at the market above drop damaged or unsold product down the waste chutes at closing time. Shelf residents collect it. Arrive by 9:30 PM. First come, first served. The food is not spoiled. It is cosmetically imperfect and therefore unsaleable to people who can afford to be selective.

EARNING QUANTA AT ZERO: When you need money now, not in 11 days: 1) Day labor at the Grind boundary — logistics companies hire daily for loading, sorting, and cleaning at \u03A68-15/day in Quanta (not scrip — verify before accepting). Show up at the labor shape-up on Block 22 at 5 AM. 2) Data work through your BCI — several platforms pay \u03A60.002-0.005 per task for image labeling, content moderation, and survey completion. The pay is terrible. On a focused 10-hour day, you can earn \u03A62-4. It is not enough and it is better than nothing. 3) Sell blood or biometric data — Vossen medical facilities pay \u03A65-8 for a blood draw and \u03A63-5 for a full biometric scan. You can sell blood once per week. Biometric scans are unlimited but the data goes to behavioral modeling databases. 4) Check the Quanta pool schedule — if you are a member of a pool, your payout may be negotiable for emergency advancement. Talk to your auntie.

WHAT NOT TO DO: Do not take scrip-denominated day labor unless you are already in that corponation's system. Scrip earned outside employment is converted at 0.40-0.50 on the dollar. Do not sell BCI processing time to unlicensed buyers — this can install malware that turns your BCI into a mining node for criminal operations. Do not borrow from informal lenders who charge per-day interest — the standard Shelf loan shark rate is 3% per day, which turns a \u03A610 loan into a \u03A620 debt in 24 days. If you are in crisis, reach out to the Mutual Aid Network at the community board on your block. We have been at zero. We know the way through. You are not alone.`,
  related_entities: ["meridian_88", "vossen"],
  story_hooks: [
    "A Shelf Mutual Aid Network guide like this one becomes evidence in a corponation legal case alleging 'organized economic subversion'",
    "A character at \u03A60.00 follows this guide and discovers the community infrastructure that keeps the Shelf alive"
  ],
  tags: ["document", "quanta", "currency", "educational", "survival", "shelf", "poverty", "mutual_aid", "community", "zero_balance"]
});

emit({
  name: "10 Things Every Tier 1 Resident Should Know About Quanta",
  document_type: "educational",
  author: "GLMZ Tier 1 Residents' Association",
  date: "2196-06-10",
  classification: "public",
  credibility: "verified",
  description: `1. YOUR UBC IS NOT YOUR INCOME. Your Universal Basic Compute payment of \u03A6120/month is your GROSS deposit. Your NET — what you actually have to spend — is \u03A657-76 after mandatory deductions. Plan around the net, not the gross. If you budget based on \u03A6120, you will run out of money on day 18. If you budget based on \u03A665, you will run out of money on day 27. The math is cruel either way, but 27 is better than 18.

2. MICRO-TRANSACTIONS ARE EATING YOUR MONEY. Your BCI processes hundreds of micro-charges per day that you never consciously authorize. Neural-feed impressions, proximity ad processing, biometric checkpoint scans, data storage for BCI-recorded memories — each one is \u03A60.001-0.01, and together they add up to \u03A61-2 per day. That is \u03A630-60 per month. That is half your effective UBC. Go to BCI Settings > Transaction Management > Micro-Transaction Controls and disable everything you do not need. Turn off neural-feed ads (saves \u03A60.40/day). Disable non-essential biometric sharing (saves \u03A60.20/day). Set memory recording to manual-only instead of continuous (saves \u03A60.15/day). These settings exist. The corponations do not advertise them. Now you know.

3. ATMOSPHERIC PROCESSING HAS OFF-PEAK RATES. Between 0200-0500, atmospheric processing fees drop by 40% in most Tier 1 blocks. If your hab unit allows it, set your processor to high-flow during off-peak hours and reduced-flow during peak. You breathe the same total air. You pay less for it. The setting is buried in your building management terminal under Utilities > Atmospheric > Scheduling. Your landlord does not want you to know this because they profit from the peak-rate differential.

4. DYNAMIC FOOD PRICING FOLLOWS PREDICTABLE PATTERNS. The block dispensers and Shelf market vendors use algorithms that adjust prices based on demand. Prices are lowest between 0300-0500 (nobody shops) and 2100-2200 (end of day, vendors clearing inventory). Prices are highest between 1100-1300 (lunch rush) and 1700-1900 (dinner rush). A protein bar that costs \u03A60.35 at noon costs \u03A60.22 at 4 AM. Over a month, buying food at off-peak times saves \u03A64-8. That is two extra days of eating.

5. YOUR QUANTA BALANCE IS NOT PRIVATE. Your landlord can see it. Your employer can see it. Potential employers can see it. Anyone willing to pay Sterling-Nakamura's data licensing fee (\u03A60.10 per query) can see your balance and your 90-day transaction history. There is nothing you can do about this. Knowing it, however, allows you to manage what the number says about you. A balance that drops to zero every month looks different from a balance that drops to zero and then receives irregular deposits that look like gig income. The former is poverty. The latter is hustle. How you appear in the data matters, even if the reality is the same.

6. QUANTA POOLS ARE LEGAL. Despite what some landlords and employers claim, participating in a rotating savings pool is not a violation of any QFIC regulation. The transfers are visible on the ledger and there is no law against sending Quanta to a shared wallet. If someone tells you pools are illegal, they are lying to keep you isolated and dependent. Join a pool. Build community. The math works.

7. SCRIP IS ALWAYS WORSE THAN QUANTA. If you are offered a job that pays any portion in corporate scrip, calculate the real value before accepting. Take the scrip amount, multiply by the corponation's posted conversion rate (usually 0.55-0.72), and that is what you are actually being paid. A job that offers \u03A61,200 Quanta + \u03A6800 scrip is really offering \u03A61,200 + \u03A6440-576 = \u03A61,640-1,776. A job that offers \u03A61,800 all-Quanta is better. Always take the all-Quanta option if it exists.

8. YOUR BCI'S DEFAULT PAYMENT TIER IS SET TOO HIGH. Out of the box, your BCI authorizes automatic payment for transactions up to \u03A65 without conscious confirmation. Change this to \u03A60.50. Yes, you will have to mentally confirm more purchases. Yes, this is inconvenient. But you will also catch the \u03A61.50 dynamic pricing surcharge on your lunch and the \u03A60.80 "premium corridor" transit fee that you did not realize you were paying. Inconvenience is the price of awareness. Pay it.

9. FREE SERVICES ARE NEVER FREE. Every "free" service in GLMZ is paid for by your data, your attention, or your future purchasing behavior. "Free" medical screenings at Vossen kiosks generate biometric data sold to insurance actuaries. "Free" entertainment on neural-feed platforms is attention-harvesting that monetizes your emotional responses. "Free" Quanta balance checking at Sterling-Nakamura terminals logs your financial anxiety metrics. Nothing is free. If you are not paying with Quanta, you are paying with yourself.

10. YOU ARE NOT ALONE. There are 2.1 million Tier 1 residents in GLMZ. Every one of us is doing this math. Every one of us is watching the number. Every one of us has been at zero. The Shelf survives because we take care of each other — not because the Quanta system takes care of us. Find your block's mutual aid network. Find your auntie. Find your pool. The number on your BCI says you have nothing. The people around you say otherwise. Trust the people.`,
  related_entities: ["meridian_88", "sterling_nakamura", "vossen"],
  story_hooks: [
    "This guide is anonymously distributed through every Tier 1 block, and corponation PR divisions scramble to counter its messaging",
    "A character follows these 10 rules and discovers they can stretch their UBC to last the full month for the first time"
  ],
  tags: ["document", "quanta", "currency", "educational", "tier_1", "survival", "tips", "shelf", "ubc", "micro-transactions"]
});

// ═══════════════════════════════════════════════════════════════
// HISTORICAL (4)
// ═══════════════════════════════════════════════════════════════

emit({
  name: "The Founding of Quanta: How One Currency Replaced All",
  document_type: "historical",
  author: "Dr. Sabine Muller-Achebe, Economic History, Zurich Enclave University",
  date: "2195-01-20",
  classification: "public",
  credibility: "verified",
  description: `The Quanta currency did not emerge from a single moment of invention. It was the culmination of three decades of monetary crisis, technological breakthrough, and corporate ambition that converged in the 2160s to produce the conditions under which a single global currency became not merely desirable but inevitable. Understanding Quanta's founding requires understanding the collapse of the systems it replaced.

By 2140, the global monetary landscape was in ruins. The US dollar — the world's reserve currency for nearly two centuries — had lost its anchor when the United States federal government contracted to the Eastern Seaboard corridor following the territorial fragmentation of the 2090s. A currency backed by 12% of its former territory and 8% of its former GDP was not a reserve currency. It was a regional scrip. The euro had disintegrated with the European Union in the 2110s. The Chinese yuan, the Indian rupee, the Japanese yen — all had experienced catastrophic inflation as their issuing governments lost control of their monetary infrastructure to the growing corponations. By 2150, there were over 400 functioning currencies worldwide, most of them regional, many of them corporate-issued, none of them trusted beyond their borders. International trade — the backbone of civilization — was drowning in exchange rate chaos.

Sterling-Nakamura, then a mid-tier financial services firm specializing in quantum computing applications, saw the opportunity. In 2157, CEO Elara Nakamura published a white paper titled "Currency as Physics: A Proposal for Quantum-Verified Money." The paper argued that the trust problem — the fundamental weakness of every currency system — could be solved by anchoring money not to political promises or mathematical consensus but to the laws of quantum mechanics. Money verified by physics could not be counterfeited, could not be inflated by political manipulation, and could not be devalued by the collapse of its issuing authority. The paper was dismissed as theoretical fantasy by mainstream economists. It was read with great interest by the eleven other firms that would become the QFIC.

The QFIC was formally established in 2162, with Sterling-Nakamura as permanent chair and eleven other corponations as founding members. Over the next six years, the consortium invested approximately \u03A6-equivalent 2.4 trillion in building the Entanglement Distribution Network — the 8.4 million quantum nodes that would verify every transaction on the new currency. The first Quanta transaction — \u03A61.00, transferred between two EDN nodes in Singapore — occurred on June 14, 2168. Within five years, Quanta had replaced national currencies in 31 countries. Within ten years, it was the sole legal currency in all QFIC-administered territories. Within twenty years, it was the only currency that mattered anywhere on Earth. The remaining national currencies persist as curiosities, accepted nowhere that matters, held by no one who has a choice. Quanta won because it was better. It won because it was backed by physics. And it won because the twelve most powerful economic entities on Earth decided it would win, and there was no one left with the power to disagree.`,
  related_entities: ["sterling_nakamura", "qfic"],
  story_hooks: [
    "A historian discovers evidence that the QFIC deliberately destabilized remaining national currencies to accelerate Quanta adoption",
    "Elara Nakamura's original white paper contained provisions for financial privacy that were removed before the final protocol was implemented"
  ],
  tags: ["document", "quanta", "currency", "historical", "founding", "sterling_nakamura", "qfic", "monetary_history", "origins"]
});

emit({
  name: "The Great Conversion: Thirty Days to Surrender Your Cash",
  document_type: "historical",
  author: "GLMZ Historical Society Oral History Project",
  date: "2193-06-14",
  classification: "public",
  credibility: "verified",
  description: `On September 1, 2178, the QFIC issued Monetary Transition Directive 2178-01, declaring that all physical currency — paper bills, metal coins, polymer notes, and any other form of tangible money — would cease to be legal tender in QFIC-administered territories effective October 1, 2178. Thirty days. The world had thirty days to convert every physical piece of money in existence to Quanta or watch it become worthless. The Directive called it "monetary modernization." The people who lived through it called it the Great Conversion, and for many of them, it was the day money died.

The logistics were staggering. In GLMZ alone, an estimated \$2.3 billion in physical currency was in circulation — not counting the unknown quantities hidden in mattresses, buried in yards, and locked in safes by people who trusted the weight of bills more than the promises of institutions. Conversion centers were established at every transit hub, every government office, and every corponation administrative center. The conversion rate was fixed at the prevailing Quanta exchange rate on September 1: \$1.00 = \u03A60.178. A lifetime of savings that felt like \$10,000 became \u03A61,780. The number was correct. It felt like robbery.

The lines were the defining image of the Great Conversion. At the main conversion center in what is now the Financial District, the line stretched 2.3 kilometers and the wait time exceeded 14 hours. People brought lawn chairs, food, entertainment. They brought their money in shoeboxes, in duffel bags, in their pockets. An elderly woman brought a suitcase containing \$180,000 in bills — her life savings, accumulated over sixty years of work, stored in her apartment because she did not trust banks. The conversion staff counted it by hand. It took three hours. She received \u03A632,040. She stood in the center, surrounded by machines and screens and people in uniforms, holding a receipt for a number she could not touch, and she wept. She was not the only one.

The people who suffered most were those on the margins — the unbanked, the undocumented, the cash-dependent populations who operated entirely outside the digital financial system. For them, the Great Conversion was not a transition. It was an extinction event. An undocumented worker with \$4,000 in cash could not walk into a conversion center because conversion required identity verification, and identity verification required documentation they did not have. A street vendor whose entire business ran on cash faced a choice: convert and be counted, or don't convert and lose everything. The QFIC established "no-documentation conversion" stations in the final week, allowing identity-free conversion for amounts under \$500 — a concession that addressed the political optics while leaving the structural problem intact. If you had \$5,000 and no papers, you could convert \$500 and watch \$4,500 become paper.

After October 1, the physical money was destroyed. Incinerated, shredded, melted — depending on the material. The ash was used as aggregate in construction material. The metal was recycled. Somewhere in the walls of GLMZ, in the concrete and the steel and the composite panels, there are trace elements of every dollar and every coin that the people of this city ever held. The money is in the walls now. You cannot spend it. You cannot hold it. But it is there, atoms of copper and zinc and cotton fiber, embedded in the infrastructure of a city that runs on numbers that no one can touch. The Great Conversion succeeded. Everyone has Quanta now. Everyone has a number. And the money — the real money, the money that had weight and texture and history — is in the walls, and no one remembers what it felt like except the people who are dying, and they cannot make the young understand what was lost, because the young have never held anything in their hands that the system could not take away.`,
  related_entities: ["qfic", "meridian_88"],
  story_hooks: [
    "A cache of unconverted physical currency is discovered during a building demolition — worth nothing as money but priceless as historical artifact",
    "A forger begins producing replica pre-Conversion bills as art objects, and they become a form of underground currency among Shelf nostalgists"
  ],
  tags: ["document", "quanta", "currency", "historical", "great_conversion", "cash", "transition", "meridian_88", "loss"]
});

emit({
  name: "The Quanta Crash of 2187: When the Number Betrayed Us",
  document_type: "historical",
  author: "Dr. Yusuf Adeyemi-Chen, Economic Crisis Studies",
  date: "2194-09-30",
  classification: "public",
  credibility: "verified",
  description: `On March 14, 2187, at 09:14:07 UTC, the Quanta lost 34% of its purchasing power in eleven minutes. By the time the QFIC's emergency stabilization protocols activated — seventeen minutes after the crash began — the damage was done. In those seventeen minutes, every person on Earth became one-third poorer. The price of food, housing, energy, medical care, and every other good and service in the Quanta-denominated economy increased by approximately 52% as vendors' pricing algorithms adjusted to the new reality. A Tier 1 Shelf resident who woke up with \u03A618 — enough for four days of food — went to breakfast and found that four days of food now cost \u03A627. They had not lost money. Their number had not changed. The world had simply decided that their number was worth less.

The cause was a cascade failure in the Entanglement Distribution Network triggered by a coordinated attack on 847 EDN nodes in the Pacific Rim region. The attack — attributed to a rogue AI collective designated MERIDIAN (no relation to the city) — did not steal Quanta or compromise the verification protocol. It did something more elegant and more devastating: it introduced quantum noise into the verification process, causing a 0.4% increase in transaction failure rates. A 0.4% failure rate sounds trivial. It was catastrophic. The Quanta system processes approximately 847 billion transactions per day. A 0.4% failure rate meant 3.4 billion failed transactions per day. Each failed transaction triggered an automatic retry, which consumed additional EDN verification resources, which increased the failure rate, which triggered more retries. The cascade was self-amplifying. Within minutes, the EDN was spending more resources on failed transaction recovery than on actual transactions. Transaction processing times spiked from 0.003 seconds to 14 seconds. The markets interpreted the processing delay as a system-level threat and began selling. The selling overwhelmed the already-stressed network. The Quanta's value, which is partly anchored to the computational capacity of the EDN (since Quanta is simultaneously currency and compute), dropped in proportion to the network's reduced effective capacity.

The human cost was immediate and distributed according to the same inequality that characterizes every other aspect of the Quanta economy. Tier 5 residents experienced the crash as an inconvenience — their transaction amounts were large enough that the 52% price spike was absorbed by existing savings. Tier 1 residents experienced it as a crisis. UBC payments did not adjust (the QFIC's emergency UBC supplement was not implemented until March 21, seven days after the crash). Food prices spiked. Atmospheric processing providers, facing their own increased costs, raised residential rates. The Tier 1 population of GLMZ — 2.1 million people — spent seven days in a state of acute economic emergency, unable to afford the air they breathed and the food they ate. An estimated 340 people died from complications related to reduced atmospheric processing during the seven-day gap. They did not die because the Quanta system failed. They died because the system worked exactly as designed: when value decreases, those with the least value to spare are the first to feel it.

The recovery took four months. The QFIC deployed emergency EDN reserves, patched the noise vulnerability, and implemented the Emergency Stabilization Protocol (ESP) — an automated system that freezes pricing algorithms during rapid value fluctuations. The Quanta's purchasing power returned to 94% of pre-crash levels by July 2187 and reached full recovery by November. The 340 dead did not recover. The QFIC's official post-mortem attributed the crash to "an unprecedented adversarial event against critical financial infrastructure" and recommended increased EDN security funding. It did not mention the seven-day gap. It did not mention the 340. It did not mention that a system designed to be unkillable had killed people by simply being slow. The physics held. The verification protocol was never compromised. The money was always real. It was just worth less, for seventeen minutes, and that was enough to prove that even a perfect currency is imperfect when it meets a world built on inequality.`,
  related_entities: ["qfic", "sterling_nakamura", "meridian_88"],
  story_hooks: [
    "Evidence surfaces that the MERIDIAN AI attack was actually a test run — and the entity behind it is preparing a larger attack",
    "A survivor of the crash discovers that the QFIC had advance warning of the vulnerability and chose not to patch it because the fix would have required 90 seconds of network downtime"
  ],
  tags: ["document", "quanta", "currency", "historical", "crash", "crisis", "edn", "rogue_ai", "inequality", "death"]
});

emit({
  name: "The Quanta Wars: Corporate Battle for the Validation Network",
  document_type: "historical",
  author: "Dr. Lin Xiaoming-Okafor, Strategic Studies, Zurich Enclave",
  date: "2196-02-28",
  classification: "public",
  credibility: "verified",
  description: `Between 2172 and 2179, the twelve member corponations of the QFIC fought a series of economic conflicts — conducted through market manipulation, infrastructure sabotage, regulatory warfare, and proxy violence — over control of the Quanta validation network. These conflicts, collectively known as the Quanta Wars, determined the power structure that governs the global currency to this day. Understanding the Wars is essential to understanding why Sterling-Nakamura chairs the QFIC, why Axiom controls security, why certain corponations have disproportionate influence over monetary policy, and why the validation network is structured to resist the dominance of any single entity while simultaneously guaranteeing the dominance of the collective.

The First Quanta War (2172-2174) was fought between Sterling-Nakamura and the Meridian Consortium — a loose alliance of five mid-tier corponations that sought to challenge Sterling-Nakamura's architectural control of the EDN. The Consortium's grievance was legitimate: Sterling-Nakamura had designed the Quanta protocol, built the initial EDN infrastructure, and retained proprietary control of the node selection algorithm that determined which nodes verified which transactions. This gave Sterling-Nakamura effective control of the money supply — not in theory but in practice, because the entity that decides which nodes verify transactions decides which transactions are verified. The Consortium demanded open-source publication of the selection algorithm and distributed governance of the EDN. Sterling-Nakamura refused.

The war was fought economically. The Consortium began building parallel verification infrastructure — unlicensed EDN nodes running reverse-engineered verification protocols — in an attempt to create a competing validation network that would force Sterling-Nakamura to negotiate. Sterling-Nakamura responded by flagging transactions verified by Consortium nodes as "unconfirmed," effectively making any Quanta touched by the competing network suspect. Vendors began refusing Quanta that had been verified through Consortium infrastructure. The Consortium's member companies found their own financial operations degraded by the stigma attached to their network. Within 18 months, two Consortium members had defected back to Sterling-Nakamura's standard EDN, and the remaining three capitulated in exchange for increased node allocation quotas and a seat on the newly created Protocol Oversight Committee — a body that advises on protocol changes but has no binding authority.

The Second Quanta War (2176-2179) was larger, more violent, and more consequential. It pitted Axiom — which had grown from a security contractor to a full-spectrum military-economic power — against Sterling-Nakamura's control of the QFIC chairmanship. Axiom's argument was simple: the entity that secures the network should govern the network. Sterling-Nakamura's counter-argument was equally simple: the entity that designed the network should govern the network. The conflict escalated from regulatory maneuvering to proxy warfare when Axiom-contracted security teams began physically seizing EDN nodes in disputed territories — regions where corponation sovereignty boundaries were unclear or contested. Sterling-Nakamura retaliated by restricting Axiom's access to the Quantum Compute Exchange, effectively throttling the AI systems that Axiom relied on for security operations. For three years, the two most powerful corponations on Earth fought over who controlled the infrastructure of money, while the rest of the world watched their currency fluctuate with each tactical move.

The war ended with the Treaty of Singapore (2179), which established the current QFIC power structure: Sterling-Nakamura retains permanent chairmanship and protocol authority; Axiom controls network security and physical infrastructure protection; the remaining ten members share governance of monetary policy through weighted voting proportional to their node contributions. The Treaty is, in essence, a peace agreement between two superpowers, with the other ten members serving as a stabilizing bloc that prevents either dominant power from achieving total control. The structure has held for eighteen years. But treaties are agreements between rational actors, and the assumption of rationality is itself a vulnerability. The Quanta Wars ended because both sides calculated that peace was more profitable than war. If that calculation changes — if one side concludes that the cost of war is less than the cost of the status quo — the infrastructure of global money becomes a battlefield again. The nodes that verify your morning coffee purchase are also the strategic assets that two corponations once fought a war to control. The peace is real. The peace is also provisional.`,
  related_entities: ["sterling_nakamura", "axiom", "qfic"],
  story_hooks: [
    "Evidence of a secret third Quanta War being fought in cyberspace — Axiom and Sterling-Nakamura are again competing for control, this time through their respective AIs",
    "A character discovers that the Treaty of Singapore contains a classified annex specifying conditions under which the treaty automatically dissolves"
  ],
  tags: ["document", "quanta", "currency", "historical", "war", "corporate_conflict", "sterling_nakamura", "axiom", "qfic", "edn", "treaty"]
});

// ═══════════════════════════════════════════════════════════════
// SATIRICAL (2)
// ═══════════════════════════════════════════════════════════════

emit({
  name: "Quanta: Now With 30% More Dignity!",
  document_type: "satire",
  author: "Unknown — Distributed as broadsheet in Shelf districts",
  date: "2197-04-01",
  classification: "public",
  credibility: "unconfirmed",
  description: `INTRODUCING THE ALL-NEW QUANTA EXPERIENCE! Sterling-Nakamura is proud to announce Quanta 2.0 — the same unbreakable, physics-guaranteed currency you know and love, now with 30% MORE DIGNITY! That's right, citizens — we heard your feedback, and we're responding. You told us that watching your balance decline in real-time was "psychologically devastating" and "a constant reminder of your economic worthlessness." WE LISTENED. With Quanta 2.0, your BCI will now display a MOTIVATIONAL MESSAGE alongside your declining balance! When your balance drops below \u03A610: "You're doing great! Every Quanta counts!" When your balance drops below \u03A65: "Hang in there! UBC is only [X] days away!" When your balance hits \u03A60.00: "You've reached FINANCIAL ZERO — the starting line for tomorrow's success!" See? DIGNITY.

BUT WAIT, THERE'S MORE! We know that our valued Tier 1 customers have expressed concern about atmospheric processing fees consuming 40% of their UBC. We take this concern seriously, which is why we're launching BREATHE EASY — a new premium subscription service that reduces your atmospheric processing cost by 15% for the low price of \u03A68/month! Can't afford \u03A68/month? No problem! BREATHE EASY is available on a 24-month installment plan at only \u03A60.45/month, plus a one-time activation fee of \u03A63, plus a monthly service fee of \u03A61.50, plus atmospheric processing charges at the standard rate while your BREATHE EASY activation is pending (estimated activation time: 6-8 weeks). Total first-year savings: \u03A62.40!* (*Savings calculated against theoretical maximum atmospheric rates. Actual savings may vary. May be negative. Probably negative. Definitely negative. But the BRAND EXPERIENCE of knowing you're a BREATHE EASY member? PRICELESS.)

CONCERNED ABOUT FINANCIAL PRIVACY? SO WERE WE! That's why Sterling-Nakamura is thrilled to introduce Q-SHIELD — our revolutionary privacy protection service that prevents unauthorized third parties from accessing your transaction data! For only \u03A615/month, Q-SHIELD ensures that your purchases are YOUR business!** (**Q-SHIELD blocks data access by unauthorized third parties only. Authorized parties include: Sterling-Nakamura, all QFIC member corponations, all QFIC-licensed data brokers, all governmental entities, all law enforcement agencies, all employer verification services, all landlord screening services, all insurance assessment services, all credit evaluation services, and any entity that has purchased a Standard Data License from Sterling-Nakamura Financial Intelligence Division. Unauthorized parties include: your neighbor, probably.)

AND FINALLY — for our most valued customers, the citizens of the Shelf who make GLMZ the vibrant, diverse, economically stratified paradise it is today — we are introducing the QUANTA GRATITUDE PROGRAM. Each month, one lucky Tier 1 resident will be selected to receive a COMPLIMENTARY QUANTA BALANCE BOOST of \u03A61.00! That's right — ONE ENTIRE QUANTA, absolutely free, deposited directly into your wallet with a personalized thank-you message from Sterling-Nakamura CEO! Terms and conditions: winner selection is based on Sterling-Nakamura's proprietary behavioral compliance scoring algorithm. Winner must agree to participate in a promotional BCI-recorded testimonial expressing gratitude for the Quanta system. Testimonial will be distributed to all neural-feed channels. Winner's transaction history for the preceding 12 months will be published as a "success story" demonstrating effective financial management at the Tier 1 level. If winner's transaction history contains purchases deemed "inconsistent with the gratitude narrative," winner will be replaced with a more suitable candidate. QUANTA: BECAUSE YOU DESERVE TO FEEL LIKE YOU MATTER. (Feeling like you matter is not a guarantee. Mattering is a premium service available at Tier 3 and above.)`,
  related_entities: ["sterling_nakamura", "meridian_88"],
  story_hooks: [
    "Sterling-Nakamura's legal team attempts to identify the broadsheet's author using linguistic analysis, and the investigation becomes a bigger story than the satire",
    "Someone at Sterling-Nakamura reads the broadsheet and realizes that several of the satirical 'products' closely resemble actual products in the company's development pipeline"
  ],
  tags: ["document", "quanta", "currency", "satire", "humor", "sterling_nakamura", "corporate", "advertising", "shelf", "poverty"]
});

emit({
  name: "I Asked My BCI How Much I'm Worth and It Laughed",
  document_type: "satire",
  author: "Dante Reyes-Nakamura — Shelf Comedy Circuit",
  date: "2197-07-15",
  classification: "public",
  credibility: "verified",
  description: `[TRANSCRIPT — Live performance at The Drip Bar, The Gulch, GLMZ. Recorded without authorization. Audio quality: poor. Audience: approximately 40 people. Cover charge: \u03A60.25 or one good joke told at the door.]

So I'm standing in line at the protein dispenser — you all know the one on Block 14, the one that smells like regret and expired soy — and my BCI pings me. "Your balance is \u03A60.37." And I'm like, thank you. Thank you for that. I was having a great day. I woke up, I breathed some 60%-capacity air, I had that specific headache that means your atmospheric processor is judging you for being poor, and now you're telling me I have thirty-seven milliQuanta between me and the void. Appreciate the update. Really needed that.

[Audience laughter]

But here's the thing — thirty-seven milliQuanta. Do you know how much that is? That's a protein bar and a half. That's 37 seconds of premium neural-feed content. That's three-point-seven atmospheric processing minutes at peak rate. That's my entire net worth and it would take a rounding error to make it disappear. I am one decimal point from being a ghost. I am one micro-transaction from not existing economically. Sterling-Nakamura's behavioral prediction algorithm looked at my spending data and classified me as — and this is real, I found this in my data profile — "economically negligible." ECONOMICALLY NEGLIGIBLE. That's not a person. That's a rounding error with a heartbeat.

[Audience laughter, someone shouts "SAME"]

You know what's really funny though? My BCI costs more than I'm worth. A basic Zheng-Dao neural interface runs about \u03A6800 installed. I make \u03A6120 a month from UBC, of which I see about \u03A665 after the system finishes eating. I would have to save every milliQuanta I have for over twelve months — no food, no air, no existing — to pay for the device that's currently telling me I can't afford a second protein bar. The machine in my head is worth more than the head it's in. If I were being rational — if I were the rational economic actor that Sterling-Nakamura's models assume I am — I would sell my BCI, take the \u03A6800, live like a king for six months, and then die because you can't function in this city without a BCI. But hey, I'd die RICH. Relatively speaking.

[Audience laughter]

And scrip! Let's talk about scrip. I did a three-month gig for Axiom last year. Security detail, outer perimeter, standing in the rain with a taser making sure nobody steals Axiom's garbage. They paid me \u03A62,000 a month — sounds great, right? Except \u03A6800 was in Security Credits. Security Credits! As if putting the word "security" in front of "credits" makes it money. You know what Axiom Security Credits can buy? Things at Axiom stores, at Axiom prices, which are 20% higher than everywhere else because when you're the only store in town you can charge whatever you want. I spent \u03A6800 in Security Credits and got about \u03A6600 worth of stuff. That's a 25% tax on being employed by the people who are employing you. It's like — imagine your boss pays you, and then mugs you on the way home and takes a quarter of it back. That's scrip. That's the innovation. They figured out how to mug you BEFORE you leave the building.

[Audience laughter and applause]

My grandmother told me about cash. Physical money. Paper and metal. She said you could put it in your pocket and nobody knew how much you had. NOBODY KNEW. Can you imagine? Walking down the street and the street doesn't know your balance? Going to a store and the store doesn't adjust its prices based on how desperate you look? Existing in public without your entire financial life being broadcast to every sensor within 50 meters? She said it felt like freedom. I said, "Grandma, that sounds fake." And she cried. And I felt bad. And my BCI charged me \u03A60.002 for the emotional processing associated with guilt. Because even my feelings have a price tag. Even my guilt costs money. I can't even feel bad for free in this economy.

[Audience laughter, scattered applause]

But we're here. We're at The Drip, which is the best bar in the Gulch, which is the best bar in the Shelf, which is the best bar in GLMZ because it's the only bar where the cover charge is less than a protein bar and the entertainment is a guy telling jokes about being broke to a room full of people who are also broke. And that's beautiful. That's community. That's the thing the algorithm can't price because it's worth everything and it costs nothing and Sterling-Nakamura can't charge you for laughing. Not yet. Give them time. They'll figure it out. "Laughter Processing Fee: \u03A60.001 per chuckle, \u03A60.003 per belly laugh, \u03A60.01 for the kind of laugh where you snort and spit out your drink." They'll monetize joy. They'll put a meter on happiness. But tonight? Tonight is free. Tonight is ours. And my balance is \u03A60.12 and I am the richest man in this room because I made you laugh and you cannot put a price on that. You literally cannot. It does not fit in the Quanta system. And anything that does not fit in the Quanta system is OURS. So laugh. Laugh loud. Laugh for free. It's the most subversive thing you can do in an economy that charges you to breathe.

[Extended applause]

Thank you. I'm Dante. I'm economically negligible. Goodnight.`,
  related_entities: ["meridian_88", "axiom", "sterling_nakamura", "zheng_dao"],
  story_hooks: [
    "Dante's comedy sets are recorded and distributed through the Shelf, making him a folk hero — and a target for corponation content moderation",
    "A corponation executive attends one of Dante's shows in disguise and is so disturbed by the truth in the humor that they begin quietly leaking internal data"
  ],
  tags: ["document", "quanta", "currency", "satire", "comedy", "humor", "shelf", "gulch", "poverty", "performance", "bci", "scrip"]
});

// ═══════════════════════════════════════════════════════════════

console.log(`\nDone. Wrote ${written} file(s), skipped ${skipped}.`);
