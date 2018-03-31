using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoCManagerBot.Model
{

    public class PlayerResponse
    {
        public int id { get; set; }
        public string tag { get; set; }
        public string name { get; set; }
        public int townHallLevel { get; set; }
        public int expLevel { get; set; }
        public int trophies { get; set; }
        public int bestTrophies { get; set; }
        public int warStars { get; set; }
        public int attackWins { get; set; }
        public int defenseWins { get; set; }
        public int builderHallLevel { get; set; }
        public int versusTrophies { get; set; }
        public int bestVersusTrophies { get; set; }
        public int versusBattleWins { get; set; }
        public string role { get; set; }
        public int donations { get; set; }
        public int donationsReceived { get; set; }
        public Clan clan { get; set; }
        public League league { get; set; }
        public Achievement[] achievements { get; set; }
        public int versusBattleWinCount { get; set; }
        public Troop[] troops { get; set; }
        public Hero[] heroes { get; set; }
        public Spell[] spells { get; set; }
        public string reason { get; set; }

        public double WarRating { get; set; }
    }

    public class Achievement
    {
        public string name { get; set; }
        public int stars { get; set; }
        public int value { get; set; }
        public int target { get; set; }
        public string info { get; set; }
        public string completionInfo { get; set; }
        public string village { get; set; }
    }

    public class Troop
    {
        public string name { get; set; }
        public int level { get; set; }
        public int maxLevel { get; set; }
        public string village { get; set; }
    }

    public class Hero
    {
        public string name { get; set; }
        public int level { get; set; }
        public int maxLevel { get; set; }
        public string village { get; set; }
    }

    public class Spell
    {
        public string name { get; set; }
        public int level { get; set; }
        public int maxLevel { get; set; }
        public string village { get; set; }
    }

}
