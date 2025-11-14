/**
 * Map Service - Gère la carte Leaflet
 */

class MapService {
    constructor() {
        this.map = null;
        this.markers = {
            origin: null,
            destination: null,
            originStation: null,
            destinationStation: null
        };
        this.polylines = [];
    }

    /**
     * Initialise la carte
     */
    initMap() {
        // Centre sur Lyon par défaut
        this.map = L.map('map').setView([45.764, 4.835], 13);

        // Tuiles OpenStreetMap
        L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
            attribution: '© OpenStreetMap contributors',
            maxZoom: 19
        }).addTo(this.map);

        console.log('? Carte initialisée');
    }

    /**
     * Ajoute un marker origine
     */
    addOriginMarker(lat, lon, name) {
        if (this.markers.origin) {
            this.map.removeLayer(this.markers.origin);
        }

        const icon = L.divIcon({
            className: 'custom-marker origin-marker',
            html: '<i class="fas fa-map-marker-alt" style="color: #3B82F6; font-size: 2rem;"></i>',
            iconSize: [30, 30],
            iconAnchor: [15, 30]
        });

        this.markers.origin = L.marker([lat, lon], { icon })
            .addTo(this.map)
            .bindPopup(`<div class="popup-content">
                <h4>?? Départ</h4>
                <p>${name}</p>
            </div>`);

        console.log('? Marker origine ajouté:', lat, lon);
    }

    /**
     * Ajoute un marker destination
     */
    addDestinationMarker(lat, lon, name) {
        if (this.markers.destination) {
            this.map.removeLayer(this.markers.destination);
        }

        const icon = L.divIcon({
            className: 'custom-marker destination-marker',
            html: '<i class="fas fa-map-marker-alt" style="color: #EF4444; font-size: 2rem;"></i>',
            iconSize: [30, 30],
            iconAnchor: [15, 30]
        });

        this.markers.destination = L.marker([lat, lon], { icon })
            .addTo(this.map)
            .bindPopup(`<div class="popup-content">
                <h4>?? Arrivée</h4>
                <p>${name}</p>
            </div>`);

        console.log('? Marker destination ajouté:', lat, lon);
    }

    /**
     * Ajoute un marker station de vélo
     */
    addStationMarker(lat, lon, name, type, availableBikes, bikeStands) {
        const markerType = type === 'origin' ? 'originStation' : 'destinationStation';

        if (this.markers[markerType]) {
            this.map.removeLayer(this.markers[markerType]);
        }

        const icon = L.divIcon({
            className: 'custom-marker station-marker',
            html: '<i class="fas fa-bicycle" style="color: #10B981; font-size: 1.5rem;"></i>',
            iconSize: [25, 25],
            iconAnchor: [12, 25]
        });

        this.markers[markerType] = L.marker([lat, lon], { icon })
            .addTo(this.map)
            .bindPopup(`<div class="popup-content">
                <h4>?? ${name}</h4>
                <p>Vélos disponibles: <strong>${availableBikes}</strong></p>
                <p>Places libres: <strong>${bikeStands - availableBikes}</strong></p>
            </div>`);

        console.log(`? Marker station ${type} ajouté:`, name);
    }

    /**
     * Dessine une polyline (chemin)
     */
    drawRoute(coordinates, color = '#3B82F6', weight = 5, dashArray = null) {
        const polyline = L.polyline(coordinates, {
            color: color,
            weight: weight,
            opacity: 0.8,
            dashArray: dashArray
        }).addTo(this.map);

        this.polylines.push(polyline);

        console.log('? Route dessinée:', coordinates.length, 'points');

        return polyline;
    }

    /**
     * Extrait les coordonnées depuis une géométrie GeoJSON
     */
    extractCoordinates(geometry) {
        if (!geometry || !geometry.coordinates) {
            return [];
        }

        // OpenRouteService retourne [lon, lat], on doit inverser en [lat, lon]
        return geometry.coordinates.map(coord => [coord[1], coord[0]]);
    }

    /**
     * Affiche l'itinéraire complet sur la carte
     */
    displayItinerary(itineraryData) {
        console.log('🗺️ displayItinerary appelé avec:', itineraryData);
        
        // Nettoyer les anciennes polylines
        this.clearRoutes();

        const { UseBike, Itinerary, ClosestOriginStation, ClosestDestinationStation } = itineraryData;

        if (UseBike && ClosestOriginStation && ClosestDestinationStation) {
            console.log('🚲 Affichage itinéraire VÉLO (3 segments)');
            
            // 1️⃣ Trajet marche : Origine → Station départ (bleu pointillé)
            if (Itinerary.OriginToStation && Itinerary.OriginToStation.routes) {
                console.log('  📍 Segment 1: Origine → Station départ');
                const coords = this.extractCoordinates(Itinerary.OriginToStation.routes[0].geometry);
                console.log(`     ${coords.length} points`);
                this.drawRoute(coords, '#3B82F6', 4, '10, 5');
            } else {
                console.error('  ❌ Pas de OriginToStation');
            }

            // 2️⃣ Trajet vélo : Station départ → Station arrivée (vert)
            if (Itinerary.StationToStation && Itinerary.StationToStation.routes) {
                console.log('  📍 Segment 2: Station → Station (vélo)');
                const coords = this.extractCoordinates(Itinerary.StationToStation.routes[0].geometry);
                console.log(`     ${coords.length} points`);
                this.drawRoute(coords, '#10B981', 6);
            } else {
                console.error('  ❌ Pas de StationToStation');
            }

            // 3️⃣ Trajet marche : Station arrivée → Destination (bleu pointillé)
            if (Itinerary.StationToDestination && Itinerary.StationToDestination.routes) {
                console.log('  📍 Segment 3: Station arrivée → Destination');
                const coords = this.extractCoordinates(Itinerary.StationToDestination.routes[0].geometry);
                console.log(`     ${coords.length} points`);
                this.drawRoute(coords, '#3B82F6', 4, '10, 5');
            } else {
                console.error('  ❌ Pas de StationToDestination');
            }

            // Markers stations
            console.log('  🚲 Ajout markers stations');
            this.addStationMarker(
                ClosestOriginStation.Latitude,
                ClosestOriginStation.Longitude,
                ClosestOriginStation.Name,
                'origin',
                ClosestOriginStation.AvailableBikes,
                ClosestOriginStation.BikeStands
            );

            this.addStationMarker(
                ClosestDestinationStation.Latitude,
                ClosestDestinationStation.Longitude,
                ClosestDestinationStation.Name,
                'destination',
                ClosestDestinationStation.AvailableBikes,
                ClosestDestinationStation.BikeStands
            );

        } else {
            console.log('🚶 Affichage itinéraire MARCHE (1 segment)');
            
            if (Itinerary && Itinerary.routes) {
                const coords = this.extractCoordinates(Itinerary.routes[0].geometry);
                console.log(`  📍 ${coords.length} points dans le trajet`);
                this.drawRoute(coords, '#3B82F6', 5);
            } else {
                console.error('  ❌ Pas de routes dans Itinerary:', Itinerary);
            }
        }

        // Ajuster la vue pour montrer tout l'itinéraire
        this.fitBounds();
        console.log('✅ Affichage terminé');
    }

    /**
     * Ajuste la carte pour afficher tous les markers et routes
     */
    fitBounds() {
        const bounds = L.latLngBounds();

        // Ajouter tous les markers
        Object.values(this.markers).forEach(marker => {
            if (marker) {
                bounds.extend(marker.getLatLng());
            }
        });

        // Ajouter toutes les polylines
        this.polylines.forEach(polyline => {
            bounds.extend(polyline.getBounds());
        });

        if (bounds.isValid()) {
            this.map.fitBounds(bounds, { padding: [50, 50] });
        }
    }

    /**
     * Nettoie toutes les routes
     */
    clearRoutes() {
        this.polylines.forEach(polyline => {
            this.map.removeLayer(polyline);
        });
        this.polylines = [];

        // Supprimer les markers de stations
        if (this.markers.originStation) {
            this.map.removeLayer(this.markers.originStation);
            this.markers.originStation = null;
        }
        if (this.markers.destinationStation) {
            this.map.removeLayer(this.markers.destinationStation);
            this.markers.destinationStation = null;
        }
    }

    /**
     * Nettoie tous les markers et routes
     */
    clearAll() {
        Object.keys(this.markers).forEach(key => {
            if (this.markers[key]) {
                this.map.removeLayer(this.markers[key]);
                this.markers[key] = null;
            }
        });

        this.clearRoutes();
    }

    /**
     * Centre la carte sur une position
     */
    centerMap(lat, lon, zoom = 13) {
        this.map.setView([lat, lon], zoom);
    }
}

// Export
window.MapService = MapService;