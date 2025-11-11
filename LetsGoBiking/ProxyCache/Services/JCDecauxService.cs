using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json;
using ProxyCache.Models;

namespace ProxyCache.Services
{
    /// <summary>
    /// Service pour interagir avec l'API JCDecaux
    /// </summary>
    public class JCDecauxService
    {
        private const string ApiKey = "4ffffd09ca3de7e586d3f46bebbd9a8a7f98191f";

        /// <summary>
        /// Récupère les stations de vélos pour une ville donnée via l'API JCDecaux
        /// </summary>
        public async Task<List<BikeStation>> GetBikeStations(string city)
        {
            string encodedCity = Uri.EscapeDataString(city);
            string url = $"https://api.jcdecaux.com/vls/v1/stations?contract={encodedCity}&apiKey={ApiKey}";

            Console.WriteLine($"🌐 Appel API JCDecaux : {url}");

            using (var client = new WebClient())
            {
                try
                {
                    // Ajout du User-Agent pour éviter les blocages
                    client.Headers.Add("User-Agent", "ProxyCache/1.0");

                    // Récupération des données
                    string json = await client.DownloadStringTaskAsync(url);

                    if (string.IsNullOrWhiteSpace(json))
                    {
                        Console.WriteLine("⚠️  Aucune donnée reçue de l'API JCDecaux");
                        return new List<BikeStation>();
                    }

                    // Désérialisation
                    var stations = JsonConvert.DeserializeObject<List<dynamic>>(json);

                    if (stations == null || stations.Count == 0)
                    {
                        Console.WriteLine("⚠️  Aucune station trouvée");
                        return new List<BikeStation>();
                    }

                    // Mapping vers le modèle BikeStation
                    var result = stations.Select(station => new BikeStation
                    {
                        Name = station.name,
                        AvailableBikes = station.available_bikes,
                        BikeStands = station.bike_stands,
                        Latitude = station.position.lat,
                        Longitude = station.position.lng
                    }).ToList();

                    Console.WriteLine($"✅ {result.Count} stations récupérées avec succès");
                    return result;
                }
                catch (WebException ex)
                {
                    Console.WriteLine($"❌ Erreur WebException : {ex.Message}");

                    if (ex.Response != null)
                    {
                        using (var reader = new StreamReader(ex.Response.GetResponseStream()))
                        {
                            string errorResponse = await reader.ReadToEndAsync();
                            Console.WriteLine($"📄 Réponse d'erreur : {errorResponse}");
                        }
                    }

                    return new List<BikeStation>();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Erreur inattendue : {ex.Message}");
                    return new List<BikeStation>();
                }
            }
        }
    }
}