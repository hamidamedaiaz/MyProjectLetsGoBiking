using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace ProxyCache.Services
{
    /// <summary>
    /// Classe générique de cache avec expiration automatique
    /// Utilise ConcurrentDictionary pour être thread-safe
    /// </summary>
    /// <typeparam name="T">Type des données à mettre en cache</typeparam>
    public class ProxyCache<T>
    {
        private readonly TimeSpan _defaultExpiration;
        private readonly ConcurrentDictionary<string, (T Value, DateTime Expiration)> _cache;

        /// <summary>
        /// Constructeur du cache
        /// </summary>
        /// <param name="defaultExpiration">Durée de vie par défaut des entrées en cache</param>
        public ProxyCache(TimeSpan defaultExpiration)
        {
            _defaultExpiration = defaultExpiration;
            _cache = new ConcurrentDictionary<string, (T Value, DateTime Expiration)>();
        }

        /// <summary>
        /// Récupère une valeur depuis le cache ou la génère si elle n'existe pas/est expirée
        /// </summary>
        /// <param name="key">Clé unique de l'entrée</param>
        /// <param name="fetchAsync">Fonction asynchrone pour générer la valeur si absente du cache</param>
        /// <returns>Valeur mise en cache ou nouvellement générée</returns>
        public async Task<T> Get(string key, Func<Task<T>> fetchAsync)
        {
            // Vérifier si l'entrée existe et est valide
            if (_cache.TryGetValue(key, out var entry) && entry.Expiration > DateTime.UtcNow)
            {
                Console.WriteLine($"💾 Cache HIT pour la clé : {key}");
                return entry.Value;
            }

            Console.WriteLine($"🔄 Cache MISS pour la clé : {key} - Récupération des données...");

            // Récupérer une nouvelle valeur
            T value = await fetchAsync();

            // Stocker dans le cache avec expiration
            _cache[key] = (value, DateTime.UtcNow.Add(_defaultExpiration));

            Console.WriteLine($"💾 Donnée mise en cache (expiration : {_defaultExpiration.TotalMinutes} min)");

            return value;
        }
    }
}