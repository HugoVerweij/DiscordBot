using Discord.WebSocket;
using Honata.Commands.Components;
using Honata.Models.Base;
using System;
using System.Xml.Serialization;

namespace Honata.Models.Servers
{
    [Serializable]
    [XmlType("Server")]
    public class Server : ComponentBase
    {
        [XmlIgnore]
        public SocketGuild Info;

        [XmlAttribute("Identifier")]
        public ulong Identifier { get; set; }

        [XmlAttribute("Prefix")]
        public string Prefix { get; set; } = ">";
    }
}
