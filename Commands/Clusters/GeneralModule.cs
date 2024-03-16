using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.Rest;
using Discord.WebSocket;
using Honata.Models.Servers;
using Honata.Modules.Helpers;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Color = Discord.Color;

namespace Honata.Commands.Clusters
{
    public class GeneralModule : InteractiveBase
    {
        [Command("help", RunMode = RunMode.Async)]
        [Summary("Displays all of the commands the bot provides specifically for you." +
                 "\n\n**Usage**" +
                 "\n\n*$prefix$help*" +
                 "\n\n**Examples**" +
                 "\n\n*$prefix$help*" +
                 "\n\n**Additional Parameters**" +
                 "\n\n*None.*")]
        public async Task HelpCommandAsync()
        {
            try
            {
                // Set up the pages and modules to display.
                List<string> pages = new List<string>();
                IOrderedEnumerable<ModuleInfo> modules = Program.Instance.CommandHelper.CommandService.Modules.Where(x => !x.Name.Contains("Owner"))
                                                                                                              .OrderBy(y => y.Name);

                // Fetch the user specific role color.
                SocketRole role = (Context.User as SocketGuildUser).Roles.OrderByDescending(x => x.Position).FirstOrDefault(x => x.Color != Colors.Default);
                Color color = role?.Color ?? Colors.Default;

                // Loop through every module.
                foreach (ModuleInfo module in modules)
                {
                    // Set a default desc.
                    string description = null;

                    // Loop through every command.
                    foreach (CommandInfo command in module.Commands)
                    {
                        // Check if the command actually works.
                        PreconditionResult result = await command.CheckPreconditionsAsync(Context, Program.Instance.CommandHelper.Services);

                        // If the result is a success, add it to the desc.
                        if (command != null && result != null && result.IsSuccess)
                            description +=
                                $"**{command.Aliases?.First().FirstCharToUpper()}**" +
                                $" *({command.Summary?.Replace("$prefix$", Context.Guild.GetServer().Prefix).Split(new[] { '\r', '\n' }).FirstOrDefault() ?? "No summary provided."})*\n";

                    }

                    // Check if the desc isn't null.
                    if (!string.IsNullOrWhiteSpace(description))
                    {
                        // Assign a new stringbuilder.
                        StringBuilder builder = new StringBuilder();

                        // Add the Module / Page name with the desc.
                        builder.AppendLine($"**Commands : {module.Name.Replace("Module", "")}**\n");
                        builder.AppendLine(description);

                        // Add it to the pages.
                        pages.Add(builder.ToString());
                    }
                }

                // Create the embed to send.
                Embed embed = new EmbedBuilder()
                {
                    // Set the author.
                    Author = new EmbedAuthorBuilder
                    {
                        // Fetch the name.
                        Name = Context.User.Username,
                        // Fetch the avatar.
                        IconUrl = Context.User.GetAvatarUrl() ?? Context.User.GetDefaultAvatarUrl()
                    },
                    // Set the color
                    Color = Colors.Default
                // Build the embed.
                }.Build();

                // Update the paginated content.
                ReactionAction paginator = new ReactionAction(async (context) =>
                {
                    // Update the embed with the corresponding content.
                    await context.Message.EditEmbed(embed.ToEmbedBuilder()
                        .WithTitle($"Commands available for you: *(Prefix: {Context.Guild.GetServer().Prefix})*")
                        .WithDescription(pages[context.Index])
                        .WithFooter($"Page {context.Index + 1} / {pages.Count()}.")
                        .Build());

                    return null;
                });

                // Send the embed, and handle the correct reponses.
                RestUserMessage response = await Program.Instance.ReactionHelper.SendAwaitResponse(Context, embed, new Dictionary<IEmote, ReactionAction>()
                {
                    {
                        new Emoji("⏮️"),
                        new ReactionAction((context) =>
                        {
                            // Set the index back to 0.
                            return Task.FromResult(new ReactionResult(0));
                        })
                    },
                    {
                        new Emoji("◀️"),
                        new ReactionAction((context) =>
                        {
                            // Down the index by 1.
                            return Task.FromResult(new ReactionResult(context.Index > 0 ? context.Index - 1 : 0));
                        })
                    },
                    {
                        new Emoji("▶️"),
                        new ReactionAction((context) =>
                        {
                            // Up the index by 1.
                            return Task.FromResult(new ReactionResult(context.Index < pages.Count - 1 ? context.Index + 1 : 0));
                        })
                    },
                    {
                        new Emoji("⏭️"),
                        new ReactionAction((context) =>
                        {
                            // Set the index back to full.
                            return Task.FromResult(new ReactionResult(pages.Count - 1));
                        })
                    },
                    {
                        new Emoji("⏹️"),
                        new ReactionAction(async (context) =>
                        {
                            // Delete the message.
                            await context.Message.DeleteAsync();
                            return null;
                        })
                    },
                }, paginator);
            }
            catch (Exception e)
            {
                // TODO : Handle exception.
                Console.WriteLine(e);
            }
        }

