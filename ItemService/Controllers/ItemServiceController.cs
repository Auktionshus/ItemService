using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MongoDB.Driver;
using RabbitMQ.Client;
using MongoDB.Bson;
using MongoDB.Driver.GridFS;

namespace ItemService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ItemController : ControllerBase
    {
        private IGridFSBucket gridFS;
        private readonly ILogger<ItemController> _logger;
        private readonly string _hostName;
        private readonly string _secret;
        private readonly string _issuer;
        private readonly string _mongoDbConnectionString;

        public ItemController(ILogger<ItemController> logger, IConfiguration config)
        {
            _mongoDbConnectionString = config["MongoDbConnectionString"];
            _hostName = config["HostnameRabbit"];
            _secret = config["Secret"];
            _issuer = config["Issuer"];

            _logger = logger;
            _logger.LogInformation($"Connection: {_hostName}");
        }

        // Placeholder for the auction data storage
        private static readonly List<Item> Items = new List<Item>();

        // Image storage path
        private readonly string _imagePath = "Images";

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetAuth()
        {
            return Ok("You're authorized");
        }

        [Authorize]
        [HttpPost("create")]
        public async Task<IActionResult> CreateItem([FromBody] ItemDTO model)
        {
            try
            {
                _logger.LogInformation(
                    $"Item with title: {model.Title} recieved, from user: {model.Seller}"
                );
                if (model != null)
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
                        string message = JsonSerializer.Serialize(model);

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
                    return Ok(model);

                    _logger.LogInformation("create item called");
                    try
                    {
                        // Opretter forbindelse til RabbitMQ
                        var factory = new ConnectionFactory { HostName = _hostName };

                        using var connection = factory.CreateConnection();
                        using var channel = connection.CreateModel();

                        channel.ExchangeDeclare(exchange: "topic_fleet", type: ExchangeType.Topic);

                        // Serialiseres til JSON
                        string message = JsonSerializer.Serialize(model);

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
                    return Ok(model);
                }
                else
                {
                    return BadRequest("Item object is null");
                }
            }
            catch
            {
                _logger.LogInformation($"An error occurred while trying to create item");
                return BadRequest();
            }
        }

        [HttpGet("list")]
        public async Task<IActionResult> ListItems()
        {
            _logger.LogInformation("Geting ItemList");
            MongoClient dbClient = new MongoClient(
                "mongodb+srv://GroenOlsen:BhvQmiihJWiurl2V@auktionshusgo.yzctdhc.mongodb.net/?retryWrites=true&w=majority"
            );
            var collection = dbClient.GetDatabase("Items").GetCollection<Item>("Item");
            var items = await collection.Find(_ => true).ToListAsync();
            return Ok(items);
        }

        [HttpGet("item/{id}")]
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

        [HttpPost("uploadImage/{id}"), DisableRequestSizeLimit]
        public async Task<IActionResult> UploadImage(Guid id, IFormFile file)
        {
            MongoClient dbClient = new MongoClient(_mongoDbConnectionString);
            var database = dbClient.GetDatabase("Item");
            var collection = dbClient.GetDatabase("Item").GetCollection<Item>("Items");
            gridFS = new GridFSBucket(database);

            if (file == null || file.Length == 0)
            {
                return BadRequest("No file uploaded.");
            }

            var fileName = id.ToString() + Path.GetExtension(file.FileName);

            var options = new GridFSUploadOptions
            {
                Metadata = new BsonDocument { { "itemId", id.ToString() } }
            };

            var imageStream = file.OpenReadStream();
            var fileId = await gridFS.UploadFromStreamAsync(fileName, imageStream, options);
            imageStream.Close();

            var filter = Builders<Item>.Filter.Eq(item => item.Id, id);
            var update = Builders<Item>.Update.Set(item => item.ImageFileId, fileId.ToString());

            await collection.UpdateOneAsync(filter, update);
            return Ok();
        }

        [HttpGet("version")]
        public IEnumerable<string> Get()
        {
            var properties = new List<string>();
            var assembly = typeof(Program).Assembly;
            foreach (var attribute in assembly.GetCustomAttributesData())
            {
                _logger.LogInformation("Tilføjer " + attribute.AttributeType.Name);
                properties.Add($"{attribute.AttributeType.Name} - {attribute.ToString()}");
            }
            return properties;
        }
    }
}
