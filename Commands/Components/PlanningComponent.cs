using Discord;
using Discord.WebSocket;
using Honata.Models.Users;
using Honata.Modules.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Honata.Commands.Components
{
    [Serializable]
    [XmlType("Activity")]
    public class Activity
    {
        [XmlAttribute("Task")]
        public string Task { get; set; }

        [XmlAttribute("Channel")]
        public ulong Channel { get; set; }

        [XmlAttribute("Reminder")]
        public bool Reminder { get; set; }

        [XmlAttribute("Expire_Date")]
        public DateTime Expire { get; set; } 
    }

    public class PlanningComponent : ComponentType
    {
        [XmlIgnore]
        private new User Parent => base.Parent as User;

        [XmlElement("Activities")]
        public List<Activity> Activities = new List<Activity>();

        public async override Task Update()
        {
            try
            {
                // Check if any of the activities are expired.
                if (Activities.Count > 0 && Activities.Any(x => x.Expire <= DateTime.Now))
                {
                    // Loop through each expired activities.
                    foreach (Activity activity in Activities.Where(x => x.Expire <= DateTime.Now).ToList())
                    {
                        // Fetch the correct channel to send the reminder to.
                        IMessageChannel channel = (IMessageChannel)Client.GetChannel(activity.Channel);

                        // Fetch the private channel if the channel resulted in a null value.
                        channel ??= await Parent.Info.GetOrCreateDMChannelAsync();

                        // Create an embed.
                        Embed embed = new EmbedBuilder()
                        {
                            // Set the author.
                            Author = new EmbedAuthorBuilder
                            {
                                Name = Parent.Info.Username,
                                IconUrl = Parent.Info.GetAvatarUrl() ?? Parent.Info.GetDefaultAvatarUrl()
                            },
                            Title = $"**Reminder**",
                            // Set the desc.
                            Description = $"*{activity.Task}*",
                            // Set the color.
                            Color = Colors.Default
                        }.Build();

                        // Send the reminder.
                        await channel.SendMessageAsync(channel is IDMChannel ? "" : $"<@{Parent.Info.Id}>", false, embed);

                        // Delete the activity.
                        Activities.Remove(activity);
                    }
                }

                // Check if there are any activities remaining.
                if (Activities.Count <= 0)
                    // Remove the component.
                    Parent.RemoveComponent<PlanningComponent>();
            }
            catch (Exception e)
            {
                // TODO : Throw excpetion.
                Console.WriteLine(e);
            }
        }
    }
}