        [Command("help", RunMode = RunMode.Async)]
        [Summary("Gives the summary of a command." +
                 "\n\n**Usage**" +
                 "\n\n*$prefix$help <command>*" +
                 "\n\n**Examples**" +
                 "\n\n*$prefix$help ping*" +
                 "\n\n**Additional Parameters**" +
                 "\n\n*None.*")]
        public async Task HelpCommandAsync([Remainder] string command)
        {
            try
            {
                // Fetch the command.
                CommandInfo result = Program.Instance.CommandHelper.CommandService.Commands.FirstOrDefault(x => x.Name.ToLower() == command.ToLower());

                // Check if the command exsits.
                if (result == null)
                {
                    // If not, handle the excpetion.
                    await Context.Channel.SendMessageAsync("Error: Unknown command.");
                    return;
                }

                // Reply with the summary.
                await ReplyAsync("", false, new EmbedBuilder()
                {
                    // Set the author.
                    Author = new EmbedAuthorBuilder
                    {
                        Name = Context.User.Username,
                        IconUrl = Context.User.GetAvatarUrl() ?? Context.User.GetDefaultAvatarUrl()
                    },
                    // Set the title.
                    Title = $"**{result.Name.FirstCharToUpper()} : {result.Module.Name.Replace("Module", "")}**\n",
                    // Set the description.
                    Description = result?.Summary.Replace("$prefix$", Context.Guild.GetServer().Prefix) ?? "No summary available.",
                    // Set the color.
                    Color = Colors.Default
                }.Build());
            }
            catch
            {
                // TODO : Throw exception.
            }
        }

        [Command("ping")]
        [Summary("Shows the current latency and other additional info for the bot." +
                 "\n\n**Usage**" +
                 "\n\n*$prefix$ping*" +
                 "\n\n**Examples**" +
                 "\n\n*$prefix$ping*" +
                 "\n\n**Additional Parameters**" +
                 "\n\n*None.*")]
        public async Task PingAsync()
        {
            // Send the message.
            RestUserMessage latency = await Context.Message.Channel.SendMessageAsync("Pong!");

            // Set the difference.
            TimeSpan difference = DateTime.Now.Subtract(latency.Timestamp.DateTime);

            // Only take the first 3 digits.
            string result = difference.ToString("fff");

            // Edit the message with an optional embed.
            await latency.ModifyAsync(x =>
            {
                // Set the content to null.
                x.Content = "";
                // Send an embed with it.
                x.Embed = new EmbedBuilder()
                {
                    Description = $":stopwatch: {result} ms\n\n:heartbeat: {Context.Client.Latency} ms",
                    Color = Colors.Default,
                }.Build();
            });
        }

        [Command("prefix")]
        [Summary("Shows the current prefix for the bot." +
                 "\n\n**Usage**" +
                 "\n\n*$prefix$prefix*" +
                 "\n\n**Examples**" +
                 "\n\n*$prefix$prefix*" +
                 "\n\n**Additional Parameters**" +
                 "\n\n*None.*")]
        public async Task ShowPrefixAsync()
        {
            // Send an embed with the corresponding prefix.
            await Context.Message.Channel.SendMessageAsync("", false, new EmbedBuilder()
            {
                // Set the desc.
                Description = $"Prefix: **{Context.Guild.GetServer().Prefix}**",
                // Set the color.
                Color = Colors.Default
            }.Build());
        }

        [RequireUserPermission(GuildPermission.Administrator)]
        [Command("set prefix")]
        [Alias("setprefix")]
        [Summary("Sets the current prefix for the bot." +
                 "\n\n**Usage**" +
                 "\n\n*$prefix$set prefix <prefix>*" +
                 "\n\n**Examples**" +
                 "\n\n*$prefix$set prefix !*" +
                 "\n\n**Additional Parameters**" +
                 "\n\n*None.*")]
        public async Task SetPrefixAsync([Remainder] string _params)
        {
            try
            {
                // Set the prefix.
                string prefix = _params.Split(" ")[0];

                // Edit the guild.
                Extensions.Servers[Extensions.Servers.IndexOf(Context.Guild.GetServer())].Prefix = prefix;

                // Send an embed with the corresponding prefix.
                await Context.Message.Channel.SendMessageAsync("", false, new EmbedBuilder()
                {
                    // Set the desc.
                    Description = $"Updated prefix: **{Context.Guild.GetServer().Prefix}**",
                    // Set the color.
                    Color = Colors.Default
                }.Build());
            }
            catch
            {
                // TODO : Handle Exception
            }
        }

        [Command("send embed")]
        [Alias("sendembed")]
        public async Task SendEmbedAsync([Remainder] string _params)
        {
            try
            {
                // Fetch the params.
                CommandParams param = new CommandParams(_params, true);

                // Fetch the embed details.
                string title = param.GetParam("-t", true);
                string desc = param.GetParam("-d", true);
                string color = param.GetParam("-c", true);

                // Send the embed.
                await new EmbedBuilder()
                {
                    Title = title,
                    Description = desc,
                    Color = (Color)System.Drawing.Color.FromArgb(int.Parse(color, NumberStyles.HexNumber))
                }.Build().SendEmbed(Context.Channel);

                // Delete the message.
                await Context.Message.DeleteAsync();
            }
            catch
            {
                // Send an error message.
                await Context.Channel.SendMessageAsync("Error, something went wrong with the formatting.");
            }
        }

        [Command("edit embed")]
        [Alias("editembed")]
        public async Task EditEmbedAsync([Remainder] string _params)
        {
            try
            {
                // Fetch the params.
                CommandParams param = new CommandParams(_params, true);

                // Fetch the embed details.
                string title = param.GetParam("-t", true);
                string desc = param.GetParam("-d", true);
                string color = param.GetParam("-c", true);

                // Send the embed.
                await new EmbedBuilder()
                {
                    Title = title,
                    Description = desc,
                    Color = (Color)System.Drawing.Color.FromArgb(int.Parse(color, NumberStyles.HexNumber))
                }.Build().SendEmbed(Context.Channel);

                // Delete the message.
                await Context.Message.DeleteAsync();
            }
            catch
            {
                // Send an error message.
                await Context.Channel.SendMessageAsync("Error, something went wrong with the formatting.");
            }
        }

    }
}
