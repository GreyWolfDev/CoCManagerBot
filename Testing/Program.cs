using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CoCManagerBot;
using CoCManagerBot.Model;
using CoCManagerBot.Helpers;

namespace Testing
{
    class Program
    {
        static void Main(string[] args)
        {
            string myClan = "#2QVV8YG8Q";
            //get current CWL
            //var cwl = ApiService.Get<CWLResponse>(UrlConstants.GetCWLInfo, myClan).Result;
            //Console.WriteLine($"Found CWL: {cwl.state} for season {cwl.season}");
            ////now check each day for our clan
            //var wars = new List<CWLWarResponse>();
            //foreach (var r in cwl.rounds)
            //{ 
            //    foreach (var w in r.warTags)
            //    {
            //        //get this particular war
            //        var war = ApiService.Get<CWLWarResponse>(UrlConstants.GetCWLWarInfo, w).Result;
            //        //no reason to check if war is still active
            //        if (war.state != "warEnded") continue;
            //        //check to see if we are in this war
            //        if (war.opponent.tag == myClan || war.clan.tag == myClan)
            //        {
            //            wars.Add(war);
            //            continue;
            //        }
            //    }
            //}
            //Console.WriteLine($"Found {wars.Count()} completed wars for our clan");

            //try
            //{
            //    var clan = cwl.clans.FirstOrDefault(x => x.tag == myClan);


            //    var members = clan.members;
            //    var blankAttack = new Attack
            //    {
            //        destructionPercentage = 0,
            //        stars = 0
            //    };
            //    var result = new List<WarRank>();
            //    //now we need to somehow join them togather....
            //    foreach (var m in members)
            //    {
            //        if (!wars.Any(x => x.clan.members.Any(z => z.tag == m.tag))) continue;
            //        //get all wars this member has been in
            //        var mw = wars;
            //        var attacksMade = 0;
            //        var warsIn = 0;
            //        var totalStarsWon = 0;
            //        var totalStarsLost = 0;
            //        double avgDestruction = 0.00;
            //        var scores = new List<double>();
            //        foreach (var w in mw)
            //        {
            //            var us = (w.clan.tag == myClan) ? w.clan : w.opponent;
            //            var opponent = (w.clan.tag == myClan) ? w.opponent : w.clan;
            //            var them = us.members.FirstOrDefault(x => x.tag == m.tag);
            //            if (them == null) continue;
            //            warsIn++;
            //            var starsLost = them.bestOpponentAttack?.stars ?? 0;
            //            totalStarsLost += starsLost;
            //            var totalStars = them.attacks?.Sum(x => x.stars) ?? 0;
            //            totalStarsWon += totalStars;
            //            avgDestruction += them.attacks?.Average(x => x.destructionPercentage) ?? 0;
            //            var rank = them.mapPosition;
            //            var netStars = totalStars - starsLost;
            //            //who they attacked
            //            double aRav = 35;
            //            if (them.attacks != null)
            //            {
            //                attacksMade++;
            //                var a1m = opponent.members.FirstOrDefault(x => x.tag == them.attacks[0].defenderTag);

            //                Member a2m;
            //                if (them.attacks.Length == 2)
            //                {
            //                    attacksMade++;
            //                    a2m = opponent.members.FirstOrDefault(x => x.tag == them.attacks[1].defenderTag);
            //                    aRav = (a1m.mapPosition + a2m.mapPosition) / (double)2;
            //                }
            //                else
            //                    aRav = a1m.mapPosition;
            //                //average rank of those attacks

            //            }
            //            var defenseMembers = opponent.members.Where(x => x.attacks != null && x.attacks.Any(a => a.defenderTag == m.tag)).ToList();
            //            var defenses = defenseMembers.SelectMany(x => x.attacks.Where(a => a.defenderTag == m.tag)).ToList();
            //            var dRav = defenseMembers.Any() ? defenseMembers.Average(x => x.mapPosition) : 1;
            //            if (dRav == 0) dRav = 1;
            //            var numD = defenses.Count();
            //            //var nD = numD - starsLost;

            //            //now some fun....
            //            var dPower = (10 * (double)numD) * ((double)1 / (double)5);
            //            var rawPower = 10 * (((((totalStars * ((double)4 / (double)6)) - (starsLost - 3)) + ((15 - aRav) * (1 / 13.5))) + ((15 - dRav) * (0.5 / 14))) + (numD * (0.5 / 5)));
            //            var unStarred = (starsLost > 0) ? rawPower + 10 : rawPower;
            //            var unRaided = (numD == 0) ? rawPower + 10 : rawPower;
            //            var defBonus = (numD > 0 && starsLost > 0) ? rawPower + dPower : rawPower;
            //            var powerFinal = Math.Max(defBonus, Math.Max(unRaided, unStarred));
            //            scores.Add(powerFinal);
            //            //Console.WriteLine(powerFinal);

            //            //calculate some things....

            //        }
            //        //don't drop lowest, this is CWL!
            //        //if (scores.Count() > 1)
            //        //{
            //        //    //drop lowest score, to be nice
            //        //    scores.Remove(scores.Min());
            //        //}

            //        var avgScore = scores.Average();

            //        var name = m.name;

            //        var thisPlayer = new WarRank
            //        {
            //            Id = 0,
            //            Name = name,
            //            Rank = avgScore,
            //            WarCount = warsIn,
            //            AttacksMade = attacksMade,
            //            AvgDestruction = avgDestruction / attacksMade,
            //            StarsLost = totalStarsLost,
            //            StarsWon = totalStarsWon
            //        };
            //        result.Add(thisPlayer);

            //        //Console.WriteLine($"{m.name}: {avgScore}");

            //        //var memberStats = mw.SelectMany(x => x.clan.members.Where(z => z.tag == m.tag)).ToList();
            //        //var attacks = new List<Attack>();

            //        //foreach (var s in memberStats)
            //        //{
            //        //    if (s.attacks == null)
            //        //    {
            //        //        attacks.Add(blankAttack);
            //        //        attacks.Add(blankAttack);
            //        //    }
            //        //    else
            //        //    {
            //        //        foreach (var a in s.attacks)
            //        //        {
            //        //            attacks.Add(a);
            //        //        }
            //        //        if (s.attacks.Count() == 1)
            //        //        {
            //        //            attacks.Add(blankAttack);
            //        //        }
            //        //    }
            //        //}
            //        ////var attacks = memberStats.Where(x => x.attacks != null).SelectMany(x => x.attacks).Where(x => x != null);

            //        //var starAvg = attacks.Average(x => x.stars);
            //        //var destAvg = attacks.Average(x => x.destructionPercentage);
            //        //var starsLost = memberStats.Average(x => x.bestOpponentAttack?.stars);

            //        //Console.WriteLine($"{m.name} - {mw.Count} Wars with {attacks.Count(x => x.attackerTag != null)} attacks\nAVG Stars: {starAvg:N2} | AVG Destruction: {destAvg:N2} | AVG Stars Lost: {starsLost:N2}\n");
            //        ////calculate a war ranking somehow...

            //    }
            //    foreach (var r in result.OrderByDescending(x => x.Rank))
            //    {
            //        Console.WriteLine($"{r.Name} | Score: {(int)r.Rank}\n{(r.WarCount) - r.AttacksMade} attacks missed\n" +
            //            $"Stars - Won: {r.StarsWon}, Lost: {r.StarsLost} | Average Destruction: {r.AvgDestruction:00}%\n------------------\n\n");
            //    }

            //    //let's build an excel file!

            //}
            //catch (Exception e)
            //{
            //    Console.WriteLine("Error calculating war ranks: " + e.Message);

            //}
            //Console.WriteLine("Writing File...");
            //Admin.GetCWLReport(myClan);
            //Console.WriteLine("Done");
            GetCWLClanInfo(myClan);
            Console.Read();
        }

