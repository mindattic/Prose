// ── 3D Force-Directed World Graph (WebGL / three.js) ──────────────────
// Hardware-accelerated alternative to graph-interop.js for large graphs.
// Uses 3d-force-graph (vasturiano) which bundles three.js + d3-force-3d.
//
// Same Blazor↔JS contract as graphInterop: init / addData / highlight /
// unpinAll / destroy. Same JSInvokable callbacks: OnNodeClicked,
// OnNodeExpand, OnBackgroundClicked.
//
// Controls:
//   Left-click node    = select (shows detail in Blazor)
//   Right-click node   = expand neighbors (dblclick is reserved for
//                        the camera-focus gesture by the library)
//   Left-drag canvas   = orbit
//   Right-drag canvas  = pan
//   Scroll             = zoom
//   Drag node          = reposition (auto-pinned while dragging)

window.graph3dInterop = {
    graph: null,
    ref: null,
    nodes: [],
    edges: [],
    selectedId: null,
    containerId: null,
    resizeHandler: null,
    nodeColorFn: null,

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
        this.ref = dotNetRef;
        this.containerId = containerId;
        // Clone — 3d-force-graph mutates objects (adds x/y/z, replaces source/target with refs).
        this.nodes = nodes.map(n => ({ ...n }));
        this.edges = edges.map(e => ({ ...e }));

        const container = document.getElementById(containerId);
        if (!container) return;
        container.innerHTML = '';

        if (typeof ForceGraph3D !== 'function') {
            container.innerHTML = '<div style="color:#dc3545; padding:1rem; font-family: monospace;">3d-force-graph library not loaded. Check &lt;script&gt; tag in App.razor.</div>';
            return;
        }

        const self = this;
        const width = container.clientWidth || 900;
        const height = container.clientHeight || 600;

        self.nodeColorFn = (n) => {
            if (n.id === self.selectedId) return '#ffffff';
            return self.typeColors[n.nodeType] || '#6c757d';
        };

        const graph = ForceGraph3D()(container)
            .width(width)
            .height(height)
            .backgroundColor('#0a0a0a')
            .showNavInfo(false)
            .graphData({ nodes: self.nodes, links: self.edges })
            .nodeId('id')
            .nodeLabel(n => `<div style="background:#111;padding:4px 8px;border:1px solid #333;border-radius:3px;color:#ddd;font-family:system-ui;font-size:12px;"><strong>${n.name}</strong> <span style="color:#888;">(${n.nodeType})</span></div>`)
            .nodeColor(self.nodeColorFn)
            .nodeVal(n => Math.max(1, (n.edgeCount || 0) * 0.5 + 2))
            .nodeOpacity(0.92)
            .nodeResolution(8)
            .linkSource('source')
            .linkTarget('target')
            .linkColor(e => {
                if (e.sentiment === 'negative') return '#dc3545';
                if (e.sentiment === 'positive') return '#28a745';
                return '#888888';
            })
            .linkOpacity(0.4)
            .linkWidth(e => Math.max(0.4, (e.weight || 1) * 0.4))
            .linkDirectionalArrowLength(3)
            .linkDirectionalArrowRelPos(0.95)
            .onNodeClick(n => {
                self.selectedId = n.id;
                graph.nodeColor(self.nodeColorFn); // re-evaluate accessor
                if (self.ref) self.ref.invokeMethodAsync('OnNodeClicked', n.id);

                // Smooth camera focus on the clicked node
                const distance = 100;
                const dist = Math.hypot(n.x || 1, n.y || 1, n.z || 1);
                const distRatio = 1 + distance / dist;
                graph.cameraPosition(
                    { x: (n.x || 0) * distRatio, y: (n.y || 0) * distRatio, z: (n.z || 0) * distRatio },
                    { x: n.x || 0, y: n.y || 0, z: n.z || 0 },
                    800
                );
            })
            .onNodeRightClick(n => {
                if (self.ref) self.ref.invokeMethodAsync('OnNodeExpand', n.id);
            })
            .onBackgroundClick(() => {
                self.selectedId = null;
                graph.nodeColor(self.nodeColorFn);
                if (self.ref) self.ref.invokeMethodAsync('OnBackgroundClicked');
            });

        // Tune the simulation forces a bit for readability with mid-sized graphs
        graph.d3Force('charge').strength(-90);
        graph.d3Force('link').distance(40);

        self.graph = graph;

        self.resizeHandler = () => {
            if (!self.graph || !container) return;
            self.graph.width(container.clientWidth);
            self.graph.height(container.clientHeight);
        };
        window.addEventListener('resize', self.resizeHandler);
    },

    highlight: function (nodeId) {
        this.selectedId = nodeId;
        if (this.graph && this.nodeColorFn) this.graph.nodeColor(this.nodeColorFn);
    },

    unpinAll: function () {
        if (!this.graph) return;
        for (const n of this.nodes) { n.fx = null; n.fy = null; n.fz = null; }
        this.graph.d3ReheatSimulation();
    },

    addData: function (newNodes, newEdges) {
        if (!this.graph) return;
        const existingIds = new Set(this.nodes.map(n => n.id));
        for (const n of newNodes) {
            if (!existingIds.has(n.id)) {
                this.nodes.push({ ...n });
                existingIds.add(n.id);
            }
        }
        for (const e of newEdges) {
            const dup = this.edges.find(x => {
                const sId = (x.source && x.source.id) || x.source;
                const tId = (x.target && x.target.id) || x.target;
                return sId === e.source && tId === e.target && x.relationType === e.relationType;
            });
            if (!dup) this.edges.push({ ...e });
        }
        this.graph.graphData({ nodes: this.nodes, links: this.edges });
    },

    destroy: function () {
        if (this.resizeHandler) {
            window.removeEventListener('resize', this.resizeHandler);
            this.resizeHandler = null;
        }
        if (this.graph) {
            try { this.graph._destructor && this.graph._destructor(); } catch (e) {}
        }
        if (this.containerId) {
            const el = document.getElementById(this.containerId);
            if (el) el.innerHTML = '';
        }
        this.graph = null;
        this.ref = null;
        this.nodes = [];
        this.edges = [];
        this.selectedId = null;
        this.containerId = null;
        this.nodeColorFn = null;
    }
};
