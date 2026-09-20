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

    // The selected passage and what surrounds it, each serialized with the SAME function that
    // produces the beat's stored markup. Deliberately no character offsets: turning a DOM range
    // into a position in the serialized beat would mean a second serializer living here and
    // drifting from that one. The server strips these three strings with the code it already
    // uses everywhere else and finds the passage itself — one implementation, and the fiddly part
    // stays where it is tested.
    function selectionSpan() {
        const sel = window.getSelection();
        if (!sel || sel.rangeCount === 0 || sel.isCollapsed) return null;

        const range = sel.getRangeAt(0);
        if (!host.contains(range.commonAncestorContainer)) return null;

        const quote = serialize(rangeFragment(range));
        if (!quote.trim()) return null;

        const before = document.createRange();
        before.setStart(host, 0);
        before.setEnd(range.startContainer, range.startOffset);

        const after = document.createRange();
        after.setStart(range.endContainer, range.endOffset);
        after.setEnd(host, host.childNodes.length);

        return {
            quote: quote,
            prefix: serialize(rangeFragment(before)),
            suffix: serialize(rangeFragment(after)),
        };
    }

    // serialize() walks childNodes, so a DocumentFragment works as-is.
    function rangeFragment(range) {
        return range.cloneContents();
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

                // Not a chip. A right-click over a selection is the author asking about that
                // passage; a right-click over nothing is left to the browser.
                const span = selectionSpan();
                if (!span) return;
                e.preventDefault();
                dotnet.invokeMethodAsync('OnDiscussRequested', span.quote, span.prefix, span.suffix);
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
            restoreTo = document.activeElement;
            trapped = dialog;
            document.addEventListener('keydown', onKeyDown, true);
            const items = focusable(dialog);
            (items[0] || dialog).focus();
        },

        close() {
            document.removeEventListener('keydown', onKeyDown, true);
            trapped = null;
            if (restoreTo && typeof restoreTo.focus === 'function') restoreTo.focus();
            restoreTo = null;
        },
    };
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

    let dotnet = null;
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

        key() { return PTT; },
    };
})();
