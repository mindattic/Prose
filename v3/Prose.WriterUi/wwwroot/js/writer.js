// window.proseWriter (theme switching) moved to theme.js — /repo needs it too and should not have
// to load the whole editor for it. Writer.razor loads theme.js before this file.

// ── The prose editor ───────────────────────────────────────────────────────────────────────
//
// A contenteditable whose DOM is serialized back to the beat's own markup on every input. Entity
// tags render as contenteditable="false" spans, which is what makes them atomic: the caret steps
// over them, typing cannot land inside one, and changing a link is a deliberate act through the
// modal rather than a stray keystroke that silently repoints a guid.
window.proseEditor = (() => {
    let host = null;          // the contenteditable element
    let dotnet = null;        // DotNetObjectReference back into the Blazor component
    let contextTarget = null; // the .ent span the author last right-clicked

    const BLOCK_TAGS = new Set(['P', 'DIV']);

    function escapeAttr(s) {
        return String(s).replace(/&/g, '&amp;').replace(/"/g, '&quot;')
                        .replace(/</g, '&lt;').replace(/>/g, '&gt;');
    }

    // Serialize one node's children, wrapping the result in whatever emphasis markers the
    // surrounding tags call for. Emphasis is re-emitted as markers rather than preserved as HTML
    // because the markers are what the database, the exporters and every other reader understand.
    function serializeNode(node) {
        if (node.nodeType === Node.TEXT_NODE) return node.nodeValue;
        if (node.nodeType !== Node.ELEMENT_NODE) return '';

        const el = node;
        const tag = el.tagName;

        if (tag === 'BR') return '\n';

        if (el.classList && el.classList.contains('ent')) {
            const guid = el.getAttribute('data-guid') || '';
            const repo = el.getAttribute('data-repo') || '';
            // The inner text only — an entity tag wraps a plain surface name, never nested markup.
            const inner = el.textContent || '';
            if (!guid) return inner;
            return `<entity repo="${escapeAttr(repo)}" guid="${escapeAttr(guid)}">${inner}</entity>`;
        }

        let inner = '';
        for (const child of el.childNodes) inner += serializeNode(child);

        // Whitespace-only content must not gain markers, or an empty <em> left behind by the
        // browser after a deletion becomes a stray "**" in the prose.
        if (inner.trim() === '') return inner;

        switch (tag) {
            case 'STRONG':
            case 'B':      return `**${inner}**`;
            case 'EM':
            case 'I':      return `*${inner}*`;
            case 'U':      return `<u>${inner}</u>`;
            case 'S':
            case 'STRIKE':
            case 'DEL':    return `~~${inner}~~`;
            default:       return inner;
        }
    }

    function serialize(el) {
        const parts = [];
        for (const child of el.childNodes) {
            if (child.nodeType === Node.ELEMENT_NODE && BLOCK_TAGS.has(child.tagName)) {
                parts.push(serializeNode(child));
            } else {
                // Loose inline content at the top level (browsers produce this after some
                // deletions) is its own paragraph rather than being dropped.
                const loose = serializeNode(child);
                if (loose.trim() !== '') parts.push(loose);
            }
        }
        // contenteditable emits U+00A0 for a typed run of spaces and at some edit
        // boundaries. Left alone it enters the prose invisibly: the text stops matching what
        // the exporters, the entity scanner and a plain grep see, and every save then reports
        // a change that is not one.
        return parts.map(p => p.replace(/ /g, ' ').trimEnd())
                    .filter(p => p.trim() !== '')
                    .join('\n\n');
    }

    function currentEntitySpan() {
        const sel = window.getSelection();
        if (!sel || sel.rangeCount === 0) return null;
        let node = sel.getRangeAt(0).commonAncestorContainer;
        while (node && node !== host) {
            if (node.nodeType === Node.ELEMENT_NODE && node.classList.contains('ent')) return node;
            node = node.parentNode;
        }
        return null;
    }

    function notifyChanged() {
        if (dotnet) dotnet.invokeMethodAsync('OnEditorInput', serialize(host));
    }

    return {
        init(element, dotNetRef) {
            host = element;
            dotnet = dotNetRef;

            // Emit real tags (<b>, <i>) instead of <span style="font-weight:bold">, so
            // serializeNode has something it can recognise.
            try { document.execCommand('styleWithCSS', false, false); } catch { /* older engines */ }

            host.addEventListener('input', notifyChanged);

            host.addEventListener('contextmenu', e => {
                let node = e.target;
                while (node && node !== host) {
                    if (node.nodeType === Node.ELEMENT_NODE && node.classList.contains('ent')) {
                        e.preventDefault();
                        contextTarget = node;
                        dotnet.invokeMethodAsync('OnEntityContextMenu',
                            node.getAttribute('data-guid') || '',
                            node.getAttribute('data-repo') || '',
                            node.textContent || '');
                        return;
                    }
                    node = node.parentNode;
                }
            });

            // Left-click an entity chip to open its wiki page in the default browser.
            //
            // Deliberately NOT preventDefault(): the chip is contenteditable="false" inside an
            // editable host, and the author still needs the click to place a caret beside it.
            // Opening a page is a side effect of the click, not a replacement for it.
            //
            // Ignored while a selection is being dragged — a click that ends a drag across a chip
            // is the author selecting text, not asking to navigate.
            host.addEventListener('click', e => {
                const sel = window.getSelection();
                if (sel && !sel.isCollapsed) return;

                let node = e.target;
                while (node && node !== host) {
                    if (node.nodeType === Node.ELEMENT_NODE && node.classList.contains('ent')) {
                        dotnet.invokeMethodAsync('OnEntityClick',
                            node.getAttribute('data-guid') || '',
                            node.getAttribute('data-repo') || '');
                        return;
                    }
                    node = node.parentNode;
                }
            });

            host.addEventListener('keydown', e => {
                if (!(e.ctrlKey || e.metaKey)) return;
                const key = e.key.toLowerCase();
                if (key === 'b' || key === 'i' || key === 'u') {
                    e.preventDefault();
                    this.format(key === 'b' ? 'bold' : key === 'i' ? 'italic' : 'underline');
                }
            });
        },

        setHtml(element, html) {
            // Only replace the DOM when the text genuinely differs from what is on screen.
            // Rewriting innerHTML on every render would move the caret to the start of the
            // document on every keystroke.
            if (element.dataset.skipNext === '1') { element.dataset.skipNext = '0'; return; }
            element.innerHTML = html;
        },

        /** True when the author has selected something that is safe to act on. */
        selection() {
            const sel = window.getSelection();
            if (!sel || sel.rangeCount === 0 || sel.isCollapsed) return { text: '', ok: false };
            if (!host || !host.contains(sel.getRangeAt(0).commonAncestorContainer)) return { text: '', ok: false };
            return { text: sel.toString(), ok: true };
        },

        format(command) {
            if (!host) return;
            host.focus();
            document.execCommand(command, false, null);
            notifyChanged();
        },

        insert(text) {
            if (!host) return;
            host.focus();
            document.execCommand('insertText', false, text);
            notifyChanged();
        },

        /** Wrap the current selection in a new entity link. */
        wrapSelection(guid, repo) {
            const sel = window.getSelection();
            if (!sel || sel.rangeCount === 0 || sel.isCollapsed) return false;

            const range = sel.getRangeAt(0);
            if (!host.contains(range.commonAncestorContainer)) return false;

            // Refuse to nest. An entity tag inside an entity tag has no meaning, and the
            // serializer would flatten it in a way the author did not ask for.
            let node = range.commonAncestorContainer;
            while (node && node !== host) {
                if (node.nodeType === Node.ELEMENT_NODE && node.classList.contains('ent')) return false;
                node = node.parentNode;
            }

            const span = document.createElement('span');
            span.className = 'ent';
            span.contentEditable = 'false';
            span.setAttribute('data-guid', guid);
            span.setAttribute('data-repo', repo || '');
            span.textContent = range.toString();

            range.deleteContents();
            range.insertNode(span);
            sel.removeAllRanges();

            notifyChanged();
            return true;
        },

        /** Apply the entity modal's edits to the span that was right-clicked. */
        applyEntity(guid, repo, text) {
            const target = contextTarget || currentEntitySpan();
            if (!target) return false;
            target.setAttribute('data-guid', guid);
            target.setAttribute('data-repo', repo || '');
            if (text !== null && text !== undefined && text !== '') target.textContent = text;
            notifyChanged();
            return true;
        },

        /**
         * The Markdown-mode equivalent of a format button: wrap the textarea's selection in
         * literal markers (or insert at the caret when nothing is selected). Blazor's @bind fires
         * on 'input', so dispatching that event is what tells the component the text changed.
         */
        wrapTextarea(el, before, after) {
            if (!el) return;
            const start = el.selectionStart, end = el.selectionEnd;
            const selected = el.value.substring(start, end);
            el.setRangeText(before + selected + after, start, end, 'end');
            // Put the caret between the markers when wrapping an empty selection, so the next
            // keystroke lands inside the emphasis rather than after it.
            if (start === end && after.length > 0) {
                const caret = start + before.length;
                el.setSelectionRange(caret, caret);
            }
            el.dispatchEvent(new Event('input', { bubbles: true }));
            el.focus();
        },

        /** Unwrap the right-clicked span back to plain prose, keeping its words. */
        removeEntity() {
            const target = contextTarget || currentEntitySpan();
            if (!target) return false;
            target.replaceWith(document.createTextNode(target.textContent || ''));
            contextTarget = null;
            notifyChanged();
            return true;
        }
    };
})();
