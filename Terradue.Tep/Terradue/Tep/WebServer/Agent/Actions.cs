using System;
using Terradue.Portal;

namespace Terradue.Tep {
    public class Actions {

        /// <summary>
        /// Updates the wps providers.
        /// </summary>
        /// <param name="context">Context.</param>
        public static void UpdateWpsProviders(IfyContext context) {
            try {
                var wpsProviders = new EntityList<WpsProvider>(context);
                wpsProviders.Load();

                foreach (var provider in wpsProviders.GetItemsAsList()) {
                    context.WriteInfo(string.Format("UpdateWpsProviders -- WPS {0} - Auto synchro = {1}", provider.Name, provider.AutoSync ? "true" : "false"));
                    if (provider.AutoSync) provider.UpdateProcessOfferings(true);
                }
            } catch (Exception e) {
                context.WriteError(string.Format("UpdateWpsProviders -- {0} - {1}", e.Message, e.StackTrace));
            }
        }
    }
}
