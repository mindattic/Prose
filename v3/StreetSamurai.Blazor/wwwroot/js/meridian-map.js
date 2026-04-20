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
                    strokeWeight: 1
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
        this._drawZones();
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
                icon: { path: google.maps.SymbolPath.CIRCLE, scale: 6, fillColor: '#dc3545', fillOpacity: 0.8, strokeColor: '#dc3545', strokeWeight: 1 }
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

    _drawZones: function () {
        var map = this.map;
        var layers = this.districtLayers;

        // Corponation sovereign territories + gray zones along The Spine (Z1–Z10+)
        // Polygons hug the western Lake Michigan coastline; gray zones are ungoverned buffer strips
        var zones = [
            // Z11 — Southern Wrap: Indiana Arc around the base of Lake Michigan
            {
                id: 'Z11',
                name: 'Z11 — Southern Wrap / Indiana Arc',
                corps: 'Ashgrave Eastern Ops · Dunes Extraction Cooperative · Hearthstone Heavy Industries',
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
            // Z6 — South Side Chicago + Gary + Hammond industrial arc
            {
                id: 'Z6',
                name: 'Z6 — South Side / Gary Industrial Arc',
                corps: 'Ashgrave Materials · Slagworks Industrial · Scoria Works · Cinderfall Energy',
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
            // Gray Zone: Z6 ↔ Z1
            {
                id: 'GZ6-1',
                name: 'Gray Zone — Z6 / Z1 Buffer',
                corps: 'Ungoverned territory',
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
            // Z1 — Chicago Loop / Meridian Core (elite hub, Tessera Grand Exchange)
            {
                id: 'Z1',
                name: 'Z1 — Chicago Loop / Meridian Core',
                corps: 'Tessera · Arcturus (Coldwall) · Axiom Kinetics · Waxwing Neuromedia',
                tier: 'Elite',
                color: '#e6edf3',
                opacity: 0.15,
                paths: [
                    { lat: 41.852, lng: -87.597 },
                    { lat: 41.921, lng: -87.614 },
                    { lat: 41.921, lng: -87.763 },
                    { lat: 41.852, lng: -87.779 }
                ]
            },
            // Z5 — West Suburbs (inland; no lake access)
            {
                id: 'Z5',
                name: 'Z5 — West Suburbs / Oak Park / Cicero',
                corps: 'Ferrogate Transit · Marrowvault Cryogenics · Stonepath Logistics',
                tier: 'Mid-Low',
                color: '#79c0ff',
                opacity: 0.10,
                paths: [
                    { lat: 41.852, lng: -87.779 },
                    { lat: 41.921, lng: -87.763 },
                    { lat: 41.921, lng: -87.998 },
                    { lat: 41.852, lng: -87.998 }
                ]
            },
            // Gray Zone: Z1 ↔ Z2
            {
                id: 'GZ1-2',
                name: 'Gray Zone — Z1 / Z2 Buffer',
                corps: 'Ungoverned territory',
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
            // Z2 — Gold Coast / Lakeview / Uptown
            {
                id: 'Z2',
                name: 'Z2 — Gold Coast / Lakeview / Uptown',
                corps: 'Helix Biosystems · Novafold Pharma · Rictus Entertainment · Vespid Dynamics',
                tier: 'High',
                color: '#d2a8ff',
                opacity: 0.13,
                paths: [
                    { lat: 41.934, lng: -87.625 },
                    { lat: 41.997, lng: -87.643 },
                    { lat: 41.997, lng: -87.744 },
                    { lat: 41.934, lng: -87.754 }
                ]
            },
            // Gray Zone: Z2 ↔ Z3
            {
                id: 'GZ2-3',
                name: 'Gray Zone — Z2 / Z3 Buffer',
                corps: 'Ungoverned territory',
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
            // Z3 — Rogers Park / Evanston
            {
                id: 'Z3',
                name: 'Z3 — Rogers Park / Evanston',
                corps: 'Vellichor Institute · Pellucid Systems',
                tier: 'Mid-High',
                color: '#58a6ff',
                opacity: 0.13,
                paths: [
                    { lat: 42.011, lng: -87.652 },
                    { lat: 42.110, lng: -87.695 },
                    { lat: 42.110, lng: -87.796 },
                    { lat: 42.011, lng: -87.750 }
                ]
            },
            // Gray Zone: Z3 ↔ Z4
            {
                id: 'GZ3-4',
                name: 'Gray Zone — Z3 / Z4 Buffer',
                corps: 'Ungoverned territory',
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
            // Z4 — North Shore / Waukegan corridor
            {
                id: 'Z4',
                name: 'Z4 — North Shore / Waukegan Corridor',
                corps: 'Saltmarsh Telecom · Ashford Signal · Oracle Drift · Ringo (Northern Ops)',
                tier: 'Mid',
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
            // Gray Zone: Z4 ↔ Z7 (IL/WI state-seam — historically the widest gray zone)
            {
                id: 'GZ4-7',
                name: 'Gray Zone — Z4 / Z7 Buffer (IL/WI State Seam)',
                corps: 'Ungoverned — IL/WI bureaucratic gap',
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
            // Z7 — Kenosha / Racine corridor
            {
                id: 'Z7',
                name: 'Z7 — Kenosha / Racine Corridor',
                corps: 'Liang-Petrova Consortium · Dredge Mining Collective · Emberlace Systems',
                tier: 'Low-Mid',
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
            // Gray Zone: Z7 ↔ Z8
            {
                id: 'GZ7-8',
                name: 'Gray Zone — Z7 / Z8 Buffer',
                corps: 'Ungoverned territory',
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
            // Z8 — Milwaukee (2nd city of The Spine)
            {
                id: 'Z8',
                name: 'Z8 — Milwaukee',
                corps: 'Ouroboros Energy · Sulfur Crown Agriculture · Ironclad Agrisystems · Gravemoss Biofoundry · Silkworm Data',
                tier: 'Mid (2nd City)',
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
            // Gray Zone: Z8 ↔ Z9
            {
                id: 'GZ8-9',
                name: 'Gray Zone — Z8 / Z9 Buffer',
                corps: 'Ungoverned territory',
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
            // Z9 — Ozaukee / Sheboygan coastal corridor
            {
                id: 'Z9',
                name: 'Z9 — Ozaukee / Sheboygan Coastal Corridor',
                corps: 'Crestfall Aquaculture · Irontide Tidal Energy · Kelpline Logistics · Pelican Drift Aquatics',
                tier: 'Low',
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
            // Gray Zone: Z9 ↔ Z10
            {
                id: 'GZ9-10',
                name: 'Gray Zone — Z9 / Z10 Buffer',
                corps: 'Ungoverned territory',
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
            // Z10 — Green Bay metro + Fox Valley + Door Peninsula
            {
                id: 'Z10',
                name: 'Z10 — Green Bay Metro / Fox Valley / Door Peninsula',
                corps: 'Thornback Agrichemical · Verdant Systems · Rendstone Nuclear · Coldwater Logistics',
                tier: 'Major City',
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
