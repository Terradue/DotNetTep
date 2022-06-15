using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using ServiceStack.ServiceHost;
using Terradue.WebService.Model;

namespace Terradue.Tep.WebServer {

    [Route("/analytics/user/current", "GET", Summary = "GET analytics for current user", Notes = "")]
    public class AnalyticsCurrentUserRequestTep : IReturn<WebAnalytics>{
        [ApiMember(Name = "startdate", Description = "start date", ParameterType = "query", DataType = "string", IsRequired = false)]
        public string startdate { get; set; }

        [ApiMember(Name = "enddate", Description = "end date", ParameterType = "query", DataType = "string", IsRequired = false)]
        public string enddate { get; set; }
    }

    [Route("/analytics/service/user/current", "GET", Summary = "GET analytics services for current user", Notes = "")]
    public class AnalyticsServicesCurrentUserRequestTep : IReturn<WebAnalytics>{
        [ApiMember(Name = "startdate", Description = "start date", ParameterType = "query", DataType = "string", IsRequired = false)]
        public string startdate { get; set; }

        [ApiMember(Name = "enddate", Description = "end date", ParameterType = "query", DataType = "string", IsRequired = false)]
        public string enddate { get; set; }
    }
    [Route("/analytics/service/community/{Identifier}", "PUT", Summary = "GET analytics services for community", Notes = "")]
    public class AnalyticsServicesCommunityRequestTep : IReturn<WebAnalytics>{
        [ApiMember(Name = "identifier", Description = "user Identifier", ParameterType = "path", DataType = "string", IsRequired = false)]
        public string Identifier { get; set; }
        [ApiMember(Name = "startdate", Description = "start date", ParameterType = "query", DataType = "string", IsRequired = false)]
        public string startdate { get; set; }

        [ApiMember(Name = "enddate", Description = "end date", ParameterType = "query", DataType = "string", IsRequired = false)]
        public string enddate { get; set; }
        [ApiMember(Name = "usernames", Description = "usernames", ParameterType = "query", DataType = "string", IsRequired = false)]
        public string Usernames { get; set; }
    }

    [Route("/analytics/asd/{Id}", "PUT", Summary = "GET analytics for asd", Notes = "")]
    public class AnalyticsAsdRequestTep : IReturn<WebAnalytics>{
        [ApiMember(Name = "id", Description = "asd Identifier", ParameterType = "path", DataType = "string", IsRequired = true)]
        public string Id { get; set; }
        [ApiMember(Name = "startdate", Description = "start date", ParameterType = "query", DataType = "string", IsRequired = false)]
        public string startdate { get; set; }

        [ApiMember(Name = "enddate", Description = "end date", ParameterType = "query", DataType = "string", IsRequired = false)]
        public string enddate { get; set; }
        [ApiMember(Name = "usernames", Description = "usernames", ParameterType = "query", DataType = "string", IsRequired = false)]
        public string Usernames { get; set; }
    }

    [Route("/analytics/service/user/{Identifier}", "GET", Summary = "GET analytics services for user", Notes = "")]
    public class AnalyticsServicesUserRequestTep : IReturn<WebAnalytics>{
        [ApiMember(Name = "identifier", Description = "user Identifier", ParameterType = "path", DataType = "string", IsRequired = false)]
        public string Identifier { get; set; }
        [ApiMember(Name = "startdate", Description = "start date", ParameterType = "query", DataType = "string", IsRequired = false)]
        public string startdate { get; set; }

        [ApiMember(Name = "enddate", Description = "end date", ParameterType = "query", DataType = "string", IsRequired = false)]
        public string enddate { get; set; }
    }

    [Route("/analytics", "GET", Summary = "GET analytics for current user", Notes = "")]
    public class AnalyticsRequestTep : IReturn<WebAnalytics> {
        [ApiMember(Name = "identifier", Description = "user Identifier", ParameterType = "path", DataType = "string", IsRequired = false)]
        public string Identifier { get; set; }

        [ApiMember(Name = "type", Description = "analytics type (user/group/community)", ParameterType = "path", DataType = "string", IsRequired = false)]
        public string Type { get; set; }

        [ApiMember(Name = "startdate", Description = "start date", ParameterType = "query", DataType = "string", IsRequired = false)]
        public string startdate { get; set; }

