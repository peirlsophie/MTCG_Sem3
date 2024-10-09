using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MTCG_Peirl.Models
{
    public class Spellcard : Card
    {
        public Spellcard(string name, int damage, elementType type)
            : base(name, damage, type) // Call the base Card constructor
        {
            // Additional initialization for Spellcard can go here if needed
        }
    }
}