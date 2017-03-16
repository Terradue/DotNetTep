using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Web;
using ServiceStack.Common.Web;
using ServiceStack.ServiceHost;
using Terradue.OpenSearch;
using Terradue.OpenSearch.Engine;
using Terradue.OpenSearch.Result;
using Terradue.OpenSearch.Schema;
using Terradue.Portal;
using Terradue.Tep.WebServer;

namespace Terradue.Tep.WebServer.Services {
    
    [Api("Tep Terradue webserver")]
    [Restrict(EndpointAttributes.InSecure | EndpointAttributes.InternalNetworkAccess | EndpointAttributes.Json,
              EndpointAttributes.Secure | EndpointAttributes.External | EndpointAttributes.Json)]
    public class AnalyticsServiceTep : ServiceStack.ServiceInterface.Service {

        private static readonly log4net.ILog log = log4net.LogManager.GetLogger
            (System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public object Get(AnalyticsCurrentUserRequestTep requet) {
            var context = TepWebContext.GetWebContext(PagePrivileges.UserView);
            WebAnalytics result = new WebAnalytics();
            try {
                context.Open();
                context.LogInfo(this,string.Format("/analytics/user/current GET"));

                Analytics analytics = new Analytics(context, UserTep.FromId(context, context.UserId));
                analytics.Load();

                result = new WebAnalytics(analytics);

                context.Close();
            } catch (Exception e) {
                context.LogError(this, e.Message);
                context.Close();
                throw e;
            }
            return result;
        }

    }
}

