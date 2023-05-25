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
                        routingKey: "auctions.create",
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
            var collection = dbClient.GetDatabase("item").GetCollection<Item>("items");
            var items = await collection.Find(_ => true).ToListAsync();
            return Ok(items);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetItem(Guid id)
        {
            MongoClient dbClient = new MongoClient(
                "mongodb+srv://GroenOlsen:BhvQmiihJWiurl2V@auktionshusgo.yzctdhc.mongodb.net/?retryWrites=true&w=majority"
            );
            var collection = dbClient.GetDatabase("item").GetCollection<Item>("items");
            Item item = await collection.Find(a => a.Id == id).FirstOrDefaultAsync();

            if (item == null)
            {
                return NotFound($"Auction with Id {id} not found.");
            }
            return Ok(item);
        }

        [HttpGet("{id}/listImages")]
        public IActionResult ListImages(Guid id)
        {
            Item item = Items.FirstOrDefault(a => a.Id == id);
            if (item == null)
            {
                return NotFound($"Item with Id {id} not found.");
            }
            return Ok(item.ImageHistory);
        }

        [HttpPost("uploadImage/{id}"), DisableRequestSizeLimit]
        public async Task<IActionResult> UploadImage(Guid id)
        {
            if (!Directory.Exists(_imagePath))
            {
                Directory.CreateDirectory(_imagePath);
            }

            MongoClient dbClient = new MongoClient(
                "mongodb+srv://GroenOlsen:BhvQmiihJWiurl2V@auktionshusgo.yzctdhc.mongodb.net/?retryWrites=true&w=majority"
            );
            var collection = dbClient.GetDatabase("item").GetCollection<Item>("items");
            var filter = Builders<Item>.Filter.Eq(a => a.Id, id);
            Item item = await collection.Find(filter).FirstOrDefaultAsync();

            if (item == null)
            {
                return NotFound($"Item with Id {id} not found.");
            }

            if (item.ImageHistory == null)
            {
                item.ImageHistory = new List<ImageRecord>();
            }

            try
            {
                foreach (var formFile in Request.Form.Files)
                {
                    // Validate file type and size
                    if (formFile.ContentType != "image/jpeg" && formFile.ContentType != "image/png")
                    {
                        return BadRequest(
                            $"Invalid file type for file {formFile.FileName}. Only JPEG and PNG files are allowed."
                        );
                    }
                    if (formFile.Length > 1048576) // 1MB
                    {
                        return BadRequest(
                            $"File {formFile.FileName} is too large. Maximum file size is 1MB."
                        );
                    }
                    if (formFile.Length > 0)
                    {
                        var fileName = "image-" + Guid.NewGuid().ToString() + ".jpg";
                        var fullPath = _imagePath + Path.DirectorySeparatorChar + fileName;

                        using (var stream = new FileStream(fullPath, FileMode.Create))
                        {
                            formFile.CopyTo(stream);
                        }

                        var imageURI = new Uri(fileName, UriKind.RelativeOrAbsolute);
                        var imageRecord = new ImageRecord
                        {
                            Id = Guid.NewGuid(),
                            Location = imageURI,
                            Date = DateTime.UtcNow,
                            // Add other properties like Description and AddedBy as needed
                        };

                        item.ImageHistory.Add(imageRecord);
                        var update = Builders<Item>.Update.Push(
                            a => a.ImageHistory,
                            imageRecord
                        );
                        await collection.UpdateOneAsync(filter, update);
                    }
                    else
                    {
                        return BadRequest("Empty file submitted.");
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex}");
            }

            return Ok("Image(s) uploaded successfully.");
        }
    }
}