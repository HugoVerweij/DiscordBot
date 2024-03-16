using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Honata.Commands.Components;
using Honata.Modules.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Honata.Commands.Clusters
{
    public class PersonalModule : InteractiveBase
    {
        [Command("personal", RunMode = RunMode.Async)]
        public async Task SetPersonalAsync()
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
                    Description = Context.Message.Author.GetUser().ContainsComponent<PersonalComponent>() ?
                                    $"Personalisation Status: **Enabled**." :
                                    $"Personalisation Status: **Disabled**.",
                    // Set the color.
                    Color = Colors.Default
                }.Build();

                // Send the embed, and handle the correct reponses.
                await Program.Instance.ReactionHelper.SendAwaitResponse(Context, embed, new Dictionary<IEmote, ReactionAction>()
                {
                    {
                        new Emoji("✅"),
                        new ReactionAction(async (context) =>
                        {
                            // Add the personal component.
                            Context.Message.Author.GetUser().AddComponent<PersonalComponent>();

                            // Modify the embed.
                            await context.Message.EditEmbed(embed.ToEmbedBuilder()
                                                 .WithDescription("Personalisation Status: **Enabled**.")
                                                 .Build());

                            return new ReactionResult();
                        })
                    },
                    {
                        new Emoji("❎"),
                        new ReactionAction(async (context) =>
                        {
                            // Remove the personal component.
                            Context.Message.Author.GetUser().RemoveComponent<PersonalComponent>();

                            // Modify the embed.
                            await context.Message.EditEmbed(embed.ToEmbedBuilder()
                                                 .WithDescription("Personalisation Status: **Disabled**.")
                                                 .Build());

                            return new ReactionResult();
                        })
                    },
                    {
                        new Emoji("⏹️"),
                        new ReactionAction(async (context) =>
                        {
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

        [Command("remind", RunMode = RunMode.Async)]
        [Alias("remindme", "remind me")]
        public async Task AddReminderAsync([Remainder] string _params)
        {
            try
            {
                // Fetch the params.
                CommandParams param = new CommandParams(_params, true);

                // Set the target time.
                DateTime time = DateTime.Now;

                // Add days.
                time = time.AddDays(Convert.ToDouble(param.GetParam("-d", true)));
                // Add hours.
                time = time.AddHours(Convert.ToDouble(param.GetParam("-h", true)));
                // Add minutes.
                time = time.AddMinutes(Convert.ToDouble(param.GetParam("-m", true)));
                // Add seconds.
                time = time.AddSeconds(Convert.ToDouble(param.GetParam("-s", true)));

                // Set the reminder tasks.
                string activity = param.GetParam("-a", true);
                ulong channel = !string.IsNullOrEmpty(param.GetParam("-c", true)) ?
                                Convert.ToUInt64(param.GetParam("-c", true)) :
                                Context.Channel.Id;

                // Fetch the planning component for the user.
                // Add the module if it does not exist yet.
                PlanningComponent planning = Context.Message.Author.GetUser().AddComponent<PlanningComponent>();

                // Create a new activity.
                Activity reminder = new Activity()
                {
                    Task = activity,
                    Expire = time,
                    Reminder = true,
                    Channel = _params.Contains("-p") ?
                              0 :
                              channel
                };

                // Check if activities already contains the same reminder.
                // If not, add the reminder.
                if (!planning.Activities.Contains(reminder))
                    planning.Activities.Add(reminder);

                // Create an embed.
                await new EmbedBuilder()
                {
                    // Set the author.
                    Author = new EmbedAuthorBuilder
                    {
                        Name = Context.User.Username,
                        IconUrl = Context.User.GetAvatarUrl() ?? Context.User.GetDefaultAvatarUrl()
                    },
                    Title = $"**Reminder added.**",
                    // Set the desc.
                    Description = $"*{reminder.Task}*\n\n" +
                                  "***Expiry date***\n\n" +
                                  $"*{reminder.Expire.Subtract(DateTime.Now).ToReadableString()}*",
                    // Set the color.
                    Color = Colors.Default
                }.Build().SendEmbed(Context.Channel);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                // TODO : Handle exception.
                await Context.Channel.SendMessageAsync("Error, something went wrong with the formatting.");
            }
        }
    }
}
