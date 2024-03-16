using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Honata.Commands.Components;
using Honata.Models.Servers;
using Honata.Models.Users;
using Honata.Modules.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace Honata
{
    class Program
    {
        #region Variables

        public static Program Instance;

        public DiscordSocketClient Client;
        public CommandHelper CommandHelper;
        public ReactionHelper ReactionHelper;

        public Timer UpdateTimer;

        #endregion

        #region OnLoaded

        static void Main(string[] args) => new Program().Load().GetAwaiter().GetResult();

        private async Task Load()
        {
            // Setup the main clients and handlers.
            Instance = this;
            Client = new DiscordSocketClient(new DiscordSocketConfig()
            {
                MessageCacheSize = 100,
                AlwaysDownloadUsers = true
            });
            CommandHelper = new CommandHelper(Client, new CommandService());
            ReactionHelper = new ReactionHelper(Client);

            // Create the xml directory if it doesn't already exists.
            Directory.CreateDirectory(Paths.XmlDir);

            // Hook into the events.
            HookEvents();

            // Load the commands.
            await CommandHelper.InstallCommandsAsync();

            // Fire up the client itself.
            await Client.LoginAsync(TokenType.Bot, Credentials.Token);
            await Client.StartAsync();

            // Create an infinite loop.
            await Task.Delay(-1);
        }

        #endregion

        #region Hooked Events

        private Task Client_Log(LogMessage arg)
        {
            // Set the exception.
            Exception exception = arg.Exception;

            // Write the log to the console.
            Console.WriteLine($"[{DateTime.Now:hh:mm:ss} | {arg.Severity}] : {exception?.Message ?? arg.Message}");

            return Task.CompletedTask;
        }

        private async Task Client_Ready()
        {
            // Load the saved data.
            await LoadServers();
            await LoadUsers();

            // Load the default components.
            await LoadComponents();

            // Set an update timer.
            UpdateTimer = new Timer((e) =>
            {
                Update();
            }, null, TimeSpan.Zero, TimeSpan.FromMinutes(1));

            // Log upon completion.
            Console.WriteLine($"[{DateTime.Now:hh:mm:ss} | Info] : Loaded {Extensions.Servers.Count()} servers.");
            Console.WriteLine($"[{DateTime.Now:hh:mm:ss} | Info] : Loaded {Extensions.Users.Count()} users.");
        }

        private Task Client_JoinedGuild(SocketGuild arg)
        {
            UpdateServers();

            return Task.CompletedTask;
        }

        private Task Client_LeftGuild(SocketGuild arg)
        {
            // Typecast the socketguilds to a server and check if it exists.
            if (arg.GetServer() is Server)
            {
                // Remove the guild.
                Extensions.Servers.Remove(arg.GetServer());
            }

            return Task.CompletedTask;
        }

        private Task Client_UserJoined(SocketGuildUser arg)
        {
            // Typecast the socketuser to a user and check if it exists.
            if (!(arg.GetUser() is User user))
            {
                // Create a new user based on the socketuser.
                user = new User()
                {
                    Info = arg,
                    Identifier = arg.Id
                };

                // Add the newly found user if it doesn't exists.
                if (!Extensions.Users.Any(x => x.Identifier == user.Identifier))
                    Extensions.Users.Add(user);
            }

            return Task.CompletedTask;
        }

        private Task Client_UserLeft(SocketGuildUser arg)
        {
            return Task.CompletedTask;
        }

        private async Task Client_MessageReceived(SocketMessage arg)
        {
            // Return if the message isn't sent from a guild.
            if (!(arg.Channel is SocketGuildChannel)) return;

            // Grab the guild and the handler.
            SocketGuild guild = (arg.Channel as SocketGuildChannel).Guild;
            DefenseComponent handler = guild?.GetServer()?.GetComponent<DefenseComponent>();

            // Check if the handler isn't null.
            // Or if the author is the bot itself.
            if (handler != null && arg.Author.Id != Client.CurrentUser.Id)
                // Track the user and handle defense.
                await handler.TrackAndDefense(arg);

            // Check if the user isn't a bot.
            if (!arg.Author.IsBot)
                // Update the xp of the user in question.
                Extensions.Users[Extensions.Users.IndexOf(arg.Author.GetUser())].XP += 1;
        }

        private Task Client_UserVoiceStateUpdated(SocketUser su, SocketVoiceState st1, SocketVoiceState st2)
        {
            // Attempt to grab the handler.
            MusicComponent handler = st1.VoiceChannel?.Guild?.GetServer()?.GetComponent<MusicComponent>() ??
                                     st2.VoiceChannel?.Guild?.GetServer()?.GetComponent<MusicComponent>();

            // Return if the handler is null.
            if (handler == null)
                return Task.CompletedTask;

            // Check if the user in question is the bot, and if the "moved" channel is empty.
            // Meaning the bot will have disconnected.
            if (handler.IsConnected && su.Id == Client.CurrentUser.Id && st2.VoiceChannel == null)
            {
                // Log the disconnected.
                Console.WriteLine($"[{DateTime.Now:hh:mm:ss} | Info] : Disconnected from: {st1}");

                // Dispose of the music related elements.
                handler.Dispose();

                // Return upon disposing.
                return Task.CompletedTask;
            }

            // Check if the handler is connected to a voice channel.
            if (handler.IsConnected && handler.VChannel is SocketVoiceChannel channel &&
                // Then we check if the voice channel the bot is connected to,
                // Is either the ingoing or outgoing voice channel.
                (channel == st1.VoiceChannel || channel == st2.VoiceChannel))
            {
                // Check if the bot is the only user in the channel.
                if (channel.Users.Count == 1)
                {
                    // (Re)Start the timer.
                    handler.InactivityCountdown.Stop();
                    handler.InactivityCountdown.Start();
                }
                else
                {
                    // Stop the timer.
                    handler.InactivityCountdown.Stop();
                }
            }

            return Task.CompletedTask;
        }

        #endregion

        #region Methods

        public void HookEvents()
        {
            // Hook into the events.
            Client.Log += Client_Log;
            Client.Ready += Client_Ready;
            Client.JoinedGuild += Client_JoinedGuild;
            Client.LeftGuild += Client_LeftGuild;
            Client.UserJoined += Client_UserJoined;
            Client.UserLeft += Client_UserLeft;
            Client.MessageReceived += Client_MessageReceived;
            Client.UserVoiceStateUpdated += Client_UserVoiceStateUpdated;
        }

        public async void Update()
        {
            // Loop through each component of every server.
            foreach (HashSet<ComponentType> components in Extensions.Servers.Select(x => x.Components).ToList())
                // Fetch the list.
                foreach (ComponentType component in components.ToList())
                    // Fire the update void.
                    await component.Update();

            // Loop through each component of every user.
            foreach (HashSet<ComponentType> components in Extensions.Users.Select(x => x.Components).ToList())
                // Fetch the list.
                foreach (ComponentType component in components.ToList())
                    // Fire the update void.
                    await component.Update();

            // Save the servers and users periodically.
            await SaveHelper.SaveTypeof(typeof(List<Server>), Extensions.Servers, Paths.Servers);
            await SaveHelper.SaveTypeof(typeof(List<User>), Extensions.Users, Paths.Users);
        }

        #region LoadSave

        public Task LoadComponents()
        {
            // Loop through every server.
            foreach (Server server in Extensions.Servers)
            {
                // Add the music component by default.
                server.AddComponent<MusicComponent>();
            }

            return Task.CompletedTask;
        }

        public async Task LoadServers()
        {
            // Typecast the servers and check if the result isn't null.
            if (await LoadHelper.LoadTypeDataAsync(typeof(Server), Paths.Servers) is List<Server> servers && servers != null)
            {
                // Loop through each loaded server.
                foreach (Server server in servers)
                {
                    // Update the server info to be the non-savable data.
                    server.Info = Client.GetGuild(server.Identifier);

                    // Check if the server isn't already in the list.
                    // Check if the server isn't null (meaning they've left.)
                    if (!Extensions.Servers.Any(x => x.Identifier == server.Identifier) && server.Info != null)
                        // Finally, add the server to the list.
                        Extensions.Servers.Add(server);
                }
            }

            // Call the update servers once the servers are loaded.
            await UpdateServers();
        }

        public Task UpdateServers()
        {
            // Loop through the active socketguilds.
            foreach(SocketGuild sguild in Client.Guilds)
            {
                // Typecast the socketguilds to a server and check if it exists.
                if (!(sguild.GetServer() is Server server))
                {
                    // Create a new server based on the socketguilds.
                    server = new Server()
                    {
                        Info = sguild,
                        Identifier = sguild.Id
                    };

                    // Add the newly found server if it doesn't exists.
                    if (!Extensions.Servers.Any(x => x.Identifier == server.Identifier))
                        Extensions.Servers.Add(server);
                }
            }

            return Task.CompletedTask;
        }

        public async Task LoadUsers()
        {
            // Typecast the users and check if the result isn't null.
            if (await LoadHelper.LoadTypeDataAsync(typeof(User), Paths.Users) is List<User> users && users != null)
            {
                // Loop through each loaded user.
                foreach (User user in users)
                {
                    // Update the user info to be the non-savable data.
                    user.Info = Client.GetUser(user.Identifier);

                    // Check if the user isn't already in the list.
                    // Check if the user isn't null (meaning they've left.)
                    if (!Extensions.Users.Any(x => x.Identifier == user.Identifier) && user.Info != null)
                    {
                        // Finally, add the user to the list.
                        Extensions.Users.Add(user);

                        // Loop through the components.
                        foreach (ComponentType component in user.Components)
                            // Add the reference on setup.
                            component.Init(user);

                    }
                }
            }

            // Call the update users once the users are loaded.
            await UpdateUsers();
        }

        public Task UpdateUsers()
        {
            // Loop through the active socketguilds.
            foreach (SocketGuild sguild in Client.Guilds)
            {
                // Loop through the active socketusers.
                foreach (SocketUser suser in sguild.Users)
                {
                    // Typecast the socketuser to a user and check if it exists.
                    if (!(suser.GetUser() is User user))
                    {
                        // Create a new user based on the socketuser.
                        user = new User()
                        {
                            Info = suser,
                            Identifier = suser.Id
                        };

                        // Add the newly found user if it doesn't exists.
                        if (!Extensions.Users.Any(x => x.Identifier == user.Identifier))
                            Extensions.Users.Add(user);
                    }
                }
            }

            return Task.CompletedTask;
        }

        #endregion

        #endregion
    }
}