        [ApiMember(Name = "enddate", Description = "end date", ParameterType = "query", DataType = "string", IsRequired = false)]
        public string enddate { get; set; }
    }

    public class WebAnalytics {

        [ApiMember(Name="CollectionQueriesCount", Description = "Collection Queries Count", ParameterType = "path", DataType = "int", IsRequired = false)]
        public int CollectionQueriesCount { get; set; }

        [ApiMember(Name = "DataPackageCreatedCount", Description = "Data Package Created Count", ParameterType = "path", DataType = "int", IsRequired = false)]
        public int DataPackageCreatedCount { get; set; }

        [ApiMember(Name="DataPackageLoadCount", Description = "Data Package Load Count", ParameterType = "path", DataType = "int", IsRequired = false)]
        public int DataPackageLoadCount { get; set; }

        [ApiMember(Name="DataPackageItemsLoadCount", Description = "Data Package Items Count", ParameterType = "path", DataType = "int", IsRequired = false)]
        public int DataPackageItemsLoadCount { get; set; }

        [ApiMember(Name="WpsJobSubmittedCount", Description = "Wps Job Submitted Count", ParameterType = "path", DataType = "int", IsRequired = false)]
        public int WpsJobSubmittedCount { get; set; }

        [ApiMember(Name="WpsJobSuccessCount", Description = "Wps Job Succeeded Count", ParameterType = "path", DataType = "int", IsRequired = false)]
        public int WpsJobSuccessCount { get; set; }

        [ApiMember(Name="WpsJobFailedCount", Description = "Wps Job Failed Count", ParameterType = "path", DataType = "int", IsRequired = false)]
        public int WpsJobFailedCount { get; set; }

		[ApiMember(Name = "WpsJobOngoingCount", Description = "Wps Job Ongoing Count", ParameterType = "path", DataType = "int", IsRequired = false)]
		public int WpsJobOngoingCount { get; set; }

        [ApiMember(Name = "WpsJobSharedPublicCount", Description = "Wps Job publicly shared Count", ParameterType = "path", DataType = "int", IsRequired = false)]
        public int WpsJobSharedPublicCount { get; set; }

        [ApiMember(Name = "WpsJobSharedRestrictedCount", Description = "Wps Job restrictedly shared Count", ParameterType = "path", DataType = "int", IsRequired = false)]
        public int WpsJobSharedRestrictedCount { get; set; }

        [ApiMember(Name = "WpsJobSharedPrivateCount", Description = "Wps Job not shared Count", ParameterType = "path", DataType = "int", IsRequired = false)]
        public int WpsJobSharedPrivateCount { get; set; }

        [ApiMember(Name = "IconUrl", Description = "Icon Url", ParameterType = "path", DataType = "string", IsRequired = false)]
        public string IconUrl { get; set; }

        [ApiMember(Name = "TotalUsers", Description = "Total Users", ParameterType = "path", DataType = "List<WebKeyValue>", IsRequired = false)]
        public List<WebKeyValue> TotalUsers { get; set; }

        [ApiMember(Name = "ActiveUsers", Description = "Active Users", ParameterType = "path", DataType = "List<WebKeyValue>", IsRequired = false)]
        public List<WebKeyValue> ActiveUsers { get; set; }

        [ApiMember(Name = "TopServices", Description = "Top services used", ParameterType = "path", DataType = "List<WebKeyValue>", IsRequired = false)]
        public List<WebKeyValue> TopServices { get; set; }

        [ApiMember(Name = "AvailableDataCollections", Description = "Available Data Collections", ParameterType = "path", DataType = "List<WebKeyValue>", IsRequired = false)]
        public List<WebKeyValue> AvailableDataCollections { get; set; }

        public WebAnalytics() {}

