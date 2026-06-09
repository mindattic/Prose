/* SacredGeometry — a catalog of 1024 unique, animatable line-art shapes.
 *
 * One canonical renderer, two consumers:
 *   • Cyberspace's "schematic" console windows draw shapes on a live <canvas>.
 *   • MindAttic.Legion.PersonaGallery maps persona-NNNN -> shape NNNN, also on a canvas,
 *     with the static SVG poster (emitted by build-previews.mjs) as a pre-hydration fallback.
 *
 * Everything is pure math: no DOM, no RNG, no Date. sample(i, t) is a pure function of
 * (index, phase), so the Node-built SVG posters and the browser canvas agree by construction.
 *
 * Indices 0..83 reproduce Cyberspace's original catalog verbatim minus the harmonograph (band 0); the
 * remaining bands sweep parametric families (prisms, antiprisms, star polygons, rose/Lissajous/
 * cycloid curves, torus knots, the Gielis superformula, fractals) up to 1024.
 *
 * Classic browser <script> AND require()-able under Node (UMD), no bundler.
 */
(function (root, factory) {
    var api = factory();
    if (typeof window !== 'undefined') window.SacredGeometry = api;
    if (typeof module !== 'undefined' && module.exports) module.exports = api;
    else if (root) root.SacredGeometry = api;
})(typeof globalThis !== 'undefined' ? globalThis : this, function () {
    'use strict';

    var PHI = (1 + Math.sqrt(5)) / 2;
    var TAU = Math.PI * 2;

    var TEAL        = 'rgba(70,210,170,0.55)';
    var TEAL_DOT    = 'rgba(70,210,170,0.85)';
    var TEAL_STRONG = 'rgba(70,210,170,0.65)';
    var CYAN_FAINT  = 'rgba(70,200,220,0.22)';

    // ── A pen that either strokes onto a canvas 2D context, or collects 2D polylines + dots ─────
    // The shape "paint" functions are written once against this interface; CanvasPen forwards to
    // a real ctx (live animation), CollectPen accumulates geometry (SVG export, sampling, tests).

    function CanvasPen(ctx) { this.ctx = ctx; }
    CanvasPen.prototype = {
        set strokeStyle(v) { this.ctx.strokeStyle = v; },
        set fillStyle(v)   { this.ctx.fillStyle = v; },
        set lineWidth(v)   { this.ctx.lineWidth = v; },
        save()      { this.ctx.save(); },
        restore()   { this.ctx.restore(); },
        translate(x, y) { this.ctx.translate(x, y); },
        rotate(a)   { this.ctx.rotate(a); },
        scale(x, y) { this.ctx.scale(x, y); },
        beginPath() { this.ctx.beginPath(); },
        moveTo(x, y){ this.ctx.moveTo(x, y); },
        lineTo(x, y){ this.ctx.lineTo(x, y); },
        arc(x, y, r, a0, a1, ccw) { this.ctx.arc(x, y, r, a0, a1, ccw); },
        ellipse(x, y, rx, ry, rot, a0, a1) { this.ctx.ellipse(x, y, rx, ry, rot, a0, a1); },
        closePath() { this.ctx.closePath(); },
        stroke()    { this.ctx.stroke(); },
        fill()      { this.ctx.fill(); }
    };

    function CollectPen() {
        this.M = [1, 0, 0, 1, 0, 0];   // current affine [a,b,c,d,e,f]
        this.stack = [];
        this.polylines = [];           // { pts:[[x,y]...], color, width }
        this.dots = [];                // { x, y, r, color }
        this.path = [];                // subpaths of current path
        this.cur = null;               // current subpath
        this._dot = null;              // candidate dot from a lone arc (for fill())
        this._stroke = '#000';
        this._fill = '#000';
        this._width = 1;
    }
    function matmul(A, B) {
        return [
            A[0] * B[0] + A[2] * B[1],
            A[1] * B[0] + A[3] * B[1],
            A[0] * B[2] + A[2] * B[3],
            A[1] * B[2] + A[3] * B[3],
            A[0] * B[4] + A[2] * B[5] + A[4],
            A[1] * B[4] + A[3] * B[5] + A[5]
        ];
    }
    CollectPen.prototype = {
        set strokeStyle(v) { this._stroke = v; },
        set fillStyle(v)   { this._fill = v; },
        set lineWidth(v)   { this._width = v; },
        _ap(x, y) { var M = this.M; return [M[0] * x + M[2] * y + M[4], M[1] * x + M[3] * y + M[5]]; },
        _scale()  { var M = this.M; return Math.sqrt(Math.abs(M[0] * M[3] - M[1] * M[2])) || 1; },
        save()    { this.stack.push(this.M.slice()); },
        restore() { if (this.stack.length) this.M = this.stack.pop(); },
        translate(x, y) { this.M = matmul(this.M, [1, 0, 0, 1, x, y]); },
        rotate(a) { var c = Math.cos(a), s = Math.sin(a); this.M = matmul(this.M, [c, s, -s, c, 0, 0]); },
        scale(x, y) { this.M = matmul(this.M, [x, 0, 0, y, 0, 0]); },
        beginPath() { this.path = []; this.cur = null; this._dot = null; },
        moveTo(x, y) { this.cur = [this._ap(x, y)]; this.path.push(this.cur); this._dot = null; },
        lineTo(x, y) { if (!this.cur) { this.cur = []; this.path.push(this.cur); } this.cur.push(this._ap(x, y)); this._dot = null; },
        arc(x, y, r, a0, a1, ccw) {
            var sweep = a1 - a0; if (ccw) sweep = -((TAU - sweep) % TAU);
            var steps = Math.max(6, Math.ceil(Math.abs(sweep) / (Math.PI / 24)));
            if (!this.cur) { this.cur = []; this.path.push(this.cur); }
            for (var k = 0; k <= steps; k++) {
                var a = a0 + sweep * (k / steps);
                this.cur.push(this._ap(x + Math.cos(a) * r, y + Math.sin(a) * r));
            }
            // remember a lone full circle so a following fill() becomes a dot
            this._dot = (Math.abs(Math.abs(sweep) - TAU) < 1e-6) ? { c: this._ap(x, y), r: r * this._scale() } : null;
        },
        ellipse(x, y, rx, ry, rot, a0, a1) {
            var sweep = a1 - a0;
            var steps = Math.max(8, Math.ceil(Math.abs(sweep) / (Math.PI / 24)));
            var cr = Math.cos(rot || 0), sr = Math.sin(rot || 0);
            if (!this.cur) { this.cur = []; this.path.push(this.cur); }
            for (var k = 0; k <= steps; k++) {
                var a = a0 + sweep * (k / steps);
                var ex = Math.cos(a) * rx, ey = Math.sin(a) * ry;
                this.cur.push(this._ap(x + ex * cr - ey * sr, y + ex * sr + ey * cr));
            }
            this._dot = null;
        },
        closePath() { if (this.cur && this.cur.length) this.cur.push(this.cur[0].slice()); },
        stroke() {
            for (var s = 0; s < this.path.length; s++) {
                var sp = this.path[s];
                if (sp.length >= 2) this.polylines.push({ pts: sp.slice(), color: this._stroke, width: this._width });
            }
        },
        fill() {
            if (this._dot) { this.dots.push({ x: this._dot.c[0], y: this._dot.c[1], r: this._dot.r, color: this._fill }); this._dot = null; return; }
            for (var s = 0; s < this.path.length; s++) {
                var sp = this.path[s];
                if (sp.length >= 2) this.polylines.push({ pts: sp.slice(), color: this._fill, width: this._width });
            }
        }
    };

    // ── 3D projection + wireframe (shared by every polyhedron) ──────────────────────────────────
    function proj3(v, rx, ry) {
        var x = v[0], y = v[1], z = v[2];
        var cry = Math.cos(ry), sry = Math.sin(ry);
        var x1 = x * cry + z * sry, z1 = -x * sry + z * cry;
        var crx = Math.cos(rx), srx = Math.sin(rx);
        var y1 = y * crx - z1 * srx, z2 = y * srx + z1 * crx;
        var d = 6 / (6 + z2 * 0.5);
        return [x1 * d * 32, y1 * d * 32];
    }
    // verts/edges -> projected edges + vertex dots. rx = t*0.4, ry = t (the original drawWire law).
    function renderPoly3d(p, sh, t, style) {
        var rx = t * 0.4, ry = t;
        var pts = sh.verts.map(function (v) { return proj3(v, rx, ry); });
        p.strokeStyle = (style && style.stroke) || TEAL; p.lineWidth = (style && style.lineWidth) || 0.7;
        sh.edges.forEach(function (e) { p.beginPath(); p.moveTo(pts[e[0]][0], pts[e[0]][1]); p.lineTo(pts[e[1]][0], pts[e[1]][1]); p.stroke(); });
        p.fillStyle = (style && style.dotColor) || TEAL_DOT;
        var dr = (style && style.dotRadius) || 1.2;
        pts.forEach(function (q) { p.beginPath(); p.arc(q[0], q[1], dr, 0, TAU); p.fill(); });
    }

    // ── Polyhedron generators (n-gonal prism / antiprism / bipyramid / pyramid / star prism) ─────
    function ngPrism(n, r, h) {
        var v = [], e = [], i, a;
        for (i = 0; i < n; i++) { a = i * TAU / n; v.push([Math.cos(a) * r, h, Math.sin(a) * r]); }
        for (i = 0; i < n; i++) { a = i * TAU / n; v.push([Math.cos(a) * r, -h, Math.sin(a) * r]); }
        for (i = 0; i < n; i++) { e.push([i, (i + 1) % n]); e.push([n + i, n + ((i + 1) % n)]); e.push([i, n + i]); }
        return { verts: v, edges: e };
    }
    function ngAnti(n, r, h) {
        var v = [], e = [], i, a;
        for (i = 0; i < n; i++) { a = i * TAU / n; v.push([Math.cos(a) * r, h, Math.sin(a) * r]); }
        for (i = 0; i < n; i++) { a = (i + 0.5) * TAU / n; v.push([Math.cos(a) * r, -h, Math.sin(a) * r]); }
        for (i = 0; i < n; i++) { e.push([i, (i + 1) % n]); e.push([n + i, n + ((i + 1) % n)]); e.push([i, n + i]); e.push([i, n + ((i + n - 1) % n)]); }
        return { verts: v, edges: e };
    }
    function ngBipy(n, r, h) {
        var v = [[0, h, 0], [0, -h, 0]], e = [], i, a;
        for (i = 0; i < n; i++) { a = i * TAU / n; v.push([Math.cos(a) * r, 0, Math.sin(a) * r]); }
        for (i = 0; i < n; i++) { e.push([0, 2 + i]); e.push([1, 2 + i]); e.push([2 + i, 2 + ((i + 1) % n)]); }
        return { verts: v, edges: e };
    }
    function ngPy(n, r, h) {
        var v = [[0, h, 0]], e = [], i, a;
        for (i = 0; i < n; i++) { a = i * TAU / n; v.push([Math.cos(a) * r, -h, Math.sin(a) * r]); }
        for (i = 0; i < n; i++) { e.push([0, 1 + i]); e.push([1 + i, 1 + ((i + 1) % n)]); }
        return { verts: v, edges: e };
    }
    function starPoly(n, k, r, h) {
        var v = [], e = [], i, a;
        for (i = 0; i < n; i++) { a = i * TAU / n; v.push([Math.cos(a) * r, h, Math.sin(a) * r]); }
        for (i = 0; i < n; i++) { a = i * TAU / n; v.push([Math.cos(a) * r, -h, Math.sin(a) * r]); }
        for (i = 0; i < n; i++) { e.push([i, (i + k) % n]); e.push([n + i, n + ((i + k) % n)]); e.push([i, n + i]); }
        return { verts: v, edges: e };
    }

    // ── Band 0: the original 85-shape catalog, verbatim ─────────────────────────────────────────
    var GEO = {
        tetra:  { label: 'tetrahedron',  verts: [[1,1,1],[-1,-1,1],[-1,1,-1],[1,-1,-1]], edges: [[0,1],[0,2],[0,3],[1,2],[1,3],[2,3]] },
        cube:   { label: 'hexahedron',   verts: [[-1,-1,-1],[1,-1,-1],[1,1,-1],[-1,1,-1],[-1,-1,1],[1,-1,1],[1,1,1],[-1,1,1]], edges: [[0,1],[1,2],[2,3],[3,0],[4,5],[5,6],[6,7],[7,4],[0,4],[1,5],[2,6],[3,7]] },
        octa:   { label: 'octahedron',   verts: [[0,1,0],[0,-1,0],[1,0,0],[-1,0,0],[0,0,1],[0,0,-1]], edges: [[0,2],[0,3],[0,4],[0,5],[1,2],[1,3],[1,4],[1,5],[2,4],[4,3],[3,5],[5,2]] },
        icosa:  { label: 'icosahedron',  verts: [[0,1,PHI],[0,-1,PHI],[0,1,-PHI],[0,-1,-PHI],[1,PHI,0],[-1,PHI,0],[1,-PHI,0],[-1,-PHI,0],[PHI,0,1],[-PHI,0,1],[PHI,0,-1],[-PHI,0,-1]], edges: [[0,1],[0,4],[0,5],[0,8],[0,9],[1,6],[1,7],[1,8],[1,9],[2,3],[2,4],[2,5],[2,10],[2,11],[3,6],[3,7],[3,10],[3,11],[4,5],[4,8],[4,10],[5,9],[5,11],[6,7],[6,8],[6,10],[7,9],[7,11],[8,10],[9,11]] },
        prism:  { label: 'triangular prism', verts: [[0,1,1],[0.866,-0.5,1],[-0.866,-0.5,1],[0,1,-1],[0.866,-0.5,-1],[-0.866,-0.5,-1]], edges: [[0,1],[1,2],[2,0],[3,4],[4,5],[5,3],[0,3],[1,4],[2,5]] },
        stella: { label: 'stella octangula', verts: [[1,1,1],[-1,-1,1],[-1,1,-1],[1,-1,-1],[-1,-1,-1],[1,1,-1],[1,-1,1],[-1,1,1]], edges: [[0,1],[0,2],[0,3],[1,2],[1,3],[2,3],[4,5],[4,6],[4,7],[5,6],[5,7],[6,7]] },
        cubocta: { label: 'cuboctahedron', verts: [[1,1,0],[-1,1,0],[1,-1,0],[-1,-1,0],[1,0,1],[-1,0,1],[1,0,-1],[-1,0,-1],[0,1,1],[0,-1,1],[0,1,-1],[0,-1,-1]], edges: [[0,4],[0,6],[0,8],[0,10],[1,5],[1,7],[1,8],[1,10],[2,4],[2,6],[2,9],[2,11],[3,5],[3,7],[3,9],[3,11],[4,8],[4,9],[5,8],[5,9],[6,10],[6,11],[7,10],[7,11]] },
        antiprism: { label: 'square antiprism', verts: [[1,1,0],[0,1,1],[-1,1,0],[0,1,-1],[0.707,-1,0.707],[-0.707,-1,0.707],[-0.707,-1,-0.707],[0.707,-1,-0.707]], edges: [[0,1],[1,2],[2,3],[3,0],[4,5],[5,6],[6,7],[7,4],[0,4],[0,7],[1,4],[1,5],[2,5],[2,6],[3,6],[3,7]] },
        pyramid: { label: 'square pyramid', verts: [[0,1.2,0],[1,-0.5,1],[-1,-0.5,1],[-1,-0.5,-1],[1,-0.5,-1]], edges: [[0,1],[0,2],[0,3],[0,4],[1,2],[2,3],[3,4],[4,1]] },
        pentaprism: { label: 'pentagonal prism', verts: [[1,1,0],[0.309,1,0.951],[-0.809,1,0.588],[-0.809,1,-0.588],[0.309,1,-0.951],[1,-1,0],[0.309,-1,0.951],[-0.809,-1,0.588],[-0.809,-1,-0.588],[0.309,-1,-0.951]], edges: [[0,1],[1,2],[2,3],[3,4],[4,0],[5,6],[6,7],[7,8],[8,9],[9,5],[0,5],[1,6],[2,7],[3,8],[4,9]] },
        dodeca: { label: 'dodecahedron', verts: [[1,1,1],[1,1,-1],[1,-1,1],[1,-1,-1],[-1,1,1],[-1,1,-1],[-1,-1,1],[-1,-1,-1],[0,0.618,1.618],[0,0.618,-1.618],[0,-0.618,1.618],[0,-0.618,-1.618],[0.618,1.618,0],[0.618,-1.618,0],[-0.618,1.618,0],[-0.618,-1.618,0],[1.618,0,0.618],[1.618,0,-0.618],[-1.618,0,0.618],[-1.618,0,-0.618]], edges: [[0,8],[0,12],[0,16],[1,9],[1,12],[1,17],[2,10],[2,13],[2,16],[3,11],[3,13],[3,17],[4,8],[4,14],[4,18],[5,9],[5,14],[5,19],[6,10],[6,15],[6,18],[7,11],[7,15],[7,19],[8,10],[9,11],[12,14],[13,15],[16,17],[18,19]] },
        hexprism: { label: 'hexagonal prism', verts: [[1,1,0],[0.5,1,0.866],[-0.5,1,0.866],[-1,1,0],[-0.5,1,-0.866],[0.5,1,-0.866],[1,-1,0],[0.5,-1,0.866],[-0.5,-1,0.866],[-1,-1,0],[-0.5,-1,-0.866],[0.5,-1,-0.866]], edges: [[0,1],[1,2],[2,3],[3,4],[4,5],[5,0],[6,7],[7,8],[8,9],[9,10],[10,11],[11,6],[0,6],[1,7],[2,8],[3,9],[4,10],[5,11]] },
        hexanti: { label: 'hexagonal antiprism', verts: [[1,1,0],[0.5,1,0.866],[-0.5,1,0.866],[-1,1,0],[-0.5,1,-0.866],[0.5,1,-0.866],[0.866,-1,0.5],[0,-1,1],[-0.866,-1,0.5],[-0.866,-1,-0.5],[0,-1,-1],[0.866,-1,-0.5]], edges: [[0,1],[1,2],[2,3],[3,4],[4,5],[5,0],[6,7],[7,8],[8,9],[9,10],[10,11],[11,6],[0,6],[0,11],[1,6],[1,7],[2,7],[2,8],[3,8],[3,9],[4,9],[4,10],[5,10],[5,11]] },
        pentabipy: { label: 'pentagonal bipyramid', verts: [[0,1,0],[0,-1,0],[1,0,0],[0.309,0,0.951],[-0.809,0,0.588],[-0.809,0,-0.588],[0.309,0,-0.951]], edges: [[0,2],[0,3],[0,4],[0,5],[0,6],[1,2],[1,3],[1,4],[1,5],[1,6],[2,3],[3,4],[4,5],[5,6],[6,2]] },
        tribipy: { label: 'triangular bipyramid', verts: [[0,1,0],[0,-1,0],[1,0,0],[-0.5,0,0.866],[-0.5,0,-0.866]], edges: [[0,2],[0,3],[0,4],[1,2],[1,3],[1,4],[2,3],[3,4],[4,2]] },
        trunctetra: { label: 'truncated tetrahedron', verts: [[0.333,1,1],[1,0.333,1],[1,1,0.333],[-0.333,-1,1],[-1,-0.333,1],[-1,-1,0.333],[-0.333,1,-1],[1,-0.333,-1],[1,-1,-0.333],[-1,0.333,-1],[-1,1,-0.333],[0.333,-1,-1]], edges: [[0,1],[1,2],[2,0],[3,4],[4,5],[5,3],[6,7],[7,8],[8,6],[9,10],[10,11],[11,9],[0,2],[2,6],[6,10],[10,0],[1,2],[1,7],[7,8],[8,1],[3,5],[5,9],[9,11],[11,3]] },
        octapyramid: { label: 'pyramid on octahedron', verts: [[0,1.6,0],[0,0.5,0],[0,-0.5,0],[1,0,0],[-1,0,0],[0,0,1],[0,0,-1]], edges: [[0,3],[0,4],[0,5],[0,6],[1,3],[1,4],[1,5],[1,6],[2,3],[2,4],[2,5],[2,6],[3,5],[5,4],[4,6],[6,3],[0,1]] },
        rhombdo: { label: 'rhombic dodecahedron', verts: [[1,1,1],[1,1,-1],[1,-1,1],[1,-1,-1],[-1,1,1],[-1,1,-1],[-1,-1,1],[-1,-1,-1],[2,0,0],[-2,0,0],[0,2,0],[0,-2,0],[0,0,2],[0,0,-2]], edges: [[0,8],[1,8],[2,8],[3,8],[4,9],[5,9],[6,9],[7,9],[0,10],[1,10],[4,10],[5,10],[2,11],[3,11],[6,11],[7,11],[0,12],[2,12],[4,12],[6,12],[1,13],[3,13],[5,13],[7,13]] },
        frustum: { label: 'square frustum', verts: [[0.5,1,0.5],[-0.5,1,0.5],[-0.5,1,-0.5],[0.5,1,-0.5],[1,-1,1],[-1,-1,1],[-1,-1,-1],[1,-1,-1]], edges: [[0,1],[1,2],[2,3],[3,0],[4,5],[5,6],[6,7],[7,4],[0,4],[1,5],[2,6],[3,7]] },
        rhombpara: { label: 'rhombic parallelepiped', verts: [[1,1,0.5],[-0.5,1,1],[-1,1,-0.5],[0.5,1,-1],[1,-1,0.5],[-0.5,-1,1],[-1,-1,-0.5],[0.5,-1,-1]], edges: [[0,1],[1,2],[2,3],[3,0],[4,5],[5,6],[6,7],[7,4],[0,4],[1,5],[2,6],[3,7]] },
        starbipy: { label: 'star tetrahedron pair', verts: [[1.4,0,0],[-0.7,0,1.21],[-0.7,0,-1.21],[0,1.4,0],[0,-1.4,0],[1.21,0,-0.7],[-0.7,1.21,0],[-0.7,-1.21,0]], edges: [[0,3],[0,4],[1,3],[1,4],[2,3],[2,4],[0,1],[1,2],[2,0],[5,6],[6,7],[7,5],[5,3],[5,4],[6,3],[6,4],[7,3],[7,4]] }
    };

    [
        ['heptaprism',  'heptagonal prism',     ngPrism(7,  1, 1)],
        ['octaprism',   'octagonal prism',      ngPrism(8,  1, 1)],
        ['nonaprism',   'enneagonal prism',     ngPrism(9,  1, 1)],
        ['decaprism',   'decagonal prism',      ngPrism(10, 1, 1)],
        ['dodecaprism', 'dodecagonal prism',    ngPrism(12, 1, 1)],
        ['cylinder',    'cylindrical prism',    ngPrism(20, 1, 1.2)],
        ['heptanti',    'heptagonal antiprism', ngAnti(7,  1, 0.85)],
        ['octanti',     'octagonal antiprism',  ngAnti(8,  1, 0.85)],
        ['nonanti',     'enneagonal antiprism', ngAnti(9,  1, 0.85)],
        ['decanti',     'decagonal antiprism',  ngAnti(10, 1, 0.85)],
        ['dodecanti',   'dodecagonal antiprism',ngAnti(12, 1, 0.85)],
        ['hexbipy',     'hexagonal bipyramid',  ngBipy(6,  1, 1.3)],
        ['heptbipy',    'heptagonal bipyramid', ngBipy(7,  1, 1.3)],
        ['octbipy',     'octagonal bipyramid',  ngBipy(8,  1, 1.3)],
        ['nonabipy',    'enneagonal bipyramid', ngBipy(9,  1, 1.3)],
        ['decabipy',    'decagonal bipyramid',  ngBipy(10, 1, 1.3)],
        ['pentapy',     'pentagonal pyramid',   ngPy(5,  1, 1)],
        ['hexapy',      'hexagonal pyramid',    ngPy(6,  1, 1)],
        ['heptapy',     'heptagonal pyramid',   ngPy(7,  1, 1)],
        ['cone',        'circular cone',        ngPy(20, 1, 1.4)],
        ['heptagram',   '7-pointed star prism', starPoly(7,  3, 1, 0.4)],
        ['octagram',    '8-pointed star prism', starPoly(8,  3, 1, 0.4)],
        ['nonagram',    '9-pointed star prism', starPoly(9,  4, 1, 0.4)],
        ['decagram',    '10-pointed star prism',starPoly(10, 3, 1, 0.4)],
        ['hendecaprism',  'hendecagonal prism',     ngPrism(11, 1, 1)],
        ['tridecaprism',  'tridecagonal prism',     ngPrism(13, 1, 1)],
        ['pentadecaprism','pentadecagonal prism',   ngPrism(15, 1, 1)],
        ['hendecanti',    'hendecagonal antiprism', ngAnti(11, 1, 0.85)],
        ['tridecanti',    'tridecagonal antiprism', ngAnti(13, 1, 0.85)],
        ['octapy',        'octagonal pyramid',      ngPy(8,  1, 1)],
        ['decapy',        'decagonal pyramid',      ngPy(10, 1, 1)]
    ].forEach(function (g) { GEO[g[0]] = { label: g[1], verts: g[2].verts, edges: g[2].edges }; });

    GEO.tesseract = { label: 'tesseract projection',
        verts: [[-1,-1,-1],[1,-1,-1],[1,1,-1],[-1,1,-1],[-1,-1,1],[1,-1,1],[1,1,1],[-1,1,1],[-0.5,-0.5,-0.5],[0.5,-0.5,-0.5],[0.5,0.5,-0.5],[-0.5,0.5,-0.5],[-0.5,-0.5,0.5],[0.5,-0.5,0.5],[0.5,0.5,0.5],[-0.5,0.5,0.5]],
        edges: [[0,1],[1,2],[2,3],[3,0],[4,5],[5,6],[6,7],[7,4],[0,4],[1,5],[2,6],[3,7],[8,9],[9,10],[10,11],[11,8],[12,13],[13,14],[14,15],[15,12],[8,12],[9,13],[10,14],[11,15],[0,8],[1,9],[2,10],[3,11],[4,12],[5,13],[6,14],[7,15]] };
    GEO.compoundCO = { label: 'cube–octa compound',
        verts: [[-1,-1,-1],[1,-1,-1],[1,1,-1],[-1,1,-1],[-1,-1,1],[1,-1,1],[1,1,1],[-1,1,1],[0,1.4,0],[0,-1.4,0],[1.4,0,0],[-1.4,0,0],[0,0,1.4],[0,0,-1.4]],
        edges: [[0,1],[1,2],[2,3],[3,0],[4,5],[5,6],[6,7],[7,4],[0,4],[1,5],[2,6],[3,7],[8,10],[8,11],[8,12],[8,13],[9,10],[9,11],[9,12],[9,13],[10,12],[12,11],[11,13],[13,10]] };
    GEO.obelisk = { label: 'obelisk',
        verts: [[-0.6,-1.4,-0.6],[0.6,-1.4,-0.6],[0.6,-1.4,0.6],[-0.6,-1.4,0.6],[-0.5,0.9,-0.5],[0.5,0.9,-0.5],[0.5,0.9,0.5],[-0.5,0.9,0.5],[0,1.5,0]],
        edges: [[0,1],[1,2],[2,3],[3,0],[4,5],[5,6],[6,7],[7,4],[0,4],[1,5],[2,6],[3,7],[4,8],[5,8],[6,8],[7,8]] };
    GEO.spire = { label: 'spire',
        verts: [[1,-1,0],[0.309,-1,0.951],[-0.809,-1,0.588],[-0.809,-1,-0.588],[0.309,-1,-0.951],[0.4,0,0],[0.124,0,0.380],[-0.324,0,0.235],[-0.324,0,-0.235],[0.124,0,-0.380],[0,1.6,0]],
        edges: [[0,1],[1,2],[2,3],[3,4],[4,0],[5,6],[6,7],[7,8],[8,9],[9,5],[0,5],[1,6],[2,7],[3,8],[4,9],[5,10],[6,10],[7,10],[8,10],[9,10]] };
    GEO.wedge = { label: 'wedge', verts: [[1,1,0],[-1,1,0],[1,-1,1],[-1,-1,1],[1,-1,-1],[-1,-1,-1]], edges: [[0,1],[2,3],[4,5],[0,2],[0,4],[1,3],[1,5],[2,4],[3,5]] };
    GEO.lattice3 = { label: '3³ lattice cube',
        verts: (function () { var v = []; for (var x = -1; x <= 1; x++) for (var y = -1; y <= 1; y++) for (var z = -1; z <= 1; z++) v.push([x, y, z]); return v; })(),
        edges: (function () { var e = [], idx = function (x, y, z) { return (x + 1) * 9 + (y + 1) * 3 + (z + 1); }; for (var x = -1; x <= 1; x++) for (var y = -1; y <= 1; y++) for (var z = -1; z <= 1; z++) { if (x < 1) e.push([idx(x,y,z), idx(x+1,y,z)]); if (y < 1) e.push([idx(x,y,z), idx(x,y+1,z)]); if (z < 1) e.push([idx(x,y,z), idx(x,y,z+1)]); } return e; })() };
    GEO.crystalPair = { label: 'crystal pair',
        verts: [[0,1.4,0],[0,-1.4,0],[0.9,0,0],[-0.9,0,0],[0,0,0.9],[0,0,-0.9],[0.6,0.9,0.6],[-0.6,-0.9,-0.6],[1.0,0,0.5],[-1.0,0,-0.5],[0.5,0,1.0],[-0.5,0,-1.0]],
        edges: [[0,2],[0,3],[0,4],[0,5],[1,2],[1,3],[1,4],[1,5],[2,4],[4,3],[3,5],[5,2],[6,8],[6,10],[7,9],[7,11],[8,10],[9,11],[6,7]] };
    GEO.trunccube = (function () {
        var t = 0.333, verts = [], edges = [], sx, sy, sz, c, b, c1, bit, c2, off;
        for (sx = -1; sx <= 1; sx += 2) for (sy = -1; sy <= 1; sy += 2) for (sz = -1; sz <= 1; sz += 2) { verts.push([sx * t, sy, sz]); verts.push([sx, sy * t, sz]); verts.push([sx, sy, sz * t]); }
        for (c = 0; c < 8; c++) { b = c * 3; edges.push([b, b + 1]); edges.push([b + 1, b + 2]); edges.push([b + 2, b]); }
        for (c1 = 0; c1 < 8; c1++) for (bit = 0; bit < 3; bit++) { c2 = c1 ^ (1 << bit); if (c2 > c1) { off = (bit === 0) ? 2 : (bit === 1) ? 1 : 0; edges.push([c1 * 3 + off, c2 * 3 + off]); } }
        return { label: 'truncated cube', verts: verts, edges: edges };
    })();
    GEO.gyropy = { label: 'gyroelongated pyramid',
        verts: [[1,0,0],[0.309,0,0.951],[-0.809,0,0.588],[-0.809,0,-0.588],[0.309,0,-0.951],[0.809,-1.2,0.588],[-0.309,-1.2,0.951],[-1,-1.2,0],[-0.309,-1.2,-0.951],[0.809,-1.2,-0.588],[0,1.2,0]],
        edges: [[0,1],[1,2],[2,3],[3,4],[4,0],[5,6],[6,7],[7,8],[8,9],[9,5],[0,5],[0,9],[1,5],[1,6],[2,6],[2,7],[3,7],[3,8],[4,8],[4,9],[10,0],[10,1],[10,2],[10,3],[10,4]] };
    GEO.dnahelix = (function () {
        var verts = [], edges = [], n = 8, R = 0.6, dA = Math.PI / 4, h0 = -1.5, dy = 3 / (n - 1), i, a;
        for (i = 0; i < n; i++) { a = i * dA; verts.push([R * Math.cos(a), h0 + i * dy, R * Math.sin(a)]); }
        for (i = 0; i < n; i++) { a = i * dA + Math.PI; verts.push([R * Math.cos(a), h0 + i * dy, R * Math.sin(a)]); }
        for (i = 0; i < n - 1; i++) { edges.push([i, i + 1]); edges.push([n + i, n + i + 1]); }
        for (i = 0; i < n; i++) edges.push([i, n + i]);
        return { label: 'double helix', verts: verts, edges: edges };
    })();

    // ── Band 0 parametric draws (ported verbatim; ctx2 -> p) ────────────────────────────────────
    function drawFlower(p, t) {
        var r = 22, i, a, a2;
        p.strokeStyle = 'rgba(70,200,220,0.38)'; p.lineWidth = 0.6;
        p.beginPath(); p.arc(0, 0, r, 0, TAU); p.stroke();
        for (i = 0; i < 6; i++) { a = (i / 6) * TAU + t * 0.05; p.beginPath(); p.arc(Math.cos(a) * r, Math.sin(a) * r, r, 0, TAU); p.stroke(); }
        p.strokeStyle = 'rgba(70,220,170,0.65)'; p.lineWidth = 0.8;
        p.beginPath();
        for (i = 0; i < 6; i++) { a2 = (i / 6) * TAU + t * 0.05; if (i === 0) p.moveTo(Math.cos(a2) * r, Math.sin(a2) * r); else p.lineTo(Math.cos(a2) * r, Math.sin(a2) * r); }
        p.closePath(); p.stroke();
    }
    function drawMetatron(p, t) {
        var r = 16, ir = r, or2 = r * 2, cs = [[0, 0]], i, a, b;
        for (i = 0; i < 6; i++) { a = (i / 6) * TAU + t * 0.03; cs.push([Math.cos(a) * ir, Math.sin(a) * ir]); }
        for (i = 0; i < 6; i++) { a = (i / 6) * TAU + Math.PI / 6 + t * 0.03; cs.push([Math.cos(a) * or2, Math.sin(a) * or2]); }
        p.strokeStyle = 'rgba(70,180,220,0.28)'; p.lineWidth = 0.5;
        cs.forEach(function (c) { p.beginPath(); p.arc(c[0], c[1], r, 0, TAU); p.stroke(); });
        p.strokeStyle = 'rgba(70,220,170,0.45)'; p.lineWidth = 0.5;
        for (a = 0; a < cs.length; a++) for (b = a + 1; b < cs.length; b++) { p.beginPath(); p.moveTo(cs[a][0], cs[a][1]); p.lineTo(cs[b][0], cs[b][1]); p.stroke(); }
        p.fillStyle = 'rgba(70,220,170,0.7)';
        cs.forEach(function (c) { p.beginPath(); p.arc(c[0], c[1], 1, 0, TAU); p.fill(); });
    }
    function drawVesica(p, t) {
        var r = 24, d = r * 0.6, half;
        p.save(); p.rotate(t * 0.02);
        p.strokeStyle = 'rgba(70,200,220,0.45)'; p.lineWidth = 0.7;
        p.beginPath(); p.arc(-d / 2, 0, r, 0, TAU); p.stroke();
        p.beginPath(); p.arc(d / 2, 0, r, 0, TAU); p.stroke();
        p.strokeStyle = 'rgba(70,220,170,0.75)'; p.lineWidth = 0.8;
        half = Math.acos(d / (2 * r));
        p.beginPath(); p.arc(-d / 2, 0, r, -half, half); p.arc(d / 2, 0, r, Math.PI - half, Math.PI + half); p.closePath(); p.stroke();
        p.restore();
    }
    function drawSpiral(p, t) {
        var maxR = 38, turns = 4, i, f, a, r;
        p.strokeStyle = TEAL_STRONG; p.lineWidth = 0.7;
        p.beginPath();
        for (i = 0; i <= 300; i++) { f = i / 300; a = f * turns * TAU + t * 0.05; r = f * maxR; if (i === 0) p.moveTo(Math.cos(a) * r, Math.sin(a) * r); else p.lineTo(Math.cos(a) * r, Math.sin(a) * r); }
        p.stroke();
        p.strokeStyle = 'rgba(70,190,220,0.35)'; p.lineWidth = 0.5;
        p.beginPath();
        for (i = 0; i <= 300; i++) { f = i / 300; a = f * turns * TAU + t * 0.05 + Math.PI; r = f * maxR; if (i === 0) p.moveTo(Math.cos(a) * r, Math.sin(a) * r); else p.lineTo(Math.cos(a) * r, Math.sin(a) * r); }
        p.stroke();
    }
    function drawLissajous(p, t) {
        var a = 3, b = 2, R = 38, i, phi, x, y;
        p.strokeStyle = 'rgba(70,210,170,0.60)'; p.lineWidth = 0.8;
        p.beginPath();
        for (i = 0; i <= 400; i++) { phi = (i / 400) * TAU; x = R * Math.sin(a * phi + t * 0.04); y = R * Math.sin(b * phi); if (i === 0) p.moveTo(x, y); else p.lineTo(x, y); }
        p.stroke();
    }
    function drawStar5(p, t) {
        var r = 36, ir = 14, pts = 5, rot = t * 0.04, i, rad, a, j, op;
        p.strokeStyle = TEAL; p.lineWidth = 0.7;
        p.beginPath();
        for (i = 0; i <= pts * 2; i++) { rad = (i % 2 === 0) ? r : ir; a = (i / (pts * 2)) * TAU + rot - Math.PI / 2; if (i === 0) p.moveTo(Math.cos(a) * rad, Math.sin(a) * rad); else p.lineTo(Math.cos(a) * rad, Math.sin(a) * rad); }
        p.closePath(); p.stroke();
        p.strokeStyle = 'rgba(70,200,220,0.38)'; p.lineWidth = 0.6;
        p.beginPath();
        for (i = 0; i <= pts; i++) { a = (i / pts) * TAU + rot - Math.PI / 2; if (i === 0) p.moveTo(Math.cos(a) * ir, Math.sin(a) * ir); else p.lineTo(Math.cos(a) * ir, Math.sin(a) * ir); }
        p.closePath(); p.stroke();
        op = []; for (i = 0; i < pts; i++) { a = (i / pts) * TAU + rot - Math.PI / 2; op.push([Math.cos(a) * r, Math.sin(a) * r]); }
        p.strokeStyle = 'rgba(70,220,170,0.28)'; p.lineWidth = 0.5;
        for (i = 0; i < pts; i++) { j = (i + 2) % pts; p.beginPath(); p.moveTo(op[i][0], op[i][1]); p.lineTo(op[j][0], op[j][1]); p.stroke(); }
    }
    function drawTorus(p, t) {
        var R = 22, r = 10, rings = 8, i, phi, cx, cy, sc, al;
        for (i = 0; i < rings; i++) { phi = (i / rings) * TAU + t * 0.03; cx = Math.cos(phi) * R; cy = Math.sin(phi) * R * 0.35; sc = 0.5 + 0.5 * Math.abs(Math.cos(phi)); al = (0.3 + 0.3 * Math.abs(Math.cos(phi))).toFixed(2); p.strokeStyle = 'rgba(70,210,170,' + al + ')'; p.lineWidth = 0.6; p.beginPath(); p.ellipse(cx, cy, r * sc, r * 0.35, phi, 0, TAU); p.stroke(); }
        p.strokeStyle = 'rgba(70,200,220,0.45)'; p.lineWidth = 0.7;
        p.beginPath(); p.ellipse(0, 0, R + r, (R + r) * 0.35, 0, 0, TAU); p.stroke();
        p.beginPath(); p.ellipse(0, 0, R - r, (R - r) * 0.35, 0, 0, TAU); p.stroke();
    }
    function drawHelix(p, t) {
        var R = 14, turns = 3, i, f, ang, y;
        p.lineWidth = 0.8;
        p.strokeStyle = TEAL_STRONG; p.beginPath();
        for (i = 0; i <= 300; i++) { f = i / 300; ang = f * turns * TAU + t * 0.05; y = f * 56 - 28; if (i === 0) p.moveTo(Math.cos(ang) * R, y); else p.lineTo(Math.cos(ang) * R, y); }
        p.stroke();
        p.strokeStyle = 'rgba(70,190,220,0.50)'; p.beginPath();
        for (i = 0; i <= 300; i++) { f = i / 300; ang = f * turns * TAU + t * 0.05 + Math.PI; y = f * 56 - 28; if (i === 0) p.moveTo(Math.cos(ang) * R, y); else p.lineTo(Math.cos(ang) * R, y); }
        p.stroke();
        p.strokeStyle = 'rgba(70,220,170,0.22)'; p.lineWidth = 0.5;
        for (i = 0; i <= 300; i += 25) { f = i / 300; ang = f * turns * TAU + t * 0.05; y = f * 56 - 28; p.beginPath(); p.moveTo(Math.cos(ang) * R, y); p.lineTo(Math.cos(ang + Math.PI) * R, y); p.stroke(); }
    }
    function drawRose(p, t) {
        var R = 38, k = 3, i, th, r;
        p.strokeStyle = 'rgba(70,210,170,0.70)'; p.lineWidth = 0.8;
        p.beginPath();
        for (i = 0; i <= 720; i++) { th = (i / 720) * TAU; r = R * Math.cos(k * (th + t * 0.018)); if (i === 0) p.moveTo(r * Math.cos(th), r * Math.sin(th)); else p.lineTo(r * Math.cos(th), r * Math.sin(th)); }
        p.stroke();
        p.strokeStyle = CYAN_FAINT; p.lineWidth = 0.4;
        p.beginPath(); p.arc(0, 0, R, 0, TAU); p.stroke();
    }
    function drawCardioid(p, t) {
        var a = 18, rot = t * 0.012, i, th, r;
        p.strokeStyle = TEAL_STRONG; p.lineWidth = 0.8;
        p.beginPath();
        for (i = 0; i <= 360; i++) { th = (i / 360) * TAU; r = a * (1 + Math.cos(th)); if (i === 0) p.moveTo(r * Math.cos(th + rot), r * Math.sin(th + rot)); else p.lineTo(r * Math.cos(th + rot), r * Math.sin(th + rot)); }
        p.stroke();
        p.strokeStyle = CYAN_FAINT; p.lineWidth = 0.5;
        p.beginPath(); p.arc(0, 0, a * 2, 0, TAU); p.stroke();
    }
    function drawAsteroid(p, t) {
        var R = 36, rot = t * 0.02, i, ph, x, y, a;
        p.strokeStyle = TEAL_STRONG; p.lineWidth = 0.8;
        p.beginPath();
        for (i = 0; i <= 360; i++) { ph = (i / 360) * TAU; x = R * Math.pow(Math.cos(ph + rot), 3); y = R * Math.pow(Math.sin(ph + rot), 3); if (i === 0) p.moveTo(x, y); else p.lineTo(x, y); }
        p.stroke();
        p.strokeStyle = 'rgba(70,200,220,0.25)'; p.lineWidth = 0.5;
        p.beginPath(); p.arc(0, 0, R, 0, TAU); p.stroke();
        for (i = 0; i < 4; i++) { a = (i / 4) * TAU + rot; p.strokeStyle = 'rgba(70,220,170,0.18)'; p.lineWidth = 0.4; p.beginPath(); p.moveTo(0, 0); p.lineTo(Math.cos(a) * R, Math.sin(a) * R); p.stroke(); }
    }
    function drawEpicycloid(p, t) {
        var Rc = 24, rc = 8, rot = t * 0.015, i, ph, x, y;
        p.strokeStyle = TEAL_STRONG; p.lineWidth = 0.8;
        p.beginPath();
        for (i = 0; i <= 600; i++) { ph = (i / 600) * TAU + rot; x = (Rc + rc) * Math.cos(ph) - rc * Math.cos((Rc / rc + 1) * ph); y = (Rc + rc) * Math.sin(ph) - rc * Math.sin((Rc / rc + 1) * ph); if (i === 0) p.moveTo(x, y); else p.lineTo(x, y); }
        p.stroke();
        p.strokeStyle = CYAN_FAINT; p.lineWidth = 0.5;
        p.beginPath(); p.arc(0, 0, Rc, 0, TAU); p.stroke();
    }
    function drawWeb(p, t) {
        var rings = 5, spokes = 8, maxR = 40, rot = t * 0.01, s, a, ri, r;
        p.strokeStyle = 'rgba(70,200,220,0.35)'; p.lineWidth = 0.5;
        for (s = 0; s < spokes; s++) { a = (s / spokes) * TAU + rot; p.beginPath(); p.moveTo(0, 0); p.lineTo(Math.cos(a) * maxR, Math.sin(a) * maxR); p.stroke(); }
        p.strokeStyle = TEAL; p.lineWidth = 0.7;
        for (ri = 1; ri <= rings; ri++) { r = (ri / rings) * maxR; p.beginPath(); for (s = 0; s <= spokes; s++) { a = (s / spokes) * TAU + rot; if (s === 0) p.moveTo(Math.cos(a) * r, Math.sin(a) * r); else p.lineTo(Math.cos(a) * r, Math.sin(a) * r); } p.closePath(); p.stroke(); }
    }
    function drawTrefoil(p, t) {
        var rot = t * 0.025, R = 18, i, u, x, y, z, al;
        p.lineWidth = 0.85;
        p.beginPath();
        for (i = 0; i <= 480; i++) {
            u = (i / 480) * TAU + rot;
            x = Math.sin(u) + 2 * Math.sin(2 * u); y = Math.cos(u) - 2 * Math.cos(2 * u); z = -Math.sin(3 * u);
            al = (0.30 + 0.50 * (z * 0.5 + 0.5)).toFixed(2); p.strokeStyle = 'rgba(70,210,170,' + al + ')';
            if (i === 0) p.moveTo(x * R, y * R); else { p.lineTo(x * R, y * R); p.stroke(); p.beginPath(); p.moveTo(x * R, y * R); }
        }
        p.strokeStyle = CYAN_FAINT; p.lineWidth = 0.5;
        p.beginPath(); p.arc(0, 0, R * 2.4, 0, TAU); p.stroke();
    }
    function drawFig8Knot(p, t) {
        var rot = t * 0.03, R = 10, i, u, x, y, z, al;
        p.lineWidth = 0.85;
        p.beginPath();
        for (i = 0; i <= 520; i++) {
            u = (i / 520) * TAU + rot;
            x = (2 + Math.cos(2 * u)) * Math.cos(3 * u); y = (2 + Math.cos(2 * u)) * Math.sin(3 * u); z = Math.sin(4 * u);
            al = (0.28 + 0.45 * (z * 0.5 + 0.5)).toFixed(2); p.strokeStyle = 'rgba(70,220,170,' + al + ')';
            if (i === 0) p.moveTo(x * R, y * R); else { p.lineTo(x * R, y * R); p.stroke(); p.beginPath(); p.moveTo(x * R, y * R); }
        }
    }
    function kochPolyline(rot, R, depth) {
        var pts = [], i, a;
        for (i = 0; i < 3; i++) { a = (i / 3) * TAU + rot - Math.PI / 2; pts.push([Math.cos(a) * R, Math.sin(a) * R]); }
        pts.push(pts[0]);
        function iterate(seg) {
            var out = [], s, A, B, dx, dy, p1, p2, bx, by, apex;
            for (s = 0; s < seg.length - 1; s++) {
                A = seg[s]; B = seg[s + 1]; dx = B[0] - A[0]; dy = B[1] - A[1];
                p1 = [A[0] + dx / 3, A[1] + dy / 3]; p2 = [A[0] + 2 * dx / 3, A[1] + 2 * dy / 3];
                bx = (dx / 3) * Math.cos(-Math.PI / 3) - (dy / 3) * Math.sin(-Math.PI / 3);
                by = (dx / 3) * Math.sin(-Math.PI / 3) + (dy / 3) * Math.cos(-Math.PI / 3);
                apex = [p1[0] + bx, p1[1] + by];
                out.push(A); out.push(p1); out.push(apex); out.push(p2);
            }
            out.push(seg[seg.length - 1]); return out;
        }
        for (i = 0; i < depth; i++) pts = iterate(pts);
        return pts;
    }
    function drawKochN(p, t, depth) {
        var pts = kochPolyline(t * 0.015, 36, depth), i;
        p.strokeStyle = 'rgba(70,210,170,0.62)'; p.lineWidth = 0.7;
        p.beginPath();
        for (i = 0; i < pts.length; i++) { if (i === 0) p.moveTo(pts[i][0], pts[i][1]); else p.lineTo(pts[i][0], pts[i][1]); }
        p.stroke();
    }
    function drawKoch(p, t) { drawKochN(p, t, 3); }
    function sierpinskiTris(rot, R, depth) {
        var seed = [], i, a;
        for (i = 0; i < 3; i++) { a = (i / 3) * TAU + rot - Math.PI / 2; seed.push([Math.cos(a) * R, Math.sin(a) * R]); }
        function mid(q, r) { return [(q[0] + r[0]) / 2, (q[1] + r[1]) / 2]; }
        var tris = [seed], iter, next, ti, T, m01, m12, m20;
        for (iter = 0; iter < depth; iter++) {
            next = [];
            for (ti = 0; ti < tris.length; ti++) { T = tris[ti]; m01 = mid(T[0], T[1]); m12 = mid(T[1], T[2]); m20 = mid(T[2], T[0]); next.push([T[0], m01, m20]); next.push([m01, T[1], m12]); next.push([m20, m12, T[2]]); }
            tris = next;
        }
        return tris;
    }
    function drawSierpinskiN(p, t, depth) {
        var tris = sierpinskiTris(t * 0.012, 42, depth), ti, T;
        p.strokeStyle = 'rgba(70,210,170,0.45)'; p.lineWidth = 0.5;
        p.beginPath();
        for (ti = 0; ti < tris.length; ti++) { T = tris[ti]; p.moveTo(T[0][0], T[0][1]); p.lineTo(T[1][0], T[1][1]); p.lineTo(T[2][0], T[2][1]); p.closePath(); }
        p.stroke();
    }
    function drawSierpinski(p, t) { drawSierpinskiN(p, t, 4); }
    function drawKlein(p, t) {
        var rot = t * 0.02, S = 16, ui, u, vi, v, q;
        function K(u, v) {
            var cu = Math.cos(u), su = Math.sin(u), cv = Math.cos(v), sv = Math.sin(v);
            var x = -(2 / 15) * cu * (3 * cv - 30 * su + 90 * Math.pow(cu, 4) * su - 60 * Math.pow(cu, 6) * su + 5 * cu * cv * su);
            var y = -(1 / 15) * su * (3 * cv - 3 * Math.pow(cu, 2) * cv - 48 * Math.pow(cu, 4) * cv + 48 * Math.pow(cu, 6) * cv - 60 * su + 5 * cu * cv * su - 5 * Math.pow(cu, 3) * cv * su - 80 * Math.pow(cu, 5) * cv * su + 80 * Math.pow(cu, 7) * cv * su);
            var rc = Math.cos(rot), rs = Math.sin(rot);
            return [(x * rc - y * rs) * S, (x * rs + y * rc) * S];
        }
        p.lineWidth = 0.55;
        for (ui = 0; ui < 14; ui++) {
            u = (ui / 14) * TAU; p.strokeStyle = 'rgba(70,210,170,' + (0.30 + 0.30 * Math.sin(u + rot)).toFixed(2) + ')';
            p.beginPath();
            for (vi = 0; vi <= 42; vi++) { v = (vi / 42) * TAU; q = K(u, v); if (vi === 0) p.moveTo(q[0], q[1]); else p.lineTo(q[0], q[1]); }
            p.stroke();
        }
    }
    function drawHyperPara(p, t) {
        var rot = t * 0.025, tilt = Math.sin(t * 0.012) * 0.4 + 0.6, R = 18, N = 8, i, j, u, v, q;
        function P(u, v) {
            var x = u, y = v, z = (u * u - v * v) * 0.4;
            var cy = Math.cos(rot), sy = Math.sin(rot);
            var x1 = x * cy + z * sy, z1 = -x * sy + z * cy;
            var cx = tilt, sx = Math.sqrt(Math.max(0, 1 - tilt * tilt));
            var y1 = y * cx - z1 * sx;
            var d = 2.6 / (2.6 + z1 * 0.3);
            return [x1 * d * R, y1 * d * R];
        }
        p.lineWidth = 0.55;
        for (i = 0; i <= N; i++) { u = -1 + i * (2 / N); p.strokeStyle = 'rgba(70,210,170,' + (0.35 + 0.25 * Math.abs(u)).toFixed(2) + ')'; p.beginPath(); for (j = 0; j <= N; j++) { v = -1 + j * (2 / N); q = P(u, v); if (j === 0) p.moveTo(q[0], q[1]); else p.lineTo(q[0], q[1]); } p.stroke(); }
        for (j = 0; j <= N; j++) { v = -1 + j * (2 / N); p.strokeStyle = 'rgba(70,190,220,' + (0.30 + 0.25 * Math.abs(v)).toFixed(2) + ')'; p.beginPath(); for (i = 0; i <= N; i++) { u = -1 + i * (2 / N); q = P(u, v); if (i === 0) p.moveTo(q[0], q[1]); else p.lineTo(q[0], q[1]); } p.stroke(); }
    }
    function drawTrochoid(p, t) {
        var Rc = 22, rc = 7, d = 11, rot = t * 0.022, i, ph, x, y;
        p.strokeStyle = TEAL_STRONG; p.lineWidth = 0.75;
        p.beginPath();
        for (i = 0; i <= 720; i++) { ph = (i / 720) * TAU * 7 + rot; x = (Rc + rc) * Math.cos(ph) - d * Math.cos(((Rc + rc) / rc) * ph); y = (Rc + rc) * Math.sin(ph) - d * Math.sin(((Rc + rc) / rc) * ph); if (i === 0) p.moveTo(x, y); else p.lineTo(x, y); }
        p.stroke();
        p.strokeStyle = CYAN_FAINT; p.lineWidth = 0.5;
        p.beginPath(); p.arc(0, 0, Rc, 0, TAU); p.stroke();
    }
    function drawButterfly(p, t) {
        var R = 11, rot = t * 0.018, i, u, f, x, y;
        p.strokeStyle = 'rgba(70,220,170,0.70)'; p.lineWidth = 0.8;
        p.beginPath();
        for (i = 0; i <= 600; i++) { u = (i / 600) * 12 * Math.PI; f = Math.exp(Math.cos(u)) - 2 * Math.cos(4 * u) - Math.pow(Math.sin(u / 12), 5); x = Math.sin(u + rot) * f; y = Math.cos(u + rot) * f; if (i === 0) p.moveTo(x * R, y * R); else p.lineTo(x * R, y * R); }
        p.stroke();
    }
    function drawHypocy(p, t) {
        var Rc = 34, rc = 9, rot = t * 0.025, k = (Rc - rc) / rc, i, ph, x, y;
        p.strokeStyle = TEAL_STRONG; p.lineWidth = 0.8;
        p.beginPath();
        for (i = 0; i <= 400; i++) { ph = (i / 400) * TAU + rot; x = (Rc - rc) * Math.cos(ph) + rc * Math.cos(k * ph); y = (Rc - rc) * Math.sin(ph) - rc * Math.sin(k * ph); if (i === 0) p.moveTo(x, y); else p.lineTo(x, y); }
        p.stroke();
        p.strokeStyle = CYAN_FAINT; p.lineWidth = 0.5;
        p.beginPath(); p.arc(0, 0, Rc, 0, TAU); p.stroke();
    }

    var LEGACY_PARAM = {
        flower: drawFlower, metatron: drawMetatron, vesica: drawVesica, spiral: drawSpiral,
        lissajous: drawLissajous, star5: drawStar5, torus: drawTorus, helix: drawHelix,
        rose: drawRose, cardioid: drawCardioid, asteroid: drawAsteroid, epicycloid: drawEpicycloid,
        web: drawWeb, trefoil: drawTrefoil, fig8knot: drawFig8Knot, koch: drawKoch,
        sierpinski: drawSierpinski, klein: drawKlein, hyperpara: drawHyperPara,
        trochoid: drawTrochoid, butterfly: drawButterfly, hypocy: drawHypocy
    };
    var LEGACY_PARAM_LABEL = {
        flower: 'flower of life', metatron: "metatron's cube", vesica: 'vesica piscis', spiral: 'archimedean spiral',
        lissajous: 'lissajous 3:2', star5: 'pentagram', torus: 'torus rings', helix: 'helix',
        rose: 'rose k=3', cardioid: 'cardioid', asteroid: 'astroid', epicycloid: 'epicycloid',
        web: 'radial web', trefoil: 'trefoil knot', fig8knot: 'figure-8 knot', koch: 'koch snowflake',
        sierpinski: 'sierpinski triangle', klein: 'klein bottle', hyperpara: 'hyperbolic paraboloid',
        trochoid: 'epitrochoid', butterfly: 'butterfly curve', hypocy: 'hypocycloid'
    };
    // The original GEO_KEYS, verbatim — defines band 0 (indices 0..84).
    var GEO_KEYS = ['tetra','cube','octa','icosa','flower','metatron','prism','stella','cubocta','antiprism','vesica','spiral','lissajous','star5','torus','helix','dodeca','pyramid','pentaprism','rose','cardioid','asteroid','epicycloid','web','hexprism','hexanti','pentabipy','tribipy','trunctetra','octapyramid','rhombdo','frustum','rhombpara','starbipy',
        'heptaprism','octaprism','nonaprism','decaprism','dodecaprism','cylinder',
        'heptanti','octanti','nonanti','decanti','dodecanti',
        'hexbipy','heptbipy','octbipy','nonabipy','decabipy',
        'pentapy','hexapy','heptapy','cone',
        'heptagram','octagram','nonagram','decagram',
        'tesseract','compoundCO','obelisk','spire','wedge','lattice3','crystalPair',
        'hendecaprism','tridecaprism','pentadecaprism','hendecanti','tridecanti',
        'octapy','decapy','trunccube','gyropy','dnahelix',
        'trefoil','fig8knot','koch','sierpinski',
        'klein','hyperpara','trochoid','butterfly','hypocy'];

    // ── Generic parametric-curve painter for the new bands ──────────────────────────────────────
    // fn(u, t) -> [x, y] in design units (~±45). Optional trailing base circle.
    function curvePainter(fn, opts) {
        opts = opts || {};
        var color = opts.color || 'rgba(70,210,170,0.62)', width = opts.width || 0.8;
        var N = opts.samples || 480, span = opts.span || TAU, base = opts.baseCircle || 0;
        return function (p, t) {
            p.strokeStyle = color; p.lineWidth = width;
            p.beginPath();
            for (var k = 0; k <= N; k++) { var u = (k / N) * span; var q = fn(u, t); if (k === 0) p.moveTo(q[0], q[1]); else p.lineTo(q[0], q[1]); }
            if (opts.closed) p.closePath();
            p.stroke();
            if (base) { p.strokeStyle = CYAN_FAINT; p.lineWidth = 0.5; p.beginPath(); p.arc(0, 0, base, 0, TAU); p.stroke(); }
        };
    }

    function gcd(a, b) { a = Math.abs(a); b = Math.abs(b); while (b) { var m = a % b; a = b; b = m; } return a; }

    // ── Catalog assembly ────────────────────────────────────────────────────────────────────────
    var POLY3D_POSTER = 2.2;   // baked rotation so polyhedra posters show a 3/4 view, never axis-aligned

    function specPoly3d(label, sh) {
        return { kind: 'poly3d', label: label, posterT: POLY3D_POSTER, key: 'p3:' + label,
                 paint: function (p, t, style) { renderPoly3d(p, sh, t, style); } };
    }
    function specCurve(kind, label, key, fn, opts) {
        return { kind: kind, label: label, posterT: (opts && opts.posterT) || 0, key: key,
                 paint: curvePainter(fn, opts) };
    }

    var catalog = [];
    var seenFp = {};
    // Geometric fingerprint of a spec: paint at two phases, quantize coords, FNV-1a hash. Two specs
    // that render identical strokes share a fingerprint, so add() rejects geometric duplicates —
    // this is what lets each family sweep skip shapes already present (e.g. band-0's heptagonal
    // prism) and keep climbing n until it finds genuinely new geometry.
    function fpOf(spec) {
        var str = spec.kind + '|', ph = [0, 1.3], pi, k, j;
        for (pi = 0; pi < ph.length; pi++) {
            var pen = new CollectPen();
            spec.paint(pen, ph[pi], {});
            for (k = 0; k < pen.polylines.length; k++) {
                var pts = pen.polylines[k].pts;
                for (j = 0; j < pts.length; j++) str += (Math.round(pts[j][0] * 100) / 100) + ',' + (Math.round(pts[j][1] * 100) / 100) + ';';
                str += '/';
            }
            str += '#';
            for (k = 0; k < pen.dots.length; k++) str += (Math.round(pen.dots[k].x * 100) / 100) + ',' + (Math.round(pen.dots[k].y * 100) / 100) + ';';
            str += '||';
        }
        var h = 2166136261;
        for (k = 0; k < str.length; k++) { h ^= str.charCodeAt(k); h = Math.imul(h, 16777619); }
        return (h >>> 0).toString(16);
    }
    function add(spec) { var f = fpOf(spec); if (seenFp[f]) return false; seenFp[f] = 1; catalog.push(spec); return true; }
    function addForce(spec) { seenFp[fpOf(spec)] = 1; catalog.push(spec); }   // band 0: always include

    // Band 0 — the 84 legacy shapes (original 85 minus the removed harmonograph), in original order.
    // addForce: every legacy shape is included.
    GEO_KEYS.forEach(function (key) {
        if (GEO[key]) addForce(specPoly3d(GEO[key].label, GEO[key]));
        else addForce({ kind: 'param', label: LEGACY_PARAM_LABEL[key] || key, posterT: 0,
                        paint: (function (fn) { return function (p, t) { fn(p, t); }; })(LEGACY_PARAM[key]) });
    });

    // Band 1 — elongated bipyramids (prism body + pyramid caps): a topology no other band produces.
    // Sweep n until 40 new shapes land.
    (function () {
        function ngElongBipy(n, r, h, cap) {
            var v = [[0, h + cap, 0], [0, -h - cap, 0]], e = [], i, a;
            for (i = 0; i < n; i++) { a = i * TAU / n; v.push([Math.cos(a) * r, h, Math.sin(a) * r]); }
            for (i = 0; i < n; i++) { a = i * TAU / n; v.push([Math.cos(a) * r, -h, Math.sin(a) * r]); }
            for (i = 0; i < n; i++) {
                e.push([2 + i, 2 + (i + 1) % n]); e.push([2 + n + i, 2 + n + (i + 1) % n]);
                e.push([2 + i, 2 + n + i]); e.push([0, 2 + i]); e.push([1, 2 + n + i]);
            }
            return { verts: v, edges: e };
        }
        var target = catalog.length + 41, n = 3;   // +1: absorbs a freed harmonograph slot
        while (catalog.length < target && n < 400) { add(specPoly3d(n + '-gon elongated bipyramid', ngElongBipy(n, 1, 0.7, 0.8))); n++; }
    })();

    // Band 2 — n-gonal prisms, n = 3..
    (function () { var target = catalog.length + 101, n = 3; while (catalog.length < target) { add(specPoly3d(n + '-gon prism', ngPrism(n, 1, 1))); n++; if (n > 600) break; } })();
    // Band 3 — antiprisms
    (function () { var target = catalog.length + 91, n = 3; while (catalog.length < target) { add(specPoly3d(n + '-gon antiprism', ngAnti(n, 1, 0.85))); n++; if (n > 600) break; } })();
    // Band 4 — bipyramids
    (function () { var target = catalog.length + 81, n = 3; while (catalog.length < target) { add(specPoly3d(n + '-gon bipyramid', ngBipy(n, 1, 1.3))); n++; if (n > 600) break; } })();
    // Band 5 — pyramids
    (function () { var target = catalog.length + 71, n = 3; while (catalog.length < target) { add(specPoly3d(n + '-gon pyramid', ngPy(n, 1, 1))); n++; if (n > 600) break; } })();
    // Band 6 — star prisms {n/k}, coprime, 1 < k < n/2
    (function () { var target = catalog.length + 101, n = 5; while (catalog.length < target && n < 400) { for (var k = 2; k < n / 2 && catalog.length < target; k++) { if (gcd(n, k) === 1) add(specPoly3d(n + '/' + k + ' star prism', starPoly(n, k, 1, 0.4))); } n++; } })();

    // Band 7 — rose curves r = 38·cos((n/d)θ), coprime n≠d
    (function () {
        var target = catalog.length + 91, sum = 4;
        while (catalog.length < target && sum < 60) {
            for (var n = 1; n < sum && catalog.length < target; n++) {
                var d = sum - n; if (d < 1 || n === d || gcd(n, d) !== 1) continue;
                (function (n, d) {
                    var span = TAU * d;
                    add(specCurve('param2d', 'rose ' + n + '/' + d, 'rose:' + n + '/' + d,
                        function (u, t) { var r = 38 * Math.cos((n / d) * (u + t * 0.018)); return [r * Math.cos(u), r * Math.sin(u)]; },
                        { span: span, samples: 720, color: 'rgba(70,210,170,0.70)', baseCircle: 38 }));
                })(n, d);
            }
            sum++;
        }
    })();
    // Band 8 — Lissajous, coprime (a,b), a≤b, δ∈{0,π/4,π/2}
    (function () {
        var target = catalog.length + 71, deltas = [0, Math.PI / 4, Math.PI / 2], di = 0;
        outer: for (var b = 2; b < 30; b++) for (var a = 1; a <= b; a++) {
            if (gcd(a, b) !== 1) continue; if (a === 1 && b === 1) continue;
            for (di = 0; di < deltas.length; di++) {
                if (catalog.length >= target) break outer;
                (function (a, b, dl) {
                    add(specCurve('param2d', 'lissajous ' + a + ':' + b, 'liss:' + a + ':' + b + ':' + dl.toFixed(2),
                        function (u, t) { return [38 * Math.sin(a * u + dl + t * 0.04), 38 * Math.sin(b * u)]; },
                        { span: TAU, samples: 480, color: 'rgba(70,210,170,0.60)' }));
                })(a, b, deltas[di]);
            }
        }
    })();
    // Band 9 — epicycloid / hypocycloid, lowest-terms (R,r), alternating
    (function () {
        var target = catalog.length + 71, R = 2, mode = 0;
        while (catalog.length < target && R < 60) {
            for (var r = 1; r < R && catalog.length < target; r++) {
                if (gcd(R, r) !== 1) continue;
                if (mode === 0) (function (R, r) {
                    add(specCurve('param2d', 'epicycloid ' + R + '/' + r, 'epi:' + R + '/' + r,
                        function (u, t) { var s = 14, k = (R + r) / r; return [s * ((R + r) * Math.cos(u + t * 0.012) - r * Math.cos(k * (u + t * 0.012))) / R, s * ((R + r) * Math.sin(u + t * 0.012) - r * Math.sin(k * (u + t * 0.012))) / R]; },
                        { span: TAU * r, samples: 600, color: 'rgba(70,210,170,0.65)', baseCircle: 0 }));
                })(R, r);
                else (function (R, r) {
                    add(specCurve('param2d', 'hypocycloid ' + R + '/' + r, 'hypo:' + R + '/' + r,
                        function (u, t) { var s = 34, k = (R - r) / r; return [s * ((R - r) * Math.cos(u + t * 0.012) + r * Math.cos(k * (u + t * 0.012))) / R, s * ((R - r) * Math.sin(u + t * 0.012) - r * Math.sin(k * (u + t * 0.012))) / R]; },
                        { span: TAU * r, samples: 600, color: 'rgba(70,210,170,0.65)', baseCircle: 0 }));
                })(R, r);
                mode ^= 1;
            }
            R++;
        }
    })();
    // Band 10 — epitrochoid (R,r,d), lowest-terms
    (function () {
        var target = catalog.length + 61, R = 2, ds = [0.5, 1.5, 2.5];
        while (catalog.length < target && R < 40) {
            for (var r = 1; r < R && catalog.length < target; r++) {
                if (gcd(R, r) !== 1) continue;
                for (var j = 0; j < ds.length && catalog.length < target; j++) {
                    (function (R, r, dm) {
                        var d = r * dm;
                        add(specCurve('param2d', 'epitrochoid ' + R + '/' + r, 'troch:' + R + '/' + r + ':' + dm,
                            function (u, t) { var s = 12, k = (R + r) / r; return [s * ((R + r) * Math.cos(u + t * 0.01) - d * Math.cos(k * (u + t * 0.01))) / R, s * ((R + r) * Math.sin(u + t * 0.01) - d * Math.sin(k * (u + t * 0.01))) / R]; },
                            { span: TAU * r, samples: 600, color: 'rgba(70,200,220,0.6)' }));
                    })(R, r, ds[j]);
                }
            }
            R++;
        }
    })();
    // Band 11 — torus knots (p,q), gcd=1, 2≤p<q ; projected 3D with depth alpha
    (function () {
        var target = catalog.length + 41;
        outer: for (var q = 3; q < 30; q++) for (var pp = 2; pp < q; pp++) {
            if (gcd(pp, q) !== 1) continue;
            if (catalog.length >= target) break outer;
            (function (P, Q) {
                add({ kind: 'param3d', label: 'torus knot ' + P + ',' + Q, posterT: 0.6,
                    paint: function (p, t) {
                        var rot = t * 0.02, R = 16, i, u, x, y, z, al, c = Math.cos(rot), s = Math.sin(rot);
                        p.lineWidth = 0.8; p.beginPath();
                        for (i = 0; i <= 600; i++) {
                            u = (i / 600) * TAU;
                            var rad = 2 + Math.cos(Q * u);
                            x = rad * Math.cos(P * u); y = rad * Math.sin(P * u); z = Math.sin(Q * u);
                            var xr = x * c + z * s; z = -x * s + z * c; x = xr;
                            al = (0.30 + 0.5 * (z * 0.25 + 0.5)).toFixed(2); p.strokeStyle = 'rgba(70,210,170,' + al + ')';
                            if (i === 0) p.moveTo(x * R, y * R); else { p.lineTo(x * R, y * R); p.stroke(); p.beginPath(); p.moveTo(x * R, y * R); }
                        }
                    } });
            })(pp, q);
        }
    })();
    // Band 13 — extra fractal depths (harmonographs removed; the 5 fractals just before the superformula fill).
    (function () {
        [['koch depth 2', 2], ['koch depth 4', 4]].forEach(function (g) { add({ kind: 'fractal', label: g[0], posterT: 0, paint: (function (d) { return function (p, t) { drawKochN(p, t, d); }; })(g[1]) }); });
        [['sierpinski depth 3', 3], ['sierpinski depth 5', 5], ['sierpinski depth 6', 6]].forEach(function (g) { add({ kind: 'fractal', label: g[0], posterT: 0, paint: (function (d) { return function (p, t) { drawSierpinskiN(p, t, d); }; })(g[1]) }); });
    })();

    // Band 12 — Gielis superformula, swept over (m, n1, n2, n3). The elastic band: fill EXACTLY up
    // to 1024 so the catalog total is guaranteed regardless of minor drift in the bands above.
    (function () {
        function superR(theta, m, n1, n2, n3) {
            var t1 = Math.pow(Math.abs(Math.cos(m * theta / 4)), n2);
            var t2 = Math.pow(Math.abs(Math.sin(m * theta / 4)), n3);
            var r = Math.pow(t1 + t2, -1 / n1);
            return isFinite(r) ? r : 0;
        }
        var ms = [3, 4, 5, 6, 7, 8, 9, 10, 12, 16, 18, 24];
        var n1s = [0.3, 0.6, 1, 2, 3, 5, 7, 10, 20, 40];
        var n2s = [0.3, 0.6, 1, 1.7, 3, 5, 7];
        var n3s = [0.3, 0.6, 1, 1.7, 3, 5, 7];
        outer: for (var mi = 0; mi < ms.length; mi++)
            for (var i1 = 0; i1 < n1s.length; i1++)
                for (var i2 = 0; i2 < n2s.length; i2++)
                    for (var i3 = 0; i3 < n3s.length; i3++) {
                        if (catalog.length >= 1024) break outer;
                        (function (m, n1, n2, n3) {
                            add(specCurve('param2d', 'superformula m=' + m, 'super:' + m + ':' + n1 + ':' + n2 + ':' + n3,
                                function (u, t) {
                                    var r = superR(u + t * 0.01, m, n1, n2, n3) * 30;
                                    return [r * Math.cos(u), r * Math.sin(u)];
                                },
                                { span: TAU, samples: 540, color: 'rgba(70,220,190,0.62)' }));
                        })(ms[mi], n1s[i1], n2s[i2], n3s[i3]);
                    }
    })();

    if (catalog.length !== 1024) {
        throw new Error('SacredGeometry: catalog has ' + catalog.length + ' shapes, expected 1024');
    }

    // ── Public renderer ─────────────────────────────────────────────────────────────────────────
    function checkIdx(i) { if (typeof i !== 'number' || i < 0 || i >= 1024 || (i | 0) !== i) throw new RangeError('SacredGeometry index out of range: ' + i); }

    function draw(ctx, i, t, style) {
        checkIdx(i); style = style || {};
        var size = style.size || 110;
        ctx.save();
        ctx.translate(size / 2, size / 2);
        if (size !== 110) ctx.scale(size / 110, size / 110);
        catalog[i].paint(new CanvasPen(ctx), t || 0, style);
        ctx.restore();
    }

    function collect(i, t) {
        var pen = new CollectPen();
        catalog[i].paint(pen, (t == null ? (catalog[i].posterT || 0) : t), {});
        return pen;
    }

    function sample(i, t) {
        checkIdx(i);
        var pen = collect(i, t);
        return {
            polylines: pen.polylines.map(function (pl) { return pl.pts.map(function (q) { return [q[0] / 55, q[1] / 55]; }); }),
            dots: pen.dots.map(function (d) { return [d.x / 55, d.y / 55]; })
        };
    }

    function r2(v) { return Math.round(v * 100) / 100; }

    function svg(i, opts) {
        checkIdx(i); opts = opts || {};
        var size = opts.size || 110;
        var t = (opts.t != null) ? opts.t : (catalog[i].posterT || 0);
        var pen = new CollectPen();
        catalog[i].paint(pen, t, {});
        var out = ['<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 ' + size + ' ' + size + '" width="' + size + '" height="' + size + '">'];
        if (opts.bg !== 'none') out.push('<rect width="' + size + '" height="' + size + '" fill="' + (opts.bg || '#0a1014') + '"/>');
        out.push('<g transform="translate(' + (size / 2) + ' ' + (size / 2) + ') scale(' + r2(size / 110) + ')" fill="none" stroke-linecap="round">');
        pen.polylines.forEach(function (pl) {
            if (pl.pts.length < 2) return;
            var pts = pl.pts.map(function (q) { return r2(q[0]) + ',' + r2(q[1]); }).join(' ');
            out.push('<polyline points="' + pts + '" stroke="' + pl.color + '" stroke-width="' + pl.width + '"/>');
        });
        pen.dots.forEach(function (d) { out.push('<circle cx="' + r2(d.x) + '" cy="' + r2(d.y) + '" r="' + r2(d.r) + '" fill="' + d.color + '" stroke="none"/>'); });
        out.push('</g></svg>');
        return out.join('');
    }

    function label(i) { checkIdx(i); return catalog[i].label; }

    function spec(i) {
        checkIdx(i);
        var s = catalog[i];
        return Object.freeze({ index: i, kind: s.kind, label: s.label, posterT: s.posterT || 0 });
    }

    function fingerprint(i) { checkIdx(i); return fpOf(catalog[i]); }

    return {
        count: catalog.length,
        spec: spec,
        sample: sample,
        draw: draw,
        svg: svg,
        label: label,
        fingerprint: fingerprint
    };
});
