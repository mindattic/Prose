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

    async function playBeatsInSequence(items) {
        if (!Array.isArray(items) || items.length === 0) return;
        stopSequence();
        cancelled = false;

        for (let i = 0; i < items.length; i++) {
            if (cancelled) return;
            const it = items[i];
            if (!it || !it.audioUrl) continue;
            await playOne(it.audioUrl);
            if (cancelled) return;
            const pause = Math.max(0, Number(it.pauseMs) || 0);
            if (pause > 0 && i < items.length - 1) {
                await sleep(pause);
            }
        }
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

    window.streetsamurai.playBeatsInSequence = playBeatsInSequence;
    window.streetsamurai.stopSequence         = stopSequence;
    window.streetsamurai.readInput            = readInput;
    window.streetsamurai.getCursorPosition    = getCursorPosition;
    window.streetsamurai.wrapSelection        = wrapSelection;
})();
