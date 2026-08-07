// Single-entity Google Maps component — used by GeoMap.razor on entity detail pages
window.geoMap = {
    _instances: {},

    show: function (elementId, apiKey, lat, lng, title) {
        var self = this;
        if (window.google && window.google.maps) {
            self._render(elementId, lat, lng, title);
        } else {
            window.__gmapsLoad(apiKey, function () { self._render(elementId, lat, lng, title); });
        }
    },

    _render: function (elementId, lat, lng, title) {
        var el = document.getElementById(elementId);
        if (!el || this._instances[elementId]) return;

        var map = new google.maps.Map(el, {
            center: { lat: lat, lng: lng },
            zoom: 15,
            disableDefaultUI: true,
            zoomControl: true,
            backgroundColor: '#0d1117',
            styles: [
                { featureType: 'all', elementType: 'all', stylers: [{ visibility: 'off' }] },
                { featureType: 'landscape', elementType: 'all', stylers: [{ color: '#0d1117' }, { visibility: 'on' }] },
                { featureType: 'water', elementType: 'all', stylers: [{ visibility: 'on' }, { color: '#161b22' }] },
                { featureType: 'road', elementType: 'geometry', stylers: [{ visibility: 'on' }, { color: '#21262d' }] },
                { featureType: 'road.arterial', elementType: 'geometry', stylers: [{ color: '#2d333b' }] },
                { featureType: 'road.highway', elementType: 'geometry', stylers: [{ color: '#388bfd', visibility: 'on' }] },
                { featureType: 'poi', elementType: 'all', stylers: [{ visibility: 'off' }] },
                { featureType: 'transit', elementType: 'all', stylers: [{ visibility: 'off' }] },
                { featureType: 'administrative', elementType: 'geometry.stroke', stylers: [{ visibility: 'on' }, { color: '#30363d' }] },
                { featureType: 'administrative.country', elementType: 'geometry.stroke', stylers: [{ visibility: 'on' }, { color: '#dc3545' }, { weight: 1.5 }] }
            ]
        });

        new google.maps.Marker({
            position: { lat: lat, lng: lng },
            map: map,
            title: title || '',
            icon: {
                path: google.maps.SymbolPath.CIRCLE,
                scale: 9,
                fillColor: '#dc3545',
                fillOpacity: 0.9,
                strokeColor: '#ff8080',
                strokeWeight: 2
            }
        });

        this._instances[elementId] = map;

        // Force resize after Blazor render cycle completes
        setTimeout(function () {
            google.maps.event.trigger(map, 'resize');
            map.setCenter({ lat: lat, lng: lng });
        }, 150);
    },

    destroy: function (elementId) {
        delete this._instances[elementId];
    }
};
