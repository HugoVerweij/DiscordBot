using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Honata.Models.Servers;
using Honata.Models.Users;
using Honata.Modules.Helpers;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Honata.Commands.Clusters
{
    [RequireOwner]
    public class OwnerModule : InteractiveBase
    {
        [Command("quit", RunMode = RunMode.Async)]
        public async Task StopAsync()
        {
            try
            {
                // Create an embed.
                Embed embed = new EmbedBuilder()
                {
                    // Set the author.
                    Author = new EmbedAuthorBuilder
                    {
                        Name = Context.User.Username,
                        IconUrl = Context.User.GetAvatarUrl() ?? Context.User.GetDefaultAvatarUrl()
                    },
                    // Set the desc.
                    Description = $"Are you **sure** you'd like to shut me down?",
                    // Set the color.
                    Color = Colors.Moderation
                }.Build();

                // Send the embed, and handle the correct reponses.
                await Program.Instance.ReactionHelper.SendAwaitResponse(Context, embed, new Dictionary<IEmote, ReactionAction>()
                {
                    {
                        new Emoji("✅"),
                        new ReactionAction(async (context) =>
                        {
                            // Save the data.
                            await SaveHelper.SaveTypeof(typeof(List<Server>), Extensions.Servers, Paths.Servers);
                            await SaveHelper.SaveTypeof(typeof(List<User>), Extensions.Users, Paths.Users);

                            // Remove the reactions.
                            await context.Message.RemoveAllReactionsAsync();

                            // Modify the embed.
                            await context.Message.EditEmbed(embed.ToEmbedBuilder()
                                                 .WithDescription("Shutting down in **3** seconds :skull_crossbones:.")
                                                 .Build());

                            // Wait 3 seconds.
                            await Task.Delay(TimeSpan.FromSeconds(3));

                            // Close the enviroment.
                            Environment.Exit(0);
                            return new ReactionResult();
                        })
                    },
                    {
                        new Emoji("❎"),
                        new ReactionAction(async (context) =>
                        {
                            // Abort.
                            await context.Message.DeleteAsync();
                            return new ReactionResult();
                        })
                    }
                });
            }
            catch
            {
                // TODO : Throw exception.
            }
        }
    }
}
