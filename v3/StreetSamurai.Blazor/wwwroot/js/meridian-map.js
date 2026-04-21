// GLMZ map — Google Maps JS API with SnazzyMaps style + district overlays
window.meridianMap = {
    map: null,
    scriptLoaded: false,
    districtLayers: [],
    districtLabels: [],
    cityMarkers: [],
    placeMarkers: [],
    placesVisible: true,
    districtVisible: true,
    currentTheme: 'dark',

    init: function (elementId, apiKey) {
        var self = this;
        self._pendingElementId = elementId;
        if (window.google && window.google.maps) {
            self._createMap(elementId);
        } else {
            window.__gmapsLoad(apiKey, function () {
                if (self._pendingElementId) self._createMap(self._pendingElementId);
            });
        }
    },

    toggleDistricts: function () {
        this.districtVisible = !this.districtVisible;
        this.districtLayers.forEach(function (layer) {
            layer.setMap(window.meridianMap.districtVisible ? window.meridianMap.map : null);
        });
    },

    _markerStyles: {
        colony:     { color: '#3fb950', scale: 8, shape: 'circle' },     // green circle — floating colonies
        corporate:  { color: '#e6edf3', scale: 4, shape: 'diamond' },    // white diamond — corporate
        medical:    { color: '#f778ba', scale: 4, shape: 'cross' },      // pink cross — medical
        nightlife:  { color: '#d2a8ff', scale: 4, shape: 'star' },       // purple star — nightlife
        food:       { color: '#f0883e', scale: 3, shape: 'circle' },      // orange dot — restaurants/food
        market:     { color: '#f0883e', scale: 4, shape: 'square' },     // orange square — markets
        anomaly:    { color: '#dc3545', scale: 5, shape: 'triangle' },   // red triangle — anomalies
        behemoth:   { color: '#f0883e', scale: 7, shape: 'diamond' },    // orange diamond — Behemoth sightings
        industrial: { color: '#8b949e', scale: 4, shape: 'square' },     // grey square — industrial
        community:  { color: '#3fb950', scale: 4, shape: 'square' },     // green square — community
        security:   { color: '#58a6ff', scale: 4, shape: 'diamond' },    // blue diamond — security
        underworld: { color: '#6e40c9', scale: 4, shape: 'triangle' },   // deep purple triangle — underworld
        'default':  { color: '#58a6ff', scale: 3, shape: 'circle' }      // blue dot — default
    },

    _getIconPath: function (shape) {
        switch (shape) {
            case 'diamond':   return 'M 0,-1 1,0 0,1 -1,0 Z';
            case 'square':    return 'M -1,-1 1,-1 1,1 -1,1 Z';
            case 'triangle':  return 'M 0,-1.2 1,0.8 -1,0.8 Z';
            case 'cross':     return 'M -0.3,-1 0.3,-1 0.3,-0.3 1,-0.3 1,0.3 0.3,0.3 0.3,1 -0.3,1 -0.3,0.3 -1,0.3 -1,-0.3 -0.3,-0.3 Z';
            case 'star':      return 'M 0,-1.2 0.36,-0.36 1.2,-0.36 0.6,0.18 0.78,1.02 0,0.54 -0.78,1.02 -0.6,0.18 -1.2,-0.36 -0.36,-0.36 Z';
            default:          return google.maps.SymbolPath.CIRCLE;
        }
    },

    _placeInfoWindow: null,

    loadPlaces: function (places) {
        var map = this.map;
        var self = this;
        var isDark = this.currentTheme === 'dark';
        if (!this._placeInfoWindow) {
            this._placeInfoWindow = new google.maps.InfoWindow();
        }
        var infoWindow = this._placeInfoWindow;

        places.forEach(function (p) {
            var style = self._markerStyles[p.cat] || self._markerStyles['default'];
            var iconPath = (style.shape === 'circle') ? google.maps.SymbolPath.CIRCLE : self._getIconPath(style.shape);
            var marker = new google.maps.Marker({
                position: { lat: p.lat, lng: p.lng },
                map: map,
                title: '',
                label: {
                    text: p.name,
                    color: isDark ? '#8b949e' : '#666666',
                    fontSize: '9px',
                    fontFamily: 'Outfit, sans-serif'
                },
                icon: {
                    path: iconPath,
                    scale: style.scale,
                    fillColor: style.color,
                    fillOpacity: 0.8,
                    strokeColor: style.color,
                    strokeWeight: 1,
                    labelOrigin: new google.maps.Point(0, style.scale + 5)
                }
            });
            marker._labelText = p.name;
            marker._category = p.cat;

            // Popover on click — stays open, contains a link to the entity page
            var entityUrl = p.id ? '/places?id=' + encodeURIComponent(p.id) : '/places';
            var popoverContent =
                '<div style="font-family:Outfit,sans-serif;max-width:280px;padding:4px 2px;">' +
                '<div style="font-weight:600;color:#dc3545;font-size:13px;margin-bottom:3px;">' + p.name + '</div>' +
                '<div style="font-size:10px;color:#8b949e;text-transform:uppercase;letter-spacing:0.5px;margin-bottom:6px;">' + (p.cat || 'place') + '</div>' +
                (p.desc ? '<div style="font-size:12px;color:#444;line-height:1.45;margin-bottom:8px;">' + p.desc + '</div>' : '') +
                '<a href="' + entityUrl + '" style="font-size:11px;color:#58a6ff;text-decoration:none;font-weight:500;">Open in Encyclopedia →</a>' +
                '</div>';

            marker.addListener('click', function () {
                infoWindow.setContent(popoverContent);
                infoWindow.open(map, marker);
            });

            self.placeMarkers.push(marker);
        });
    },

    togglePlaces: function () {
        this.placesVisible = !this.placesVisible;
        var map = this.placesVisible ? this.map : null;
        this.placeMarkers.forEach(function (m) { m.setMap(map); });
    },

    overlayPolygons: [],

    loadOverlayPolygons: function (polygons) {
        var map = this.map;
        var self = this;
        if (!map) return;

        // Clear existing overlay polygons
        self.overlayPolygons.forEach(function (p) { p.setMap(null); });
        self.overlayPolygons = [];

        polygons.forEach(function (p) {
            var paths = p.coords.map(function (c) { return { lat: c.lat, lng: c.lng }; });
            var poly = new google.maps.Polygon({
                paths: paths,
                strokeColor: p.strokeColor || '#dc3545',
                strokeOpacity: p.strokeOpacity || 0.8,
                strokeWeight: p.strokeWeight || 2,
                fillColor: p.fillColor || '#dc3545',
                fillOpacity: p.fillOpacity || 0.25,
                map: map
            });

            if (p.name) {
                var infoWindow = new google.maps.InfoWindow();
                poly.addListener('click', function (e) {
                    infoWindow.setContent(
                        '<div style="color:#0d1117;font-family:Outfit,sans-serif;padding:4px;max-width:300px;">' +
                        '<strong style="color:#dc3545;">' + p.name + '</strong>' +
                        (p.desc ? '<br><span style="font-size:12px;color:#555;">' + p.desc + '</span>' : '') +
                        '</div>'
                    );
                    infoWindow.setPosition(e.latLng);
                    infoWindow.open(map);
                });
            }

            self.overlayPolygons.push(poly);
        });
    },

    _categoryVisible: {},

    toggleCategory: function (category) {
        var visible = this._categoryVisible[category];
        if (visible === undefined) visible = true;
        visible = !visible;
        this._categoryVisible[category] = visible;
        var map = this.map;
        this.placeMarkers.forEach(function (m) {
            if (m._category === category) {
                m.setMap(visible ? map : null);
            }
        });
    },

    setTheme: function (theme) {
        if (!this.map) return;
        this.currentTheme = theme;
        var cityLabelColor = theme === 'light' ? '#1a1a1a' : '#e6edf3';
        // Update city marker labels
        this.cityMarkers.forEach(function (m) {
            m.setLabel({ text: m._labelText, color: cityLabelColor, fontSize: '11px', fontFamily: 'Outfit, sans-serif' });
        });
        // Update place marker labels
        var placeLabelColor = theme === 'light' ? '#666666' : '#8b949e';
        this.placeMarkers.forEach(function (m) {
            m.setLabel({ text: m._labelText, color: placeLabelColor, fontSize: '9px', fontFamily: 'Outfit, sans-serif' });
        });
        // Update district label colors — keep their district color but darken for light mode
        this.districtLabels.forEach(function (m) {
            var color = theme === 'light' ? '#333333' : m._districtColor;
            m.setLabel({ text: m._labelText, color: color, fontSize: '10px', fontFamily: 'Outfit, sans-serif', fontWeight: '600' });
        });
        if (theme === 'light') {
            this.map.setOptions({ styles: [
                { "featureType": "all", "elementType": "all", "stylers": [{ "visibility": "off" }] },
                { "featureType": "administrative.country", "elementType": "all", "stylers": [{ "visibility": "on" }] },
                { "featureType": "landscape", "elementType": "all", "stylers": [{ "color": "#fdfdfc" }, { "visibility": "on" }] },
                { "featureType": "poi", "elementType": "all", "stylers": [{ "visibility": "off" }] },
                { "featureType": "road", "elementType": "all", "stylers": [{ "visibility": "off" }] },
                { "featureType": "transit", "elementType": "all", "stylers": [{ "visibility": "off" }] },
                { "featureType": "water", "elementType": "all", "stylers": [{ "visibility": "on" }, { "lightness": -100 }, { "color": "#393737" }] },
                { "featureType": "administrative.province", "elementType": "geometry.stroke", "stylers": [{ "visibility": "on" }, { "color": "#cccccc" }, { "weight": 1 }] },
                { "featureType": "administrative.country", "elementType": "geometry.stroke", "stylers": [{ "visibility": "on" }, { "color": "#dc3545" }, { "weight": 1.5 }] },
                { "featureType": "administrative.country", "elementType": "labels.text.fill", "stylers": [{ "visibility": "on" }, { "color": "#666666" }] },
                { "featureType": "administrative.province", "elementType": "labels.text.fill", "stylers": [{ "visibility": "on" }, { "color": "#cccccc" }] }
            ]});
        } else {
            this.map.setOptions({ styles: [
                { "featureType": "all", "elementType": "all", "stylers": [{ "visibility": "off" }] },
                { "featureType": "administrative.country", "elementType": "all", "stylers": [{ "visibility": "on" }] },
                { "featureType": "landscape", "elementType": "all", "stylers": [{ "color": "#0d1117" }, { "visibility": "on" }] },
                { "featureType": "poi", "elementType": "all", "stylers": [{ "visibility": "off" }] },
                { "featureType": "road", "elementType": "all", "stylers": [{ "visibility": "off" }] },
                { "featureType": "transit", "elementType": "all", "stylers": [{ "visibility": "off" }] },
                { "featureType": "water", "elementType": "all", "stylers": [{ "visibility": "on" }, { "lightness": -100 }, { "color": "#161b22" }] },
                { "featureType": "administrative.province", "elementType": "geometry.stroke", "stylers": [{ "visibility": "on" }, { "color": "#30363d" }, { "weight": 1 }] },
                { "featureType": "administrative.country", "elementType": "geometry.stroke", "stylers": [{ "visibility": "on" }, { "color": "#dc3545" }, { "weight": 1.5 }] },
                { "featureType": "administrative.country", "elementType": "labels.text.fill", "stylers": [{ "visibility": "on" }, { "color": "#8b949e" }] },
                { "featureType": "administrative.province", "elementType": "labels.text.fill", "stylers": [{ "visibility": "on" }, { "color": "#30363d" }] }
            ]});
        }
    },

    _createMap: function (elementId) {
        var el = document.getElementById(elementId);
        if (!el) return;

        this.map = new google.maps.Map(el, {
            center: { lat: 43.1, lng: -87.90 },
            zoom: 8,
            disableDefaultUI: true,
            zoomControl: true,
            mapTypeControl: false,
            streetViewControl: false,
            fullscreenControl: true,
            backgroundColor: '#0d1117',
            styles: [
                { "featureType": "all", "elementType": "all", "stylers": [{ "visibility": "off" }] },
                { "featureType": "administrative.country", "elementType": "all", "stylers": [{ "visibility": "on" }] },
                { "featureType": "landscape", "elementType": "all", "stylers": [{ "color": "#0d1117" }, { "visibility": "on" }] },
                { "featureType": "poi", "elementType": "all", "stylers": [{ "visibility": "off" }] },
                { "featureType": "road", "elementType": "all", "stylers": [{ "visibility": "off" }] },
                { "featureType": "transit", "elementType": "all", "stylers": [{ "visibility": "off" }] },
                { "featureType": "water", "elementType": "all", "stylers": [{ "visibility": "on" }, { "lightness": -100 }, { "color": "#161b22" }] },
                { "featureType": "administrative.province", "elementType": "geometry.stroke", "stylers": [{ "visibility": "on" }, { "color": "#30363d" }, { "weight": 1 }] },
                { "featureType": "administrative.country", "elementType": "geometry.stroke", "stylers": [{ "visibility": "on" }, { "color": "#dc3545" }, { "weight": 1.5 }] },
                { "featureType": "administrative.country", "elementType": "labels.text.fill", "stylers": [{ "visibility": "on" }, { "color": "#8b949e" }] },
                { "featureType": "administrative.province", "elementType": "labels.text.fill", "stylers": [{ "visibility": "on" }, { "color": "#30363d" }] }
            ]
        });

        this._drawCorridor();
        this._drawTerritories();
        this._drawLakeMichiganRegion();
        this._drawCityMarkers();
    },

    _drawCorridor: function () {
        // The Spine — Green Bay to Milwaukee to Chicago
        new google.maps.Polyline({
            path: [
                { lat: 44.51, lng: -88.01 },
                { lat: 43.04, lng: -87.91 },
                { lat: 41.88, lng: -87.63 }
            ],
            geodesic: true,
            strokeColor: '#dc3545',
            strokeOpacity: 0.5,
            strokeWeight: 4,
            map: this.map
        });
    },

    _drawCityMarkers: function () {
        var map = this.map;
        var cities = [
            { lat: 44.51, lng: -88.01, label: 'Green Bay' },
            { lat: 43.04, lng: -87.91, label: 'Milwaukee' },
            { lat: 41.88, lng: -87.63, label: 'Chicago' }
        ];
        var self = window.meridianMap;
        var infoWindow = new google.maps.InfoWindow({ disableAutoPan: true });
        cities.forEach(function (city) {
            var marker = new google.maps.Marker({
                position: { lat: city.lat, lng: city.lng },
                map: map,
                title: '',
                label: { text: city.label, color: '#e6edf3', fontSize: '11px', fontFamily: 'Outfit, sans-serif' },
                icon: { path: google.maps.SymbolPath.CIRCLE, scale: 6, fillColor: '#dc3545', fillOpacity: 0.8, strokeColor: '#dc3545', strokeWeight: 1, labelOrigin: new google.maps.Point(0, 11) }
            });
            marker._labelText = city.label;
            marker.addListener('mouseover', function () {
                infoWindow.setContent('<div style="font-family:Outfit,sans-serif;padding:4px;"><strong style="color:#dc3545;">' + city.label + '</strong><br><span style="font-size:11px;color:#666;">Urban core — GLMZ corridor</span></div>');
                infoWindow.open(map, marker);
            });
            marker.addListener('mouseout', function () { infoWindow.close(); });
            self.cityMarkers.push(marker);
        });
    },

    _drawLakeMichiganRegion: function () {
        var map = this.map;
        var layers = this.districtLayers;
        var labels = this.districtLabels;

        function render(geoJson) {
            geoJson.features.forEach(function (feature) {
                var label  = feature.properties.name  || '';
                var region = feature.properties.region || '';

                function ring(coords) {
                    return coords.map(function (c) { return { lat: c[1], lng: c[0] }; });
                }

                var geom  = feature.geometry;
                var rings = [];
                if (geom.type === 'Polygon') {
                    rings = [ring(geom.coordinates[0])];
                } else if (geom.type === 'MultiPolygon') {
                    rings = geom.coordinates.map(function (poly) { return ring(poly[0]); });
                }

                rings.forEach(function (paths, i) {
                    var polygon = new google.maps.Polygon({
                        paths: paths,
                        strokeColor: '#3d444d',
                        strokeOpacity: 0.9,
                        strokeWeight: 1,
                        fillColor: '#58a6ff',
                        fillOpacity: 0.03,
                        map: map
                    });

                    var infoWindow = new google.maps.InfoWindow();
                    polygon.addListener('click', function (e) {
                        infoWindow.setContent(
                            '<div style="color:#0d1117;font-family:Outfit,sans-serif;padding:4px;">' +
                            '<strong style="color:#58a6ff;">' + label + '</strong>' +
                            '<br><span style="font-size:10px;color:#8b949e;text-transform:uppercase;letter-spacing:0.5px;">' + region + '</span>' +
                            '</div>'
                        );
                        infoWindow.setPosition(e.latLng);
                        infoWindow.open(map);
                    });

                    layers.push(polygon);

                    if (i === 0) {
                        var bounds = new google.maps.LatLngBounds();
                        paths.forEach(function (p) { bounds.extend(p); });
                        var center = bounds.getCenter();
                        var labelMarker = new google.maps.Marker({
                            position: center,
                            map: map,
                            label: {
                                text: label,
                                color: '#484f58',
                                fontSize: '8px',
                                fontFamily: 'Outfit, sans-serif',
                                fontWeight: '500'
                            },
                            icon: { path: 'M 0,0', scale: 0 }
                        });
                        labelMarker._districtColor = '#484f58';
                        labelMarker._labelText = label;
                        labels.push(labelMarker);
                    }
                });
            });
        }

        fetch('/data/lake-michigan-region.geojson')
            .then(function (r) { return r.json(); })
            .then(render)
            .catch(function (err) { console.warn('Lake Michigan region boundaries unavailable:', err); });
    },

    _drawTerritories: function () {
        var self = this;
        var map = this.map;
        var layers = this.districtLayers;
        var labels = this.districtLabels;

        fetch('/data/territory-map.json')
            .then(function (r) { return r.json(); })
            .then(function (entries) {
                entries.forEach(function (d) {
                    var isGray = d.type === 'grayzone';

                    var polygon = new google.maps.Polygon({
                        paths: d.paths,
                        strokeColor: d.color,
                        strokeOpacity: isGray ? 0.25 : 0.60,
                        strokeWeight: isGray ? 0.5 : 1.0,
                        fillColor: d.color,
                        fillOpacity: d.opacity,
                        map: map
                    });

                    var infoWindow = new google.maps.InfoWindow();
                    polygon.addListener('click', function (e) {
                        var content;
                        if (isGray) {
                            content =
                                '<div style="color:#0d1117;font-family:Outfit,sans-serif;padding:4px;max-width:300px;">' +
                                '<strong style="color:#8b949e;">' + d.name + '</strong>' +
                                '<br><span style="font-size:10px;color:#8b949e;text-transform:uppercase;letter-spacing:0.5px;">Gray Zone — Ungoverned</span>' +
                                (d.governance ? '<br><span style="font-size:11px;color:#555;display:block;margin-top:4px;">' + d.governance + '</span>' : '') +
                                '</div>';
                        } else {
                            content =
                                '<div style="color:#0d1117;font-family:Outfit,sans-serif;padding:4px;max-width:300px;">' +
                                '<strong style="color:' + d.color + ';">' + d.name + '</strong>' +
                                '<br><span style="font-size:10px;color:#8b949e;text-transform:uppercase;letter-spacing:0.5px;">Prestige ' + d.prestige + ' · ' + (d.loopProximity || '') + '</span>' +
                                '<br><span style="font-size:11px;color:#555;display:block;margin-top:4px;">' + d.corponationName + '</span>' +
                                '</div>';
                        }
                        infoWindow.setContent(content);
                        infoWindow.setPosition(e.latLng);
                        infoWindow.open(map);
                    });

                    layers.push(polygon);

                    if (!isGray && d.label) {
                        var bounds = new google.maps.LatLngBounds();
                        d.paths.forEach(function (p) { bounds.extend(p); });
                        var center = bounds.getCenter();
                        var fontSize = d.prestige >= 4 ? '10px' : (d.prestige >= 2 ? '9px' : '8px');
                        var labelMarker = new google.maps.Marker({
                            position: center,
                            map: map,
                            label: {
                                text: d.label,
                                color: d.color,
                                fontSize: fontSize,
                                fontFamily: 'Outfit, sans-serif',
                                fontWeight: '600'
                            },
                            icon: { path: 'M 0,0', scale: 0 }
                        });
                        labelMarker._districtColor = d.color;
                        labelMarker._labelText = d.label;
                        labels.push(labelMarker);
                    }
                });
            });
    },

    _drawZones_UNUSED: function () {
        var map = this.map;
        var layers = this.districtLayers;

        var zones = [
            // Indiana Arc — Eastern Industrial Corridor
            {
                id: 'Indiana Arc',
                name: 'Indiana Arc — Eastern Industrial Corridor',
                corps: 'Charnel Propulsion Campus · Vespid Arcology Cluster · Michigan City Gap · St. Joseph Corridor',
                tier: 'Industrial',
                color: '#ff7b72',
                opacity: 0.11,
                paths: [
                    { lat: 41.620, lng: -87.530 },
                    { lat: 41.580, lng: -87.290 },
                    { lat: 41.645, lng: -86.960 },
                    { lat: 41.775, lng: -86.660 },
                    { lat: 41.900, lng: -86.650 },
                    { lat: 41.900, lng: -86.910 },
                    { lat: 41.785, lng: -87.110 },
                    { lat: 41.678, lng: -87.530 }
                ]
            },
            // South Industrial Belt
            {
                id: 'South Industrial Belt',
                name: 'South Industrial Belt — Gary / Calumet / South Chicago',
                corps: 'Ashgrave Synthesis Corridor · Slagworks Foundry Belt · Scoria Crucible Belt · Crucible Belt · Palladian Prime · Vespid Cluster · Cinderfall Subterranean',
                tier: 'Industrial',
                color: '#f85149',
                opacity: 0.14,
                paths: [
                    { lat: 41.840, lng: -87.575 },
                    { lat: 41.826, lng: -87.533 },
                    { lat: 41.720, lng: -87.516 },
                    { lat: 41.628, lng: -87.528 },
                    { lat: 41.620, lng: -87.530 },
                    { lat: 41.626, lng: -87.650 },
                    { lat: 41.630, lng: -87.825 },
                    { lat: 41.762, lng: -87.832 },
                    { lat: 41.840, lng: -87.786 }
                ]
            },
            // Bridgeport Pocket — ungoverned seam between Loop and industrial belt
            {
                id: 'GZ6-1',
                name: 'Bridgeport Pocket — Ungoverned Seam',
                corps: 'Ungoverned — Bridgeport Block Federation · South Loop Seam · Printer\'s Row Drift',
                tier: 'Gray Zone',
                color: '#8b949e',
                opacity: 0.09,
                paths: [
                    { lat: 41.840, lng: -87.575 },
                    { lat: 41.840, lng: -87.786 },
                    { lat: 41.852, lng: -87.779 },
                    { lat: 41.852, lng: -87.597 }
                ]
            },
            // The Loop — Chicago Core (prestige 5, maximum value)
            {
                id: 'The Loop',
                name: 'The Loop — Chicago Core',
                corps: 'Tessera Sovereign Enclave · Zheng-dao Financial Corridor · Coldwall Quarter · Waxwing Spire District · Mirrorwell Arcology District',
                tier: 'Prestige 5',
                color: '#e6edf3',
                opacity: 0.15,
                paths: [
                    { lat: 41.852, lng: -87.597 },
                    { lat: 41.921, lng: -87.614 },
                    { lat: 41.921, lng: -87.763 },
                    { lat: 41.852, lng: -87.779 }
                ]
            },
            // West Corridor — O'Hare / Suburbs / Transit Infrastructure
            {
                id: 'West Corridor',
                name: 'West Corridor — O\'Hare / Suburbs / Transit Infrastructure',
                corps: 'Stonepath O\'Hare Sovereignty · Marrowvault Preserve · Pulse Hyperlane Rights · Ferrogate Rail Corridor',
                tier: 'Transit Hub',
                color: '#79c0ff',
                opacity: 0.10,
                paths: [
                    { lat: 41.852, lng: -87.779 },
                    { lat: 41.921, lng: -87.763 },
                    { lat: 41.921, lng: -87.998 },
                    { lat: 41.852, lng: -87.998 }
                ]
            },
            // Gold Coast Seam — ungoverned between Loop and Near North
            {
                id: 'GZ1-2',
                name: 'Gold Coast Seam — Ungoverned',
                corps: 'Ungoverned — legacy residential · River North Pocket · Gold Coast Seam',
                tier: 'Gray Zone',
                color: '#8b949e',
                opacity: 0.09,
                paths: [
                    { lat: 41.921, lng: -87.614 },
                    { lat: 41.921, lng: -87.763 },
                    { lat: 41.934, lng: -87.754 },
                    { lat: 41.934, lng: -87.625 }
                ]
            },
            // Near North — Streeterville to Lakeview
            {
                id: 'Near North',
                name: 'Near North — Streeterville to Lakeview',
                corps: 'Helix Streeterville Campus · Vantablack Spire · Novafold Medical Sovereign Zone · Rictus Pleasure Corridor',
                tier: 'Prestige 3–4',
                color: '#d2a8ff',
                opacity: 0.13,
                paths: [
                    { lat: 41.934, lng: -87.625 },
                    { lat: 41.997, lng: -87.643 },
                    { lat: 41.997, lng: -87.744 },
                    { lat: 41.934, lng: -87.754 }
                ]
            },
            // Rogers Park Seam — ungoverned transition
            {
                id: 'GZ2-3',
                name: 'Rogers Park Seam — Ungoverned',
                corps: 'Ungoverned — Rogers Park Commons · Lakeview Seam',
                tier: 'Gray Zone',
                color: '#8b949e',
                opacity: 0.09,
                paths: [
                    { lat: 41.997, lng: -87.643 },
                    { lat: 41.997, lng: -87.744 },
                    { lat: 42.011, lng: -87.750 },
                    { lat: 42.011, lng: -87.652 }
                ]
            },
            // Evanston Corridor — Rogers Park to North Shore
            {
                id: 'Evanston',
                name: 'Evanston Corridor — Rogers Park to North Shore',
                corps: 'Pellucid Atrium · Lazarus Compound · Vellichor Campus · Veil Campus',
                tier: 'Prestige 3–4',
                color: '#58a6ff',
                opacity: 0.13,
                paths: [
                    { lat: 42.011, lng: -87.652 },
                    { lat: 42.110, lng: -87.695 },
                    { lat: 42.110, lng: -87.796 },
                    { lat: 42.011, lng: -87.750 }
                ]
            },
            // North Shore Seam — ungoverned transition
            {
                id: 'GZ3-4',
                name: 'North Shore Seam — Ungoverned',
                corps: 'Ungoverned — North Shore Gap · Highland Park Seam',
                tier: 'Gray Zone',
                color: '#8b949e',
                opacity: 0.09,
                paths: [
                    { lat: 42.110, lng: -87.695 },
                    { lat: 42.110, lng: -87.796 },
                    { lat: 42.126, lng: -87.803 },
                    { lat: 42.126, lng: -87.707 }
                ]
            },
            // Waukegan Corridor — North Shore to Illinois Border
            {
                id: 'Waukegan',
                name: 'Waukegan Corridor — North Shore to Illinois Border',
                corps: 'Lacuna North Shore Campus · Ashford Naval Station · Ringo Northern Operations · Saltmarsh Signal Network',
                tier: 'Prestige 2–3',
                color: '#3fb950',
                opacity: 0.12,
                paths: [
                    { lat: 42.126, lng: -87.707 },
                    { lat: 42.235, lng: -87.768 },
                    { lat: 42.378, lng: -87.848 },
                    { lat: 42.378, lng: -87.978 },
                    { lat: 42.126, lng: -87.908 }
                ]
            },
            // Kenosha Gap — IL/WI bureaucratic seam (widest gray zone in the corridor)
            {
                id: 'GZ4-7',
                name: 'Kenosha Gap — IL/WI Bureaucratic Seam',
                corps: 'Ungoverned — Kenosha Corridor Gap · IL/WI jurisdictional void',
                tier: 'Gray Zone',
                color: '#8b949e',
                opacity: 0.13,
                paths: [
                    { lat: 42.378, lng: -87.848 },
                    { lat: 42.378, lng: -87.978 },
                    { lat: 42.415, lng: -87.988 },
                    { lat: 42.415, lng: -87.858 }
                ]
            },
            // Racine Corridor — Kenosha to Milwaukee Approach
            {
                id: 'Racine',
                name: 'Racine Corridor — Kenosha to Milwaukee Approach',
                corps: 'Liang-Petrova Racine Complex · Dredge Kenosha Extraction Field · Ouroboros Ring',
                tier: 'Prestige 2–3',
                color: '#d29922',
                opacity: 0.12,
                paths: [
                    { lat: 42.415, lng: -87.858 },
                    { lat: 42.574, lng: -87.822 },
                    { lat: 42.787, lng: -87.798 },
                    { lat: 42.787, lng: -87.995 },
                    { lat: 42.415, lng: -87.988 }
                ]
            },
            // Milwaukee Approach — ungoverned transition
            {
                id: 'GZ7-8',
                name: 'Milwaukee Approach — Ungoverned',
                corps: 'Ungoverned — Racine Seam · South Milwaukee Pocket',
                tier: 'Gray Zone',
                color: '#8b949e',
                opacity: 0.09,
                paths: [
                    { lat: 42.787, lng: -87.798 },
                    { lat: 42.787, lng: -87.995 },
                    { lat: 42.808, lng: -88.001 },
                    { lat: 42.808, lng: -87.806 }
                ]
            },
            // Milwaukee — The Second City
            {
                id: 'Milwaukee',
                name: 'Milwaukee — The Second City',
                corps: 'Ferment Quarter · Silkworm Arcology Cluster · Ironclad Milwaukee HQ · Sulfur Crown Territories · Ouroboros Ring',
                tier: 'Prestige 2–3',
                color: '#56d364',
                opacity: 0.12,
                paths: [
                    { lat: 42.808, lng: -87.806 },
                    { lat: 42.904, lng: -87.854 },
                    { lat: 43.047, lng: -87.903 },
                    { lat: 43.242, lng: -87.880 },
                    { lat: 43.242, lng: -88.118 },
                    { lat: 42.808, lng: -88.062 }
                ]
            },
            // Sheboygan Seam — ungoverned transition
            {
                id: 'GZ8-9',
                name: 'Sheboygan Seam — Ungoverned',
                corps: 'Ungoverned — Sheboygan Harbor Council · Menomonee Valley Seam',
                tier: 'Gray Zone',
                color: '#8b949e',
                opacity: 0.09,
                paths: [
                    { lat: 43.242, lng: -87.880 },
                    { lat: 43.242, lng: -88.118 },
                    { lat: 43.264, lng: -88.110 },
                    { lat: 43.264, lng: -87.889 }
                ]
            },
            // Sheboygan Coast — Offshore Platform Cluster
            {
                id: 'Sheboygan Coast',
                name: 'Sheboygan Coast — Offshore Platform Cluster',
                corps: 'Kelpline Coastal Network · Pelican Drift Yards · Crestfall Platform Network · Irontide Anchor Platform',
                tier: 'Prestige 1–2',
                color: '#1f6feb',
                opacity: 0.12,
                paths: [
                    { lat: 43.264, lng: -87.889 },
                    { lat: 43.514, lng: -87.760 },
                    { lat: 43.767, lng: -87.726 },
                    { lat: 43.767, lng: -87.944 },
                    { lat: 43.264, lng: -88.010 }
                ]
            },
            // Green Bay Fringe — ungoverned transition
            {
                id: 'GZ9-10',
                name: 'Green Bay Fringe — Ungoverned',
                corps: 'Ungoverned — Port Washington Pocket · Green Bay Urban Council',
                tier: 'Gray Zone',
                color: '#8b949e',
                opacity: 0.09,
                paths: [
                    { lat: 43.767, lng: -87.726 },
                    { lat: 43.767, lng: -87.944 },
                    { lat: 43.793, lng: -87.950 },
                    { lat: 43.793, lng: -87.735 }
                ]
            },
            // Green Bay / Door Peninsula — Upper Corridor
            {
                id: 'Green Bay',
                name: 'Green Bay / Door Peninsula — Upper Corridor',
                corps: 'Verdant Canopy Zones · Thornback Basin · Rendstone Exclusion Corridor · Door Peninsula Gap',
                tier: 'Prestige 1–2',
                color: '#ffa657',
                opacity: 0.13,
                paths: [
                    { lat: 43.793, lng: -87.735 },
                    { lat: 44.115, lng: -87.642 },
                    { lat: 44.385, lng: -87.565 },
                    { lat: 44.562, lng: -87.654 },
                    { lat: 44.575, lng: -88.058 },
                    { lat: 44.575, lng: -88.228 },
                    { lat: 43.793, lng: -87.963 }
                ]
            }
        ];

        zones.forEach(function (d) {
            var isGray = d.tier === 'Gray Zone';
            var polygon = new google.maps.Polygon({
                paths: d.paths,
                strokeColor: d.color,
                strokeOpacity: isGray ? 0.3 : 0.65,
                strokeWeight: isGray ? 1 : 1.5,
                fillColor: d.color,
                fillOpacity: d.opacity,
                map: map
            });

            var infoWindow = new google.maps.InfoWindow();
            polygon.addListener('click', function (e) {
                var corpsHtml = (d.corps === 'Ungoverned territory' || d.corps.startsWith('Ungoverned'))
                    ? '<span style="font-size:11px;color:#777;font-style:italic;display:block;margin-top:4px;">Ungoverned — gray market access</span>'
                    : '<span style="font-size:11px;color:#555;display:block;margin-top:4px;">' + d.corps + '</span>';
                infoWindow.setContent(
                    '<div style="color:#0d1117;font-family:Outfit,sans-serif;padding:4px;max-width:320px;">' +
                    '<strong style="color:' + d.color + ';">' + d.name + '</strong>' +
                    '<br><span style="font-size:10px;color:#8b949e;text-transform:uppercase;letter-spacing:0.5px;">' + d.tier + '</span>' +
                    corpsHtml +
                    '</div>'
                );
                infoWindow.setPosition(e.latLng);
                infoWindow.open(map);
            });

            layers.push(polygon);

            if (!isGray) {
                var bounds = new google.maps.LatLngBounds();
                d.paths.forEach(function (p) { bounds.extend(p); });
                var center = bounds.getCenter();
                var labelMarker = new google.maps.Marker({
                    position: center,
                    map: map,
                    label: {
                        text: d.id,
                        color: d.color,
                        fontSize: '10px',
                        fontFamily: 'Outfit, sans-serif',
                        fontWeight: '600'
                    },
                    icon: { path: 'M 0,0', scale: 0 }
                });
                labelMarker._districtColor = d.color;
                labelMarker._labelText = d.id;
                window.meridianMap.districtLabels.push(labelMarker);
            }
        });
    }
};
