using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace WebApplication8.Models
{
    public class IoTData
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; } = null; // ID automático 

        public string DeviceId { get; set; }
        public double Temperature { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
