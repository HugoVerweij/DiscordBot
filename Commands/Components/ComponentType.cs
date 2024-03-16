using Discord.WebSocket;
using Honata.Models.Base;
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Honata.Commands.Components
{
    [XmlInclude(typeof(PlanningComponent))]
    [XmlInclude(typeof(PersonalComponent))]
    [XmlInclude(typeof(MusicComponent))]
    public abstract class ComponentType
    {
        [XmlIgnore]
        public DiscordSocketClient Client => Program.Instance.Client;

        [XmlIgnore]
        public ComponentBase Parent;

        public void Init(ComponentBase _parent)
        {
            Parent = _parent;
        }

        public virtual void Setup()
        {
        }

        public virtual Task Update()
        {
            return Task.CompletedTask;
        }
    }
}
