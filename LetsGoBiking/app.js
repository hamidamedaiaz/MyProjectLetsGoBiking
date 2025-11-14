/**
 * Application principale
 */

class App {
    constructor() {
        this.mapService = new MapService();
        this.originCoords = null;
        this.destinationCoords = null;
        this.selectedMode = 'bike';
        this.suggestionTimeout = null;

        this.init();
    }

    /**
     * Initialisation
     */
    async init() {
        console.log('?? Démarrage de l\'application...');

        // Initialiser la carte
        this.mapService.initMap();

        // Tester la connexion au backend
        const isConnected = await APIService.testConnection();
        if (!isConnected) {
            this.showError('?? Backend non accessible. Assurez-vous que RoutingService est démarré sur le port 8080.');
        }

        // Initialiser les événements
        this.initEvents();

        console.log('? Application prête');
    }

    /**
     * Initialise tous les événements
     */
    initEvents() {
        // Inputs
        const originInput = document.getElementById('originInput');
        const destinationInput = document.getElementById('destinationInput');

        originInput.addEventListener('input', (e) => this.handleInputChange(e, 'origin'));
        destinationInput.addEventListener('input', (e) => this.handleInputChange(e, 'destination'));

        // Clear buttons
        document.getElementById('clearOrigin').addEventListener('click', () => this.clearInput('origin'));
        document.getElementById('clearDestination').addEventListener('click', () => this.clearInput('destination'));

        // Mode buttons
        document.querySelectorAll('.mode-btn').forEach(btn => {
            btn.addEventListener('click', (e) => this.handleModeChange(e));
        });

        // Calculate button
        document.getElementById('calculateBtn').addEventListener('click', () => this.calculateRoute());

        // Close results
        document.getElementById('closeResults').addEventListener('click', () => this.closeResults());

        // Sidebar toggle (mobile)
        document.getElementById('toggleSidebar').addEventListener('click', () => this.toggleSidebar());
        document.getElementById('openSidebar').addEventListener('click', () => this.toggleSidebar());

        // Cacher suggestions au clic ailleurs
        document.addEventListener('click', (e) => {
            if (!e.target.closest('.input-group')) {
                this.hideSuggestions('origin');
                this.hideSuggestions('destination');
            }
        });
    }

    /**
     * Gère le changement d'input (autocomplétion)
     */
    handleInputChange(event, type) {
        const value = event.target.value;
        const clearBtn = document.getElementById(type === 'origin' ? 'clearOrigin' : 'clearDestination');

        // Afficher/cacher bouton clear
        clearBtn.style.display = value ? 'block' : 'none';

        // Réinitialiser les coordonnées si l'utilisateur modifie
        if (type === 'origin') {
            this.originCoords = null;
        } else {
            this.destinationCoords = null;
        }

        this.updateCalculateButton();

        // Autocomplétion avec délai (debounce)
        clearTimeout(this.suggestionTimeout);

        if (value.length >= 3) {
            this.suggestionTimeout = setTimeout(() => {
                this.fetchSuggestions(value, type);
            }, 300);
        } else {
            this.hideSuggestions(type);
        }
    }

    /**
     * Récupère les suggestions
     */
    async fetchSuggestions(query, type) {
        try {
            const suggestions = await APIService.getSuggestions(query);
            this.displaySuggestions(suggestions, type);
        } catch (error) {
            console.error('Erreur suggestions:', error);
        }
    }

    /**
     * Affiche les suggestions
     */
    displaySuggestions(suggestions, type) {
        const boxId = type === 'origin' ? 'originSuggestions' : 'destinationSuggestions';
        const box = document.getElementById(boxId);

        box.innerHTML = '';

        if (suggestions.length === 0) {
            box.classList.remove('active');
            return;
        }

        suggestions.forEach(suggestion => {
            const item = document.createElement('div');
            item.className = 'suggestion-item';
            item.innerHTML = `<i class="fas fa-map-marker-alt"></i> ${suggestion.displayName}`;

            item.addEventListener('click', () => {
                this.selectSuggestion(suggestion, type);
            });

            box.appendChild(item);
        });

        box.classList.add('active');
    }

