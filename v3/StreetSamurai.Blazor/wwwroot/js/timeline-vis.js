// Story timeline — vis-timeline rendering of every book / chapter / beat
// plotted on the in-world clock, with a draggable "now" marker and click-to-
// snap selection that calls back into Blazor for the world-state-at-this-
// moment panel.
//
// Uses vis-timeline (Apache-2.0). Loaded via UMD bundle from CDN — same
// family as vis-network so the bundle is largely shared.

window.streetSamuraiTimeline = (function () {
    let tl = null;          // vis.Timeline
    let dataset = null;     // vis.DataSet
    let groupset = null;    // vis.DataSet (tracks)
    let dotnetRef = null;

    // Color tokens per item type — kept consistent with the SVG version we
    // replaced so visual continuity is preserved.
    const typeColor = {
        book:    { background: '#1f4e79', border: '#65aaff' },
        chapter: { background: '#1f6240', border: '#5edcaa' },
        beat:    { background: '#5b3737', border: '#e89c9c' },
        ledger:  { background: '#3a3034', border: '#d68fa6' },
    };

    function styleFor(type) {
        const c = typeColor[type] || typeColor.beat;
        return `background-color:${c.background}; border-color:${c.border}; color:#eee;`;
    }

    function makeItem(ev) {
        return {
            id:         ev.id,
            group:      ev.type,                  // book / chapter / beat / ledger
            content:    escapeHtml(ev.title),
            start:      ev.at,                    // ISO string from .NET DateTime
            type:       'point',                  // dot on the axis
            title:      `${ev.type} · ${ev.title}\n${ev.at}` + (ev.subtitle ? `\n${ev.subtitle}` : ''),
            style:      styleFor(ev.type),
            className:  `tl-${ev.type}`,
            // Stash the navigation route for click-handling in JS.
            _route:     ev.route,
        };
    }

    return {
        async init(elementId, eventsJson, currentNowIso, dotnetObjRef) {
            const events = JSON.parse(eventsJson);
            const container = document.getElementById(elementId);
            if (!container) { console.error('timeline: container not found:', elementId); return; }

            dotnetRef = dotnetObjRef;

            groupset = new vis.DataSet([
                { id: 'book',    content: 'Books'    },
                { id: 'chapter', content: 'Chapters' },
                { id: 'beat',    content: 'Beats'    },
                { id: 'ledger',  content: 'Events'   },
            ]);
            dataset = new vis.DataSet(events.map(makeItem));

            const options = {
                stack: false,
                horizontalScroll: true,
                zoomKey: 'ctrlKey',
                orientation: { axis: 'top', item: 'top' },
                tooltip: { followMouse: true, overflowMethod: 'cap' },
                editable: false,
                multiselect: false,
                selectable: true,
                snap: null,
                showCurrentTime: false,
                margin: { item: 8 },
            };

            tl = new vis.Timeline(container, dataset, groupset, options);

            // The custom "story now" line — draggable. vis-timeline supports
            // user-modifiable custom-time markers natively.
            tl.addCustomTime(currentNowIso, 'storyNow');
            tl.setCustomTimeMarker('NOW', 'storyNow', false);

            tl.on('timechange',    onCustomTimeMove);
            tl.on('timechanged',   onCustomTimeFinal);
            tl.on('select',        onItemSelect);
            tl.on('doubleClick',   onDoubleClick);
        },

        setNow(iso) {
            if (!tl) return;
            tl.setCustomTime(new Date(iso), 'storyNow');
        },

        focus(eventId) {
            if (!tl || !dataset) return;
            const item = dataset.get(eventId);
            if (!item) return;
            tl.focus(eventId, { animation: true });
            tl.setSelection([eventId], { focus: true });
        },

        zoomToFit() {
            if (!tl || !dataset || dataset.length === 0) return;
            const items = dataset.get();
            const first = items.reduce((min, i) => new Date(i.start) < new Date(min.start) ? i : min, items[0]);
            const last  = items.reduce((max, i) => new Date(i.start) > new Date(max.start) ? i : max, items[0]);
            tl.setWindow(first.start, last.start, { animation: true });
        },
    };

    function onCustomTimeMove(ev) {
        if (ev.id !== 'storyNow') return;
        // Throttled visual feedback only — final commit happens on timechanged.
    }

    async function onCustomTimeFinal(ev) {
        if (ev.id !== 'storyNow' || !dotnetRef) return;
        try {
            await dotnetRef.invokeMethodAsync('OnNowDragged', new Date(ev.time).toISOString());
        } catch (e) { console.warn('timeline: dotnet callback failed', e); }
    }

    async function onItemSelect(ev) {
        if (!ev.items || ev.items.length === 0 || !dotnetRef) return;
        const id = ev.items[0];
        const item = dataset.get(id);
        try {
            await dotnetRef.invokeMethodAsync('OnEventSelected', id, item.start);
        } catch (e) { console.warn('timeline: select callback failed', e); }
    }

    async function onDoubleClick(props) {
        // Two distinct double-click behaviors:
        //   • on an item dot  → navigate to its associated route (e.g. /talk/{bookId}/{chapterId})
        //   • on background / axis → set storyNow to that instant
        if (!dotnetRef) return;
        if (props.item) {
            const item = dataset.get(props.item);
            if (item && item._route) {
                try { await dotnetRef.invokeMethodAsync('OnEventActivated', item._route); }
                catch (e) { console.warn('timeline: dblclick activate failed', e); }
            }
            return;
        }
        if (props.what !== 'background' && props.what !== 'axis') return;
        if (!props.time) return;
        const iso = new Date(props.time).toISOString();
        if (tl) tl.setCustomTime(props.time, 'storyNow');
        try {
            await dotnetRef.invokeMethodAsync('OnNowDragged', iso);
        } catch (e) { console.warn('timeline: dblclick callback failed', e); }
    }

    function escapeHtml(s) {
        if (s == null) return '';
        return String(s).replace(/[&<>"']/g, c =>
            ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));
    }
})();
