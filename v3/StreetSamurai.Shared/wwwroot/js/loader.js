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

// Unified loader — used for both app startup and page navigation.
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
        if (el) el.style.display = 'none';
    };

    // Auto-start timer on initial load
    start = performance.now();
    timer = setInterval(tick, 1);

    // Auto-hide: DOMContentLoaded fires after full HTML is parsed (reliable for static SSR).
    // By this point the server-rendered .app-shell div is guaranteed to be in the DOM.
    document.addEventListener('DOMContentLoaded', function() {
        window.__loaderHide();
    });

    // Safety net: force-hide after 10s in case something goes wrong
    safetyTimer = setTimeout(function() {
        window.__loaderHide();
    }, 10000);
})();
