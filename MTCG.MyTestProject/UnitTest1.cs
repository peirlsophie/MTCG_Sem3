using NUnit.Framework;
using MTCG.NewFolder;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading.Tasks;
using MTCG.Backend;
using MTCG_Peirl.Models;
using MTCG.Database;
using MTCG.HTTP;

namespace MTCG.MyTestProject
{
    public class Tests
    {
        private HttpServer _server;
        private HttpResponse _response;
        private Card _card;
        private UserDatabase _userDatabase;
        private PackagesEndpoint _packagesEndpoint;
        private BattlesEndpoint _battlesEndpoint;

        [SetUp]
        public void Setup()
        {
            _server = new HttpServer(IPAddress.Parse("127.0.0.1"), 10001);
            _response = new HttpResponse(new StreamWriter(new MemoryStream()));
            _card = new Card("1", "Dragon", 50.0, ElementType.fire, "Monster");
            _userDatabase = new UserDatabase(new DatabaseAccess());
            _packagesEndpoint = new PackagesEndpoint(new DatabaseAccess());
            _battlesEndpoint = new BattlesEndpoint(new DatabaseAccess());
        }

        [Test]
        public void TestHttpServerInitialization()
        {
            Assert.That(_server, Is.Not.Null);
        }

        [Test]
        public void TestHttpResponseInitialization()
        {
            Assert.That(_response, Is.Not.Null);
        }

        [Test]
        public void TestCardInitialization()
        {
            Assert.That(_card.Name, Is.EqualTo("Dragon"));
            Assert.That(_card.Damage, Is.EqualTo(50.0));
            Assert.That(_card.ElementType, Is.EqualTo(ElementType.fire));
            Assert.That(_card.CardType, Is.EqualTo("Monster"));
        }

        [Test]
        public void TestUserExists()
        {
            bool userExists = _userDatabase.UserExists("testuser");
            Assert.That(userExists, Is.False);
        }
        [Test]
        public void TestCreatePackages()
        {
            var request = new HttpRequest(new StreamReader(new MemoryStream()));
            var response = new HttpResponse(new StreamWriter(new MemoryStream()));
            Assert.DoesNotThrow(() => _packagesEndpoint.createPackages(request, response));
        }
        [Test]
        public void TestPurchasePackages()
        {
            var request = new HttpRequest(new StreamReader(new MemoryStream()));
            var response = new HttpResponse(new StreamWriter(new MemoryStream()));
            Assert.DoesNotThrow(() => _packagesEndpoint.purchasePackages(request, response));
        }

        [Test]
        public void TestHttpResponseSendResponse()
        {
            _response.statusCode = 200;
            _response.statusMessage = "OK";
            Assert.DoesNotThrow(() => _response.SendResponse());
        }

        [Test]
        public void TestCardProperties()
        {
            _card.Name = "Water Dragon";
            _card.Damage = 60.0;
            _card.ElementType = ElementType.water;
            _card.CardType = "Monster";

            Assert.That(_card.Name, Is.EqualTo("Water Dragon"));
            Assert.That(_card.Damage, Is.EqualTo(60.0));
            Assert.That(_card.ElementType, Is.EqualTo(ElementType.water));
            Assert.That(_card.CardType, Is.EqualTo("Monster"));
        }

        [Test]
        public void TestHttpServerHandleEndpoints()
        {
            var client = new TcpClient();
            Assert.DoesNotThrowAsync(async () => await _server.HandleEndpoints(client));
        }

        [Test]
        public void TestHandlePackageRequests()
        {
            var request = new HttpRequest(new StreamReader(new MemoryStream()));
            var response = new HttpResponse(new StreamWriter(new MemoryStream()));
            Assert.DoesNotThrowAsync(async () => await _packagesEndpoint.handlePackageRequests(request, response));
        }

        [Test]
        public void TestHandleBattlesRequests()
        {
            var request = new HttpRequest(new StreamReader(new MemoryStream()));
            var response = new HttpResponse(new StreamWriter(new MemoryStream()));
            Assert.DoesNotThrowAsync(async () => await _battlesEndpoint.handleBattlesRequests(request, response));
        }

        [Test]
        public void TestStartBattle()
        {
            var request = new HttpRequest(new StreamReader(new MemoryStream()));
            var response = new HttpResponse(new StreamWriter(new MemoryStream()));
            Assert.DoesNotThrow(() => _battlesEndpoint.startBattle(request, response));
        }

        [Test]
        public void TestEloCalcUsers()
        {
            var winner = new User("winner",  "test1", 10, 10, 120, 1, 3, 1);
            var loser = new User("loser", "test2", 10, 10, 90, 1, 1, 3);
            Assert.DoesNotThrow(() => _battlesEndpoint.eloCalcUsers(winner, loser));
        }

    

        [Test]
        public void TestCountAvailablePackages()
        {
            var cardPackagesDb = new CardPackagesDatabase(new DatabaseAccess());
            int count = cardPackagesDb.countAvailablePackages();
            Assert.That(count, Is.GreaterThanOrEqualTo(0));
        }

        [Test]
        public void TestHttpResponseWriter()
        {
            Assert.That(_response.writer, Is.Not.Null);
        }

        [Test]
        public void TestHttpResponseStatusCode()
        {
            _response.statusCode = 404;
            Assert.That(_response.statusCode, Is.EqualTo(404));
        }

        [Test]
        public void TestHttpResponseStatusMessage()
        {
            _response.statusMessage = "Not Found";
            Assert.That(_response.statusMessage, Is.EqualTo("Not Found"));
        }

        [Test]
        public void TestHttpServerStatusCode()
        {
            _server.statusCode = 500;
            Assert.That(_server.statusCode, Is.EqualTo(500));
        }

        [Test]
        public void TestHttpServerStatusMessage()
        {
            _server.statusMessage = "Internal Server Error";
            Assert.That(_server.statusMessage, Is.EqualTo("Internal Server Error"));
        }

        [Test]
        public void TestHttpServerPath()
        {
            _server.Path = "/api/test";
            Assert.That(_server.Path, Is.EqualTo("/api/test"));
        }

        [Test]
        public void TestHttpServerDatabaseAccess()
        {
            Assert.That(_server.dbAccess, Is.Not.Null);
        }

        [Test]
        public void TestHttpServerUserEndpoint()
        {
            Assert.That(_server.userEndpoint, Is.Not.Null);
        }

        [Test]
        public void TestHttpServerPackagesEndpoint()
        {
            Assert.That(_server.packagesEndpoint, Is.Not.Null);
        }

        [Test]
        public void TestHttpServerCardsDeckEndpoint()
        {
            Assert.That(_server.cardsDeckEndpoint, Is.Not.Null);
        }

        [Test]
        public void TestHttpServerBattlesEndpoint()
        {
            Assert.That(_server.battlesEndpoint, Is.Not.Null);
        }

        [Test]
        public void TestHttpServerUserDatabase()
        {
            Assert.That(_server.userDatabase, Is.Not.Null);
        }

        [Test]
        public void TestHttpServerCardPackagesDatabase()
        {
            Assert.That(_server.cardPackagesDb, Is.Not.Null);
        }
    }
}
