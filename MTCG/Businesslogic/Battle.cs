using Microsoft.IdentityModel.Protocols;
using MTCG.Database;
using MTCG.NewFolder;
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
        private static readonly object battleLock = new object();
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

            lock (battleLock)
            {
                userDatabase.enterPlayersInBattle(userIdFirstPlayer);
               
            }            
        }

        public User playBattleRound(User user1, User user2, HttpResponse response)
        {

            List<Card> cards = choosePlayingCards(user1.Username, user2.Username, response);

            Card card1 = cards[0];
            Card card2 = cards[1];

            Card winnercard = FightingLogic(card1, card2, response);

            if(winnercard == null)
            {
                return null;
            }
            else
            {
                if(winnercard.Id == card1.Id)
                {
                    addLoserCardToWinnerDeck(card2, user1);
                    removeLoserCardFromLoserDeck(card2, user2, response);
                    return user1;
                }
                else
                {
                    addLoserCardToWinnerDeck(card1, user2);
                    removeLoserCardFromLoserDeck(card1, user1, response);
                    return user2;
                }

            }

        }

        

        public List<Card> choosePlayingCards(string username1, string username2, HttpResponse response)
        {
            Random rnd = new Random();
            
            List<string> cardsPlayer1 = cardPackagesDb.findOwnedCardIdsInDecks(username1);
            List<string> cardsPlayer2 = cardPackagesDb.findOwnedCardIdsInDecks(username2);

            int randomNumberPlayer1 = rnd.Next(cardsPlayer1.Count);
            int randomNumberPlayer2 = rnd.Next(cardsPlayer2.Count);

            string cardIdPlayer1 = cardsPlayer1[randomNumberPlayer1];
            string cardIdPlayer2 = cardsPlayer2[randomNumberPlayer2];


            Card card1 = cardPackagesDb.getCardFromDeckForBattle(cardIdPlayer1);

            Card card2 = cardPackagesDb.getCardFromDeckForBattle(cardIdPlayer2);
            
            List<Card> cards = new List<Card> { card1, card2 };


            response.statusMessage = $"\nThe chosen card for player1 is: {card1.Name}. The chosen card for player2 is {card2.Name}.";
            response.SendResponse();
            return cards;
        }

        public List<User> createPlayerObjects(string username1, string username2, HttpResponse response)
        {
            
            int userId1 = cardPackagesDb.findUserIdByName(username1);
            User player1 = userDatabase.getUserObjectById(userId1);
            addDeckCardsToUserDeck(player1);

            int userId2 = cardPackagesDb.findUserIdByName(username2);
            User player2 = userDatabase.getUserObjectById(userId2);
            addDeckCardsToUserDeck(player2);

            response.statusMessage = $"\nPlayer1 is: {player1.Username}, Player2 is: {player2.Username}";
            response.SendResponse();

            List<User> users = new List<User> { player1, player2 };

            return users;
        }

        

        public void addLoserCardToWinnerDeck(Card card, User winner)
        {
            winner.ownedCards.Push(card);
        }
        public void removeLoserCardFromLoserDeck(Card card, User loser, HttpResponse response)
        {
            Stack<Card> tempStack = new Stack<Card>();

            // Transfer cards to temporary stack until the target card is found
            while (loser.ownedCards.Count > 0)
            {
                Card currentCard = loser.ownedCards.Pop();
                if (currentCard.Id != card.Id)
                {
                    tempStack.Push(currentCard);
                }
                else
                {
                    response.statusMessage = $"\nCard {card.Name} has been removed from {loser.Username}'s deck.";
                    response.SendResponse();

                    break;
                }
            }

            // Return cards back to the original stack (reversed back to original order)
            while (tempStack.Count > 0)
            {
                loser.ownedCards.Push(tempStack.Pop());
            }
        }


        public void addDeckCardsToUserDeck(User player)
        {
            List<string> cardIds = cardPackagesDb.findOwnedCardIdsInDecks(player.Username);
            foreach (var cardId in cardIds)
            {
                Card card = cardPackagesDb.getCardFromDeckForBattle(cardId);
                player.ownedCards.Push(card);
            }

        }

        public Card FightingLogic(Card card1, Card card2, HttpResponse response)
        {
            string cardLog = $"\nCard {card1.Name} has a damage value of {card1.Damage} and is of the element {card1.ElementType}.\n"+
                               $"Card {card2.Name} has a damage value of {card2.Damage} and is of the element {card2.ElementType}.\n"+
                               $"Card {card1.Name} is of type {card1.CardType}.\n"+
                               $"Card {card2.Name} is of type {card2.CardType}.";

            bool player1Wins = checkSpecialMonsterEffects(card1, card2, response);
            bool player2Wins = checkSpecialMonsterEffects(card2, card1, response);

             Card winnercard = null;
                      
            //determine winnercard depending on instant wins

            if (player1Wins == true && player2Wins == false)
            {
                return card1;
            }
            else if (player1Wins == false && player2Wins == true)
            {
                return card2;
            }

            var doubleDamage_halfedDamage1 = checkElementEffects(card1.ElementType, card2.ElementType);
            var doubleDamage_halfedDamage2 = checkElementEffects(card2.ElementType, card1.ElementType);

            // real damage value should not be changed
            var card1DamageTemp = card1.Damage;
            var card2DamageTemp = card2.Damage;

            string damageLog1 = "";
            string damageLog2 = "";

            //temporarily alter damage value depending if doubled/halfed

            if (doubleDamage_halfedDamage1 == (true, false))
            {
                card1DamageTemp *= 2;
                damageLog1 = $"\n{card1.Name}s {card1.ElementType}-attack was very effective! {card1.Name} doubled its damage power!";
            }
            else if (doubleDamage_halfedDamage1 == (false, true))
            {
                card1DamageTemp /= 2;
                damageLog1 = $"\n{card1.Name}s {card1.ElementType}-attack was not effective! {card1.Name} damage power was halfed!";
            }
            else if (doubleDamage_halfedDamage2 == (true, false))
            {
                card2DamageTemp *= 2;
                damageLog2 = $"\n{card2.Name}s {card2.ElementType}-attack was very effective! {card2.Name} doubled its damage power!";
            }
            else if (doubleDamage_halfedDamage2 == (false, true))
            {
                card2DamageTemp /= 2;
                damageLog2 = $"\n{card2.Name}s {card2.ElementType}-attack was not effective! {card2.Name} damage power was halfed!";
            }

            string fightingLog = string.Concat(cardLog, damageLog1, damageLog2);

            response.statusMessage = fightingLog;
            response.SendResponse();

            return determineWinnerCard(card1, card2, card1DamageTemp, card2DamageTemp, response); 
            
        }

        public Card determineWinnerCard(Card card1, Card card2, double damage1, double damage2, HttpResponse response)
        {
            if(damage1 > damage2)
            {
                response.statusMessage = $"\n{card1.Name} has won with {damage1} against {damage2}!";
                response.SendResponse();
                return card1;
            }
            else if(damage1 < damage2)
            {
                response.statusMessage = $"\n{card2.Name} has won with {damage2} against {damage1}!";
                response.SendResponse();
                return card2;
            }
            else
            {
                response.statusMessage = $"\nThe cards damage are equal with {damage1} against {damage2}!";
                response.SendResponse();
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


        public bool checkSpecialMonsterEffects(Card card1, Card card2, HttpResponse response)
        {
            bool instantWinCard = false;
            Random rnd = new Random();
            

            if (card1.CardType == "Dragon" && card2.CardType == "Goblin")
            {
                instantWinCard = true;
                response.statusMessage = $"\nThe Goblin is too afraid to attack the Dragon!";
            }
            else if(card1.CardType == "Wizard" && card2.CardType == "Ork")
            {
                instantWinCard = true;
                response.statusMessage = $"\nThe Wizard uses his powers to control the Ork!";
            }
            else if(card1.CardType == "Spell" && card1.ElementType == ElementType.water && card2.CardType == "Knight")
            {
                response.statusMessage = $"\nThe Knights armor is too heavy! The WaterSpell has drowned the Knight.";
                instantWinCard = true;
            }
            else if (card1.CardType == "Kraken" && card2.CardType == "Spell")
            {
                response.statusMessage = $"\nThe Kraken is immune against the Spell!";
                instantWinCard = true;
            }
            else if(card1.CardType == "Elf" && card1.ElementType == ElementType.fire && card2.CardType == "Dragon")
            {
                int chanceThatDragonMisses = rnd.Next(2);
                if(chanceThatDragonMisses == 1)
                {
                    response.statusMessage = $"\nThe FireElf could evade the Dragons attack!";
                    instantWinCard = true;
                }
                else
                {
                    response.statusMessage = $"\nThe FireElf could not evade the Dragons attack!";
                    instantWinCard = false;
                }
            }
            response.SendResponse();

            return instantWinCard;

        }
    }
}