    /**
     * Sélectionne une suggestion
     */
    selectSuggestion(suggestion, type) {
        const input = document.getElementById(type === 'origin' ? 'originInput' : 'destinationInput');
        input.value = suggestion.displayName;

        // Stocker les coordonnées
        if (type === 'origin') {
            this.originCoords = { lat: suggestion.lat, lon: suggestion.lon, name: suggestion.displayName };
            this.mapService.addOriginMarker(suggestion.lat, suggestion.lon, suggestion.displayName);
            this.mapService.centerMap(suggestion.lat, suggestion.lon);
        } else {
            this.destinationCoords = { lat: suggestion.lat, lon: suggestion.lon, name: suggestion.displayName };
            this.mapService.addDestinationMarker(suggestion.lat, suggestion.lon, suggestion.displayName);
            this.mapService.centerMap(suggestion.lat, suggestion.lon);
        }

        this.hideSuggestions(type);
        this.updateCalculateButton();

        console.log(`? ${type} sélectionné:`, suggestion.displayName);
    }

    /**
     * Cache les suggestions
     */
    hideSuggestions(type) {
        const boxId = type === 'origin' ? 'originSuggestions' : 'destinationSuggestions';
        const box = document.getElementById(boxId);
        box.classList.remove('active');
    }

    /**
     * Efface un input
     */
    clearInput(type) {
        const input = document.getElementById(type === 'origin' ? 'originInput' : 'destinationInput');
        const clearBtn = document.getElementById(type === 'origin' ? 'clearOrigin' : 'clearDestination');

        input.value = '';
        clearBtn.style.display = 'none';

        if (type === 'origin') {
            this.originCoords = null;
            if (this.mapService.markers.origin) {
                this.mapService.map.removeLayer(this.mapService.markers.origin);
                this.mapService.markers.origin = null;
            }
        } else {
            this.destinationCoords = null;
            if (this.mapService.markers.destination) {
                this.mapService.map.removeLayer(this.mapService.markers.destination);
                this.mapService.markers.destination = null;
            }
        }

        this.hideSuggestions(type);
        this.updateCalculateButton();
    }

    /**
     * Gère le changement de mode
     */
    handleModeChange(event) {
        const btn = event.currentTarget;
        const mode = btn.dataset.mode;

        // Update UI
        document.querySelectorAll('.mode-btn').forEach(b => b.classList.remove('active'));
        btn.classList.add('active');

        this.selectedMode = mode;

        console.log('?? Mode changé:', mode);
    }

    /**
     * Active/désactive le bouton de calcul
     */
    updateCalculateButton() {
        const btn = document.getElementById('calculateBtn');
        btn.disabled = !(this.originCoords && this.destinationCoords);
    }

    /**
     * Calcule l'itinéraire
     */
    async calculateRoute() {
        if (!this.originCoords || !this.destinationCoords) {
            return;
        }

        console.log('?? Calcul de l\'itinéraire...');

        // Afficher le loading
        this.showLoading();
        this.hideError();
        this.hideResults();

        try {
            const useBike = this.selectedMode === 'bike';

            const result = await APIService.calculateItinerary(
                this.originCoords.lat,
                this.originCoords.lon,
                this.destinationCoords.lat,
                this.destinationCoords.lon,
                useBike
            );

            console.log('? Itinéraire calculé:', result);

            // Afficher les résultats
            this.displayResults(result);

            // Afficher sur la carte
            this.mapService.displayItinerary(result);

        } catch (error) {
            console.error('? Erreur:', error);
            this.showError(error.message);
        } finally {
            this.hideLoading();
        }
    }

