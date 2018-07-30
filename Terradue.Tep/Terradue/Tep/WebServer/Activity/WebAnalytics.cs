using ServiceStack.ServiceHost;

namespace Terradue.Tep.WebServer {

    [Route("/analytics/user/current", "GET", Summary = "GET analytics for current user", Notes = "")]
    public class AnalyticsCurrentUserRequestTep : IReturn<WebAnalytics>{}

    [Route("/analytics", "GET", Summary = "GET analytics for current user", Notes = "")]
    public class AnalyticsRequestTep : IReturn<WebAnalytics> {
        [ApiMember(Name = "identifier", Description = "user Identifier", ParameterType = "path", DataType = "string", IsRequired = false)]
        public string Identifier { get; set; }

        [ApiMember(Name = "type", Description = "analytics type (user/group/community)", ParameterType = "path", DataType = "string", IsRequired = false)]
        public string Type { get; set; }
    }

    public class WebAnalytics {

        [ApiMember(Name="CollectionQueriesCount", Description = "Collection Queries Count", ParameterType = "path", DataType = "int", IsRequired = false)]
        public int CollectionQueriesCount { get; set; }

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

		[ApiMember(Name = "IconUrl", Description = "Icon Url", ParameterType = "path", DataType = "string", IsRequired = false)]
        public string IconUrl { get; set; }

		[ApiMember(Name = "TotalUsers", Description = "Total Users", ParameterType = "path", DataType = "int", IsRequired = false)]
		public int TotalUsers { get; set; }

		[ApiMember(Name = "ActiveUsers", Description = "Active Users", ParameterType = "path", DataType = "int", IsRequired = false)]
        public int ActiveUsers { get; set; }

        public WebAnalytics() {}

        public WebAnalytics(Analytics entity){
            this.CollectionQueriesCount = entity.CollectionQueriesCount;
            this.DataPackageLoadCount = entity.DataPackageLoadCount;
            this.DataPackageItemsLoadCount = entity.DataPackageItemsLoadCount;
            this.WpsJobSubmittedCount = entity.WpsJobSubmittedCount;
            this.WpsJobSuccessCount = entity.WpsJobSuccessCount;
            this.WpsJobFailedCount = entity.WpsJobFailedCount;
			this.WpsJobOngoingCount = entity.WpsJobOngoingCount;
			this.IconUrl = entity.IconUrl;
			this.ActiveUsers = entity.ActiveUsers;
			this.TotalUsers = entity.TotalUsers;
        }

    }
}


