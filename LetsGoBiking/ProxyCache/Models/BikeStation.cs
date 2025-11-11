using System;

namespace ProxyCache.Models
{
    /// <summary>
    /// Modèle représentant une station de vélos JCDecaux
    /// </summary>
    [Serializable]
    public class BikeStation
    {
        /// <summary>
        /// Nom de la station
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Nombre de vélos disponibles
        /// </summary>
        public int AvailableBikes { get; set; }

        /// <summary>
        /// Nombre total de places (occupées + libres)
        /// </summary>
        public int BikeStands { get; set; }

        /// <summary>
        /// Latitude de la station (coordonnée GPS)
        /// </summary>
        public double Latitude { get; set; }

        /// <summary>
        /// Longitude de la station (coordonnée GPS)
        /// </summary>
        public double Longitude { get; set; }

        public override string ToString()
        {
            return $"{Name} - Vélos: {AvailableBikes}/{BikeStands} - Position: ({Latitude}, {Longitude})";
        }
    }
}