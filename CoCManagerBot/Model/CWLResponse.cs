using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoCManagerBot.Model
{

    public class CWLResponse
    {
        public string state { get; set; }
        public string season { get; set; }
        public Clan[] clans { get; set; }
        public Round[] rounds { get; set; }
    }

    public class CWLClan
    {
        public string tag { get; set; }
        public string name { get; set; }
        public int clanLevel { get; set; }
        public Badgeurls badgeUrls { get; set; }
        public Member[] members { get; set; }
    }

    public class CWLMember
    {
        public string tag { get; set; }
        public string name { get; set; }
        public int townHallLevel { get; set; }
    }

    public class Round
    {
        public string[] warTags { get; set; }
    }

}
