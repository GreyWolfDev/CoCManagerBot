using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoCManagerBot.Model
{
    public class WarRank
    {
        public string Name { get; set; }
        public long Id { get; set; }
        public double Rank { get; set; }
        public int WarCount { get; set; }
        public int AttacksMade { get; set; }
        public int StarsWon { get; set; }
        public int StarsLost { get; set; }
        public double AvgDestruction { get; set; }
        public int TH { get; set; }
    }
}
