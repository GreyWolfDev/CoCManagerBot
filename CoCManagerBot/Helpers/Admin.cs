﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CoCManagerBot.Model;

namespace CoCManagerBot.Helpers
{
    public static class Admin
    {
        public static string GetWarReport(WarResponse war, ClanResponse c)
        {
            var result = "War Report:";
            
            if (war.clan.stars > war.opponent.stars)
                result += " (Win)";
            if (war.clan.stars == war.opponent.stars)
            {
                //get average desctruction
                var us = war.clan.members.Sum(x => x.attacks.Sum(a => a.destructionPercentage)) / war.clan.attacks;
                var them = war.opponent.members.Sum(x => x.attacks.Sum(a => a.destructionPercentage)) / war.opponent.attacks;
                if (us > them)
                    result += " (Win)";
                if (us == them)
                    result += " (Draw)";
                if (us < them)
                    result += " (Lose)";
            }
            if (war.clan.stars < war.opponent.stars)
                result += " (Lose)";
            result += $" {war.clan.stars} to {war.opponent.stars}\n\nFlagged Members:\n\n";
            foreach (var m in war.clan.members)
            {
                var attacks = m.attacks?.Length ?? 0;
                //check which members did not attack at all
                if (attacks == 0)
                {
                    //no attack whatsoever
                    result += $"{m.name} no attacks.\n";
                    continue;
                }
                //check which members failed to get 2 stars
                var stars = m.attacks?.Sum(x => x.stars) ?? 0;
                if (stars < 2)
                {
                    result += $"{m.name} got {stars} stars.\n";
                }
                //check which members did not attack twice

                if (attacks == 1)
                {
                    result += $"{m.name} attacked once.\n";
                }
                //check which members attacked outside their mirrors
                var pos = m.mapPosition;
                if (m.attacks != null)
                    foreach (var attack in m.attacks)
                    {
                        var opp = war.opponent.members.FirstOrDefault(x => x.tag == attack.defenderTag)?.mapPosition ?? 0;
                        if (Math.Abs(opp - pos) > 2 && opp != 0)
                        {
                            result += $"{m.name} (#{pos}) attacked #{opp}\n";
                        }
                    }
            }


            return result;
        }
    }
}
