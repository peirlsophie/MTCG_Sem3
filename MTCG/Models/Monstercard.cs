using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MTCG_Peirl.Models
{
    public class Monstercard : Card
    {
        public Monstercard(string id, string name, double damage, ElementType elementType, string cardType)
            : base(id, name, damage, elementType, cardType) 
        {
            
        }
    }
}
