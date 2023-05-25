using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using MongoDB.Driver;



namespace ItemService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ItemController : ControllerBase
    {
        private readonly ILogger<ItemController> _logger;
        private readonly string _hostName;

        public ItemController(ILogger<ItemController> logger, IConfiguration config)
        {
            _logger = logger;
            _hostName = config["HostnameRabbit"];
            _logger.LogInformation($"Connection: {_hostName}");
        }

        // Placeholder for the auction data storage
        private static readonly List<Item> Items = new List<Item>();

        // Image storage path
        private readonly string _imagePath = "Images";

        [HttpPost("create")]
        public async Task<IActionResult> CreateItem(Item item)
        {
            if (item != null)
            {
                _logger.LogInformation("create item called");
                try
                {
                    // Opretter forbindelse til RabbitMQ
                    var factory = new ConnectionFactory { HostName = _hostName };

                    using var connection = factory.CreateConnection();
                    using var channel = connection.CreateModel();

                    channel.ExchangeDeclare(exchange: "topic_fleet", type: ExchangeType.Topic);

                    // Serialiseres til JSON
                    string message = JsonSerializer.Serialize(item);

                    // Konverteres til byte-array
                    var body = Encoding.UTF8.GetBytes(message);

                    // Sendes til kø
                    channel.BasicPublish(
                        exchange: "topic_fleet",
                        routingKey: "items.create",
                        basicProperties: null,
                        body: body
                    );

                    _logger.LogInformation("Item created and sent to RabbitMQ");
                }
                catch (Exception ex)
                {
                    _logger.LogInformation("error " + ex.Message);
                    return StatusCode(500);
                }
                return Ok(item);
            }
            else
            {
                return BadRequest("Item object is null");
            }
        }

        [HttpGet("list")]
        public async Task<IActionResult> ListItems()
        {
            MongoClient dbClient = new MongoClient(
                "mongodb+srv://GroenOlsen:BhvQmiihJWiurl2V@auktionshusgo.yzctdhc.mongodb.net/?retryWrites=true&w=majority"
            );
            var collection = dbClient.GetDatabase("Items").GetCollection<Item>("Item");
            var items = await collection.Find(_ => true).ToListAsync();
            return Ok(items);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetItem(Guid id)
        {
            MongoClient dbClient = new MongoClient(
                "mongodb+srv://GroenOlsen:BhvQmiihJWiurl2V@auktionshusgo.yzctdhc.mongodb.net/?retryWrites=true&w=majority"
            );
            var collection = dbClient.GetDatabase("Items").GetCollection<Item>("Item");
            Item item = await collection.Find(a => a.Id == id).FirstOrDefaultAsync();

            if (item == null)
            {
                return NotFound($"Item with Id {id} not found.");
            }
            return Ok(item);
        }

    }
}