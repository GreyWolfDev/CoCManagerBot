using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CoCManagerBot.Model;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace CoCManagerBot.Helpers
{
    public static class Admin
    {
        //public static string GetWarReport(Item war, ClanResponse c)
        //{
        //    var result = $"War Report for {c.name} vs {war.opponent.name}:\n";

        //    if (war.clan.stars > war.opponent.stars)
        //        result += "(Win)";
        //    var damage = "";
        //    if (war.clan.stars == war.opponent.stars)
        //    {
        //        //get average desctruction
        //        var us = war.clan.members.Where(x => x.attacks != null).Sum(x => x.attacks.Sum(a => a.destructionPercentage)) / war.clan.attacks;
        //        var them = war.opponent.members.Where(x => x.attacks != null).Sum(x => x.attacks.Sum(a => a.destructionPercentage)) / war.opponent.attacks;
        //        if (us > them)
        //            result += "(Win)";
        //        if (us == them)
        //            result += "(Draw)";
        //        if (us < them)
        //            result += "(Lose)";
        //        damage = $"{us:00.00}% vs {them:00.00}%";
        //    }
        //    if (war.clan.stars < war.opponent.stars)
        //        result += "(Lose)";
        //    result += $" {war.clan.stars} to {war.opponent.stars}";
        //    if (war.clan.stars == war.opponent.stars)
        //        result += $"\nAverage Damage: {damage}";
        //    result += "\n\nNotes:\n\n";
        //    foreach (var m in war.clan.members)
        //    {
        //        var attacks = m.attacks?.Length ?? 0;
        //        //check which members did not attack at all
        //        if (attacks == 0)
        //        {
        //            //no attack whatsoever
        //            result += $"{m.name} no attacks.\n";
        //            continue;
        //        }
        //        //check which members failed to get 2 stars
        //        var stars = m.attacks?.Sum(x => x.stars) ?? 0;
        //        if (stars < 2)
        //        {
        //            result += $"{m.name} got {stars} stars.\n";
        //        }
        //        //check which members did not attack twice

        //        if (attacks == 1)
        //        {
        //            result += $"{m.name} attacked once.\n";
        //        }
        //        //check which members attacked outside their mirrors
        //        var pos = m.mapPosition;
        //        if (m.attacks != null)
        //            foreach (var attack in m.attacks)
        //            {
        //                var opp = war.opponent.members.FirstOrDefault(x => x.tag == attack.defenderTag)?.mapPosition ?? 0;
        //                if (Math.Abs(opp - pos) > 2 && opp != 0)
        //                {
        //                    result += $"{m.name} (#{pos}) attacked #{opp}\n";
        //                }
        //            }
        //    }


        //    return result;

        //}

        public static string GetCWLReport(string myClan)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            //let's build an excel file!
            var path = $"{myClan.Replace("#", "")}.xlsx";
            
            if (File.Exists(path))
            {
                File.Delete(path);
            }
            var fi = new FileInfo(path);
            var headers = new[] { "TH", 
                "Name", 
                "Score",
                "Attacks Missed", 
                "Stars Gained", 
                "Stars Lost", 
                "Destruction" };
            using (var p = new ExcelPackage(fi))
            {

                var cwl = ApiService.Get<CWLResponse>(UrlConstants.GetCWLInfo, myClan).Result;
                Console.WriteLine($"Found CWL: {cwl.state} for season {cwl.season}");
                //now check each day for our clan
                var wars = new List<CWLWarResponse>();
                foreach (var r in cwl.rounds)
                {
                    foreach (var w in r.warTags)
                    {
                        //get this particular war
                        var war = ApiService.Get<CWLWarResponse>(UrlConstants.GetCWLWarInfo, w).Result;
                        //no reason to check if war is still active
                        if (war.state != "warEnded") continue;
                        //check to see if we are in this war
                        if (war.opponent.tag == myClan || war.clan.tag == myClan)
                        {
                            wars.Add(war);
                            continue;
                        }
                    }
                }
                Console.WriteLine($"Found {wars.Count()} completed wars for our clan");
                //setup sheets
                p.Workbook.Worksheets.Add("Summary");
                for (var i = 0; i < wars.Count(); i++)
                {
                    p.Workbook.Worksheets.Add($"War {i + 1}");
                }
                foreach (var s in p.Workbook.Worksheets)
                {
                    for (var i = 0; i < headers.Length; i++)
                    {
                        s.Cells[1, i + 1].Value = headers[i];
                    }
                }



                try
                {
                    var clan = cwl.clans.FirstOrDefault(x => x.tag == myClan);


                    var members = clan.members;
                    var blankAttack = new Attack
                    {
                        destructionPercentage = 0,
                        stars = 0
                    };
                    var result = new List<WarRank>();
                    //now we need to somehow join them togather....
                    foreach (var m in members.OrderByDescending(x => x.townhallLevel).ThenBy(x => x.name))
                    {
                        if (!wars.Any(x => x.clan.members.Concat(x.opponent.members).Any(z => z.tag == m.tag))) continue;
                        //get all wars this member has been in
                        var mw = wars;
                        var attacksMade = 0;
                        var warsIn = 0;
                        var totalStarsWon = 0;
                        var totalStarsLost = 0;
                        double avgDestruction = 0.00;
                        var scores = new List<double>();
                        var curWar = 0;
                        foreach (var w in mw)
                        {
                            curWar++;
                            var us = (w.clan.tag == myClan) ? w.clan : w.opponent;
                            var opponent = (w.clan.tag == myClan) ? w.opponent : w.clan;
                            var them = us.members.FirstOrDefault(x => x.tag == m.tag);
                            if (them == null) continue;
                            warsIn++;
                            var wk = p.Workbook.Worksheets[$"War {curWar}"];
                            var row = wk.Dimension.End.Row + 1;
                            wk.Cells[row, 2].Value = them.name;
                            var starsLost = them.bestOpponentAttack?.stars ?? 0;
                            wk.Cells[row, 1].Value = them.townhallLevel;
                            wk.Cells[row, 6].Value = starsLost;
                            totalStarsLost += starsLost;
                            var totalStars = them.attacks?.Sum(x => x.stars) ?? 0;
                            wk.Cells[row, 5].Value = totalStars;
                            totalStarsWon += totalStars;
                            var destruction = them.attacks?.Average(x => x.destructionPercentage) ?? 0;
                            wk.Cells[row, 7].Value = $"{destruction:00}%";
                            avgDestruction += destruction;
                            var rank = them.mapPosition;
                            var netStars = totalStars - starsLost;
                            //who they attacked
                            double aRav = 35;
                            if ((them.attacks?.Length ?? 0) > 0)
                            {
                                attacksMade++;
                                var a1m = opponent.members.FirstOrDefault(x => x.tag == them.attacks[0].defenderTag);

                                Member a2m;
                                if (them.attacks.Length == 2)
                                {
                                    attacksMade++;
                                    a2m = opponent.members.FirstOrDefault(x => x.tag == them.attacks[1].defenderTag);
                                    aRav = (a1m.mapPosition + a2m.mapPosition) / (double)2;
                                }
                                else
                                    aRav = a1m.mapPosition;
                                //average rank of those attacks

                            }
                            var defenseMembers = opponent.members.Where(x => x.attacks != null && x.attacks.Any(a => a.defenderTag == m.tag)).ToList();
                            var defenses = defenseMembers.SelectMany(x => x.attacks.Where(a => a.defenderTag == m.tag)).ToList();
                            var dRav = defenseMembers.Any() ? defenseMembers.Average(x => x.mapPosition) : 1;
                            if (dRav == 0) dRav = 1;
                            var numD = defenses.Count();
                            //var nD = numD - starsLost;

                            //now some fun....
                            var dPower = (10 * (double)numD) * ((double)1 / (double)5);
                            var rawPower = 10 * ((((((totalStars + totalStars) * ((double)4 / (double)6)) - ((starsLost * 2) - 3)) + ((15 - aRav) * (1 / 13.5))) + ((15 - dRav) * (0.5 / 14))) + (numD * (0.5 / 5)));
                            var unStarred = (starsLost > 0) ? rawPower + 10 : rawPower;
                            var unRaided = (numD == 0) ? rawPower + 10 : rawPower;
                            var defBonus = (numD > 0 && starsLost > 0) ? rawPower + dPower : rawPower;
                            var powerFinal = Math.Max(defBonus, Math.Max(unRaided, unStarred));
                            scores.Add(powerFinal);
                            wk.Cells[row, 3].Value = $"{powerFinal:00}";
                            wk.Cells[row, 4].Value = 1 - them.attacks?.Length ?? 1;
                            //Console.WriteLine(powerFinal);
                            
                            //calculate some things....

                        }
                        //don't drop lowest, this is CWL!
                        //if (scores.Count() > 1)
                        //{
                        //    //drop lowest score, to be nice
                        //    scores.Remove(scores.Min());
                        //}

                        var avgScore = scores.Average();

                        var name = m.name;

                        var thisPlayer = new WarRank
                        {
                            Id = 0,
                            Name = name,
                            Rank = avgScore,
                            WarCount = warsIn,
                            AttacksMade = attacksMade,
                            AvgDestruction = avgDestruction / attacksMade,
                            StarsLost = totalStarsLost,
                            StarsWon = totalStarsWon,
                            TH = m.townhallLevel
                        };
                        result.Add(thisPlayer);

                        //Console.WriteLine($"{m.name}: {avgScore}");

                        //var memberStats = mw.SelectMany(x => x.clan.members.Where(z => z.tag == m.tag)).ToList();
                        //var attacks = new List<Attack>();

                        //foreach (var s in memberStats)
                        //{
                        //    if (s.attacks == null)
                        //    {
                        //        attacks.Add(blankAttack);
                        //        attacks.Add(blankAttack);
                        //    }
                        //    else
                        //    {
                        //        foreach (var a in s.attacks)
                        //        {
                        //            attacks.Add(a);
                        //        }
                        //        if (s.attacks.Count() == 1)
                        //        {
                        //            attacks.Add(blankAttack);
                        //        }
                        //    }
                        //}
                        ////var attacks = memberStats.Where(x => x.attacks != null).SelectMany(x => x.attacks).Where(x => x != null);

                        //var starAvg = attacks.Average(x => x.stars);
                        //var destAvg = attacks.Average(x => x.destructionPercentage);
                        //var starsLost = memberStats.Average(x => x.bestOpponentAttack?.stars);

                        //Console.WriteLine($"{m.name} - {mw.Count} Wars with {attacks.Count(x => x.attackerTag != null)} attacks\nAVG Stars: {starAvg:N2} | AVG Destruction: {destAvg:N2} | AVG Stars Lost: {starsLost:N2}\n");
                        ////calculate a war ranking somehow...

                    }
                    var sum = p.Workbook.Worksheets.First();
                    
                    var curRow = 2;
                    foreach (var r in result.OrderByDescending(x => x.Rank))
                    {
                        sum.Cells[curRow, 1].Value = r.TH;
                        sum.Cells[curRow, 2].Value = r.Name;
                        sum.Cells[curRow, 3].Value = $"{r.Rank:00}";
                        sum.Cells[curRow, 4].Value = (r.WarCount) - r.AttacksMade;
                        sum.Cells[curRow, 5].Value = r.StarsWon;
                        sum.Cells[curRow, 6].Value = r.StarsLost;
                        sum.Cells[curRow, 7].Value = double.IsNaN(r.AvgDestruction) ? "0%" : $"{r.AvgDestruction:00}%";
                        curRow++;
                        Console.WriteLine($"{r.Name} | Score: {(int)r.Rank}\n{(r.WarCount) - r.AttacksMade} attacks missed\n" +
                            $"Stars - Won: {r.StarsWon}, Lost: {r.StarsLost} | Average Destruction: {r.AvgDestruction:00}%\n------------------\n\n");
                    }
                    foreach (var sheet in p.Workbook.Worksheets)
                        sheet.Cells.AutoFitColumns();
                    //save the workbook
                    p.Save();

                }
                catch (Exception e)
                {
                    Console.WriteLine("Error calculating war ranks: " + e.Message);
                    p.Dispose();
                }
            }
            return path;
        }

        public static string GetWarReport(WarResponse war, ClanResponse c)
        {
            var result = $"War Report for {c.name} vs {war.opponent.name}:\n";

            if (war.clan.stars > war.opponent.stars)
                result += "(Win)";
            var damage = "";
            if (war.clan.stars == war.opponent.stars)
            {
                //get average desctruction
                var us = war.clan.members.Where(x => x.attacks != null).Sum(x => x.attacks.Sum(a => a.destructionPercentage)) / war.clan.attacks;
                var them = war.opponent.members.Where(x => x.attacks != null).Sum(x => x.attacks.Sum(a => a.destructionPercentage)) / war.opponent.attacks;
                if (us > them)
                    result += "(Win)";
                if (us == them)
                    result += "(Draw)";
                if (us < them)
                    result += "(Lose)";
                damage = $"{us:00.00}% vs {them:00.00}%";
            }
            if (war.clan.stars < war.opponent.stars)
                result += "(Lose)";
            result += $" {war.clan.stars} to {war.opponent.stars}";
            if (war.clan.stars == war.opponent.stars)
                result += $"\nAverage Damage: {damage}";
            result += "\n\nNotes:\n\n";
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
