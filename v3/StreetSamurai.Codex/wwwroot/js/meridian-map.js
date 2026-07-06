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
    pulseRoutes: [],
    aeroplexLayers: [],
    uwtrMarkers: [],
    waveLine: null,
    pulseVisible: true,
    aeroplexesVisible: true,
    underwaterVisible: true,
    waveVisible: true,

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
            center: { lat: 41.86, lng: -87.67 },
            zoom: 11,
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
        this._drawFerrocementWave();
        this._drawPulseRoutes();
        this._drawAeroplexes();
        this._drawUnderwaterFeatures();
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
                        var isNull = d.type === 'null';
                        var nameColor = (isGray || isNull) ? '#8b949e' : d.color;
                        var subtitle = isNull
                            ? 'Z&#x221E; &mdash; Ungoverned'
                            : isGray
                                ? 'Gray Zone &mdash; Ungoverned'
                                : 'Prestige ' + d.prestige + ' &middot; ' + (d.loopProximity || '');
                        var govLine = (isGray || isNull) && d.governance
                            ? '<div style="font-size:11px;color:#555;line-height:1.45;margin-bottom:6px;">' + d.governance + '</div>'
                            : !isGray && !isNull && d.corponationName
                                ? '<div style="font-size:11px;color:#666;line-height:1.45;margin-bottom:6px;">' + d.corponationName + '</div>'
                                : '';
                        var descLine = d.desc
                            ? '<div style="font-size:11px;color:#444;line-height:1.5;margin-bottom:6px;">' + d.desc + '</div>'
                            : '';
                        var quoteLine = d.quote
                            ? '<div style="font-size:11px;color:#666;font-style:italic;border-left:2px solid #ccc;padding-left:8px;margin-bottom:6px;">' + d.quote + '</div>'
                            : '';
                        var transitLine = d.transit
                            ? '<div style="font-size:9px;color:#58a6ff;text-transform:uppercase;letter-spacing:0.5px;margin-bottom:2px;">Pulse / Transit</div>' +
                              '<div style="font-size:11px;color:#444;line-height:1.5;margin-bottom:6px;white-space:pre-line;">' + d.transit + '</div>'
                            : '';
                        var warnLine = d.tierWarning
                            ? '<div style="font-size:9px;color:#dc3545;text-transform:uppercase;letter-spacing:0.4px;">&#x26A0; ' + d.tierWarning + '</div>'
                            : '';
                        var content =
                            '<div style="font-family:Outfit,sans-serif;max-width:360px;padding:4px 2px;">' +
                            '<div style="font-weight:700;color:' + nameColor + ';font-size:13px;margin-bottom:2px;">' + d.name + '</div>' +
                            '<div style="font-size:9px;color:#8b949e;text-transform:uppercase;letter-spacing:0.5px;margin-bottom:6px;">' + subtitle + '</div>' +
                            govLine + descLine + quoteLine + transitLine + warnLine +
                            '</div>';
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

    togglePulse: function () {
        this.pulseVisible = !this.pulseVisible;
        var m = this.pulseVisible ? this.map : null;
        this.pulseRoutes.forEach(function (r) { r.setMap(m); });
    },

    toggleAeroplexes: function () {
        this.aeroplexesVisible = !this.aeroplexesVisible;
        var m = this.aeroplexesVisible ? this.map : null;
        this.aeroplexLayers.forEach(function (c) { c.setMap(m); });
    },

    toggleUnderwaterFeatures: function () {
        this.underwaterVisible = !this.underwaterVisible;
        var m = this.underwaterVisible ? this.map : null;
        this.uwtrMarkers.forEach(function (mk) { mk.setMap(m); });
    },

    toggleWave: function () {
        this.waveVisible = !this.waveVisible;
        if (this.waveLine) this.waveLine.setMap(this.waveVisible ? this.map : null);
    },

    zoomToChicago: function () {
        if (!this.map) return;
        this.map.setCenter({ lat: 41.86, lng: -87.67 });
        this.map.setZoom(11);
    },

    _drawFerrocementWave: function () {
        var map = this.map;
        var wavePath = [
            {lat:42.05,lng:-87.660},{lat:42.019,lng:-87.647},{lat:42.000,lng:-87.636},
            {lat:41.984,lng:-87.627},{lat:41.968,lng:-87.614},{lat:41.956,lng:-87.607},
            {lat:41.941,lng:-87.600},{lat:41.930,lng:-87.594},{lat:41.916,lng:-87.591},
            {lat:41.906,lng:-87.587},{lat:41.896,lng:-87.582},{lat:41.886,lng:-87.578},
            {lat:41.876,lng:-87.576},{lat:41.863,lng:-87.575},{lat:41.850,lng:-87.572},
            {lat:41.838,lng:-87.570},{lat:41.821,lng:-87.566},{lat:41.802,lng:-87.558},
            {lat:41.782,lng:-87.553},{lat:41.762,lng:-87.547},{lat:41.742,lng:-87.543},
            {lat:41.722,lng:-87.538},{lat:41.702,lng:-87.534},{lat:41.665,lng:-87.527},
            {lat:41.625,lng:-87.520}
        ];
        this.waveLine = new google.maps.Polyline({
            path: wavePath,
            geodesic: false,
            strokeColor: '#8Ab8D0',
            strokeOpacity: 0.7,
            strokeWeight: 2.5,
            map: map
        });
        var infoWindow = new google.maps.InfoWindow();
        this.waveLine.addListener('click', function (e) {
            infoWindow.setContent(
                '<div style="font-family:Outfit,sans-serif;max-width:320px;padding:4px 2px;">' +
                '<div style="font-weight:700;color:#8Ab8D0;font-size:13px;margin-bottom:2px;">FERROCEMENT WAVE</div>' +
                '<div style="font-size:9px;color:#8b949e;text-transform:uppercase;letter-spacing:0.5px;margin-bottom:6px;">Flood Barrier &mdash; Z1 to Gary</div>' +
                '<div style="font-size:11px;color:#444;line-height:1.5;margin-bottom:6px;">60 to 90 meter poured-ferrocement flood barrier running the full GLMZ lakefront. Built in phases 2148&ndash;2186 after the two major flood cycles. The structure doubles as a transit conduit, broadcast platform, and arcology anchor in several zones.</div>' +
                '<div style="font-size:11px;color:#666;font-style:italic;border-left:2px solid #ccc;padding-left:8px;">You can walk the top of it in some places. ACS patrols are intermittent. The view is spectacular.</div>' +
                '</div>'
            );
            infoWindow.setPosition(e.latLng);
            infoWindow.open(map);
        });
    },

    _drawPulseRoutes: function () {
        var map = this.map;
        var self = this;
        var lineInfoWindow = new google.maps.InfoWindow();

        var PULSE = [
            {
                id: 'L', label: 'LAKELINE', color: '#F0A830', glowColor: 'rgba(240,168,48,0.25)',
                path: [
                    {lat:42.025,lng:-87.618},{lat:42.010,lng:-87.616},{lat:41.995,lng:-87.613},
                    {lat:41.978,lng:-87.610},{lat:41.965,lng:-87.607},{lat:41.951,lng:-87.604},
                    {lat:41.935,lng:-87.601},{lat:41.921,lng:-87.598},{lat:41.907,lng:-87.596},
                    {lat:41.886,lng:-87.621},{lat:41.874,lng:-87.619},{lat:41.862,lng:-87.618},
                    {lat:41.840,lng:-87.615},{lat:41.812,lng:-87.612},{lat:41.783,lng:-87.607},
                    {lat:41.760,lng:-87.604},{lat:41.737,lng:-87.598},{lat:41.700,lng:-87.575}
                ]
            },
            {
                id: 'X', label: 'CROSSTOWN', color: '#C040E0', glowColor: 'rgba(192,64,224,0.25)',
                path: [
                    {lat:41.882,lng:-87.840},{lat:41.882,lng:-87.800},{lat:41.882,lng:-87.766},
                    {lat:41.884,lng:-87.740},{lat:41.886,lng:-87.706},{lat:41.887,lng:-87.683},
                    {lat:41.888,lng:-87.666},{lat:41.888,lng:-87.648},{lat:41.888,lng:-87.635},
                    {lat:41.888,lng:-87.622},{lat:41.888,lng:-87.606}
                ]
            },
            {
                id: 'I', label: 'INDUSTRIAL', color: '#30B0E0', glowColor: 'rgba(48,176,224,0.25)',
                path: [
                    {lat:41.889,lng:-87.630},{lat:41.878,lng:-87.640},{lat:41.868,lng:-87.648},
                    {lat:41.858,lng:-87.655},{lat:41.843,lng:-87.648},{lat:41.830,lng:-87.638},
                    {lat:41.815,lng:-87.628},{lat:41.800,lng:-87.617},{lat:41.783,lng:-87.606},
                    {lat:41.762,lng:-87.594},{lat:41.738,lng:-87.581},{lat:41.715,lng:-87.568},
                    {lat:41.690,lng:-87.554},{lat:41.660,lng:-87.540}
                ]
            }
        ];

        var PULSE_DESC = {
            'L': 'Mach 6 vacuum tube running the lakeshore corridor from Lacuna Genomics (N) to South Chicago Hub (S). Berth class (Tier 3+) runs express; Bench class (open) stops at every platform. The 19Hz sub-audible hum is felt before the slug arrives.',
            'X': 'East-west line from Austin Terminal to Waxwing Spur. Carries the highest civilian daily volume in the GLMZ. The Humboldt Park platform is sealed since the 2221 incident.',
            'I': 'Industrial freight line from Bloom Quarter spur south to Gary Freight Hub. Officially freight-only south of Pullman. Passengers board at Bloom Quarter and Hyde Park only.'
        };

        var STAS = [
            {lat:41.886,lng:-87.621,name:'Loop Central',lines:['L','X','I']},
            {lat:41.888,lng:-87.606,name:'Waxwing Spur',lines:['X']},
            {lat:41.862,lng:-87.618,name:'Narrows',lines:['L']},
            {lat:41.783,lng:-87.607,name:'Hyde Park',lines:['L']},
            {lat:41.965,lng:-87.607,name:'Uptown',lines:['L']},
            {lat:41.995,lng:-87.613,name:'Rogers Park',lines:['L']},
            {lat:41.700,lng:-87.575,name:'South Chicago Hub',lines:['L']},
            {lat:41.882,lng:-87.800,name:'Austin Terminal',lines:['X']},
            {lat:41.886,lng:-87.706,name:'Kedzie Node',lines:['X']},
            {lat:41.878,lng:-87.640,name:'Bloom Quarter',lines:['I']},
            {lat:41.660,lng:-87.540,name:'Gary Freight',lines:['I']}
        ];

        var stationInfoWindow = new google.maps.InfoWindow();

        PULSE.forEach(function (route) {
            // glow pass
            var glow = new google.maps.Polyline({
                path: route.path, geodesic: false,
                strokeColor: route.color, strokeOpacity: 0.18, strokeWeight: 8, map: map
            });
            // core line
            var line = new google.maps.Polyline({
                path: route.path, geodesic: false,
                strokeColor: route.color, strokeOpacity: 0.85, strokeWeight: 2, map: map
            });
            [glow, line].forEach(function (pl) {
                pl.addListener('click', function (e) {
                    lineInfoWindow.setContent(
                        '<div style="font-family:Outfit,sans-serif;max-width:320px;padding:4px 2px;">' +
                        '<div style="font-weight:700;color:' + route.color + ';font-size:13px;margin-bottom:2px;">PULSE — ' + route.label + '</div>' +
                        '<div style="font-size:9px;color:#8b949e;text-transform:uppercase;letter-spacing:0.5px;margin-bottom:6px;">Mach 6 Vacuum Tube Transit</div>' +
                        '<div style="font-size:11px;color:#444;line-height:1.5;">' + (PULSE_DESC[route.id] || '') + '</div>' +
                        '</div>'
                    );
                    lineInfoWindow.setPosition(e.latLng);
                    lineInfoWindow.open(map);
                });
                self.pulseRoutes.push(pl);
            });
        });

        var LINE_COLORS = { 'L': '#F0A830', 'X': '#C040E0', 'I': '#30B0E0' };

        STAS.forEach(function (sta) {
            var lineColors = sta.lines.map(function (l) { return LINE_COLORS[l]; });
            var primaryColor = lineColors[0];
            var marker = new google.maps.Marker({
                position: { lat: sta.lat, lng: sta.lng },
                map: map,
                title: '',
                label: {
                    text: sta.name,
                    color: '#b0b8c4',
                    fontSize: '9px',
                    fontFamily: 'Outfit, sans-serif'
                },
                icon: {
                    path: google.maps.SymbolPath.CIRCLE,
                    scale: 5,
                    fillColor: '#0d1117',
                    fillOpacity: 1,
                    strokeColor: primaryColor,
                    strokeWeight: 2,
                    labelOrigin: new google.maps.Point(0, 11)
                }
            });
            marker.addListener('click', function () {
                var linesHtml = sta.lines.map(function (l) {
                    return '<span style="display:inline-block;background:' + LINE_COLORS[l] + ';color:#000;font-size:9px;font-weight:700;padding:1px 5px;border-radius:3px;margin-right:3px;">' + l + '</span>';
                }).join('');
                stationInfoWindow.setContent(
                    '<div style="font-family:Outfit,sans-serif;max-width:280px;padding:4px 2px;">' +
                    '<div style="font-weight:700;color:' + primaryColor + ';font-size:13px;margin-bottom:4px;">' + sta.name + '</div>' +
                    '<div style="margin-bottom:4px;">' + linesHtml + '</div>' +
                    '<div style="font-size:9px;color:#8b949e;text-transform:uppercase;letter-spacing:0.5px;">Pulse Station &mdash; GLMZ 2226</div>' +
                    '</div>'
                );
                stationInfoWindow.open(map, marker);
            });
            self.pulseRoutes.push(marker);
        });
    },

    _drawAeroplexes: function () {
        var map = this.map;
        var self = this;
        var infoWindow = new google.maps.InfoWindow();

        var AERO = [
            {lng:-87.631,lat:41.882,rad:1443,name:'Axiom Tower Complex',note:'Largest arcology footprint in the Loop. 94-story primary spire + 3 secondary towers. Axiom BioNanics sovereign air rights from 12m up.'},
            {lng:-87.610,lat:41.888,rad:888, name:'Waxwing Spire',note:'94-story neural broadcast facility. The Hive extends 11 stories below water table into the lakebed.'},
            {lng:-87.619,lat:41.876,rad:777, name:'Mirrorwell Arcology',note:'Mirrorwell Media sovereign from 4th floor up by treaty. Floors below leased to mixed tenants.'},
            {lng:-87.626,lat:41.883,rad:666, name:'Halcyon Combine Spire',note:'Financial clearinghouse and data vault. Air tax applies at street level beneath the shadow.'},
            {lng:-87.637,lat:41.975,rad:555, name:'Emberlace North Tower',note:'Sensor mesh relay hub for the northern GLMZ. Antenna array visible from the lake.'},
            {lng:-87.636,lat:41.858,rad:666, name:'Bloom Sciences Tower',note:'Bloom Quarter flagship research spire. Class III nanotech clean-room in the upper 20 floors.'},
            {lng:-87.567,lat:41.714,rad:1110,name:'Crucible Genomics Spire',note:'Sealed compound arcology. Greenhouse biome levels visible from Lake Michigan on clear days.'},
            {lng:-87.608,lat:41.635,rad:1998,name:'Ashgrave Slagworks Cluster',note:'Largest industrial arcology cluster in GLMZ. Continuous thermal output, visible at night as an orange glow from 40km.'}
        ];

        AERO.forEach(function (a) {
            var circle = new google.maps.Circle({
                center: { lat: a.lat, lng: a.lng },
                radius: a.rad,
                strokeColor: '#8CA0FF',
                strokeOpacity: 0.35,
                strokeWeight: 1,
                fillColor: '#8CA0FF',
                fillOpacity: 0.08,
                map: map
            });
            circle.addListener('click', function (e) {
                infoWindow.setContent(
                    '<div style="font-family:Outfit,sans-serif;max-width:300px;padding:4px 2px;">' +
                    '<div style="font-weight:700;color:#8CA0FF;font-size:13px;margin-bottom:2px;">' + a.name + '</div>' +
                    '<div style="font-size:9px;color:#8b949e;text-transform:uppercase;letter-spacing:0.5px;margin-bottom:6px;">Aeroplex Shadow &mdash; ' + Math.round(a.rad) + 'm radius</div>' +
                    '<div style="font-size:11px;color:#444;line-height:1.5;">' + a.note + '</div>' +
                    '</div>'
                );
                infoWindow.setPosition(e.latLng);
                infoWindow.open(map);
            });
            self.aeroplexLayers.push(circle);
        });
    },

    _drawUnderwaterFeatures: function () {
        var map = this.map;
        var self = this;
        var infoWindow = new google.maps.InfoWindow();

        var TYPE_COLORS = {
            node: '#1888D8', tunnel: '#106898', colony: '#3fb950',
            thermal: '#E06020', cryogenic: '#20C0E0', platform: '#8870D0'
        };

        var UWTR = [
            {lat:41.883,lng:-87.553,name:'Bathysphere Hub — 40m depth',type:'node',note:'Primary lakebed transit interchange. Bathysphere Network junction for the north lake routes. Depth: 40m. Six docking arms. Civilian access Tier 2+.'},
            {lat:41.823,lng:-87.520,name:'Kelpline Freight Node 7',type:'tunnel',note:'Kelpline Logistics sub-lake freight tunnel junction. Freight transit only. No passenger access.'},
            {lat:41.754,lng:-87.533,name:'Fishmen Settlement — Old South Beach',type:'colony',note:'Permanent underwater community on the former South Beach shelf. ~400 residents. ACS contract does not extend to lakebed.'},
            {lat:41.912,lng:-87.512,name:'Bathysphere Station North',type:'node',note:'Northern approach station. Connects to Vellichor Institute shore access. Tier 3+ required.'},
            {lat:41.703,lng:-87.543,name:'Cinderfall Thermal Drill 3',type:'thermal',note:'Active geothermal extraction shaft, Cinderfall Energy. 2.1km depth. Produces the warm upwelling current at the Calumet shelf.'},
            {lat:41.655,lng:-87.554,name:'Marrowvault Entry Shaft',type:'cryogenic',note:'Marrowvault cryogenic storage access. Depth: 180m. 12,000 cryo-berths in the lakebed strata. Access by invitation only.'},
            {lat:41.785,lng:-87.522,name:'Fishmen Settlement — Hyde Shelf',type:'colony',note:'Secondary lakebed community below Hyde Park. ~200 residents. Informal Kelpline freight connection.'},
            {lat:41.852,lng:-87.503,name:'Pelican Drift Platform 7',type:'platform',note:'Offshore autonomous processing platform, Pelican Drift Yards. Managed remotely. No permanent crew. Occasional Scav boarding attempts.'}
        ];

        UWTR.forEach(function (u) {
            var color = TYPE_COLORS[u.type] || '#58a6ff';
            var marker = new google.maps.Marker({
                position: { lat: u.lat, lng: u.lng },
                map: map,
                title: '',
                label: {
                    text: u.name.split(' — ')[0],
                    color: color,
                    fontSize: '9px',
                    fontFamily: 'Outfit, sans-serif'
                },
                icon: {
                    path: google.maps.SymbolPath.CIRCLE,
                    scale: 4,
                    fillColor: color,
                    fillOpacity: 0.7,
                    strokeColor: color,
                    strokeWeight: 1,
                    labelOrigin: new google.maps.Point(0, 10)
                }
            });
            marker.addListener('click', function () {
                infoWindow.setContent(
                    '<div style="font-family:Outfit,sans-serif;max-width:300px;padding:4px 2px;">' +
                    '<div style="font-weight:700;color:' + color + ';font-size:13px;margin-bottom:2px;">' + u.name + '</div>' +
                    '<div style="font-size:9px;color:#8b949e;text-transform:uppercase;letter-spacing:0.5px;margin-bottom:6px;">' + u.type + ' &mdash; Lake Michigan Lakebed</div>' +
                    '<div style="font-size:11px;color:#444;line-height:1.5;">' + u.note + '</div>' +
                    '</div>'
                );
                infoWindow.open(map, marker);
            });
            self.uwtrMarkers.push(marker);
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
