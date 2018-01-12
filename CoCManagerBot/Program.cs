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
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace CoCManagerBot
{
    class Program
    {
        internal static string TGToken = RegHelper.GetRegValue("TelegramBotToken");
        internal static TelegramBotClient Bot;
        internal static LiteDatabase DB;
        internal static User Me;
        static void Main(string[] args)
        {
            DB = new LiteDatabase(@"coc.db");
            Bot = new TelegramBotClient(TGToken);
            Bot.OnInlineQuery += Bot_OnInlineQuery;
            Bot.OnMessage += Bot_OnMessage;
            Bot.OnCallbackQuery += Bot_OnCallbackQuery;
            new Task(Monitor).Start();
            Bot.StartReceiving();
            Me = Bot.GetMeAsync().Result;

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
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"Running checks, {clans.Count()} clans, {users.Count()} users");
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
                            //check for war
                            var war = await ApiService.Get<WarResponse>(UrlConstants.GetCurrentWarInformationTemplate,
                                c.tag);
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
                                    DB.GetCollection<WarResponse>("wars").Insert(war);
                                    //TODO send report to leaders
                                    var report = Admin.GetWarReport(war, clan);
                                    foreach (var l in c.memberList.Where(x => x.role != "member"))
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

                                    var msg = $"War has started against {war.opponent.name}!  Make sure you attack :)";
                                    if (!String.IsNullOrEmpty(c.WarRules))
                                    {
                                        msg += $"\n{c.WarRules}";
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

                                    var msg = $"War has been declared against {war.opponent.name}! You are in this war\n";
                                    var log = ApiService.Get<WarLogResponse>(UrlConstants.GetWarLogInformationTemplate,
                                        war.opponent.tag).Result;
                                    if (log.reason != "accessDenied")
                                    {
                                        msg +=
                                            $"Of the last {log.items.Length} wars, the opponent has won {log.items.Count(x => x.result == "win")} war(s)";
                                    }

                                    if (!String.IsNullOrEmpty(c.WarRules))
                                    {
                                        msg += $"\n{c.WarRules}\n";
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
                                var minutes = (time - DateTime.Now).Minutes;
                                if (minutes < 59 && minutes >= 54)
                                {
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
                            // ignored for now
                        }
                    }
                }
                Thread.Sleep(TimeSpan.FromMinutes(5));
            }
        }

        private static void Bot_OnMessage(object sender, Telegram.Bot.Args.MessageEventArgs e)
        {
            
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
                    var command = e.Message?.Text.Trim();
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
                            Send(e.Message.From.Id, "Hi there! I'm a clash of clans management bot.  Use /setid <yourusertag> to register yourself with me.\n\nYour user tag is from Clash of Clans, and looks like #5568FHN43.  It is NOT your telegram id, or your name in Clash of Clans.");
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
                                //invalid id!
                                Bot.SendPhotoAsync(e.Message.From.Id, new FileToSend("AgADAQADuacxG0fO6Ueo3opj6hFXMjIoAzAABF4YD9Tyw4lhmnoAAgI"),
                                    "Id not found, please check it in Clash of Clans.  Use this image as an example.  You are looking for the part marked out, in this one it starts with #2",
                                    replyToMessageId: e.Message.MessageId);
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
                        case "recheck":
                            user = DB.GetCollection<Player>("player")
                                .FindOne(x => x.TelegramId == e.Message.From.Id);
                            //clan was marked for having private war log.  Check settings now.
                            //var check = ApiService.Get<WarResponse>()
                            var check = ApiService.Get<WarResponse>(UrlConstants.GetCurrentWarInformationTemplate,
                                user.ClanId).Result;
                            if (check.reason != null)
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
                        case "statusreport!":
                            var allUsers = users.FindAll().ToList();
                            var byClan = allUsers.GroupBy(x => x.ClanId);
                            var reply = byClan.OrderByDescending(x => x.Count()).ThenBy(x => clans.FindOne(a => a.tag == x.Key)?.name).Aggregate("",
                                (cur, v) => cur + "- " + clans.FindOne(x => x.tag == v.Key)?.name + $"({v.Key}): {v.Count()}\n");

                            reply += "\n" + DB.GetCollection<WarResponse>("wars").Count() + " wars logged.";

                            Send(e.Message.From.Id, reply);
                            break;
                            
                        case ("war"):
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
                            var war = ApiService.Get<WarResponse>(UrlConstants.GetCurrentWarInformationTemplate,
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
                        case "test":
                            //clean up wars
                            var wars = DB.GetCollection<WarResponse>("wars");
                            
                            foreach (var unique in wars.FindAll().GroupBy(x => x.startTime))
                            {
                                foreach (var dup in unique.Skip(1))
                                {
                                    wars.Delete(dup.id);
                                }
                            }
                            break;
                    }

                }
                catch (Exception ex)
                {
                    Send(e.Message.From.Id, ex.Message);
                }

            }).Start();
        }

        private static void Bot_OnInlineQuery(object sender, Telegram.Bot.Args.InlineQueryEventArgs e)
        {
            new Task(() =>
            {

            }).Start();
        }

        private static void Bot_OnCallbackQuery(object sender, Telegram.Bot.Args.CallbackQueryEventArgs e)
        {
            new Task(() =>
            {

            }).Start();
        }

        private static void Send(long id, string msg, ParseMode parseMode = ParseMode.Default,
            bool disableWebPagePreview = false,
            bool disableNotification = false,
            int replyToMessageId = 0,
            IReplyMarkup replyMarkup = null)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"Replying: {msg}");
            Console.ForegroundColor = ConsoleColor.Gray;
            Bot.SendTextMessageAsync(id, msg, parseMode, disableWebPagePreview, disableNotification, replyToMessageId,
                replyMarkup);
        }
    }
}
