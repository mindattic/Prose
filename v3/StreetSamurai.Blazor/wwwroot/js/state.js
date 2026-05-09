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
        // Ergonomic helper: save scroll on every page hide so navigations
        // away always have a fresh value to restore.
        installAutoScroll: function (key) {
            window.addEventListener('pagehide', function () { saveScroll(key); }, { once: true });
            window.addEventListener('beforeunload', function () { saveScroll(key); });
        },
    };
})();
