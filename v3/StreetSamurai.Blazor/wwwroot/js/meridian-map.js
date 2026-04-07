// Meridian 88 map — Google Maps JS API with SnazzyMaps style + district overlays
window.meridianMap = {
    map: null,
    scriptLoaded: false,
    districtLayers: [],
    districtVisible: true,

    init: function (elementId, apiKey) {
        if (!this.scriptLoaded) {
            var script = document.createElement('script');
            script.src = 'https://maps.googleapis.com/maps/api/js?key=' + apiKey + '&callback=meridianMap._onLoad';
            script.async = true;
            script.defer = true;
            this._pendingElementId = elementId;
            document.head.appendChild(script);
            this.scriptLoaded = true;
        } else if (window.google && window.google.maps) {
            this._createMap(elementId);
        }
    },

    _onLoad: function () {
        if (window.meridianMap._pendingElementId) {
            window.meridianMap._createMap(window.meridianMap._pendingElementId);
        }
    },

    toggleDistricts: function () {
        this.districtVisible = !this.districtVisible;
        this.districtLayers.forEach(function (layer) {
            layer.setMap(window.meridianMap.districtVisible ? window.meridianMap.map : null);
        });
    },

    _createMap: function (elementId) {
        var el = document.getElementById(elementId);
        if (!el) return;

        this.map = new google.maps.Map(el, {
            center: { lat: 42.2, lng: -87.85 },
            zoom: 9,
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
        this._drawDistricts();
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
        cities.forEach(function (city) {
            new google.maps.Marker({
                position: { lat: city.lat, lng: city.lng },
                map: map,
                label: { text: city.label, color: '#e6edf3', fontSize: '11px', fontFamily: 'Outfit, sans-serif' },
                icon: { path: google.maps.SymbolPath.CIRCLE, scale: 6, fillColor: '#dc3545', fillOpacity: 0.8, strokeColor: '#dc3545', strokeWeight: 1 }
            });
        });
    },

    _drawDistricts: function () {
        var map = this.map;
        var layers = this.districtLayers;

        // District definitions — approximate Chicago-area polygons
        // These overlap intentionally to show the vertical layering of tiers

        var districts = [
            {
                name: 'The Shelf',
                tier: 'Tier 1',
                color: '#dc3545', // red
                opacity: 0.15,
                // South Side + far West Side — ground level, the foundation
                paths: [
                    { lat: 41.92, lng: -87.76 },
                    { lat: 41.92, lng: -87.58 },
                    { lat: 41.82, lng: -87.54 },
                    { lat: 41.72, lng: -87.54 },
                    { lat: 41.64, lng: -87.56 },
                    { lat: 41.64, lng: -87.68 },
                    { lat: 41.72, lng: -87.76 },
                    { lat: 41.78, lng: -87.80 },
                    { lat: 41.85, lng: -87.80 }
                ]
            },
            {
                name: 'The Circuit',
                tier: 'Tier 2-3',
                color: '#f0883e', // orange
                opacity: 0.12,
                // Broad working ring — Pilsen, Bridgeport, Logan Square, Humboldt Park
                paths: [
                    { lat: 41.97, lng: -87.72 },
                    { lat: 41.97, lng: -87.63 },
                    { lat: 41.92, lng: -87.60 },
                    { lat: 41.84, lng: -87.60 },
                    { lat: 41.82, lng: -87.64 },
                    { lat: 41.84, lng: -87.70 },
                    { lat: 41.88, lng: -87.74 },
                    { lat: 41.93, lng: -87.74 }
                ]
            },
            {
                name: 'Old Harbor',
                tier: 'Tier 2',
                color: '#58a6ff', // blue
                opacity: 0.15,
                // Southern lakefront — Navy Pier south through Calumet
                paths: [
                    { lat: 41.90, lng: -87.61 },
                    { lat: 41.90, lng: -87.58 },
                    { lat: 41.86, lng: -87.54 },
                    { lat: 41.74, lng: -87.52 },
                    { lat: 41.66, lng: -87.53 },
                    { lat: 41.66, lng: -87.56 },
                    { lat: 41.73, lng: -87.56 },
                    { lat: 41.82, lng: -87.58 },
                    { lat: 41.87, lng: -87.60 }
                ]
            },
            {
                name: 'The Laceworks',
                tier: 'Tier 3-4',
                color: '#a371f7', // purple
                opacity: 0.12,
                // North Side — Lakeview, Lincoln Square, Andersonville, Uptown
                paths: [
                    { lat: 42.02, lng: -87.70 },
                    { lat: 42.02, lng: -87.63 },
                    { lat: 41.98, lng: -87.63 },
                    { lat: 41.95, lng: -87.64 },
                    { lat: 41.94, lng: -87.66 },
                    { lat: 41.95, lng: -87.70 },
                    { lat: 41.98, lng: -87.71 }
                ]
            },
            {
                name: 'Meridian Core',
                tier: 'Tier 3-4',
                color: '#e6edf3', // white
                opacity: 0.10,
                // The Loop — compact downtown
                paths: [
                    { lat: 41.895, lng: -87.645 },
                    { lat: 41.895, lng: -87.620 },
                    { lat: 41.875, lng: -87.620 },
                    { lat: 41.875, lng: -87.640 },
                    { lat: 41.880, lng: -87.645 }
                ]
            },
            {
                name: 'The Spires',
                tier: 'Tier 4-5',
                color: '#d2a8ff', // gold/lavender
                opacity: 0.10,
                // Gold Coast, Mag Mile, Lake Shore Drive north — plus tower tops everywhere
                paths: [
                    { lat: 41.92, lng: -87.635 },
                    { lat: 41.92, lng: -87.615 },
                    { lat: 41.895, lng: -87.615 },
                    { lat: 41.895, lng: -87.625 },
                    { lat: 41.90, lng: -87.635 }
                ]
            }
        ];

        districts.forEach(function (d) {
            var polygon = new google.maps.Polygon({
                paths: d.paths,
                strokeColor: d.color,
                strokeOpacity: 0.6,
                strokeWeight: 1.5,
                fillColor: d.color,
                fillOpacity: d.opacity,
                map: map
            });

            // Info window on click
            var infoWindow = new google.maps.InfoWindow();
            polygon.addListener('click', function (e) {
                infoWindow.setContent(
                    '<div style="color:#0d1117;font-family:Outfit,sans-serif;padding:4px;">' +
                    '<strong style="color:#dc3545;">' + d.name + '</strong><br>' +
                    '<span style="font-size:12px;color:#555;">' + d.tier + '</span>' +
                    '</div>'
                );
                infoWindow.setPosition(e.latLng);
                infoWindow.open(map);
            });

            layers.push(polygon);

            // District label
            var bounds = new google.maps.LatLngBounds();
            d.paths.forEach(function (p) { bounds.extend(p); });
            var center = bounds.getCenter();

            new google.maps.Marker({
                position: center,
                map: map,
                label: {
                    text: d.name,
                    color: d.color,
                    fontSize: '10px',
                    fontFamily: 'Outfit, sans-serif',
                    fontWeight: '600'
                },
                icon: { path: 'M 0,0', scale: 0 } // invisible marker, label only
            });
        });
    }
};