    /**
     * Affiche les résultats
     */
    displayResults(data) {
        console.log('🎨 displayResults appelé avec:', data);
        
        const resultsDiv = document.getElementById('results');
        const summaryDiv = document.getElementById('summary');
        const stationsDiv = document.getElementById('stationsInfo');
        const stepsDiv = document.getElementById('steps');

        // Summary
        let totalDistance = 0;
        let totalDuration = 0;

        if (data.UseBike && data.Itinerary && data.Itinerary.OriginToStation) {
            console.log('✅ Mode VÉLO détecté');
            console.log('📍 Station origine:', data.ClosestOriginStation);
            console.log('📍 Station destination:', data.ClosestDestinationStation);
            
            // Vélo : sommer les 3 trajets
            totalDistance = this.sumDistance([
                data.Itinerary.OriginToStation,
                data.Itinerary.StationToStation,
                data.Itinerary.StationToDestination
            ]);
            totalDuration = this.sumDuration([
                data.Itinerary.OriginToStation,
                data.Itinerary.StationToStation,
                data.Itinerary.StationToDestination
            ]);
        } else {
            console.log('✅ Mode MARCHE détecté');
            console.log('📍 Itinéraire:', data.Itinerary);
            
            // Marche : 1 seul trajet
            if (data.Itinerary && data.Itinerary.routes) {
                const route = data.Itinerary.routes[0];
                totalDistance = route.summary.distance;
                totalDuration = route.summary.duration;
            } else {
                console.error('❌ Pas de routes dans data.Itinerary:', data.Itinerary);
            }
        }

        console.log(`📏 Distance totale: ${totalDistance}m, Durée: ${totalDuration}s`);

        summaryDiv.innerHTML = `
            <div class="summary-item">
                <i class="fas fa-route"></i>
                <div>
                    <strong>Distance :</strong>
                    <span>${(totalDistance / 1000).toFixed(2)} km</span>
                </div>
            </div>
            <div class="summary-item">
                <i class="fas fa-clock"></i>
                <div>
                    <strong>Durée :</strong>
                    <span>${Math.round(totalDuration / 60)} minutes</span>
                </div>
            </div>
            <div class="summary-item">
                <i class="fas ${data.UseBike ? 'fa-bicycle' : 'fa-walking'}"></i>
                <div>
                    <strong>Mode :</strong>
                    <span>${data.UseBike ? 'Vélo + Marche' : 'À pied'}</span>
                </div>
            </div>
            ${data.PreferredOption ? `
                <div class="recommendation ${data.UseBike ? '' : 'walking'}">
                    <p><i class="fas fa-lightbulb"></i> ${data.PreferredOption}</p>
                </div>
            ` : ''}
        `;

        // Stations (si vélo)
        if (data.UseBike && data.ClosestOriginStation && data.ClosestDestinationStation) {
            console.log('🚲 Affichage des stations');
            stationsDiv.style.display = 'block';
            stationsDiv.innerHTML = `
                <h4><i class="fas fa-bicycle"></i> Stations de vélo</h4>
                <div class="station-card">
                    <h4>📍 Station de départ</h4>
                    <p>${data.ClosestOriginStation.Name}</p>
                    <div class="bikes-info">
                        <span><i class="fas fa-bicycle"></i> ${data.ClosestOriginStation.AvailableBikes} vélos</span>
                        <span><i class="fas fa-parking"></i> ${data.ClosestOriginStation.BikeStands - data.ClosestOriginStation.AvailableBikes} places</span>
                    </div>
                </div>
                <div class="station-card">
                    <h4>🎯 Station d'arrivée</h4>
                    <p>${data.ClosestDestinationStation.Name}</p>
                    <div class="bikes-info">
                        <span><i class="fas fa-bicycle"></i> ${data.ClosestDestinationStation.AvailableBikes} vélos</span>
                        <span><i class="fas fa-parking"></i> ${data.ClosestDestinationStation.BikeStands - data.ClosestDestinationStation.AvailableBikes} places</span>
                    </div>
                </div>
            `;
        } else {
            console.log('⚠️ Pas de stations à afficher');
            stationsDiv.style.display = 'none';
        }

        // Steps
        stepsDiv.innerHTML = '<h4>📋 Instructions</h4>';

        if (data.UseBike && data.Itinerary.OriginToStation) {
            console.log('📝 Affichage des 3 étapes (vélo)');
            // 3 étapes
            this.addStepsFromRoute(stepsDiv, data.Itinerary.OriginToStation, 'walk', '1️⃣ Marche vers la station');
            this.addStepsFromRoute(stepsDiv, data.Itinerary.StationToStation, 'bike', '2️⃣ À vélo');
            this.addStepsFromRoute(stepsDiv, data.Itinerary.StationToDestination, 'walk', '3️⃣ Marche vers la destination');
        } else {
            console.log('📝 Affichage de 1 étape (marche)');
            // 1 étape
            this.addStepsFromRoute(stepsDiv, data.Itinerary, 'walk', 'À pied');
        }

        resultsDiv.style.display = 'block';
    }

