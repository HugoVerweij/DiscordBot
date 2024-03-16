using Discord;
using Discord.Commands;
using Discord.Rest;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Honata.Modules.Helpers
{
    /// <summary>
    ///  The result that the reaction delegate will return.
    /// </summary>
    public class ReactionResult
    {
        /// <summary>
        /// The index of the paginated context.
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// The result the reaction returns.
        /// </summary>
        /// <param name="index">The paginated index, return nothing in order to quit out.</param>
        public ReactionResult(int index = -1)
        {
            this.Index = index;
        }
    }

    /// <summary>
    /// The context that the delegate will be provided with.
    /// </summary>
    public class ReactionContext
    {
        /// <summary>
        /// The index of the paginated context.
        /// </summary>
        public int Index { get; set; }
        /// <summary>
        /// The reference to the active <see cref="RestUserMessage"/>
        /// </summary>
        public RestUserMessage Message { get; set; }
    }

    /// <summary>
    /// <see cref="ReactionAction"/> is a delegate responsible for handling certain actions within the <see cref="ReactionHelper"/.>
    /// </summary>
    /// <param name="context">The context of the current reaction.</param>
    /// <returns></returns>
    public delegate Task<ReactionResult> ReactionAction(ReactionContext context);

    public class ReactionSettings
    {
        /// <summary>
        /// The <see cref="SocketUser"/> the class should be tracking.
        /// </summary>
        public SocketUser Source { get; set; }
        /// <summary>
        /// The <see cref="RestUserMessage"/> the class is listening to.
        /// </summary>
        public RestUserMessage Message { get; set; }
        /// <summary>
        /// The most recent <see cref="SocketReaction"/> that was added.
        /// </summary>
        public SocketReaction Reaction { get; set; }
        /// <summary>
        /// The reactions of the message.
        /// </summary>
        public IReadOnlyDictionary<IEmote, ReactionMetadata> Reactions { get; set; }

        /// <summary>
        /// The response the class could be waiting for.
        /// </summary>
        public SocketMessage Response { get; set; }

        /// <summary>
        /// The timeout bool, it gets set to true by <see cref="TimeoutTime"/>.
        /// </summary>
        public bool Timeout = false;
        /// <summary>
        /// The time it takes for the function to timeout.
        /// </summary>
        public int TimeoutTime = 5;
        /// <summary>
        /// If it's a paginated message.
        /// </summary>
        public bool Paginated = false;

        /// <summary>
        /// The settings for the reaction handler.
        /// </summary>
        public ReactionSettings()
        {
            // Start the timeout.
            Task.Run(async () => await TimeoutHandlerAynsc());
        }

        /// <summary>
        /// Timeout funciton, handles the timeout.
        /// </summary>
        /// <returns></returns>
        private async Task TimeoutHandlerAynsc()
        {
            // Wait for 'n' amount of seconds.
            await Task.Delay(TimeSpan.FromSeconds(TimeoutTime));

            // Set timeout to true if no reactions were placed.
            if ((Message != null && Reactions == null) || (Message == null && Response == null))
                Timeout = true;
        }
    }

    public class ReactionHelper
    {
        /// <summary>
        /// The reaction handler itself.
        /// </summary>
        /// <param name="_client">The discord client the class should hook into.</param>
        public ReactionHelper(DiscordSocketClient _client)
        {
            // Subscribe to the events.
            _client.ReactionAdded += Client_ReactionAdded;
            _client.MessageReceived += Client_MessageReceived;
        }

        #region Internal

        /// <summary>
        /// Public listeners, the messages / channels the bot should listen to bound with their settings.
        /// </summary>
        public Dictionary<ulong, ReactionSettings> Listeners = new Dictionary<ulong, ReactionSettings>();

        /// <summary>
        /// The dynamic function that fires everytime a response has been recieved.
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        private async Task Client_MessageReceived(SocketMessage arg)
        {
            try
            {
                // Return if the author is a bot.
                if (!arg.Author.IsBot)
                {
                    // Define empty settings.
                    ReactionSettings settings = null;

                    // Check if the message that was reacted to is being listened to.
                    // Check if the channel of the message is being listened to.
                    if (Listeners != null && Listeners.ContainsKey(arg.Channel.Id))
                    {
                        // Set the settings.
                        settings = Listeners[arg.Channel.Id];

                        // Return if source is enabled and the user doesn't match.
                        if (settings.Source != null && settings.Source.Id != arg.Author.Id) return;

                        // Update the response.
                        Listeners[arg.Channel.Id].Response = arg;
                    }

                }

                // Return the taks.
                await Task.CompletedTask;
            }
            catch
            {
                // TODO : Throw exception.
            }
        }

        /// <summary>
        /// The dynamic function that fires everytime a reaction has been added.
        /// </summary>
        /// <param name="arg1"></param>
        /// <param name="arg2"></param>
        /// <param name="arg3"></param>
        /// <returns></returns>
        private async Task Client_ReactionAdded(Cacheable<IUserMessage, ulong> arg1, ISocketMessageChannel arg2, SocketReaction arg3)
        {
            try
            {
                // Return if the reactor is a bot.
                if (!arg3.User.Value.IsBot)
                {
                    // Define empty settings.
                    ReactionSettings settings = null;

                    // Check if the message that was reacted to is being listened to.
                    if (Listeners != null && Listeners.ContainsKey(arg1.Id))
                        // Set the settings.
                        settings = Listeners[arg1.Id];

                    // Check if the settings are set.
                    if (settings != null && settings.Message.Id == arg1.Id)
                    {
                        // Check if the reaction needs to be removed.
                        if (settings.Paginated)
                            // Remove the reaction.
                            await arg1.Value.RemoveReactionAsync(arg3.Emote, arg3.User.Value);

                        // Return if source is enabled and the user doesn't match.
                        if (settings.Source != null && arg3.User.Value.Id != settings.Source.Id) return;

                        // Update the reaction.
                        Listeners[arg1.Value.Id].Reaction = arg3;
                        Listeners[arg1.Value.Id].Reactions = arg1.Value.Reactions;
                    }

                    // Return the taks.
                    await Task.CompletedTask;
                }
            }
            catch
            {
                // TODO: Throw exception.
            }
        }

        /// <summary>
        /// NextReactionAsync waits and returns the next reaction that is triggered.
        /// </summary>
        /// <param name="_message">The message that it's tracking.</param>
        /// <param name="_settings">The settings for the function.</param>
        /// <returns></returns>
        public async Task<SocketMessage> NextMessageAsync(IMessageChannel _channel, ReactionSettings _settings, int _refresh_rate = 100)
        {
            // Add the channel along with it's settings to the listeners.
            Listeners.Add(_channel.Id, _settings);

            // While the message isn't timed out.
            while (!_settings.Timeout)
            {
                // Check if someone has responded.
                if (_settings.Response != null)
                {
                    // Set the result.
                    SocketMessage result = Listeners[_channel.Id].Response;

                    // Remove the channel from the listener.
                    Listeners.Remove(_channel.Id);

                    // Return the result.
                    return result;
                }

                // Wait with a delay.
                await Task.Delay(_refresh_rate);
            }

            // Remove the channel from the listener.
            Listeners.Remove(_channel.Id);

            // Return null if the reponse timed out.
            return null;
        }

        /// <summary>
        /// NextReactionAsync waits and returns the next reaction that is triggered.
        /// </summary>
        /// <param name="_message">The message that it's tracking.</param>
        /// <param name="_settings">The settings for the function.</param>
        /// <returns></returns>
        public async Task<KeyValuePair<IEmote, ReactionMetadata>> NextReactionAsync(RestUserMessage _message, ReactionSettings _settings, int _refresh_rate = 100)
        {
            // Set the message.
            _settings.Message = _message;
            // Add the message with its settings to the listener.
            Listeners.Add(_message.Id, _settings);

            // While the message isn't timed out.
            while (!_settings.Timeout)
            {
                // Check if someone has reacted.
                if (_settings.Reactions != null)
                {
                    // Set the result.
                    KeyValuePair<IEmote, ReactionMetadata> result = new KeyValuePair<IEmote, ReactionMetadata>(Listeners[_message.Id].Reaction.Emote, new ReactionMetadata());

                    // Remove the message from the listener.
                    Listeners.Remove(_message.Id);

                    // Return the result.
                    return result;
                }

                // Wait with a delay.
                await Task.Delay(_refresh_rate);
            }

            // Remove the message from the listener.
            Listeners.Remove(_message.Id);

            // Return an empty list if the function timed out.
            return new KeyValuePair<IEmote, ReactionMetadata>();
        }

        #endregion

        #region External

        public async Task<RestUserMessage> SendAwaitResponse(SocketCommandContext _context,
                                                             Embed _embed,
                                                             Dictionary<IEmote, ReactionAction> _actions,
                                                             ReactionAction update = null)
        {
            // Set the resp message.
            RestUserMessage message = null;

            try
            {
                // Seperate the found emotes within the actions.
                IEmote[] emotes = _actions.Keys.ToArray();

                // Set the response.
                KeyValuePair<IEmote, ReactionMetadata> response = new KeyValuePair<IEmote, ReactionMetadata>();

                // Send the confirmation message.
                message = await _embed.SendEmbed(_context);

                // Set the response message.
                string resp = string.Empty;
                // Set the index.
                int index = 0;

                // Update the paginated message if it's subscribed to.
                update?.Invoke(new ReactionContext() { Index = index, Message = message });

                // Add the reactions.
                await message?.AddReactionsAsync(emotes);

                // Loop if the cancellation token is set.
                while (true)
                {
                    // Wait for a response.
                    response = await NextReactionAsync(message, new ReactionSettings()
                    {
                        TimeoutTime = 60,
                        Source = _context.Message.Author,
                        Paginated = update != null
                    });

                    // Check for timeout.
                    if (response.Key != null)
                    {
                        // Set the result to be null.
                        ReactionResult result = null;

                        try
                        {
                            // Invoke the corresponding action with context, and fetch the result.
                            result = await _actions[response.Key].Invoke(new ReactionContext() { Index = index, Message = message });
                        }
                        catch
                        {
                            // TODO : Catch exception.
                        }

                        // TODO : Check if the message was deleted manually.

                        // Break out of the while loop if nothing was returned.
                        if (result == null || result.Index == -1) break;

                        // Update the index.
                        index = result.Index;

                        // Update the paginated message if it's subscribed to.
                        update?.Invoke(new ReactionContext() { Index = index, Message = message });
                    }
                    else
                    {
                        // Handle timeout.
                        break;
                    }
                }

                // Remove the reactions.
                await message?.RemoveAllReactionsAsync();

                // Return true upon completion.
                return message;
            }
            catch
            {
                // TODO : Throw an exception.

                // Raise the exception to the user.
                await message?.ModifyAsync(x => x.Embed = _embed.ToEmbedBuilder()
                                                                .WithDescription("Error, something went wrong.")
                                                                .Build());
            }

            // Remove the reactions.
            await message?.RemoveAllReactionsAsync();

            // Return false upon completion.
            return message;
        }

        #endregion
    }
}
