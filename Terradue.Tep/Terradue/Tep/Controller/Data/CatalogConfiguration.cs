using System;
using System.Collections.Generic;
using System.Configuration;

namespace Terradue.Tep {

     public class CatalogConfiguration : ConfigurationSection {
        
        [ConfigurationProperty("Catalogs", IsDefaultCollection = false)]
        [ConfigurationCollection(typeof(ConfigurationElement),
            AddItemName = "add",
            ClearItemsName = "clear",
            RemoveItemName = "remove")]
        public ConfigurationCollection Catalogs
        {
            get
            {
                return (ConfigurationCollection)base["Catalogs"];
            }
        }
    }

}