    /**
     * Ajoute les étapes d'une route
     */
    addStepsFromRoute(container, routeData, type, title) {
        console.log('📋 addStepsFromRoute:', { title, routeData, type });
        
        if (!routeData) {
            console.warn('⚠️ routeData est null/undefined');
            return;
        }
        
        if (!routeData.routes) {
            console.warn('⚠️ Pas de propriété routes dans routeData:', routeData);
            return;
        }

        const route = routeData.routes[0];
        if (!route) {
            console.warn('⚠️ routes[0] est vide');
            return;
        }

        if (!route.segments || route.segments.length === 0) {
            console.warn('⚠️ Pas de segments dans la route');
            return;
        }

        const segment = route.segments[0];
        
        if (!segment.steps || segment.steps.length === 0) {
            console.warn('⚠️ Pas de steps dans le segment');
            return;
        }

        console.log(`✅ ${segment.steps.length} étapes trouvées pour "${title}"`);

        const titleDiv = document.createElement('h5');
        titleDiv.style.marginTop = '1rem';
        titleDiv.style.marginBottom = '0.5rem';
        titleDiv.textContent = title;
        container.appendChild(titleDiv);

        segment.steps.forEach((step, index) => {
            const stepDiv = document.createElement('div');
            stepDiv.className = 'step-item';
            stepDiv.innerHTML = `
                <div class="step-icon ${type}">
                    <i class="fas fa-${type === 'bike' ? 'bicycle' : 'walking'}"></i>
                </div>
                <div class="step-content">
                    <p>${step.instruction || 'Continuez tout droit'}</p>
                    <small>${(step.distance || 0).toFixed(0)} m • ${Math.round((step.duration || 0) / 60)} min</small>
                </div>
            `;
            container.appendChild(stepDiv);
        });
    }

    /**
     * Calcule la distance totale
     */
    sumDistance(routes) {
        return routes.reduce((sum, route) => {
            if (route && route.routes) {
                return sum + route.routes[0].summary.distance;
            }
            return sum;
        }, 0);
    }

    /**
     * Calcule la durée totale
     */
    sumDuration(routes) {
        return routes.reduce((sum, route) => {
            if (route && route.routes) {
                return sum + route.routes[0].summary.duration;
            }
            return sum;
        }, 0);
    }

    /**
     * Affiche le loading
     */
    showLoading() {
        document.getElementById('loading').style.display = 'block';
    }

    /**
     * Cache le loading
     */
    hideLoading() {
        document.getElementById('loading').style.display = 'none';
    }

    /**
     * Affiche une erreur
     */
    showError(message) {
        const errorDiv = document.getElementById('errorMessage');
        const errorText = document.getElementById('errorText');
        errorText.textContent = message;
        errorDiv.style.display = 'block';
    }

    /**
     * Cache l'erreur
     */
    hideError() {
        document.getElementById('errorMessage').style.display = 'none';
    }

    /**
     * Cache les résultats
     */
    hideResults() {
        document.getElementById('results').style.display = 'none';
    }

    /**
     * Ferme les résultats
     */
    closeResults() {
        this.hideResults();
        this.mapService.clearRoutes();
    }

    /**
     * Toggle sidebar (mobile)
     */
    toggleSidebar() {
        document.getElementById('sidebar').classList.toggle('active');
    }
}

// Démarrer l'application quand le DOM est prêt
document.addEventListener('DOMContentLoaded', () => {
    window.app = new App();
});