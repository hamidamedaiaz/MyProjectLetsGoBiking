using System;
using System.Runtime.Serialization;

namespace RoutingService.Models
{
    [DataContract]
    public class BikeStation
    {
        [DataMember]
        public string Name { get; set; }
        
        [DataMember]
        public int AvailableBikes { get; set; }
        
        [DataMember]
        public int BikeStands { get; set; }
        
        [DataMember]
        public double Latitude { get; set; }
        
        [DataMember]
        public double Longitude { get; set; }
    }
}