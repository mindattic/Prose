"""Insert the Chen-offers-noodles-for-life / Kyle-must-pay scene into With Teeth.
Locks the canon link: With Teeth's wired-jaw client wife = the noodle-stall Mrs. Chen / Chen Wei-Lin."""
import sys, json, datetime
sys.stdout.reconfigure(encoding='utf-8')

teeth_path = 'engine/data/stories/019d6143ab61752da68e0bc71595cd6c/story.json'
with open(teeth_path, encoding='utf-8-sig') as f:
    t = json.load(f)
html = t['html']

# Locate the seam: paragraph A ends with "...whether those two things are connected." then the Lotus coda begins with a duplicate "He walked."
seam_marker = "He walked like a man who has not yet decided whether those two things are connected."
seam_idx = html.find(seam_marker)
if seam_idx < 0:
    print('WARN: seam anchor not found')
    sys.exit(1)

# Find where the actual Lotus coda content begins (skipping the duplicate "He walked." paragraph)
coda_anchor = "Someone had been watching the loading-dock work"
coda_idx = html.find(coda_anchor)
if coda_idx < 0:
    print('WARN: Lotus coda anchor not found')
    sys.exit(1)

arrival_scene = """He walked like a man who has not yet decided whether those two things are connected.

---

Chen's stall is two blocks south of the freight tier and one alley off the main, the warm orange glow of its hood lamp visible through the rain from a block away. He has eaten what comes out of her kitchen once - the smell that pushed through the camphor in the apartment six hours ago, fried oil and chili and pork bone - but he has not been to the stall itself. He walks toward it the way a man walks toward a thing he has earned without intending to earn it.

She is at the counter when he arrives. The apron is fresh; the hands are washed; the small scar at the base of her left thumb catches the lamp light at an angle Kyle catalogues without making the catalogue mean anything yet. The daughter is somewhere he cannot see. The husband is somewhere he cannot see either. The teeth, the array confirms by absence - no second delivery to make - have already arrived where they were going. The smell of the stall is what the apartment smelled like, transposed into a place where the smell is the work and not the wound.

She does not greet him. She sets a bowl down on the counter - pork bone broth, the long-simmered kind, chili oil already added the way she will remember he likes it for the next four years. He sits on the stool. The stool is the right height for him. She has not adjusted it; he has not adjusted it; the geometry simply is what it is the first time, and the first time is the way it will be from now on.

He eats. The broth is hot. He eats the whole bowl and does not speak. She does not refill it. She does not move. When he sets the chopsticks down she is still standing exactly where she was when she set the bowl, and she is looking at him the way she had looked at the chips she had pressed into his hand at half rate six hours ago - she has more she wants to give and no language for the giving, and she is now finding the language.

Mandarin first, low. *Xie xie.* The two syllables doing the work of an entire ledger. Then, more deliberately, in the tongue she has chosen to run this stall in, the one she gives the customers because the customers do not all know hers: *Free. Always. For you. Forever.* Her hands are flat on the counter beside the bowl. Her thumb with the scar is the closest thing to him on the wood. She has just offered him the only thing she owns.

The NeoCortex catalogues her offer with the precision it catalogues exits. *She means it. She is not negotiating. She does not understand that she is offering you a thing you cannot accept.*

Kyle reaches into his coat and sets a credit chip on the counter beside her hand. Standard rate. The kind of standard rate a man pays when he wants the transaction to be a transaction and not the beginning of an obligation he has spent his life refusing to accumulate.

*I pay,* he says. His voice is level. He has practiced level for a long time. *Every bowl. Standard rate. Every time.*

She does not pick up the chip. He does not pick up the bowl. The standoff lasts perhaps four seconds and is the most important four seconds of their relationship, and they both know it without naming it.

She tries again. Slower this time, the way you try again with a man you have decided not to insult by giving up on the first refusal. *You took the contract at half. You did the work whole. The bowl is nothing. Forever is nothing.*

*Forever is everything,* the NeoCortex flags, in the catalogue. *Forever is the largest thing she has, and she is offering it because she does not yet know how large it is.*

Kyle does not move the chip. He looks at her the way the discipline allows him to look at a woman who has just tried to give him something he cannot, in good conscience, accept. The blade is the discipline. The bowl is the discipline. The credit chip is the discipline. He does not say that aloud. He says, aloud, what he can: *If I do not pay, I do not eat. If I do not eat here, I do not eat. I would rather pay you than not see you again. The chip stays.*

The four seconds become five. Then her hand closes over the chip the way her daughter's hand had closed over the teeth - not refusing the gift, accepting that the gift is the form the man in front of her needs the gift to take. She bows. Not the convulsive bow of the apartment six hours ago. A smaller bow. The bow of a vendor accepting a customer's price. The kind of bow she will give him every night, four hundred and seventeen nights a year by the count he will eventually run, for the next four years and longer.

He stands. He bows back, smaller still, the bow of a man who has just successfully refused a kindness larger than he can carry. *Tomorrow,* he says. *Same bowl. Same price.*

*Same price,* she says. The agreement is the contract. The contract is the bond.

He turns and walks out of the stall. The rain has not stopped. It will not stop tonight.

---

"""

# Reconstruct: everything before the seam + arrival_scene + Lotus coda from coda_idx onward
new_html = html[:seam_idx] + arrival_scene + html[coda_idx:]

print(f'Old length: {len(html)}')
print(f'New length: {len(new_html)}')
print(f'Delta: +{len(new_html) - len(html)} chars')

t['html'] = new_html
t['modified'] = datetime.datetime.utcnow().isoformat() + 'Z'
with open(teeth_path, 'w', encoding='utf-8') as f:
    json.dump(t, f, indent=2, ensure_ascii=False)
print('With Teeth: Chen-offer/Kyle-refuses scene inserted')

# Verification
with open(teeth_path, encoding='utf-8-sig') as f:
    t = json.load(f)
html = t['html']
print()
checks = [
    ("Chen's stall arrival", "Chen's stall is two blocks"),
    ("Small scar at base of left thumb", "scar at the base of her left thumb"),
    ("Mandarin xie xie", "Xie xie"),
    ("Free. Always. For you. Forever.", "Free. Always. For you. Forever."),
    ('I pay refusal', "I pay"),
    ('Every bowl. Standard rate.', "Every bowl. Standard rate"),
    ('Forever is everything', "Forever is everything"),
    ('Bond / contract', "The contract is the bond"),
    ('Tomorrow. Same bowl. Same price.', "Same price"),
    ('Lotus surveillance still present', "south-arm cell captain named Mira"),
    ('Closing line preserved', "noodles would be cold by now"),
]
for label, needle in checks:
    print(f'  [{"+" if needle in html else "-"}] {label}')
