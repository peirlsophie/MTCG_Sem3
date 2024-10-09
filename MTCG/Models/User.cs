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
        public string Username { get;  set; }
        public string Password { get; set; }
        public int highscore;
        public int ELO;
        public int coins;
        public Stack<Card> ownedCards;

        public User(string Username, string Password)
        {
            this.Username = Username;
            this.Password = Password;
            highscore = 0;
            ELO = 0;
            coins = 20;
            ownedCards = new Stack<Card>();
        }

    }


}
