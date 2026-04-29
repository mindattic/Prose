"""Extract Part III: Eleven Minutes from the archived Teeth pre-split file
into its own standalone book with a single chapter."""
import sys, json, os, secrets, datetime
sys.stdout.reconfigure(encoding='utf-8')

archive_path = 'engine/data/archives/teeth-full-original-20260428T193321-pre-split/parts_ii_through_vi.txt'
with open(archive_path, encoding='utf-8') as f:
    raw = f.read()

i3 = raw.find('## Part III')
i4 = raw.find('## Part IV')
part_iii = raw[i3:i4].strip()
print(f'Part III extracted: {len(part_iii)} chars')

book_id = secrets.token_hex(16)
chapter_id = secrets.token_hex(16)
print(f'New book ID: {book_id}')
print(f'New chapter ID: {chapter_id}')

now_iso = datetime.datetime.utcnow().isoformat() + 'Z'

# Build chapter html: replace "## Part III: Eleven Minutes" heading with chapter heading
body_after_heading = part_iii.split('\n', 1)[1].lstrip()
chapter_html = '# Eleven Minutes\n\n*Protagonist: Kyle Ellen Corbin-Vasik*\n\n' + body_after_heading

print(f'Chapter html length: {len(chapter_html)} chars')

chapter_dir = f'engine/data/stories/{chapter_id}'
os.makedirs(chapter_dir, exist_ok=True)

story = {
    "id": chapter_id,
    "book_id": book_id,
    "number": 1,
    "title": "Eleven Minutes",
    "synopsis": "Ten years ago, GLMZ Lower District. Kyle has just walked away from a contract he refused to complete - a man named Deshi Okafor with a daughter's photograph in his pocket. Walking home in the rain, a directed cardiac pulse takes him down. Two unhurried men who knew his name carry him to Carver's repair shop. He is unconscious for eleven minutes. Something is placed inside him. He does not ask what.",
    "characters": ["Kyle Ellen Corbin-Vasik", "Carver", "Deshi Okafor (offscreen)"],
    "status": "draft",
    "html": chapter_html,
    "beats": [],
    "created": now_iso,
    "modified": now_iso
}
with open(f'{chapter_dir}/story.json', 'w', encoding='utf-8') as f:
    json.dump(story, f, indent=2, ensure_ascii=False)

outline = {
    "title": "Eleven Minutes",
    "logline": "Ten years before the events of any other Kyle chapter, Kyle refuses a no-witnesses contract because of a daughter's photograph, and is taken down on his way home by two unhurried men who knew his name. He is unconscious for eleven minutes. Something is placed inside him. For ten years he does not ask what.",
    "theme": "The price of mercy. Kyle's refusal to kill Deshi Okafor produces the eleven-minute downtime. The chapter's argument: Kyle's discipline made him interesting to whoever orchestrated the eleven minutes. Being a moral person made him a target. The not-asking that follows is itself a moral choice, repeated daily for a decade.",
    "premise": "Existing prose preserved from an earlier chapter draft. Standalone book - relationship to other Kyle/Pixel material is undecided. Working canon kept intact for future use.",
    "characters": ["Kyle Ellen Corbin-Vasik", "Carver", "Deshi Okafor (offscreen)"],
    "acts": [],
    "character_arcs": [],
    "seeds_and_payoffs": []
}
with open(f'{chapter_dir}/outline.json', 'w', encoding='utf-8') as f:
    json.dump(outline, f, indent=2, ensure_ascii=False)

checkpoint = {
    "ProjectId": chapter_id,
    "Title": "Eleven Minutes",
    "Protagonist": "Kyle Ellen Corbin-Vasik",
    "Characters": story['characters'],
    "Premise": outline['premise'],
    "Location": "GLMZ Lower District (ten years before present)",
    "Outline": outline,
    "OutlineReview": None,
    "QualityReport": None,
    "CanonGrounding": None,
    "Beats": [],
    "FullText": chapter_html,
    "Complete": True,
    "FailureReason": None,
    "Created": now_iso,
    "LastModified": now_iso
}
with open(f'{chapter_dir}/checkpoint.json', 'w', encoding='utf-8') as f:
    json.dump(checkpoint, f, indent=2, ensure_ascii=False)
print(f'Chapter created at {chapter_dir}')

book = {
    "id": book_id,
    "series_id": None,
    "title": "Eleven Minutes",
    "premise": "Standalone single-chapter book held for future development. Working canon preserved from an earlier Bushido Coda draft. The chapter is the origin event of the harmonic in Kyle's chest and the undocumented modifications referenced in A Borrowed Hand - but the relationship between this book and Bushido Coda has not been decided. The book may eventually become a prelude, a separate novella, an inserted flashback chapter, or material for a different project. For now, it is preserved.",
    "arc_target": "TBD",
    "protagonists": ["Kyle Ellen Corbin-Vasik"],
    "cover_image_url": None,
    "tagline": "Ten years before. Eleven minutes. Two unhurried men who knew his name.",
    "chapter_ids": [chapter_id],
    "state_at_end": {
        "character_status": {
            "Kyle Ellen Corbin-Vasik": "Carries a harmonic device placed below his sternum during eleven minutes of unconsciousness. Will not ask what was placed there for ten years."
        },
        "open_threads": [
            "Who orchestrated the eleven-minute downtime - two unhurried men, almost clinical, who knew Kyle's name",
            "Carver's silence - what he knows and chose not to say",
            "Deshi Okafor and his daughter - referenced; never seen on the page",
            "The harmonic itself - placed without consent, learned the rhythm of Kyle's lungs anyway"
        ],
        "canon_changes": [],
        "in_world_time": "Approximately ten years before the events of Bushido Coda's present-day chapters"
    },
    "status": "preserved",
    "created": now_iso,
    "modified": now_iso
}
with open(f'engine/data/books/{book_id}.json', 'w', encoding='utf-8') as f:
    json.dump(book, f, indent=2, ensure_ascii=False)

book_outline = {
    "book_id": book_id,
    "premise": book['premise'],
    "arc_target": "TBD",
    "theme": "TBD",
    "structure": "single_chapter",
    "status": "Preserved",
    "chapters": [
        {
            "chapter_id": chapter_id,
            "number": 1,
            "title": "Eleven Minutes",
            "short_synopsis": story['synopsis'][:200],
            "long_synopsis": outline['logline'],
            "key_beats": [],
            "opens_threads": [
                "Who orchestrated the eleven-minute downtime",
                "Carver's silence",
                "The harmonic in Kyle's chest, placed without consent"
            ],
            "closes_threads": [],
            "state_changes": {
                "Kyle Ellen Corbin-Vasik": "Carries the harmonic. Has begun the ten-year not-asking."
            },
            "pov_character": "Kyle Ellen Corbin-Vasik"
        }
    ],
    "threads": [],
    "pending_adjustments": [],
    "modified": now_iso
}
with open(f'engine/data/books/{book_id}.outline.json', 'w', encoding='utf-8') as f:
    json.dump(book_outline, f, indent=2, ensure_ascii=False)

print()
print('=== STANDALONE BOOK CREATED ===')
print(f'Book ID:    {book_id}')
print(f'Chapter ID: {chapter_id}')
print(f'Title:      Eleven Minutes')
print(f'Status:     preserved (single-chapter, working canon)')
print(f'Location:   engine/data/books/{book_id}.json + .outline.json')
print(f'            engine/data/stories/{chapter_id}/')
print()
print('Bushido Coda is unchanged - still 7 chapters, still has The Rogue AI as placeholder at slot 3.')
