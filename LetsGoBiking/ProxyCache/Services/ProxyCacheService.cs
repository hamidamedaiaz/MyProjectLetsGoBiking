using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ProxyCache.Models;

namespace ProxyCache.Services
{
    /// <summary>
    /// Implémentation du service ProxyCache
    /// Gère le cache et les appels à l'API JCDecaux
    /// </summary>
    public class ProxyCacheService : IProxyCache
    {
        private readonly JCDecauxService _jcdecauxService;

        // Cache générique pour les stations de vélos (durée : 5 minutes)
        private static readonly ProxyCache<List<BikeStation>> _stationsCache =
            new ProxyCache<List<BikeStation>>(TimeSpan.FromMinutes(5));

        public ProxyCacheService()
        {
            _jcdecauxService = new JCDecauxService();
        }

        /// <summary>
        /// Récupère les stations de vélos avec mise en cache
        /// </summary>
        public async Task<List<BikeStation>> GetStationsAsync(string city)
        {
            try
            {
                Console.WriteLine($"📥 Requête reçue pour la ville : {city}");

                // Clé de cache unique par ville
                string cacheKey = $"JCDecaux-Stations-{city}";

                // Récupération depuis le cache ou appel API
                var stations = await _stationsCache.Get(
                    cacheKey,
                    async () => await _jcdecauxService.GetBikeStations(city)
                );

                Console.WriteLine($"✅ {stations.Count} stations retournées pour {city}");
                return stations;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erreur dans GetStationsAsync : {ex.Message}");
                throw;
            }
        }
    }
}