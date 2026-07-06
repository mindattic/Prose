// Focus helper — used by SearchOverlay to focus the input after mount
window.focusElement = function(el) { if (el) { el.focus(); } };

// Terminal auto-scroll — used by Script Runner in Utilities
window.scrollTerminalToBottom = function(id) {
    var el = document.getElementById(id);
    if (el) el.scrollTop = el.scrollHeight;
};

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
    // If the script fails to load (404, bad key, offline), release the latch so
    // a later __gmapsLoad call can retry instead of hanging forever. Drop the
    // dead <script> and clear the queued callbacks — they'll never be invoked.
    s.onerror = function () {
        window.__gmapsLoading = false;
        window.__gmapsCbs = [];
        if (s.parentNode) s.parentNode.removeChild(s);
    };
    document.head.appendChild(s);
};
window.__gmapsReady = function() {
    (window.__gmapsCbs || []).forEach(function(cb) { try { cb(); } catch(e) {} });
    window.__gmapsCbs = [];
};

// Navigation loader — small corner indicator only. C# code (MainLayout)
// handles show/hide; loader starts display:none in Blazor. The previous
// full-screen overlay + millisecond counter UX was retired 2026-05-09; the
// corner spinner is unobtrusive enough that we don't need an "appear after
// N ms" delay either.
(function() {
    var timeout = null;

    function getEl() { return document.getElementById('app-loader'); }

    window.__loaderShow = function(delay) {
        clearTimeout(timeout);
        if (delay > 0) {
            timeout = setTimeout(function() {
                var el = getEl(); if (el) el.style.display = 'block';
            }, delay);
        } else {
            var el = getEl(); if (el) el.style.display = 'block';
        }
    };

    window.__loaderHide = function() {
        clearTimeout(timeout);
        timeout = null;
        var el = getEl(); if (el) el.style.display = 'none';
    };
})();
