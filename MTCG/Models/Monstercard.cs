using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MTCG_Peirl.Models
{
    public class Monstercard : Card
    {
        public Monstercard(string name, int damage, elementType type)
            : base(name, damage, type) 
        {
            
        }
    }
}
