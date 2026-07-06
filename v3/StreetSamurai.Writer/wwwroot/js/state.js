// ── State persistence: localStorage-backed scrollY, selection, and field
// state per page. Called from Razor pages via JSInterop:
//
//   ssState.set(key, value)        // string-stringified value
//   ssState.get(key)               // returns value or null
//   ssState.saveScroll(key)        // current window.scrollY
//   ssState.restoreScroll(key)     // jumps to saved scrollY (if any)
//   ssState.saveCursor(key, sel)   // textarea selectionStart/End
//   ssState.restoreCursor(key, el) // sets cursor on element
//
// Keys are page-scoped strings the consumer chooses
// (e.g. "write:beat-cursor:<beatId>"). Errors are swallowed so
// localStorage being disabled never breaks the page.

(function () {
    if (window.ssState) return;

    function safeGet(key) {
        try { return localStorage.getItem(key); } catch { return null; }
    }
    function safeSet(key, value) {
        try { localStorage.setItem(key, value); } catch { /* quota or disabled */ }
    }
    function safeRemove(key) {
        try { localStorage.removeItem(key); } catch { }
    }

    function set(key, value) {
        if (value === null || value === undefined) safeRemove(key);
        else safeSet(key, String(value));
    }
    function get(key) {
        return safeGet(key);
    }
    function setJson(key, value) {
        try { safeSet(key, JSON.stringify(value)); } catch { }
    }
    function getJson(key) {
        var raw = safeGet(key);
        if (raw == null) return null;
        try { return JSON.parse(raw); } catch { return null; }
    }

    function saveScroll(key) {
        if (!key) return;
        safeSet(key, String(window.scrollY || 0));
    }
    function restoreScroll(key) {
        if (!key) return;
        var v = safeGet(key);
        if (v == null) return;
        var y = parseInt(v, 10);
        if (!isNaN(y)) {
            // Defer to the next frame so the DOM is laid out before we scroll.
            requestAnimationFrame(function () { window.scrollTo(0, y); });
        }
    }

    function saveCursor(key, sel) {
        if (!key || !sel) return;
        safeSet(key, sel.start + ',' + sel.end);
    }
    function restoreCursor(key, el) {
        if (!key || !el) return;
        var raw = safeGet(key);
        if (!raw) return;
        var parts = raw.split(',');
        if (parts.length !== 2) return;
        var s = parseInt(parts[0], 10);
        var e = parseInt(parts[1], 10);
        if (isNaN(s) || isNaN(e)) return;
        try {
            el.focus();
            el.setSelectionRange(s, e);
        } catch { }
    }

    function captureCursor(el) {
        if (!el || typeof el.selectionStart !== 'number') return null;
        return { start: el.selectionStart, end: el.selectionEnd };
    }

    // Convenience wrappers that locate an element by id and delegate to the
    // capture/restore helpers — saves the caller from an eval round-trip.
    function saveCursorById(elementId, key) {
        var el = document.getElementById(elementId);
        if (!el) return;
        var c = captureCursor(el);
        if (c) saveCursor(key, c);
    }
    function restoreCursorById(elementId, key) {
        var el = document.getElementById(elementId);
        if (!el) return;
        restoreCursor(key, el);
    }

    // Scroll persistence for arbitrary scrollable containers (not just window).
    // Pass a selector + storage key; the helpers read/write the element's
    // scrollTop. Useful for sidebars and beat lists where window scrolling
    // doesn't apply.
    function saveScrollOf(selector, key) {
        try {
            var el = document.querySelector(selector);
            if (!el) return;
            safeSet(key, String(el.scrollTop || 0));
        } catch { }
    }
    function restoreScrollOf(selector, key) {
        try {
            var el = document.querySelector(selector);
            if (!el) return;
            var raw = safeGet(key);
            if (raw == null) return;
            var y = parseInt(raw, 10);
            if (!isNaN(y)) {
                requestAnimationFrame(function () { el.scrollTop = y; });
            }
        } catch { }
    }
    // One-shot listener install — debounced so heavy scroll storms don't
    // hammer localStorage. Caller provides a unique key; previous installs
    // on the same selector are not removed (caller responsibility).
    function autoSaveScrollOf(selector, key) {
        try {
            var el = document.querySelector(selector);
            if (!el) return;
            var t = null;
            el.addEventListener('scroll', function () {
                if (t) clearTimeout(t);
                t = setTimeout(function () { saveScrollOf(selector, key); }, 250);
            }, { passive: true });
        } catch { }
    }

    window.ssState = {
        set: set,
        get: get,
        setJson: setJson,
        getJson: getJson,
        saveScroll: saveScroll,
        restoreScroll: restoreScroll,
        saveCursor: saveCursor,
        restoreCursor: restoreCursor,
        captureCursor: captureCursor,
        saveCursorById: saveCursorById,
        restoreCursorById: restoreCursorById,
        saveScrollOf: saveScrollOf,
        restoreScrollOf: restoreScrollOf,
        autoSaveScrollOf: autoSaveScrollOf,
        // Ergonomic helper: save scroll on every page hide so navigations
        // away always have a fresh value to restore.
        installAutoScroll: function (key) {
            window.addEventListener('pagehide', function () { saveScroll(key); }, { once: true });
            window.addEventListener('beforeunload', function () { saveScroll(key); });
        },
    };
})();
