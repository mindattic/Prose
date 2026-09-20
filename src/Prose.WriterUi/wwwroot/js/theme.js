// Theme switching, matching the convention every other MindAttic Blazor app uses: a lowercase
// data-theme attribute on <html>, dark as the bare :root default, light as an attribute override.
// The attribute is also stamped by an inline script in the page head BEFORE the stylesheets load —
// without that, a light-theme user sees one frame of dark on every launch.
//
// Lives in its own file because more than one Hub-served page needs it (/writer and /repo) and the
// second page should not have to load the whole prose editor to remember a colour preference.
window.proseWriter = {
    key: 'prose.writer.theme',

    getTheme() {
        try {
            const stored = localStorage.getItem(this.key);
            return stored === 'light' || stored === 'dark' ? stored : 'dark';
        } catch {
            // Private browsing / blocked site data. A theme is a preference, not a feature.
            return 'dark';
        }
    },

    setTheme(theme) {
        const value = theme === 'light' ? 'light' : 'dark';
        document.documentElement.setAttribute('data-theme', value);
        try { localStorage.setItem(this.key, value); } catch { /* see above */ }
        return value;
    },

    // ── text size ──────────────────────────────────────────────────────────
    //
    // Every font size in this UI is absolute, so the browser's minimum-font-size setting cannot
    // move any of it — and the Writer host has AreBrowserAcceleratorKeysEnabled off, so Ctrl+plus
    // does nothing either. That left Ctrl+scroll, which is a mouse gesture and therefore not a
    // resize mechanism at all for someone who does not use one. This is the in-app control that
    // WCAG 1.4.4 actually wants.

    scaleKey: 'prose.writer.scale',

    // 200% is the criterion's bar; below 0.9 the 24px hit targets stop making sense.
    minScale: 0.9,
    maxScale: 2.0,

    getScale() {
        try {
            const stored = parseFloat(localStorage.getItem(this.scaleKey) || '1');
            if (!isFinite(stored)) return 1;
            return Math.min(this.maxScale, Math.max(this.minScale, stored));
        } catch {
            return 1;
        }
    },

    setScale(scale) {
        const value = Math.min(this.maxScale, Math.max(this.minScale, Number(scale) || 1));
        // Set as a custom property rather than by rewriting the tokens: everything in the
        // stylesheets already reads --text-* and those are defined in terms of --ui-scale.
        document.documentElement.style.setProperty('--ui-scale', String(value));
        try { localStorage.setItem(this.scaleKey, String(value)); } catch { /* see above */ }
        return value;
    },

    /** Applied on load, before the author touches anything. */
    applyScale() { return this.setScale(this.getScale()); }
};
