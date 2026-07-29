# New Testament Claims Campaign

A standing, multi-session campaign: work through **every verse of the New Testament**, in
canonical book order, cataloguing every claim it makes. Method and citation standard inherited
from [`../README.md`](../README.md) — this file only adds the campaign-tracking layer.

## Scope rule (both kinds of claim, neither skipped)

Every pericope gets a ledger entry. Two claim types:

- **Checkable** — a person, place, date, number, event, office, genealogy, or custom that
  external evidence could in principle confirm or contradict. Gets the full six-section
  treatment in a standalone `docs/gospel/<topic>.md` (or a shared doc covering a tight cluster
  of related verses, e.g. the nativity chronology below).
- **Theological/moral** — teaching content with no possible external check (a parable's moral,
  a beatitude, a prayer). **Noted explicitly as out-of-method, not silently omitted** — the
  ledger records that the verse was considered and why it has no Gap Table, rather than the
  verse simply not appearing anywhere.

Many pericopes are mixed (e.g., a healing narrative makes a checkable claim about time/place/
witnesses alongside a theological point about faith) — ledger entries note both where relevant.

## How to read a book ledger

Each book gets `docs/gospel/nt-claims/<book>.md`: a chapter-by-chapter, pericope-by-pericope
table with columns **Reference | Claim type | Status | Doc**. Status is one of:

- `Drafted` — full topic doc exists, linked in the Doc column
- `Noted (theological)` — considered, flagged as having no external check, no doc needed
- `Pending` — checkable claim identified, not yet drafted

## Book status

| # | Book | Chapters | Status |
|---|---|---|---|
| 1 | [Matthew](matthew.md) | 28 | Complete end-to-end — 28 chapters, Notes chapter (380 citations), Glossary (178 entries); depth-pass 2026-07-28 brought citation density to parity with Mark/Luke/John (~15.8 refs/chapter avg) |
| 2 | Mark | 16 | Complete end-to-end — 16 chapters, Notes chapter (185 citations), Glossary (113 entries), no known prose or citation gaps |
| 3 | Luke | 24 | Complete end-to-end — 24 chapters, Notes chapter (301 citations), Glossary (151 entries), no known prose or citation gaps |
| 4 | John | 21 | Complete end-to-end — 21 chapters, Notes chapter (355 citations), Glossary (154 entries), no known prose or citation gaps |
| 5 | Acts | 28 | Not started |
| 6 | Romans | 16 | Not started |
| 7 | 1 Corinthians | 16 | Not started |
| 8 | 2 Corinthians | 13 | Not started |
| 9 | Galatians | 6 | Not started |
| 10 | Ephesians | 6 | Not started |
| 11 | Philippians | 4 | Not started |
| 12 | Colossians | 4 | Not started |
| 13 | 1 Thessalonians | 5 | Not started |
| 14 | 2 Thessalonians | 3 | Not started |
| 15 | 1 Timothy | 6 | Not started |
| 16 | 2 Timothy | 4 | Not started |
| 17 | Titus | 3 | Not started |
| 18 | Philemon | 1 | Not started |
| 19 | Hebrews | 13 | Not started |
| 20 | James | 5 | Not started |
| 21 | 1 Peter | 5 | Not started |
| 22 | 2 Peter | 3 | Not started |
| 23 | 1 John | 5 | Not started |
| 24 | 2 John | 1 | Not started |
| 25 | 3 John | 1 | Not started |
| 26 | Jude | 1 | Not started |
| 27 | Revelation | 22 | Not started |

## Why order matters here

Canonical order was chosen over "richest evidence first" or "highest-profile claims first" —
see the decision recorded in [`../../../memory`](../../../) project memory for this campaign.
Practically: the Gospels (books 1–4) and Acts (book 5) carry the overwhelming majority of
checkable historical/geographical/biographical claims in the entire NT — genealogies, place
names, named officials, dated events, population/crowd numbers, named disciples and relatives.
The Epistles (books 6–26) are mostly theological argument and pastoral instruction addressed to
specific communities, with occasional checkable claims (named individuals in closing greetings,
named events like the Corinthian famine collection, autobiographical claims in Galatians and
2 Corinthians about Paul's own movements). Revelation is almost entirely apocalyptic-symbolic
and will need its own method note when reached, since most of its content is not truth-apt in
the checkable sense at all.
