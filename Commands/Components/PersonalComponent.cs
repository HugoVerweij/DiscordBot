using Discord;
using Discord.WebSocket;
using Honata.Models.Users;
using Honata.Modules.Helpers;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Honata.Commands.Components
{
    public class PersonalComponent : ComponentType
    {
        [XmlIgnore]
        private new User Parent => base.Parent as User;

        [XmlAttribute("Nickname")]
        public string Nickname { get; set; } = string.Empty;

        [XmlAttribute("Birthday")]
        public DateTime Birthday { get; set; }

        [XmlAttribute("Location")]
        public string Location { get; set; } = string.Empty;

        public async override void Setup()
        {
            await SendMessage($"Hi there, {Parent.Info.Username}.");
            await SendMessage("It appears we've never met before, I'm honata and I'll be in your care from now on.");
            await SendMessage("Would you mind telling me how you'd like to be adressed before we continue?");

            SocketMessage responseName = await AwaitResponse(10);

            if (responseName == null)
            {
                await SendMessage("I'm sorry but I didn't catch that, can you tell me your Name / Nickname?.");

                responseName = await AwaitResponse();

                if (await CheckIgnore(responseName)) return;
            }

            Nickname = responseName.Content;

            await SendMessage($"Splendid, it's nice to meet you {Nickname}!");
            await SendMessage("If you'd like to stop the setup at any point, you can simply ignore my questions.");
            await SendMessage("You can also change any settings later on, or even completely rerun the setup process.");

            await SendMessage("With that out of the way, I should probably introduce myself.");
            await SendMessage("I'm Honata, your personal assistent; ready to do all kind of things for you.");
            await SendMessage("This includes but is not limited to, waking you up in the morning, sending you reminders, showing you news, etc.");
            await SendMessage("One of these features is showing you the weather, would you mind telling me where you roughly live?");
            await SendMessage("This'll help with providing accurate data about... the weather!");

            SocketMessage responseLocation = await AwaitResponse();

            if (await CheckIgnore(responseName)) return;

            Location = responseLocation.Content;

            await SendMessage("Thank you, I don't think I've ever been there before; but judging from my data it looks like a lovely place!");
            await SendMessage("This'll be all for the setup for now, feel free to change anything at a later date if you so desire.");
            await SendMessage("This is the personal information I've been able to gather from this conversation:");

            // Create an embed.
            await new EmbedBuilder()
            {
                // Set the author.
                Author = new EmbedAuthorBuilder
                {
                    Name = Nickname,
                    IconUrl = Parent.Info.GetAvatarUrl() ?? Parent.Info.GetDefaultAvatarUrl()
                },
                // Set the desc.
                Description = "***Identity***\n\n" +
                              $"**Username:** '{Parent.Info.Username}'.\n" +
                              $"**Nickname:** '{Nickname}'.\n" +
                              $"**Location:** '{Location}'." +
                              "\n\n***Account***\n\n" +
                              $"**Account Age:** '{DateTime.Now.Subtract(Parent.Info.CreatedAt.DateTime).ToReadableString()}'.\n" +
                              $"**Account Level:** '{Parent.Info.GetUser().Level}'.",
                // Set the color.
                Color = Colors.Default
            }.Build().SendEmbed((ISocketMessageChannel)await Parent.Info.GetOrCreateDMChannelAsync());

            await SendMessage($"You'll be hearing from me soon {Nickname}, have a nice day!");
        }

        private async Task<IUserMessage> SendMessage(string _message)
        {
            // Enter typing state.
            IDisposable typing = (await Parent.Info.GetOrCreateDMChannelAsync()).EnterTypingState();

            // Wait a few seconds.
            await Task.Delay(TimeSpan.FromSeconds(new Random().Next(3, 3 + _message.Length / 10)));

            // Exit typing state.
            typing.Dispose();

            // Send the message.
            return await Parent.Info.SendMessageAsync(_message);
        }

        private async Task<SocketMessage> AwaitResponse(int _timeout = 60)
        {
            // Wait for a response.
            return await Program.Instance.ReactionHelper.NextMessageAsync(await Parent.Info.GetOrCreateDMChannelAsync(), new ReactionSettings()
            {
                TimeoutTime = _timeout,
                Source = Parent.Info
            });
        }

        private async Task<bool> CheckIgnore(SocketMessage _message)
        {
            // Check if the message is null.
            if (_message == null)
            {
                // Send a short resopnse and return true.
                await SendMessage("I'm sorry for bothering you, feel free to resume the setup at any other time.");
                return true;
            }

            // Return false.
            return false;
        }

    }
}
