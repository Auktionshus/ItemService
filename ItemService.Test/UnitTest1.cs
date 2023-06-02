using NUnit.Framework;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using ItemService.Controllers;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace ItemService.Test
{
    [TestFixture]  // Denne attribut markerer klassen som en testklasse for NUnit.
    public class UnitTest1 // renamed to avoid collision with actual controller
    {
        // Vi definerer Mock objekter, som vi vil bruge til at "lade som om" vi har en ILogger<BidController> og en IConfiguration
        private Mock<ILogger<ItemController>> _loggerMock;
        private Mock<IConfiguration> _configurationMock;
         private Mock<Environment> _environmentMock;

        [SetUp]  // Denne attribut fortæller NUnit, at denne metode skal køres før hver enkelt testmetode.
        public void Setup()
        {
            // Vi initialiserer vores Mock objekter.
            _loggerMock = new Mock<ILogger<ItemController>>();
            _configurationMock = new Mock<IConfiguration>();
            _environmentMock = new Mock<Environment>();
        }

        [Test]  // Denne attribut fortæller NUnit, at denne metode er en testmetode.
        public async Task PlaceBid_ValidBid_ReturnsStatusCodeResult()
        {
            // Arrange

            // Vi "opsætter" vores Mock IConfiguration til at returnere specifikke værdier, når visse metoder bliver kaldt.
            _configurationMock.Setup(x => x["HostnameRabbit"]).Returns("test");
            _configurationMock.Setup(x => x["Secret"]).Returns("test");
            _configurationMock.Setup(x => x["Issuer"]).Returns("test");
            _configurationMock.Setup(x => x["MongoDbConnectionString"]).Returns("test");

            // Vi opretter en ny instans af BidController, som vi vil teste, ved at give den vores Mock objekter.
              var controller = new ItemController(_loggerMock.Object, _environmentMock.Object, _configurationMock.Object);




            // Vi opretter en ny BidDTO, som vi vil bruge til at kalde PlaceBid metoden.
            var item = new ItemDTO
            {
                Seller = Guid.NewGuid(),
                Title = "Chair",
                Brand = "Armani",
                Description = "DesignerCheir",
                CategoryCode = "CH",
                Location = "Eaaa Aarhus"
            };

            // Act

            // Vi kalder PlaceBid metoden med vores ItemDTO og gemmer resultatet.
            var result = await controller.CreateItem(item);

            // Assert

            // Vi kontrollerer, at resultatet af PlaceBid metoden er en instans af StatusCodeResult.
            Assert.IsInstanceOf<StatusCodeResult>(result);
        }


        // Add more test methods here for other scenarios
    }
}