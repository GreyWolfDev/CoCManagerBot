using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using log4net;

namespace CoCManagerBot
{
    public static class DiscordBot
    {
        public static DiscordSocketClient _client;
        private static CommandService _commands;
        private static CommandHandler _handler;
        private static ILog _log;
        public static async Task Start(string token, ILog log)
        {
            _log = log;
            _commands = new CommandService(new CommandServiceConfig
            {                                       // Add the command service to the collection
                LogLevel = LogSeverity.Verbose,     // Tell the logger to give Verbose amount of info
                DefaultRunMode = RunMode.Async,     // Force all commands to run async by default
            });
            _client = new DiscordSocketClient();

            _client.Log += Log;

            //  You can assign your bot token to a string, and pass that in to connect.
            //  This is, however, insecure, particularly if you plan to have your code hosted in a public repository.
            

            // Some alternative options would be to keep your token in an Environment Variable or a standalone file.
            // var token = Environment.GetEnvironmentVariable("NameOfYourEnvironmentVariable");
            // var token = File.ReadAllText("token.txt");
            // var token = JsonConvert.DeserializeObject<AConfigurationClass>(File.ReadAllText("config.json")).Token;
            _handler = new CommandHandler(_client, _commands);
            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();
            await _handler.InstallCommandsAsync();
            // Block this task until the program is closed.
            await Task.Delay(-1);
        }
        private static Task Log(LogMessage msg)
        {
            switch (msg.Severity)
            {
                case LogSeverity.Critical:
                case LogSeverity.Error:
                    _log.Error(msg.Message, msg.Exception);
                    break;
                case LogSeverity.Debug:
                    _log.Debug(msg.Message);
                    break;
                case LogSeverity.Verbose:
                case LogSeverity.Info:
                    _log.Info(msg.Message);
                    break;
                case LogSeverity.Warning:
                    _log.Warn(msg.Message);
                    break;
            }
            
            return Task.CompletedTask;
        }
    }
}
