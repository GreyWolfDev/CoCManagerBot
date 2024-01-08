using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using CoCManagerBot.Helpers;
using CoCManagerBot.Model;
using LiteDB;
using Telegram.BotAPI.AvailableMethods;
using Telegram.BotAPI;
using Telegram.BotAPI.GettingUpdates;
using System.IO;
using System.Net;
using Telegram.BotAPI.AvailableTypes;
using Telegram.BotAPI.AvailableMethods.FormattingOptions;
using log4net;

namespace CoCManagerBot
{
    class Program
    {
        internal static string TGToken = RegHelper.GetRegValue("TelegramBotToken");
        internal static string DiscordToken = RegHelper.GetRegValue("DiscordBotToken");
        internal static BotClient Bot;
        internal static LiteDatabase DB;
        internal static User Me;
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        static void Main(string[] args)
        {
            System.Net.ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
            DB = new LiteDatabase(@"coc.2.db");
            //temporary code for db conversion
            #region conversion
            //var oldDb = new LiteDatabase("coc.db");
            //LiteCollection<ClanResponse> clans = DB.GetCollection<ClanResponse>("clans");
            //LiteCollection<Player> users = DB.GetCollection<Player>("player");
            //LiteCollection<WarResponse> wars = DB.GetCollection<WarResponse>("wars");
            //foreach (var p in oldDb.GetCollection<OldPlayer>("player").FindAll())
            //{
            //    var tempP = new Player { ClanId = p.ClanId, CoCId = p.CoCId, id = p.id, LastNotification = p.LastNotification, TelegramId = p.TelegramId };
            //    users.EnsureIndex(x => x.TelegramId, true);
            //    users.Upsert(tempP);
            //}
            //foreach (var c in oldDb.GetCollection<ClanResponse>("clans").FindAll())
            //{
            //    clans.EnsureIndex(x => x.tag);
            //    clans.Upsert(c);
            //}
            //foreach (var w in oldDb.GetCollection<WarResponse>("wars").FindAll())
            //{
            //    wars.Upsert(w);

            //}
            #endregion
            DiscordBot.Start(DiscordToken, log);
            while (DiscordBot._client.ConnectionState != Discord.ConnectionState.Connected)
            {
                Thread.Sleep(1000);
            }
            Bot = new BotClient(TGToken);
            //Bot.OnInlineQuery += Bot_OnInlineQuery;
            //Bot.OnMessage += Bot_OnMessage;
            //Bot.OnCallbackQuery += Bot_OnCallbackQuery;
            new Task(Monitor).Start();
            //Bot.StartReceiving();
            Me = Bot.GetMeAsync().Result;


            var updates = Bot.GetUpdates();
            while (true)
            {
                if (updates.Any())
                {
                    foreach (var update in updates)
                    {
                        // Process update
                        Bot_OnMessage(update);
                    }
                    var offset = updates.Last().UpdateId + 1;
                    updates = Bot.GetUpdates(offset);
                }
                else
                {
                    updates = Bot.GetUpdates();
                }
            }
            //testing stuffs

            Thread.Sleep(-1);
        }

