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
        public ConfigurationCollection Settings
        {
            get
            {
                return (ConfigurationCollection)base["Settings"];
            }
        }

        [ConfigurationProperty("Missions", IsDefaultCollection = false)]
        [ConfigurationCollection(typeof(ConfigurationElement),
            AddItemName = "add",
            ClearItemsName = "clear",
            RemoveItemName = "remove")]
        public ConfigurationCollection Missions
        {
            get
            {
                return (ConfigurationCollection)base["Missions"];
            }
        }
    }

     public class ElementConfiguration : ConfigurationElement {

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

    public class ConfigurationCollection : ConfigurationElementCollection
    {
        public ConfigurationCollection(){}

        public new ElementConfiguration this[string key]
        {
        get { return (ElementConfiguration)BaseGet(key); }      
        }

        public void Add(ElementConfiguration config)
        {
        BaseAdd(config);
        }

        public void Clear()
        {
        BaseClear();
        }

        protected override ConfigurationElement CreateNewElement()
        {
        return new ElementConfiguration();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
        return ((ElementConfiguration) element).Key;
        }

        public void Remove(ElementConfiguration config)
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
