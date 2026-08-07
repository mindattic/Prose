// ── D3 Force-Directed World Graph ────────────────────────────────────
// Renders the world graph as an interactive force layout.
// Blazor sends node/edge data via JS interop; D3 handles rendering.
//
// Controls:
//   Click node     = select (shows detail in Blazor)
//   Double-click   = expand neighbors
//   Drag           = reposition node
//   P              = pin/unpin hovered or selected node (locks position)
//   Scroll         = zoom
//   Click + drag background = pan

window.graphInterop = {
    _sim: null,
    _svg: null,
    _g: null,
    _ref: null,
    _nodes: [],
    _edges: [],
    _selectedId: null,
    _hoveredId: null,
    _nodeSelection: null,
    _containerId: null,

    typeColors: {
        character:    '#17a2b8',
        organization: '#ffc107',
        place:        '#28a745',
        faction:      '#6f42c1',
        weapon:       '#dc3545',
        equipment:    '#0d6efd',
        technology:   '#0dcaf0',
        unknown:      '#6c757d',
        event:        '#fd7e14',
        fact:         '#adb5bd',
        lore:         '#e83e8c',
    },

    init: function (containerId, dotNetRef, nodes, edges) {
        this.destroy();
        this._ref = dotNetRef;
        this._nodes = nodes;
        this._edges = edges;
        this._containerId = containerId;

        const container = document.getElementById(containerId);
        if (!container) return;
        container.innerHTML = '';

        const width = container.clientWidth || 900;
        const height = container.clientHeight || 600;

        const svg = d3.select(container)
            .append('svg')
            .attr('width', '100%')
            .attr('height', '100%')
            .attr('viewBox', [0, 0, width, height])
            .attr('style', 'cursor: grab; outline: none;')
            .attr('tabindex', '0'); // focusable for keyboard events

        // Zoom
        const g = svg.append('g');
        svg.call(d3.zoom()
            .scaleExtent([0.05, 5])
            .on('zoom', e => g.attr('transform', e.transform)));

        this._svg = svg;
        this._g = g;

        // Arrow markers
        const defs = svg.append('defs');
        defs.append('marker')
            .attr('id', 'arrowhead')
            .attr('viewBox', '0 -5 10 10')
            .attr('refX', 18).attr('refY', 0)
            .attr('markerWidth', 5).attr('markerHeight', 5)
            .attr('orient', 'auto')
            .append('path').attr('d', 'M0,-5L10,0L0,5').attr('fill', '#555');

        // Pin icon marker (small square inside a circle)
        defs.append('marker')
            .attr('id', 'pin-icon')
            .attr('viewBox', '0 0 10 10')
            .attr('refX', 5).attr('refY', 5)
            .attr('markerWidth', 8).attr('markerHeight', 8)
            .append('circle')
            .attr('cx', 5).attr('cy', 5).attr('r', 4)
            .attr('fill', '#dc3545').attr('opacity', 0.8);

        // Simulation
        const sim = d3.forceSimulation(nodes)
            .force('link', d3.forceLink(edges).id(d => d.id).distance(100))
            .force('charge', d3.forceManyBody().strength(-250))
            .force('center', d3.forceCenter(width / 2, height / 2))
            .force('collide', d3.forceCollide().radius(d => this._radius(d) + 8));

        this._sim = sim;
        this._render(g, sim, nodes, edges, dotNetRef);

        // ── Keyboard: P to pin/unpin ──
        this._keyHandler = (e) => {
            if (e.key === 'p' || e.key === 'P') {
                const targetId = this._hoveredId || this._selectedId;
                if (targetId) this._togglePin(targetId);
            }
        };
        svg.node().addEventListener('keydown', this._keyHandler);
        // Focus the SVG so keyboard events work
        svg.node().focus();
    },

    _radius: function (d) {
        return Math.min(6 + (d.edgeCount || 0) * 0.8, 28);
    },

    _togglePin: function (nodeId) {
        const d = this._nodes.find(n => n.id === nodeId);
        if (!d) return;

        if (d._pinned) {
            // Unpin
            d._pinned = false;
            d.fx = null;
            d.fy = null;
        } else {
            // Pin at current position
            d._pinned = true;
            d.fx = d.x;
            d.fy = d.y;
        }

        // Update visual
        this._updatePinVisuals();

        // Reheat slightly so unpinned nodes settle
        if (this._sim) this._sim.alpha(0.3).restart();
    },

    _updatePinVisuals: function () {
        if (!this._nodeSelection) return;
        const self = this;

        // Pin indicator: pushpin emoji overlapping top-center of the square node
        // Positioned so the pin tip appears to pierce through the node
        this._nodeSelection.selectAll('.pin-indicator').remove();
        this._nodeSelection.filter(d => d._pinned)
            .append('text')
            .attr('class', 'pin-indicator')
            .attr('x', d => self._radius(d) * 0.4)
            .attr('y', d => -self._radius(d) + 4)
            .attr('text-anchor', 'middle')
            .attr('font-size', 14)
            .text('\uD83D\uDCCC');

        // Pinned nodes get a dashed stroke on the rect
        this._nodeSelection.selectAll('rect')
            .attr('stroke-dasharray', d => d._pinned ? '3,2' : 'none')
            .attr('stroke', d => {
                if (d.id === self._selectedId) return '#fff';
                if (d._pinned) return '#dc3545';
                return '#222';
            });
    },

    _render: function (g, sim, nodes, edges, dotNetRef) {
        const self = this;

        // Edges
        const link = g.append('g')
            .selectAll('line')
            .data(edges)
            .enter().append('line')
            .attr('stroke', d => {
                if (d.sentiment === 'negative') return '#dc3545';
                if (d.sentiment === 'positive') return '#28a745';
                return '#555';
            })
            .attr('stroke-opacity', 0.5)
            .attr('stroke-width', d => Math.max(1, (d.weight || 1) * 0.6))
            .attr('marker-end', 'url(#arrowhead)');

        // Edge labels
        const edgeLabel = g.append('g')
            .selectAll('text')
            .data(edges)
            .enter().append('text')
            .attr('font-size', 8)
            .attr('fill', '#777')
            .attr('text-anchor', 'middle')
            .attr('opacity', 0.0)
            .text(d => d.relationType || '');

        // Node groups
        const node = g.append('g')
            .selectAll('g')
            .data(nodes)
            .enter().append('g')
            .attr('style', 'cursor: pointer;')
            .call(d3.drag()
                .on('start', (e, d) => {
                    if (!e.active) sim.alphaTarget(0.3).restart();
                    d.fx = d.x;
                    d.fy = d.y;
                })
                .on('drag', (e, d) => {
                    d.fx = e.x;
                    d.fy = e.y;
                })
                .on('end', (e, d) => {
                    if (!e.active) sim.alphaTarget(0);
                    // Only release if not pinned
                    if (!d._pinned) {
                        d.fx = null;
                        d.fy = null;
                    }
                }));

        this._nodeSelection = node;

        // Node squares (rounded rect)
        node.append('rect')
            .attr('width', d => self._radius(d) * 2)
            .attr('height', d => self._radius(d) * 2)
            .attr('x', d => -self._radius(d))
            .attr('y', d => -self._radius(d))
            .attr('rx', 3).attr('ry', 3)
            .attr('fill', d => self.typeColors[d.nodeType] || '#6c757d')
            .attr('stroke', '#222')
            .attr('stroke-width', 1.5);

        // Node labels
        node.append('text')
            .attr('dx', d => self._radius(d) + 4)
            .attr('dy', 4)
            .attr('font-size', 11)
            .attr('font-family', 'system-ui, sans-serif')
            .attr('fill', '#ddd')
            .text(d => d.name);

        // Click = select
        node.on('click', (e, d) => {
            e.stopPropagation();
            self._selectedId = d.id;
            self._updatePinVisuals();
            dotNetRef.invokeMethodAsync('OnNodeClicked', d.id);
        });

        // Double-click = expand neighbors
        node.on('dblclick', (e, d) => {
            e.stopPropagation();
            dotNetRef.invokeMethodAsync('OnNodeExpand', d.id);
        });

        // Hover tracking (for P key targeting)
        node.on('mouseenter', (e, d) => {
            self._hoveredId = d.id;
            edgeLabel.attr('opacity', el =>
                el.source.id === d.id || el.target.id === d.id ? 0.9 : 0.0);
            link.attr('stroke-opacity', el =>
                el.source.id === d.id || el.target.id === d.id ? 0.9 : 0.15);
        });
        node.on('mouseleave', (e, d) => {
            if (self._hoveredId === d.id) self._hoveredId = null;
            edgeLabel.attr('opacity', 0.0);
            link.attr('stroke-opacity', 0.5);
        });

        // Click background = deselect
        this._svg.on('click', () => {
            self._selectedId = null;
            self._updatePinVisuals();
            dotNetRef.invokeMethodAsync('OnBackgroundClicked');
        });

        // Tick
        sim.on('tick', () => {
            link.attr('x1', d => d.source.x).attr('y1', d => d.source.y)
                .attr('x2', d => d.target.x).attr('y2', d => d.target.y);
            edgeLabel.attr('x', d => (d.source.x + d.target.x) / 2)
                     .attr('y', d => (d.source.y + d.target.y) / 2);
            node.attr('transform', d => `translate(${d.x},${d.y})`);
        });

        // Restore pin visuals for any previously pinned nodes
        self._updatePinVisuals();
    },

    _highlightSelected: function (g) {
        // Handled by _updatePinVisuals now
        this._updatePinVisuals();
    },

    highlight: function (nodeId) {
        this._selectedId = nodeId;
        this._updatePinVisuals();
    },

    // Pin a node programmatically from Blazor
    pinNode: function (nodeId) {
        const d = this._nodes.find(n => n.id === nodeId);
        if (d && !d._pinned) this._togglePin(nodeId);
    },

    // Unpin a node programmatically from Blazor
    unpinNode: function (nodeId) {
        const d = this._nodes.find(n => n.id === nodeId);
        if (d && d._pinned) this._togglePin(nodeId);
    },

    // Unpin all nodes
    unpinAll: function () {
        for (const d of this._nodes) {
            d._pinned = false;
            d.fx = null;
            d.fy = null;
        }
        this._updatePinVisuals();
        if (this._sim) this._sim.alpha(0.5).restart();
    },

    // Grow the graph: add new nodes/edges without full re-init
    addData: function (newNodes, newEdges) {
        if (!this._sim || !this._g) return;

        // Merge new nodes (skip duplicates)
        const existingIds = new Set(this._nodes.map(n => n.id));
        for (const n of newNodes) {
            if (!existingIds.has(n.id)) {
                this._nodes.push(n);
                existingIds.add(n.id);
            }
        }

        // Merge new edges
        for (const e of newEdges) {
            if (!this._edges.find(x => x.source.id === e.source && x.target.id === e.target && x.relationType === e.relationType)) {
                this._edges.push(e);
            }
        }

        // Re-render with merged data
        this._g.selectAll('*').remove();
        this._sim.nodes(this._nodes);
        this._sim.force('link').links(this._edges);
        this._render(this._g, this._sim, this._nodes, this._edges, this._ref);
        this._sim.alpha(0.5).restart();
    },

    destroy: function () {
        if (this._svg && this._keyHandler) {
            try { this._svg.node().removeEventListener('keydown', this._keyHandler); } catch (e) {}
        }
        if (this._sim) this._sim.stop();
        // Clear the SVG content so the canvas visually empties
        if (this._containerId) {
            const el = document.getElementById(this._containerId);
            if (el) el.innerHTML = '';
        }
        this._sim = null;
        this._svg = null;
        this._g = null;
        this._ref = null;
        this._nodes = [];
        this._edges = [];
        this._selectedId = null;
        this._hoveredId = null;
        this._nodeSelection = null;
        this._keyHandler = null;
        this._containerId = null;
    }
};
