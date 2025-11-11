using System;
using System.Linq;
using System.Threading.Tasks;
using RoutingService.Models;
using RoutingService.ProxyCacheReference; // Client SOAP généré
using Newtonsoft.Json;

namespace RoutingService.Services
{
    /// <summary>
    /// Service contenant la logique métier pour le calcul d'itinéraires
    /// </summary>
    public class ItineraryService
    {
        // Client SOAP pour appeler le ProxyCache (JCDecaux uniquement)
        private readonly ProxyCacheClient _proxyCacheClient;

        // Services pour appels REST directs
        private readonly OpenRouteAPIService _openRouteService;
        private readonly OpenStreetAPIService _openStreetService;

        public ItineraryService()
        {
            // ✅ MODIFICATION : Initialisation du client SOAP
            _proxyCacheClient = new ProxyCacheClient();

            // Services REST directs
            _openRouteService = new OpenRouteAPIService();
            _openStreetService = new OpenStreetAPIService();
        }

        /// <summary>
        /// Calcule l'itinéraire optimal (marche vs vélo)
        /// </summary>
        public async Task<string> ComputeItinerary(
            double originLat,
            double originLon,
            double destinationLat,
            double destinationLon,
            bool useBike)
        {
            try
            {
                Console.WriteLine("🔍 Étape 1 : Géocodage inverse...");

                // ✅ Appel DIRECT à OpenStreet (pas via proxy)
                string originCity = await _openStreetService.ReverseGeocode(originLat, originLon);
                string destinationCity = await _openStreetService.ReverseGeocode(destinationLat, destinationLon);

                Console.WriteLine($"   Ville origine : {originCity}");
                Console.WriteLine($"   Ville destination : {destinationCity}");
                Console.WriteLine();

                Console.WriteLine("🚲 Étape 2 : Récupération des stations JCDecaux...");

                // ✅ MODIFICATION : Appel SOAP au ProxyCache pour JCDecaux
                var originStations = await _proxyCacheClient.GetStationsAsync(originCity);
                var destinationStations = await _proxyCacheClient.GetStationsAsync(destinationCity);

                Console.WriteLine($"   Stations à l'origine : {originStations.Length}");
                Console.WriteLine($"   Stations à la destination : {destinationStations.Length}");
                Console.WriteLine();

                // Convertir les stations du client SOAP en modèle local
                var originStationsList = originStations.Select(s => new BikeStation
                {
                    Name = s.Name,
                    AvailableBikes = s.AvailableBikes,
                    BikeStands = s.BikeStands,
                    Latitude = s.Latitude,
                    Longitude = s.Longitude
                }).ToList();

                var destinationStationsList = destinationStations.Select(s => new BikeStation
                {
                    Name = s.Name,
                    AvailableBikes = s.AvailableBikes,
                    BikeStands = s.BikeStands,
                    Latitude = s.Latitude,
                    Longitude = s.Longitude
                }).ToList();

                Console.WriteLine("📍 Étape 3 : Recherche des stations les plus proches...");

                // Trouver les stations les plus proches
                var closestOriginStation = originStationsList
                    .Where(station => station.AvailableBikes > 0)
                    .OrderBy(station => HaversineDistance(originLat, originLon, station.Latitude, station.Longitude))
                    .FirstOrDefault();

                var closestDestinationStation = destinationStationsList
                    .Where(station => station.BikeStands > station.AvailableBikes)
                    .OrderBy(station => HaversineDistance(destinationLat, destinationLon, station.Latitude, station.Longitude))
                    .FirstOrDefault();

                if (closestOriginStation != null)
                    Console.WriteLine($"   Station origine : {closestOriginStation.Name}");
                if (closestDestinationStation != null)
                    Console.WriteLine($"   Station destination : {closestDestinationStation.Name}");
                Console.WriteLine();

                Console.WriteLine("🗺️  Étape 4 : Calcul des itinéraires...");

                // ✅ Appel DIRECT à OpenRoute (pas via proxy)
                double walkingTime = await GetWalkingTime(originLat, originLon, destinationLat, destinationLon);
                double walkingDistance = await GetWalkingDistance(originLat, originLon, destinationLat, destinationLon);

                Console.WriteLine($"   Marche directe : {walkingDistance}m en {walkingTime} min");

                // Si stations disponibles et vélo demandé
                if (closestOriginStation != null && closestDestinationStation != null && useBike)
                {
                    Console.WriteLine("   Calcul itinéraire avec vélo...");

                    // Segments de l'itinéraire vélo
                    string originToStationItinerary = await _openRouteService.ComputeItinerary(
                        originLat, originLon,
                        closestOriginStation.Latitude, closestOriginStation.Longitude,
                        false // marche
                    );

                    string stationToStationItinerary = await _openRouteService.ComputeItinerary(
                        closestOriginStation.Latitude, closestOriginStation.Longitude,
                        closestDestinationStation.Latitude, closestDestinationStation.Longitude,
                        true // vélo
                    );

                    string stationToDestinationItinerary = await _openRouteService.ComputeItinerary(
                        closestDestinationStation.Latitude, closestDestinationStation.Longitude,
                        destinationLat, destinationLon,
                        false // marche
                    );

                    double bikeTime = CalculateTotalBikeTime(stationToStationItinerary, originToStationItinerary, stationToDestinationItinerary);
                    double bikeDistance = CalculateTotalBikeDistance(stationToStationItinerary, originToStationItinerary, stationToDestinationItinerary);

                    Console.WriteLine($"   Avec vélo : {bikeDistance}m en {bikeTime} min");
                    Console.WriteLine();

                    string preferredOption = walkingTime < bikeTime
                        ? $"Walking is better with a total time of {walkingTime:F1} minutes and a distance of {walkingDistance:F0} meters, compared to a bike time of {bikeTime:F1} minutes and a bike distance of {bikeDistance:F0} meters."
                        : $"Bike is better with a total time of {bikeTime:F1} minutes and a bike distance of {bikeDistance:F0} meters, compared to a walking time of {walkingTime:F1} minutes and a walking distance of {walkingDistance:F0} meters.";

                    Console.WriteLine($"💡 Décision : {(walkingTime < bikeTime ? "Marche" : "Vélo")} recommandé");

                    if (walkingTime > bikeTime)
                    {
                        // Vélo est meilleur
                        return JsonConvert.SerializeObject(new
                        {
                            UseBike = true,
                            ClosestOriginStation = closestOriginStation,
                            ClosestDestinationStation = closestDestinationStation,
                            Itinerary = new
                            {
                                OriginToStation = originToStationItinerary,
                                StationToStation = stationToStationItinerary,
                                StationToDestination = stationToDestinationItinerary
                            },
                            PreferredOption = preferredOption
                        });
                    }
                    else
                    {
                        // Marche est meilleure
                        string direct = await _openRouteService.ComputeItinerary(originLat, originLon, destinationLat, destinationLon, false);
                        return JsonConvert.SerializeObject(new
                        {
                            UseBike = false,
                            ClosestOriginStation = (BikeStation)null,
                            ClosestDestinationStation = (BikeStation)null,
                            Itinerary = direct,
                            PreferredOption = preferredOption
                        });
                    }
                }

                // Itinéraire direct si pas de stations ou vélo non demandé
                Console.WriteLine("   Itinéraire marche uniquement");
                string directItinerary = await _openRouteService.ComputeItinerary(originLat, originLon, destinationLat, destinationLon, useBike);
                return JsonConvert.SerializeObject(new
                {
                    UseBike = useBike,
                    ClosestOriginStation = (BikeStation)null,
                    ClosestDestinationStation = (BikeStation)null,
                    Itinerary = directItinerary
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erreur dans ComputeItinerary : {ex.Message}");
                return JsonConvert.SerializeObject(new
                {
                    Error = "An unexpected error occurred.",
                    Details = ex.Message
                });
            }
        }

        // ✅ TOUT LE RESTE DU CODE RESTE IDENTIQUE

        private static double HaversineDistance(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371e3;
            double phi1 = lat1 * Math.PI / 180;
            double phi2 = lat2 * Math.PI / 180;
            double deltaPhi = (lat2 - lat1) * Math.PI / 180;
            double deltaLambda = (lon2 - lon1) * Math.PI / 180;

            double a = Math.Sin(deltaPhi / 2) * Math.Sin(deltaPhi / 2) +
                       Math.Cos(phi1) * Math.Cos(phi2) *
                       Math.Sin(deltaLambda / 2) * Math.Sin(deltaLambda / 2);

            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return R * c;
        }

        private async Task<double> GetWalkingTime(double originLat, double originLon, double destinationLat, double destinationLon)
        {
            string walkingItineraryJson = await _openRouteService.ComputeItinerary(
                originLat, originLon, destinationLat, destinationLon, false);
            return CalculateTotalTime(walkingItineraryJson);
        }

        private async Task<double> GetWalkingDistance(double originLat, double originLon, double destinationLat, double destinationLon)
        {
            string walkingItineraryJson = await _openRouteService.ComputeItinerary(
                originLat, originLon, destinationLat, destinationLon, false);
            return CalculateTotalDistance(walkingItineraryJson);
        }

        private static double CalculateTotalDistance(string itineraryJson)
        {
            double totalDistance = 0.0;
            try
            {
                var itinerary = JsonConvert.DeserializeObject<dynamic>(itineraryJson);
                foreach (var route in itinerary.routes)
                {
                    foreach (var segment in route.segments)
                    {
                        totalDistance += (double)segment.distance;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur calcul distance : {ex.Message}");
            }
            return totalDistance;
        }

        private static double CalculateTotalTime(string itineraryJson)
        {
            double totalTime = 0.0;
            try
            {
                var itinerary = JsonConvert.DeserializeObject<dynamic>(itineraryJson);
                foreach (var route in itinerary.routes)
                {
                    foreach (var segment in route.segments)
                    {
                        totalTime += (double)segment.duration / 60.0;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur calcul temps : {ex.Message}");
            }
            return totalTime;
        }

        private static double CalculateTotalBikeDistance(string stationToStation, string originToStation, string stationToDest)
        {
            return CalculateTotalDistance(stationToStation) +
                   CalculateTotalDistance(originToStation) +
                   CalculateTotalDistance(stationToDest);
        }

        private static double CalculateTotalBikeTime(string stationToStation, string originToStation, string stationToDest)
        {
            return CalculateTotalTime(stationToStation) +
                   CalculateTotalTime(originToStation) +
                   CalculateTotalTime(stationToDest);
        }
    }
}