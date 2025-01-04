using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace MTCG_Peirl.Models
{
    public class User
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public int coins { get;  set; }

        public int Highscore { get;  set; }

        public int ELO { get; set; }

        public int games_played { get;  set; }
        public int Wins { get;  set; }
        public int Losses { get;  set; }

        public Stack<Card> ownedCards { get;  set; }
        

        public User(string username, string password, int coins, int highscore, int elo, int games_played, int wins, int losses)
        {
            Username = username;
            Password = password;
            coins = coins;
            Highscore = highscore;
            ELO = elo;
            games_played = games_played;
            Wins = wins;
            Losses = losses;

            ownedCards = new Stack<Card>();
            
        }
        public string getUsername() {  return this.Username; }
        
       
        public int GetELO() { return ELO; }
        public int GetCoins() { return coins; }

        public void UseCoins()
        {
            if (coins >= 5)
            {
                coins -= 5;
                
            }
            return;
        }

        public List<string> GetOwnedCards() 
        { 
            List<string> cardNames = new List<string>(); 
            foreach (Card card in ownedCards) 
            { 
                cardNames.Add(card.Name); 
            } 
            return cardNames; 
        }

      
        public void addPackageCardsToStack(Package newPackage)
        {
            foreach (Card card in newPackage.cards)
            {
                ownedCards.Push(card);
            }
        }        

        
    
    
    }


}
