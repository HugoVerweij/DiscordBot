using Discord;
using Discord.Audio;
using Discord.WebSocket;
using Honata.Modules.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Xml.Serialization;
using YoutubeExplode.Videos;
using Timer = System.Timers.Timer;

namespace Honata.Commands.Components
{
    public class MusicComponent : ComponentType
    {
        [XmlIgnore] public bool IsPlaying { get; set; }
        [XmlIgnore] public bool IsConnected { get => AClient != null; }
        [XmlIgnore] public bool IsLooping { get; set; }

        [XmlIgnore] public Queue<IVideo> Playlist = new Queue<IVideo>();
        [XmlIgnore] public Timer InactivityCountdown;
        [XmlIgnore] public DateTime? StartTime;
        [XmlIgnore] public TimeSpan? SeekTime;

        [XmlIgnore] public ISocketMessageChannel TChannel;
        [XmlIgnore] public IVoiceChannel VChannel;
        [XmlIgnore] public IAudioClient AClient;

        [XmlIgnore] public AudioOutStream AudioPlayer;
        [XmlIgnore] public CancellationTokenSource CSource = new CancellationTokenSource();

        public string GetTotalLength(bool _exclude = false)
        {
            return new TimeSpan(Playlist.Skip(_exclude ? 1 : 0)
                                        .Select(x => x.Duration)
                                        .Sum(x => ((TimeSpan)x).Ticks))
                                        .ToReadableString();
        }

        public MusicComponent()
        {
            // Initialize the timer.
            InactivityCountdown = new Timer
            {
                Interval = TimeSpan.FromSeconds(10).TotalMilliseconds
            };

            // Subscribe to the elapsed event.
            InactivityCountdown.Elapsed += async (ss, ee) =>
            {
                // Disconnect the bot.
                await VChannel.DisconnectAsync();

                // Create a new embed.
                Embed embed = new EmbedBuilder()
                {
                    Description = $"⛔ Left \"{VChannel.Name}\" due to inactivity.",
                    Color = Colors.Default
                }.Build();

                // Send the new embed.
                await embed.SendEmbed(TChannel);

                // Dispose of the elements.
                Dispose();
            };
        }

        public void Dispose()
        {
            // Reset all the references.
            VChannel = null;
            TChannel = null;
            AClient = null;
            AudioPlayer = null;

            // Reset the music elements.
            Playlist.Clear();
            StartTime = null;
            SeekTime = null;
            IsPlaying = false;
            IsLooping = false;

            // Stop the timer.
            InactivityCountdown.Stop();

            // Reset the CSource.
            CSource = new CancellationTokenSource();
        }
    }
}
