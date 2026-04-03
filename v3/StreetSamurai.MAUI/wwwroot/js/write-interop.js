window.writeInterop = {

    // ── Plain textarea interop (kept for backwards compat) ──────────

    getSelection: function (id) {
        const el = document.getElementById(id);
        if (!el) return { start: 0, end: 0, text: "" };
        // contenteditable
        if (el.contentEditable === 'true') {
            const sel = window.getSelection();
            return { start: 0, end: 0, text: sel.toString() };
        }
        return {
            start: el.selectionStart,
            end: el.selectionEnd,
            text: el.value.substring(el.selectionStart, el.selectionEnd)
        };
    },

    setText: function (id, text) {
        const el = document.getElementById(id);
        if (!el) return;
        if (el.contentEditable === 'true') {
            el.innerText = text;
        } else {
            el.value = text;
            el.dispatchEvent(new Event("input", { bubbles: true }));
        }
    },

    focusEnd: function (id) {
        const el = document.getElementById(id);
        if (!el) return;
        el.focus();
        if (el.contentEditable === 'true') {
            const range = document.createRange();
            range.selectNodeContents(el);
            range.collapse(false);
            const sel = window.getSelection();
            sel.removeAllRanges();
            sel.addRange(range);
        } else {
            el.selectionStart = el.value.length;
            el.selectionEnd = el.value.length;
        }
    },

    replaceRange: function (id, start, end, text) {
        const el = document.getElementById(id);
        if (!el) return "";
        const before = el.value.substring(0, start);
        const after = el.value.substring(end);
        el.value = before + text + after;
        el.selectionStart = start;
        el.selectionEnd = start + text.length;
        el.dispatchEvent(new Event("input", { bubbles: true }));
        el.dispatchEvent(new Event("change", { bubbles: true }));
        return el.value;
    },

    downloadText: function (filename, text) {
        const blob = new Blob([text], { type: "text/plain" });
        const url = URL.createObjectURL(blob);
        const a = document.createElement("a");
        a.href = url;
        a.download = filename;
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
        URL.revokeObjectURL(url);
    },

    // ── Rich editor (contenteditable) ───────────────────────────

    richEditor: {
        _ref: null,
        _debounce: null,
        _entityIndex: [],

        init: function (id, dotNetRef, initialHtml) {
            const el = document.getElementById(id);
            if (!el) return;
            this._ref = dotNetRef;
            el.innerHTML = initialHtml || '';

            // Debounced content sync back to Blazor
            el.addEventListener('input', () => {
                clearTimeout(this._debounce);
                this._debounce = setTimeout(() => {
                    if (this._ref) this._ref.invokeMethodAsync('OnRichContentChanged', el.innerHTML, el.innerText);
                }, 500);
            });

            // Entity click delegation
            el.addEventListener('click', (e) => {
                const link = e.target.closest('.entity-link');
                if (link && this._ref) {
                    e.preventDefault();
                    const entityId = link.getAttribute('data-entity-id');
                    if (entityId) this._ref.invokeMethodAsync('OnEntityClicked', entityId);
                }
            });
        },

        getPlainText: function (id) {
            const el = document.getElementById(id);
            return el ? el.innerText : '';
        },

        getHtml: function (id) {
            const el = document.getElementById(id);
            return el ? el.innerHTML : '';
        },

        setHtml: function (id, html) {
            const el = document.getElementById(id);
            if (el) el.innerHTML = html;
        },

        appendHtml: function (id, html) {
            const el = document.getElementById(id);
            if (!el) return;
            el.innerHTML += html;
            // Scroll to bottom
            el.scrollTop = el.scrollHeight;
        },

        // Build the entity name index from graph data (called once from Blazor)
        buildEntityIndex: function (entities) {
            // entities: [{name, id, nodeType}, ...]
            // Sort by name length desc for greedy matching
            this._entityIndex = entities.sort((a, b) => b.name.length - a.name.length);
        },

        // Scan all text nodes and wrap entity mentions in colored spans
        highlightEntities: function (id) {
            const el = document.getElementById(id);
            if (!el || this._entityIndex.length === 0) return;

            const walker = document.createTreeWalker(el, NodeFilter.SHOW_TEXT);
            const replacements = [];

            while (walker.nextNode()) {
                const textNode = walker.currentNode;
                // Skip if inside an entity span or chapter header
                if (textNode.parentElement.closest('.entity-link, .chapter-break')) continue;

                for (const ent of this._entityIndex) {
                    if (ent.name.length < 3) continue; // skip very short names
                    const escaped = ent.name.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
                    const regex = new RegExp('\\b' + escaped + '\\b', 'gi');
                    let match;
                    while ((match = regex.exec(textNode.textContent)) !== null) {
                        replacements.push({
                            node: textNode,
                            start: match.index,
                            end: match.index + match[0].length,
                            matchText: match[0],
                            entity: ent
                        });
                    }
                }
            }

            // Deduplicate overlapping matches (keep longest)
            const filtered = [];
            replacements.sort((a, b) => a.start - b.start || b.end - a.end);
            let lastEnd = -1;
            let lastNode = null;
            for (const rep of replacements) {
                if (rep.node !== lastNode) { lastEnd = -1; lastNode = rep.node; }
                if (rep.start >= lastEnd) {
                    filtered.push(rep);
                    lastEnd = rep.end;
                }
            }

            // Apply in reverse order so offsets stay valid
            filtered.sort((a, b) => {
                if (a.node !== b.node) return 0; // different nodes, order doesn't matter
                return b.start - a.start;
            });

            for (const rep of filtered) {
                try {
                    const range = document.createRange();
                    range.setStart(rep.node, rep.start);
                    range.setEnd(rep.node, rep.end);

                    const span = document.createElement('span');
                    span.className = 'entity-link entity-' + rep.entity.nodeType;
                    span.setAttribute('data-entity-id', rep.entity.id);
                    span.setAttribute('title', rep.entity.nodeType + ': ' + rep.entity.name);
                    span.textContent = rep.matchText;

                    range.deleteContents();
                    range.insertNode(span);
                } catch (e) {
                    // Range may be invalid if DOM changed during iteration
                }
            }

            if (this._ref) {
                this._ref.invokeMethodAsync('OnRichContentChanged', el.innerHTML, el.innerText);
            }
        },

        // Strip all entity markup — return plain text with paragraph breaks
        stripToPlainText: function (id) {
            const el = document.getElementById(id);
            if (!el) return '';

            const clone = el.cloneNode(true);
            // Remove chapter break divs but keep their text
            clone.querySelectorAll('.chapter-break').forEach(cb => {
                const title = cb.getAttribute('data-title') || '';
                const text = document.createTextNode('\n\n--- ' + title + ' ---\n\n');
                cb.replaceWith(text);
            });
            // Remove entity spans but keep text
            clone.querySelectorAll('.entity-link').forEach(span => {
                span.replaceWith(document.createTextNode(span.textContent));
            });
            return clone.innerText;
        },

        // Insert a chapter break at the end
        insertChapterBreak: function (id, chapterNum, title) {
            const el = document.getElementById(id);
            if (!el) return;

            const divider = document.createElement('div');
            divider.className = 'chapter-break';
            divider.contentEditable = 'false';
            divider.setAttribute('data-chapter', chapterNum);
            divider.setAttribute('data-title', title);
            divider.innerHTML = '<hr style="border-color: #dc3545; margin: 2rem 0 0.5rem;"><span class="chapter-title" style="color: #dc3545; font-weight: bold; font-size: 1.1rem;">' +
                title + '</span><hr style="border-color: #333; margin: 0.5rem 0 2rem;">';

            el.appendChild(divider);

            // Add a new editable paragraph after the break
            const p = document.createElement('p');
            p.innerHTML = '<br>';
            el.appendChild(p);
        }
    }
};
