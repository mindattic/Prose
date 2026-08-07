// UserTimeout — idle timeout warning + auto-logout. Idempotent. Arms ONLY when #ut-root exists
// (the .razor renders it solely inside <Authorized>), so it no-ops while unauthenticated.
// Config from #ut-root data attributes: data-idle-ms (default 1800000 = 30m, tie to
// AuthSessionOptions.IdleTimeout), data-warn-ms (default 60000). Expiry submits the native
// antiforgery logout form (#ut-logout-form) via requestSubmit — never fetch.
(function () {
    'use strict';
    if (window.__userTimeoutInited) return;
    window.__userTimeoutInited = true;

    function init() {
        var root = document.getElementById('ut-root');
        if (!root) return;   // unauthenticated -> no timer

        var idleMs = parseInt(root.dataset.idleMs, 10) || 1800000;
        var warnMs = parseInt(root.dataset.warnMs, 10) || 60000;
        var overlay = document.querySelector('.ut-overlay');
        var countEl = document.querySelector('.ut-count');
        var logoutForm = document.getElementById('ut-logout-form');
        var last = Date.now();
        var lastReset = 0;

        function resetActivity() {
            var now = Date.now();
            if (now - lastReset < 1000) return;   // throttle ~1/sec
            lastReset = now;
            last = now;
            if (overlay) overlay.classList.remove('ut-show');
        }

        ['mousemove', 'keydown', 'click', 'scroll', 'touchstart'].forEach(function (ev) {
            window.addEventListener(ev, resetActivity, { passive: true });
        });

        var stay = document.querySelector('.ut-stay');
        if (stay) stay.addEventListener('click', function () { last = Date.now(); lastReset = Date.now(); if (overlay) overlay.classList.remove('ut-show'); });
        var logoutNow = document.querySelector('.ut-logout');
        if (logoutNow && logoutForm) logoutNow.addEventListener('click', function () { logoutForm.requestSubmit(); });

        setInterval(function () {
            var elapsed = Date.now() - last;   // keeps counting in backgrounded tabs
            if (elapsed >= idleMs) {
                if (logoutForm) logoutForm.requestSubmit();
                return;
            }
            if (elapsed >= idleMs - warnMs) {
                if (overlay) overlay.classList.add('ut-show');
                if (countEl) countEl.textContent = Math.ceil((idleMs - elapsed) / 1000).toString();
            }
        }, 1000);
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
})();
