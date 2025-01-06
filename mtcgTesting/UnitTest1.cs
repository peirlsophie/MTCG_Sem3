using Microsoft.VisualStudio.TestTools.UnitTesting;
using MTCG_Peirl.Models;

namespace mtcgTesting
{
    [TestClass]
    public class UserTests
    {
        [TestMethod]
        public void Constructor_Testing()
        {
            string username = "testuser";
            string password = "password";
            int coins = 1;
            int highscore = 1;
            int ELO = 1;
            int games_played = 1;
            int Wins = 1;
            int Losses = 1;

            User user = new User(username, password, coins, highscore, ELO, games_played, Wins, Losses);

            Assert.AreEqual(username, user.Username);
            Assert.AreEqual(password, user.Password);
            Assert.AreEqual(coins, user.coins);
            Assert.AreEqual(ELO, user.ELO);
            Assert.AreEqual(games_played, user.games_played);
            Assert.AreEqual(highscore, user.Highscore);
            Assert.AreEqual(Wins, user.Wins);
            Assert.AreEqual(Losses, user.Losses);

        }
    }
}
