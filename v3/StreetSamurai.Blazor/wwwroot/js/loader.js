// Focus helper — used by SearchOverlay to focus the input after mount
window.focusElement = function(el) { if (el) { el.focus(); } };

// Shared Google Maps API loader — deduplicates script injection across meridianMap + geoMap
window.__gmapsLoad = function(apiKey, cb) {
    if (window.google && window.google.maps) { if (cb) cb(); return; }
    window.__gmapsCbs = window.__gmapsCbs || [];
    if (cb) window.__gmapsCbs.push(cb);
    if (window.__gmapsLoading) return;
    window.__gmapsLoading = true;
    var s = document.createElement('script');
    s.src = 'https://maps.googleapis.com/maps/api/js?key=' + apiKey + '&callback=__gmapsReady';
    s.async = true; s.defer = true;
    document.head.appendChild(s);
};
window.__gmapsReady = function() {
    (window.__gmapsCbs || []).forEach(function(cb) { try { cb(); } catch(e) {} });
    window.__gmapsCbs = [];
};

// Unified loader — navigation overlay that appears after 500ms on slow page transitions.
// The overlay element must exist in the HTML with id="app-loader" and child id="loader-ms".
(function() {
    var timer = null;
    var timeout = null;
    var safetyTimer = null;
    var start = 0;

    function getEl() { return document.getElementById('app-loader'); }
    function getMs() { return document.getElementById('loader-ms'); }

    function tick() {
        var ms = getMs();
        if (ms) ms.textContent = String(Math.floor(performance.now() - start)).padStart(4, '0');
    }

    window.__loaderShow = function(delay) {
        clearTimeout(timeout);
        clearInterval(timer);
        start = performance.now();
        if (delay > 0) {
            timeout = setTimeout(function() {
                var el = getEl();
                if (el) el.style.display = 'flex';
                timer = setInterval(tick, 1);
            }, delay);
        } else {
            var el = getEl();
            if (el) el.style.display = 'flex';
            timer = setInterval(tick, 1);
        }
    };

    window.__loaderHide = function() {
        clearTimeout(timeout);
        clearTimeout(safetyTimer);
        clearInterval(timer);
        timer = null;
        timeout = null;
        var el = getEl();
        var ms = getMs();
        if (el) el.style.display = 'none';
        if (ms) ms.textContent = '0000';
    };

    // ── Navigation loader ─────────────────────────────────────────────────
    // Intercepts Blazor interactive-server navigation via history.pushState patch.
    // Shows the loader only if navigation takes longer than 500ms.
    // Hides when DOM mutations in the main content area stop for 150ms (page rendered).
    (function() {
        var navigating = false;
        var hideDebounce = null;

        function navStart() {
            navigating = true;
            window.__loaderShow(500);
        }

        function navMaybeEnd() {
            if (!navigating) return;
            clearTimeout(hideDebounce);
            hideDebounce = setTimeout(function() {
                navigating = false;
                window.__loaderHide();
            }, 150);
        }

        // Blazor interactive server uses history.pushState for navigation
        var origPush = history.pushState.bind(history);
        history.pushState = function() {
            origPush.apply(history, arguments);
            navStart();
        };

        // Back/forward browser navigation
        window.addEventListener('popstate', navStart);

        // Blazor enhanced navigation fallback (fires in SSR/static mode)
        document.addEventListener('enhancednavigationstart', navStart);
        document.addEventListener('enhancedload', function() { if (navigating) navMaybeEnd(); });

        // Watch the main content area — when DOM mutations settle, the new page is rendered
        var mo = new MutationObserver(navMaybeEnd);

        function setup() {
            var main = document.querySelector('.topnav-main') || document.querySelector('main');
            if (main) mo.observe(main, { childList: true, subtree: true });
        }

        document.addEventListener('DOMContentLoaded', setup);

        // Safety: hide loader after 10s regardless
        document.addEventListener('DOMContentLoaded', function() {
            safetyTimer = setTimeout(window.__loaderHide, 10000);
        });
    })();
})();
