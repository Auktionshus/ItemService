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

        private MongoClient dbClient;

        public ItemController(
            ILogger<ItemController> logger,
            Environment secrets,
            IConfiguration config
        )
        {
            try
            {
                _hostName = config["HostnameRabbit"];
                _secret = secrets.dictionary["Secret"];
                _issuer = secrets.dictionary["Issuer"];
                _mongoDbConnectionString = secrets.dictionary["ConnectionString"];

                _logger = logger;
                _logger.LogInformation($"Secret: {_secret}");
                _logger.LogInformation($"Issuer: {_issuer}");
                _logger.LogInformation($"MongoDbConnectionString: {_mongoDbConnectionString}");

                // Connect to MongoDB
                dbClient = new MongoClient(_mongoDbConnectionString);
            }
            catch (Exception e)
            {
                _logger.LogError($"Error getting environment variables{e.Message}");
            }
        }

        /// <summary>
        /// Creates an item
        /// </summary>
        /// <param name="item">ItemDTO</param>
        /// <returns>The created item</returns>
        [Authorize]
        [HttpPost("create")]
        public async Task<IActionResult> CreateItem([FromBody] ItemDTO item)
        {
            _logger.LogInformation(
                $"Item with title: {item.Title} recieved, from user: {item.Seller}"
            );
            if (item != null)
            {
                _logger.LogInformation("create item called");
                User user = null;
                try
                {
                    var userCollection = dbClient.GetDatabase("User").GetCollection<User>("Users");
                    user = userCollection.Find(u => u.Id == item.Seller).FirstOrDefault();
                    _logger.LogInformation($" [x] Received user with id: {user.Id}");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"An error occurred while querying the user collection: {ex}");
                }
                if (user == null)
                {
                    _logger.LogInformation("User not found");
                    return BadRequest("User not found");
                }
                else if (
                    item.CategoryCode == "CH"
                    || item.CategoryCode == "LA"
                    || item.CategoryCode == "CO"
                    || item.CategoryCode == "RI"
                )
                {
                    try
                    {
                        // Connect to RabbitMQ
                        var factory = new ConnectionFactory { HostName = _hostName };

                        using var connection = factory.CreateConnection();
                        using var channel = connection.CreateModel();

                        channel.ExchangeDeclare(exchange: "topic_fleet", type: ExchangeType.Topic);

                        // Serialize to JSON
                        string message = JsonSerializer.Serialize(item);

                        // Convert to byte-array
                        var body = Encoding.UTF8.GetBytes(message);

                        // Send to queue
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
                    _logger.LogInformation("CategoryCode: " + item.CategoryCode + " not valid");
                    return BadRequest("CategoryCode not valid");
                }
            }
            else
            {
                return BadRequest("Item object is null");
            }
        }

        /// <summary>
        /// Lists all items
        /// </summary>
        /// <returns>A list of items</returns>
        [HttpGet("list")]
        public async Task<IActionResult> ListItems()
        {
            _logger.LogInformation("Geting ItemList");
            var collection = dbClient.GetDatabase("Items").GetCollection<Item>("Item");
            var items = await collection.Find(_ => true).ToListAsync();
            return Ok(items);
        }

        /// <summary>
        /// Gets item from id
        /// </summary>
        /// <param name="id">Item id</param>
        /// <returns>An item</returns>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetItem(Guid id)
        {
            var collection = dbClient.GetDatabase("Items").GetCollection<Item>("Item");
            Item item = await collection.Find(a => a.Id == id).FirstOrDefaultAsync();

            if (item == null)
            {
                return NotFound($"Item with Id {id} not found.");
            }
            return Ok(item);
        }

        /// <summary>
        /// Creates an image for an item
        /// </summary>
        /// <param name="id">Item id</param>
        /// <param name="file">Image file</param>
        /// <returns>Ok</returns>
        [HttpPost("uploadImage/{id}"), DisableRequestSizeLimit]
        public async Task<IActionResult> UploadImage(Guid id, IFormFile file)
        {
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

        /// <summary>
        /// Gets the version information of the service
        /// </summary>
        /// <returns>A list of version information</returns>
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
