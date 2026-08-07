// ── Story Shape Visualizer ────────────────────────────────────────────
// Vonnegut "Shape of Stories" — Fortune/Tension vs Time, D3 v7
// Archetypes: Cinderella, Man in a Hole, Greek Tragedy, Freytag's Pyramid, Hero's Journey

window.storyShape = {

    ARCHETYPES: {
        'cinderella': {
            label: 'Cinderella',
            color: '#7c6ae8',
            desc: 'Starting in hardship, a stroke of luck brings joy — but it\'s snatched away. Earned transformation leads to permanent rise.',
            points: [
                [0.00, -0.25], [0.12, -0.38], [0.25, -0.42], [0.38, 0.05],
                [0.52, 0.35],  [0.65, -0.55], [0.75, -0.42], [0.87, 0.58], [1.00, 0.62]
            ]
        },
        'man-in-hole': {
            label: 'Man in a Hole',
            color: '#3fb950',
            desc: 'Character starts okay, falls into trouble, climbs back out — better for the experience. The simplest satisfying shape.',
            points: [
                [0.00, 0.10], [0.18, -0.10], [0.35, -0.32], [0.52, -0.55],
                [0.65, -0.48], [0.80, -0.05], [0.90, 0.18], [1.00, 0.32]
            ]
        },
        'greek-tragedy': {
            label: 'Greek tragedy',
            color: '#e05a1f',
            desc: 'Aristotle\'s model: the hero begins high, is undone by a fatal flaw (hamartia), and falls to ruin. The audience feels catharsis — pity and fear — precisely because the peak was so high.',
            points: [
                [0.00, 0.00], [0.15, 0.22], [0.35, 0.42], [0.50, 0.58],
                [0.65, 0.42], [0.75, -0.05], [0.85, -0.48], [0.93, -0.75], [1.00, -0.72]
            ]
        },
        'freytag': {
            label: "Freytag's Pyramid",
            color: '#e6c44a',
            desc: 'Gustav Freytag (1863): rising action builds to a climax, then falling action descends through resolution to catastrophe or denouement.',
            points: [
                [0.00, -0.05], [0.15, 0.12], [0.28, 0.35], [0.45, 0.62],
                [0.55, 0.58],  [0.68, 0.30], [0.78, -0.08], [0.88, -0.42], [1.00, -0.60]
            ]
        },
        'hero-journey': {
            label: "Hero's Journey",
            color: '#c8922a',
            desc: "Joseph Campbell's monomyth (1949), popularized by Star Wars. The hero leaves home, descends into a dangerous unknown, faces a death-like ordeal, and returns transformed. The dip at the midpoint is intentional — the lowest point creates the most powerful rise.",
            points: [
                [0.00, 0.00], [0.10, -0.05], [0.20, -0.15], [0.30, -0.22],
                [0.42, -0.32], [0.52, -0.62], [0.62, -0.28], [0.72, -0.05],
                [0.82, 0.22],  [0.90, 0.42],  [1.00, 0.55]
            ]
        }
    },

    KEY_ROLES: {
        'Catalyst':         { color: '#3fb950', r: 5.5, label: 'C' },
        'Midpoint':         { color: '#e6c44a', r: 5.5, label: 'M' },
        'All Is Lost':      { color: '#dc3545', r: 6.0, label: '✕' },
        'Break Into Two':   { color: '#58a6ff', r: 5.0, label: '→' },
        'Break Into Three': { color: '#bc8cff', r: 5.0, label: '↑' },
        'Finale':           { color: '#f0883e', r: 5.5, label: '★' },
        'Dark Night':       { color: '#6e40c9', r: 5.0, label: '●' },
        'Opening Image':    { color: '#8b949e', r: 4.5, label: '○' },
        'Final Image':      { color: '#8b949e', r: 4.5, label: '◎' }
    },

    _instances: {},

    render: function (containerId, beats, archetypeKey, dotnetRef, compact) {
        this.destroy(containerId);

        const container = document.getElementById(containerId);
        if (!container) return;

        const archetype = this.ARCHETYPES[archetypeKey] || this.ARCHETYPES['cinderella'];
        beats = beats || [];

        const margin  = compact
            ? { top: 14, right: 14, bottom: 22, left: 40 }
            : { top: 22, right: 22, bottom: 38, left: 56 };
        const totalW  = Math.max(container.clientWidth || 640, 300);
        const totalH  = compact ? 100 : 248;
        const W = totalW - margin.left - margin.right;
        const H = totalH - margin.top  - margin.bottom;

        // ── SVG ────────────────────────────────────────────────────────
        const svg = d3.select(container)
            .append('svg')
            .attr('width', totalW)
            .attr('height', totalH)
            .style('display', 'block');

        const g = svg.append('g')
            .attr('transform', `translate(${margin.left},${margin.top})`);

        // ── Scales ─────────────────────────────────────────────────────
        const xDomain = Math.max(beats.length - 1, 1);
        const xScale  = d3.scaleLinear().domain([0, xDomain]).range([0, W]);
        const yScale  = d3.scaleLinear().domain([-1, 1]).range([H, 0]);

        // ── Grid lines ─────────────────────────────────────────────────
        g.append('line')
            .attr('x1', 0).attr('x2', W)
            .attr('y1', yScale(0)).attr('y2', yScale(0))
            .attr('stroke', '#30363d').attr('stroke-width', 1)
            .attr('stroke-dasharray', '3,3');

        [-1, 1].forEach(v => {
            g.append('line')
                .attr('x1', 0).attr('x2', W)
                .attr('y1', yScale(v)).attr('y2', yScale(v))
                .attr('stroke', '#1c2128').attr('stroke-width', 0.5);
        });

        // ── Y axis labels ──────────────────────────────────────────────
        if (!compact) {
            [['high', 1], ['neutral', 0], ['low', -1]].forEach(([lbl, v]) => {
                g.append('text')
                    .attr('x', -8).attr('y', yScale(v) + 4)
                    .attr('text-anchor', 'end')
                    .attr('font-size', '10px').attr('fill', '#6e7681')
                    .attr('font-family', 'Outfit, sans-serif')
                    .text(lbl);
            });
            g.append('text')
                .attr('transform', `translate(-42,${H / 2}) rotate(-90)`)
                .attr('text-anchor', 'middle')
                .attr('font-size', '10px').attr('fill', '#6e7681')
                .attr('font-family', 'Outfit, sans-serif')
                .text('Fortune / tension');
        } else {
            // Compact: tiny labels only
            [['↑', 1], ['—', 0], ['↓', -1]].forEach(([lbl, v]) => {
                g.append('text')
                    .attr('x', -5).attr('y', yScale(v) + 3)
                    .attr('text-anchor', 'end')
                    .attr('font-size', '9px').attr('fill', '#484f58')
                    .attr('font-family', 'Outfit, sans-serif')
                    .text(lbl);
            });
        }

        // ── Act dividers ───────────────────────────────────────────────
        if (beats.length > 1) {
            for (let i = 1; i < beats.length; i++) {
                if (beats[i].act !== beats[i - 1].act) {
                    const dx = xScale(i - 0.5);
                    g.append('line')
                        .attr('x1', dx).attr('x2', dx)
                        .attr('y1', 0).attr('y2', H)
                        .attr('stroke', '#30363d').attr('stroke-width', 1)
                        .attr('stroke-dasharray', '4,3');
                    g.append('text')
                        .attr('x', dx + 4).attr('y', H + 26)
                        .attr('font-size', '9px').attr('fill', '#6e7681')
                        .attr('font-family', 'Outfit, sans-serif')
                        .text(`Act ${beats[i].act}`);
                }
            }
            // Act 1 label at left
            g.append('text')
                .attr('x', 2).attr('y', H + 26)
                .attr('font-size', '9px').attr('fill', '#6e7681')
                .attr('font-family', 'Outfit, sans-serif')
                .text('Act 1');
        }

        // ── X axis label ───────────────────────────────────────────────
        if (!compact) {
            g.append('text')
                .attr('x', W / 2).attr('y', H + 34)
                .attr('text-anchor', 'middle')
                .attr('font-size', '10px').attr('fill', '#6e7681')
                .attr('font-family', 'Outfit, sans-serif')
                .text('Story progression →');
        }

        // ── Archetype curve (dashed reference) ─────────────────────────
        const arcData = archetype.points.map(([xn, yn]) => ({
            px: xn * W,
            py: yScale(yn)
        }));

        const arcLine = d3.line()
            .x(d => d.px).y(d => d.py)
            .curve(d3.curveCatmullRom.alpha(0.5));

        g.append('path')
            .datum(arcData)
            .attr('fill', 'none')
            .attr('stroke', archetype.color)
            .attr('stroke-width', 1.5)
            .attr('stroke-dasharray', '5,3')
            .attr('opacity', 0.45)
            .attr('d', arcLine);

        // ── Story curve ────────────────────────────────────────────────
        if (beats.length >= 2) {
            const storyData = beats.map((b, i) => ({
                px: xScale(i),
                py: yScale((b.tension - 5.5) / 4.5)
            }));

            const storyLine = d3.line()
                .x(d => d.px).y(d => d.py)
                .curve(d3.curveCatmullRom.alpha(0.5));

            g.append('path')
                .datum(storyData)
                .attr('fill', 'none')
                .attr('stroke', '#e6edf3')
                .attr('stroke-width', 2)
                .attr('d', storyLine);
        }

        // ── Tooltip ────────────────────────────────────────────────────
        const tooltip = d3.select(container)
            .append('div')
            .style('position', 'absolute')
            .style('pointer-events', 'none')
            .style('background', '#161b22')
            .style('border', '1px solid #30363d')
            .style('border-radius', '6px')
            .style('padding', '8px 12px')
            .style('font-family', 'Outfit, sans-serif')
            .style('font-size', '12px')
            .style('color', '#e6edf3')
            .style('max-width', '240px')
            .style('line-height', '1.5')
            .style('z-index', '10')
            .style('display', 'none');

        // ── Beat dots (full mode only) ─────────────────────────────────
        if (!compact && beats.length > 0) {
            const self = this;

            beats.forEach((beat, i) => {
                const cx = xScale(i);
                const cy = yScale((beat.tension - 5.5) / 4.5);
                const keyRole = self.KEY_ROLES[beat.role];
                const r     = keyRole ? keyRole.r : 4;
                const color = keyRole ? keyRole.color
                                      : (beat.sceneType === 'sequel' ? '#58a6ff' : '#f0883e');

                const circle = g.append('circle')
                    .attr('cx', cx).attr('cy', cy).attr('r', r)
                    .attr('fill', color)
                    .attr('stroke', '#0d1117').attr('stroke-width', 1.5)
                    .style('cursor', 'pointer')
                    .style('transition', 'r 0.1s');

                circle.on('mouseover', function (event) {
                    d3.select(this).attr('r', r + 2);
                    const typeLabel = beat.sceneType === 'sequel'
                        ? '<span style="color:#58a6ff">sequel</span>'
                        : '<span style="color:#f0883e">scene</span>';
                    tooltip
                        .style('display', 'block')
                        .html(
                            `<div style="color:${color};font-weight:600;margin-bottom:2px;">${beat.title}</div>` +
                            (beat.role
                                ? `<div style="color:#8b949e;font-size:10px;text-transform:uppercase;letter-spacing:0.5px;margin-bottom:4px;">${beat.role}</div>`
                                : '') +
                            `<div>Tension <strong style="color:#f0883e;">${beat.tension}/10</strong> · ${typeLabel}</div>` +
                            (beat.goal
                                ? `<div style="color:#8b949e;margin-top:4px;font-size:11px;">${beat.goal}</div>`
                                : '')
                        );
                    let ttx = cx + margin.left + 12;
                    const tty = Math.max(0, cy + margin.top - 48);
                    if (ttx + 248 > totalW) ttx = cx + margin.left - 256;
                    tooltip.style('left', Math.max(0, ttx) + 'px').style('top', tty + 'px');
                });

                circle.on('mouseout', function () {
                    d3.select(this).attr('r', r);
                    tooltip.style('display', 'none');
                });

                circle.on('click', function () {
                    if (dotnetRef) dotnetRef.invokeMethodAsync('SelectBeat', i);
                });
            });
        }

        // ── Archetype label (bottom right) ─────────────────────────────
        g.append('text')
            .attr('x', W).attr('y', H + 26)
            .attr('text-anchor', 'end')
            .attr('font-size', '9px').attr('fill', archetype.color)
            .attr('opacity', 0.7)
            .attr('font-family', 'Outfit, sans-serif')
            .text('— ' + archetype.label);

        // ── Playhead line (hidden until trackScroll fires) ─────────────
        const playhead = g.append('line')
            .attr('x1', 0).attr('x2', 0)
            .attr('y1', 0).attr('y2', H)
            .attr('stroke', '#dc3545')
            .attr('stroke-width', 1.5)
            .attr('stroke-dasharray', '3,2')
            .attr('opacity', 0);

        const playheadDot = g.append('circle')
            .attr('cy', 0).attr('r', 4)
            .attr('fill', '#dc3545')
            .attr('opacity', 0);

        const playheadLabel = g.append('text')
            .attr('y', -6)
            .attr('font-size', '9px')
            .attr('fill', '#dc3545')
            .attr('font-family', 'Outfit, sans-serif')
            .attr('opacity', 0);

        this._instances[containerId] = {
            dotnetRef, beats, W, H, xScale, yScale, margin,
            playhead, playheadDot, playheadLabel
        };
    },

    setPlayhead: function (containerId, progress) {
        const inst = this._instances[containerId];
        if (!inst) return;

        const { beats, W, H, xScale, yScale, playhead, playheadDot, playheadLabel } = inst;
        const px = Math.max(0, Math.min(W, progress * W));

        playhead.attr('x1', px).attr('x2', px).attr('opacity', 0.85);

        // Find closest beat for label + dot y position
        if (beats && beats.length > 0) {
            const beatIdx  = Math.min(beats.length - 1, Math.round(progress * (beats.length - 1)));
            const beat     = beats[beatIdx];
            const dotY     = yScale((beat.tension - 5.5) / 4.5);
            const anchor   = px > W * 0.72 ? 'end' : 'start';
            const labelX   = anchor === 'end' ? px - 5 : px + 5;

            playheadDot
                .attr('cx', px).attr('cy', dotY)
                .attr('opacity', 1);

            playheadLabel
                .attr('x', labelX)
                .attr('text-anchor', anchor)
                .attr('opacity', 0.9)
                .text(beat.title || '');
        } else {
            playheadDot.attr('cx', px).attr('cy', H / 2).attr('opacity', 0.6);
            playheadLabel.attr('opacity', 0);
        }
    },

    trackScroll: function (scrollContainerId, chartId) {
        const self = this;
        const container = document.getElementById(scrollContainerId);
        if (!container) return;

        const handler = function () {
            const max = container.scrollHeight - container.clientHeight;
            const progress = max > 0 ? container.scrollTop / max : 0;
            self.setPlayhead(chartId, progress);
        };

        container.addEventListener('scroll', handler, { passive: true });

        const inst = self._instances[chartId];
        if (inst) {
            inst.scrollEl      = container;
            inst.scrollHandler = handler;
        }
    },

    update: function (containerId, beats, archetypeKey, dotnetRef, compact) {
        this.render(containerId, beats, archetypeKey, dotnetRef, compact);
    },

    destroy: function (containerId) {
        const inst = this._instances[containerId];
        if (inst) {
            if (inst.scrollEl && inst.scrollHandler) {
                inst.scrollEl.removeEventListener('scroll', inst.scrollHandler);
            }
            if (inst.dotnetRef) {
                try { inst.dotnetRef.dispose(); } catch (_) {}
            }
        }
        const container = document.getElementById(containerId);
        if (container) {
            while (container.firstChild) container.removeChild(container.firstChild);
        }
        delete this._instances[containerId];
    }
};
