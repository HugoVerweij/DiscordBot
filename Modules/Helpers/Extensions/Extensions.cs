using Discord;
using Discord.Commands;
using Discord.Rest;
using Discord.WebSocket;
using Honata.Models.Servers;
using Honata.Models.Users;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace Honata.Modules.Helpers
{
    public static class Extensions
    {
        #region Variables

        public static List<Server> Servers = new List<Server>();
        public static List<User> Users = new List<User>();
        private static Random rng = new Random();

        #endregion

        #region GetEntity

        /// <summary>
        /// Fetches a server with the corresponding <see cref="SocketGuild"/>.
        /// </summary>
        /// <param name="_guild"></param>
        /// <returns></returns>
        public static Server GetServer(this SocketGuild _guild) => GetServerLocal(_guild.Id);

        /// <summary>
        /// Fetches a server with the corresponding id.
        /// </summary>
        /// <param name="_id"></param>
        /// <returns></returns>
        public static Server GetServer(ulong _id) => GetServerLocal(_id);

        private static Server GetServerLocal(ulong _id)
        {
            try
            {
                // Fetch the matching server.
                IEnumerable<Server> results = Servers.Where(x => x.Info.Id == _id);

                // Check if any results were found.
                if (results.Count() <= 0)
                {
                    // TODO: Throw exception.
                }
                else
                {
                    // Return the first (and only) result.
                    return results.FirstOrDefault();
                }
            }
            catch
            {
                // TODO: Throw exception.
            }

            // Return null if nothing was found.
            return null;
        }

        /// <summary>
        /// Fetches a user with the corresponding <see cref="SocketUser"/>.
        /// </summary>
        /// <param name="_guild"></param>
        /// <returns></returns>
        public static User GetUser(this SocketUser _user) => GetUserLocal(_user.Id);

        /// <summary>
        /// Fetches a user with the corresponding id.
        /// </summary>
        /// <param name="_id"></param>
        /// <returns></returns>
        public static User GetUser(ulong _id) => GetUserLocal(_id);

        private static User GetUserLocal(ulong _id)
        {
            try
            {
                // Fetch the matching server.
                IEnumerable<User> results = Users.Where(x => x.Info.Id == _id);

                // Check if any results were found.
                if (results.Count() <= 0)
                {
                    // TODO: Throw exception.
                }
                else
                {
                    // Return the first (and only) result.
                    return results.FirstOrDefault();
                }
            }
            catch
            {
                // TODO: Throw exception.
            }

            // Return null if nothing was found.
            return null;
        }

        #endregion

        #region Embed

        public static async Task<RestUserMessage> SendEmbed(this Embed _embed, ISocketMessageChannel _channel)
        {
            return await SendEmbedLocal(_embed, _channel);
        }

        public static async Task<IUserMessage> SendEmbed(this Embed _embed, SocketUser _user)
        {
            return await _user.SendMessageAsync("", false, _embed);
        }

        public static async Task<RestUserMessage> SendEmbed(this Embed _embed, SocketCommandContext _context)
        {
            return await SendEmbedLocal(_embed, _context.Channel);
        }

        private static async Task<RestUserMessage> SendEmbedLocal(Embed _embed, ISocketMessageChannel _channel)
        {
            // Return upon null channel.
            if (_channel == null)
                return null;

            return await _channel.SendMessageAsync("", false, _embed);
        }

        public static async Task EditEmbed(this RestUserMessage _message, Embed _embed)
        {
            await _message.ModifyAsync(x => { x.Embed = _embed; });
        }

        #endregion

        #region String/List Modification

        public static string FirstCharToUpper(this string input) =>
        input switch
        {
            null => throw new ArgumentNullException(nameof(input)),
            "" => throw new ArgumentException($"{nameof(input)} cannot be empty", nameof(input)),
            _ => input.First().ToString().ToUpper() + input.Substring(1)
        };

        public static string ToReadableString(this TimeSpan? span)
        {
            return ToReadableString((TimeSpan)span);
        }

        public static string ToReadableString(this TimeSpan span)
        {
            string formatted = string.Format("{0}{1}{2}{3}",
                span.Duration().Days > 0 ? string.Format("{0:0} day{1}, ", span.Days, span.Days == 1 ? string.Empty : "s") : string.Empty,
                span.Duration().Hours > 0 ? string.Format("{0:0} hour{1}, ", span.Hours, span.Hours == 1 ? string.Empty : "s") : string.Empty,
                span.Duration().Minutes > 0 ? string.Format("{0:0} minute{1}, ", span.Minutes, span.Minutes == 1 ? string.Empty : "s") : string.Empty,
                span.Duration().Seconds > 0 ? string.Format("{0:0} second{1}", span.Seconds, span.Seconds == 1 ? string.Empty : "s") : string.Empty);

            if (formatted.EndsWith(", ")) formatted = formatted.Substring(0, formatted.Length - 2);

            if (string.IsNullOrEmpty(formatted)) formatted = "0 seconds";

            return formatted;
        }

        public static void Shuffle<T>(this IList<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        public static bool ContainsType<T>(this ICollection<T> list, Type type)
        {
            return list.Any(x => x.GetType().Equals(type));
        }

        #endregion
    }
}
