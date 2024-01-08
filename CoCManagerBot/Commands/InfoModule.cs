using CoCManagerBot.Helpers;
using CoCManagerBot.Model;
using Discord;
using Discord.Commands;
using Discord.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;

namespace CoCManagerBot.Commands
{
    public class InfoModule : ModuleBase<SocketCommandContext>
    {

        // ~say hello world -> hello world
        [Command("register")]
        [Summary("Register a clan tag")]
        [Priority(0)]
        public Task SayAsync([Remainder][Summary("Register a clan tag")] string tag)
        {
            //check if this server has an entry in the database
            var clans = Program.DB.GetCollection<ClanResponse>("clans");
            var servers = Program.DB.GetCollection<DiscordServer>("guilds");
            var s = servers.FindOne(x => x.ServerId == (long)Context.Guild.Id);
            if (String.IsNullOrWhiteSpace(tag))
            {
                //check if this server has a clan already associated
                if (s != null)
                {
                    tag = s.ClanTag;
                }
            }
            if (!tag.StartsWith("#"))
                tag = "#" + tag;
            var clan = ApiService.Get<ClanResponse>(UrlConstants.GetClanInformationUrlTemplate, tag).Result;
            if (clan.reason != null)
            {
                ReplyAsync(clan.reason);
                return Task.CompletedTask;
            }
            //TODO: remove old server if exists


            if (s == null)
            {
                servers.EnsureIndex(x => x.ServerId);
                s = new DiscordServer
                {
                    ServerId = (long)Context.Guild.Id,
                    ClanTag = tag,
                    PrimaryChannelId = (long)Context.Channel.Id
                };
                servers.Insert(s);
            }
            var c = clans.FindOne(x => x.tag == tag);
            if (c == null)
            {
                ReplyAsync("Clan found, please wait");
                //clan.RegisteredBy = e.Message.From.Username;
                clans.EnsureIndex(x => x.tag, true);
                clans.Insert(clan);
            }
            else
            {
                ReplyAsync("Clan already found in database, setting this channel to primary notification channel");
                c.DiscordServerId = s.id;
                s.PrimaryChannelId = (long)Context.Channel.Id;
                s.AnnouncementChannelId = s.PrimaryChannelId;
                servers.Update(s);
                c.HasError = false;
                clans.Update(c);
            }

            //now check to see if the clan wars are visible



            return Task.CompletedTask;
        }

        [Command("recheck")]
        [Summary("Checks for errors")]
        public Task RecheckAsync()
        {
            var servers = Program.DB.GetCollection<DiscordServer>("guilds");
            var s = servers.FindOne(x => x.ServerId == (long)Context.Guild.Id);
            string tag = null;
            if (s != null)
            {
                tag = s.ClanTag;
            }

            if (String.IsNullOrWhiteSpace(tag))
            {
                ReplyAsync("Please send !register with your clan tag, for example: !register #00000000");
                return Task.CompletedTask;
            }
            var clans = Program.DB.GetCollection<ClanResponse>("clans");
            var check = ApiService.Get<WarResponse>(UrlConstants.GetCurrentWarInformationTemplate,
                                tag).Result;
            if (check.reason != null && !check.reason.Contains("inMaintenance"))
            {
                ReplyAsync($"Still unable to get war log.  Reason: {check.reason}");
            }
            else
            {
                var c = clans.FindOne(x => x.tag == tag);
                if (c != null)
                {
                    c.HasError = false;
                    c.MembersNotifiedOfWarPrep = false;
                    c.MembersNotifiedOfWarStart = false;
                    c.WarFinalized = false;
                    clans.Update(c);
                    ReplyAsync("I was able to pull your war information!  Clan is now being monitored");
                }
            }
            return Task.CompletedTask;
        }

        [Command("register")]
        [Summary("Register a clan tag")]
        [Priority(1)]
        public Task SayAsync()
        {
            var servers = Program.DB.GetCollection<DiscordServer>("guilds");
            var s = servers.FindOne(x => x.ServerId == (long)Context.Guild.Id);
            string tag = null;
            //check if this server has a clan already associated
            if (s != null)
            {
                tag = s.ClanTag;
            }

            if (String.IsNullOrWhiteSpace(tag))
            {
                ReplyAsync("Please send !register with your clan tag, for example: !register #00000000");
                return Task.CompletedTask;
            }
            return SayAsync(tag);
        }

