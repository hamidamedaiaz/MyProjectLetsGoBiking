/**
 * API Service - Gère tous les appels au backend
 */

const API_CONFIG = {
    // URL de base du RoutingService
    BASE_URL: 'http://localhost:8080',

    // Endpoints
    ENDPOINTS: {
        ITINERARY: '/itinerary/compute',
        PING: '/itinerary/ping'
    }
};

class APIService {
    /**
     * Calcule un itinéraire
     */
    static async calculateItinerary(originLat, originLon, destLat, destLon, useBike = true) {
        const url = new URL(API_CONFIG.BASE_URL + API_CONFIG.ENDPOINTS.ITINERARY);

        // Paramètres de la requête
        url.searchParams.append('originLat', originLat);
        url.searchParams.append('originLon', originLon);
        url.searchParams.append('destLat', destLat);
        url.searchParams.append('destLon', destLon);
        url.searchParams.append('useBike', useBike);

        try {
            console.log('🌐 Appel API:', url.toString());

            const response = await fetch(url.toString(), {
                method: 'GET',
                headers: {
                    'Accept': 'application/json'
                }
            });

            if (!response.ok) {
                throw new Error(`Erreur HTTP ${response.status}: ${response.statusText}`);
            }

            const data = await response.json();
            console.log('✅ Réponse API:', data);

            return data;
        } catch (error) {
            console.error('❌ Erreur API:', error);
            throw new Error(`Impossible de calculer l'itinéraire: ${error.message}`);
        }
    }

    /**
     * Récupère des suggestions d'adresses via Nominatim
     */
    static async getSuggestions(query) {
        if (!query || query.length < 3) {
            return [];
        }

        const url = `https://nominatim.openstreetmap.org/search?` +
            `q=${encodeURIComponent(query)}&` +
            `format=json&` +
            `limit=5&` +
            `countrycodes=fr&` +
            `addressdetails=1`;

        try {
            const response = await fetch(url, {
                headers: {
                    'User-Agent': 'LetsGoBiking/1.0'
                }
            });

            if (!response.ok) {
                throw new Error('Erreur lors de la récupération des suggestions');
            }

            const data = await response.json();

            return data.map(item => ({
                displayName: item.display_name,
                lat: parseFloat(item.lat),
                lon: parseFloat(item.lon)
            }));
        } catch (error) {
            console.error('Erreur suggestions:', error);
            return [];
        }
    }

    /**
     * Test de connectivité avec le backend
     */
    static async testConnection() {
        try {
            const response = await fetch(API_CONFIG.BASE_URL + API_CONFIG.ENDPOINTS.PING);
            const data = await response.json();
            console.log('✅ Backend connecté:', data);
            return true;
        } catch (error) {
            console.error('❌ Backend non accessible:', error);
            return false;
        }
    }
}

// Export pour utilisation dans d'autres fichiers
window.APIService = APIService;