        public static void GetCWLWars(string myClan)
        { 

        }

        public static void GetCWLClanInfo(string myClan)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            var cwl = ApiService.Get<CWLResponse>(UrlConstants.GetCWLInfo, myClan).Result;
            Console.WriteLine($"Found CWL: preparation for season {cwl.season}");

            //choose clan to look at
            //var ix = 1;
            //foreach (var c in cwl.clans)
            //{
            //    Console.WriteLine($"{ix}: {c.name}");
            //    ix++;
            //}
            //var choice = int.Parse(Console.ReadLine()) - 1;
            //var clan = cwl.clans[choice].tag;

            //now check each day for our clan
            var clanInfo = new Clan();
            var myClanInfo = new Clan();
            var clans = new Clan[8];
            var i = 0;
            foreach (var r in cwl.rounds.Where(x => x.warTags.All(t => t != "#0")))
            {
                i = 0;
                foreach (var w in r.warTags)
                {
                    if (w == "#0") break;
                    //get this particular war
                    var war = ApiService.Get<CWLWarResponse>(UrlConstants.GetCWLWarInfo, w).Result;
                    //no reason to check if war is still active
                    //if (war.state != "warEnded") continue;
                    //check to see if we are in this war
                    if (war.state != "preparation") break;
                    clans[i] = war.clan;
                    i++;
                    clans[i] = war.opponent;
                    i++;
                    //if (war.opponent.tag == myClan)
                    //{
                    //    clanInfo = war.clan;
                    //    myClanInfo = war.opponent;
                    //    break;
                    //}
                    //if (war.clan.tag == myClan)
                    //{
                    //    clanInfo = war.opponent;
                    //    myClanInfo = war.clan;
                    //    break;
                    //}
                }
            }
            
            foreach (var c in clans)
            {
                Console.WriteLine($"{c.name}");
                foreach (var m in c.members.OrderBy(x => x.mapPosition))
                {
                    Console.WriteLine($"{m.townhallLevel} : {m.name}");
                }
                Console.WriteLine($"\nTotal TH level for clan: {c.members.Sum(x => x.townhallLevel)}");
                Console.WriteLine("----------------------\n");
            }
            Console.WriteLine("Total / avg townhall levels for this CWL war:");
            foreach (var c in clans.OrderByDescending(c => c.members.Sum(x => x.townhallLevel)))
            {
                Console.WriteLine($"{c.members.Sum(x => x.townhallLevel)} / {c.members.Average(x => x.townhallLevel):00.00} - {c.name}");
            }


            //Console.WriteLine($"Next war, vs {clanInfo.name}");

            //Console.WriteLine("Enemy Townhall levels:\n");
            //for (var i = 0; i < 15; i++)
            //{
            //    var us = myClanInfo.members.OrderBy(x => x.mapPosition).ToArray()[i];
            //    var them = clanInfo.members.OrderBy(x => x.mapPosition).ToArray()[i];
            //    Console.WriteLine($"{us.name} | {us.townhallLevel}  VS  {them.townhallLevel} | {them.name}");
            //}
            //foreach (var m in clanInfo.members.OrderBy(x => x.mapPosition))
            //{
            //    Console.WriteLine(m.townhallLevel + " : " + m.name);
            //}

            //create dictionary
            //var grouping = clanInfo.members.GroupBy(x => x.townhallLevel);
            //Console.WriteLine("\nCounts:");
            //foreach (var g in grouping.OrderByDescending(x => x.Key))
            //{
            //    Console.WriteLine($"{g.Key}: {g.Count()}");
            //}
        }
    }
}
