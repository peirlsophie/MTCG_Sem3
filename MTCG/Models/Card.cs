using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MTCG_Peirl.Models
{
    public enum elementType
    {
        water,
        fire,
        normal
    }

    public abstract class Card
    {
        private string cardName;
        int cardDamage;
        private elementType cardType;



        public Card(string name, int damage, elementType type)
        {
            cardName = name;
            cardDamage = damage;
            cardType = type;

        }
    }



}
