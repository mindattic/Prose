// DCM-Viz renderer (observability plan, Phase 7) — adapted from the original, standalone
// `prose --dcm-viz` export (Prose.Core/Services/DcmVisualizationService.cs's embedded
// <script>), simplified for one fixed dark palette (no light/dark toggle — the rest of the
// observer UI doesn't have one either) and scoped to a container element instead of the
// whole document, so it can be called repeatedly against a live-updating root.
//
// window.dcmVizInterop.render(rootElementId, payloadJson) does a full rebuild every call —
// not a true incremental DOM patch. That's a deliberate simplification: the live path calls
// this once per beat with the whole run-so-far's payload (cheap to recompute and cheap to
// redraw for realistic beat counts), so history mode and live mode share the exact same
// entry point and payload shape.
(function () {
    'use strict';

    const COLOR = {
        bg: '#0d1117', surf: '#161b22', line: '#30363d', ink: '#e6edf3', mut: '#8b949e',
        always: '#f0a868', node: '#6cb6ff', topic: '#7ee787',
    };
    const SVG_NS = 'http://www.w3.org/2000/svg';

    function svgEl(tag, attrs) {
        const el = document.createElementNS(SVG_NS, tag);
        for (const [k, v] of Object.entries(attrs || {})) el.setAttribute(k, String(v));
        return el;
    }
    function svgText(x, y, txt, attrs) {
        const el = svgEl('text', Object.assign({ x, y, 'font-family': 'ui-monospace,SFMono-Regular,Menlo,monospace', 'font-size': '11' }, attrs || {}));
        el.textContent = txt;
        return el;
    }
    function esc(s) { return String(s).replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;'); }
    function tierColor(tier) { return COLOR[tier] || COLOR.mut; }

    function render(rootElementId, payload) {
        const root = document.getElementById(rootElementId);
        if (!root) return;
        const D = typeof payload === 'string' ? JSON.parse(payload) : payload;
        if (!D) return;

        root.innerHTML = '';
        root.style.fontFamily = 'ui-monospace,SFMono-Regular,Menlo,monospace';
        root.style.color = COLOR.ink;

        const tip = document.createElement('div');
        tip.style.cssText = 'position:fixed;pointer-events:none;background:' + COLOR.surf + ';border:1px solid ' + COLOR.line +
            ';padding:8px 10px;border-radius:6px;font-size:11px;max-width:380px;display:none;z-index:99;line-height:1.6;color:' + COLOR.ink;
        document.body.appendChild(tip);
        function showTip(e, html) {
            tip.style.display = 'block'; tip.innerHTML = html;
            const pad = 14, tw = tip.offsetWidth, th = tip.offsetHeight;
            let x = e.clientX + pad, y = e.clientY + pad;
            if (x + tw > window.innerWidth - pad) x = e.clientX - tw - pad;
            if (y + th > window.innerHeight - pad) y = e.clientY - th - pad;
            tip.style.left = x + 'px'; tip.style.top = y + 'px';
        }
        function hideTip() { tip.style.display = 'none'; }

        // ── Stat cards ───────────────────────────────────────────────────────
        const avg = D.counts.length ? (D.counts.reduce((s, b) => s + b.n, 0) / D.counts.length).toFixed(1) : '0';
        const cards = document.createElement('div');
        cards.style.cssText = 'display:flex;flex-wrap:wrap;gap:10px;padding:0 0 14px;font-size:12px';
        [['Total beats', D.totalBeats], ['Unique docs', D.docs.length], ['Avg active', avg], ['Peak active', D.maxActive],
         ['always', D.docs.filter(d => d.tier === 'always').length],
         ['node', D.docs.filter(d => d.tier === 'node').length],
         ['topic', D.docs.filter(d => d.tier === 'topic').length]].forEach(([k, v]) => {
            const c = document.createElement('div');
            c.style.cssText = 'background:' + COLOR.surf + ';border:1px solid ' + COLOR.line + ';border-radius:8px;padding:8px 12px;min-width:90px';
            c.innerHTML = '<div style="color:' + COLOR.mut + ';font-size:10px;text-transform:uppercase">' + esc(k) +
                '</div><div style="font-size:18px;font-weight:600;margin-top:2px">' + esc(v) + '</div>';
            cards.appendChild(c);
        });
        root.appendChild(cards);

        if (D.totalBeats === 0) {
            const empty = document.createElement('p');
            empty.style.color = COLOR.mut;
            empty.textContent = 'No beats recorded yet.';
            root.appendChild(empty);
            return;
        }

        // ── Shared X geometry ────────────────────────────────────────────────
        const LABEL_W = 220, MARG_R = 20, MARG_B = 28;
        const BEAT_W = Math.max(4, Math.min(16, Math.floor(860 / D.totalBeats)));
        const CHART_W = LABEL_W + D.totalBeats * BEAT_W + MARG_R;
        const bx = i => LABEL_W + i * BEAT_W;
        const TICK = Math.max(1, Math.round(D.totalBeats / 10));

        function scrollWrap() {
            const w = document.createElement('div');
            w.style.cssText = 'overflow-x:auto;border:1px solid ' + COLOR.line + ';border-radius:8px;background:' + COLOR.surf + ';margin-bottom:16px';
            return w;
        }

        // ── Chart 1: active-doc-count area ───────────────────────────────────
        {
            const h2 = document.createElement('div');
            h2.style.cssText = 'font-size:11px;color:' + COLOR.mut + ';text-transform:uppercase;margin:8px 0 6px';
            h2.textContent = 'Active .md count per beat';
            root.appendChild(h2);

            const H = 80, MARG_T = 16, totalH = H + MARG_T + MARG_B;
            const svg = svgEl('svg', { width: CHART_W, height: totalH, viewBox: '0 0 ' + CHART_W + ' ' + totalH, style: 'display:block' });
            const mx = D.maxActive || 1;
            const cy = n => MARG_T + H - Math.round(n / mx * H);

            svg.appendChild(svgEl('rect', { x: LABEL_W, y: MARG_T, width: D.totalBeats * BEAT_W, height: H, fill: COLOR.bg, stroke: COLOR.line }));
            [0, Math.round(mx / 2), mx].forEach(v => {
                const y = cy(v);
                svg.appendChild(svgEl('line', { x1: LABEL_W, y1: y, x2: LABEL_W + D.totalBeats * BEAT_W, y2: y, stroke: COLOR.line }));
                svg.appendChild(svgText(LABEL_W - 5, y + 4, String(v), { 'text-anchor': 'end', fill: COLOR.mut }));
            });

            if (D.counts.length > 0) {
                const pts = D.counts.map(b => [bx(b.i) + BEAT_W / 2, cy(b.n)]);
                const first = pts[0], last = pts[pts.length - 1];
                const areaD = 'M' + first[0] + ',' + (MARG_T + H) + ' ' + pts.map(([x, y]) => 'L' + x + ',' + y).join(' ') + ' L' + last[0] + ',' + (MARG_T + H) + ' Z';
                const lineD = 'M' + pts.map(([x, y]) => x + ',' + y).join(' L');
                svg.appendChild(svgEl('path', { d: areaD, fill: COLOR.node, opacity: '0.2' }));
                svg.appendChild(svgEl('path', { d: lineD, fill: 'none', stroke: COLOR.node, 'stroke-width': '1.5' }));
            }

            for (let i = 0; i < D.totalBeats; i += TICK) {
                const x = bx(i) + BEAT_W / 2;
                svg.appendChild(svgEl('line', { x1: x, y1: MARG_T + H, x2: x, y2: MARG_T + H + 4, stroke: COLOR.mut }));
                svg.appendChild(svgText(x, totalH - 6, String(i), { 'text-anchor': 'middle', fill: COLOR.mut }));
            }

            D.counts.forEach(b => {
                const r = svgEl('rect', { x: bx(b.i), y: MARG_T, width: BEAT_W, height: H, fill: 'transparent', style: 'cursor:crosshair' });
                r.addEventListener('mouseenter', e => showTip(e, '<b>Beat #' + b.i + '</b>' + (b.t ? '<br>' + esc(b.t) : '') + '<br>Active: <b>' + b.n + '</b> docs'));
                r.addEventListener('mouseleave', hideTip);
                svg.appendChild(r);
            });

            const wrap = scrollWrap();
            wrap.appendChild(svg);
            root.appendChild(wrap);
        }

        // ── Chart 2: Gantt ───────────────────────────────────────────────────
        {
            const h2 = document.createElement('div');
            h2.style.cssText = 'font-size:11px;color:' + COLOR.mut + ';text-transform:uppercase;margin:8px 0 6px';
            h2.textContent = 'DCM Gantt — one row per .md file; gap = evicted';
            root.appendChild(h2);

            const ROW_H = 24, BAR_H = 14, MARG_T = 20;
            const nRows = D.docs.length;
            const totalH = MARG_T + nRows * ROW_H + MARG_B;
            const svg = svgEl('svg', { width: CHART_W, height: totalH, viewBox: '0 0 ' + CHART_W + ' ' + totalH, style: 'display:block' });

            for (let i = 0; i < D.totalBeats; i += TICK) {
                const x = bx(i);
                svg.appendChild(svgEl('line', { x1: x, y1: MARG_T, x2: x, y2: MARG_T + nRows * ROW_H, stroke: COLOR.line, 'stroke-dasharray': '2,3' }));
            }

            D.docs.forEach((doc, ri) => {
                const rowY = MARG_T + ri * ROW_H;
                const barY = rowY + (ROW_H - BAR_H) / 2;
                const color = tierColor(doc.tier);
                svg.appendChild(svgEl('rect', { x: LABEL_W, y: rowY, width: D.totalBeats * BEAT_W, height: ROW_H, fill: ri % 2 === 0 ? COLOR.surf : COLOR.bg }));
                svg.appendChild(svgEl('rect', { x: LABEL_W - 4, y: rowY + 5, width: 3, height: ROW_H - 10, rx: '1', fill: color }));
                const lbl = doc.label.length > 26 ? '…' + doc.label.slice(-24) : doc.label;
                svg.appendChild(svgText(LABEL_W - 8, rowY + ROW_H / 2 + 4, lbl, { 'text-anchor': 'end', fill: COLOR.ink }));

                doc.segs.forEach(([s, e]) => {
                    const bar = svgEl('rect', { x: bx(s), y: barY, width: (e - s + 1) * BEAT_W, height: BAR_H, rx: '2', fill: color, style: 'cursor:pointer' });
                    bar.addEventListener('mouseenter', ev => {
                        const range = s === e ? 'beat #' + s : 'beats ' + s + '–' + e + ' (' + (e - s + 1) + ' consecutive)';
                        showTip(ev, '<b>' + esc(doc.path) + '</b><br>tier: ' + doc.tier + '<br>' + range +
                            '<br>total active: ' + doc.total + ' / ' + D.totalBeats + ' beats (' + Math.round(doc.total / D.totalBeats * 100) + '%)');
                    });
                    bar.addEventListener('mouseleave', hideTip);
                    svg.appendChild(bar);
                });
            });

            svg.appendChild(svgEl('rect', { x: LABEL_W, y: MARG_T, width: D.totalBeats * BEAT_W, height: nRows * ROW_H, fill: 'none', stroke: COLOR.line }));
            for (let i = 0; i < D.totalBeats; i += TICK) {
                const x = bx(i) + BEAT_W / 2;
                svg.appendChild(svgEl('line', { x1: x, y1: MARG_T + nRows * ROW_H, x2: x, y2: MARG_T + nRows * ROW_H + 4, stroke: COLOR.mut }));
                svg.appendChild(svgText(x, totalH - 6, String(i), { 'text-anchor': 'middle', fill: COLOR.mut }));
            }

            const wrap = scrollWrap();
            wrap.appendChild(svg);
            root.appendChild(wrap);
        }
    }

    window.dcmVizInterop = { render };
})();
