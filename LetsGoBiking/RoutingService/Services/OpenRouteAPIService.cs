using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace RoutingService.Services
{
    public class OpenRouteAPIService
    {
        private const string ApiKey = "5b3ce3597851110001cf6248b57a555dface41ab9054282972f1ccd3";

        public async Task<string> ComputeItinerary(double originLat, double originLon, double destinationLat, double destinationLon, bool useBike)
        {
            string profile = useBike ? "cycling-regular" : "foot-walking";
            string url = $"https://api.openrouteservice.org/v2/directions/{profile}?language=fr";

            var payload = new
            {
                coordinates = new[]
                {
                    new[] { originLon, originLat },
                    new[] { destinationLon, destinationLat }
                }
            };

            try
            {
                using (var client = new WebClient())
                {
                    client.Encoding = Encoding.UTF8;
                    client.Headers.Add("Authorization", ApiKey);
                    client.Headers.Add("Content-Type", "application/json; charset=utf-8");
                    client.Headers.Add("User-Agent", "RoutingService/1.0");

                    string payloadJson = JsonConvert.SerializeObject(payload, new JsonSerializerSettings
                    {
                        StringEscapeHandling = StringEscapeHandling.EscapeNonAscii
                    });

                    string response = await client.UploadStringTaskAsync(url, "POST", payloadJson);
                    JObject jsonResponse = JObject.Parse(response);
                    return jsonResponse.ToString();
                }
            }
            catch (WebException webEx)
            {
                Console.WriteLine($"Erreur OpenRoute : {webEx.Message}");
                if (webEx.Response != null)
                {
                    using (var reader = new StreamReader(webEx.Response.GetResponseStream()))
                    {
                        string errorResponse = await reader.ReadToEndAsync();
                        Console.WriteLine($"Réponse erreur : {errorResponse}");
                    }
                }
                throw new Exception("Erreur OpenRouteService");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur inattendue : {ex.Message}");
                throw;
            }
        }
    }
}