        [Command("announce")]
        [Summary("Set announcement channel")]
        public Task AnnounceAsync()
        {
            var servers = Program.DB.GetCollection<DiscordServer>("guilds");
            var s = servers.FindOne(x => x.ServerId == (long)Context.Guild.Id);
            string tag = null;
            //check if this server has a clan already associated
            if (s != null)
            {
                tag = s.ClanTag;
            }

            if (String.IsNullOrWhiteSpace(tag))
            {
                ReplyAsync("Please use !register with your clan tag, for example: !register #00000000\n" +
                    "Do this in the channel you want war reports / etc to show in.  You can use !announce to set a channel to announce when wars are starting.");
                return Task.CompletedTask;
            }

            s.AnnouncementChannelId = (long)Context.Channel.Id;
            servers.Update(s);
            ReplyAsync("Announcement channel set");
            return Task.CompletedTask;
        }

        [Command("warflag")]
        [Summary("Check war report")]
        public Task WarFlagAsync()
        {
            var clans = Program.DB.GetCollection<ClanResponse>("clans");
            var servers = Program.DB.GetCollection<DiscordServer>("guilds");
            var s = servers.FindOne(x => x.ServerId == (long)Context.Guild.Id);
            if (s == null || String.IsNullOrEmpty(s.ClanTag))
            {
                ReplyAsync("I don't have a clan registered with this server.  Please use the !register command in whichever channel you wish to get notifications in");
                return Task.CompletedTask;
            }
            var c = clans.FindOne(x => x.tag == s.ClanTag);
            if (c == null)
            {
                ReplyAsync("I'm not finding a clan for you :(");
                return Task.CompletedTask;
            }

            var wars = Program.DB.GetCollection<WarResponse>("wars").Find(x => x.clan.tag == c.tag && x.state == "warEnded").OrderByDescending(x => x.endTime);
            if (wars.Count() == 0)
            {
                ReplyAsync("I have no wars on record for your clan");
                return Task.CompletedTask;
            }
            //get most recent war
            var war = wars.OrderByDescending(x => x.endTime).First();
            var report = Admin.GetWarReport(war, c);
            ReplyAsync(report);
            return Task.CompletedTask;
        }

        [Command("war")]
        [Summary("Check war report")]
        public Task WarAsync()
        {
            var clans = Program.DB.GetCollection<ClanResponse>("clans");
            var servers = Program.DB.GetCollection<DiscordServer>("guilds");
            var s = servers.FindOne(x => x.ServerId == (long)Context.Guild.Id);
            if (s == null || String.IsNullOrEmpty(s.ClanTag))
            {
                ReplyAsync("I don't have a clan registered with this server.  Please use the !register command in whichever channel you wish to get notifications in");
                return Task.CompletedTask;
            }
            var c = clans.FindOne(x => x.tag == s.ClanTag);
            if (c == null)
            {
                ReplyAsync("I'm not finding a clan for you :(");
                return Task.CompletedTask;
            }
            var war = ApiService.Get<WarResponse>(UrlConstants.GetCurrentWarInformationTemplate,
                                c.tag).Result;

            if (war.reason != null)
            {
                c.HasError = true;
                clans.Update(c);
                if (war.reason.Contains("accessDenied"))
                {
                    //find the players of the clan

                    ReplyAsync("It appears your clan has its war log settings to private.  Ask the leader or co leader to set it to public if you'd like to use this bot.\n\nYou can use the !register command once they have to check your clan settings again and get it monitoring.");

                }
            }
            var response = "";
            //let's take a quick look
            var state = war.state;

            state = char.ToUpper(state[0]) + state.Substring(1);
            var r = new Regex(@"(?<=[A-Z])(?=[A-Z][a-z]) | (?<=[^A-Z])(?=[A-Z]) | (?<=[A-Za-z])(?=[^A-Za-z])", RegexOptions.IgnorePatternWhitespace);
            state = r.Replace(state, " ");

            if (state == "War Ended")
            {
                if (war.clan.stars > war.opponent.stars)
                    state += " (Win)";
                if (war.clan.stars == war.opponent.stars)
                {
                    //get average desctruction
                    var us = war.clan.members.Sum(x => x.attacks.Sum(a => a.destructionPercentage)) / war.clan.attacks;
                    var them = war.opponent.members.Sum(x => x.attacks.Sum(a => a.destructionPercentage)) / war.opponent.attacks;
                    if (us > them)
                        state += " (Win)";
                    if (us == them)
                        state += " (Draw)";
                    if (us < them)
                        state += " (Lose)";
                }
                if (war.clan.stars < war.opponent.stars)
                    state += " (Lose)";
            }
            if (war.state == "notInWar")
            {
                response = "You are currently not in war.  Displaying results of previous war instead.\n";

                war = Program.DB.GetCollection<WarResponse>("wars").Find(x => x.clan.tag == c.tag)
                    .OrderByDescending(x => x.endTime).FirstOrDefault();
            }
            if (war == null)
            {
                response += "No wars on record for you clan, sorry!";
                ReplyAsync(response);
                return Task.CompletedTask;
            }
            //update war in db
            war.Upsert();
            //Program.DB.GetCollection<WarResponse>("wars").Upsert(war);
            response += $"Stage: {state}" +
                           $"\n{war.teamSize} v {war.teamSize}" +
                           $"\n----------------------------------" +
                           $"\n{war.clan.name}: {war.clan.stars} ⭐️" +
                           $"\n{war.clan.attacks} attacks made out of {war.teamSize * 2}" +
                           $"\n----------------------------------" +
                           $"\n{war.opponent.name}: {war.opponent.stars} ⭐️" +
                           $"\n{war.opponent.attacks} attacks made out of {war.teamSize * 2}" +
                           $"\n----------------------------------" +
                           $"\nMembers:" +
                           $"\n----------------------------------";

            foreach (var member in war.clan.members.OrderBy(x => x.mapPosition))
            {
                response += $"\nTH{member.townhallLevel} {member.name}";
                if (war.state != "preparation")
                {
                    response +=
                        $"\n{member.attacks?.Length ?? 0} ⚔️ - {member.attacks?.Sum(x => x.stars) ?? 0} ⭐️ ({member.attacks?.Average(x => x.destructionPercentage) ?? 0:N0}% damage)";
                }
                else
                {
                    var opp = war.opponent.members.OrderBy(x => x.mapPosition)
                        .ElementAt(member.mapPosition - 1);

                    response += $"\nVS\nTH{opp.townhallLevel} {opp.name}";
                }
                response += $"\n--------------------";
            }

            ReplyAsync(response);
            return Task.CompletedTask;
        }

