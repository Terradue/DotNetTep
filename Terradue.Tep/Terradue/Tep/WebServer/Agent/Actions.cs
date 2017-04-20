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
                context.LogInfo(new Actions(), string.Format("AGENT -- WPS {0} - Auto synchro = {1}",provider.Name, provider.AutoSync ? "true" : "false"));
                if (provider.AutoSync) provider.UpdateProcessOfferings();
            }
        }
    }
}
