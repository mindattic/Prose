// ── Toasts: bottom-center, level-styled, auto-dismiss ─────────────────────
//
// The ToastNotifier C# service calls window.ssToasts.show(level, code, message)
// from any Razor page that catches an error or wants to surface a notice. Errors
// stay on screen longer (8s) than info/success (3s) so they can't be missed.
//
// Vanilla JS — no NuGet package dependency, no toast-library version drift,
// nothing that needs the running app to restart to pick up.

(function () {
    if (window.ssToasts) return; // hot-reload safety

    function ensureContainer() {
        var c = document.getElementById('ss-toasts-container');
        if (c) return c;
        c = document.createElement('div');
        c.id = 'ss-toasts-container';
        document.body.appendChild(c);
        return c;
    }

    function show(level, code, message) {
        var container = ensureContainer();
        var t = document.createElement('div');
        var lvl = (level || 'info').toLowerCase();
        t.className = 'ss-toast ss-toast--' + lvl;

        var icon = document.createElement('span');
        icon.className = 'ss-toast-icon';
        icon.textContent = lvl === 'error' ? '⚠' : lvl === 'warn' ? '!' : lvl === 'success' ? '✓' : 'ℹ';
        t.appendChild(icon);

        var body = document.createElement('div');
        body.className = 'ss-toast-body';

        if (code) {
            var codeEl = document.createElement('div');
            codeEl.className = 'ss-toast-code';
            codeEl.textContent = code;
            body.appendChild(codeEl);
        }

        var msg = document.createElement('div');
        msg.className = 'ss-toast-msg';
        msg.textContent = message || '';
        body.appendChild(msg);

        t.appendChild(body);

        var close = document.createElement('button');
        close.className = 'ss-toast-close';
        close.setAttribute('aria-label', 'Dismiss');
        close.textContent = '×';
        close.addEventListener('click', function () { dismiss(t); });
        t.appendChild(close);

        container.appendChild(t);
        // Trigger transition by removing the entry class on the next frame.
        requestAnimationFrame(function () { t.classList.add('ss-toast--shown'); });

        // Errors linger; everything else self-dismisses quickly.
        var ttl = lvl === 'error' ? 8000 : lvl === 'warn' ? 5000 : 3000;
        setTimeout(function () { dismiss(t); }, ttl);
    }

    function dismiss(t) {
        if (!t || t.dataset.dismissed === '1') return;
        t.dataset.dismissed = '1';
        t.classList.remove('ss-toast--shown');
        t.classList.add('ss-toast--leaving');
        setTimeout(function () { if (t.parentNode) t.parentNode.removeChild(t); }, 300);
    }

    window.ssToasts = { show: show, dismiss: dismiss };
})();
