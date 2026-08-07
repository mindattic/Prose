// ════════════════════════════════════════════════════════════════════════
// Pin-when-short footer
// ════════════════════════════════════════════════════════════════════════
// If the document is shorter than the viewport, pin any element with
// class `.pin-when-short` to the bottom of the viewport so the page
// doesn't end with awkward dead space. As soon as content overflows we
// let the element flow normally — a pinned footer over scrollable content
// would cover the last line.
//
// Companion CSS lives alongside this file in pin-footer.css (`.pin-when-short.pinned`).
//
// Opt-in: `<footer class="pin-when-short">...</footer>`
// Multiple targets are supported; each toggles independently.

(function () {
    'use strict';

    // Idempotency: marker-block syncs can splice this script into a page that
    // already runs it. Bail out so we don't stack duplicate observers/listeners.
    if (window.__pinFooterInited) return;
    window.__pinFooterInited = true;

    function pinAll() {
        var hasScrollbar = document.documentElement.scrollHeight > window.innerHeight;
        var targets = document.querySelectorAll('.pin-when-short');
        for (var i = 0; i < targets.length; i++) {
            targets[i].classList.toggle('pinned', !hasScrollbar);
        }
    }

    // Coalesce rapid triggers (mutation bursts, resize storms) into one rAF.
    var rafId = 0;
    function schedulePin() {
        if (rafId) return;
        rafId = requestAnimationFrame(function () {
            rafId = 0;
            pinAll();
        });
    }

    function start() {
        pinAll();
        window.addEventListener('resize', schedulePin);
        // Re-evaluate after fonts/images settle (late reflow common on mindattic.com).
        if (document.fonts && document.fonts.ready) {
            document.fonts.ready.then(pinAll).catch(function () {});
        }
        // Catch any DOM growth that pushes content past the viewport later.
        new MutationObserver(schedulePin).observe(document.body, { childList: true, subtree: true });
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', start);
    } else {
        start();
    }
})();
