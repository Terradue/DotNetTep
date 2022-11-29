using System;
using System.Collections.Generic;
using System.Configuration;

namespace Terradue.Tep {

     public class PublishConfiguration : ConfigurationSection {
        
        [ConfigurationProperty("Types", IsDefaultCollection = false)]
        [ConfigurationCollection(typeof(ConfigurationElement),
            AddItemName = "add",
            ClearItemsName = "clear",
            RemoveItemName = "remove")]
        public ConfigurationCollection Types
        {
            get
            {
                return (ConfigurationCollection)base["Types"];
            }
        }
    }

}