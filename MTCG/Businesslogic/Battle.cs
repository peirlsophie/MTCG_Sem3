using Microsoft.IdentityModel.Protocols;
using MTCG.Database;
using MTCG_Peirl.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace MTCG.Businesslogic
{
    public class Battle
    {
        private readonly DatabaseAccess dbAccess;
        private readonly UserDatabase userDatabase;
        private readonly CardPackagesDatabase cardPackagesDb;

        public int round;

        public Battle(DatabaseAccess dbAccess) 
        {
            this.dbAccess = dbAccess;
            this.cardPackagesDb = new CardPackagesDatabase(dbAccess);
            this.userDatabase = new UserDatabase(dbAccess);

        }

        public void startBattle(string username)
        {
            int userIdFirstPlayer = cardPackagesDb.findUserIdByName(username);
            Console.WriteLine("kommt es hierher / userfinden");

            userDatabase.enterPlayersInBattle(userIdFirstPlayer);
            
        }

        public void playBattleRound(string username1, string username2)
        {
           




        }

        public List<Card> choosePlayingCards(string username1, string username2)
        {
            Random rnd = new Random();
            int randomNumberPlayer1 = rnd.Next(4);
            int randomNumberPlayer2 = rnd.Next(4);

            
            List<string> cardsPlayer1 = cardPackagesDb.findOwnedCardIdsInDecks(username1);
            List<string> cardsPlayer2 = cardPackagesDb.findOwnedCardIdsInDecks(username2);

            string cardIdPlayer1 = cardsPlayer1[randomNumberPlayer1];
            string cardIdPlayer2 = cardsPlayer2[randomNumberPlayer2];


            Card card1 = cardPackagesDb.getCardFromDeckForBattle(cardIdPlayer1);

            Card card2 = cardPackagesDb.getCardFromDeckForBattle(cardIdPlayer2);
            
            List<Card> cards = new List<Card> { card1, card2 };


            Console.WriteLine($"The chosen card for player1 is: {card1.Name}. The chosen card for player2 is {card2.Name}.");

            return cards;
        }

        public List<User> createPlayerObjects(string username1, string username2)
        {
            
            int userId1 = cardPackagesDb.findUserIdByName(username1);
            User player1 = userDatabase.getUserObjectById(userId1);

            int userId2 = cardPackagesDb.findUserIdByName(username2);
            User player2 = userDatabase.getUserObjectById(userId2);

            Console.WriteLine($"Player1 is: {player1.Username}, Player2 is: {player2.Username}");
            List<User> users = new List<User> { player1, player2 };

            return users;
        }

        public void FightingLogic(Card card1, Card card2)
        {
            Console.WriteLine($"Card {card1.Name} has a damage value of {card1.Damage}.");
            Console.WriteLine($"Card {card2.Name} has a damage value of {card2.Damage}.");

            var doubleDamage_halfedDamage1 = checkElementEffects(card1.ElementType, card2.ElementType);

            Console.WriteLine($"Card {card1.Name} is of the element {card1.ElementType}.");

            var doubleDamage_halfedDamage2 = checkElementEffects(card2.ElementType, card1.ElementType);

            Console.WriteLine($"Card {card2.Name} is of the element {card2.ElementType}.");

            bool player1Wins = checkSpecialMonsterEffects(card1, card2);

            Console.WriteLine($"Card {card1.Name} is of type {card1.CardType}.");

            bool player2Wins = checkSpecialMonsterEffects(card1, card2);

            Console.WriteLine($"Card {card2.Name} is of type {card2.CardType}.");

            //Anpassung damage Wert je nach bool ob doubled/halfed#

            if(doubleDamage_halfedDamage1 == (false, false) && player1Wins == false && player2Wins == false)
            {
                

            }


        }

        public Card determineWinnerCard(Card card1, Card card2)
        {
            if(card1.Damage > card2.Damage)
            {
                return card1;
            }
            else if(card1.Damage < card2.Damage)
            {
                return card2;
            }
            else if(card1.Damage == card2.Damage)
            {
                return null;
            }

        }

        public (bool doubleDamage, bool halfedDamage) checkElementEffects(ElementType type1, ElementType type2)
        {
            return (type1, type2) switch
            {
                (ElementType.water, ElementType.fire) => (true, false),
                (ElementType.fire, ElementType.normal) => (true, false),
                (ElementType.normal, ElementType.water) => (true, false),
                (ElementType.fire, ElementType.water) => (false, true),
                _ => (false, false)
            };
        }


        public bool checkSpecialMonsterEffects(Card card1, Card card2)
        {
            bool instantWinCard = false;
            Random rnd = new Random();
            

            if (card1.CardType == "Dragon" && card2.CardType == "Goblin")
            {
                instantWinCard = true;
            }
            else if(card1.CardType == "Wizard" && card2.CardType == "Ork")
            {
                instantWinCard = true;
            }
            else if(card1.CardType == "Spell" && card1.ElementType == ElementType.water && card2.CardType == "Knight")
            {
                instantWinCard = true;
            }
            else if (card1.CardType == "Kraken" && card2.CardType == "Spell")
            {
                instantWinCard = true;
            }
            else if(card1.CardType == "Elf" && card1.ElementType == ElementType.fire && card2.CardType == "Dragon")
            {
                int chanceThatDragonMisses = rnd.Next(2);
                if(chanceThatDragonMisses == 1)
                {
                    instantWinCard = true;
                }
                else
                { 
                    instantWinCard = false;
                }
            }

            return instantWinCard;

        }
    }
}