        public WebAnalytics(Analytics entity){
            this.CollectionQueriesCount = entity.CollectionQueriesCount;
            this.DataPackageCreatedCount = entity.DataPackageCreatedCount;
            this.DataPackageLoadCount = entity.DataPackageLoadCount;
            this.DataPackageItemsLoadCount = entity.DataPackageItemsLoadCount;
            this.WpsJobSubmittedCount = entity.WpsJobSubmittedCount;
            this.WpsJobSuccessCount = entity.WpsJobSuccessCount;
            this.WpsJobFailedCount = entity.WpsJobFailedCount;
			this.WpsJobOngoingCount = entity.WpsJobOngoingCount;
            this.WpsJobSharedPublicCount = entity.WpsJobSharedPublicCount;
            this.WpsJobSharedRestrictedCount = entity.WpsJobSharedRestrictedCount;
            this.WpsJobSharedPrivateCount = entity.WpsJobSharedPrivateCount;
			this.IconUrl = entity.IconUrl;
            this.ActiveUsers = new List<WebKeyValue>();
            var visitor = 0;
            var starter = 0;
            var explorer = 0;
            var administrator = 0;
            if(entity.ActiveUsers != null){
                foreach(var id in entity.ActiveUsers.AllKeys){
                    var level = entity.ActiveUsers[id];
                    switch(level){
                        case "1":
                            visitor++;
                            break;
                        case "2":
                            starter++;
                            break;
                        case "3":
                            explorer++;
                            break;
                        case "4":
                            administrator++;
                            break;
                    }
                }
            }
            this.ActiveUsers.Add(new WebKeyValue("visitor", visitor.ToString()));
            this.ActiveUsers.Add(new WebKeyValue("starter", starter.ToString()));
            this.ActiveUsers.Add(new WebKeyValue("explorer", explorer.ToString()));
            this.ActiveUsers.Add(new WebKeyValue("administrator", administrator.ToString()));
            this.TotalUsers = new List<WebKeyValue>();
            visitor = 0;
            starter = 0;
            explorer = 0;
            administrator = 0;
            if (entity.TotalUsers != null) {
                foreach (var id in entity.TotalUsers.AllKeys) {
                    var level = entity.TotalUsers[id];
                    switch (level) {
                        case "1":
                            visitor++;
                            break;
                        case "2":
                            starter++;
                            break;
                        case "3":
                            explorer++;
                            break;
                        case "4":
                            administrator++;
                            break;
                    }
                }
            }
            this.TotalUsers.Add(new WebKeyValue("visitor", visitor.ToString()));
            this.TotalUsers.Add(new WebKeyValue("starter", starter.ToString()));
            this.TotalUsers.Add(new WebKeyValue("explorer", explorer.ToString()));
            this.TotalUsers.Add(new WebKeyValue("administrator", administrator.ToString()));
            if (entity.TopServices != null) {
                this.TopServices = new List<WebKeyValue>();
                foreach (var kv in entity.TopServices) {
                    this.TopServices.Insert(0, new WebKeyValue(kv.Key, kv.Value.ToString()));
                }
            }
            if (entity.AvailableDataCollections != null) {
                this.AvailableDataCollections = new List<WebKeyValue>();
                foreach (var kv in entity.AvailableDataCollections) {
                    this.AvailableDataCollections.Insert(0, new WebKeyValue(kv.Key, kv.Value));
                }
            }
        }
    }

    public class WebAnalyticsService {

        [ApiMember(Name="Identifier", Description = "Identifier", ParameterType = "path", DataType = "string", IsRequired = false)]
        public string Identifier { get; set; }
        [ApiMember(Name="Name", Description = "Name", ParameterType = "path", DataType = "string", IsRequired = false)]
        public string Name { get; set; }
        [ApiMember(Name="Version", Description = "Name", ParameterType = "path", DataType = "string", IsRequired = false)]
        public string Version { get; set; }
        [ApiMember(Name="AppId", Description = "App Identifier", ParameterType = "path", DataType = "string", IsRequired = false)]
        public string AppId { get; set; }
        [ApiMember(Name="Icon", Description = "Name", ParameterType = "path", DataType = "string", IsRequired = false)]
        public string Icon { get; set; }
        [ApiMember(Name="NbInputs", Description = "Nb inputs", ParameterType = "path", DataType = "int", IsRequired = false)]
        public int NbInputs { get; set; }

         public WebAnalyticsService() {}

        public WebAnalyticsService(ServiceAnalytic entity){
            this.Identifier = entity.Identifier;
            this.Name = entity.Name;
            this.Version = entity.Version;
            this.Icon = entity.Icon;
            this.NbInputs = entity.NbInputs;
            this.AppId = entity.AppId;
        }
    }
}


