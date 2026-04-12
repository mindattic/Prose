// Home page background — torn-edge compositing with localStorage caching.
//
// Effect: the image fades into the page with a ragged, organic tear on the
// left and top edges, a corner bite eating into the top-left, and a
// smattering of cigarette burns scattered across the visible area.
//
// Public API (called from Home.razor via JS interop):
//   window.homeBg.apply(imgElementId, imageUrl)

window.homeBg = (function () {
    'use strict';

    // ── Noise primitives ────────────────────────────────────────────────────

    function hash(x, y) {
        var n = Math.sin(x * 127.1 + y * 311.7) * 43758.5453123;
        return n - Math.floor(n);
    }

    function vnoise(x, y) {
        var ix = Math.floor(x), iy = Math.floor(y);
        var fx = x - ix, fy = y - iy;
        var ux = fx * fx * (3.0 - 2.0 * fx);
        var uy = fy * fy * (3.0 - 2.0 * fy);
        var a = hash(ix,     iy),  b = hash(ix + 1, iy);
        var c = hash(ix, iy + 1),  d = hash(ix + 1, iy + 1);
        return a + (b - a) * ux + (c - a) * uy + (a - b - c + d) * ux * uy;
    }

    // Fractal Brownian Motion — 2.07× avoids visible lattice repetition
    function fbm(x, y, octaves) {
        var value = 0, amp = 0.5, freq = 1.0, sum = 0;
        for (var i = 0; i < octaves; i++) {
            value += vnoise(x * freq, y * freq) * amp;
            sum   += amp;
            amp   *= 0.5;
            freq  *= 2.07;
        }
        return value / sum;
    }

    function smoothstep(e0, e1, t) {
        t = Math.max(0, Math.min(1, (t - e0) / (e1 - e0)));
        return t * t * (3.0 - 2.0 * t);
    }

    // ── Per-session seed & opacity ───────────────────────────────────────────
    // Both change every load so the fraying pattern, burns, and overall opacity
    // differ each time.
    var SEED           = Math.random() * 1000;
    var GLOBAL_OPACITY = 0.90 + Math.random() * 0.06;  // 0.90–0.96

    // ── Cigarette burns ──────────────────────────────────────────────────────
    // Positions randomised per session via SEED.

    var BURNS = (function () {
        var list = [];
        var count = 4;
        for (var b = 0; b < count; b++) {
            list.push({
                nx: hash(b * 17.3 + SEED, b * 7.1 + 2.3) * 0.62 + 0.26,
                ny: hash(b *  3.7 + 4.1,  b * 11.9 + SEED) * 0.58 + 0.28,
                r:  0.016 + hash(b * 9.1 + SEED, b * 2.7 + 6.1) * 0.038
            });
        }
        return list;
    })();

    // Returns an alpha multiplier in [0,1]: 0 at burn centre, 1 outside radius.
    function burnFactor(nx, ny) {
        var factor = 1.0;
        for (var b = 0; b < BURNS.length; b++) {
            var bd = BURNS[b];
            var ddx = nx - bd.nx, ddy = ny - bd.ny;
            var t = Math.sqrt(ddx * ddx + ddy * ddy) / bd.r;
            if (t < 1.15) {
                // Charred ring: fades from fully transparent at centre to opaque at edge
                factor = Math.min(factor, smoothstep(0.28, 1.0, t));
            }
        }
        return factor;
    }

    // ── Torn-edge + corner alpha ─────────────────────────────────────────────
    //
    // Layers (multiplied together):
    //   torn  — fBm noise drives the ragged tear line; wide FEATHER = fading opacity
    //   fade  — smooth gradient from the literal image border (always 0 at edge)
    //   bite  — exponential corner penalty eats deeper into the top-left corner
    //   burn  — circular cigarette-burn cutouts

    var EDGE_FRAC_X = 0.42;  // left tear zone  (42% of width)
    var EDGE_FRAC_Y = 0.22;  // top  tear zone  (22% of height)
    var NOISE_FREQ  = 4.5;
    var NOISE_OCTS  = 6;
    var TEAR_SCALE  = 0.62;  // noise max contribution to tear depth
    var FEATHER     = 0.38;  // wide feather → gradual fading opacity
    var EDGE_FADE   = 0.24;  // gradient ramp over first 24% of fade zone

    function tornAlpha(x, y, W, H) {
        var nx = x / W, ny = y / H;

        var dx = Math.min(nx / EDGE_FRAC_X, 1.0);
        var dy = Math.min(ny / EDGE_FRAC_Y, 1.0);

        // ── Corner bite ──────────────────────────────────────────────────────
        // Double-exponential decay: approaches 0 when EITHER axis is near 0,
        // creating a deeper transparent region along the diagonal than
        // min(dx,dy) alone would produce.
        var bite = 1.0 - Math.exp(-2.2 * dx) * Math.exp(-2.2 * dy);

        var edge = Math.min(dx, dy) * bite;

        // ── Torn boundary ────────────────────────────────────────────────────
        var n    = fbm(nx * NOISE_FREQ + SEED, ny * NOISE_FREQ + SEED, NOISE_OCTS);
        var lo   = n * TEAR_SCALE;
        var torn = smoothstep(lo, lo + FEATHER, edge);
        var fade = smoothstep(0.0, EDGE_FADE, edge);

        // ── Cigarette burns ──────────────────────────────────────────────────
        var burn = burnFactor(nx, ny);

        return torn * fade * burn * GLOBAL_OPACITY;
    }

    // ── Canvas compositing ───────────────────────────────────────────────────

    var MAX_W = 480;

    function renderTorn(img) {
        var scale = img.naturalWidth > MAX_W ? MAX_W / img.naturalWidth : 1.0;
        var W = Math.round(img.naturalWidth  * scale);
        var H = Math.round(img.naturalHeight * scale);

        var canvas = document.createElement('canvas');
        canvas.width = W; canvas.height = H;
        var ctx = canvas.getContext('2d');
        ctx.drawImage(img, 0, 0, W, H);

        var imgd = ctx.getImageData(0, 0, W, H);
        var d = imgd.data;

        for (var y = 0; y < H; y++) {
            for (var x = 0; x < W; x++) {
                var i = (y * W + x) * 4;
                d[i + 3] = Math.round(d[i + 3] * tornAlpha(x, y, W, H));
            }
        }

        ctx.putImageData(imgd, 0, 0);
        try { return canvas.toDataURL('image/png'); } catch (e) { return null; }
    }

    // ── localStorage cache ───────────────────────────────────────────────────

    var PREFIX = 'ss-home-bg:';

    function cacheGet(key) {
        try { return localStorage.getItem(PREFIX + key); } catch (e) { return null; }
    }

    function cacheSet(key, dataUrl) {
        try {
            localStorage.setItem(PREFIX + key, dataUrl);
        } catch (e) {
            evictAll();
            try { localStorage.setItem(PREFIX + key, dataUrl); } catch (e2) {}
        }
    }

    function evictAll() {
        var victims = [];
        try {
            for (var i = 0; i < localStorage.length; i++) {
                var k = localStorage.key(i);
                if (k && k.indexOf(PREFIX) === 0) victims.push(k);
            }
            victims.forEach(function (k) { try { localStorage.removeItem(k); } catch (e) {} });
        } catch (e) {}
    }

    // ── Animated scanline overlay ────────────────────────────────────────────
    // A live canvas sits over the image and runs its own rAF loop.
    // Fires 10% of the time.  Each line drifts up or down and flickers.

    var scanlinesRaf = null;
    var scanlinesEl  = null;

    function stopScanlineAnim() {
        if (scanlinesRaf) { cancelAnimationFrame(scanlinesRaf); scanlinesRaf = null; }
        if (scanlinesEl && scanlinesEl.parentNode) scanlinesEl.parentNode.removeChild(scanlinesEl);
        scanlinesEl = null;
    }

    function startScanlineAnim(imgEl) {
        stopScanlineAnim();

        var ol = document.createElement('canvas');
        ol.id = 'home-bg-scanlines';
        ol.style.cssText = 'position:fixed;pointer-events:none;z-index:1;';
        document.body.appendChild(ol);
        scanlinesEl = ol;

        var ctx2 = ol.getContext('2d');

        // Match the image rect, refresh each frame
        function syncRect() {
            var r = imgEl.getBoundingClientRect();
            ol.style.left   = r.left   + 'px';
            ol.style.top    = r.top    + 'px';
            ol.style.width  = r.width  + 'px';
            ol.style.height = r.height + 'px';
            ol.width  = Math.round(r.width);
            ol.height = Math.round(r.height);
        }
        syncRect();

        // Initialise 1–2 lines
        var lineCount = 1 + Math.floor(Math.random() * 2);
        var lines = [];
        for (var i = 0; i < lineCount; i++) {
            lines.push({
                y:           Math.random() * ol.height,
                speed:       (0.3 + Math.random() * 0.8) * (Math.random() < 0.5 ? 1 : -1),
                h:           1 + Math.floor(Math.random() * 3),
                baseOpacity: 0.30 + Math.random() * 0.45
            });
        }

        function frame() {
            syncRect();
            ctx2.clearRect(0, 0, ol.width, ol.height);

            for (var j = 0; j < lines.length; j++) {
                var ln = lines[j];
                ln.y += ln.speed;
                if (ln.y > ol.height) ln.y = 0;
                if (ln.y < 0)         ln.y = ol.height;

                // Occasional flicker: random opacity drop
                var flicker = Math.random() < 0.12 ? Math.random() * 0.5 : 0;
                var op = Math.max(0, ln.baseOpacity - flicker);

                ctx2.fillStyle = 'rgba(255,255,255,' + op + ')';
                ctx2.fillRect(0, Math.round(ln.y), ol.width, ln.h);
            }

            scanlinesRaf = requestAnimationFrame(frame);
        }

        frame();
    }

    // ── Public entry point ───────────────────────────────────────────────────

    function apply(imgId, imageUrl) {
        var imgEl = document.getElementById(imgId);
        if (!imgEl) return;

        stopScanlineAnim();

        // DEV: nuke any previously stored composites so nothing sneaks through
        evictAll();

        var showScanlines = Math.random() < 0.10;

        var src = new Image();
        src.onload = function () {
            var dataUrl = renderTorn(src);
            if (dataUrl) { imgEl.src = dataUrl; }
            imgEl.style.opacity = '1';
            if (showScanlines) {
                // Wait one frame so the img has its final layout rect
                requestAnimationFrame(function () { startScanlineAnim(imgEl); });
            }
        };
        src.onerror = function () { imgEl.style.opacity = '1'; };
        src.src = imageUrl;
    }

    return { apply: apply };
})();