        /// <summary>
        /// Monitors clans for war activities
        /// </summary>
        private static async void Monitor()
        {
            while (true)
            {
                var clans = DB.GetCollection<ClanResponse>("clans");
                var users = DB.GetCollection<Player>("player");
                var servers = DB.GetCollection<DiscordServer>("guilds");
                //foreach (var u in users.FindAll())
                //{
                //    try
                //    {
                //        Console.Write($"Sending message to {u.TelegramId}: ");
                //        var result = await Send(u.TelegramId, "Hey all, this is Para - I've decided to revive this project, and am working on some bugs and new features.  Also, it looks like I may be extending this bot to Discord as well.\n\nIf you have requests or anything, feel free to join this group: @cocbotdev", log: false);
                //        Console.WriteLine("Success");
                //    }
                //    catch
                //    {
                //        Console.WriteLine("Failed");
                //    }
                //    Thread.Sleep(500);
                //}
                Console.ForegroundColor = ConsoleColor.Cyan;
                log.Info($"Running checks, {clans.Count()} clans, {users.Count()} users, {servers.Count()} guilds");
                //Console.WriteLine($"Running checks, {clans.Count()} clans, {users.Count()} users");
                Console.ForegroundColor = ConsoleColor.Gray;
                if (clans.Count() > 0)
                {
                    //TODO - recommend attacks during last 2 hours
                    //ignore outside mirror last 2 hours
                    //recommend war line ups
                    //attach time of attack when scanning wars
                    foreach (var c in clans.FindAll())
                    {
                        try
                        {
                            if (c.HasError) continue;
                            //TODO update this to find latest server
                            var s = servers.FindOne(x => x.ClanTag == c.tag);
                            ulong announce = 0;
                            if (s != null)
                                announce = (ulong)(s.AnnouncementChannelId ?? s.PrimaryChannelId);
                            //if (s != null)
                            //{
                            //    DiscordBot._client.GetGuild((ulong)s.ServerId).GetTextChannel((ulong)s.PrimaryChannelId).SendMessageAsync("Online");
                            //}
                            //check for war
                            var war = await ApiService.Get<WarResponse>(UrlConstants.GetCurrentWarInformationTemplate,
                                c.tag);
                            if (war?.reason != null && !(war?.reason??"").Contains("inMaintenance"))
                            {
                               
                                c.HasError = true;
                                clans.Update(c);
                                if (war.reason.Contains("accessDenied"))
                                {

                                    if (s != null)
                                    {

                                        SendDiscord(s, s.PrimaryChannelId, "It appears your clan has its war log settings to private.  Ask the leader or co leader to set it to public if you'd like to use this bot.\n\nYou can use the !register command once they have to check your clan settings again and get it monitoring.");

                                    }
                                    //find the players of the clan
                                    foreach (var usr in users.Find(x => x.ClanId == c.tag))
                                    {
                                        Send(usr.TelegramId, "It appears your clan has its war log settings to private.  Ask the leader or co leader to set it to public if you'd like to use this bot.\n\nYou can use the /recheck command once they have to check your clan settings again and get it monitoring.");
                                    }
                                }
                                else
                                {
                                    if (s != null)
                                    {

                                        SendDiscord(s, s.PrimaryChannelId, $"Error monitoring your clan: {war.reason}.  If able, fix the issue.  You can use !recheck to try again.  Monitoring is now disabled until a successful !recheck happens.");

                                    }
                                    //find the players of the clan
                                    foreach (var usr in users.Find(x => x.ClanId == c.tag))
                                    {
                                        Send(usr.TelegramId, $"Error monitoring your clan: {war.reason}.  If able, fix the issue.  You can use !recheck to try again.  Monitoring is now disabled until a successful !recheck happens.");
                                    }

                                }
                            }
                            if (war?.state == null) continue;
                            if (war.state == "warEnded")
                            {
                                if (!c.WarFinalized)
                                {
                                    //first, reload the clan from the api
                                    var clan = await ApiService.Get<ClanResponse>(UrlConstants.GetClanInformationUrlTemplate, c.tag);
                                    //reset
                                    clan.MembersNotifiedOfWarStart = false;
                                    clan.MembersNotifiedOfWarPrep = false;
                                    clan.WarFinalized = true;
                                    clan.WarRules = c.WarRules;
                                    clan.WarStartTime = c.WarStartTime;
                                    clan.id = c.id;
                                    clans.Update(clan);
                                    //log the war
                                    war.Upsert();
                                    //DB.GetCollection<WarResponse>("wars").Insert(war);
                                    //TODO send report to leaders
                                    var report = Admin.GetWarReport(war, clan);
                                    if (s != null)
                                    {
                                        SendDiscord(s, s.PrimaryChannelId, report);
                                    }
                                    foreach (var l in c.memberList.Where(x => x.role != "member" || x.name == "Para"))
                                    {
                                        //check if they are a user
                                        var u = users.FindOne(x => x.CoCId == l.tag);
                                        if (u != null)
                                        {
                                            Send(u.TelegramId, report);
                                        }
                                    }
                                }
                                continue;
                            }
                            if (war.state == "inWar") //battle day started
                            {
                                if (!c.MembersNotifiedOfWarStart)
                                {
                                    c.WarFinalized = false;
                                    c.MembersNotifiedOfWarStart = true;
                                    clans.Update(c);

                                    var msg = $"War has started against {war.opponent.name}!  Make sure you attack :)\n";
                                    if (!String.IsNullOrEmpty(c.WarRules))
                                    {
                                        msg += $"\n{c.WarRules}";
                                    }
                                    if (s != null)
                                    {
                                        SendDiscord(s, (long)announce, msg);
                                    }
                                    //notify all members war has started
                                    foreach (var m in war.clan.members)
                                    {
                                        var u = users.FindOne(x => x.CoCId == m.tag);
                                        if (u != null)
                                        {
                                            Send(u.TelegramId, msg);
                                        }
                                    }
                                }
                                var time = Converters.GetTime(war.endTime);
                                //check time left
                                var hours = (time - DateTime.Now).Hours;
                                var minutes = (time - DateTime.Now).Minutes;
                                if (((hours == 6 || hours == 3 || hours == 1) && minutes <= 5 && minutes > 0) || (hours == 0 && minutes < 34 && minutes > 27))
                                {
                                    //get war members that haven't attacked
                                    var toNotify = war.clan.members.Where(x => (x.attacks?.Length ?? 0) != 2);
                                    var msg = "You have not used both your attacks yet.  Time Remaining: ";
                                    if (hours != 0) msg += $"{hours} hours.";
                                    else msg += $"{minutes} minutes.";
                                    foreach (var n in toNotify)
                                    {
                                        //dammit! get your attacks in!
                                        //check if they are a user
                                        var u = users.FindOne(x => x.CoCId == n.tag);
                                        if (u != null)
                                        {
                                            Send(u.TelegramId, msg);
                                        }
                                    }
                                }
                                continue;
                            }
                            if (war.state == "preparation") //prep day
                            {

                                if (!c.MembersNotifiedOfWarPrep)
                                {

                                    c.WarFinalized = false;
                                    c.MembersNotifiedOfWarPrep = true;
                                    clans.Update(c);
                                    var discordMsg = $"War has been declared against {war.opponent.name}!\n";
                                    var msg = $"War has been declared against {war.opponent.name}! You are in this war\n";
                                    var log = ApiService.Get<WarLogResponse>(UrlConstants.GetWarLogInformationTemplate,
                                        war.opponent.tag).Result;
                                    if (log.reason != "accessDenied")
                                    {
                                        discordMsg += $"Of the last {log.items.Length} wars, the opponent has won {log.items.Count(x => x.result == "win")} war(s)\n";
                                        msg +=
                                            $"Of the last {log.items.Length} wars, the opponent has won {log.items.Count(x => x.result == "win")} war(s)\n";
                                    }

                                    if (!String.IsNullOrEmpty(c.WarRules))
                                    {
                                        discordMsg += $"\n{c.WarRules}\n";
                                        msg += $"\n{c.WarRules}\n";
                                    }
                                    if (s != null)
                                    {
                                        SendDiscord(s, (long)announce, discordMsg);
                                    }
                                    //notify all members war has started
                                    foreach (var m in war.clan.members)
                                    {
                                        var u = users.FindOne(x => x.CoCId == m.tag);
                                        if (u != null)
                                        {
                                            Send(u.TelegramId, msg + $"Your position is #{m.mapPosition}");
                                        }
                                    }
                                }
                                var time = Converters.GetTime(war.startTime);
                                var minutes = (int)(time - DateTime.Now).TotalMinutes;
                                if (minutes < 59 && minutes >= 54)
                                {
                                    if (s != null)
                                    {
                                        SendDiscord(s, (long)announce, $"{minutes} minutes left until war starts.  Please make sure clan castles are filled.");
                                    }
                                    //get the top 3 members in the war
                                    var notify = war.clan.members.OrderBy(x => x.mapPosition).Take(3);
                                    foreach (var n in notify)
                                    {
                                        var u = users.FindOne(x => x.CoCId == n.tag);
                                        if (u != null)
                                        {
                                            Send(u.TelegramId, $"{minutes} minutes left until war starts.  Please make sure clan castles are filled.");
                                        }
                                    }
                                }

                                continue;
                            }
                        }
                        catch (Exception e)
                        {
                            log.Error("Error in monitoring", e);
                        }
                    }
                }
                Thread.Sleep(TimeSpan.FromMinutes(5));
            }
        }

