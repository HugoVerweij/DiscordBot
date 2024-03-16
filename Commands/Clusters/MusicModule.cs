using Discord;
using Discord.Addons.Interactive;
using Discord.Audio;
using Discord.Commands;
using Discord.Rest;
using Honata.Commands.Components;
using Honata.Models.Servers;
using Honata.Modules.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
using YoutubeExplode;
using YoutubeExplode.Channels;
using YoutubeExplode.Common;
using YoutubeExplode.Playlists;
using YoutubeExplode.Search;
using YoutubeExplode.Videos;
using YoutubeExplode.Videos.Streams;

namespace Honata.Commands.Clusters
{
    public class AudioSettings
    {
        public string Url;
        public TimeSpan Seek;
    }

    public class MusicModule : InteractiveBase
    {
        /// <summary>
        /// The guild specific music reference.
        /// </summary>
        private MusicComponent Music => Context.Guild.GetServer().GetComponent<MusicComponent>();

        /// <summary>
        /// Checks if the bot is allowed to play music under certain conditions.
        /// </summary>
        /// <param name="_handler"></param>
        /// <returns></returns>
        public async Task<bool> CheckConditionsAsync(MusicComponent _handler)
        {
            // Set the channel.
            IVoiceChannel channel = (Context.User as IGuildUser)?.VoiceChannel;

            // Check if the channel is null.
            if (channel == null)
            {
                // Show an error.
                await Context.Channel.SendMessageAsync("Error: Please join a voice channel.");
                return false;
            }

            // Check if the handler is already playing music somewhere else in the server.
            if (_handler.IsConnected && _handler.IsPlaying && _handler.VChannel != channel)
            {
                // Show an error.
                await Context.Channel.SendMessageAsync("Error: I'm already playing music somewhere else.");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Converts the queue to a readable list.
        /// </summary>
        /// <param name="_handler"></param>
        /// <returns></returns>
        private List<string> ToReadableList(MusicComponent _handler)
        {
            // Set the result.
            List<string> result = new List<string>();

            // Set the desc.
            string description = $"♫ Current song: [{_handler?.Playlist?.FirstOrDefault()?.Title ?? "None"}]({_handler?.Playlist?.FirstOrDefault()?.Url ?? ""}).\n\n";

            // Loop through the music.
            for (int i = 1; i < _handler.Playlist.Count + 1; i++)
            {
                // Set the video.
                IVideo video = _handler.Playlist.ToList()[i - 1];

                // Shorten the title if needed
                string title = (video.Title.Length > 38 ? video.Title.Remove(38) + "..." : video.Title).Replace("[", "").Replace("]", "");

                // Update the desc.
                description += $"{i}. [{title}]({video.Url}). {video.Duration.ToReadableString()}.\n";

                // Create a new page on 10.
                if (i % 10 == 0)
                {
                    // Add the result.
                    result.Add(description);
                    description = "";
                }
            }

            // Add the first.
            if (_handler.Playlist.Count != 10) result.Add(description);

            // Return the result.
            return result;
        }

        /// <summary>
        /// Creates a ffmpeg stream and returns the process.
        /// </summary>
        /// <param name="_url"></param>
        /// <param name="_seek"></param>
        /// <returns></returns>
        private Process CreateStream(string _url, TimeSpan? _seek = null)
        {
            // Set the seek if given.
            string seek = _seek == null ? "" : $"-ss {((TimeSpan)_seek).ToString(@"hh\:mm\:ss")}";

            // Check if a seektime was given.
            if (_seek != null)
                // Reset the seektime.
                Music.SeekTime = null;

            // Start the process.
            return Process.Start(new ProcessStartInfo
            {
                FileName = "ffmpeg",
                /*Arguments = $"-reconnect 1 -reconnect_at_eof 1 -reconnect_streamed 1 -reconnect_delay_max 2 {seek} -hide_banner -loglevel panic -i \"{_url}\" -ac 2 -f s16le -ar 48000 pipe:1"*/
                Arguments = $"-reconnect 1 -reconnect_at_eof 1 -reconnect_streamed 1 -reconnect_delay_max 2 {seek} -hide_banner -loglevel panic -i \"{_url}\" -ac 2 -f s16le -ar 48000 pipe:1",
                RedirectStandardOutput = true,
                UseShellExecute = false
            });
        }

        /// <summary>
        /// Plays a stream url through an IAudioClient.
        /// </summary>
        /// <param name="_url">The url to stream.</param>
        /// <returns></returns>
        public async Task PlayAsync(AudioSettings _settings = null)
        {
            // Check if the bot is allowed to play music.
            if (!await CheckConditionsAsync(Music))
                return;

            // Set the channel(s) the user is connected to.
            Music.VChannel = (Context.User as IGuildUser)?.VoiceChannel;
            if (Music.TChannel == null)
                Music.TChannel = Context.Channel;

            // Check if the client is null or not connected.
            if (Music.AClient == null || Music.AClient.ConnectionState != ConnectionState.Connected)
                // Connected the client.
                Music.AClient = await Music.VChannel.ConnectAsync();

            // Check if the url is empty or null.
            if (_settings == null)
            {
                // Create a new settings instance.
                _settings = new AudioSettings();

                // Create a new youtube client.
                YoutubeClient client = new YoutubeClient();

                // Fetch the video's manifest.
                var manifest = await client.Videos.Streams.GetManifestAsync(Music.Playlist.First().Url);

                // Fetch the audio only.
                var stream = manifest.GetAudioOnlyStreams().GetWithHighestBitrate();

                // Set the url.
                _settings.Url = stream.Url;
            }

            // Set the starttime.
            Music.StartTime = Music.SeekTime == null ? DateTime.Now : DateTime.Now.Subtract((TimeSpan)Music.SeekTime);

            try
            {
                // Create a new ffempg based on the stream url.
                using var ffmpeg = CreateStream(_settings?.Url, Music.SeekTime);
                // Create a corresponding stream output.
                using var output = ffmpeg.StandardOutput.BaseStream;
                // Create a corresponding player based on the stream.
                using var player = Music.AClient.CreatePCMStream(AudioApplication.Music, 96000, 1000, 0);
                try
                {
                    // Set the is playing to true.
                    Music.IsPlaying = true;

                    // Use the player to play sound.
                    await output.CopyToAsync(player, Music.CSource.Token);

                    // Set the is playing to false.
                    Music.IsPlaying = false;
                }
                finally
                {
                    // Release the memory from the player.
                    await player.FlushAsync();
                    // Kill the ffmpeg.
                    ffmpeg.Kill();

                    // Reset the guild specific variables.
                    Music.CSource = new CancellationTokenSource();
                    Music.IsPlaying = false;

                    // Check if the handler has looping enabled.
                    if (!Music.IsLooping && Music.SeekTime == null)
                        // Dequeue the first song.
                        Music.Playlist.Dequeue();

                    // Check if there's anything left to play.
                    if (Music.Playlist.Count > 0 && Music.IsConnected)
                    {
                        // Play the damn song!
                        await PlayAsync();
                    }
                }
            }
            catch
            {
                // TODO : Handle exception.
            }
        }

        [Command("play", RunMode = RunMode.Async)]
        [Summary("Play a song via playlist, link, or search terms." +
                 "\n\n**Usage**" +
                 "\n\n*$prefix$play <search term>*" +
                 "\n*$prefix$play <playlist link>*" +
                 "\n\n**Examples**" +
                 "\n\n*$prefix$play avatar lofi mix*" +
                 "\n\n**Additional Parameters**" +
                 "\n\n*None.*")]
        public async Task SearchAsync([Remainder] string _params)
        {
            try
            {
                // Check if the bot is allowed to play music.
                if (!await CheckConditionsAsync(Music))
                    return;

                // Parse the params.
                CommandParams parameters = new CommandParams(_params);

                // Create a new client.
                YoutubeClient client = new YoutubeClient();

                // Check if the params is parsable with a video id.
                if (VideoId.TryParse(_params) != null && VideoId.TryParse(_params) is VideoId videoId)
                {
                    // Fetch the video in question.
                    Video video = await client.Videos.GetAsync(videoId);

                    // Enqueue the video in question.
                    Music.Playlist.Enqueue(video);

                    // Send a response.
                    await new EmbedBuilder()
                    {
                        // Set the title.
                        Title = video.Title,
                        // Set the description.
                        Description = $"♫ Added [{video.Title}]({video.Url}) to the queue.",
                        // Set the footer.
                        Footer = new EmbedFooterBuilder()
                        {
                            Text = $"Queue position: {Music.Playlist.Count}. Time until song: {Music.GetTotalLength(true)}."
                        },
                        // Set the color.
                        Color = Colors.Default
                    }.Build().SendEmbed(Context);

                    // Start the song if the bot isn't already playing music.
                    if (!Music.IsPlaying)
                        await PlayAsync();
                }
                // Check if the params is parsable with a playlist id.
                else if (PlaylistId.TryParse(_params) != null && PlaylistId.TryParse(_params) is PlaylistId playlistId)
                {
                    // Create the new embed.
                    Embed embed = new EmbedBuilder()
                    {
                        Description = "Loading, this may take some time depending on the playlist size.",
                        Color = Colors.Default
                    }.Build();

                    // Send the embed.
                    RestUserMessage message = await embed.SendEmbed(Context);

                    // Get the playlist info / video list.
                    Playlist playlist = await client.Playlists.GetAsync(playlistId);
                    IReadOnlyList<PlaylistVideo> videos = await client.Playlists.GetVideosAsync(playlistId)
                                                                                .CollectAsync();

                    // Edit the embed.
                    await message.EditEmbed(embed.ToEmbedBuilder()
                        .WithTitle(playlist.Title)
                        .WithDescription($"♫ Added [{playlist.Title}]({playlist.Url}) ({videos.Count() + 1} songs) to the queue.")
                        .Build());

                    // Enqueue the videos.
                    foreach (PlaylistVideo video in videos)
                        Music.Playlist.Enqueue(video);

                    // Start the song if the bot isn't already playing music.
                    if (!Music.IsPlaying)
                        await PlayAsync();
                }
                // If both aren't the case, start up a search instead.
                else
                {
                    // Look for videos using a search query.
                    List<VideoSearchResult> results = (List<VideoSearchResult>)await client.Search.GetVideosAsync(_params)
                                                                                                  .CollectAsync(20);

                    // Create the new embed.
                    Embed embed = new EmbedBuilder()
                    {
                        Description = "Searching...",
                        Color = Colors.Default
                    }.Build();

                    // Send the embed, and handle the correct reponses.
                    await Program.Instance.ReactionHelper.SendAwaitResponse(Context, embed, new Dictionary<IEmote, ReactionAction>()
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
                                return Task.FromResult(new ReactionResult(context.Index < results.Count - 1 ? context.Index + 1 : 0));
                            })
                        },
                        {
                            new Emoji("⏭️"),
                            new ReactionAction((context) =>
                            {
                                // Set the index back to full.
                                return Task.FromResult(new ReactionResult(results.Count - 1));
                            })
                        },
                        {
                            new Emoji("✅"),
                            new ReactionAction(async (context) =>
                            {
                                // Enqueue the video in question.
                                Music.Playlist.Enqueue(results[context.Index]);

                                // Update the embed with the corresponding content.
                                await context.Message.EditEmbed(embed.ToEmbedBuilder()
                                    .WithTitle(results[context.Index].Title)
                                    .WithDescription($"♫ Added [{results[context.Index].Title}]({results[context.Index].Url}) to the queue.")
                                    .WithImageUrl("")
                                    .WithFooter($"Queue position: {Music.Playlist.Count}. Time until song: {Music.GetTotalLength(true)}.")
                                    .Build());

                                // Remove the reactions.
                                await context.Message?.RemoveAllReactionsAsync();

                                // Start the song if the bot isn't already playing music.
                                if (!Music.IsPlaying)
                                    await PlayAsync();

                                return new ReactionResult();
                            })
                        },
                        //{
                        //    new Emoji("🎨"),
                        //    new ReactionAction(async (context) =>
                        //    {
                        //        // Set the youtube client.
                        //        YoutubeClient client = new YoutubeClient();
                        //        var test = (PlaylistId)PlaylistId.TryParse($"RD{results[context.Index].Id}");
                        //        // Get the playlist info / video list.
                        //        Playlist playlist = await client.Playlists.GetAsync((PlaylistId)PlaylistId.TryParse($"RD{results[context.Index].Id}"));
                        //        List<PlaylistVideo> videos = await client.Playlists.GetVideosAsync(playlist.Id).ToListAsync();

                        //        // Enqueue the first video.
                        //        Music.Playlist.Enqueue(results[context.Index]);

                        //        // Enqueue the other videos.
                        //        videos.ForEach(x => Music.Playlist.Enqueue(x));

                        //        // Modify the message.
                        //        await context.Message.EditEmbed(embed.ToEmbedBuilder()
                        //            .WithTitle(playlist.Title)
                        //            .WithDescription($"♫ Added [{playlist.Title}]({playlist.Url}) ({videos.Count() + 1} songs) to the queue.")
                        //            .Build());

                        //        // Remove the reactions.
                        //        await context.Message?.RemoveAllReactionsAsync();


                        //        // Start the song if the bot isn't already playing music.
                        //        if (!Music.IsPlaying)
                        //            await PlayAsync();

                        //        return new ReactionResult();
                        //    })
                        //},
                        {
                            new Emoji("⏹️"),
                            new ReactionAction(async (context) =>
                            {
                                await context.Message.DeleteAsync();
                                return new ReactionResult();
                            })
                        },
                    },
                    new ReactionAction(async (context) =>
                    {
                        // Update the embed with the corresponding content.
                        await context.Message.EditEmbed(embed.ToEmbedBuilder()
                            .WithTitle(results[context.Index].Title)
                            .WithDescription($"[Video Url]({results[context.Index].Url})\n*{results[context.Index].Duration.ToReadableString()}.*")
                            .WithImageUrl(results[context.Index]?.Thumbnails?.GetWithHighestResolution().Url ?? "")
                            .WithFooter($"Video {context.Index + 1} / {results.Count()}")
                            .Build());

                        return null;
                    }));
                }
            }
            catch (Exception e)
            {
                // TODO : Throw exception.
                Console.WriteLine(e);
            }
        }

        [Command("stop", RunMode = RunMode.Async)]
        [Summary("Stop and disconnect the bot." +
                 "\n\n**Usage**" +
                 "\n\n*$prefix$stop*" +
                 "\n\n**Examples**" +
                 "\n\n*$prefix$stop*" +
                 "\n\n**Additional Parameters**" +
                 "\n\n*None.*")]
        public async Task StopAsync()
        {
            // Check if the bot is connected.
            if (!Music.IsConnected)
            {
                // Send an error response.
                await Context.Channel.SendMessageAsync("Error: I'm currently not playing any music.");
                return;
            }

            // Stop the song.
            Music.CSource.Cancel();

            // Disconnect.
            await Music.VChannel.DisconnectAsync();

            // Create the new embed.
            Embed embed = new EmbedBuilder()
            {
                Description = $"⛔ Left the channel.",
                Color = Colors.Default
            }.Build();

            // Send the new embed.
            await embed.SendEmbed(Context);
        }

        [Command("queue", RunMode = RunMode.Async)]
        [Summary("Shows the current queue." +
                 "\n\n**Usage**" +
                 "\n\n*$prefix$queue*" +
                 "\n\n**Examples**" +
                 "\n\n*$prefix$queue*" +
                 "\n\n**Additional Parameters**" +
                 "\n\n*None.*")]
        public async Task QueueAsync()
        {
            // Fetch the guild specific playlist.
            List<string> pages = ToReadableList(Music);

            // Create the new embed.
            Embed embed = new EmbedBuilder()
            {
                Description = "Loading...",
                Color = Colors.Default
            }.Build();

            // Create the paginated content.
            ReactionAction paginator = new ReactionAction(async (context) =>
            {
                // Update the list.
                pages = ToReadableList(Music);

                // Update the embed with the corresponding content.
                await context.Message.EditEmbed(embed.ToEmbedBuilder()
                    .WithTitle($"Music queue for {Context.Guild.Name}.")
                    .WithDescription(pages[context.Index])
                    .WithFooter($"Page {context.Index + 1} / {pages.Count()}. Total length: {Music.GetTotalLength()}. Looping: {Music.IsLooping}.")
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
                    new Emoji("🔁"),
                    new ReactionAction((context) =>
                    {
                        // Swap the looping bool.
                        Music.IsLooping = !Music.IsLooping;

                        paginator.Invoke(context);
                        return Task.FromResult(new ReactionResult(context.Index));
                    })
                },
                {
                    new Emoji("🔀"),
                    new ReactionAction((context) =>
                    {
                        // Create a temp playlist list.
                        List<IVideo> temp = Music.Playlist.ToList();

                        // Take away the current video.
                        IVideo current = temp.FirstOrDefault();
                        temp.RemoveAt(0);

                        // Shuffle said list.
                        temp.Shuffle();

                        // Re-add the current video.
                        temp.Insert(0, current);

                        // Re-set the playlist.
                        Music.Playlist = new Queue<IVideo>(temp);

                        paginator.Invoke(context);
                        return Task.FromResult(new ReactionResult(context.Index));
                    })
                },
                {
                    new Emoji("♻️"),
                    new ReactionAction((context) =>
                    {
                        // Create a temp queue.
                        Queue<IVideo> first = new Queue<IVideo>();
                        // Populate it with the first item.
                        first.Enqueue(Music.Playlist.First());

                        // Update the actual queue.
                        Music.Playlist = new Queue<IVideo>(first);

                        paginator.Invoke(context);
                        return Task.FromResult(new ReactionResult(context.Index));
                    })
                },
                {
                    new Emoji("⏹️"),
                    new ReactionAction(async (context) =>
                    {
                        // Delete the message.
                        await context.Message.DeleteAsync();
                        return new ReactionResult();
                    })
                },
            }, paginator);

            // Remove any leftover reactions.
            await response?.RemoveAllReactionsAsync();
        }

        [Command("np", RunMode = RunMode.Async)]
        [Summary("Shows the current song and information about it." +
                 "\n\n**Usage**" +
                 "\n\n*$prefix$np*" +
                 "\n\n**Examples**" +
                 "\n\n*$prefix$np*" +
                 "\n\n**Additional Parameters**" +
                 "\n\n*None.*")]
        public async Task NowplayingAsync()
        {
            // Define the out of try catch variables.
            Embed embed;

            try
            {
                // Set the video and current timespan.
                IVideo video = Music.Playlist.FirstOrDefault();

                // Calculate the elapsed time, and clamp it between 0 and the max value.
                TimeSpan current = new TimeSpan(Math.Clamp(DateTime.Now
                                                                   .Subtract((DateTime)Music.StartTime)
                                                                   .Ticks, 0, ((TimeSpan)video.Duration)
                                                                   .Ticks));

                // Set the visual feedback.
                string visual = "---------------------------------------";

                // Get the percentage of the current time to the end time.
                double percentage = (double)current.Ticks / (double)((TimeSpan)video.Duration).Ticks * 100;

                // Get the index for the dot on the visual and clamp it to the max.
                int index = (int)Math.Clamp(Math.Floor(percentage / 100 * visual.Length), 0, visual.Length - 1);

                // Set the visual with the dot.
                visual = visual.Remove(index, 1).Insert(index, "●");

                // Create the new embed.
                embed = new EmbedBuilder()
                {
                    Description = $"♫ [{video.Title}]({video.Url}).\n**{current:hh\\:mm\\:ss}** | {visual} | **{video.Duration}**",
                    Color = Colors.Default
                }.Build();

                // Set the out of trycatch variables.
                // Send the new embed.
                RestUserMessage response = await Extensions.SendEmbed(embed, Context);
            }
            catch
            {
                // Create the new embed.
                embed = new EmbedBuilder()
                {
                    Description = $"Nothing is playing at the moment.",
                    Color = Colors.Default
                }.Build();

                // Send the new embed.
                await embed.SendEmbed(Context);
            }
        }

        [Command("remove", RunMode = RunMode.Async)]
        [Summary("Removes a song from the queue." +
                 "\n\n**Usage**" +
                 "\n\n*$prefix$remove <song id>*" +
                 "\n\n**Examples**" +
                 "\n\n*$prefix$remove 1*" +
                 "\n\n**Additional Parameters**" +
                 "\n\n*None.*")]
        public async Task RemoveAsync([Remainder] string _params)
        {
            // Set the out of trycatch variables.
            RestUserMessage response = null;
            Embed embed = null;

            try
            {
                // Parse the params.
                CommandParams parameters = new CommandParams(_params);

                // Get the video information
                IVideo video = Music.Playlist.ToList()[int.Parse(parameters.Command[0]) - 1];

                // Remove the video from the queue.
                Music.Playlist = new Queue<IVideo>(Music.Playlist.Where(x => x != video));

                // Create the new embed.
                embed = new EmbedBuilder()
                {
                    Description = $"🧹 Removed [{video.Title}]({video.Url}) from the queue.",
                    Color = Colors.Default
                }.Build();

                // Send the new embed.
                response = await Extensions.SendEmbed(embed, Context);
            }
            catch
            {
                // Create the new embed.
                embed = new EmbedBuilder()
                {
                    Description = $"Error: something went wrong.",
                    Color = Colors.Default
                }.Build();

                // Send the new embed.
                await Extensions.SendEmbed(embed, Context);
            }
        }

        [Command("skip", RunMode = RunMode.Async)]
        [Summary("Skips the current song." +
                 "\n\n**Usage**" +
                 "\n\n*$prefix$skip*" +
                 "\n\n**Examples**" +
                 "\n\n*$prefix$skip*" +
                 "\n\n**Additional Parameters**" +
                 "\n\n*None.*")]
        public async Task SkipAsync()
        {
            // Set the out of reach trycatch variables.
            Embed embed;

            try
            {
                // Get the current video.
                IVideo video = Music.Playlist.First();

                // Cancel the current song.
                Music.CSource.Cancel();

                // Create the new embed.
                embed = new EmbedBuilder()
                {
                    Description = $"⏭ Skipped [{video.Title}]({video.Url}).",
                    Color = Colors.Default
                }.Build();

                // Set the out of trycatch variables.
                // Send the new embed.
                RestUserMessage response = await Extensions.SendEmbed(embed, Context);
            }
            catch
            {
                // Create the new embed.
                embed = new EmbedBuilder()
                {
                    Description = $"Error: something went wrong.",
                    Color = Colors.Default
                }.Build();

                // Send the new embed.
                await Extensions.SendEmbed(embed, Context);
            }
        }

        [Command("seek", RunMode = RunMode.Async)]
        [Summary("Skips the song to a given timestamp." +
                 "\n\n**Usage**" +
                 "\n\n*$prefix$seek <timestamp>*" +
                 "\n\n**Examples**" +
                 "\n\n*$prefix$seek 00:04:59*" +
                 "\n\n**Additional Parameters**" +
                 "\n\n*None.*")]
        public async Task SeekAsync([Remainder] string _params)
        {
            // Set the out of trycatch variables.
            Embed embed;

            try
            {
                // Set the reponse string.
                string resp = string.Empty;

                // Parse the params.
                CommandParams parameters = new CommandParams(_params);

                if (TimeSpan.TryParse(parameters.Command[0], out TimeSpan time))
                {
                    if (time.Ticks > ((TimeSpan)Music.Playlist.First().Duration).Ticks || time.Ticks < 0)
                    {
                        resp = "Error: time not within the video length.";
                    }
                    else
                    {
                        // Set the seektime.
                        Music.SeekTime = time;

                        // Cancel the current song.
                        Music.CSource.Cancel();

                        // Update the resp.
                        resp = $"⏭ Attempting to seek to **{parameters.Command[0]}**.";
                    }
                }
                else
                {
                    // Update the resp.
                    resp = "Error: incorrect format. (hh:mm:ss / 00:04:30).";
                }

                // Create the new embed.
                embed = new EmbedBuilder()
                {
                    Description = resp,
                    Color = Colors.Default
                }.Build();

                // Send the new embed.
                RestUserMessage response = await Extensions.SendEmbed(embed, Context);
            }
            catch
            {
                // Create the new embed.
                embed = new EmbedBuilder()
                {
                    Description = "Nothing is playing at the moment.",
                    Color = Colors.Default
                }.Build();

                // Send the new embed.
                await Extensions.SendEmbed(embed, Context);
            }
        }
    }
}
