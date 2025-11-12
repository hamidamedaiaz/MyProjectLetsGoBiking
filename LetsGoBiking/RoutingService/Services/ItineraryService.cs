using System;
using System.Linq;
using System.ServiceModel;
using System.Threading.Tasks;
using RoutingService.Models;
using Newtonsoft.Json;
using RoutingService.ServiceReference1;

namespace RoutingService.Services
{
    public class ItineraryService
    {
        private readonly IProxyCache _proxyCacheClient;
        private readonly OpenRouteAPIService _openRouteService;
        private readonly OpenStreetAPIService _openStreetService;

        public ItineraryService()
        {
            var binding = new BasicHttpBinding
            {
                MaxReceivedMessageSize = 2147483647,
                MaxBufferSize = 2147483647,
                ReaderQuotas = System.Xml.XmlDictionaryReaderQuotas.Max
            };

            var endpoint = new EndpointAddress("http://localhost:8081/ProxyCache");
            _proxyCacheClient = new ProxyCacheClient(binding, endpoint);

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

                string originCity = await _openStreetService.ReverseGeocode(originLat, originLon);
                string destinationCity = await _openStreetService.ReverseGeocode(destinationLat, destinationLon);

                Console.WriteLine($"   Ville origine : {originCity}");
                Console.WriteLine($"   Ville destination : {destinationCity}");
                Console.WriteLine();

                Console.WriteLine("🚲 Étape 2 : Récupération des stations JCDecaux via ProxyCache...");

                var originStations = await _proxyCacheClient.GetStationsAsync(originCity);
                var destinationStations = await _proxyCacheClient.GetStationsAsync(destinationCity);

                Console.WriteLine($"   Stations à l'origine : {originStations?.Length ?? 0}");
                Console.WriteLine($"   Stations à la destination : {destinationStations?.Length ?? 0}");
                Console.WriteLine();

                // ✅ Conversion des stations SOAP vers le modèle local
                var originStationsList = ConvertSoapStationsToLocalModel(originStations);
                var destinationStationsList = ConvertSoapStationsToLocalModel(destinationStations);

                Console.WriteLine("📍 Étape 3 : Recherche des stations les plus proches...");

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

                double walkingTime = await GetWalkingTime(originLat, originLon, destinationLat, destinationLon);
                double walkingDistance = await GetWalkingDistance(originLat, originLon, destinationLat, destinationLon);

                Console.WriteLine($"   Marche directe : {walkingDistance}m en {walkingTime} min");

                if (closestOriginStation != null && closestDestinationStation != null && useBike)
                {
                    Console.WriteLine("   Calcul itinéraire avec vélo...");

                    string originToStationItinerary = await _openRouteService.ComputeItinerary(
                        originLat, originLon,
                        closestOriginStation.Latitude, closestOriginStation.Longitude,
                        false
                    );

                    string stationToStationItinerary = await _openRouteService.ComputeItinerary(
                        closestOriginStation.Latitude, closestOriginStation.Longitude,
                        closestDestinationStation.Latitude, closestDestinationStation.Longitude,
                        true
                    );

                    string stationToDestinationItinerary = await _openRouteService.ComputeItinerary(
                        closestDestinationStation.Latitude, closestDestinationStation.Longitude,
                        destinationLat, destinationLon,
                        false
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
                        return Newtonsoft.Json.JsonConvert.SerializeObject(new
                        {
                            UseBike = true,
                            ClosestOriginStation = closestOriginStation,
                            ClosestDestinationStation = closestDestinationStation,
                            Itinerary = new
                            {
                                // ✅ Désérialiser les strings JSON en objets
                                OriginToStation = Newtonsoft.Json.JsonConvert.DeserializeObject(originToStationItinerary),
                                StationToStation = Newtonsoft.Json.JsonConvert.DeserializeObject(stationToStationItinerary),
                                StationToDestination = Newtonsoft.Json.JsonConvert.DeserializeObject(stationToDestinationItinerary)
                            },
                            PreferredOption = preferredOption
                        });
                    }
                    
                    else
                    {
                        string direct = await _openRouteService.ComputeItinerary(originLat, originLon, destinationLat, destinationLon, false);
                        return Newtonsoft.Json.JsonConvert.SerializeObject(new
                        {
                            UseBike = false,
                            ClosestOriginStation = (Models.BikeStation)null,
                            ClosestDestinationStation = (Models.BikeStation)null,
                            Itinerary = Newtonsoft.Json.JsonConvert.DeserializeObject(direct), // ✅ Objet JSON
                            PreferredOption = preferredOption
                        });
                    }
                }

                Console.WriteLine("   Itinéraire marche uniquement");
                string directItinerary = await _openRouteService.ComputeItinerary(originLat, originLon, destinationLat, destinationLon, useBike);
                return JsonConvert.SerializeObject(new
                {
                    UseBike = useBike,
                    ClosestOriginStation = (Models.BikeStation)null,
                    ClosestDestinationStation = (Models.BikeStation)null,
                    Itinerary = Newtonsoft.Json.JsonConvert.DeserializeObject(directItinerary)
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

        /// <summary>
        /// ✅ Méthode helper pour convertir les stations SOAP vers le modèle local
        /// </summary>
        private System.Collections.Generic.List<Models.BikeStation> ConvertSoapStationsToLocalModel(ServiceReference1.BikeStation[] soapStations)
        {
            if (soapStations == null || soapStations.Length == 0)
                return new System.Collections.Generic.List<Models.BikeStation>();

            return soapStations.Select(s => new Models.BikeStation
            {
                Name = s.Namek__BackingField,
                AvailableBikes = s.AvailableBikesk__BackingField,
                BikeStands = s.BikeStandsk__BackingField,
                Latitude = s.Latitudek__BackingField,
                Longitude = s.Longitudek__BackingField
            }).ToList();
        }

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