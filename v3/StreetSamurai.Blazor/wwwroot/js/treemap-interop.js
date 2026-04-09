// Ethnic Heritage Treemap — D3.js visualization
window.heritageTreemap = {
    render: function (elementId, data) {
        var container = document.getElementById(elementId);
        if (!container) return;
        container.innerHTML = '';

        var width = container.clientWidth || 900;
        var height = Math.max(500, width * 0.6);

        var root = d3.hierarchy({ name: 'GLMZ Heritage', children: data })
            .sum(function (d) { return d.value || 0; })
            .sort(function (a, b) { return b.value - a.value; });

        d3.treemap()
            .size([width, height])
            .padding(2)
            .round(true)(root);

        var svg = d3.select('#' + elementId)
            .append('svg')
            .attr('width', width)
            .attr('height', height)
            .style('font-family', 'Outfit, sans-serif');

        // Color scale — warm tones for the diaspora
        var colorScale = d3.scaleOrdinal()
            .range([
                '#dc3545', '#e85d6c', '#f0883e', '#d2a8ff', '#58a6ff',
                '#3fb950', '#7ee787', '#f778ba', '#79c0ff', '#6e40c9',
                '#ffc107', '#e6edf3', '#8b949e', '#ff6b6b', '#4ecdc4',
                '#45b7d1', '#f9ca24', '#6c5ce7', '#a29bfe', '#fd79a8',
                '#00cec9', '#fdcb6e', '#e17055', '#74b9ff', '#55efc4',
                '#b2bec3', '#dfe6e9', '#636e72', '#2d3436', '#fab1a0'
            ]);

        var leaf = svg.selectAll('g')
            .data(root.leaves())
            .join('g')
            .attr('transform', function (d) { return 'translate(' + d.x0 + ',' + d.y0 + ')'; });

        // Rectangles
        leaf.append('rect')
            .attr('width', function (d) { return d.x1 - d.x0; })
            .attr('height', function (d) { return d.y1 - d.y0; })
            .attr('fill', function (d) { return colorScale(d.data.name); })
            .attr('fill-opacity', 0.85)
            .attr('stroke', '#0d1117')
            .attr('stroke-width', 1)
            .attr('rx', 3)
            .style('cursor', 'pointer')
            .on('mouseover', function (event, d) {
                d3.select(this).attr('fill-opacity', 1).attr('stroke', '#e6edf3').attr('stroke-width', 2);
                tooltip.style('display', 'block')
                    .html('<strong>' + d.data.name + '</strong><br>' +
                          d.data.value + ' heritage references (' + d.data.pct + '%)')
                    .style('left', (event.pageX + 12) + 'px')
                    .style('top', (event.pageY - 28) + 'px');
            })
            .on('mousemove', function (event) {
                tooltip.style('left', (event.pageX + 12) + 'px')
                    .style('top', (event.pageY - 28) + 'px');
            })
            .on('mouseout', function () {
                d3.select(this).attr('fill-opacity', 0.85).attr('stroke', '#0d1117').attr('stroke-width', 1);
                tooltip.style('display', 'none');
            });

        // Labels — only show if box is big enough
        leaf.append('text')
            .attr('x', 6)
            .attr('y', 16)
            .attr('fill', '#0d1117')
            .attr('font-size', function (d) {
                var w = d.x1 - d.x0;
                var h = d.y1 - d.y0;
                if (w < 60 || h < 20) return '0px';
                if (w < 100) return '9px';
                return '11px';
            })
            .attr('font-weight', 600)
            .text(function (d) {
                var w = d.x1 - d.x0;
                if (w < 60) return '';
                if (w < 100) return d.data.name.length > 10 ? d.data.name.substring(0, 8) + '..' : d.data.name;
                return d.data.name;
            });

        // Count labels
        leaf.append('text')
            .attr('x', 6)
            .attr('y', 30)
            .attr('fill', '#0d1117')
            .attr('font-size', function (d) {
                var w = d.x1 - d.x0;
                var h = d.y1 - d.y0;
                return (w < 60 || h < 35) ? '0px' : '9px';
            })
            .attr('opacity', 0.7)
            .text(function (d) {
                var w = d.x1 - d.x0;
                return w < 60 ? '' : d.data.value + ' (' + d.data.pct + '%)';
            });

        // Tooltip div
        var tooltip = d3.select('body').selectAll('.heritage-tooltip').data([0]).join('div')
            .attr('class', 'heritage-tooltip')
            .style('display', 'none')
            .style('position', 'absolute')
            .style('background', '#1c2128')
            .style('border', '1px solid #30363d')
            .style('border-radius', '6px')
            .style('padding', '8px 12px')
            .style('font-size', '12px')
            .style('color', '#e6edf3')
            .style('pointer-events', 'none')
            .style('z-index', '10000')
            .style('font-family', 'Outfit, sans-serif');
    }
};
