

    namespace TestProject1
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
            public void CompareCardDamageReturnWinner()
            {
                Assert.Pass();

                //Arrange = erstellen was wir für Test brauchen

                Battle battle = new Battle();
                Card card1 = new Card();
                card1.Damage = 5;
                Card card2 = new Card();
                card2.Damage = 5;

                //Act = Methode die wir testen wollen ausführen

                Card winner = battle.Fight(card1, card2);

                //Assert = Stimmt das Ergebnis?

                Assert.That(Equals(winner, card2));
            }
        }
    }
