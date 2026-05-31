// strand-interop.js — JS helpers for the unified strand writer page.
//
// Two surfaces:
//   1. streetsamurai.playBeatsInSequence(items) — play an ordered list of
//      beat audio files with precise digital silence between them. Items
//      shape: [{ audioUrl, pauseMs }]. The pauseMs is honoured AFTER the
//      audio of that item plays, before the next item starts.
//   2. streetsamurai.readInput(id) — read the current value of a DOM
//      input element by id. Used by the gap editor to grab the user's
//      typed number without round-tripping through @bind (which would
//      need an explicit lose-focus trigger).
//
// Stop button: the player exposes streetsamurai.stopSequence() so the UI
// can interrupt mid-playback.

(function () {
    if (!window.streetsamurai) window.streetsamurai = {};

    let activeAudio = null;
    let activeTimeout = null;
    let cancelled = false;

    function stopSequence() {
        cancelled = true;
        if (activeAudio) {
            try { activeAudio.pause(); } catch (e) { }
            activeAudio = null;
        }
        if (activeTimeout) {
            clearTimeout(activeTimeout);
            activeTimeout = null;
        }
    }

    // The currently-installed Blazor callback, if any. Cleared when the
    // sequence ends or is cancelled so we don't hold a reference longer
    // than the playback session.
    let progressCallback = null;

    async function playBeatsInSequence(items, callbackRef) {
        if (!Array.isArray(items) || items.length === 0) return;
        stopSequence();
        cancelled = false;
        progressCallback = callbackRef ?? null;

        for (let i = 0; i < items.length; i++) {
            if (cancelled) return;
            const it = items[i];
            if (!it || !it.audioUrl) continue;
            // Resume-from-here: tell C# which beat is about to play so the
            // strand row's LastPlayedBeatId / LastPlayedSec stay current.
            // Fire-and-forget — a failed invoke shouldn't stall playback.
            if (progressCallback && it.beatId) {
                try { progressCallback.invokeMethodAsync('OnBeatStarted', it.beatId); }
                catch (e) { /* ignore — JS continues regardless */ }
            }
            await playOne(it.audioUrl);
            if (cancelled) return;
            const pause = Math.max(0, Number(it.pauseMs) || 0);
            if (pause > 0 && i < items.length - 1) {
                await sleep(pause);
            }
        }
        progressCallback = null;
    }

    function playOne(url) {
        return new Promise((resolve, reject) => {
            const audio = new Audio(url);
            audio.preload = 'auto';
            activeAudio = audio;
            audio.addEventListener('ended', () => { activeAudio = null; resolve(); });
            audio.addEventListener('error', (e) => { activeAudio = null; resolve(); /* swallow — keep sequence going */ });
            // Some browsers reject autoplay; user has just clicked Play
            // selected so the gesture should be in scope.
            const p = audio.play();
            if (p && typeof p.catch === 'function') {
                p.catch(err => { activeAudio = null; resolve(); });
            }
        });
    }

    function sleep(ms) {
        return new Promise(resolve => {
            activeTimeout = setTimeout(() => { activeTimeout = null; resolve(); }, ms);
        });
    }

    function readInput(id) {
        const el = document.getElementById(id);
        return el ? el.value : '';
    }

    /// Read the current selectionStart of a textarea so the server-side
    /// split-at-cursor handler knows where to split the beat. Falls back
    /// to -1 when the element is not a focusable input.
    function getCursorPosition(id) {
        const el = document.getElementById(id);
        if (!el || typeof el.selectionStart !== 'number') return -1;
        return el.selectionStart;
    }

    /// Wrap the current selection in the beat-edit textarea with the given
    /// marker on both sides (e.g. "**" for bold, "*" for italic, "__" for
    /// underline, "~~" for strikethrough). If there is no selection, drops
    /// an empty marker pair at the caret and parks the caret in the middle.
    /// Dispatches an `input` event afterwards so Blazor's @bind picks up
    /// the new value without waiting for blur.
    function wrapSelection(id, marker) {
        const el = document.getElementById(id);
        if (!el) return;
        const start = typeof el.selectionStart === 'number' ? el.selectionStart : el.value.length;
        const end   = typeof el.selectionEnd   === 'number' ? el.selectionEnd   : start;
        const before   = el.value.substring(0, start);
        const selected = el.value.substring(start, end);
        const after    = el.value.substring(end);
        el.value = before + marker + selected + marker + after;
        // Park the caret so a follow-up keystroke continues to type inside
        // the wrap (or, if there was a real selection, re-selects it).
        const caretStart = start + marker.length;
        const caretEnd   = end + marker.length;
        try {
            el.setSelectionRange(caretStart, caretEnd);
            el.focus();
        } catch (e) { /* some browsers throw on detached elements */ }
        // Blazor's @bind:event="oninput" listens for this.
        el.dispatchEvent(new Event('input', { bubbles: true }));
    }

    // ── Live Broadcast clip player ───────────────────────────────────────
    // Unlike playBeatsInSequence (a fixed playlist), Live Broadcast is driven
    // beat-by-beat from C#: the server renders the look-ahead buffer and
    // advances the cursor, while JS just plays ONE clip and resolves its
    // promise when the clip ends. That lets the C# loop await each beat,
    // render ahead between beats, and pick up edits before the cursor arrives.
    let broadcastAudio = null;
    let broadcastResolve = null;
    function playClip(url) {
        stopClip();
        return new Promise((resolve) => {
            broadcastResolve = resolve;
            const audio = new Audio(url);
            audio.preload = 'auto';
            broadcastAudio = audio;
            const done = () => {
                if (broadcastAudio === audio) broadcastAudio = null;
                const r = broadcastResolve; broadcastResolve = null;
                if (r) r();
            };
            audio.addEventListener('ended', done);
            audio.addEventListener('error', done); // swallow — keep the broadcast moving
            const p = audio.play();
            if (p && typeof p.catch === 'function') p.catch(() => done());
        });
    }
    // Halt the current clip AND resolve its pending promise so the awaiting
    // C# loop unwinds (it checks its own cancellation flag after the await).
    function stopClip() {
        if (broadcastAudio) { try { broadcastAudio.pause(); } catch (e) { } broadcastAudio = null; }
        const r = broadcastResolve; broadcastResolve = null;
        if (r) r();
    }
    window.streetsamurai.playClip = playClip;
    window.streetsamurai.stopClip = stopClip;

    window.streetsamurai.playBeatsInSequence = playBeatsInSequence;
    window.streetsamurai.stopSequence         = stopSequence;
    window.streetsamurai.readInput            = readInput;
    window.streetsamurai.getCursorPosition    = getCursorPosition;
    window.streetsamurai.wrapSelection        = wrapSelection;

    // Per-instance Escape handler: each call returns a token that the caller
    // can pass to uninstallPageEscape on teardown. Priority order matches the
    // old eval-installed handler — modal cancel first, then selection-bar
    // clear — but the listener is now tracked and removable, so navigating
    // away from /strand/X doesn't leak the handler onto the next page.
    let escapeRegistry = new Map();
    let escapeNextToken = 1;
    function installPageEscape() {
        const token = (escapeNextToken++).toString();
        const handler = function (e) {
            if (e.key !== 'Escape') return;
            const cancel = document.querySelector('.modal.d-block .modal-footer .btn-outline-secondary');
            if (cancel) { cancel.click(); return; }
            const clear = document.querySelector('[data-cy="clear-selection"]');
            if (clear) { clear.click(); return; }
        };
        document.addEventListener('keydown', handler);
        escapeRegistry.set(token, handler);
        return token;
    }
    function uninstallPageEscape(token) {
        if (!token) return;
        const handler = escapeRegistry.get(token);
        if (handler) {
            document.removeEventListener('keydown', handler);
            escapeRegistry.delete(token);
        }
    }
    window.streetsamurai.installPageEscape    = installPageEscape;
    window.streetsamurai.uninstallPageEscape  = uninstallPageEscape;

    // Drag-reorder support: HTML5 dragstart sets the dragged beat id +
    // owning strand id on the dataTransfer payload; the drop handler reads
    // both, rejects cross-strand drops (MoveBeatAsync only re-orders within
    // one strand — cross-strand re-parenting is a future feature), and calls
    // back into Blazor on accept. Pure DOM — no Blazor event-marshalling
    // overhead per pointermove.
    function attachBeatDragHandlers(rootId, callbackRef) {
        const root = document.getElementById(rootId);
        if (!root || !callbackRef) return;
        // Idempotent: re-attaching is a no-op (the handlers live on the
        // delegated root listener, no per-row state to clean up).
        if (root.__ssDragAttached) return;
        root.__ssDragAttached = true;

        function sameStrand(rowA, rowB) {
            const a = rowA.getAttribute('data-strand-id') || '';
            const b = rowB.getAttribute('data-strand-id') || '';
            return a && b && a === b;
        }

        root.addEventListener('dragstart', function (e) {
            const row = e.target.closest('[data-beat-id]');
            if (!row) return;
            const beatId   = row.getAttribute('data-beat-id');
            const strandId = row.getAttribute('data-strand-id') || '';
            if (!beatId) return;
            e.dataTransfer.setData('text/x-beat-id', beatId);
            e.dataTransfer.setData('text/x-strand-id', strandId);
            e.dataTransfer.effectAllowed = 'move';
            row.classList.add('beat-dragging');
        });
        root.addEventListener('dragend', function (e) {
            const row = e.target.closest('[data-beat-id]');
            if (row) row.classList.remove('beat-dragging');
            root.querySelectorAll('.beat-drop-target, .beat-drop-blocked').forEach(el =>
                el.classList.remove('beat-drop-target', 'beat-drop-blocked'));
        });
        root.addEventListener('dragover', function (e) {
            const row = e.target.closest('[data-beat-id]');
            if (!row) return;
            const dragging = root.querySelector('.beat-dragging');
            const ok = dragging ? sameStrand(dragging, row) : true;
            // Always preventDefault so the browser shows "no drop allowed"
            // cursor instead of bouncing the drag back — we want feedback,
            // not silence, when the user attempts a cross-strand drop.
            e.preventDefault();
            e.dataTransfer.dropEffect = ok ? 'move' : 'none';
            // Toggle the drop-target ring on the row currently hovered;
            // cross-strand drops get a different ring colour via .beat-drop-blocked.
            root.querySelectorAll('.beat-drop-target, .beat-drop-blocked').forEach(el => {
                if (el !== row) el.classList.remove('beat-drop-target', 'beat-drop-blocked');
            });
            row.classList.toggle('beat-drop-target', ok);
            row.classList.toggle('beat-drop-blocked', !ok);
        });
        root.addEventListener('dragleave', function (e) {
            const row = e.target.closest('[data-beat-id]');
            if (row && !row.contains(e.relatedTarget))
                row.classList.remove('beat-drop-target', 'beat-drop-blocked');
        });
        root.addEventListener('drop', function (e) {
            const row = e.target.closest('[data-beat-id]');
            if (!row) return;
            e.preventDefault();
            const draggedId       = e.dataTransfer.getData('text/x-beat-id');
            const draggedStrandId = e.dataTransfer.getData('text/x-strand-id');
            const targetId        = row.getAttribute('data-beat-id');
            const targetStrandId  = row.getAttribute('data-strand-id') || '';
            row.classList.remove('beat-drop-target', 'beat-drop-blocked');
            if (!draggedId || !targetId || draggedId === targetId) return;
            // Cross-strand drop: tell Blazor so it can toast the user with
            // a clear "not supported yet" hint instead of silently dropping.
            if (draggedStrandId && targetStrandId && draggedStrandId !== targetStrandId) {
                try { callbackRef.invokeMethodAsync('OnCrossStrandDropRejected'); }
                catch (err) { /* ignore */ }
                return;
            }
            try { callbackRef.invokeMethodAsync('OnBeatDropped', draggedId, targetId); }
            catch (err) { /* Blazor side logs */ }
        });
    }
    window.streetsamurai.attachBeatDragHandlers = attachBeatDragHandlers;

    // Scroll a beat into view by guid. No-op when the row hasn't rendered
    // yet (page still loading). Used by the resume-from-here feature.
    function scrollBeatIntoView(beatGuid) {
        const id = 'beat-' + beatGuid.replace(/-/g, '');
        const el = document.getElementById(id);
        if (el) el.scrollIntoView({ behavior: 'smooth', block: 'center' });
    }
    window.streetsamurai.scrollBeatIntoView   = scrollBeatIntoView;

    /// Set a textarea's height to exactly fit its content. No padding,
    /// no extra blank rows. Called on every input + once on open so
    /// the textarea matches the visual height of the prose it replaced
    /// (Blazor's `rows` attribute is too coarse — it counts text-rows,
    /// not wrapped-display-rows, which leaves blank space at the bottom).
    function autoSizeTextarea(id) {
        const el = document.getElementById(id);
        if (!el) return;
        // Reset to 0 first so shrink-on-delete works (scrollHeight only
        // grows as content does; without resetting it stays at the peak).
        el.style.height = '0px';
        // scrollHeight is the content's intrinsic height; +2px is a fudge
        // for browser sub-pixel rounding so the bottom line isn't clipped.
        el.style.height = (el.scrollHeight + 2) + 'px';
    }
    window.streetsamurai.autoSizeTextarea     = autoSizeTextarea;

    /// Focus an element by id. Replaces the old eval(`...focus()`) shim
    /// (which was CSP-hostile and didn't compose with strict policies).
    /// Safe to call before the element exists — returns false in that case.
    function focusElement(id) {
        const el = document.getElementById(id);
        if (!el) return false;
        el.focus();
        return true;
    }
    window.streetsamurai.focusElement         = focusElement;
})();
