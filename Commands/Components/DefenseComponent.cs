using Discord;
using Discord.WebSocket;
using Honata.Models.Servers;
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
    public class DefenseLimits
    {
        public float MessageLimit;
        public TimeSpan TimeLimit;
        public TimeSpan TimeOut;
    }

    public class DefenseTrackers
    {
        public Dictionary<int, DateTime> MessageTracker = new Dictionary<int, DateTime>();

        private readonly DefenseLimits limits;

        public DefenseTrackers(DefenseLimits _limits)
        {
            limits = _limits;
        }

        public bool Track(DateTime _timestamp)
        {
            // One up the tracker.
            MessageTracker.Add(MessageTracker.Count + 1, _timestamp);

            // Set the first and the last messages.
            // Check if the message limit has more than 2.
            DateTime? prelast = MessageTracker.Count >= 2 ?
                                    // Set the second to last message.
                                    MessageTracker.Values.ElementAt(MessageTracker.Count - 2) :
                                    // Set null if it doesn't contain enough messages.
                                    (DateTime?)null;
            // Check if the messagetracker has at least some messages.
            DateTime? last = MessageTracker.Count > 0 ?
                                // Grab the last message.
                                MessageTracker.Values.Last() :
                                // Set null.
                                (DateTime?)null;

            // Check if the second to last and last messages are set.
            // Check if the time difference between the messages is bigger than the timeout time.
            if (prelast != null && last != null && ((DateTime)last).Subtract((DateTime)prelast) is TimeSpan difference && difference >= limits.TimeOut)
                // Clear the message list.
                MessageTracker.Clear();

            // Check if the user has reached the tracker limit.
            if (MessageTracker.Count >= limits.MessageLimit)
            {
                // Set the first message.
                DateTime first = MessageTracker.Values.First();

                // Check if the total time is lower than the timelimit.
                if (((DateTime)last).Subtract(first) <= limits.TimeLimit)
                {
                    // Return true.
                    return true;
                }
                else
                {
                    // Clear the tracker.
                    MessageTracker.Clear();

                    // Return false.
                    return false;
                }
            }

            // Return false.
            return false;
        }
    }

    public class DefenseComponent : ComponentType
    {
        #region Variables

        [XmlIgnore] public SocketGuild Guild
        {
            set
            {
                guild = value;
                PopulateListeners();
            }
        }

        [XmlIgnore] private SocketGuild guild;
        [XmlIgnore] private readonly Dictionary<ulong, DefenseTrackers> defenseListeners = new Dictionary<ulong, DefenseTrackers>();

        [XmlIgnore] public DefenseLimits Limits = new DefenseLimits()
        {
            MessageLimit = 5,
            TimeLimit = TimeSpan.FromSeconds(2),
            TimeOut = TimeSpan.FromSeconds(1)
        };

        #endregion

        #region OnLoad

        public DefenseComponent()
        {
        }

        #endregion

        #region Methods

        public async Task TrackAndDefense(SocketMessage _message)
        {
            try
            {
                // Track the message and check if it's marked as spam.
                if (defenseListeners[_message.Author.Id].Track(_message.Timestamp.DateTime))
                // Handle the mute.
                {
                    await _message.Channel.SendMessageAsync("Your messages have been marked as spam, and have been muted.");
                }
            }
            catch
            {
                // TODO : Handle exception.
            }
        }

        private Task PopulateListeners()
        {
            // Loop through every user within the guild.
            foreach (SocketUser user in guild.Users)
            {
                // Check if the listeners are already listening.
                if (!defenseListeners.ContainsKey(user.Id))
                    // Add the user to the listener.
                    defenseListeners.Add(user.Id, new DefenseTrackers(Limits));
            }

            return Task.CompletedTask;
        }

        #endregion
    }
}
