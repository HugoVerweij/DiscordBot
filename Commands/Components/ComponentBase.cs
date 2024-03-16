using Honata.Commands.Components;
using Honata.Modules.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Xml.Serialization;

namespace Honata.Models.Base
{
    [Serializable]
    [XmlType("Component")]
    public class ComponentBase
    {
        #region Variables

        [XmlElement("Components")]
        public readonly HashSet<ComponentType> Components = new HashSet<ComponentType>();

        #endregion

        #region Methods

        public bool ContainsComponent<T>() where T : ComponentType
        {
            // Check if the component list contains the type given.
            return Components.ContainsType(typeof(T));
        }

        public T GetComponent<T>() where T : ComponentType
        {
            // Return null if the component list doesn't contain the type.
            if (!Components.ContainsType(typeof(T))) return null;

            // Return the found component.
            return (T)Components.Where(x => x.GetType().Equals(typeof(T))).First();
        }

        public T AddComponent<T>() where T : ComponentType
        {
            // Return if the component is already added.
            if (Components.ContainsType(typeof(T))) return GetComponent<T>();

            // Create a new instance of type T.
            ComponentType component = (ComponentType)Activator.CreateInstance(typeof(T));

            // Add the compoment.
            Components.Add(component);

            // Run the setup if it has one.
            component.Init(this);
            component.Setup();

            // Return the component.
            return (T)component;
        }

        public void RemoveComponent<T>() where T : ComponentType
        {
            // Return if the component is not added.
            if (!Components.ContainsType(typeof(T))) return;

            // Get the component that needs to be removed.
            ComponentType component = Components.Where(x => x.GetType() == typeof(T)).First();

            // Remove the component.
            Components.Remove(component);
        }

        #endregion
    }
}
