// Story workspace (RFC 0019): the Story Atlas constellation, the brainstorm board's dragging, and
// stepping onto a passage. Plain DOM + canvas, no library: the Hub serves everything itself.
(function () {
    'use strict';

    const CATEGORY_COLORS = {
        character: '#7cc242',
        place: '#9b5de5',
        item: '#2bb3a3',
        organization: '#e5484d',
        concept: '#3e7bfa',
    };

    // ── Atlas ────────────────────────────────────────────────────────────────────────────────
    const atlases = new WeakMap();

    function layout(stars, threads, w, h) {
        // Deterministic force layout: seeded positions (by key hash) so the same book draws the
        // same map every time, and scrubbing never reshuffles the stars.
        const pos = new Map();
        const hash = s => { let x = 2166136261; for (let i = 0; i < s.length; i++) { x ^= s.charCodeAt(i); x = Math.imul(x, 16777619); } return (x >>> 0) / 4294967295; };
        stars.forEach(s => {
            const a = hash(s.key) * Math.PI * 2, r = 0.15 + 0.35 * hash(s.key + '#');
            pos.set(s.key, { x: w / 2 + Math.cos(a) * r * w, y: h / 2 + Math.sin(a) * r * h, vx: 0, vy: 0 });
        });
        const links = threads.filter(t => pos.has(t.from) && pos.has(t.to));
        const n = stars.length, k = Math.sqrt((w * h) / Math.max(1, n)) * 0.75;
        for (let iter = 0; iter < 320; iter++) {
            const temp = 1 - iter / 320;
            for (let i = 0; i < n; i++) {
                const a = pos.get(stars[i].key);
                for (let j = i + 1; j < n; j++) {
                    const b = pos.get(stars[j].key);
                    let dx = a.x - b.x, dy = a.y - b.y, d2 = dx * dx + dy * dy + 0.01;
                    const f = (k * k) / d2;
                    a.vx += dx * f * 0.02; a.vy += dy * f * 0.02; b.vx -= dx * f * 0.02; b.vy -= dy * f * 0.02;
                }
            }
            links.forEach(t => {
                const a = pos.get(t.from), b = pos.get(t.to);
                const dx = b.x - a.x, dy = b.y - a.y, d = Math.sqrt(dx * dx + dy * dy) + 0.01;
                const f = (d - k) / d * 0.04 * Math.min(3, t.weight || 1);
                a.vx += dx * f; a.vy += dy * f; b.vx -= dx * f; b.vy -= dy * f;
            });
            pos.forEach(p => {
                p.vx += (w / 2 - p.x) * 0.004; p.vy += (h / 2 - p.y) * 0.004;
                const sp = Math.sqrt(p.vx * p.vx + p.vy * p.vy), max = 18 * temp + 0.5;
                if (sp > max) { p.vx *= max / sp; p.vy *= max / sp; }
                p.x += p.vx; p.y += p.vy; p.vx *= 0.6; p.vy *= 0.6;
            });
        }
        return pos;
    }

    function draw(state) {
        const { canvas, data, pos, view, asOf, hover } = state;
        const ctx = canvas.getContext('2d');
        const dpr = window.devicePixelRatio || 1;
        const w = canvas.clientWidth, h = canvas.clientHeight;
        if (canvas.width !== Math.round(w * dpr) || canvas.height !== Math.round(h * dpr)) {
            canvas.width = Math.round(w * dpr); canvas.height = Math.round(h * dpr);
        }
        ctx.setTransform(dpr, 0, 0, dpr, 0, 0);
        const css = getComputedStyle(canvas);
        ctx.fillStyle = css.getPropertyValue('--atlas-bg') || '#0f0f10';
        ctx.fillRect(0, 0, w, h);

        // Faint background stars.
        ctx.fillStyle = 'rgba(255,255,255,0.35)';
        for (let i = 0; i < 70; i++) {
            const x = (Math.sin(i * 12.9898) * 43758.5453 % 1 + 1) % 1 * w, y = (Math.sin(i * 78.233) * 12345.678 % 1 + 1) % 1 * h;
            ctx.fillRect(x, y, 1.2, 1.2);
        }

        ctx.save();
        ctx.translate(view.x, view.y);
        ctx.scale(view.k, view.k);
        const hidden = (state.options && state.options.hidden) || [];
        const visible = s => (asOf <= 0 || s.firstChapter <= asOf) && !hidden.includes(s.category);
        const byKey = new Map(data.stars.map(s => [s.key, s]));

        ctx.lineWidth = 0.9 / view.k;
        data.threads.forEach(t => {
            const a = byKey.get(t.from), b = byKey.get(t.to);
            if (!a || !b || !visible(a) || !visible(b) || (asOf > 0 && t.firstChapter > asOf)) return;
            const pa = pos.get(t.from), pb = pos.get(t.to);
            const near = hover && (hover === t.from || hover === t.to);
            ctx.strokeStyle = near ? 'rgba(255,255,255,0.85)' : (t.type === 'appears with' ? 'rgba(255,255,255,0.18)' : 'rgba(255,255,255,0.55)');
            ctx.beginPath(); ctx.moveTo(pa.x, pa.y); ctx.lineTo(pb.x, pb.y); ctx.stroke();
            // RA-T3.5.1.1: name the relationship on the threads of the star under the pointer.
            if (near && t.type !== 'appears with') {
                ctx.fillStyle = 'rgba(242,193,78,0.95)';
                ctx.font = `${11 / view.k}px system-ui, sans-serif`;
                ctx.fillText(t.type, (pa.x + pb.x) / 2 + 3 / view.k, (pa.y + pb.y) / 2 - 3 / view.k);
            }
        });

        data.stars.forEach(s => {
            if (!visible(s)) return;
            const p = pos.get(s.key), col = CATEGORY_COLORS[s.category] || '#aaa';
            const r = (4 + Math.min(10, Math.sqrt(s.mentions) * 1.4)) * (s.key === hover ? 1.35 : 1);
            const g = ctx.createRadialGradient(p.x, p.y, 0, p.x, p.y, r * 5);
            g.addColorStop(0, col + '55'); g.addColorStop(1, col + '00');
            ctx.fillStyle = g; ctx.beginPath(); ctx.arc(p.x, p.y, r * 5, 0, Math.PI * 2); ctx.fill();
            ctx.globalAlpha = s.draft ? 0.5 : 1;
            ctx.fillStyle = col; ctx.beginPath(); ctx.arc(p.x, p.y, r, 0, Math.PI * 2); ctx.fill();
            if (s.draft) { ctx.strokeStyle = col; ctx.setLineDash([2 / view.k, 2 / view.k]); ctx.beginPath(); ctx.arc(p.x, p.y, r + 3, 0, Math.PI * 2); ctx.stroke(); ctx.setLineDash([]); }
            ctx.globalAlpha = 1;
            if (state.options && state.options.focus === s.key) {
                ctx.strokeStyle = '#f2c14e'; ctx.lineWidth = 2 / view.k;
                ctx.beginPath(); ctx.arc(p.x, p.y, r + 7 / view.k, 0, Math.PI * 2); ctx.stroke();
            }
            ctx.fillStyle = css.getPropertyValue('--atlas-label') || '#e8e8e8';
            ctx.font = `${13 / view.k}px system-ui, sans-serif`;
            ctx.fillText(s.name, p.x + r + 4 / view.k, p.y + 4 / view.k);
        });
        ctx.restore();
    }

    // Frame the whole constellation: every star on screen, with room for its label.
    function fit(state, w, h) {
        if (state.pos.size === 0) { state.view = { x: 0, y: 0, k: 1 }; return; }
        let x0 = Infinity, y0 = Infinity, x1 = -Infinity, y1 = -Infinity;
        state.pos.forEach(p => { x0 = Math.min(x0, p.x); y0 = Math.min(y0, p.y); x1 = Math.max(x1, p.x); y1 = Math.max(y1, p.y); });
        const pad = 60, labelRoom = 140;
        const k = Math.min(2.2, (w - pad * 2 - labelRoom) / Math.max(1, x1 - x0), (h - pad * 2) / Math.max(1, y1 - y0));
        state.view = { k, x: pad + (w - pad * 2 - labelRoom - (x1 - x0) * k) / 2 - x0 * k, y: pad + (h - pad * 2 - (y1 - y0) * k) / 2 - y0 * k };
    }

    // Pinned star positions, per book, in this browser only (try/catch: storage can be blocked).
    function pinKey(state) { return 'prose.atlas.' + ((state.options && state.options.bookKey) || 'book'); }
    function savePins(state) {
        try {
            const pins = JSON.parse(localStorage.getItem(pinKey(state)) || '{}');
            state.data.stars.forEach(s => { const p = state.pos.get(s.key); if (p && p.pinned) pins[s.key] = [p.x, p.y]; });
            localStorage.setItem(pinKey(state), JSON.stringify(pins));
        } catch (e) { /* a preference, never worth failing over */ }
    }
    function applyPins(state) {
        try {
            const pins = JSON.parse(localStorage.getItem(pinKey(state)) || '{}');
            Object.entries(pins).forEach(([k, xy]) => { const p = state.pos.get(k); if (p) { p.x = xy[0]; p.y = xy[1]; p.pinned = true; } });
        } catch (e) { }
    }

    function starAt(state, mx, my) {
        const x = (mx - state.view.x) / state.view.k, y = (my - state.view.y) / state.view.k;
        let best = null, bestD = 14 / state.view.k;
        state.data.stars.forEach(s => {
            if (state.asOf > 0 && s.firstChapter > state.asOf) return;
            if (state.options && (state.options.hidden || []).includes(s.category)) return;
            const p = state.pos.get(s.key), d = Math.hypot(p.x - x, p.y - y);
            if (d < bestD) { best = s; bestD = d; }
        });
        return best;
    }

    window.proseStory = {
        // options: { hidden: [category], focus: starKey|null, bookKey: string } (RA-T3.5.1)
        atlasRender(canvas, data, asOf, dotnet, options) {
            let state = atlases.get(canvas);
            const w = canvas.clientWidth || 800, h = canvas.clientHeight || 600;
            if (!state) {
                state = { canvas, data, pos: null, view: { x: 0, y: 0, k: 1 }, asOf, hover: null, dotnet, drag: null };
                atlases.set(canvas, state);
                const tip = canvas.parentElement.querySelector('.atlas-tip');
                canvas.addEventListener('mousemove', e => {
                    const r = canvas.getBoundingClientRect();
                    if (state.dragStar) {
                        // RA-T3.5.1.2: a star dragged is a star pinned.
                        const p = state.pos.get(state.dragStar);
                        p.x = (e.clientX - r.left - state.view.x) / state.view.k;
                        p.y = (e.clientY - r.top - state.view.y) / state.view.k;
                        state.starMoved = true;
                        draw(state); return;
                    }
                    if (state.drag) {
                        state.view.x = state.drag.vx + (e.clientX - state.drag.x);
                        state.view.y = state.drag.vy + (e.clientY - state.drag.y);
                        draw(state); return;
                    }
                    const s = starAt(state, e.clientX - r.left, e.clientY - r.top);
                    const key = s ? s.key : null;
                    if (key !== state.hover) { state.hover = key; draw(state); }
                    canvas.style.cursor = s ? 'pointer' : 'grab';
                    if (tip) {
                        if (s) {
                            tip.hidden = false;
                            tip.style.left = (e.clientX - r.left + 14) + 'px';
                            tip.style.top = (e.clientY - r.top + 10) + 'px';
                            tip.textContent = `${s.name} · ${s.category}${s.draft ? ' · draft' : ''} — first in chapter ${s.firstChapter}, ${s.mentions} mention${s.mentions === 1 ? '' : 's'}, ${s.degree} thread${s.degree === 1 ? '' : 's'}`;
                        } else tip.hidden = true;
                    }
                });
                canvas.addEventListener('mousedown', e => {
                    const r = canvas.getBoundingClientRect();
                    const hit = starAt(state, e.clientX - r.left, e.clientY - r.top);
                    if (hit) { state.dragStar = hit.key; state.starMoved = false; state.downAt = { x: e.clientX, y: e.clientY }; return; }
                    state.drag = { x: e.clientX, y: e.clientY, vx: state.view.x, vy: state.view.y, moved: false };
                });
                window.addEventListener('mouseup', e => {
                    if (state.dragStar) {
                        const key = state.dragStar, moved = state.starMoved && Math.hypot(e.clientX - state.downAt.x, e.clientY - state.downAt.y) > 4;
                        state.dragStar = null;
                        if (moved) { state.pos.get(key).pinned = true; savePins(state); return; }
                        const s = state.data.stars.find(x => x.key === key);
                        if (s && state.dotnet) state.dotnet.invokeMethodAsync('OpenStar', s.key, s.entityId || null);
                        return;
                    }
                    if (!state.drag) return;
                    const moved = Math.hypot(e.clientX - state.drag.x, e.clientY - state.drag.y) > 4;
                    state.drag = null;
                    if (moved || e.target !== canvas) return;
                    const r = canvas.getBoundingClientRect();
                    const s = starAt(state, e.clientX - r.left, e.clientY - r.top);
                    if (s && state.dotnet) state.dotnet.invokeMethodAsync('OpenStar', s.key, s.entityId || null);
                });
                canvas.addEventListener('wheel', e => {
                    e.preventDefault();
                    const r = canvas.getBoundingClientRect(), mx = e.clientX - r.left, my = e.clientY - r.top;
                    const f = e.deltaY < 0 ? 1.12 : 1 / 1.12;
                    state.view.x = mx - (mx - state.view.x) * f; state.view.y = my - (my - state.view.y) * f; state.view.k *= f;
                    draw(state);
                }, { passive: false });
                new ResizeObserver(() => draw(state)).observe(canvas);
            }
            const keysChanged = !state.pos || data.stars.some(s => !state.pos.has(s.key));
            const prevFocus = state.options && state.options.focus;
            state.data = data; state.asOf = asOf; state.dotnet = dotnet; state.options = options || {};
            if (keysChanged) { state.pos = layout(data.stars, data.threads, w, h); applyPins(state); fit(state, w, h); }
            if (state.options.focus && state.options.focus !== prevFocus && state.pos.has(state.options.focus)) {
                const p = state.pos.get(state.options.focus);
                state.view.x = w / 2 - p.x * state.view.k; state.view.y = h / 2 - p.y * state.view.k;
            }
            draw(state);
        },
        atlasZoom(canvas, factor) {
            const state = atlases.get(canvas); if (!state) return;
            if (factor === 0) { fit(state, canvas.clientWidth, canvas.clientHeight); }
            else {
                const w = canvas.clientWidth / 2, h = canvas.clientHeight / 2;
                state.view.x = w - (w - state.view.x) * factor; state.view.y = h - (h - state.view.y) * factor; state.view.k *= factor;
            }
            draw(state);
        },

        // ── Brainstorm board ──────────────────────────────────────────────────────────────
        boardInit(board, dotnet) {
            if (board._proseBoard) { board._proseBoard.dotnet = dotnet; return; }
            const st = { dotnet, card: null };
            board._proseBoard = st;
            board.addEventListener('pointerdown', e => {
                const handle = e.target.closest('.bs-handle');
                if (!handle) return;
                const card = handle.closest('.bs-card');
                st.card = card; st.sx = e.clientX; st.sy = e.clientY;
                st.ox = parseFloat(card.style.left) || 0; st.oy = parseFloat(card.style.top) || 0;
                card.setPointerCapture(e.pointerId); card.classList.add('dragging');
                e.preventDefault();
            });
            board.addEventListener('pointermove', e => {
                if (!st.card) return;
                st.card.style.left = Math.max(0, st.ox + e.clientX - st.sx) + 'px';
                st.card.style.top = Math.max(0, st.oy + e.clientY - st.sy) + 'px';
            });
            board.addEventListener('pointerup', () => {
                if (!st.card) return;
                const c = st.card; st.card = null; c.classList.remove('dragging');
                st.dotnet.invokeMethodAsync('MoveCard', c.dataset.id, parseFloat(c.style.left) || 0, parseFloat(c.style.top) || 0);
            });
        },

        // Import drop zone (RA-T2.4.2.4): files or whole folders dropped anywhere on the zone are read
        // (folders recursively) and handed to the files input, which Blazor's InputFile then reads.
        importDrop(zone, inputId) {
            if (!zone || zone._proseDrop) return;
            zone._proseDrop = true;
            const readEntry = entry => new Promise(resolve => {
                if (entry.isFile) entry.file(f => resolve([f]), () => resolve([]));
                else if (entry.isDirectory) {
                    const reader = entry.createReader(), all = [];
                    const next = () => reader.readEntries(async batch => {
                        if (!batch.length) { resolve((await Promise.all(all.map(readEntry))).flat()); return; }
                        all.push(...batch); next();
                    }, () => resolve([]));
                    next();
                } else resolve([]);
            });
            zone.addEventListener('dragover', e => { e.preventDefault(); zone.classList.add('over'); });
            zone.addEventListener('dragleave', () => zone.classList.remove('over'));
            zone.addEventListener('drop', async e => {
                e.preventDefault(); zone.classList.remove('over');
                const items = [...(e.dataTransfer.items || [])].map(i => i.webkitGetAsEntry && i.webkitGetAsEntry()).filter(Boolean);
                const files = items.length ? (await Promise.all(items.map(readEntry))).flat() : [...e.dataTransfer.files];
                const input = document.getElementById(inputId);
                if (!input || !files.length) return;
                const dt = new DataTransfer();
                files.forEach(f => dt.items.add(f));
                input.files = dt.files;
                input.dispatchEvent(new Event('change', { bubbles: true }));
            });
        },

        // Keep the address bar in step with the open book and tab, so a reload or a shared link
        // opens the same view. replaceState: no history entry per tab click.
        setLocation(book, tab) {
            try {
                const u = new URL(window.location.href);
                if (book) u.searchParams.set('book', book); else u.searchParams.delete('book');
                if (tab) u.searchParams.set('tab', tab); else u.searchParams.delete('tab');
                history.replaceState(null, '', u.pathname + u.search);
            } catch (e) { /* cosmetic */ }
        },

        // ── Stepping onto a passage ───────────────────────────────────────────────────────
        scrollToBeat(beatId) {
            const el = document.getElementById('beat-' + beatId);
            if (!el) return false;
            el.scrollIntoView({ block: 'center', behavior: 'smooth' });
            el.classList.add('flash');
            setTimeout(() => el.classList.remove('flash'), 2200);
            return true;
        },
    };
})();
