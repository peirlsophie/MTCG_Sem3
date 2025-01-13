using MTCG_Peirl.Models;

namespace MTCG.MyTestProject
{
    public class Tests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void Test1()
        {
            Assert.Pass();
        }

        [Test]
        public void Test2()
        {
            string username = "username";
            string password = "password";
            int coins = 1;
            int highscore = 1;
            int elo = 1;
            int played_games = 1;
            int wins = 1;
            int losses = 1;

            User testUser = new User(username, password, coins, highscore, elo, played_games, wins, losses);

            Assert.Equals(username, testUser.Username);
            Assert.Equals(password, testUser.Password);
            Assert.Equals(coins, testUser.coins);
            Assert.Equals(highscore, testUser.Highscore);
            Assert.Equals(elo, testUser.ELO);
            Assert.Equals(played_games, testUser.games_played);
            Assert.Equals(wins, testUser.Wins);
            Assert.Equals(losses, testUser.Losses);


        }


    }
}

//[Fact]
//public void Test1()
//{

//    string username = "username";
//    string password = "password";
//    int coins = 1;
//    int highscore = 1;
//    int elo = 1;
//    int played_games = 1;
//    int wins = 1;
//    int losses = 1;

//    User testUser = new User(username, password, coins, highscore, elo, played_games, wins, losses);

//    Assert.Equal(username, testUser.username);
//    Assert.Equal(password, testUser.password);
//    Assert.Equal(coins, testUser.coins);
//    Assert.Equal(highscore, testUser.highscore);
//    Assert.Equal(elo, testUser.elo);
//    Assert.Equal(played_games, testUser.played_games);
//    Assert.Equal(wins, testUser.wins);
//    Assert.Equal(losses, testUser.losses);
//    Assert.Equal(1, 1);

//}

//[Fact]

//public void Test2()
//{
//    string id = "abc123";
//    string name = "WaterGoblin";
//    int damage = 1;
//    ElementType type= ElementType.water;
//    string cardType = "Monster";

//    Card testCard = new Card(id, name, damage, type, cardType);


//    Assert.Equal(id, testCard.Id);
//    Assert.Equal(name, testCard.Name);
//    Assert.Equal(damage, testCard.Damage);
//    Assert.Equal(type, testCard.ElementType);
//    Assert.Equal(cardType, testCard.CardType);


//}