
var activeTags = new Set();
var xrefData = null;

// Load cross-reference index
fetch('xref.json').then(r => r.ok ? r.json() : {}).then(data => {
    xrefData = data;
    applyXrefs();
}).catch(() => { xrefData = {}; });

function applyXrefs() {
    if (!xrefData || Object.keys(xrefData).length === 0) return;
    // Build a regex from all entity names (longest first to avoid partial matches)
    var names = Object.keys(xrefData).sort((a, b) => b.length - a.length);
    // Only xref names 4+ chars to avoid false matches
    names = names.filter(n => n.length >= 4);
    if (names.length === 0) return;

    // Process all elements with data-xref attribute
    document.querySelectorAll('[data-xref]').forEach(el => {
        var html = el.innerHTML;
        // Don't process if already has xref links
        if (html.indexOf('class="xref"') !== -1) return;

        // Simple approach: replace first occurrence of each name
        var used = new Set();
        for (var i = 0; i < names.length && used.size < 10; i++) {
            var name = names[i];
            var idx = html.indexOf(name);
            if (idx === -1) continue;
            // Don't link if inside an HTML tag
            var before = html.substring(0, idx);
            if ((before.match(/</g) || []).length > (before.match(/>/g) || []).length) continue;
            var ref = xrefData[name];
            var link = '<a class="xref" href="' + ref.r + '.htm#' + ref.e + '">' + name + '</a>';
            html = html.substring(0, idx) + link + html.substring(idx + name.length);
            used.add(name);
        }
        if (used.size > 0) el.innerHTML = html;
    });
}

function applyFilters() {
    var q = (document.getElementById('filterInput') || {}).value || '';
    q = q.toLowerCase();
    var clearBtn = document.getElementById('filterClear');
    if (clearBtn) clearBtn.style.display = q.length > 0 ? 'block' : 'none';

    var entries = document.querySelectorAll('.entry');
    var visible = 0;
    entries.forEach(function(e) {
        var entryName = (e.getAttribute('data-name') || '').toLowerCase();
        var textMatch = q.length === 0 || entryName.indexOf(q) !== -1;
        var tagMatch = true;
        if (activeTags.size > 0) {
            var entryTags = (e.getAttribute('data-tags') || '').split(',').filter(Boolean);
            activeTags.forEach(function(t) { if (entryTags.indexOf(t) === -1) tagMatch = false; });
        }
        var show = textMatch && tagMatch;
        e.style.display = show ? '' : 'none';
        if (show) visible++;
    });

    // Sync TOC items with visible entries
    document.querySelectorAll('.toc li').forEach(function(li) {
        var link = li.querySelector('a');
        if (!link) return;
        var href = link.getAttribute('href');
        if (!href || !href.startsWith('#')) return;
        var targetId = href.substring(1);
        // Find the entry-anchor with this id, then check its sibling .entry
        var anchor = document.getElementById(targetId);
        if (!anchor) { li.style.display = 'none'; return; }
        var entry = anchor.nextElementSibling;
        if (!entry || !entry.classList.contains('entry')) { li.style.display = 'none'; return; }
        li.style.display = entry.style.display === 'none' ? 'none' : '';
    });

    var counter = document.getElementById('visibleCount');
    if (counter) counter.textContent = 'Showing ' + visible + ' of ' + entries.length;
}

function filterEntries() { applyFilters(); }
function clearFilter() { document.getElementById('filterInput').value = ''; applyFilters(); }

function toggleTagDropdown() {
    var dd = document.getElementById('tagDropdown');
    dd.style.display = dd.style.display === 'none' ? 'block' : 'none';
    if (dd.style.display === 'block') document.getElementById('tagSearch').focus();
}

function toggleTag(tag, btn) {
    if (activeTags.has(tag)) {
        activeTags.delete(tag);
    } else {
        activeTags.add(tag);
    }
    // Sync checkboxes in dropdown
    document.querySelectorAll('.tag-dd-item input[data-tag="' + tag + '"]').forEach(cb => cb.checked = activeTags.has(tag));
    // Sync entry tag pills
    document.querySelectorAll('.entry-tag').forEach(et => {
        if (et.textContent.toLowerCase() === tag) {
            et.style.borderColor = activeTags.has(tag) ? 'var(--accent)' : '';
            et.style.color = activeTags.has(tag) ? 'var(--accent)' : '';
        }
    });
    updateTagLabel();
    updateActivePills();
    applyFilters();
}

function updateTagLabel() {
    var label = document.getElementById('tagLabel');
    var count = document.getElementById('tagSelectedCount');
    if (label) label.textContent = activeTags.size === 0 ? 'Filter by tags...' : activeTags.size + ' tag' + (activeTags.size !== 1 ? 's' : '') + ' selected';
    if (count) count.textContent = activeTags.size > 0 ? activeTags.size + ' active' : '';
}

