// ════════════════════════════════════════════════════════════════════════
// Pin-when-short footer
// ════════════════════════════════════════════════════════════════════════
// If the document is shorter than the viewport, pin any element with
// class `.pin-when-short` to the bottom of the viewport so the page
// doesn't end with awkward dead space. As soon as content overflows we
// let the element flow normally — a pinned footer over scrollable content
// would cover the last line.
//
// Companion CSS lives in cbg/frontpage.css (`.pin-when-short.pinned`).
//
// Opt-in: `<footer class="pin-when-short">...</footer>`
// Multiple targets are supported; each toggles independently.

(function () {
    'use strict';

    function pinAll() {
        var hasScrollbar = document.documentElement.scrollHeight > window.innerHeight;
        var targets = document.querySelectorAll('.pin-when-short');
        for (var i = 0; i < targets.length; i++) {
            targets[i].classList.toggle('pinned', !hasScrollbar);
        }
    }

    function start() {
        pinAll();
        window.addEventListener('resize', pinAll);
        // Re-evaluate after fonts/images settle (late reflow common on mindattic.com).
        if (document.fonts && document.fonts.ready) {
            document.fonts.ready.then(pinAll).catch(function () {});
        }
        // Catch any DOM growth that pushes content past the viewport later.
        var lastH = document.documentElement.scrollHeight;
        new MutationObserver(function () {
            var h = document.documentElement.scrollHeight;
            if (h !== lastH) { lastH = h; pinAll(); }
        }).observe(document.body, { childList: true, subtree: true });
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', start);
    } else {
        start();
    }
})();
