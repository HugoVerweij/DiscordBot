using System;
using System.Xml.Serialization;

namespace Honata.Models.Base
{
    [Serializable]
    public class Entity
    {
        [XmlAttribute("Identifier")]
        public ulong Identifier { get; set; }
    }
}
