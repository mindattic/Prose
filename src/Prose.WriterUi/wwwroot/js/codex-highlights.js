// Story-bible names highlighted in the Writer's editor (RFC 0019, RA-T2.2.2.2).
//
// Uses the CSS Custom Highlight API: ranges are registered with CSS.highlights and painted by
// ::highlight(prose-codex). The editor's DOM and the beat's markup are never touched, so a save can
// never pick up a highlight as a tag. A click on a highlighted name asks the shell to open the entry.
(function () {
    'use strict';
    const NAME = 'prose-codex';
    let names = [];          // [{ name, entityId }]
    let rx = null;
    let shell = null;
    let timer = 0;
    let observer = null;
    let ranges = [];         // [{ range, entityId }]

    const supported = () => typeof CSS !== 'undefined' && CSS.highlights && typeof Highlight !== 'undefined';
    const escape = s => s.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');

    function editor() { return document.querySelector('article.prose[contenteditable="true"]'); }

    function rebuild() {
        timer = 0;
        ranges = [];
        if (!supported()) return;
        CSS.highlights.delete(NAME);
        const el = editor();
        if (!el || !rx) return;
        const walker = document.createTreeWalker(el, NodeFilter.SHOW_TEXT, {
            // Already-tagged entities (span.ent) have their own styling; never double them.
            acceptNode: n => n.parentElement && n.parentElement.closest('.ent') ? NodeFilter.FILTER_REJECT : NodeFilter.FILTER_ACCEPT,
        });
        const hl = new Highlight();
        for (let node = walker.nextNode(); node; node = walker.nextNode()) {
            rx.lastIndex = 0;
            let m;
            while ((m = rx.exec(node.data)) !== null) {
                const r = new Range();
                r.setStart(node, m.index);
                r.setEnd(node, m.index + m[0].length);
                hl.add(r);
                const hit = names.find(n => n.bare === m[0]);
                ranges.push({ range: r, entityId: hit ? hit.entityId : null });
            }
        }
        CSS.highlights.set(NAME, hl);
    }

    function schedule() {
        if (timer) clearTimeout(timer);
        timer = setTimeout(rebuild, 300);
    }

    function rangeAt(x, y) {
        let node = null, offset = 0;
        if (document.caretPositionFromPoint) {
            const p = document.caretPositionFromPoint(x, y);
            if (p) { node = p.offsetNode; offset = p.offset; }
        } else if (document.caretRangeFromPoint) {
            const r = document.caretRangeFromPoint(x, y);
            if (r) { node = r.startContainer; offset = r.startOffset; }
        }
        if (!node) return null;
        return ranges.find(h => h.range.startContainer === node && offset >= h.range.startOffset && offset <= h.range.endOffset) || null;
    }

    document.addEventListener('click', e => {
        if (!shell || !ranges.length || !e.target.closest || !e.target.closest('article.prose')) return;
        const hit = rangeAt(e.clientX, e.clientY);
        if (hit && hit.entityId) shell.invokeMethodAsync('OpenCodexEntry', hit.entityId).catch(() => { });
    });
    document.addEventListener('input', e => { if (e.target.closest && e.target.closest('article.prose')) schedule(); }, true);

    window.proseCodex = {
        supported,
        set(list, shellRef) {
            shell = shellRef;
            names = (list || [])
                .map(n => ({ entityId: n.entityId, bare: n.name.replace(/^the\s+/i, '') }))
                .filter(n => n.bare.length >= 2);
            names.sort((a, b) => b.bare.length - a.bare.length);
            rx = names.length ? new RegExp('(?<![\\p{L}])(' + names.map(n => escape(n.bare)).join('|') + ')(?![\\p{L}])', 'gu') : null;
            if (!observer) {
                // A new beat replaces the editor's content; repaint whenever it does.
                observer = new MutationObserver(schedule);
                observer.observe(document.body, { childList: true, subtree: true, characterData: false });
            }
            schedule();
            return supported();
        },
        count() { return ranges.length; },
    };
})();
