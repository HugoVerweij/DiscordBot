using Discord.WebSocket;
using Honata.Models.Base;
using System;
using System.Xml.Serialization;

namespace Honata.Models.Users
{
    [Serializable]
    [XmlType("User")]
    public class User : ComponentBase
    {
        [XmlIgnore]
        public SocketUser Info { get; set; }

        [XmlAttribute("Identifier")]
        public ulong Identifier { get; set; }

        [XmlAttribute("XP")]
        public float XP { get; set; }

        [XmlIgnore]
        public float Level => MathF.Round(XP / 1000);
    }
}
