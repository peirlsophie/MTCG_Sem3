using MTCG_Peirl.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MTCG.Businesslogic 
{ 
    public class Trade 
    { 
        public string Id{ get; set; } 
        public string CardToTrade { get; set; } 
        public string Type { get; set; } 
        public int MinimumDamage { get; set; } 
       
        public Trade(string Id, string CardToTrade, string Type, int MinimumDamage) 
        {
            this.Id = Id;
            this.CardToTrade = CardToTrade;
            this.Type = Type;
            this.MinimumDamage = MinimumDamage;
        } 
    } 
}