        [Command("cwlnext")]
        [Summary("Get CWL next war info (peek)")]
        public async Task CwlNextAsync()
        {
            try
            {
                var clans = Program.DB.GetCollection<ClanResponse>("clans");
                var servers = Program.DB.GetCollection<DiscordServer>("guilds");
                var s = servers.FindOne(x => x.ServerId == (long)Context.Guild.Id);
                var emotes = Context.Guild.Emotes.ToList();
                if (s == null || String.IsNullOrEmpty(s.ClanTag))
                {
                    await ReplyAsync("I don't have a clan registered with this server.  Please use the !register command in whichever channel you wish to get notifications in");

                }

                var myClan = s.ClanTag;
                var toSend = "";
                var buffer = "";
                var cwl = ApiService.Get<CWLResponse>(UrlConstants.GetCWLInfo, myClan).Result;
                //sb.AppendLine($"Found CWL: preparation for season {cwl.season}");
                //now check each day for our clan
                var clanInfo = new Clan();
                var myClanInfo = new Clan();
                var day = 0;
                foreach (var r in cwl.rounds.Where(x => x.warTags.All(t => t != "#0")))
                {
                    day++;
                    foreach (var w in r.warTags)
                    {
                        if (w == "#0") break;
                        //get this particular war
                        var war = ApiService.Get<CWLWarResponse>(UrlConstants.GetCWLWarInfo, w).Result;
                        //no reason to check if war is still active
                        //check to see if we are in this war
                        if (war.state != "preparation") break;
                        if (war.clan.tag == myClan)
                        {
                            clanInfo = war.opponent;
                            myClanInfo = war.clan;
                        }
                        if (war.opponent.tag == myClan)
                        {
                            clanInfo = war.clan;
                            myClanInfo = war.opponent;
                        }
                    }
                }
                var embed = new EmbedBuilder { Title = $"CWL Day {day} vs **{clanInfo.name}**" };

                var c = myClanInfo;


                foreach (var m in c.members.OrderBy(x => x.mapPosition))
                {
                    var emoji = emotes.FirstOrDefault(x => x.Name == $"th{m.townhallLevel}");
                    if (emoji != null)
                    {
                        buffer += $"<:{emoji.Name}:{emoji.Id}>";
                    }
                    buffer += ($"{m.townhallLevel} : {m.name}\n");
                }
                if (buffer.Length > 1024)
                {
                    //trim out the townhall emojis
                    buffer = "";

                    foreach (var m in c.members.OrderBy(x => x.mapPosition))
                    {
                        //var emoji = emotes.FirstOrDefault(x => x.Name == $"th{m.townhallLevel}");
                        buffer += $"TH{m.townhallLevel} : {m.name}\n";
                    }
                }
                embed.AddField(c.name, buffer, true).WithCurrentTimestamp();

                c = clanInfo;
                buffer = "";

                foreach (var m in c.members.OrderBy(x => x.mapPosition))
                {
                    var emoji = emotes.FirstOrDefault(x => x.Name == $"th{m.townhallLevel}");
                    if (emoji != null)
                    {
                        buffer += $"<:{emoji.Name}:{emoji.Id}>";
                    }
                    buffer += ($"{m.townhallLevel} : {m.name}\n");
                }
                if (buffer.Length > 1024)
                {
                    //trim out the townhall emojis
                    buffer = "";

                    foreach (var m in c.members.OrderBy(x => x.mapPosition))
                    {
                        //var emoji = emotes.FirstOrDefault(x => x.Name == $"th{m.townhallLevel}");
                        buffer += $"TH{m.townhallLevel} : {m.name}\n";
                    }
                }
                embed.AddField(c.name, buffer, true).WithCurrentTimestamp();


                await ReplyAsync(embed: embed.Build());
            }
            catch(Exception ex)
            {
                await ReplyAsync(ex.Message);
            }

        }

