using System;

namespace RoutingService.Models
{
    [Serializable]
    public class BikeStation
    {
        public string Name { get; set; }
        public int AvailableBikes { get; set; }
        public int BikeStands { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }
}