using System;
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

        [EntityDataField("price")]
        public double Price { get; set; }

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

        public static StoreService FromWpsName(IfyContext context, string wpsname) {
            StoreService result = new StoreService(context);
            result.WpsName = wpsname;
            result.Load();
            return result;
        }

        public new void Store() {
            if(string.IsNullOrEmpty(this.Identifier)) this.Identifier = Guid.NewGuid().ToString();
            base.Store();
        }

    }
}
