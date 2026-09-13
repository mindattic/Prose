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
    }
};
