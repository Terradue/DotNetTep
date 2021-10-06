using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;

namespace Terradue.Tep {

    /// <summary>
    /// Represents a EventLogConfiguration section within a configuration file.
    /// </summary>
    public class EventLogConfiguration : ConfigurationSection {
        
        [ConfigurationProperty("Settings", IsDefaultCollection = false)]
        [ConfigurationCollection(typeof(ConfigurationElement),
            AddItemName = "add",
            ClearItemsName = "clear",
            RemoveItemName = "remove")]
        public EventLogCollection Settings
        {
            get
            {
                return (EventLogCollection)base["Settings"];
            }
        }

        [ConfigurationProperty("Missions", IsDefaultCollection = false)]
        [ConfigurationCollection(typeof(ConfigurationElement),
            AddItemName = "add",
            ClearItemsName = "clear",
            RemoveItemName = "remove")]
        public EventLogCollection Missions
        {
            get
            {
                return (EventLogCollection)base["Missions"];
            }
        }
    }

     public class EventLogElementConfiguration : ConfigurationElement {

        [ConfigurationProperty("key", IsRequired = true, IsKey = true)]
        public string Key {
            get {
                return (string)this["key"];
            }
            set {
                this["key"] = value;
            }
        }

        [ConfigurationProperty("value", IsRequired = true, IsKey = true)]
        public string Value {
            get {
                return (string)this["value"];
            }
            set {
                this["value"] = value;
            }
        }
    }

    public class EventLogCollection : ConfigurationElementCollection
    {
        public EventLogCollection(){}

        public new EventLogElementConfiguration this[string key]
        {
        get { return (EventLogElementConfiguration)BaseGet(key); }      
        }

        public void Add(EventLogElementConfiguration config)
        {
        BaseAdd(config);
        }

        public void Clear()
        {
        BaseClear();
        }

        protected override ConfigurationElement CreateNewElement()
        {
        return new EventLogElementConfiguration();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
        return ((EventLogElementConfiguration) element).Key;
        }

        public void Remove(EventLogElementConfiguration config)
        {
        BaseRemove(config.Key);
        }

        public void RemoveAt(int index)
        {
        BaseRemoveAt(index);
        }

        public void Remove(string name)
        {
        BaseRemove(name);
        }
    }

}
