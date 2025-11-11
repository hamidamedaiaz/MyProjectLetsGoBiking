using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace RoutingService.Services
{
    public class OpenStreetAPIService
    {
        private static readonly HttpClient client = new HttpClient();

        public OpenStreetAPIService()
        {
            if (client.DefaultRequestHeaders.UserAgent.Count == 0)
            {
                client.DefaultRequestHeaders.Add("User-Agent", "RoutingService/1.0");
            }
        }

        public async Task<string> ReverseGeocode(double latitude, double longitude)
        {
            try
            {
                string latFormatted = latitude.ToString(System.Globalization.CultureInfo.InvariantCulture);
                string lonFormatted = longitude.ToString(System.Globalization.CultureInfo.InvariantCulture);

                string url = $"https://nominatim.openstreetmap.org/reverse?lat={latFormatted}&lon={lonFormatted}&format=json";

                HttpResponseMessage response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();

                string json = await response.Content.ReadAsStringAsync();
                var jsonResponse = JsonConvert.DeserializeObject<dynamic>(json);

                string city = jsonResponse?.address?.city ??
                              jsonResponse?.address?.municipality ??
                              jsonResponse?.address?.town ??
                              jsonResponse?.address?.village;

                if (string.IsNullOrEmpty(city))
                {
                    throw new Exception("Ville non trouvée dans la réponse de géocodage inverse");
                }

                return city;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur ReverseGeocode : {ex.Message}");
                throw;
            }
        }
    }
}