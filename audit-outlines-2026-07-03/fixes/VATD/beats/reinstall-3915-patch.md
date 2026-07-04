ACTION: RE-ENABLE (patched)
Target beat Id: 019EC6B2-6A27-7CCE-A27D-66D0528956C9
Target beat Number: 3915
NodeBeats.IsEnabled: 0 -> 1
NodeBeats.SortKey: 1625.0 -> 875.0 (insert between 4912 @825.0 and 3885 @900.0; leaves 3884 @850.0 in place before it)
BeatTitle: (none currently — leave NULL, matches sibling beats in Act V)

## Why RE-ENABLE instead of a new beat

3915 already contains the exact scene the bible calls for — the procedure at Daria Holme's
door, the twenty-six-minute operation, "You came back," and Levin's flat "Change of heart."
That prose is good and on-voice; writing a new beat from scratch would just be re-deriving it
worse. The only real problem is the *frame*: the original opening invents a standalone
"06:40 inventory query, excised twelve weeks prior" trigger that contradicts the live
decision beat (4912), where Tomas says plainly: "I pulled it this morning before we went to
the Ward and never moved it. It's still viable." Re-enabling verbatim would put two
contradictory origin stories for the same kidney back to back. Patching the frame is the
cheaper fix — everything after "Cold-pack case, standard issue" survives close to verbatim.

The original also closes on an "in the van" dialogue tail ("That's not in any playbook" /
"Carrion's going to flag it eventually" / "Worth it?" / "We'll see.") that duplicates ground
the live closing beat (3885) already covers better, with the photograph beat and the
"three on the queue" continuation. Keeping both would read as the same conversation twice in
a row. The patch below cuts 3915's tail so 3885's "In the Wagon, Levin drove, and for a while
neither of them said anything" picks up the baton clean, instead of restating it.

## Continuity notes carried forward from Fix decisions elsewhere

- Does not touch the Casimir/Ekow question — this beat has no Ekow content.
- Does not touch the "Three nights" line — that's beat 4859, patched separately.
- Preserves Lock #5 verbatim ("Change of heart" delivered as a flat clinical answer to a
  direct question, never explained further, never underlined).

## Patched text (replaces 3915's Text field in full)

They drove to the depot first — the cold-storage annex behind Carrion's main yard, five
aisles of numbered lockers holding everything on the company's books that wasn't attached to
a person yet. Tomas already knew which locker. He'd filed the form himself that morning,
before the Ward, before any of it: on the manifest, REALLOCATE TO NEW RECIPIENT sat lit and
waiting, and RETURN TO ORIGINAL ACCOUNT sat greyed out under a prompt — *Reason required.* He'd
typed one anyway. *Clerical error — original excision order issued against incorrect account.
Return authorized.* He hadn't sent it. He'd sat with it the rest of the day, the cursor
blinking on a decision he wasn't ready to make, and then the night had happened, and by the
time he was ready the form was still there, exactly where he'd left it.

He sent it now. The reply came back automated, indifferent, four seconds later: authorization
logged, no human in the loop, because nobody had ever built one for this particular kind of
mistake.

Cold-pack case, standard issue. Protein markers nominal. Carrion's storage protocols were the
best in the business — they had to be, given the inventory.

Levin drove. Tomas had the case on his lap.

The building was the same. The lobby tile was the same. The elevator took forty-five seconds,
same as before.

Daria Holme answered the door. She was wearing the same coat. She looked at the case, and then
at their jackets, and then at Tomas, and she didn't say anything. She stepped back from the
door.

The old man was in the bedroom. The dialysis machine was running beside the bed, the lines
clean and the cuff fitted. He'd learned to do it himself. He watched them come in the way he'd
watched them leave — without surprise, without accusation. He'd been a man who expected exactly
as much from the world as the world had given him.

Tomas set the case on the chair. He opened it.

The procedure took twenty-six minutes. Levin assisted. Neither of them spoke.

When they came out, Daria was at the kitchen table with the paperwork Tomas had set in front
of her. She was reading it with the careful attention of someone who had signed her name on
things she didn't fully understand before.

"His account is cleared," Tomas said. "Discrepancy in the original work order."

"What kind of discrepancy."

"Change of heart," Levin said.

She looked at the paper. She looked at Tomas. She looked at the hallway toward the bedroom.

"He'll need to rest for a few days," Tomas said. "Dialysis can come out in the morning.
There's care documentation in the packet."

She nodded.

He picked up the procedure case. Levin was already at the door.

"You came back," Daria said. She wasn't asking.

Tomas looked at her. He didn't say anything, because there wasn't anything useful to say, and
because *change of heart* had covered it about as well as anything was going to.

They went down the stairs. The lobby was the same. The street outside was cold and bright,
early light coming off the windows across the way, and the Wagon was waiting at the curb where
they'd left it, engine running, the larder empty and the whole rest of the shift still ahead of
them.
