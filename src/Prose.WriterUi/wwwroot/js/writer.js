// window.proseWriter (theme switching) moved to theme.js — /repo needs it too and should not have
// to load the whole editor for it. Writer.razor loads theme.js before this file.

// ── The prose editor ───────────────────────────────────────────────────────────────────────
//
// A contenteditable whose DOM is serialized back to the beat's own markup on every input. Entity
// tags render as contenteditable="false" spans, which is what makes them atomic: the caret steps
// over them, typing cannot land inside one, and changing a link is a deliberate act through the
// modal rather than a stray keystroke that silently repoints a guid.
//
// Every edit this module makes goes through document.execCommand where one exists, so it lands on
// the browser's own undo stack. A direct DOM mutation does not, and worse, it leaves that stack
// pointing at nodes that have moved — the next Ctrl+Z then undoes something other than what the
// author last did. Direct mutation is only the fallback when a command refuses.
window.proseEditor = (() => {
    let host = null;          // the contenteditable element
    let dotnet = null;        // DotNetObjectReference back into the Blazor component
    let contextTarget = null; // the .ent span the author last opened the menu on
    let lastRange = null;     // the last selection that was inside the editor
    let savedRange = null;    // the selection a link dialog was opened for

    const BLOCK_TAGS = new Set(['P', 'DIV']);

    // The four emphasis styles, by the tags a browser may emit for each, and the markers
    // ProseInline parses them back from.
    const EMPHASIS = { STRONG: 'bold', B: 'bold', EM: 'italic', I: 'italic', U: 'underline', S: 'strike', STRIKE: 'strike', DEL: 'strike' };
    const MARKERS = { bold: ['**', '**'], italic: ['*', '*'], underline: ['<u>', '</u>'], strike: ['~~', '~~'] };

    // Markdown mode's equivalent, keyed by execCommand name so one toolbar command drives both.
    const RAW_MARKERS = { bold: ['**', '**'], italic: ['*', '*'], underline: ['<u>', '</u>'], strikeThrough: ['~~', '~~'] };

    const FORMAT_COMMANDS = ['bold', 'italic', 'underline', 'strikeThrough'];

    const NO_STYLE = new Set();

    function call(method, ...args) {
        // Swallowed on purpose: a rejected promise here means the component was disposed (the
        // author moved to another beat) and there is nobody left to tell.
        if (dotnet) dotnet.invokeMethodAsync(method, ...args).catch(() => { });
    }

    function escapeAttr(s) {
        return String(s).replace(/&/g, '&amp;').replace(/"/g, '&quot;')
                        .replace(/</g, '&lt;').replace(/>/g, '&gt;');
    }

    function escapeText(s) {
        return String(s).replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
    }

    function isElement(node, tag) {
        return node && node.nodeType === Node.ELEMENT_NODE && (!tag || node.tagName === tag);
    }

    function isChip(node) {
        return isElement(node) && node.classList.contains('ent');
    }

    function isBlock(node) {
        return isElement(node) && BLOCK_TAGS.has(node.tagName) && !isChip(node);
    }

    // ── Serialization ──────────────────────────────────────────────────────

    // Serialize one node, wrapping it in whatever emphasis markers its tag calls for. Emphasis is
    // re-emitted as markers rather than preserved as HTML because the markers are what the
    // database, the exporters and every other reader understand.
    //
    // `active` is the set of styles already open around this node, so a <b> inside a <b> does not
    // emit a second "**" — that pair would CLOSE the bold ProseInline has open, not nest in it.
    function serializeNode(node, active) {
        if (node.nodeType === Node.TEXT_NODE) return node.nodeValue;
        if (node.nodeType !== Node.ELEMENT_NODE) return '';

        const tag = node.tagName;
        if (tag === 'BR') return '\n';

        if (isChip(node)) {
            const guid = node.getAttribute('data-guid') || '';
            const repo = node.getAttribute('data-repo') || '';
            // The inner text only — an entity tag wraps a plain surface name, never nested markup.
            const inner = node.textContent || '';
            if (!guid) return inner;
            return `<entity repo="${escapeAttr(repo)}" guid="${escapeAttr(guid)}">${inner}</entity>`;
        }

        const style = EMPHASIS[tag];
        const opens = style && !active.has(style);
        const inside = opens ? new Set(active).add(style) : active;

        let inner = '';
        for (const child of node.childNodes) inner += serializeNode(child, inside);

        // Whitespace-only content must not gain markers, or an empty <em> left behind by the
        // browser after a deletion becomes a stray "**" in the prose.
        if (!opens || inner.trim() === '') return inner;

        // Whitespace goes OUTSIDE the markers. Double-clicking a word on Windows selects its
        // trailing space too, so bolding it gave "**word **" — bold whitespace nobody can see,
        // and markup nobody would have typed.
        const [, lead, body, trail] = /^(\s*)([\s\S]*?)(\s*)$/.exec(inner);
        const [open, close] = MARKERS[style];
        return lead + open + body + close + trail;
    }

    // Paragraphs, from a container's children. A run of consecutive inline siblings is ONE
    // paragraph: after select-all-and-delete Chromium types straight into the host with no <p>,
    // and the old per-node rule turned "Hello <b>world</b>" into two paragraphs on the next save.
    // A block that itself holds blocks (a paste, or Enter inside a pasted <div>) is walked into
    // rather than flattened into one line.
    function collectParagraphs(container, parts) {
        let run = '';
        const flush = () => { parts.push(run); run = ''; };

        for (const child of container.childNodes) {
            if (isBlock(child)) {
                flush();
                if (Array.from(child.children).some(isBlock)) collectParagraphs(child, parts);
                else parts.push(serializeNode(child, NO_STYLE));
            } else if (isElement(child) && !isChip(child) && child.querySelector('p, div')) {
                // An inline element wrapped around paragraphs — a <b> around a pasted document, or a
                // restructure Chromium made. Serialized as a run, its paragraphs ran together into
                // one; walked into, each keeps its own (its emphasis does not survive the wrap).
                flush();
                collectParagraphs(child, parts);
            } else {
                run += serializeNode(child, NO_STYLE);
            }
        }
        flush();
    }

    function serialize(container) {
        const parts = [];
        collectParagraphs(container, parts);
        // contenteditable emits U+00A0 for a typed run of spaces and at some edit boundaries.
        // Left alone it enters the prose invisibly: the text stops matching what the exporters,
        // the entity scanner and a plain grep see, and every save then reports a change that is
        // not one.
        return parts.map(p => p.replace(/ /g, ' ').trimEnd())
                    .filter(p => p.trim() !== '')
                    .join('\n\n');
    }

    function notifyChanged() {
        if (host) call('OnEditorInput', serialize(host));
    }

    // ── Spelling ───────────────────────────────────────────────────────────
    //
    // Prose's own spellcheck (SpellingService in the Hub: Hunspell, the author's dictionary, the
    // world's entity names). Chromium's is off on the editor, so there is one dictionary: the one
    // the author edits from the menu, Settings, the CLI and MCP. The squiggles are a CSS Custom
    // Highlight, ranges over the text, so nothing is ever added to the beat's markup.

    const SPELL_HIGHLIGHT = 'prose-misspelled';
    const WORD_RE = /[\p{L}\p{M}]+(?:['’][\p{L}\p{M}]+)*/gu;
    let spellVerdicts = new Map();   // word → true when misspelled, false when known
    let spellTimer = 0;
    let spellTarget = null;          // { range, word } the menu was opened on

    function spellSupported() {
        return typeof CSS !== 'undefined' && CSS.highlights && typeof Highlight !== 'undefined';
    }

    /** The editor's text nodes, less entity chips: a linked name is a name, not a typo. */
    function spellTextNodes(root) {
        const out = [];
        const walker = document.createTreeWalker(root, NodeFilter.SHOW_TEXT, {
            acceptNode: n => n.parentElement && n.parentElement.closest('.ent')
                ? NodeFilter.FILTER_REJECT : NodeFilter.FILTER_ACCEPT,
        });
        for (let n = walker.nextNode(); n; n = walker.nextNode()) out.push(n);
        return out;
    }

    function scheduleSpell() {
        clearTimeout(spellTimer);
        spellTimer = setTimeout(runSpell, 450);
    }

    /** Ask the Hub about words not seen before, then paint every misspelled one. */
    async function runSpell() {
        const el = host;
        if (!el || !dotnet || !spellSupported()) return;
        const unknown = new Set();
        for (const n of spellTextNodes(el))
            for (const m of (n.textContent || '').matchAll(WORD_RE))
                if (!spellVerdicts.has(m[0])) unknown.add(m[0]);
        if (unknown.size) {
            let bad;
            try { bad = await dotnet.invokeMethodAsync('CheckSpelling', [...unknown]); }
            catch { return; }   // disposed or disconnected: leave the squiggles as they were
            if (el !== host) return;
            const badSet = new Set(bad || []);
            for (const w of unknown) spellVerdicts.set(w, badSet.has(w));
        }
        paintSpell(el);
    }

    function paintSpell(el) {
        const ranges = [];
        for (const n of spellTextNodes(el)) {
            for (const m of (n.textContent || '').matchAll(WORD_RE)) {
                if (spellVerdicts.get(m[0]) !== true) continue;
                const r = document.createRange();
                r.setStart(n, m.index);
                r.setEnd(n, m.index + m[0].length);
                ranges.push(r);
            }
        }
        CSS.highlights.set(SPELL_HIGHLIGHT, new Highlight(...ranges));
    }

    /** The misspelled word a range starts in, with a range over exactly that word; else null. */
    function misspelledAt(range) {
        const node = range && range.startContainer;
        if (!node || node.nodeType !== Node.TEXT_NODE || !host || !host.contains(node)) return null;
        if (node.parentElement && node.parentElement.closest('.ent')) return null;
        const text = node.textContent || '';
        const at = range.startOffset;
        for (const m of text.matchAll(WORD_RE)) {
            if (m.index > at) break;
            if (at > m.index + m[0].length) continue;
            if (spellVerdicts.get(m[0]) !== true) return null;
            const r = document.createRange();
            r.setStart(node, m.index);
            r.setEnd(node, m.index + m[0].length);
            return { range: r, word: m[0] };
        }
        return null;
    }

    // ── Selection ──────────────────────────────────────────────────────────

    /** The live selection's range when it is inside the editor, else null. */
    function liveRange() {
        const sel = window.getSelection();
        if (!host || !sel || sel.rangeCount === 0) return null;
        const range = sel.getRangeAt(0);
        return host.contains(range.commonAncestorContainer) ? range : null;
    }

    function usable(range) {
        return range && host && host.contains(range.commonAncestorContainer) ? range : null;
    }

    function select(range) {
        // Not the author moving the caret: without this, the selection a command restores (the
        // picker's insert point, a restored toolbar selection) opened an announced entity card.
        navigating = false;
        const sel = window.getSelection();
        sel.removeAllRanges();
        sel.addRange(range);
    }

    /**
     * Put focus back in the editor with the selection the author last had there.
     *
     * A toolbar button reached by Tab takes focus away from the prose; focusing the host again
     * lets Chromium put the caret wherever it likes, which is usually the top of the beat — so a
     * keyboard user pressing Bold would have bolded nothing, somewhere else.
     */
    function focusEditor() {
        const remembered = usable(lastRange);
        const inside = liveRange();
        if (document.activeElement !== host) host.focus({ preventScroll: true });
        if (!inside && remembered) select(remembered);
    }

    function blockOf(node) {
        while (node && node !== host) {
            if (isBlock(node)) return node;
            node = node.parentNode;
        }
        return host;
    }

    /** The entity chip at or above a node, or null. */
    function chipAt(node) {
        while (node && node !== host) {
            if (isChip(node)) return node;
            node = node.parentNode;
        }
        return null;
    }

    /**
     * The entity chip the caret is in or immediately beside.
     *
     * A chip is contenteditable="false", so the caret never lands INSIDE it — it lands in the text
     * node before or after. Both neighbours are checked, which is what makes "arrow over to the
     * name, press Shift+F10" reach the chip a mouse user would have clicked.
     */
    function chipAtCaret() {
        const range = liveRange();
        if (!range) return null;

        const direct = chipAt(range.startContainer);
        if (direct) return direct;

        // Element containers index child nodes; text containers are between them.
        const node = range.startContainer;
        if (node.nodeType === Node.ELEMENT_NODE) {
            const kids = node.childNodes;
            return chipAt(kids[range.startOffset]) || chipAt(kids[range.startOffset - 1]);
        }

        if (range.startOffset === 0) return chipAt(node.previousSibling);
        if (range.startOffset === (node.textContent || '').length) return chipAt(node.nextSibling);
        return null;
    }

    /** The chip a dialog is acting on, provided it is still in this beat. */
    function targetChip() {
        if (contextTarget && host && host.contains(contextTarget)) return contextTarget;
        return chipAtCaret();
    }

    // The selected passage and what surrounds it, each serialized with the SAME function that
    // produces the beat's stored markup. Deliberately no character offsets: turning a DOM range
    // into a position in the serialized beat would mean a second serializer living here and
    // drifting from that one. The server strips these three strings with the code it already
    // uses everywhere else and finds the passage itself — one implementation, and the fiddly part
    // stays where it is tested.
    function selectionSpan() {
        const range = liveRange();
        if (!range || range.collapsed) return null;

        const quote = serialize(range.cloneContents());
        if (!quote.trim()) return null;

        const before = document.createRange();
        before.setStart(host, 0);
        before.setEnd(range.startContainer, range.startOffset);

        const after = document.createRange();
        after.setStart(range.endContainer, range.endOffset);
        after.setEnd(host, host.childNodes.length);

        return {
            quote: quote,
            prefix: serialize(before.cloneContents()),
            suffix: serialize(after.cloneContents()),
        };
    }

    /**
     * Where to put a keyboard-invoked menu. The caret's own rectangle when there is one — a menu
     * that opens in the corner of the screen is technically keyboard-accessible and useless.
     */
    function caretPoint() {
        const range = liveRange();
        if (range) {
            const rects = range.getClientRects();
            const r = rects.length > 0 ? rects[rects.length - 1] : null;
            if (r) return { x: r.left, y: r.bottom, top: r.top };

            // A collapsed range in an empty paragraph (<p><br></p>) has no rectangles at all, and
            // the picker opened in the editor's top corner instead of on the line. The line's own
            // element knows where it is.
            let node = range.startContainer;
            if (node.nodeType === Node.ELEMENT_NODE && node.childNodes[range.startOffset]) node = node.childNodes[range.startOffset];
            let el = node.nodeType === Node.ELEMENT_NODE ? node : node.parentElement;
            if (el && el.tagName === 'BR') el = el.parentElement;
            if (el && host.contains(el)) {
                const b = el.getBoundingClientRect();
                return { x: b.left, y: b.bottom, top: b.top };
            }
        }
        const h = host ? host.getBoundingClientRect() : { left: 0, top: 0 };
        return { x: h.left + 8, y: h.top + 8, top: h.top };
    }

    /**
     * Tell the shell what was clicked and where, so it can render the menu.
     *
     * Everything the menu needs is gathered HERE, at the moment of the click, and not asked for
     * again when an item is chosen: opening the menu moves focus, and a selection read back
     * afterwards is not reliably the one the author clicked.
     */
    function openContextMenu(target, x, y) {
        // From the pointer's target first, then from the CARET. Without the second, Shift+F10 with
        // the caret beside a chip offered no chip actions at all — document.activeElement is the
        // contenteditable host, never the chip.
        const chip = chipAt(target) || chipAtCaret();
        contextTarget = chip;
        hideCard();

        const span = selectionSpan();

        // The word under the pointer, or failing that the one the caret is in (Shift+F10).
        const atPoint = document.caretRangeFromPoint ? document.caretRangeFromPoint(x, y) : null;
        spellTarget = misspelledAt(atPoint) || misspelledAt(liveRange());

        call('OnContextMenu', {
            x: Math.round(x),
            y: Math.round(y),
            entityGuid: chip ? (chip.getAttribute('data-guid') || '') : null,
            entityRepo: chip ? (chip.getAttribute('data-repo') || '') : null,
            entityText: chip ? (chip.textContent || '') : null,
            quote: span ? span.quote : null,
            prefix: span ? span.prefix : null,
            suffix: span ? span.suffix : null,
            selectedText: span ? window.getSelection().toString() : null,
            spellWord: spellTarget ? spellTarget.word : null,
        });
    }

    // ── Toolbar state ──────────────────────────────────────────────────────

    // Bold/Italic/Underline/Strikethrough are toggles, so they say whether they are on: visually
    // for the author who cannot otherwise tell whether a word is already bold, and as aria-pressed
    // for a screen reader (WCAG 4.1.2). Set here rather than round-tripped through Blazor — this
    // runs on every caret move, and the buttons only need an attribute Blazor never renders.
    function updateToolbar() {
        if (!liveRange()) return;
        for (const command of FORMAT_COMMANDS) {
            let on = false;
            try { on = document.queryCommandState(command); } catch { /* not editable here */ }
            for (const button of document.querySelectorAll(`[data-format="${command}"]`)) {
                button.setAttribute('aria-pressed', on ? 'true' : 'false');
            }
        }
    }

    // One document listener for the life of the page, however many times init() runs: it reads
    // the current `host`, so a new editor (a new beat, a mode switch) needs nothing re-attached.
    let tracking = false;
    function trackSelection() {
        if (tracking) return;
        tracking = true;
        document.addEventListener('selectionchange', () => {
            const range = liveRange();
            if (!range) return;
            lastRange = range.cloneRange();
            updateToolbar();
            cardForCaret();
        });
    }

    // ── Keyboard ───────────────────────────────────────────────────────────

    /** The editing command a keystroke asks for, or null. Shared by both modes. */
    function shortcut(e) {
        if (!(e.ctrlKey || e.metaKey) || e.altKey) return null;
        const key = e.key.toLowerCase();
        if (!e.shiftKey && key === 'b') return 'bold';
        if (!e.shiftKey && key === 'i') return 'italic';
        if (!e.shiftKey && key === 'u') return 'underline';
        if (e.shiftKey && key === 'x') return 'strikeThrough';
        if (!e.shiftKey && key === 'k') return 'link';
        return null;
    }

    // ── Paste sanitization ─────────────────────────────────────────────────
    //
    // Nothing used to sanitize a paste: whatever HTML the clipboard carried went straight into the
    // contenteditable. A heading, a list, or a styled span losing its formatting is fine — the
    // serializer already reads anything it doesn't recognise as plain text — but a pasted
    // `<span class="ent" data-guid="...">` reaching the DOM unexamined would be indistinguishable
    // from a REAL entity chip. Every node this rebuilds is a brand-new element with none of the
    // original's attributes, which forecloses that whatever a foreign span was named or classed.
    const PASTE_BLOCK = new Set(['P', 'DIV', 'LI', 'H1', 'H2', 'H3', 'H4', 'H5', 'H6', 'BLOCKQUOTE',
                                 'PRE', 'TD', 'TH', 'DT', 'DD', 'FIGCAPTION', 'ADDRESS', 'SECTION', 'ARTICLE']);
    // Content that is not prose at all. A <style> element's text is CSS, and Word puts one in the
    // body of what it copies — without this, its rules arrived as a paragraph of the beat.
    const PASTE_SKIP = new Set(['STYLE', 'SCRIPT', 'TEMPLATE', 'NOSCRIPT', 'HEAD', 'TITLE', 'META', 'LINK',
                                'BUTTON', 'INPUT', 'SELECT', 'TEXTAREA']);
    const PASTE_MEDIA = new Set(['IMG', 'PICTURE', 'SVG', 'CANVAS', 'VIDEO', 'AUDIO', 'IFRAME', 'OBJECT', 'EMBED']);

    const GUID = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;
    const REPO = /^[a-z0-9-]{0,64}$/;

    /**
     * The emphasis a pasted element carries, from its tag AND its inline style: Google Docs marks
     * bold as <span style="font-weight:700">, not <b>, so tags alone dropped it. And Docs wraps its
     * whole clipboard in <b style="font-weight:normal">, so a tag-only reading made every paste
     * from Docs entirely bold.
     */
    function pastedStyles(node, tag) {
        const styles = [];
        const st = node.style || {};
        const w = st.fontWeight || '';
        const weight = w === 'bold' || w === 'bolder' ? 700 : parseInt(w, 10);
        const unbolded = w === 'normal' || w === 'lighter' || weight <= 400;
        if (((tag === 'B' || tag === 'STRONG') && !unbolded) || weight >= 600) styles.push('STRONG');
        if (((tag === 'I' || tag === 'EM') && st.fontStyle !== 'normal') || st.fontStyle === 'italic') styles.push('EM');
        const deco = `${st.textDecorationLine || ''} ${st.textDecoration || ''}`;
        // A link's underline is the link, not emphasis the author chose.
        if (tag === 'U' || (tag !== 'A' && /underline/.test(deco))) styles.push('U');
        if (tag === 'S' || tag === 'STRIKE' || tag === 'DEL' || /line-through/.test(deco)) styles.push('S');
        return styles;
    }

    /** Nodes inside a fresh chain of emphasis elements, outermost first. */
    function dressed(styles, nodes) {
        if (styles.length === 0) return nodes;
        const outer = document.createElement(styles[0]);
        let inner = outer;
        for (const tag of styles.slice(1)) { const next = document.createElement(tag); inner.appendChild(next); inner = next; }
        nodes.forEach(n => inner.appendChild(n));
        return [outer];
    }

    function sanitizePastedNode(node, out, dropped, inPre) {
        if (node.nodeType === Node.TEXT_NODE) {
            if (inPre) {
                node.nodeValue.split('\n').forEach((line, i) => {
                    if (i > 0) out.push(document.createElement('br'));
                    if (line) out.push(document.createTextNode(line));
                });
            } else {
                // HTML source indentation and line wrapping render as a single space, and must
                // arrive as one — a raw "\n\n" from the markup would become a paragraph break in
                // the stored beat. U+00A0 is left for serialize() to fold.
                const text = node.nodeValue.replace(/[ \t\n\r\f]+/g, ' ');
                if (text) out.push(document.createTextNode(text));
            }
            return;
        }
        if (node.nodeType !== Node.ELEMENT_NODE) return;

        // tagName is lowercase for SVG and MathML inside an HTML document.
        const tag = node.tagName.toUpperCase();
        if (PASTE_SKIP.has(tag)) return;

        // An entity link cut or copied from a beat. Rebuilt from its guid and repo, both checked
        // against a strict shape — nothing of the pasted element itself is kept — so a moved
        // sentence keeps its links instead of silently losing every one.
        if (node.classList && node.classList.contains('ent')) {
            const guid = node.getAttribute('data-guid') || '';
            const repo = node.getAttribute('data-repo') || '';
            const words = (node.textContent || '').trim();
            if (GUID.test(guid) && REPO.test(repo) && words) {
                const holder = document.createElement('span');
                holder.innerHTML = chipHtml(guid, repo, words);
                out.push(holder.firstChild);
                return;
            }
        }
        if (PASTE_MEDIA.has(tag)) { dropped.media = true; return; }
        if (tag === 'BR') { out.push(document.createElement('br')); return; }

        if (tag === 'TABLE') dropped.tables = true;
        if (/^H[1-6]$/.test(tag)) dropped.headings = true;
        if (tag === 'UL' || tag === 'OL') dropped.lists = true;
        if (tag === 'A' && node.hasAttribute('href')) dropped.links = true;

        const children = [];
        for (const child of node.childNodes) sanitizePastedNode(child, children, dropped, inPre || tag === 'PRE');

        const styles = pastedStyles(node, tag);
        const holdsParagraphs = children.some(c => isElement(c, 'DIV'));

        if (PASTE_BLOCK.has(tag) || holdsParagraphs) {
            // A heading, a list item, a table cell — none of those exist in this editor's grammar,
            // but each keeping its own paragraph reads far better than a pasted list running
            // together into one sentence. A block holding blocks (an <li> around a <p>) is
            // flattened into its paragraphs rather than nested, and so is an INLINE element
            // holding them (Docs' <b> around the whole document), whose emphasis moves inside
            // each paragraph instead of wrapping blocks in a <strong>.
            if (holdsParagraphs) {
                for (const para of groupIntoParagraphs(children)) {
                    para.replaceChildren(...dressed(styles, Array.from(para.childNodes)));
                    out.push(para);
                }
            } else {
                out.push(paragraphOf(dressed(styles, children)));
            }
            return;
        }

        // Anything else — SPAN, FONT, A, UL/OL, TABLE/TBODY/TR, or a tag this editor has never
        // heard of — keeps its emphasis as fresh <strong>/<em>/<u>/<s> and nothing else: the tag
        // and every one of its attributes are dropped.
        dressed(styles, children).forEach(c => out.push(c));
    }

    function paragraphOf(nodes) {
        const p = document.createElement('div');
        nodes.forEach(n => p.appendChild(n));
        return p;
    }

    /** Paragraphs pass through; each run of loose inline nodes between them becomes one. */
    function groupIntoParagraphs(nodes) {
        const result = [];
        let run = [];
        const flush = () => {
            if (run.some(n => n.nodeType !== Node.TEXT_NODE || n.nodeValue.trim() !== '')) result.push(paragraphOf(run));
            run = [];
        };
        for (const n of nodes) {
            if (isElement(n, 'DIV')) { flush(); result.push(n); }
            else run.push(n);
        }
        flush();
        return result;
    }

    function trimEdges(el) {
        const walker = document.createTreeWalker(el, NodeFilter.SHOW_TEXT);
        const texts = [];
        while (walker.nextNode()) texts.push(walker.currentNode);
        if (texts.length === 0) return;
        texts[0].nodeValue = texts[0].nodeValue.replace(/^\s+/, '');
        const last = texts[texts.length - 1];
        last.nodeValue = last.nodeValue.replace(/\s+$/, '');
    }

    function sanitizePastedHtml(html) {
        // Windows hands over the whole CF_HTML document, with line breaks around the fragment
        // markers inside <body>. Those became a space on each side of every pasted phrase.
        const start = html.indexOf('<!--StartFragment-->');
        const end = html.indexOf('<!--EndFragment-->');
        if (start >= 0 && end > start) html = html.slice(start + '<!--StartFragment-->'.length, end);

        const doc = new DOMParser().parseFromString(html, 'text/html');
        const dropped = {};
        let nodes = [];
        for (const child of doc.body.childNodes) sanitizePastedNode(child, nodes, dropped, false);

        const blank = n => n.nodeType === Node.TEXT_NODE && n.nodeValue.trim() === '';
        while (nodes.length && blank(nodes[0])) nodes.shift();
        while (nodes.length && blank(nodes[nodes.length - 1])) nodes.pop();

        if (nodes.some(n => isElement(n, 'DIV'))) {
            nodes = groupIntoParagraphs(nodes);
            nodes.forEach(trimEdges);
            // ONE paragraph is pasted into the current one, not as a new one. A phrase copied out
            // of Word arrives wrapped in <p class=MsoNormal>, and inserting that block split the
            // sentence it was pasted into across three paragraphs.
            if (nodes.length === 1) nodes = Array.from(nodes[0].childNodes);
        }

        const box = document.createElement('div');
        nodes.forEach(n => box.appendChild(n));
        return { html: box.innerHTML, dropped };
    }

    // A plain-text clipboard entry (no text/html) gets the same paragraph convention
    // ProseRenderer.ToHtml uses on the server: a blank line starts a new paragraph. A single
    // paragraph goes in inline, for the same reason as above.
    function pastedPlainTextHtml(text) {
        const paragraphs = text.replace(/\r\n?/g, '\n').split(/\n{2,}/).filter(p => p.trim() !== '');
        const fill = (el, para) => para.split('\n').forEach((line, i) => {
            if (i > 0) el.appendChild(document.createElement('br'));
            el.appendChild(document.createTextNode(line));
        });

        const box = document.createElement('div');
        if (paragraphs.length <= 1) fill(box, paragraphs[0] || '');
        else paragraphs.forEach(p => { const d = document.createElement('div'); fill(d, p.trim()); box.appendChild(d); });
        return box.innerHTML;
    }

    function describeDropped(dropped) {
        const parts = [];
        if (dropped.media) parts.push('images or media');
        if (dropped.tables) parts.push('a table');
        if (dropped.lists) parts.push('list formatting');
        if (dropped.headings) parts.push('heading formatting');
        if (dropped.links) parts.push('hyperlinks');
        if (parts.length === 0) return null;
        return `The pasted text included ${parts.join(', ')}, which this editor does not keep — `
             + 'the words were pasted, that formatting was not.';
    }

    function onPaste(e) {
        e.preventDefault();
        const data = e.clipboardData;
        const html = data && data.getData('text/html');

        let insert, note = null;
        if (html) {
            const result = sanitizePastedHtml(html);
            insert = result.html;
            note = describeDropped(result.dropped);
        } else {
            insert = pastedPlainTextHtml((data && data.getData('text/plain')) || '');
        }

        if (insert) {
            // A pasted link lands outside its paragraph when the paste ends one — the same
            // Chromium behaviour insertChip works around, and the same answer.
            const range = liveRange();
            if (range && /class="ent"/.test(insert) && endsBlock(range)) {
                const box = document.createElement('div');
                box.innerHTML = insert;
                const fragment = document.createDocumentFragment();
                while (box.firstChild) fragment.appendChild(box.firstChild);
                placeByHand(range, fragment, null);
            } else {
                document.execCommand('insertHTML', false, insert);
            }
            notifyChanged();
        }
        if (note) call('OnPasteNote', note);
    }

    // ── Entity chips ───────────────────────────────────────────────────────

    function chipHtml(guid, repo, text) {
        return `<span class="ent" contenteditable="false" data-guid="${escapeAttr(guid)}" `
             + `data-repo="${escapeAttr(repo || '')}">${escapeText(text)}</span>`;
    }

    function chipsFor(guid) {
        return host.querySelectorAll(`.ent[data-guid="${CSS.escape(guid)}"]`);
    }

    /**
     * Replace a range with a chip, undoably. `replacing` is the chip being repointed, if any.
     *
     * insertHTML is checked rather than trusted. If the command refuses, the chip is built by hand
     * (this one edit then cannot be undone, but it is made). If it runs and the result is not
     * exactly one new chip, the step is undone and reported — a half-made link in the prose is
     * worse than none.
     *
     * @returns true when the chip is in place.
     */
    function insertChip(range, guid, repo, text, replacing) {
        // Chromium lifts an inserted non-editable span OUT of its paragraph when the insertion
        // ends the paragraph — at the end of a line, over its last word, over a chip that ends it —
        // so the link saved as a paragraph of its own. Measured, not guessed: nothing appended
        // after the chip (a space, a character, a zero-width space) changes it. There the chip is
        // placed by hand; that one step cannot be undone, but it stays in its paragraph.
        if (endsBlock(range)) { placeByHand(range, chipElement(guid, repo, text), replacing); return true; }

        const sameGuid = replacing && replacing.getAttribute('data-guid') === guid;
        const before = chipsFor(guid).length - (sameGuid ? 1 : 0);

        select(range);
        const ok = document.execCommand('insertHTML', false, chipHtml(guid, repo, text));

        if (!ok) {
            placeByHand(range, chipElement(guid, repo, text), replacing);
            return true;
        }

        const made = chipsFor(guid);
        if (made.length !== before + 1 || (replacing && host.contains(replacing))) {
            document.execCommand('undo');
            return false;
        }
        made.forEach(c => { if (c.contentEditable !== 'false') c.contentEditable = 'false'; });
        return true;
    }

    function chipElement(guid, repo, text) {
        const holder = document.createElement('span');
        holder.innerHTML = chipHtml(guid, repo, text);
        return holder.firstChild;
    }

    /** True when nothing but a placeholder <br> follows the range's end in its paragraph. */
    function endsBlock(range) {
        const block = blockOf(range.endContainer);
        const rest = document.createRange();
        rest.setStart(range.endContainer, range.endOffset);
        rest.setEnd(block, block.childNodes.length);
        const after = rest.cloneContents();
        return after.textContent === '' && !after.querySelector('.ent, img');
    }

    /** Put a node (or fragment) where the range is, by DOM rather than by command. */
    function placeByHand(range, node, replacing) {
        const last = node.nodeType === Node.DOCUMENT_FRAGMENT_NODE ? node.lastChild : node;
        if (replacing && host.contains(replacing)) replacing.replaceWith(node);
        else { range.deleteContents(); range.insertNode(node); }
        if (last && last.parentNode) {
            const after = document.createRange();
            after.setStartAfter(last);
            after.collapse(true);
            select(after);
        }
    }

    /**
     * Move a range's start forward and its end back by a number of characters, walking text
     * nodes. Used to leave a selection's edge whitespace out of a new link.
     */
    function shrinkRange(range, fromStart, fromEnd) {
        // Walked from the parent when the range sits inside one text node: a TreeWalker never
        // returns its own root, so rooting it at that node found no text at all.
        const common = range.commonAncestorContainer;
        const root = common.nodeType === Node.TEXT_NODE ? common.parentNode : common;
        const walker = document.createTreeWalker(root, NodeFilter.SHOW_TEXT);
        const texts = [];
        while (walker.nextNode()) {
            if (range.intersectsNode(walker.currentNode)) texts.push(walker.currentNode);
        }
        if (texts.length === 0) return;

        let node = range.startContainer.nodeType === Node.TEXT_NODE ? range.startContainer : texts[0];
        let offset = node === range.startContainer ? range.startOffset : 0;
        let left = fromStart;
        let i = texts.indexOf(node);
        while (left > 0 && i < texts.length) {
            const room = texts[i].nodeValue.length - offset;
            if (left <= room) { offset += left; left = 0; break; }
            left -= room; i++; offset = 0;
        }
        if (i < texts.length) range.setStart(texts[i], offset);

        node = range.endContainer.nodeType === Node.TEXT_NODE ? range.endContainer : texts[texts.length - 1];
        offset = node === range.endContainer ? range.endOffset : node.nodeValue.length;
        left = fromEnd;
        i = texts.indexOf(node);
        while (left > 0 && i >= 0) {
            if (left <= offset) { offset -= left; left = 0; break; }
            left -= offset; i--; offset = i >= 0 ? texts[i].nodeValue.length : 0;
        }
        if (i >= 0) range.setEnd(texts[i], offset);
    }

    // ── Entity cards ───────────────────────────────────────────────────────
    //
    // Hovering a link shows a card for its entity — name, type, a paragraph, aliases, how often the
    // book mentions it — with its actions, the way a link preview works in any document app. It
    // replaced a bare title="" tooltip, which could say only that the chip was clickable.
    //
    // WCAG 1.4.13, content on hover: it waits before appearing, so moving the pointer across the
    // prose does not flash a card at every name; it stays while the pointer is on the chip OR the
    // card, so it can be read and its buttons reached; and Escape dismisses it without moving
    // anything.
    //
    // A keyboard reaches the same information by arrowing the caret beside a chip — the card opens
    // and its summary is announced — and the same actions through Shift+F10. So the card itself is
    // a pointer convenience, kept out of the accessibility tree rather than offered as a second,
    // unreachable copy of what the menu already provides.
    const CARD_SHOW_DELAY = 350;
    const CARD_HIDE_DELAY = 250;
    const CARD_CARET_DELAY = 700;
    const NAV_KEYS = new Set(['ArrowLeft', 'ArrowRight', 'ArrowUp', 'ArrowDown', 'Home', 'End', 'PageUp', 'PageDown']);

    let card = null;          // the one card element, created on first use
    let cardLive = null;      // where a keyboard-opened card is announced
    let cardChip = null;      // the chip the card is showing, or null
    let cardByKeyboard = false;
    let pendingByKeyboard = false;  // the card waiting to open was asked for by the caret
    let cardTicket = 0;       // only the newest request may render
    let showTimer = 0;
    let hideTimer = 0;
    let previews = new Map(); // guid -> Promise<preview | null | undefined>, per editor
    let navigating = false;   // the caret last moved by arrow keys or a click, not by typing

    function el(tag, className, text) {
        const e = document.createElement(tag);
        if (className) e.className = className;
        if (text !== undefined && text !== null) e.textContent = text;
        return e;
    }

    function ensureCard() {
        if (card && card.isConnected) return;
        card = el('div', 'ent-card');
        card.hidden = true;
        card.setAttribute('aria-hidden', 'true');
        // A click on the card must never take focus from the prose, or the caret goes with it.
        card.addEventListener('mousedown', e => e.preventDefault());
        card.addEventListener('mouseenter', () => clearTimeout(hideTimer));
        card.addEventListener('mouseleave', scheduleHide);
        document.body.appendChild(card);

        cardLive = el('div', 'visually-hidden');
        cardLive.setAttribute('aria-live', 'polite');
        document.body.appendChild(cardLive);
    }

    /** null: the entity does not exist. undefined: it could not be asked for (not cached). */
    function preview(guid) {
        if (!previews.has(guid)) {
            const asked = dotnet
                ? dotnet.invokeMethodAsync('GetEntityPreview', guid).catch(() => undefined)
                : Promise.resolve(undefined);
            previews.set(guid, asked);
        }
        return previews.get(guid);
    }

    // Built from text nodes only. Every string here came from the database, and a description is
    // free text an author typed — none of it is ever parsed as markup.
    function renderCard(chip, p) {
        card.replaceChildren();
        const words = (chip.textContent || '').trim();

        const head = el('div', 'ent-card-head');
        head.append(el('span', 'ent-card-name', p ? p.name : words));
        if (p) head.append(el('span', 'ent-card-type', p.typeName));
        card.append(head);

        if (p === undefined) {
            card.append(el('p', 'ent-card-desc', 'Could not load this entity.'));
        } else if (p === null) {
            card.append(el('p', 'ent-card-desc', 'This link points at an entity that no longer exists. '
                                                + 'Edit it to point at a real one, or remove it.'));
        } else {
            if (p.status && p.status !== 'canon') card.append(el('div', 'ent-card-status', p.status));
            card.append(p.description
                ? el('p', 'ent-card-desc', p.description)
                : el('p', 'ent-card-desc absent', 'No description yet.'));
            if (words && words !== p.name) card.append(el('div', 'ent-card-meta', `Linked here as “${words}”`));
            if (p.aliases && p.aliases.length) card.append(el('div', 'ent-card-meta', `Also called ${p.aliases.join(', ')}`));
            card.append(el('div', 'ent-card-meta', `Mentioned in ${p.mentions} beat${p.mentions === 1 ? '' : 's'}`));
        }

        const actions = el('div', 'ent-card-actions');
        const button = (label, action) => {
            const b = el('button', 'mini', label);
            b.type = 'button';
            b.tabIndex = -1;   // inside an aria-hidden card; the keyboard route is Shift+F10
            b.addEventListener('click', () => cardAction(action));
            return b;
        };
        actions.append(button('Edit link', 'edit'), button('Remove link', 'remove'));
        if (p) actions.append(button('Open in the wiki', 'open'));
        card.append(actions);
    }

    function placeCard(chip) {
        const margin = 8;
        card.style.left = '0px';
        card.style.top = '0px';
        card.hidden = false;
        const r = chip.getBoundingClientRect();
        const w = card.offsetWidth, h = card.offsetHeight;
        const left = Math.max(margin, Math.min(r.left, window.innerWidth - w - margin));
        let top = r.bottom + 6;
        // Above the chip when there is no room below it, rather than off the bottom of the window.
        if (top + h > window.innerHeight - margin) top = Math.max(margin, r.top - h - 6);
        card.style.left = `${left}px`;
        card.style.top = `${top}px`;
    }

    async function showCard(chip, byKeyboard) {
        ensureCard();
        clearTimeout(hideTimer);
        const mine = ++cardTicket;
        const guid = chip.getAttribute('data-guid') || '';
        const p = await preview(guid);
        if (p === undefined) previews.delete(guid);   // a failure is worth asking again next time
        if (mine !== cardTicket || !chip.isConnected) return;

        cardChip = chip;
        cardByKeyboard = byKeyboard;
        renderCard(chip, p);
        placeCard(chip);
        if (byKeyboard) cardLive.textContent = announce(chip, p);
    }

    function announce(chip, p) {
        if (p === null) return `${chip.textContent}: this link points at an entity that no longer exists. Shift F10 to edit or remove it.`;
        if (!p) return '';
        return `${p.name}, ${p.typeName}. ${p.description ? p.description + ' ' : ''}Shift F10 for link actions.`;
    }

    function hideCard() {
        cardTicket++;
        clearTimeout(showTimer);
        clearTimeout(hideTimer);
        cardChip = null;
        cardByKeyboard = false;
        pendingByKeyboard = false;
        if (card) card.hidden = true;
        if (cardLive) cardLive.textContent = '';
    }

    function scheduleHide() {
        clearTimeout(showTimer);
        clearTimeout(hideTimer);
        hideTimer = setTimeout(hideCard, CARD_HIDE_DELAY);
    }

    function scheduleShow(chip, delay, byKeyboard) {
        clearTimeout(hideTimer);
        if (chip === cardChip && card && !card.hidden) return;
        clearTimeout(showTimer);
        pendingByKeyboard = byKeyboard;
        showTimer = setTimeout(() => { pendingByKeyboard = false; showCard(chip, byKeyboard); }, delay);
    }

    function cardAction(action) {
        const chip = cardChip;
        hideCard();
        if (!chip || !host || !host.contains(chip)) return;
        contextTarget = chip;
        if (action === 'remove') { api.removeEntity(); return; }
        call('OnEntityCardAction', action,
             chip.getAttribute('data-guid') || '', chip.getAttribute('data-repo') || '', chip.textContent || '');
    }

    // Once for the page, like the selection tracking: these read the current host.
    let cardWatching = false;
    function watchCards() {
        if (cardWatching) return;
        cardWatching = true;
        document.addEventListener('keydown', e => {
            if (e.key === 'Escape' && card && !card.hidden) hideCard();
        }, true);
        // A fixed card left behind by a scroll would point at nothing.
        window.addEventListener('scroll', hideCard, true);
        window.addEventListener('blur', hideCard);
    }

    // The caret came to rest beside a chip, by the keyboard or a click: open its card after a
    // pause, announced. Typing beside a name does not count — that would pop a card at the end
    // of every name the author types past.
    function cardForCaret() {
        const range = liveRange();
        const chip = navigating && range && range.collapsed ? chipAtCaret() : null;
        if (chip) scheduleShow(chip, CARD_CARET_DELAY, true);
        else if (cardByKeyboard) hideCard();
        // Arrowing PAST a chip in under the delay used to open its card anyway, for a name the
        // caret had already left, and announce it.
        else if (pendingByKeyboard) { clearTimeout(showTimer); pendingByKeyboard = false; }
    }

    /**
     * Chromium preserves the look of what it replaced: unlinking inserted the words inside a
     * <font color> or a styled <span> copied from the chip, so they still looked like a link and
     * whatever was typed next continued in that colour. The stored text was right — the serializer
     * ignores both — but the page lied until reload.
     */
    function unwrapTypingStyle() {
        const sel = window.getSelection();
        if (!sel || sel.rangeCount === 0) return;
        let node = sel.anchorNode;
        if (node && node.nodeType === Node.TEXT_NODE) node = node.parentNode;
        while (node && node !== host && !isBlock(node)) {
            const presentational = isElement(node, 'FONT')
                || (isElement(node, 'SPAN') && !isChip(node) && node.hasAttribute('style'));
            const parent = node.parentNode;
            if (presentational) node.replaceWith(...node.childNodes);
            node = parent;
        }
    }

    // ── The module ─────────────────────────────────────────────────────────

    // Named rather than returned inline so the listeners inside init() can call it without leaning
    // on how the interop layer binds `this`.
    const api = {
        /** Close any entity card. proseModal calls this when a dialog or menu opens over the prose. */
        hideCard() { hideCard(); },

        init(element, dotNetRef) {
            host = element;
            dotnet = dotNetRef;
            contextTarget = null;
            lastRange = null;
            savedRange = null;
            previews = new Map();
            hideCard();
            // Made now rather than on the first hover: a live region created a moment before its
            // first text is set is often not announced at all, so the first card went unheard.
            ensureCard();
            trackSelection();
            watchCards();

            spellTarget = null;
            scheduleSpell();

            // init() can be asked twice for the same element (a JS error resets the component's
            // wiring flag). Listeners are per element, so a second set would fire every handler
            // twice — including the paste handler, which would paste twice.
            if (element.dataset.proseWired === '1') return;
            element.dataset.proseWired = '1';

            // Emit real tags (<b>, <i>) instead of <span style="font-weight:bold">, so
            // serializeNode has something it can recognise.
            try { document.execCommand('styleWithCSS', false, false); } catch { /* older engines */ }

            element.addEventListener('input', notifyChanged);
            element.addEventListener('input', scheduleSpell);
            element.addEventListener('paste', onPaste);

            // The context menu. AreDefaultContextMenusEnabled is false in the Writer host, so this
            // is the ONLY menu the editor has — there is no browser one behind it to fall back on.
            element.addEventListener('contextmenu', e => {
                e.preventDefault();
                openContextMenu(e.target, e.clientX, e.clientY);
            });

            // Left-click an entity chip: the same menu right-click opens (Edit link, Remove link,
            // Open in the wiki). A chip used to send this click straight out to the default
            // browser, so the only way to edit or remove a link was to already know right-click
            // did something different.
            //
            // Not preventDefault(): the author still needs the click to place a caret beside the
            // chip. Ignored while a selection is being dragged — a click that ends a drag across a
            // chip is the author selecting text, not asking to act on it.
            element.addEventListener('click', e => {
                const sel = window.getSelection();
                if (sel && !sel.isCollapsed) return;
                const chip = chipAt(e.target);
                if (chip) openContextMenu(chip, e.clientX, e.clientY);
            });

            // Hover cards. mouseover/mouseout bubble from the chip's own text, so the chip is
            // found from the target; moving between the chip's parts is not leaving it.
            element.addEventListener('mouseover', e => {
                const chip = chipAt(e.target);
                if (chip) scheduleShow(chip, CARD_SHOW_DELAY, false);
            });
            element.addEventListener('mouseout', e => {
                const chip = chipAt(e.target);
                if (chip && !chip.contains(e.relatedTarget)) scheduleHide();
            });
            element.addEventListener('mousedown', () => { navigating = true; });

            element.addEventListener('keydown', e => {
                navigating = NAV_KEYS.has(e.key);
                if (!navigating && !e.shiftKey) hideCard();

                // Shift+F10 and the Menu key are how a keyboard reaches a context menu, and a
                // voice user cannot chase a mouse menu at all. Positioned at the caret.
                if (e.key === 'ContextMenu' || (e.shiftKey && e.key === 'F10')) {
                    e.preventDefault();
                    const at = caretPoint();
                    openContextMenu(document.activeElement, at.x, at.y);
                    return;
                }

                const command = shortcut(e);
                if (!command) return;
                e.preventDefault();
                if (command === 'link') call('OnLinkRequested');
                else api.format(command);
            });
        },

        /**
         * Markdown mode's keyboard shortcuts. The textarea edits the markup itself, so a shortcut
         * wraps the selection in the markers the toolbar would have used.
         */
        initRaw(element) {
            if (!element || element.dataset.proseWired === '1') return;
            element.dataset.proseWired = '1';
            element.addEventListener('keydown', e => {
                const command = shortcut(e);
                if (!command || !RAW_MARKERS[command]) return;
                e.preventDefault();
                api.formatRaw(element, command);
            });
        },

        setHtml(element, html) {
            hideCard();
            element.innerHTML = html;
            // Every node a remembered range or menu target pointed at has just been replaced.
            contextTarget = null;
            lastRange = null;
            savedRange = null;
            spellTarget = null;
            scheduleSpell();
        },

        /**
         * Replace the misspelled word the menu was opened on. Through insertText, so Ctrl+Z
         * undoes it like typing would. Refused if the text under the word has changed since.
         */
        spellReplace(replacement) {
            const t = spellTarget;
            spellTarget = null;
            if (!t || !host || !host.contains(t.range.startContainer)) return false;
            if (t.range.toString() !== t.word) return false;
            focusEditor();
            select(t.range);
            if (!document.execCommand('insertText', false, replacement)) {
                t.range.deleteContents();
                t.range.insertNode(document.createTextNode(replacement));
            }
            notifyChanged();
            scheduleSpell();
            return true;
        },

        /** The dictionary changed: forget every verdict and check the beat again. */
        spellRecheck() {
            spellVerdicts = new Map();
            scheduleSpell();
        },

        /**
         * How many reader-visible characters precede the caret.
         *
         * Reported in PLAIN coordinates on purpose. A markup offset computed here would have to
         * serialize a partial fragment, and a partial serialization is not guaranteed to equal
         * the same substring of the whole — which, for a split, would cut inside a tag. The server
         * maps this through PlainTextMap instead, which is built from the real parsers.
         *
         * @returns the offset, or -1 when the caret is not in the editor.
         */
        caretOffset() {
            const caret = liveRange();
            if (!caret) return -1;
            const before = document.createRange();
            before.setStart(host, 0);
            before.setEnd(caret.startContainer, caret.startOffset);
            return before.toString().length;
        },

        /**
         * What is selected, when it is safe to act on. Also remembers the range: the link dialog
         * that opens next moves focus into its search box, and by the time the author presses
         * Link the live selection is in that box, not in the prose.
         */
        selection() {
            // The live selection, or failing that the last one the author had in the prose: a
            // menu or a toolbar button reached by keyboard can have taken focus in between.
            const live = liveRange();
            const range = live && !live.collapsed ? live : usable(lastRange);
            if (!range || range.collapsed) return { text: '', ok: false };
            savedRange = range.cloneRange();
            return { text: range.toString(), ok: true };
        },

        /**
         * Where the author is: the selection, or just the caret. Remembered for the entity picker,
         * which takes focus into its search box, and with a screen position so the picker opens at
         * the caret rather than in a corner.
         */
        caret() {
            const range = liveRange() || usable(lastRange);
            if (!range) return { ok: false, text: '', x: 0, y: 0, top: 0 };
            savedRange = range.cloneRange();
            select(range);
            const at = caretPoint();
            return {
                ok: true, text: range.collapsed ? '' : range.toString(),
                x: Math.round(at.x), y: Math.round(at.y), top: Math.round(at.top),
            };
        },

        /**
         * Insert an entity as a link at the remembered caret: its name, linked.
         * @returns null when inserted, otherwise why not.
         */
        insertEntity(guid, repo, name) {
            if (!host) return 'The editor is not open.';
            const remembered = usable(savedRange) || liveRange() || usable(lastRange);
            savedRange = null;
            if (!remembered) return 'Put the caret where the entity should go first.';

            const range = remembered.cloneRange();
            range.collapse(false);
            if (chipAt(range.startContainer)) return 'The caret is inside a link. Move it outside the link first.';

            focusEditor();
            if (!insertChip(range, guid, repo, name, null)) return 'The entity could not be inserted here — nothing was changed.';
            notifyChanged();
            return null;
        },

        format(command) {
            if (!host) return;
            focusEditor();
            document.execCommand(command, false, null);
            notifyChanged();
            updateToolbar();
        },

        insert(text) {
            if (!host) return;
            focusEditor();
            document.execCommand('insertText', false, text);
            notifyChanged();
        },

        /** Markdown mode: wrap the textarea's selection in a command's markers. */
        formatRaw(el, command) {
            const markers = RAW_MARKERS[command];
            if (markers) api.wrapTextarea(el, markers[0], markers[1]);
        },

        /**
         * Wrap the textarea's selection in literal markers, or insert them at the caret. Through
         * insertText so it can be undone — setRangeText does not reach the undo stack. Either way
         * an 'input' event fires, which is what tells the component the text changed.
         */
        wrapTextarea(el, before, after) {
            if (!el) return;
            const start = el.selectionStart, end = el.selectionEnd;
            const replacement = before + el.value.substring(start, end) + after;

            el.focus();
            if (!document.execCommand('insertText', false, replacement)) {
                el.setRangeText(replacement, start, end, 'end');
                el.dispatchEvent(new Event('input', { bubbles: true }));
            }

            // Put the caret between the markers when wrapping an empty selection, so the next
            // keystroke lands inside the emphasis rather than after it.
            if (start === end && after.length > 0) {
                const caret = start + before.length;
                el.setSelectionRange(caret, caret);
            }
        },

        /**
         * Wrap the selection in a new entity link.
         * @returns null when linked, otherwise a sentence saying why not.
         */
        wrapSelection(guid, repo) {
            if (!host) return 'The editor is not open.';

            const live = liveRange();
            const range = (live && !live.collapsed ? live : null) || usable(savedRange);
            savedRange = null;
            if (!range || range.collapsed) return 'Select the words you want to link first.';

            if (chipAt(range.commonAncestorContainer) || range.cloneContents().querySelector('.ent')) {
                return 'That selection already contains an entity link. Remove that link first, or select only unlinked words.';
            }

            // A chip lives inside one paragraph. Wrapping a selection that crossed a paragraph
            // break deleted the break along with it.
            if (blockOf(range.startContainer) !== blockOf(range.endContainer)) {
                return 'An entity link cannot cross a paragraph break. Select words within one paragraph.';
            }

            const text = range.toString();
            const [, lead, name, trail] = /^(\s*)([\s\S]*?)(\s*)$/.exec(text);
            if (!name) return 'Select the words you want to link first.';

            // The spaces a double-click drags in stay outside the link, as plain prose.
            const trimmed = range.cloneRange();
            if (lead || trail) shrinkRange(trimmed, lead.length, trail.length);

            focusEditor();
            if (!insertChip(trimmed, guid, repo, name, null)) return 'The link could not be made here — nothing was changed.';
            notifyChanged();
            return null;
        },

        /**
         * Repoint the chip the menu was opened on, and optionally change its words.
         * @returns null when applied, otherwise why not.
         */
        applyEntity(guid, repo, text) {
            const target = targetChip();
            if (!target) return 'That link is no longer in the beat — it may have been edited away.';

            const words = text ? text : (target.textContent || '');
            const range = document.createRange();
            range.selectNode(target);
            focusEditor();
            if (!insertChip(range, guid, repo, words, target)) return 'That link could not be changed — nothing was changed.';
            contextTarget = null;
            notifyChanged();
            return null;
        },

        /**
         * Unwrap the chip back to plain prose, keeping its words.
         * @returns null when removed, otherwise why not.
         */
        removeEntity() {
            const target = targetChip();
            if (!target) return 'That link is no longer in the beat — it may have been edited away.';

            const words = target.textContent || '';
            const range = document.createRange();
            range.selectNode(target);
            focusEditor();
            select(range);
            // Checked, not trusted: if the command refused, nothing was inserted and the chip is
            // unwrapped by hand; if it ran but left the chip behind, the words are already in and
            // only the chip goes — replacing it too would print the name twice.
            const ok = document.execCommand('insertText', false, words);
            if (!ok) target.replaceWith(document.createTextNode(words));
            else if (host.contains(target)) target.remove();
            else unwrapTypingStyle();

            contextTarget = null;
            notifyChanged();
            return null;
        },
    };

    return api;
})();

// ── Modal focus management ─────────────────────────────────────────────────────────────────
//
// role="dialog" + aria-modal describes a dialog; it does not behave like one. aria-modal has no
// effect on the Tab order, so without this a keyboard user tabs straight out of the dialog and
// into the editor and toolbar sitting behind the scrim, with no way to tell they have left.
//
// Escape is handled in the component rather than here, because only the component knows what
// cancelling means — this module owns the focus, not the lifecycle.
window.proseModal = (() => {
    const FOCUSABLE = [
        'a[href]', 'button:not([disabled])', 'input:not([disabled])',
        'select:not([disabled])', 'textarea:not([disabled])', '[tabindex]:not([tabindex="-1"])',
    ].join(',');

    let restoreTo = null;
    let trapped = null;

    function focusable(dialog) {
        return Array.from(dialog.querySelectorAll(FOCUSABLE))
                    .filter(el => el.offsetParent !== null || el === document.activeElement);
    }

    function onKeyDown(e) {
        if (e.key !== 'Tab' || !trapped) return;
        const items = focusable(trapped);
        if (items.length === 0) { e.preventDefault(); return; }

        const first = items[0], last = items[items.length - 1];
        // Shift+Tab off the first element wraps to the last, and vice versa, so focus can
        // circle the dialog forever but never leave it.
        if (e.shiftKey && document.activeElement === first) { e.preventDefault(); last.focus(); }
        else if (!e.shiftKey && document.activeElement === last) { e.preventDefault(); first.focus(); }
    }

    return {
        open(dialog) {
            if (!dialog) return;
            // Remembered before focus moves, so closing returns the author to the control they
            // opened the dialog from rather than dropping them at the top of the document.
            // A hover card belongs to the prose; it has no business showing over a dialog.
            if (window.proseEditor) window.proseEditor.hideCard();
            restoreTo = document.activeElement;
            trapped = dialog;
            document.addEventListener('keydown', onKeyDown, true);
            // A dialog can name the control the author came to use. The link dialog's first
            // control is the read-only text being linked; the one they type into is Search.
            const preferred = dialog.querySelector('[data-autofocus]');
            const items = focusable(dialog);
            (preferred || items[0] || dialog).focus();
        },

        close() {
            document.removeEventListener('keydown', onKeyDown, true);
            trapped = null;
            if (restoreTo && typeof restoreTo.focus === 'function') restoreTo.focus();
            restoreTo = null;
        },

        /**
         * Keep a pointer-positioned popup on screen. A context menu opened near the bottom or
         * right edge used to run off the window, and the items past the edge could be neither
         * seen nor clicked.
         */
        fit(el, x, below, above) {
            if (!el) return;
            const margin = 8;
            // With an anchor, start from it every time: this runs again whenever the popup grows,
            // and measuring from where the last fit moved it would flip it back and forth.
            if (typeof below === 'number') {
                el.style.left = `${x}px`;
                el.style.top = `${below}px`;
            }
            el.style.maxHeight = '';

            let r = el.getBoundingClientRect();
            if (r.right > window.innerWidth - margin) {
                el.style.left = `${Math.max(margin, window.innerWidth - r.width - margin)}px`;
            }
            if (r.bottom > window.innerHeight - margin) {
                // Above the anchor when there is room there — above the caret's LINE, not over it,
                // when one is given — and pinned to the bottom edge when not.
                const flipped = (typeof above === 'number' ? above : r.top) - r.height;
                el.style.top = `${flipped >= margin ? flipped : Math.max(margin, window.innerHeight - r.height - margin)}px`;
            }
            // Still taller than what is left: cap it, and its list scrolls.
            r = el.getBoundingClientRect();
            if (r.bottom > window.innerHeight - margin) {
                el.style.maxHeight = `${Math.max(120, window.innerHeight - r.top - margin)}px`;
            }
        },

        /** Scroll an option into view without moving focus to it. */
        reveal(id) {
            const el = document.getElementById(id);
            if (el) el.scrollIntoView({ block: 'nearest' });
        },
    };
})();

// ── Toolbars ───────────────────────────────────────────────────────────────────────────────
//
// role="toolbar" promises the WAI-ARIA toolbar model: ONE tab stop, arrow keys between the
// controls, Home/End to the ends. Without it the formatting toolbar was a dozen tab stops the
// author had to walk through on the way to the prose, on every beat.
//
// Delegated from the document, so a toolbar needs no wiring and a button Blazor adds later (Stop,
// while reading aloud) joins the moment focus reaches the toolbar.
(() => {
    function items(toolbar) {
        return Array.from(toolbar.querySelectorAll('button'))
                    .filter(b => !b.disabled && b.offsetParent !== null);
    }

    function rove(toolbar, current) {
        for (const b of items(toolbar)) b.tabIndex = b === current ? 0 : -1;
    }

    document.addEventListener('focusin', e => {
        const toolbar = e.target.closest && e.target.closest('[role="toolbar"]');
        if (toolbar && e.target.tagName === 'BUTTON') rove(toolbar, e.target);
    });

    // Exactly one stop, checked as Tab is pressed — before the browser picks where focus goes.
    // Roving leaves only the last-focused button at 0, so when THAT one went away (Stop, once
    // reading finished) or was disabled, the toolbar had no stop at all and Tab skipped it for good.
    // And a button Blazor adds later arrives at the default 0, making two.
    document.addEventListener('keydown', e => {
        if (e.key !== 'Tab') return;
        for (const toolbar of document.querySelectorAll('[role="toolbar"]')) {
            const list = items(toolbar);
            if (list.length === 0) continue;
            const inside = list.find(b => b === document.activeElement);
            rove(toolbar, inside || list.find(b => b.tabIndex === 0) || list[0]);
        }
    }, true);

    document.addEventListener('keydown', e => {
        const toolbar = e.target.closest && e.target.closest('[role="toolbar"]');
        if (!toolbar) return;
        const list = items(toolbar);
        const at = list.indexOf(e.target);
        if (at < 0) return;

        let next = null;
        switch (e.key) {
            case 'ArrowRight': next = list[(at + 1) % list.length]; break;
            case 'ArrowLeft':  next = list[(at - 1 + list.length) % list.length]; break;
            case 'Home':       next = list[0]; break;
            case 'End':        next = list[list.length - 1]; break;
            default: return;
        }
        e.preventDefault();
        rove(toolbar, next);
        next.focus();
    });
})();

// ── Speaking ───────────────────────────────────────────────────────────────────────────────
//
// Audio arrives as a data: URL over the Blazor circuit rather than from an endpoint. The UI runs
// inside the Hub, so the bytes are already in this process — a route would mean an API key, CORS
// and a second way to get the universe scope wrong, for nothing. A whole chapter would need real
// streaming; a selected paragraph does not.
window.proseSpeech = (() => {
    let current = null;

    // Sentence-chunked speech arrives faster than it can be played: three clips can be synthesized
    // while the first is still speaking. They queue, in order — playing them as they land would
    // talk over itself, and it is the ORDER that carries the meaning.
    let queue = [];
    let draining = false;

    function stop() {
        queue = [];
        draining = false;
        if (!current) return;
        current.pause();
        // Release the element before dropping the reference, or a long clip keeps decoding.
        current.src = '';
        current = null;
    }

    function playOne(src) {
        return new Promise(resolve => {
            const audio = new Audio(src);
            current = audio;
            // Resolve on any ending, including failure: one clip that will not decode must not
            // strand every sentence behind it.
            audio.onended = resolve;
            audio.onerror = resolve;
            audio.play().catch(resolve);
        });
    }

    async function drain() {
        if (draining) return;
        draining = true;
        try {
            while (queue.length > 0) {
                const next = queue.shift();
                await playOne(next);
                // stop() clears the queue and nulls current; that is how barge-in cuts a reply off
                // mid-sentence rather than after it.
                if (current === null) break;
            }
        } finally {
            draining = false;
            current = null;
        }
    }

    return {
        /** Play base64 audio, replacing whatever was already speaking. */
        play(base64, mime) {
            stop();
            queue = [`data:${mime};base64,${base64}`];
            drain();
            return true;
        },

        /** Add a clip to the end of what is already being said. */
        enqueue(base64, mime) {
            queue.push(`data:${mime};base64,${base64}`);
            drain();
            return true;
        },

        /** Barge-in: the author started talking again, or moved on. */
        stop() { stop(); return true; },

        speaking() { return draining || (current !== null && !current.paused); },
    };
})();

// ── Listening ──────────────────────────────────────────────────────────────────────────────
//
// MediaRecorder into memory, then base64 back over the circuit — the mirror of proseSpeech, and
// for the same reason: the Hub is this process, so an upload endpoint would buy nothing but a key
// and a CORS rule. An utterance is seconds long; a whole dictated chapter would need streaming.
//
// Every entry point resolves rather than throws. A microphone can be absent, denied by the host,
// grabbed by another app or unplugged mid-sentence, and an unhandled rejection crossing the JS
// interop boundary tears the Blazor circuit down and freezes the window — so the failure is
// returned as data and the panel says what happened.
window.proseMic = (() => {
    let stream = null;
    let recorder = null;
    let chunks = [];
    let startedAt = 0;

    // Chromium gives webm/opus; the list is ordered by what Whisper handles most happily.
    const PREFERRED = ['audio/webm;codecs=opus', 'audio/webm', 'audio/ogg;codecs=opus', 'audio/mp4'];

    function pickMime() {
        if (typeof MediaRecorder === 'undefined') return null;
        for (const t of PREFERRED) {
            if (MediaRecorder.isTypeSupported && MediaRecorder.isTypeSupported(t)) return t;
        }
        return '';   // let the browser choose; recorder.mimeType still reports what it picked
    }

    function release() {
        if (stream) {
            // Without this the OS microphone indicator stays lit after the turn is over, which is
            // the single most alarming thing a writing app can do.
            stream.getTracks().forEach(t => { try { t.stop(); } catch { } });
        }
        stream = null;
        recorder = null;
    }

    function toBase64(blob) {
        return new Promise(resolve => {
            const reader = new FileReader();
            reader.onerror = () => resolve(null);
            // readAsDataURL, not readAsArrayBuffer + manual encode: the browser's own base64 is
            // faster than any loop here and cannot overflow the argument list on a long clip.
            reader.onloadend = () => {
                const s = String(reader.result || '');
                const comma = s.indexOf(',');
                resolve(comma < 0 ? null : s.slice(comma + 1));
            };
            reader.readAsDataURL(blob);
        });
    }

    return {
        /** True when this browser could record at all — asked before a button is offered. */
        supported() {
            return !!(navigator.mediaDevices && navigator.mediaDevices.getUserMedia
                      && typeof MediaRecorder !== 'undefined');
        },

        /**
         * Begin recording. Resolves { ok, error } — a denied permission is an expected outcome,
         * not an exception.
         */
        async start() {
            if (recorder) return { ok: true, error: null };

            if (!this.supported()) {
                return { ok: false, error: 'This window cannot record audio — MediaRecorder is unavailable.' };
            }

            // Whatever was being read aloud stops the moment the author starts talking. Barge-in
            // is the difference between a conversation and a walkie-talkie.
            try { window.proseSpeech && window.proseSpeech.stop(); } catch { }

            try {
                stream = await navigator.mediaDevices.getUserMedia({
                    audio: {
                        // Dictation into a headset or a laptop mic in a room with a fan. These are
                        // hints, not guarantees, and cost nothing when the device ignores them.
                        echoCancellation: true,
                        noiseSuppression: true,
                        autoGainControl: true,
                    },
                });
            } catch (e) {
                release();
                const name = (e && e.name) || '';
                if (name === 'NotAllowedError' || name === 'SecurityError') {
                    return {
                        ok: false,
                        error: 'The microphone was refused. In the Writer window this is granted by '
                             + 'the app itself — if you are seeing this, the running Writer.exe '
                             + 'predates microphone support and needs a redeploy.',
                    };
                }
                if (name === 'NotFoundError' || name === 'OverconstrainedError') {
                    return { ok: false, error: 'No microphone was found on this machine.' };
                }
                if (name === 'NotReadableError') {
                    return { ok: false, error: 'The microphone is in use by another application.' };
                }
                return { ok: false, error: `The microphone could not be opened: ${e}` };
            }

            try {
                const mimeType = pickMime();
                recorder = mimeType ? new MediaRecorder(stream, { mimeType }) : new MediaRecorder(stream);
                chunks = [];
                recorder.ondataavailable = e => { if (e.data && e.data.size > 0) chunks.push(e.data); };
                recorder.start();
                startedAt = Date.now();
                return { ok: true, error: null };
            } catch (e) {
                release();
                return { ok: false, error: `Recording could not start: ${e}` };
            }
        },

        /**
         * Stop and hand back the audio. Resolves { ok, base64, mime, durationMs, error }.
         * base64 is null when nothing was captured — a tap rather than a hold.
         */
        async stop() {
            if (!recorder) return { ok: false, base64: null, mime: '', durationMs: 0, error: 'Not recording.' };

            const rec = recorder;
            const mime = rec.mimeType || 'audio/webm';
            const durationMs = Date.now() - startedAt;

            const blob = await new Promise(resolve => {
                // onstop fires after the last ondataavailable, which is the only point at which
                // the chunk list is complete.
                rec.onstop = () => resolve(new Blob(chunks, { type: mime }));
                try { rec.stop(); } catch { resolve(null); }
            });

            release();
            chunks = [];

            if (!blob || blob.size === 0) {
                return { ok: false, base64: null, mime, durationMs, error: 'Nothing was recorded.' };
            }

            const base64 = await toBase64(blob);
            return base64
                ? { ok: true, base64, mime, durationMs, error: null }
                : { ok: false, base64: null, mime, durationMs, error: 'The recording could not be read.' };
        },

        /** Abandon a recording without transcribing it. */
        cancel() {
            if (recorder) { try { recorder.stop(); } catch { } }
            release();
            chunks = [];
            return true;
        },

        recording() { return recorder !== null; },
    };
})();

// ── Push to talk ───────────────────────────────────────────────────────────────────────────
//
// A hold-to-speak key, so a follow-up costs no mouse and no click. F4 because the editor is a
// contenteditable — any printable key would be text the author was trying to write — and because
// AreBrowserAcceleratorKeysEnabled is false in the Writer host, so the function keys are free.
//
// Listeners are capturing and the event is consumed, so the key never reaches the prose.
window.proseHotkeys = (() => {
    const PTT = 'F4';

    // Two consumers, because the two keys belong to two components: push-to-talk is the Discuss
    // panel's (it owns the microphone state) and Ctrl+F is the shell's (it owns which book is
    // open). One shared registration would mean one of them routing the other's key through
    // itself, which is how a panel ends up knowing about Find.
    let dotnet = null;
    let shell = null;
    let held = false;

    function release(reason) {
        if (!held) return;
        held = false;
        if (dotnet) dotnet.invokeMethodAsync('PushToTalkUp', reason).catch(() => { });
    }

    function onKeyDown(e) {
        if (e.key !== PTT) return;
        e.preventDefault();
        // Holding a key fires keydown on repeat. Without this guard a two-second hold starts the
        // recorder thirty times.
        if (e.repeat || held) return;
        held = true;
        if (dotnet) dotnet.invokeMethodAsync('PushToTalkDown').catch(() => { });
    }

    function onKeyUp(e) {
        if (e.key !== PTT) return;
        e.preventDefault();
        release('released');
    }

    // Ctrl+F. The browser's own find was taken away with AreBrowserAcceleratorKeysEnabled, and
    // nothing replaced it — so the key reaches the page, and the page owes the author a Find.
    function onFind(e) {
        if (!(e.ctrlKey || e.metaKey) || e.key.toLowerCase() !== 'f' || e.shiftKey || e.altKey) return;
        e.preventDefault();
        if (shell) shell.invokeMethodAsync('OpenFind').catch(() => { });
    }

    // Ctrl+S. Same reasoning as Ctrl+F: the browser's own save (a page-save dialog) was taken
    // away, and nothing replaced it, so a beat with unsaved words and a focused editor had no
    // keyboard route to Save at all — only the mouse.
    function onSave(e) {
        if (!(e.ctrlKey || e.metaKey) || e.key.toLowerCase() !== 's' || e.shiftKey || e.altKey) return;
        e.preventDefault();
        if (shell) shell.invokeMethodAsync('RequestSave').catch(() => { });
    }

    // Alt-tabbing away mid-sentence never delivers the keyup, and the microphone would stay open
    // until the author noticed the indicator. Treat losing the window as letting go.
    function onBlur() { release('lost-focus'); }

    return {
        register(ref) {
            if (dotnet) return true;
            dotnet = ref;
            document.addEventListener('keydown', onKeyDown, true);
            document.addEventListener('keyup', onKeyUp, true);
            window.addEventListener('blur', onBlur);
            return true;
        },

        unregister() {
            document.removeEventListener('keydown', onKeyDown, true);
            document.removeEventListener('keyup', onKeyUp, true);
            window.removeEventListener('blur', onBlur);
            dotnet = null;
            held = false;
            return true;
        },

        registerShell(ref) {
            if (shell) return true;
            shell = ref;
            document.addEventListener('keydown', onFind, true);
            document.addEventListener('keydown', onSave, true);
            return true;
        },

        unregisterShell() {
            document.removeEventListener('keydown', onFind, true);
            document.removeEventListener('keydown', onSave, true);
            shell = null;
            return true;
        },

        key() { return PTT; },
    };
})();

// ── Clipboard ──────────────────────────────────────────────────────────────────────────────
//
// navigator.clipboard is not available on every origin and can be refused outright, so this
// reports failure as a value rather than throwing across the interop boundary — and falls back to
// the old execCommand path, which still works in WebView2 where the async API is gated.
window.proseClipboard = {
    async write(text) {
        try {
            if (navigator.clipboard && window.isSecureContext) {
                await navigator.clipboard.writeText(text);
                return true;
            }
        } catch { /* fall through to the textarea route */ }

        try {
            const scratch = document.createElement('textarea');
            scratch.value = text;
            // Off-screen rather than display:none — a hidden element cannot be selected.
            scratch.style.position = 'fixed';
            scratch.style.left = '-9999px';
            document.body.appendChild(scratch);
            scratch.select();
            const ok = document.execCommand('copy');
            document.body.removeChild(scratch);
            return ok;
        } catch {
            return false;
        }
    }
};
