using System.Collections.Generic;
using System.ServiceModel;
using System.Threading.Tasks;
using ProxyCache.Models;

namespace ProxyCache.Services
{
    /// <summary>
    /// Interface du service ProxyCache (SOAP/WCF)
    /// Responsable de la mise en cache des appels à l'API JCDecaux
    /// </summary>
    [ServiceContract]
    public interface IProxyCache
    {
        /// <summary>
        /// Récupère les stations de vélos pour une ville donnée
        /// Utilise un cache de 5 minutes pour éviter les appels répétés
        /// </summary>
        /// <param name="city">Nom de la ville (contrat JCDecaux)</param>
        /// <returns>Liste des stations de vélos disponibles</returns>
        [OperationContract]
        Task<List<BikeStation>> GetStationsAsync(string city);
    }
}