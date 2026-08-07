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

// Unified loader — small corner indicator. The overlay element is now a
// subtle top-right spinner (id="app-loader"); the millisecond counter and
// full-screen darken-everything overlay were retired 2026-05-09.
(function() {
    var timeout = null;
    var safetyTimer = null;

    function getEl() { return document.getElementById('app-loader'); }

    window.__loaderShow = function(delay) {
        clearTimeout(timeout);
        if (delay > 0) {
            timeout = setTimeout(function() {
                var el = getEl();
                if (el) el.style.display = 'block';
            }, delay);
        } else {
            var el = getEl();
            if (el) el.style.display = 'block';
        }
    };

    window.__loaderHide = function() {
        clearTimeout(timeout);
        clearTimeout(safetyTimer);
        timeout = null;
        var el = getEl();
        if (el) el.style.display = 'none';
    };

    // Auto-hide: DOMContentLoaded fires after full HTML is parsed (reliable
    // for static SSR). By this point the server-rendered .app-shell div is
    // guaranteed to be in the DOM, so any boot-time spinner can come down.
    document.addEventListener('DOMContentLoaded', function() {
        window.__loaderHide();
    });

    // Safety net: force-hide after 10 s in case something goes wrong
    safetyTimer = setTimeout(function() {
        window.__loaderHide();
    }, 10000);
})();
