using System;
using System.Collections.Generic;
using System.Linq;
using Terradue.Portal;

namespace Terradue.Tep {

    [EntityTable("service_store", EntityTableConfiguration.Custom, IdentifierField = "identifier", NameField = "name")]
    public class StoreService : Entity {

        [EntityDataField("pack")]
        public string ServicePack { get; set; }

        [EntityDataField("level")]
        public string ServiceLevel { get; set; }

        [EntityDataField("description")]
        public string Description { get; set; }

        [EntityDataField("abstract")]
        public string Abstract { get; set; }

        [EntityDataField("link")]
        public string Link { get; set; }

        // [EntityDataField("tag")]
        // public string Tag { get; set; }

        [EntityDataField("apps")]
        public string Apps { get; set; }

        [EntityDataField("icon_url")]
        public string IconUrl { get; set; }

        [EntityDataField("wps_name")]
        public string WpsName { get; set; }

        [EntityDataField("wps_version")]
        public string WpsVersion { get; set; }

        [EntityDataField("price")]
        public double Price { get; set; }

        [EntityDataField("price_type")]
        public PriceCalculKind PriceType { get; set; }

        [EntityDataField("max_concurrent_inputs")]
        public int MaxConcurrentInputs { get; set; }

        public StoreService(IfyContext context) : base(context) {}

        public override string GetIdentifyingConditionSql() {
            if (Id == 0 && !string.IsNullOrEmpty(WpsName)) return String.Format("t.wps_name='{0}'", WpsName);
            return null;
        }

        public static StoreService FromId(IfyContext context, int id) {
            StoreService result = new StoreService(context);
            result.Id = id;
            result.Load();
            return result;
        }

        public static StoreService FromWpsName(IfyContext context, string wpsname, string version = null) {
            var services = new EntityList<StoreService>(context);
            services.SetFilter("WpsName", wpsname);
            if(!string.IsNullOrEmpty(version))
                services.SetFilter("WpsVersion", new object[]{SpecialSearchValue.Null,version});
            services.Load();
            var items = services.GetItemsAsList();
            if(items.Count > 0) return items[0];
            else return null;            
        }

        public new void Store() {
            if(string.IsNullOrEmpty(this.Identifier)) this.Identifier = Guid.NewGuid().ToString();
            base.Store();
        }

        public List<ThematicApplicationCached> GetApps(){
            // context.ConsoleDebug = true;
            // get all services (name = wps name, version = wps version, available = true)
            // get domain of service -> get app associated to domain
            var services = new EntityList<WpsProcessOffering>(context);
            services.SetFilter("Name", this.WpsName);
            if(!string.IsNullOrEmpty(this.WpsVersion)) services.SetFilter("Version", this.WpsVersion);
            services.SetFilter("Available", "true");
            services.Load();

            var servicesItems = services.GetItemsAsList();

            context.LogDebug(this, string.Format("GetApps for service '{0}' with Version '{1}' -- found {2} services on DB", this.WpsName, this.WpsVersion, servicesItems.Count));

            var apps = new List<ThematicApplicationCached>();
            
            foreach(var service in servicesItems){                
                var servicetep = WpsProcessOfferingTep.FromIdentifier(context, service.Identifier);
                var service_apps = servicetep.GetApps();
                context.LogDebug(this, string.Format("GetApps for service '{0}' -- found {1}", service.Identifier, service_apps.Count));
                foreach(var app in service_apps){
                    if(!apps.Any(a => a.UId == app.UId)){
                        context.LogDebug(this, string.Format("GetApps -- adding"));
                        apps.Add(app);
                    } else context.LogDebug(this, string.Format("GetApps -- not adding"));
                }
            }
            context.LogDebug(context, "GetApps done for this service");
            return apps;
        }

    }

    public enum PriceCalculKind { 
        None = 0,
        PerInput = 1,
        PerInputPair = 2,
        PerJob = 3,
        PerAcquisitionDate = 4
    }
}
