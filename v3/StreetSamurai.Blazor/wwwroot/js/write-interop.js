window.writeInterop = {

    // ── Auto-resize textareas to fit content ────────────────

    autoResizeAll: function () {
        document.querySelectorAll('textarea.auto-grow').forEach(el => {
            el.style.height = 'auto';
            el.style.height = el.scrollHeight + 'px';
        });
    },

    initAutoGrow: function () {
        // Run once on page load
        this.autoResizeAll();
        // Watch for new textareas via MutationObserver
        if (this._growObserver) this._growObserver.disconnect();
        this._growObserver = new MutationObserver(() => this.autoResizeAll());
        this._growObserver.observe(document.body, { childList: true, subtree: true });
        // Also resize on input
        document.addEventListener('input', (e) => {
            if (e.target.tagName === 'TEXTAREA' && e.target.classList.contains('auto-grow')) {
                e.target.style.height = 'auto';
                e.target.style.height = e.target.scrollHeight + 'px';
            }
        });
    },

    // ── Plain textarea interop ──────────────────────────────

    getSelection: function (id) {
        const el = document.getElementById(id);
        if (!el) return { start: 0, end: 0, text: "" };
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
        if (el.contentEditable === 'true') { el.innerText = text; }
        else { el.value = text; el.dispatchEvent(new Event("input", { bubbles: true })); }
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
        el.dispatchEvent(new Event("input", { bubbles: true }));
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

    downloadBlob: function (filename, base64, mimeType) {
        const byteChars = atob(base64);
        const byteArray = new Uint8Array(byteChars.length);
        for (let i = 0; i < byteChars.length; i++) byteArray[i] = byteChars.charCodeAt(i);
        const blob = new Blob([byteArray], { type: mimeType });
        const url = URL.createObjectURL(blob);
        const a = document.createElement("a");
        a.href = url;
        a.download = filename;
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
        URL.revokeObjectURL(url);
    },

    openPrintWindow: function (html) {
        const win = window.open('', '_blank');
        if (!win) return false;
        win.document.write(html);
        win.document.close();
        return true;
    },

    // ── Rich editor (contenteditable) ───────────────────────

    richEditor: {
        _ref: null,
        _syncDebounce: null,
        _linkDebounce: null,
        _entityIndex: [],
        _isLinking: false,   // guard to prevent input→link→input loop
        _savedRange: null,

        init: function (id, dotNetRef, initialHtml) {
            const el = document.getElementById(id);
            if (!el) return;
            this._ref = dotNetRef;
            el.innerHTML = initialHtml || '';

            const self = this;

            // Debounced content sync + auto entity linking
            el.addEventListener('input', () => {
                if (self._isLinking) return; // don't re-fire during linking

                // Quick sync (500ms)
                clearTimeout(self._syncDebounce);
                self._syncDebounce = setTimeout(() => {
                    if (self._ref) self._ref.invokeMethodAsync('OnRichContentChanged', el.innerHTML, el.innerText);
                }, 500);

                // Auto-linking disabled — user can right-click > Ask for context instead
                // clearTimeout(self._linkDebounce);
                // self._linkDebounce = setTimeout(() => { self._autoLink(id); }, 2000);
            });

            // Track cursor for tag insertion
            el.addEventListener('keyup', () => self._saveCaret(el));
            el.addEventListener('mouseup', () => self._saveCaret(el));

            // Track selection changes for Read Selected availability
            if (!self._selectionListener) {
                self._selectionDebounce = null;
                self._selectionListener = true;
                document.addEventListener('selectionchange', () => {
                    clearTimeout(self._selectionDebounce);
                    self._selectionDebounce = setTimeout(() => {
                        const sel = window.getSelection();
                        const text = sel ? sel.toString() : '';
                        if (self._ref) {
                            self._ref.invokeMethodAsync('OnSelectionChanged', text);
                        }
                    }, 150);
                });
            }

            // Entity click delegation
            el.addEventListener('click', (e) => {
                const link = e.target.closest('.entity-link');
                if (link && self._ref) {
                    e.preventDefault();
                    const entityId = link.getAttribute('data-entity-id');
                    if (entityId) self._ref.invokeMethodAsync('OnEntityClicked', entityId);
                }
            });

            // Paste handler — decode unicode escapes like \u0022 \u0027 etc
            el.addEventListener('paste', (e) => {
                const clipText = (e.clipboardData || window.clipboardData).getData('text');
                if (clipText && /\\u[0-9a-fA-F]{4}/.test(clipText)) {
                    e.preventDefault();
                    const decoded = clipText.replace(/\\u([0-9a-fA-F]{4})/g, (_, hex) =>
                        String.fromCharCode(parseInt(hex, 16)));
                    document.execCommand('insertText', false, decoded);
                }
            });

            // Right-click context menu
            el.addEventListener('contextmenu', (e) => {
                e.preventDefault();
                if (self._ref) {
                    self._ref.invokeMethodAsync('OnEditorContextMenu', e.clientY, e.clientX);
                }
            });
        },

        _saveCaret: function (el) {
            const sel = window.getSelection();
            if (sel.rangeCount > 0 && el.contains(sel.anchorNode)) {
                this._savedRange = sel.getRangeAt(0).cloneRange();
            }
        },

        _restoreCaret: function () {
            if (!this._savedRange) return;
            try {
                const sel = window.getSelection();
                sel.removeAllRanges();
                sel.addRange(this._savedRange);
            } catch (e) { /* range may be stale */ }
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
            el.scrollTop = el.scrollHeight;
        },

        buildEntityIndex: function (entities) {
            this._entityIndex = entities.sort((a, b) => b.name.length - a.name.length);
        },

        // ── Auto-link: runs on timer, saves/restores cursor ──

        _autoLink: function (id) {
            return; // Auto-linking disabled — use right-click > Ask instead
            const el = document.getElementById(id);
            if (!el || this._entityIndex.length === 0) return;

            // Save cursor
            this._saveCaret(el);

            // Set guard so the DOM mutation doesn't trigger another input cycle
            this._isLinking = true;
            this._linkTextNodes(el);
            this._isLinking = false;

            // Restore cursor
            this._restoreCaret();
        },

        // Called from Blazor button (manual trigger)
        highlightEntities: function (id) {
            this._autoLink(id);
            // Sync back
            const el = document.getElementById(id);
            if (el && this._ref) {
                this._ref.invokeMethodAsync('OnRichContentChanged', el.innerHTML, el.innerText);
            }
        },

        _linkTextNodes: function (el) {
            const walker = document.createTreeWalker(el, NodeFilter.SHOW_TEXT);
            const replacements = [];

            while (walker.nextNode()) {
                const textNode = walker.currentNode;
                if (textNode.parentElement.closest('.entity-link, .chapter-break, .elevenlabs-tag, .facet-tag')) continue;

                for (const ent of this._entityIndex) {
                    if (ent.name.length < 3) continue;
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

            // Deduplicate overlapping
            const filtered = [];
            replacements.sort((a, b) => a.start - b.start || b.end - a.end);
            let lastEnd = -1, lastNode = null;
            for (const rep of replacements) {
                if (rep.node !== lastNode) { lastEnd = -1; lastNode = rep.node; }
                if (rep.start >= lastEnd) { filtered.push(rep); lastEnd = rep.end; }
            }

            // Apply in reverse
            filtered.sort((a, b) => {
                if (a.node !== b.node) return 0;
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
                } catch (e) {}
            }
        },

        // ── Insert tag at cursor position ──

        insertAtCursor: function (id, text) {
            const el = document.getElementById(id);
            if (!el) return;

            el.focus();
            if (this._savedRange) {
                const sel = window.getSelection();
                sel.removeAllRanges();
                sel.addRange(this._savedRange);
            }

            // Use execCommand for undo-friendly insertion
            document.execCommand('insertText', false, text);

            // Save new position
            this._saveCaret(el);

            // Notify Blazor
            if (this._ref) {
                this._ref.invokeMethodAsync('OnRichContentChanged', el.innerHTML, el.innerText);
            }
        },

        // ── Formatting commands (undo-friendly via execCommand) ──

        formatBlock: function (id, tag) {
            const el = document.getElementById(id);
            if (!el) return;
            el.focus();
            document.execCommand('formatBlock', false, '<' + tag + '>');
            this._notifyChange(el);
        },

        formatInline: function (id, command) {
            const el = document.getElementById(id);
            if (!el) return;
            el.focus();
            document.execCommand(command, false, null);
            this._notifyChange(el);
        },

        insertImage: function (id, src, alt) {
            const el = document.getElementById(id);
            if (!el) return;
            el.focus();
            if (this._savedRange) {
                const sel = window.getSelection();
                sel.removeAllRanges();
                sel.addRange(this._savedRange);
            }
            document.execCommand('insertHTML', false,
                '<img src="' + src + '" alt="' + (alt || '') + '" style="max-width:100%;border-radius:4px;margin:8px 0;" />');
            this._notifyChange(el);
        },

        insertHr: function (id) {
            const el = document.getElementById(id);
            if (!el) return;
            el.focus();
            document.execCommand('insertHorizontalRule', false, null);
            this._notifyChange(el);
        },

        _notifyChange: function (el) {
            if (this._ref) {
                this._ref.invokeMethodAsync('OnRichContentChanged', el.innerHTML, el.innerText);
            }
        },

        toggleLinks: function (id, show) {
            const el = document.getElementById(id);
            if (!el) return;
            if (show) el.classList.remove('hide-links');
            else el.classList.add('hide-links');
        },

        // ── Markdown formatting (for textarea/Md mode) ──

        mdWrapSelection: function (id, prefix, suffix) {
            const el = document.getElementById(id);
            if (!el) return '';
            const start = el.selectionStart;
            const end = el.selectionEnd;
            const text = el.value;
            const selected = text.substring(start, end);
            const replacement = prefix + selected + suffix;
            el.value = text.substring(0, start) + replacement + text.substring(end);
            // Position cursor after the replacement
            el.selectionStart = start + prefix.length;
            el.selectionEnd = start + prefix.length + selected.length;
            el.focus();
            el.dispatchEvent(new Event('input', { bubbles: true }));
            return el.value;
        },

        mdPrefixLine: function (id, prefix) {
            const el = document.getElementById(id);
            if (!el) return '';
            const start = el.selectionStart;
            const text = el.value;
            // Find the start of the current line
            const lineStart = text.lastIndexOf('\n', start - 1) + 1;
            // Find existing heading prefix to replace
            const lineEnd = text.indexOf('\n', start);
            const line = text.substring(lineStart, lineEnd === -1 ? text.length : lineEnd);
            const stripped = line.replace(/^#{1,6}\s*|^>\s*/, '');
            const newLine = prefix + stripped;
            el.value = text.substring(0, lineStart) + newLine + text.substring(lineEnd === -1 ? text.length : lineEnd);
            el.selectionStart = lineStart + prefix.length;
            el.selectionEnd = lineStart + newLine.length;
            el.focus();
            el.dispatchEvent(new Event('input', { bubbles: true }));
            return el.value;
        },

        stripToPlainText: function (id) {
            const el = document.getElementById(id);
            if (!el) return '';
            const clone = el.cloneNode(true);
            clone.querySelectorAll('.chapter-break').forEach(cb => {
                cb.replaceWith(document.createTextNode('\n\n======\n\n'));
            });
            clone.querySelectorAll('.entity-link').forEach(span => {
                span.replaceWith(document.createTextNode(span.textContent));
            });
            clone.querySelectorAll('.elevenlabs-tag').forEach(span => {
                const tag = span.getAttribute('data-tag') || '';
                span.replaceWith(document.createTextNode(tag));
            });
            return clone.innerText;
        },

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
            const p = document.createElement('p');
            p.innerHTML = '<br>';
            el.appendChild(p);
        }
    }
};

window.__dictScrollActive = function () {
    const active = document.querySelector('.dict-items .list-item.active');
    if (active) active.scrollIntoView({ block: 'nearest', behavior: 'smooth' });
};

// Auto-scroll active dict list item whenever its class changes (Blazor DOM diffing)
(function () {
    let observer;
    function setup() {
        if (observer) observer.disconnect();
        const el = document.querySelector('.dict-items');
        if (!el) return;
        observer = new MutationObserver(function () {
            const a = el.querySelector('.list-item.active');
            if (a) a.scrollIntoView({ block: 'nearest', behavior: 'smooth' });
        });
        observer.observe(el, { subtree: true, attributes: true, attributeFilter: ['class'] });
    }
    document.addEventListener('DOMContentLoaded', setup);
    document.addEventListener('enhancedload', setup);
})();

// ── Profile avatar interop ──────────────────────────────────────────────────
window.profileInterop = {
    loadPreview: function (dataUrl, canvasId) {
        const canvas = document.getElementById(canvasId);
        if (!canvas) return;
        const ctx = canvas.getContext('2d');
        const img = new Image();
        img.onload = function () {
            const size = Math.min(img.width, img.height);
            const sx = (img.width - size) / 2;
            const sy = (img.height - size) / 2;
            canvas.width = 128;
            canvas.height = 128;
            ctx.clearRect(0, 0, 128, 128);
            ctx.save();
            ctx.beginPath();
            ctx.arc(64, 64, 64, 0, Math.PI * 2);
            ctx.clip();
            ctx.drawImage(img, sx, sy, size, size, 0, 0, 128, 128);
            ctx.restore();
        };
        img.src = dataUrl;
    },
    capture: function (canvasId) {
        const canvas = document.getElementById(canvasId);
        if (!canvas) return null;
        const out = document.createElement('canvas');
        out.width = 64; out.height = 64;
        const ctx = out.getContext('2d');
        ctx.beginPath();
        ctx.arc(32, 32, 32, 0, Math.PI * 2);
        ctx.clip();
        ctx.drawImage(canvas, 0, 0, 128, 128, 0, 0, 64, 64);
        return out.toDataURL('image/png');
    }
};

// ── Mobile dict drawer: swipe up/down or hold-drag to resize ────────────────
(function () {
    var drag = null;
    var suppressNextClick = false;

    function getList() { return document.querySelector('.dict-list'); }
    function fabPos() { if (window._updateNavFabPos) window._updateNavFabPos(); }

    function openList() {
        var l = getList(); if (!l) return;
        l.style.transition = ''; l.style.transform = '';
        l.classList.add('dict-list-open');
        fabPos();
    }
    function closeList() {
        var l = getList(); if (!l) return;
        l.style.transition = ''; l.style.transform = '';
        l.classList.remove('dict-list-open');
        fabPos();
    }
    function isOpen() { var l = getList(); return l && l.classList.contains('dict-list-open'); }

    // Tap: open via handle tab, close via handle tab or outside click
    document.addEventListener('click', function (e) {
        if (suppressNextClick) { suppressNextClick = false; return; }
        var list = getList(); if (!list) return;
        if (isOpen()) {
            // Click outside the list → close
            if (!list.contains(e.target)) { closeList(); return; }
            // Click in top handle zone (::before pill area) → close
            var rect = list.getBoundingClientRect();
            if (e.clientY < rect.top + 30) closeList();
        } else {
            if (list.contains(e.target)) openList();
        }
    });

    // Touch start — only track if touch is on the list
    document.addEventListener('touchstart', function (e) {
        var list = getList(); if (!list || !list.contains(e.target)) return;
        var t = e.touches[0];
        var maxY = list.getBoundingClientRect().height - 40;
        var startY = isOpen() ? 0 : maxY;
        var m = list.style.transform && list.style.transform.match(/translateY\(([^p]+)px\)/);
        if (m) startY = parseFloat(m[1]);
        drag = { clientY: t.clientY, startY: startY, curY: startY, maxY: maxY, moved: false, time: Date.now() };
    }, { passive: true });

    // Touch move — follow finger, suppress CSS transition during drag
    document.addEventListener('touchmove', function (e) {
        if (!drag) return;
        var list = getList(); if (!list) return;
        var delta = e.touches[0].clientY - drag.clientY;
        var newY = Math.max(0, Math.min(drag.maxY, drag.startY + delta));
        drag.curY = newY;
        if (!drag.moved && Math.abs(delta) > 8) drag.moved = true;
        if (!drag.moved) return;
        list.style.transition = 'none';
        list.style.transform = 'translateY(' + newY + 'px)';
    }, { passive: true });

    // Touch end — quick flick → open/close; slow drag → snap by position
    document.addEventListener('touchend', function (e) {
        if (!drag) return;
        var state = drag; drag = null;
        var list = getList(); if (!list) return;
        if (!state.moved) return; // tap handled by click
        suppressNextClick = true;
        var totalDy = e.changedTouches[0].clientY - state.clientY; // positive = downward
        var elapsed = Date.now() - state.time;
        var velocity = Math.abs(totalDy) / elapsed; // px/ms
        // Fast flick: direction wins
        if (velocity > 0.4 && elapsed < 350) {
            if (totalDy < 0) openList(); else closeList();
            return;
        }
        // Slow drag: snap closed if more than 45% toward bottom, else open
        if (state.curY > state.maxY * 0.45) closeList(); else openList();
    }, { passive: true });

    function setup() {
        var list = getList();
        if (list) { list.classList.remove('dict-list-open'); list.style.transform = ''; list.style.transition = ''; }
        fabPos();
    }
    document.addEventListener('DOMContentLoaded', setup);
    document.addEventListener('enhancedload', setup);
})();

// ── Search viewport sync ──────────────────────────────────────────────
window.setupSearchSync = function (dotNetRef) {
    var mq = window.matchMedia('(min-width: 768px)');
    mq.addEventListener('change', function (e) {
        if (e.matches) {
            // Went desktop: copy overlay query → desktop search bar, close overlay
            var overlayInput = document.querySelector('.search-overlay-input');
            var desktopInput = document.querySelector('.navbar-collapse .topnav-search');
            if (overlayInput && desktopInput && overlayInput.value) {
                desktopInput.value = overlayInput.value;
                desktopInput.dispatchEvent(new Event('input', { bubbles: true }));
            }
            dotNetRef.invokeMethodAsync('ForceClose').catch(function () {});
        } else {
            // Went mobile: copy desktop search bar → overlay, clear bar
            var desktopInput = document.querySelector('.navbar-collapse .topnav-search');
            var q = desktopInput ? desktopInput.value : '';
            if (desktopInput) {
                desktopInput.value = '';
                desktopInput.dispatchEvent(new Event('input', { bubbles: true }));
            }
            if (q) dotNetRef.invokeMethodAsync('OpenWithQuery', q).catch(function () {});
        }
    });
};

