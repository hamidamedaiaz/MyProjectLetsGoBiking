using System;
using System.Threading.Tasks;
using System.Web.Http;
using RoutingService.Services;

namespace RoutingService.Controllers
{
    /// <summary>
    /// Contrôleur REST pour le calcul d'itinéraires
    /// </summary>
    public class ItineraryController : ApiController
    {
        private readonly ItineraryService _itineraryService;

        public ItineraryController()
        {
            // Initialisation du service avec le client SOAP ProxyCache
            _itineraryService = new ItineraryService();
        }

        /// <summary>
        /// Endpoint REST pour calculer un itinéraire
        /// GET http://localhost:8080/itinerary/compute?originLat=43.7&originLon=7.25&destLat=43.71&destLon=7.26&useBike=true
        /// </summary>
        [HttpGet]
        [Route("itinerary/compute")]
        public async Task<IHttpActionResult> Compute(
            double originLat,
            double originLon,
            double destLat,
            double destLon,
            bool useBike = true)
        {
            try
            {
                Console.WriteLine();
                Console.WriteLine("═══════════════════════════════════════");
                Console.WriteLine("📥 NOUVELLE REQUÊTE REÇUE");
                Console.WriteLine("═══════════════════════════════════════");
                Console.WriteLine($"📍 Origine      : ({originLat}, {originLon})");
                Console.WriteLine($"📍 Destination  : ({destLat}, {destLon})");
                Console.WriteLine($"🚲 Utiliser vélo : {useBike}");
                Console.WriteLine();

                // Validation des paramètres
                if (!IsValidCoordinate(originLat, originLon))
                {
                    Console.WriteLine("❌ Coordonnées d'origine invalides");
                    return BadRequest("Coordonnées d'origine invalides");
                }

                if (!IsValidCoordinate(destLat, destLon))
                {
                    Console.WriteLine("❌ Coordonnées de destination invalides");
                    return BadRequest("Coordonnées de destination invalides");
                }

                // Appel au service métier
                var result = await _itineraryService.ComputeItinerary(
                    originLat, originLon, destLat, destLon, useBike
                );

                Console.WriteLine("✅ Itinéraire calculé avec succès");
                Console.WriteLine("═══════════════════════════════════════");
                Console.WriteLine();

                return Ok(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Console.WriteLine("❌ ERREUR dans le contrôleur :");
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
                Console.WriteLine();

                return InternalServerError(new Exception(
                    "Erreur lors du calcul de l'itinéraire. Vérifiez les logs du serveur.",
                    ex
                ));
            }
        }

        /// <summary>
        /// Endpoint de test pour vérifier que le service fonctionne
        /// GET http://localhost:8080/itinerary/ping
        /// </summary>
        [HttpGet]
        [Route("itinerary/ping")]
        public IHttpActionResult Ping()
        {
            return Ok(new
            {
                status = "online",
                service = "RoutingService",
                timestamp = DateTime.Now,
                endpoints = new[]
                {
                    "GET /itinerary/compute?originLat=&originLon=&destLat=&destLon=&useBike=",
                    "GET /itinerary/ping"
                }
            });
        }

        /// <summary>
        /// Validation des coordonnées GPS
        /// </summary>
        private bool IsValidCoordinate(double lat, double lon)
        {
            return lat >= -90 && lat <= 90 && lon >= -180 && lon <= 180;
        }
    }
}