function updateActivePills() {
    var area = document.getElementById('activePills');
    if (!area) return;
    if (activeTags.size === 0) { area.innerHTML = ''; return; }
    area.innerHTML = Array.from(activeTags).sort().map(function(t) {
        return '<span class="tag-pill" onclick="toggleTag(\'' + t.replace(/'/g, "\\'") + '\', null)">' + t + ' <i class="bi bi-x"></i></span>';
    }).join('') + '<span class="tag-pill tag-pill-clear" onclick="clearTags()">Clear all</span>';
}

function clearTags() {
    activeTags.clear();
    document.querySelectorAll('.tag-dd-item input').forEach(cb => cb.checked = false);
    document.querySelectorAll('.entry-tag').forEach(et => { et.style.borderColor = ''; et.style.color = ''; });
    updateTagLabel();
    updateActivePills();
    applyFilters();
}

function filterTags() {
    var q = document.getElementById('tagSearch').value.toLowerCase();
    document.querySelectorAll('.tag-dd-item').forEach(item => {
        var tag = item.querySelector('input').getAttribute('data-tag');
        item.style.display = tag.indexOf(q) !== -1 ? '' : 'none';
    });
}

function copyLink(slug) {
    var url = location.href.split('#')[0] + '#' + slug;
    navigator.clipboard.writeText(url);
}

function toggleSidebar() {
    document.getElementById('sidebar').classList.toggle('open');
}

function globalSearchGo() {
    var q = document.getElementById('globalSearch').value.trim();
    if (!q) return;
    // If on a repo page, use the local filter
    var local = document.getElementById('filterInput');
    if (local) { local.value = q; filterEntries(); return; }
    // On index, find first repo page and search there
    var firstLink = document.querySelector('.sidebar-nav a');
    if (firstLink) window.location = firstLink.href + '?q=' + encodeURIComponent(q);
}

// Scroll-to-top visibility
var mainContent = document.getElementById('mainContent');
if (mainContent) {
    mainContent.addEventListener('scroll', function() {
        var btn = document.getElementById('scrollTop');
        if (btn) btn.classList.toggle('visible', mainContent.scrollTop > 300);
    });
}

// Build tag bar on load
document.addEventListener('DOMContentLoaded', function() {
    // Apply query string filter
    var params = new URLSearchParams(window.location.search);
    var q = params.get('q');
    if (q) {
        var fi = document.getElementById('filterInput');
        if (fi) { fi.value = q; setTimeout(filterEntries, 100); }
    }

    // Show initial count
    applyFilters();

    var tagBar = document.getElementById('tagBar');
    if (!tagBar) return;

    var tagCounts = {};
    document.querySelectorAll('.entry').forEach(function(e) {
        var tags = (e.getAttribute('data-tags') || '').split(',').filter(Boolean);
        tags.forEach(function(t) { tagCounts[t] = (tagCounts[t] || 0) + 1; });
    });

    var sorted = Object.entries(tagCounts).filter(function(t) { return t[1] >= 2; }).sort(function(a, b) { return a[0].localeCompare(b[0]); });
    if (sorted.length === 0) { tagBar.style.display = 'none'; return; }

    // Dropdown with checkboxes
    var dd = document.createElement('div');
    dd.className = 'tag-dropdown-wrap';
    dd.innerHTML = '<button class="tag-toggle" id="tagToggle" onclick="toggleTagDropdown()">' +
        '<i class="bi bi-tags"></i> <span id="tagLabel">Filter by tags...</span> <i class="bi bi-chevron-down" style="font-size:0.7em;margin-left:4px;"></i></button>' +
        '<div class="tag-dropdown" id="tagDropdown" style="display:none;">' +
        '<input type="text" id="tagSearch" class="tag-dd-search" placeholder="Search tags..." oninput="filterTags()" />' +
        '<div class="tag-dd-actions"><button onclick="clearTags()">Clear all</button><span id="tagSelectedCount"></span></div>' +
        '<div class="tag-dd-list">' +
        sorted.map(function(t) {
            var checked = activeTags.has(t[0]) ? ' checked' : '';
            return '<label class="tag-dd-item"><input type="checkbox" data-tag="' + t[0] + '"' + checked +
                ' onchange="toggleTag(\'' + t[0].replace(/'/g, "\\'") + '\', null)" />' +
                '<span class="tag-dd-name">' + t[0] + '</span><span class="tag-dd-count">' + t[1] + '</span></label>';
        }).join('') +
        '</div></div>';
    tagBar.appendChild(dd);

    // Show active tag pills below dropdown
    var pillArea = document.createElement('div');
    pillArea.id = 'activePills';
    pillArea.className = 'tag-pills';
    tagBar.appendChild(pillArea);

    // Close dropdown on outside click
    document.addEventListener('click', function(e) {
        var wrap = document.querySelector('.tag-dropdown-wrap');
        if (wrap && !wrap.contains(e.target)) {
            document.getElementById('tagDropdown').style.display = 'none';
        }
    });
});
