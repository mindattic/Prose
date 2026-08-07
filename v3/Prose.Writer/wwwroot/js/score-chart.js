// Score history line chart — rendered into a <canvas> via Chart.js.
// Blazor calls window.scoreChart.render(canvasId, points) where points is
// an array of { label: string, score: number, sd: number|null }.
window.scoreChart = (function () {
    const _instances = {};

    function render(canvasId, points) {
        if (_instances[canvasId]) {
            _instances[canvasId].destroy();
            delete _instances[canvasId];
        }
        const canvas = document.getElementById(canvasId);
        if (!canvas) return;

        const labels = points.map(p => p.label);
        const scores = points.map(p => p.score);

        _instances[canvasId] = new Chart(canvas, {
            type: 'line',
            data: {
                labels,
                datasets: [{
                    label: 'Score',
                    data: scores,
                    borderColor: '#f59e0b',
                    backgroundColor: 'rgba(245,158,11,0.07)',
                    fill: true,
                    tension: 0.35,
                    pointRadius: 5,
                    pointHoverRadius: 7,
                    pointBackgroundColor: '#f59e0b',
                    pointBorderColor: '#1c1c2e',
                    pointBorderWidth: 2,
                    borderWidth: 2,
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                animation: { duration: 400 },
                plugins: {
                    legend: { display: false },
                    tooltip: {
                        backgroundColor: '#1c1c2e',
                        borderColor: '#f59e0b',
                        borderWidth: 1,
                        titleColor: '#e5e7eb',
                        bodyColor: '#f59e0b',
                        padding: 10,
                        callbacks: {
                            label: ctx => '  Score: ' + ctx.parsed.y.toFixed(1) + '%'
                        }
                    }
                },
                scales: {
                    y: {
                        min: 0,
                        max: 100,
                        grid: { color: 'rgba(255,255,255,0.06)' },
                        ticks: {
                            color: '#9ca3af',
                            callback: v => v + '%',
                            stepSize: 20
                        }
                    },
                    x: {
                        grid: { color: 'rgba(255,255,255,0.06)' },
                        ticks: { color: '#9ca3af', maxRotation: 35, autoSkip: true, maxTicksLimit: 12 }
                    }
                }
            }
        });
    }

    function destroy(canvasId) {
        if (_instances[canvasId]) {
            _instances[canvasId].destroy();
            delete _instances[canvasId];
        }
    }

    return { render, destroy };
})();
