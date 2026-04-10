// HERE Maps JS API integration for StreetSamurai
// Dark-themed map with satellite toggle, minimal UI, neo-noir urban aesthetic

window.hereMap = {
    _platform: null,
    _map: null,
    _ui: null,
    _defaultLayers: null,
    _lastLat: null,
    _lastLng: null,
    _isSatellite: false,

    show: function (containerId, appId, apiKey, lat, lng, label) {
        var el = document.getElementById(containerId);
        if (!el) return;

        // Don't reinitialize if same location
        if (this._map && this._lastLat === lat && this._lastLng === lng) return;
        this._lastLat = lat;
        this._lastLng = lng;

        el.style.background = '#1a1a2e';

        if (!window.H) {
            this._loadScripts(function () {
                window.hereMap._initMap(el, apiKey, lat, lng, label);
            });
            return;
        }

        this._initMap(el, apiKey, lat, lng, label);
    },

    _loadScripts: function (callback) {
        var scripts = [
            'https://js.api.here.com/v3/3.1/mapsjs-core.js',
            'https://js.api.here.com/v3/3.1/mapsjs-service.js',
            'https://js.api.here.com/v3/3.1/mapsjs-ui.js',
            'https://js.api.here.com/v3/3.1/mapsjs-mapevents.js'
        ];

        var link = document.createElement('link');
        link.rel = 'stylesheet';
        link.type = 'text/css';
        link.href = 'https://js.api.here.com/v3/3.1/mapsjs-ui.css';
        document.head.appendChild(link);

        var loaded = 0;
        var total = scripts.length;

        scripts.forEach(function (src) {
            var script = document.createElement('script');
            script.src = src;
            script.async = false;
            script.onload = function () {
                loaded++;
                if (loaded === total && callback) callback();
            };
            document.head.appendChild(script);
        });
    },

    _initMap: function (el, apiKey, lat, lng, label) {
        if (this._map) {
            this._map.dispose();
            this._map = null;
        }
        el.innerHTML = '';
        el.style.background = '#1a1a2e';

        try {
            var platform = new H.service.Platform({ apikey: apiKey });
            this._platform = platform;

            var defaultLayers = platform.createDefaultLayers();
            this._defaultLayers = defaultLayers;

            // Start with vector map — terrain only, no labels/roads/borders
            var baseLayer = defaultLayers.vector.normal.map;
            var map = new H.Map(el, baseLayer, {
                center: { lat: lat, lng: lng },
                zoom: 14,
                pixelRatio: window.devicePixelRatio || 1
            });
            this._map = map;

            // Strip EVERYTHING except land and water — maximally featureless
            var provider = baseLayer.getProvider();
            if (provider && provider.getStyle) {
                var style = provider.getStyle();
                var applyMinimalStyle = function () {
                    var config = style.extractConfig();
                    if (config && config.layers) {
                        var keepPatterns = ['water', 'ocean', 'sea', 'lake', 'river', 'land', 'earth', 'background', 'continent', 'natural', 'green', 'park', 'forest', 'wood'];
                        Object.keys(config.layers).forEach(function (layerName) {
                            var lower = layerName.toLowerCase();
                            var keep = keepPatterns.some(function (p) { return lower.indexOf(p) !== -1; });
                            if (!keep) {
                                config.layers[layerName].visible = false;
                            }
                        });
                        style.mergeConfig(config);
                    }
                };
                if (style.getState() === 'ready') {
                    applyMinimalStyle();
                } else {
                    style.addEventListener('change', function () {
                        if (style.getState() === 'ready') applyMinimalStyle();
                    });
                }
            }

            // Resize listener
            window.addEventListener('resize', function () { map.getViewPort().resize(); });

            // Interactive controls
            var behavior = new H.mapevents.Behavior(new H.mapevents.MapEvents(map));

            // Minimal UI — just zoom, no settings panel
            var ui = new H.ui.UI(map);
            ui.addControl('zoom', new H.ui.ZoomControl());
            ui.addControl('scalebar', new H.ui.ScaleBar());
            this._ui = ui;

            // Add marker with custom styling
            var markerIcon = new H.map.Icon(
                '<svg xmlns="http://www.w3.org/2000/svg" width="24" height="32" viewBox="0 0 24 32">' +
                '<path d="M12 0C5.4 0 0 5.4 0 12c0 9 12 20 12 20s12-11 12-20C24 5.4 18.6 0 12 0z" fill="#dc3545"/>' +
                '<circle cx="12" cy="12" r="5" fill="#0d1117"/>' +
                '</svg>',
                { size: { w: 24, h: 32 }, anchor: { x: 12, y: 32 } }
            );
            var marker = new H.map.Marker({ lat: lat, lng: lng }, { icon: markerIcon });
            map.addObject(marker);

            // Info bubble on tap
            if (label) {
                marker.addEventListener('tap', function () {
                    var bubble = new H.ui.InfoBubble({ lat: lat, lng: lng }, {
                        content: '<div style="padding:6px 10px;font-size:12px;background:#0d1117;color:#e6edf3;border:1px solid #30363d;border-radius:4px;">' +
                            '<b style="color:#dc3545;">' + label + '</b><br>' +
                            '<span style="color:#8b949e;">' + lat.toFixed(4) + ', ' + lng.toFixed(4) + '</span></div>'
                    });
                    ui.addBubble(bubble);
                });
            }

            // Add satellite toggle button
            this._addLayerToggle(el, map, defaultLayers);

            // Force resize after Blazor render
            setTimeout(function () { map.getViewPort().resize(); }, 200);

        } catch (e) {
            el.innerHTML = '<div style="color:#dc3545;padding:20px;text-align:center;">Map error: ' + e.message + '</div>';
        }
    },

    _addLayerToggle: function (el, map, layers) {
        // Floating button to toggle satellite/vector view
        var btn = document.createElement('button');
        btn.innerHTML = '🛰';
        btn.title = 'Toggle satellite view';
        btn.style.cssText = 'position:absolute;top:10px;left:10px;z-index:100;width:32px;height:32px;' +
            'background:#0d1117;color:#e6edf3;border:1px solid #30363d;border-radius:4px;cursor:pointer;' +
            'font-size:16px;display:flex;align-items:center;justify-content:center;';

        var self = this;
        btn.addEventListener('click', function () {
            if (self._isSatellite) {
                map.setBaseLayer(layers.vector.normal.map);
                btn.innerHTML = '🛰';
                btn.title = 'Switch to satellite';
            } else {
                map.setBaseLayer(layers.raster.satellite.map);
                btn.innerHTML = '🗺';
                btn.title = 'Switch to map';
            }
            self._isSatellite = !self._isSatellite;
        });

        el.style.position = 'relative';
        el.appendChild(btn);
    },

    destroy: function () {
        if (this._map) {
            this._map.dispose();
            this._map = null;
        }
        this._platform = null;
        this._ui = null;
        this._defaultLayers = null;
        this._lastLat = null;
        this._lastLng = null;
        this._isSatellite = false;
    }
};
