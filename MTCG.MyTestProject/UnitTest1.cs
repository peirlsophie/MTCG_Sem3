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
using MTCG.Businesslogic;

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
        private Battle _battle;

        [SetUp]
        public void Setup()
        {
            _server = new HttpServer(IPAddress.Parse("127.0.0.1"), 10001);
            _response = new HttpResponse(new StreamWriter(new MemoryStream()));
            _card = new Card("1", "Dragon", 50.0, ElementType.fire, "Monster");
            _userDatabase = new UserDatabase(new DatabaseAccess());
            _packagesEndpoint = new PackagesEndpoint(new DatabaseAccess());
            _battlesEndpoint = new BattlesEndpoint(new DatabaseAccess());
            _battle = new Battle(new DatabaseAccess());
        }

        [Test]
        public void TestHttpServer()
        {
            Assert.That(_server, Is.Not.Null);
        }

        [Test]
        public void TestHttpResponse()
        {
            Assert.That(_response, Is.Not.Null);
        }

        [Test]
        public void TestCard_CardCreation()
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
        public void TestSendResponse()
        {
            _response.statusCode = 200;
            _response.statusMessage = "OK";
            Assert.DoesNotThrow(() => _response.SendResponse());
        }

        [Test]
        public void TestCard_CheckProperties()
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
        public void TestHandleEndpoints()
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
        public void TestDetermineWinnerCard_TieReturnsNull()
        {
            Card card1 = new Card("11", "Same Monster", 50, ElementType.fire, "Monster");
            Card card2 = new Card("12", "Same Monster", 50, ElementType.water, "Monster");

            Card winner = _battle.determineWinnerCard(card1, card2, 50, 50, _response);

            Assert.That(winner, Is.Null);
        }

        [Test]
        public void TestCheckElementEffect_DoubleDamage()
        {
            var (doubleDamage, halfedDamage) = _battle.checkElementEffects(ElementType.water, ElementType.fire);

            Assert.That(doubleDamage, Is.True);
            Assert.That(halfedDamage, Is.False);
        }

        [Test]
        public void TestCheckElementEffect_HalfedDamage()
        {
            var (doubleDamage, halfedDamage) = _battle.checkElementEffects(ElementType.fire, ElementType.water);

            Assert.That(doubleDamage, Is.False);
            Assert.That(halfedDamage, Is.True);
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
