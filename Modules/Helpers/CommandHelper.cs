using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml.Schema;

namespace Honata.Modules.Helpers
{
    public class CommandParams
    {
        public List<string> Command;
        public List<string> Params;

        public string GetParam(string _param, bool _value = false)
        {
            // Create a new output.
            string output = null;

            // Check if the param exists, and set it.
            if (Params.Where(x => x.StartsWith(_param)) is IEnumerable<string> result && result.Count() > 0) output = result.First();

            // Get the value if the value bool is true.
            if (output != null && _value) output = output.Substring(_param.Length).Trim();

            // Return the output.
            return output;
        }

        public CommandParams(string _params, bool remainder = false)
        {
            Command = new List<string>();
            Params = new List<string>();

            // Parse the intput.
            string[] input = remainder ? 
                             _params.Split("-").Select(x => $"-{x}").ToArray() :
                             _params.Split(" ");

            // Loop through the segments.
            foreach (string segment in input)
            {
                // Add them to the correct list.
                if (segment.StartsWith("-")) Params.Add(segment.Trim());
                else Command.Add(segment.Trim());
            }
        }
    }

    public class CommandHelper
    {
        #region Variables

        private readonly DiscordSocketClient _client;
        public readonly CommandService CommandService;
        public IServiceProvider Services;

        #endregion

        #region OnLoad

        /// <summary>
        /// Handles the usage of commands.
        /// </summary>
        /// <param name="_client"></param>
        /// <param name="_commands"></param>
        public CommandHelper(DiscordSocketClient _client, CommandService _commands)
        {
            // Set the variables.
            this._client = _client;
            this.CommandService = _commands;
        }

        #endregion

        #region Methods

        public async Task InstallCommandsAsync()
        {
            // Hook the MessageReceived event into our command handler
            _client.MessageReceived += HandleCommandAsync;

            Services = new ServiceCollection()
                .AddSingleton(_client)
                .AddSingleton<InteractiveService>()
                .BuildServiceProvider();

            // Here we discover all of the command modules in the entry.
            await CommandService.AddModulesAsync(assembly: Assembly.GetEntryAssembly(),
                                           services: Services);

            Console.WriteLine($"[{DateTime.Now:hh:mm:ss} | Info] : Preloaded {CommandService.Modules.Count()} modules.");
            Console.WriteLine($"[{DateTime.Now:hh:mm:ss} | Info] : Preloaded {CommandService.Modules.Select(x => x.Commands).Count()} commands.");
        }

        private async Task HandleCommandAsync(SocketMessage _message)
        {
            // Don't process the command if it was a system message.
            if (!(_message is SocketUserMessage message)) return;

            // Create a number to track where the prefix ends and the command begins.
            int argPos = 0;

            // Check if the the message came form a guild.
            string prefix = _message.Channel is SocketGuildChannel ?
                            // Fetch the guild's prefix.
                            (_message.Channel as SocketGuildChannel).Guild.GetServer().Prefix :
                            // Set the default prefix.
                            ">";

            // Determine if the message is a command based on the prefix and make sure no bots trigger commands.
            if (!(message.HasStringPrefix(prefix, ref argPos) ||
                  message.HasMentionPrefix(_client.CurrentUser, ref argPos)) ||
                  message.Author.IsBot)
                return;

            // Create a WebSocket-based command context based on the message.
            SocketCommandContext context = new SocketCommandContext(_client, message);

            // Execute the command with the command context we just
            // created, along with the service provider for precondition checks.
            // Keep in mind that result does not indicate a return value
            // rather an object stating if the command executed successfully.
            IResult result = await CommandService.ExecuteAsync(context: context, argPos: argPos, services: Services);

            // Optionally, we may inform the user if the command fails
            // to be executed; however, this may not always be desired,
            // as it may clog up the request queue should a user spam a
            // command.
            if (!result.IsSuccess)
                await context.Channel.SendMessageAsync($"Error: {result.ErrorReason}");
        }

        #endregion
    }
}
