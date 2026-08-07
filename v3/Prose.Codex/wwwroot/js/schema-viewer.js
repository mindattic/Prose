// Schema viewer — vis-network rendering of the Prose SQL schema.
// Tables become multiline-label nodes; bridge tables become small dots.
// FK columns get arrow edges to the referenced PK. Drag-and-drop, zoom,
// and infinite-scroll canvas come for free with vis-network.
//
// Loaded as a UMD bundle from CDN — no npm install required.

window.proseSchema = (function () {
    let net = null;          // vis.Network instance
    let allNodes = null;     // vis.DataSet
    let allEdges = null;     // vis.DataSet

    const groupColors = {
        core:               { background: '#1f4e79', border: '#65aaff' },  // Entities
        entity:             { background: '#1f6240', border: '#5edcaa' },  // Characters, Weapons, …
        'child:character':  { background: '#3a4a30', border: '#b6d670' },
        'child:book':       { background: '#3f3358', border: '#a486c0' },
        bridge:             { background: '#5b3737', border: '#e89c9c' },
        event:              { background: '#3a3034', border: '#d68fa6' },
        config:             { background: '#3d3a25', border: '#c9b16a' },
        other:              { background: '#2c2f33', border: '#7a8088' },
    };

    function makeLabel(table) {
        // Bridge tables get just the name + row count (rendered as a dot).
        if (table.isBridge) {
            const rc = table.rowCount > 0 ? `\n${formatRows(table.rowCount)}` : '';
            return `${table.name}${rc}`;
        }

        // Real tables: name + row count, then column list with PK/FK markers.
        const lines = [];
        lines.push(`${table.name}  (${formatRows(table.rowCount)})`);
        lines.push('────────────────────────────');
        const max = 14;
        const cols = table.columns.slice(0, max);
        for (const c of cols) {
            const marker =
                c.isPk && c.isFk ? 'PF' :
                c.isPk           ? 'PK' :
                c.isFk           ? 'FK' :
                                   '  ';
            const ref = c.fkRefersToTable ? `  →${c.fkRefersToTable}` : '';
            lines.push(`${marker} ${c.name}  ${c.type}${ref}`);
        }
        if (table.columns.length > max) {
            lines.push(`   … ${table.columns.length - max} more cols`);
        }
        return lines.join('\n');
    }

    function formatRows(n) {
        if (n >= 1_000_000) return (n / 1_000_000).toFixed(1) + 'M rows';
        if (n >= 1_000)     return (n / 1_000).toFixed(1) + 'k rows';
        return `${n} row${n === 1 ? '' : 's'}`;
    }

    function nodeForTable(t) {
        const colors = groupColors[t.group] || groupColors.other;
        if (t.isBridge) {
            return {
                id: t.name,
                label: t.name,
                title: `${t.name}\n${formatRows(t.rowCount)}\n(bridge: ${t.columns.map(c => c.name).join(', ')})`,
                shape: 'dot',
                size: Math.min(40, 8 + Math.log10(Math.max(t.rowCount, 1)) * 4),
                color: colors,
                font: { color: '#ddd', size: 11 },
                group: t.group,
            };
        }
        return {
            id: t.name,
            label: makeLabel(t),
            shape: 'box',
            color: colors,
            font: { color: '#ddd', size: 11, face: 'monospace', multi: false, align: 'left' },
            margin: 8,
            widthConstraint: { minimum: 200, maximum: 360 },
            group: t.group,
            heightConstraint: { valign: 'top' },
        };
    }

    function edgeForFk(fk) {
        return {
            from: fk.fromTable,
            to:   fk.toTable,
            arrows: { to: { enabled: true, scaleFactor: 0.7 } },
            color: { color: 'rgba(140,180,210,.45)', highlight: '#9cf' },
            smooth: { type: 'cubicBezier', forceDirection: 'horizontal', roundness: 0.4 },
            title: `${fk.fromTable}.${fk.fromColumn} → ${fk.toTable}.${fk.toColumn}` +
                   (fk.onDelete && fk.onDelete !== 'NO_ACTION' ? `\nON DELETE ${fk.onDelete}` : ''),
        };
    }

    return {
        async init(elementId, graphJson) {
            const graph = JSON.parse(graphJson);
            const container = document.getElementById(elementId);
            if (!container) { console.error('schema viewer: container not found:', elementId); return; }

            allNodes = new vis.DataSet(graph.tables.map(nodeForTable));
            allEdges = new vis.DataSet(graph.edges.map(edgeForFk));

            const options = {
                physics: {
                    enabled: true,
                    solver: 'forceAtlas2Based',
                    forceAtlas2Based: {
                        gravitationalConstant: -120,
                        centralGravity: 0.005,
                        springLength: 220,
                        springConstant: 0.05,
                        avoidOverlap: 1,
                    },
                    stabilization: { iterations: 200, fit: true },
                },
                interaction: {
                    dragNodes: true,
                    dragView: true,
                    zoomView: true,
                    hover: true,
                    multiselect: true,
                    tooltipDelay: 100,
                    navigationButtons: true,
                    keyboard: { enabled: true, bindToWindow: false },
                },
                edges: {
                    chosen: false,
                    selectionWidth: 2,
                    width: 1.2,
                },
                nodes: { borderWidth: 1.4 },
            };

            net = new vis.Network(container, { nodes: allNodes, edges: allEdges }, options);

            // Once the layout settles, freeze physics so manual drags stick.
            net.once('stabilizationIterationsDone', () => {
                net.setOptions({ physics: { enabled: false } });
            });
        },

        focus(tableName) {
            if (!net) return;
            net.selectNodes([tableName]);
            net.focus(tableName, { scale: 1.0, animation: { duration: 400 } });
        },

        filter(searchText) {
            if (!net || !allNodes) return;
            const q = (searchText || '').toLowerCase();
            const matchingIds = allNodes.get()
                .filter(n => n.id.toLowerCase().includes(q))
                .map(n => n.id);
            if (!q) {
                net.unselectAll();
                net.fit({ animation: { duration: 250 } });
                return;
            }
            if (matchingIds.length === 0) return;
            net.selectNodes(matchingIds);
            // Frame the matching nodes
            net.fit({ nodes: matchingIds, animation: { duration: 400 } });
        },

        resetLayout() {
            if (!net) return;
            net.setOptions({ physics: { enabled: true } });
            setTimeout(() => net.setOptions({ physics: { enabled: false } }), 1500);
        },
    };
})();
