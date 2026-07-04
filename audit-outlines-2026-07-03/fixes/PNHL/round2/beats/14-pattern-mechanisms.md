---
action: PATCH
target-beat-id: B77C73A1-939D-4A84-AEBD-51F55D4DFEFE
BeatTitle: Pattern
SortKey: 1050
---

## Synopsis

Three incidents over two weeks, each grounded in one named mechanism instead of vague "fault
architecture": a relay's internal routing table poisoned with dead branches, a handoff auto-frozen
by a spoofed safety-flag through the job platform's escrow dispute process, and a shipment
redirected via a manifest she didn't share. The third incident is trimmed to end on the discovery
of the redirect address rather than treating it as a closed loss — she decides to go there in
person, which is the hinge into the new Beat 15.

## Text

The relay came first.

A client hired her to diagnose an intermittent dropout on a commercial line — a two-hour job, by
the fault description. She got three hops in and found someone had already been inside the same
relay, recently, and had gone after the thing under the thing: the relay's internal fault-routing
table, the map that told a diagnostic query which branch to check first when a fault fired. Someone
had quietly re-weighted it, seeding three dead branches ahead of the two real ones, so any query
that followed the table's own logic — which every automated tool did, because that was the point of
having a table — chased empty conduit for an hour before it ever reached the actual fault. She
recognized the hand behind it the way she'd recognize a thermostat rewired to read a healthy
temperature off a dead sensor: not incompetence. Design, aimed at the exact tool that would go
looking.

It took her six hours, through the night, working past the point where her eyes started reading
code as texture instead of meaning, because she'd had to rebuild the routing table from clean
copies before she could even see the real fault underneath it. She fixed it. She billed for the two
hours she'd quoted, because the client hadn't asked for six and she wasn't going to charge someone
for a mess that wasn't theirs to pay for. She ate the other four hours herself. It was the first
money she'd lost in GLMZ that she hadn't lost to her own mistake, and there was nothing to point to
— no signature, no message, nothing but a table that had taken exactly the kind of care someone
with real access would put into it.

The second one cost her more than money.

A handoff, twenty minutes out, client already in transit — and then a cancellation, terse, no
explanation beyond *change of plans.* She traced it, because she traced everything she touched, and
found the platform's own escrow system had auto-frozen the handoff four minutes before the
cancellation came through, triggered by a safety-flag filed against her account from a credential
that had no legitimate reason to know the handoff existed. The flag didn't even need to be true. The
platform's dispute process was built to freeze first and review later, which meant a false flag and
a real one cost the client the same four minutes of doubt — and four minutes, it turned out, was
all it took for a client already halfway to the door to decide the doubt wasn't worth carrying.
She never learned who'd filed it. What she learned instead, a week later, was that the client
wouldn't take her calls anymore — not hostile, just gone, the specific silence of someone who'd
decided, on information she'd never get to see, that working with her was more trouble than it was
worth. She'd built that relationship over three jobs. It closed over one flag she couldn't even
prove was false.

She logged it: *contact lost, mechanism confirmed — spoofed safety-flag, escrow auto-freeze.
Cause traced. Effect permanent.* Her throat went tight around the last word before she made
herself finish typing it. There wasn't a version of that sentence that made it hurt less to write.

The third was smaller and somehow worse for being smaller: a component she'd ordered — a coupling
she needed for a job already half-scheduled — never arrived at her pickup point. She traced the
shipment through the carrier's manifest, a record she hadn't shared with anyone, and found it
delivered and signed for at an address three blocks from where she actually lived. Not lost.
Redirected, cleanly, by someone with access to a manifest that shouldn't have had a second reader.

She sat with the address a long moment before she closed the trace. She could log it the way
she'd logged the relay and the handoff — a line in the *Changes* file, a cost absorbed, a pattern
one entry thicker — and let it join the shape she was already certain of. Or she could walk three
blocks and find out who was standing on the other end of a redirect that specific.

She had the operation. She'd had it since the elevator shaft, if she was honest with herself — the
mark on that transfer had told her everything the pattern was now confirming in triplicate. She
didn't need a fourth incident to know what she already knew.

What she needed was to see one of them.

She closed the log, put her boots back on, and went to find the address.