        [Command("cwlinfo")]
        [Summary("Get CWL clan info (peek)")]
        public async Task CwlInfoAsync()
        {
            Console.WriteLine("Getting CWL info");
            var clans = Program.DB.GetCollection<ClanResponse>("clans");
            var servers = Program.DB.GetCollection<DiscordServer>("guilds");
            var s = servers.FindOne(x => x.ServerId == (long)Context.Guild.Id);
            var emotes = Context.Guild.Emotes.ToList();
            if (s == null || String.IsNullOrEmpty(s.ClanTag))
            {
                await ReplyAsync("I don't have a clan registered with this server.  Please use the !register command in whichever channel you wish to get notifications in");

            }
            Console.WriteLine(s.ClanTag);
            var myClan = s.ClanTag;
            var toSend = "";
            var buffer = "";
            var cwl = ApiService.Get<CWLResponse>(UrlConstants.GetCWLInfo, myClan).Result;
            Console.WriteLine($"CWL Info retrieved, working");
            //sb.AppendLine($"Found CWL: preparation for season {cwl.season}");
            //now check each day for our clan
            var clanInfo = new Clan();
            var myClanInfo = new Clan();
            var clanArray = new Clan[8];
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
                    //check to see if we are in this war
                    if (war.state != "preparation") break;
                    clanArray[i] = war.clan;
                    i++;
                    clanArray[i] = war.opponent;
                    i++;

                }
            }

            foreach (var c in clanArray)
            {
                buffer = ($"**{c.name}**\n");
                foreach (var m in c.members.OrderBy(x => x.mapPosition))
                {
                    var emoji = emotes.FirstOrDefault(x => x.Name == $"th{m.townhallLevel}");
                    if (emoji != null)
                    {
                        buffer += $"<:{emoji.Name}:{emoji.Id}>";
                    }
                    buffer += ($"{m.townhallLevel} : {m.name}\n");
                }
                buffer += ($"\nTotal TH level for clan: {c.members.Sum(x => x.townhallLevel)}\n");
                buffer += ("----------------------\n\n");
                if ((toSend + buffer).Length > 2000)
                {
                    //message limit reached
                    Console.WriteLine($"sending message length {toSend.Length}");
                    await ReplyAsync("----\n" + toSend);
                    //await Task.Delay(1000);
                    toSend = buffer;
                }
                else
                {
                    toSend += buffer;
                }
            }
            if (toSend.Length > 0)
            {
                Console.WriteLine($"sending message length {toSend.Length}");
                await ReplyAsync("----\n" + toSend);
                toSend = "";
            }

            toSend = ("Total / avg townhall levels for this CWL war:\n");

            foreach (var c in clanArray.OrderByDescending(c => c.members.Sum(x => x.townhallLevel)))
            {
                toSend += ($"{c.members.Sum(x => x.townhallLevel)} / {c.members.Average(x => x.townhallLevel):00.00} - {c.name}\n");
            }

            Console.WriteLine($"sending message length {toSend.Length}");
            await ReplyAsync("----\n" + toSend);
            //return Task.CompletedTask;
        }
    }
}
