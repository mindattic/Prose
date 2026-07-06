// ── 3D Force-Directed World Graph (WebGL / three.js) ──────────────────
// Hardware-accelerated alternative to graph-interop.js for large graphs.
// Uses 3d-force-graph (vasturiano) which bundles three.js + d3-force-3d.
//
// Same Blazor↔JS contract as graphInterop: init / addData / highlight /
// unpinAll / destroy. Same JSInvokable callbacks: OnNodeClicked,
// OnNodeExpand, OnBackgroundClicked.
//
// Style alignment with the 2D graph (graph-interop.js):
//   • Cubes (3D) ↔ rounded squares (2D) — same typeColors palette
//   • Edges drawn undirected (no cone arrows) — graph relationships
//     are treated as bidirectional in the visual layer
//   • No transient hover tooltip — clicking a cube opens a sticky popover
//     anchored above it. Click the popover entry to load the entity into
//     the side panel and dismiss the popover. Click the canvas to dismiss.
//
// Controls:
//   Left-click node    = open popover (loads side panel via popover entry)
//   Right-click node   = expand neighbors
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
    popover: null,
    popoverNode: null,
    makeCube: null,

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
        // Popover overlay needs absolute positioning relative to the canvas wrapper.
        if (getComputedStyle(container).position === 'static') container.style.position = 'relative';

        if (typeof ForceGraph3D !== 'function') {
            container.innerHTML = '<div style="color:#dc3545; padding:1rem; font-family: monospace;">3d-force-graph library not loaded. Check &lt;script&gt; tag in App.razor.</div>';
            return;
        }
        if (typeof THREE === 'undefined') {
            container.innerHTML = '<div style="color:#dc3545; padding:1rem; font-family: monospace;">THREE.js not loaded. Check &lt;script&gt; tag in App.razor.</div>';
            return;
        }

        const self = this;
        const width = container.clientWidth || 900;
        const height = container.clientHeight || 600;

        // Cubes instead of spheres — each node is a BoxGeometry sized by edge count.
        // Side ≈ 2·∛(val) keeps small nodes legible and scales sublinearly.
        // The closure reads self.selectedId so re-applying nodeThreeObject re-tints
        // the selected cube white without rebuilding the simulation.
        self.makeCube = (n) => {
            const val = Math.max(1, (n.edgeCount || 0) * 0.5 + 2);
            const side = Math.max(3, 2 * Math.cbrt(val) * 1.6);
            const isSelected = (n.id === self.selectedId);
            const color = isSelected ? '#ffffff' : (self.typeColors[n.nodeType] || '#6c757d');
            const mat = new THREE.MeshLambertMaterial({ color, transparent: true, opacity: 0.92 });
            return new THREE.Mesh(new THREE.BoxGeometry(side, side, side), mat);
        };

        const graph = ForceGraph3D()(container)
            .width(width)
            .height(height)
            .backgroundColor('#0a0a0a')
            .showNavInfo(false)
            .graphData({ nodes: self.nodes, links: self.edges })
            .nodeId('id')
            // No hover tooltip — replaced by the click-anchored popover (see showPopover).
            .nodeLabel(() => '')
            .nodeThreeObject(self.makeCube)
            .nodeOpacity(0.92)
            .linkSource('source')
            .linkTarget('target')
            .linkColor(e => {
                if (e.sentiment === 'negative') return '#dc3545';
                if (e.sentiment === 'positive') return '#28a745';
                return '#888888';
            })
            .linkOpacity(0.4)
            .linkWidth(e => Math.max(0.4, (e.weight || 1) * 0.4))
            // Edges are bidirectional — no directional cone arrows.
            .linkDirectionalArrowLength(0)
            .onNodeClick(n => {
                self.selectedId = n.id;
                self.graph.nodeThreeObject(self.makeCube); // refresh cube tints

                // Smooth camera focus on the clicked node
                const distance = 100;
                const dist = Math.hypot(n.x || 1, n.y || 1, n.z || 1);
                const distRatio = 1 + distance / dist;
                graph.cameraPosition(
                    { x: (n.x || 0) * distRatio, y: (n.y || 0) * distRatio, z: (n.z || 0) * distRatio },
                    { x: n.x || 0, y: n.y || 0, z: n.z || 0 },
                    800
                );

                self.showPopover(n);
            })
            .onNodeRightClick(n => {
                if (self.ref) self.ref.invokeMethodAsync('OnNodeExpand', n.id);
            })
            .onBackgroundClick(() => {
                self.selectedId = null;
                self.hidePopover();
                if (self.graph) self.graph.nodeThreeObject(self.makeCube); // refresh cube tints
                if (self.ref) self.ref.invokeMethodAsync('OnBackgroundClicked');
            });

        // Tune the simulation forces a bit for readability with mid-sized graphs
        graph.d3Force('charge').strength(-90);
        graph.d3Force('link').distance(40);

        self.graph = graph;

        // Reposition the popover every simulation tick so it tracks the cube
        // as the camera or the node moves. Cheap — one element transform.
        graph.onEngineTick(() => self.repositionPopover());

        self.resizeHandler = () => {
            if (!self.graph || !container) return;
            self.graph.width(container.clientWidth);
            self.graph.height(container.clientHeight);
        };
        window.addEventListener('resize', self.resizeHandler);
    },

    // ── Sticky popover ─────────────────────────────────────────────────
    // Anchored above the selected cube. Stays open until the user clicks
    // the entry (which loads the entity in the side panel and closes the
    // popover) or clicks the canvas background.
    showPopover: function (node) {
        const container = document.getElementById(this.containerId);
        if (!container) return;
        const self = this;

        if (!this.popover) {
            const el = document.createElement('div');
            el.className = 'graph3d-popover';
            // Inline styles match the 2D graph's labelling palette and the dark theme.
            el.style.cssText = [
                'position: absolute',
                'pointer-events: auto',
                'background: #111',
                'border: 1px solid #333',
                'border-radius: 4px',
                'padding: 6px 10px',
                'color: #ddd',
                'font-family: system-ui, sans-serif',
                'font-size: 12px',
                'line-height: 1.3',
                'box-shadow: 0 4px 12px rgba(0,0,0,0.6)',
                'z-index: 50',
                'white-space: nowrap',
                'transform: translate(-50%, calc(-100% - 12px))',
            ].join(';');
            container.appendChild(el);
            this.popover = el;
        }

        const typeColor = this.typeColors[node.nodeType] || '#6c757d';
        const safeName = (node.name || '').replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
        const safeType = (node.nodeType || 'unknown').replace(/[<>&]/g, '');
        this.popover.innerHTML = `
            <div style="display:flex; align-items:center; gap:6px;">
                <span style="display:inline-block; width:9px; height:9px; background:${typeColor}; border-radius:1px;"></span>
                <span style="color:#888; font-size:0.7rem; text-transform:uppercase; letter-spacing:0.06em;">${safeType}</span>
            </div>
            <a href="#" class="graph3d-popover-link" style="color:#0dcaf0; font-weight:600; text-decoration:none; cursor:pointer; display:block; margin-top:2px;">${safeName}</a>
        `;

        const link = this.popover.querySelector('.graph3d-popover-link');
        link.onclick = (e) => {
            e.preventDefault();
            e.stopPropagation();
            if (self.ref) self.ref.invokeMethodAsync('OnNodeClicked', node.id);
            self.hidePopover();
        };

        this.popoverNode = node;
        this.repositionPopover();
    },

    hidePopover: function () {
        if (this.popover) {
            this.popover.style.display = 'none';
        }
        this.popoverNode = null;
    },

    repositionPopover: function () {
        if (!this.popover || !this.popoverNode || !this.graph) return;
        const n = this.popoverNode;
        if (typeof n.x !== 'number') return;
        // graph2ScreenCoords gives client-relative coords; subtract the container's
        // bounding rect to get container-relative for the absolute-positioned popover.
        const screen = this.graph.graph2ScreenCoords(n.x, n.y, n.z);
        if (!screen) return;
        const container = document.getElementById(this.containerId);
        if (!container) return;
        // The 3d-force-graph renderer's screen coords are already canvas-relative
        // (origin at the canvas top-left), so apply directly.
        this.popover.style.display = 'block';
        this.popover.style.left = `${screen.x}px`;
        this.popover.style.top = `${screen.y}px`;
    },

    highlight: function (nodeId) {
        this.selectedId = nodeId;
        if (this.graph) this.graph.nodeThreeObject(this.makeCube);
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
        if (this.popover && this.popover.parentNode) {
            this.popover.parentNode.removeChild(this.popover);
        }
        this.popover = null;
        this.popoverNode = null;
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
        this.makeCube = null;
    }
};