        private static void Bot_OnMessage(Update e)
        {
            if (e.Message == null) return;
            new Task(() =>
            {
                try
                {
                    if (e.Message.Photo != null && e.Message.From.FirstName == "Para")
                    {
                        Send(e.Message.From.Id, e.Message.Photo.OrderByDescending(x => x.Height).First().FileId);
                        return;
                    }
                    LiteCollection<ClanResponse> clans = DB.GetCollection<ClanResponse>("clans");
                    LiteCollection<Player> users = DB.GetCollection<Player>("player");
                    var user = DB.GetCollection<Player>("player").FindOne(x => x.TelegramId == e.Message.From.Id);
                    ClanResponse c;
                    var command = e.Message?.Text?.Trim();
                    if (String.IsNullOrEmpty(command)) return;

                    var args = "";
                    if (command.Contains(" "))
                    {
                        args = command.Split(' ').Skip(1).Aggregate((a, b) => a + " " + b).Trim();
                        command = command.Split(' ')[0];
                    }
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine($"{e.Message.From.FirstName} {e.Message.From.LastName} (@{e.Message.From.Username}): {e.Message?.Text}");
                    Console.ForegroundColor = ConsoleColor.Gray;
                    command = command.Replace("/", "").Replace("@" + Me.Username, "");
                    switch (command)
                    {
                        case "start":
                            Send(e.Message.From.Id, "Hi there! I'm a clash of clans management bot.  Use /setid <yourusertag> to register yourself with me.\n\nYour user tag is from Clash of Clans, and looks like #5568FHN43.  It is NOT your telegram id, or your name in Clash of Clans.  Questions?  Come to @cocbotdev");
                            break;
                        #region UserId
                        //set user id
                        case "setid":
                            //validate the id first!
                            if (!args.Contains("#"))
                                args = "#" + args;
                            var p = ApiService.Get<PlayerResponse>(UrlConstants.GetPlayerInformationTemplate, args)
                                .Result;
                            if (p.troops == null)
                            {
                                Bot.SendPhotoAsync(e.Message.From.Id, "AgADAQADuacxG0fO6Ueo3opj6hFXMjIoAzAABF4YD9Tyw4lhmnoAAgI", "Id not found, please check it in Clash of Clans.  Use this image as an example.  You are looking for the part marked out, in this one it starts with #2",
                                    replyToMessageId: e.Message.MessageId);
                                //invalid id!

                                return;
                            }
                            //get the player from the database (if they exist)

                            var pl = users.FindOne(x => x.TelegramId == e.Message.From.Id);
                            var isNew = false;
                            if (pl == null)
                            {
                                isNew = true;
                                pl = new Player
                                {
                                    TelegramId = e.Message.From.Id
                                };
                            }
                            pl.CoCId = args;
                            if (isNew)
                            {
                                users.EnsureIndex(x => x.TelegramId, true);
                                users.Insert(pl);
                            }
                            else
                            {
                                users.Update(pl);
                            }

                            var response = "Your player tag has been updated.  Welcome " + p.name;
                            if (p.clan != null)
                            {
                                pl.ClanId = p.clan.tag;
                                users.Update(pl);
                                var role = char.ToUpper(p.role[0]) + p.role.Substring(1);
                                var reg = new Regex(@"(?<=[A-Z])(?=[A-Z][a-z]) | (?<=[^A-Z])(?=[A-Z]) | (?<=[A-Za-z])(?=[^A-Za-z])", RegexOptions.IgnorePatternWhitespace);
                                role = reg.Replace(role, " ");
                                response += $", {role} of the {p.clan.name} clan ({p.clan.tag})";
                                //awesome.  we got the player.  Let's see if we can find their clan!
                                var clan = ApiService.Get<ClanResponse>(UrlConstants.GetClanInformationUrlTemplate,
                                    p.clan.tag).Result;
                                //check to see if clan is in database

                                c = clans.FindOne(x => x.tag == p.clan.tag);
                                if (c == null)
                                {
                                    clan.RegisteredBy = e.Message.From.Username;
                                    clans.EnsureIndex(x => x.tag, true);
                                    clans.Insert(clan);
                                }
                                else
                                {
                                    c.HasError = false;
                                    clans.Update(c);
                                }
                            }
                            Send(e.Message.From.Id,
                                response,
                                replyToMessageId: e.Message.MessageId);
                            break;
                        #endregion
                        case "donate":
                            Send(e.Message.From.Id, "Has this bot helped you and your clan?  Consider dropping me a donation to continue development:\n" +
                                "https://www.paypal.me/GreyWolfDevelopment");
                            break;
                        case "recheck":
                            user = DB.GetCollection<Player>("player")
                                .FindOne(x => x.TelegramId == e.Message.From.Id);
                            //clan was marked for having private war log.  Check settings now.
                            //var check = ApiService.Get<WarResponse>()
                            var check = ApiService.Get<WarResponse>(UrlConstants.GetCurrentWarInformationTemplate,
                                user.ClanId).Result;
                            if (check.reason != null && !check.reason.Contains("inMaintenance"))
                            {
                                Send(user.TelegramId, $"Still unable to get war log.  Reason: {check.reason}");
                            }
                            else
                            {
                                c = clans.FindOne(x => x.tag == user.ClanId);
                                if (c != null)
                                {
                                    c.HasError = false;
                                    clans.Update(c);
                                    Send(user.TelegramId,
                                        "I was able to pull your war information!  Clan is now being monitored");
                                }
                            }
                            break;
                        case "warflag":
                            //check for last war
                            if (user == null)
                            {
                                Send(e.Message.From.Id,
                                    "Please use /setid to set your user id.  Use the #AAAAAAAAA tag from Clash of Clans.");
                                return;
                            }
                            //update the user information, in case they switch clans or whatever.
                            p = ApiService.Get<PlayerResponse>(UrlConstants.GetPlayerInformationTemplate, user.CoCId)
                                .Result;
                            if (p.clan.tag != user.ClanId)
                            {
                                user.ClanId = p.clan.tag;
                                DB.GetCollection<Player>("player").Update(user);
                            }


                            if (String.IsNullOrEmpty(user.ClanId))
                            {
                                Send(user.TelegramId, "You aren't in a clan!");
                                return;
                            }

                            clans = DB.GetCollection<ClanResponse>("clans");
                            c = clans.FindOne(x => x.tag == user.ClanId);
                            if (c == null)
                            {
                                Send(user.TelegramId, "I'm not finding a clan for you :(");
                                return;
                            }
                            var wars = DB.GetCollection<WarResponse>("wars").Find(x => x.clan.tag == c.tag && x.state == "warEnded");
                            if (wars.Count() == 0)
                            {
                                Send(user.TelegramId, "I have no wars on record for your clan");
                                return;
                            }
                            //get most recent war
                            var war = wars.OrderByDescending(x => x.endTime).First();

                            var report = Admin.GetWarReport(war, c);
                            Send(user.TelegramId, report);
                            break;
                        case "statusreport!":
                            var allUsers = users.FindAll().ToList();
                            var byClan = allUsers.GroupBy(x => x.ClanId);
                            var reply = byClan.OrderByDescending(x => x.Count()).ThenBy(x => clans.FindOne(a => a.tag == x.Key)?.name).Aggregate("",
                                (cur, v) => cur + "- " + clans.FindOne(x => x.tag == v.Key)?.name + $"({v.Key}): {v.Count()}\n");

                            reply += "\n" + DB.GetCollection<WarResponse>("wars").Count() + " wars logged.";

                            Send(e.Message.From.Id, reply);
                            break;

                        case "war":
                            //get war report!
                            //first find the user


                            if (user == null)
                            {
                                Send(e.Message.From.Id,
                                    "Please use /setid to set your user id.  Use the #AAAAAAAAA tag from Clash of Clans.");
                                return;
                            }

                            //update the user information, in case they switch clans or whatever.
                            p = ApiService.Get<PlayerResponse>(UrlConstants.GetPlayerInformationTemplate, user.CoCId)
                                .Result;
                            if (p.clan.tag != user.ClanId)
                            {
                                user.ClanId = p.clan.tag;
                                DB.GetCollection<Player>("player").Update(user);
                            }


                            if (String.IsNullOrEmpty(user.ClanId))
                            {
                                Send(user.TelegramId, "You aren't in a clan!");
                                return;
                            }

                            clans = DB.GetCollection<ClanResponse>("clans");
                            c = clans.FindOne(x => x.tag == user.ClanId);

                            //ok, we have everything we need...  let's build our war report
                            war = ApiService.Get<WarResponse>(UrlConstants.GetCurrentWarInformationTemplate,
                                user.ClanId).Result;
                            if (war.reason != null)
                            {
                                c.HasError = true;
                                clans.Update(c);
                                if (war.reason.Contains("accessDenied"))
                                {
                                    //find the players of the clan
                                    foreach (var usr in users.Find(x => x.ClanId == c.tag))
                                    {
                                        Send(usr.TelegramId, "It appears your clan has its war log settings to private.  Ask the leader or co leader to set it to public if you'd like to use this bot.\n\nYou can use the /recheck command once they have to check your clan settings again and get it monitoring.");
                                    }
                                }
                            }
                            response = "";
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

                                war = DB.GetCollection<WarResponse>("wars").Find(x => x.clan.tag == user.ClanId)
                                    .OrderByDescending(x => x.endTime).FirstOrDefault();
                            }
                            if (war == null)
                            {
                                response += "No wars on record for you clan, sorry!";
                                Send(e.Message.Chat.Id, response);
                                break;
                            }
                            //update war in db
                            war.Upsert();
                            //DB.GetCollection<WarResponse>("wars").Upsert(war);
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
                            Send(e.Message.Chat.Id, response);
                            break;
                        case "members":

                            var selectClan = clans.FindOne(x => x.tag == args || x.name == args);
                            if (selectClan != null)
                            {
                                var members = users.Find(x => x.ClanId == selectClan.tag);
                                var list = "";
                                foreach (var member in members)
                                {
                                    var mem = selectClan.memberList.FirstOrDefault(x => x.tag == member.CoCId);
                                    list += $"{mem?.name} ({mem?.tag})\n";
                                }

                                Send(e.Message.From.Id, list);
                            }
                            break;
                        case "rankings":

                            if (user == null)
                            {
                                Send(e.Message.From.Id,
                                    "Please use /setid to set your user id.  Use the #AAAAAAAAA tag from Clash of Clans.");
                                return;
                            }
                            var players = CalculateWarRanks(user.ClanId)?.OrderByDescending(x => x.Rank);
                            if (players == null)
                            {
                                Send(e.Message.From.Id, "Either you are not in a clan, or your clan does not have any wars logged with me.  The Clash of Clans API won't let me get old war results (player performance), so you'll need to start a war and finish it before I can give you rankings for your clan.");
                            }
                            else
                            {
                                Send(e.Message.From.Id, players.Aggregate("", (a, v) => a + "\n" + $"{v.Name}: {v.Rank:N2} ({v.WarCount} wars)"), ParseMode.HTML);
                            }

                            break;
                        case "warlog":
                            if (user == null)
                            {
                                Send(e.Message.From.Id,
                                    "Please use /setid to set your user id.  Use the #AAAAAAAAA tag from Clash of Clans.");
                                return;
                            }
                            //update the user information, in case they switch clans or whatever.
                            p = ApiService.Get<PlayerResponse>(UrlConstants.GetPlayerInformationTemplate, user.CoCId)
                                .Result;
                            if (p.clan.tag != user.ClanId)
                            {
                                user.ClanId = p.clan.tag;
                                DB.GetCollection<Player>("player").Update(user);
                            }


                            if (String.IsNullOrEmpty(user.ClanId))
                            {
                                Send(user.TelegramId, "You aren't in a clan!");
                                return;
                            }

                            clans = DB.GetCollection<ClanResponse>("clans");
                            c = clans.FindOne(x => x.tag == user.ClanId);
                            if (c == null)
                            {
                                Send(user.TelegramId, "I'm not finding a clan for you :(");
                                return;
                            }
                            wars = DB.GetCollection<WarResponse>("wars").Find(x => x.clan.tag == c.tag && x.state == "warEnded");
                            if (wars.Count() == 0)
                            {
                                Send(user.TelegramId, "I have no wars on record for your clan");
                                return;
                            }
                            using (var sw = new StreamWriter(c.tag + ".csv", false, Encoding.UTF8))
                            {
                                //header
                                sw.WriteLine("Attack Order,Member,Member TH Level,Member Rank,Attacked,Attacked TH Level,Attacked Rank,Stars,Damage");
                                foreach (var w in wars)
                                {
                                    var status = "";
                                    if (w.clan.stars > w.opponent.stars)
                                        status = "Win";
                                    if (w.clan.stars == w.opponent.stars)
                                    {
                                        //get average desctruction
                                        var us = w.clan.members.Sum(x => x.attacks?.Sum(a => a.destructionPercentage) ?? 0) / w.clan.attacks;
                                        var them = w.opponent.members.Sum(x => x.attacks?.Sum(a => a.destructionPercentage) ?? 0) / w.opponent.attacks;
                                        if (us > them)
                                            status = "Win";
                                        if (us == them)
                                            status = "Draw";
                                        if (us < them)
                                            status = "Lose";
                                    }
                                    if (w.clan.stars < w.opponent.stars)
                                        status = "(Lose)";
                                    sw.WriteLine($"VS {w.opponent.name} - {status} {w.clan.stars} to {w.opponent.stars}");
                                    sw.WriteLine($"{w.clan.name} Attacks");
                                    //start with our clan

                                    foreach (var m in w.clan.members.OrderBy(x => x.mapPosition))
                                    {
                                        if (m.attacks == null)
                                        {
                                            sw.WriteLine($"-,{m.name},{m.townhallLevel},{m.mapPosition},-,-,-,0,0");
                                            sw.WriteLine($"-,{m.name},{m.townhallLevel},{m.mapPosition},-,-,-,0,0");
                                        }
                                        else if (m.attacks.Length == 1)
                                        {
                                            var a = m.attacks[0];
                                            var d = w.opponent.members.FirstOrDefault(x => x.tag == a.defenderTag);
                                            sw.WriteLine($"{a.order},{m.name},{m.townhallLevel},{m.mapPosition},{d.name},{d.townhallLevel},{d.mapPosition},{a.stars},{a.destructionPercentage}");
                                            sw.WriteLine($"-,{m.name},{m.townhallLevel},{m.mapPosition},-,-,-,0,0");
                                        }
                                        else
                                        {
                                            foreach (var a in m.attacks)
                                            {
                                                var d = w.opponent.members.FirstOrDefault(x => x.tag == a.defenderTag);
                                                sw.WriteLine($"{a.order},{m.name},{m.townhallLevel},{m.mapPosition},{d.name},{d.townhallLevel},{d.mapPosition},{a.stars},{a.destructionPercentage}");
                                            }
                                        }
                                    }
                                    sw.WriteLine($"\n{w.opponent.name} Attacks");
                                    foreach (var m in w.opponent.members.OrderBy(x => x.mapPosition))
                                    {
                                        if (m.attacks == null)
                                        {
                                            sw.WriteLine($"-,{m.name},{m.townhallLevel},{m.mapPosition},-,-,-,0,0");
                                            sw.WriteLine($"-,{m.name},{m.townhallLevel},{m.mapPosition},-,-,-,0,0");
                                        }
                                        else if (m.attacks.Length == 1)
                                        {
                                            var a = m.attacks[0];
                                            var d = w.clan.members.FirstOrDefault(x => x.tag == a.defenderTag);
                                            sw.WriteLine($"{a.order},{m.name},{m.townhallLevel},{m.mapPosition},{d.name},{d.townhallLevel},{d.mapPosition},{a.stars},{a.destructionPercentage}");
                                            sw.WriteLine($"-,{m.name},{m.townhallLevel},{m.mapPosition},-,-,-,0,0");
                                        }
                                        else
                                        {
                                            foreach (var a in m.attacks)
                                            {
                                                var d = w.clan.members.FirstOrDefault(x => x.tag == a.defenderTag);
                                                sw.WriteLine($"{a.order},{m.name},{m.townhallLevel},{m.mapPosition},{d.name},{d.townhallLevel},{d.mapPosition},{a.stars},{a.destructionPercentage}");
                                            }
                                        }
                                    }

                                    sw.WriteLine("\n\n");
                                }
                                sw.Flush();
                            }
                            //now send the file
                            var fs = new FileStream(c.tag + ".csv", System.IO.FileMode.OpenOrCreate);
                            var bytes = new byte[fs.Length];
                            fs.Read(bytes, 0, (int)fs.Length);
                            Bot.SendDocumentAsync(user.TelegramId, new InputFile(bytes, $"{c.tag}.csv"), null, $"War Log for {c.name}: {wars.Count()} wars");

                            break;
                        case "cwl":
                            if (user == null)
                            {
                                Send(e.Message.From.Id,
                                    "Please use /setid to set your user id.  Use the #AAAAAAAAA tag from Clash of Clans.");
                                return;
                            }
                            //update the user information, in case they switch clans or whatever.
                            p = ApiService.Get<PlayerResponse>(UrlConstants.GetPlayerInformationTemplate, user.CoCId)
                                .Result;
                            if (p.clan.tag != user.ClanId)
                            {
                                user.ClanId = p.clan.tag;
                                DB.GetCollection<Player>("player").Update(user);
                            }


                            if (String.IsNullOrEmpty(user.ClanId))
                            {
                                Send(user.TelegramId, "You aren't in a clan!");
                                return;
                            }

                            clans = DB.GetCollection<ClanResponse>("clans");
                            c = clans.FindOne(x => x.tag == user.ClanId);
                            if (c == null)
                            {
                                Send(user.TelegramId, "I'm not finding a clan for you :(");
                                return;
                            }
                            report = Admin.GetCWLReport(c.tag);

                            fs = new FileStream(report, System.IO.FileMode.OpenOrCreate);
                            bytes = new byte[fs.Length];
                            fs.Read(bytes, 0, (int)fs.Length);
                            Bot.SendDocumentAsync(user.TelegramId, new InputFile(bytes, report), null, $"CWL Log for {c.name}");
                            break;
                        case "test":
                            //clean up wars
                            var ccoll = DB.GetCollection<ClanResponse>("clans");
                            var tclans = ccoll.FindAll();
                            foreach (var cl in tclans)
                            {
                                cl.HasError = false;
                                cl.error = null;
                                cl.reason = null;
                                ccoll.Upsert(cl);
                            }

                            //var wars = DB.GetCollection<WarResponse>("wars");

                            //foreach (var unique in wars.FindAll().GroupBy(x => x.startTime))
                            //{
                            //    foreach (var dup in unique.Skip(1))
                            //    {
                            //        wars.Delete(dup.id);
                            //    }
                            //}
                            break;
                        case "reset":
                            //if (e.Message.From.FirstName != "")
                            break;
                    }

                }
                catch (Exception ex)
                {
                    log.Error($"Error replying to Telegram {e.Message}", ex);
                    Send(e.Message.From.Id, $"{ex.Message}\n{ex.StackTrace}");
                }

            }).Start();
        }

