using System;

namespace MTCG_Peirl.Models
{
    public enum ElementType
    {
        water,
        fire,
        normal
    }

    public class Card
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public double Damage { get; set; }
        public ElementType ElementType { get; set; }
        public string CardType { get; set; }
        

        public Card(string id, string name, double damage, ElementType elementType, string cardType )
        {
            Id = id;
            Name = name;
            Damage = damage;
            ElementType = elementType;
            CardType = cardType;
            
        }
    }
}
