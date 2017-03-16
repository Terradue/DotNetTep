using System;
using System.Collections.Generic;
using ServiceStack.Common.Web;
using ServiceStack.ServiceHost;
using Terradue.Portal;
using Terradue.WebService.Model;

namespace Terradue.Tep.WebServer {

    [Route("/analytics/user/current", "GET", Summary = "GET analytics for current user", Notes = "")]
    public class AnalyticsCurrentUserRequestTep : IReturn<WebAnalytics>{}

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

        public WebAnalytics() {}

        public WebAnalytics(Analytics entity){
            this.CollectionQueriesCount = entity.CollectionQueriesCount;
            this.DataPackageLoadCount = entity.DataPackageLoadCount;
            this.DataPackageItemsLoadCount = entity.DataPackageItemsLoadCount;
            this.WpsJobSubmittedCount = entity.WpsJobSubmittedCount;
            this.WpsJobSuccessCount = entity.WpsJobSuccessCount;
            this.WpsJobFailedCount = entity.WpsJobFailedCount;
        }

    }
}