        private static List<WarRank> CalculateWarRanks(string clanTag)
        {
            try
            {
                var clan = DB.GetCollection<ClanResponse>("clans").FindOne(x => x.tag == clanTag);
                if (clan == null) return null;
                var wars = DB.GetCollection<WarResponse>("wars").Find(x => x.clan.tag == clanTag);
                if (wars.Count() == 0) return null;
                var members = ApiService.Get<MemberListResponse>(UrlConstants.ListClanMembersUrlTemplate, clanTag).Result;
                var blankAttack = new Attack
                {
                    destructionPercentage = 0,
                    stars = 0
                };
                var result = new List<WarRank>();
                //now we need to somehow join them togather....
                foreach (var m in members.items)
                {
                    if (!wars.Any(x => x.clan.members.Any(z => z.tag == m.tag))) continue;
                    //get all wars this member has been in
                    var mw = wars.Where(x => x.clan.members.Any(z => z.tag == m.tag)).ToList();
                    var scores = new List<double>();
                    foreach (var w in mw)
                    {
                        var them = w.clan.members.FirstOrDefault(x => x.tag == m.tag);
                        var starsLost = them.bestOpponentAttack?.stars ?? 0;
                        var totalStars = them.attacks?.Sum(x => x.stars) ?? 0;
                        var rank = them.mapPosition;
                        var netStars = totalStars - starsLost;
                        //who they attacked
                        double aRav = 35;
                        if (them.attacks != null)
                        {
                            var a1m = w.opponent.members.FirstOrDefault(x => x.tag == them.attacks[0].defenderTag);

                            Member a2m;
                            if (them.attacks.Length == 2)
                            {
                                a2m = w.opponent.members.FirstOrDefault(x => x.tag == them.attacks[1].defenderTag);
                                aRav = (a1m.mapPosition + a2m.mapPosition) / (double)2;
                            }
                            else
                                aRav = a1m.mapPosition;
                            //average rank of those attacks

                        }
                        var defenseMembers = w.opponent.members.Where(x => x.attacks != null && x.attacks.Any(a => a.defenderTag == m.tag)).ToList();
                        var defenses = defenseMembers.SelectMany(x => x.attacks.Where(a => a.defenderTag == m.tag)).ToList();
                        var dRav = defenseMembers.Any() ? defenseMembers.Average(x => x.mapPosition) : 1;
                        if (dRav == 0) dRav = 1;
                        var numD = defenses.Count();
                        //var nD = numD - starsLost;

                        //now some fun....
                        var dPower = (10 * (double)numD) * ((double)1 / (double)5);
                        var rawPower = 10 * (((((totalStars * ((double)4 / (double)6)) - (starsLost - 3)) + ((15 - aRav) * (1 / 13.5))) + ((15 - dRav) * (0.5 / 14))) + (numD * (0.5 / 5)));
                        var unStarred = (starsLost > 0) ? rawPower + 10 : rawPower;
                        var unRaided = (numD == 0) ? rawPower + 10 : rawPower;
                        var defBonus = (numD > 0 && starsLost > 0) ? rawPower + dPower : rawPower;
                        var powerFinal = Math.Max(defBonus, Math.Max(unRaided, unStarred));
                        scores.Add(powerFinal);
                        //Console.WriteLine(powerFinal);

                        //calculate some things....

                    }
                    if (scores.Count() > 1)
                    {
                        //drop lowest score, to be nice
                        scores.Remove(scores.Min());
                    }

                    var avgScore = scores.Average();
                    var user = DB.GetCollection<Player>("player").FindOne(x => x.CoCId == m.tag);
                    var name = $"<b>{m.name.FormatHTML()}</b>";
                    if (user != null)
                    {
                        var urlName = $"<a href=\"tg://user?id={user.TelegramId}\">{m.name.FormatHTML()}</a>";
                        name = $"{urlName}";
                    }
                    var thisPlayer = new WarRank
                    {
                        Id = user?.TelegramId ?? 0,
                        Name = name,
                        Rank = avgScore,
                        WarCount = mw.Count()
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

                return result;
            }
            catch (Exception e)
            {
                log.Error("Error calculating war ranks", e);
                return new List<WarRank>();
            }
        }

        //private static void Bot_OnInlineQuery(object sender, Telegram.Bot.Args.InlineQueryEventArgs e)
        //{
        //    new Task(() =>
        //    {

        //    }).Start();
        //}

        //private static void Bot_OnCallbackQuery(object sender, Telegram.Bot.Args.CallbackQueryEventArgs e)
        //{
        //    new Task(() =>
        //    {

        //    }).Start();
        //}

        private static async Task SendDiscord(DiscordServer s, long channelId, string msg)
        {
            try
            {
                log.Info($"Sending message to server {s.ServerId} ({s.ClanTag}) on channel {channelId}");
                await DiscordBot._client.GetGuild((ulong)s.ServerId).GetTextChannel((ulong)channelId).SendMessageAsync(msg);
            }
            catch (Exception e)
            {
                log.Error("Error sending message to discord", e);
            }
        }

        private static async Task<Message> Send(long id, string msg, string parseMode = null,
            bool disableWebPagePreview = false,
            bool disableNotification = false,
            int replyToMessageId = 0,
            //IReplyMarkup replyMarkup = null,
            bool log = true)
        {
            try
            {
                if (log)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Program.log.Debug($"Replying: {msg}");
                    //Console.WriteLine($"Replying: {msg}");
                    Console.ForegroundColor = ConsoleColor.Gray;
                }
                var result = await Bot.SendMessageAsync(id, msg, parseMode, null, disableWebPagePreview, disableNotification, null, replyToMessageId);
                return result;
            }
            catch (Exception e)
            {
                Program.log.Error("Error sending to Telegram", e);
            }
            return null;
        }


    }
}
