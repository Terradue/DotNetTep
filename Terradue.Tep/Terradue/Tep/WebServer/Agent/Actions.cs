using System;
using Terradue.Portal;

namespace Terradue.Tep {
    public class Actions {

        /// <summary>
        /// Updates the wps providers.
        /// </summary>
        /// <param name="context">Context.</param>
        public static void UpdateWpsProviders(IfyContext context) {
            var wpsProviders = new EntityList<WpsProvider>(context);
            wpsProviders.Load();

            foreach(var provider in wpsProviders.GetItemsAsList()){
                if (provider.AutoSync) provider.UpdateProcessOfferings();
            }
        }
    }
}
