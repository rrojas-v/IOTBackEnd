using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using WebApplication8.Models;

namespace WebApplication8.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // JWT tokens
    public class IoTController : ControllerBase
    {

        private readonly IMongoCollection<IoTData> _iotCollection;

        public IoTController(IMongoDatabase mongoDatabase)
        {
            var collectionName = "IoTData";  // Debe coincidir con appsettings
            _iotCollection = mongoDatabase.GetCollection<IoTData>(collectionName);
        }

        //[HttpPost]
        //public async Task<IActionResult> PostData([FromBody] IoTData data)
        //{
        //    if (data == null)
        //    {
        //        return BadRequest("Datos inválidos.");
        //    }

        //    await _iotCollection.InsertOneAsync(data);
        //    return Ok("Datos guardados exitosamente.");
        //}

        [HttpPost]
        public async Task<IActionResult> PostData([FromBody] List<IoTData> dataList)
        {
            if (dataList == null || dataList.Count == 0)
            {
                return BadRequest("Datos inválidos o lista vacía.");
            }

            await _iotCollection.InsertManyAsync(dataList);
            return Ok($"Se insertaron {dataList.Count} datos exitosamente.");
        }
        [HttpGet("GetLast/{deviceId}")]
        public async Task<IActionResult> GetLast(string deviceId)
        {
            if (deviceId == null)
            {
                return NotFound("Falta deviceId.");
            }
            var filter = Builders<IoTData>.Filter.Empty;
            if (!string.IsNullOrEmpty(deviceId))
            {
                filter &= Builders<IoTData>.Filter.Eq(d => d.DeviceId, deviceId);
            }
            var result = await _iotCollection
                .Find(filter)
                .Sort(Builders<IoTData>.Sort.Descending(d => d.Timestamp))
                .Limit(1)
                .FirstOrDefaultAsync();

            if (result == null)
            {
                return NotFound("No se encontró ningún dato.");
            }

            return Ok(result);
        }
        [HttpGet]
        public async Task<IActionResult> GetData(
            [FromQuery] string deviceId = null,
            [FromQuery] DateTime? startTimestamp = null,
            [FromQuery] DateTime? endTimestamp = null)
        {
            var filter = Builders<IoTData>.Filter.Empty;

            if (!string.IsNullOrEmpty(deviceId))
            {
                filter &= Builders<IoTData>.Filter.Eq(d => d.DeviceId, deviceId);
            }

            if (startTimestamp.HasValue)
            {
                filter &= Builders<IoTData>.Filter.Gte(d => d.Timestamp, startTimestamp.Value);
            }

            if (endTimestamp.HasValue)
            {
                filter &= Builders<IoTData>.Filter.Lte(d => d.Timestamp, endTimestamp.Value);
            }

            var results = await _iotCollection.Find(filter)
                .Sort(Builders<IoTData>.Sort.Descending(d => d.Timestamp))
                .Limit(100) // Limitar a los 100 registros más recientes
                .ToListAsync();

            if (results.Count == 0)
            {
                return NotFound("No se encontraron datos.");
            }

            return Ok(results);
        }

    }
}
