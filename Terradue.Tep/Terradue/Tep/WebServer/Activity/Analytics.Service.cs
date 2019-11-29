using System;
using ServiceStack.ServiceHost;
using Terradue.Portal;

namespace Terradue.Tep.WebServer.Services {

    [Api("Tep Terradue webserver")]
    [Restrict(EndpointAttributes.InSecure | EndpointAttributes.InternalNetworkAccess | EndpointAttributes.Json,
              EndpointAttributes.Secure | EndpointAttributes.External | EndpointAttributes.Json)]
    public class AnalyticsServiceTep : ServiceStack.ServiceInterface.Service {

        public object Get(AnalyticsCurrentUserRequestTep request) {
            var context = TepWebContext.GetWebContext(PagePrivileges.UserView);
            WebAnalytics result = new WebAnalytics();
            try {
                context.Open();
                context.LogInfo(this, string.Format("/analytics/user/current GET"));

                Analytics analytics = new Analytics(context, UserTep.FromId(context, context.UserId));
                analytics.Load();

                result = new WebAnalytics(analytics);

                context.Close();
            } catch (Exception e) {
                context.LogError(this, e.Message, e);
                context.Close();
                throw e;
            }
            return result;
        }

        public object Get(AnalyticsRequestTep request) {
            var context = TepWebContext.GetWebContext(PagePrivileges.EverybodyView);
            WebAnalytics result = new WebAnalytics();
            try {
                context.Open();
                context.LogInfo(this, string.Format("/analytics GET - type='{0}', identifier='{1}'", request.Type, request.Identifier));

                Analytics analytics = null;

                if (context.UserId == 0) request.Type = "all";

                switch (request.Type) {
                    case "user":
                        analytics = new Analytics(context, UserTep.FromIdentifier(context, request.Identifier));
                        analytics.Load(request.startdate, request.enddate);
                        break;
                    case "community":
                        analytics = new Analytics(context, ThematicCommunity.FromIdentifier(context, request.Identifier));
                        analytics.Load(request.startdate, request.enddate);
                        break;
                    case "group":
                        analytics = new Analytics(context, Group.FromIdentifier(context, request.Identifier));
                        analytics.Load(request.startdate, request.enddate);
                        break;
                    case "service":
                        analytics = new Analytics(context, Service.FromIdentifier(context, request.Identifier));
                        analytics.Load(request.startdate, request.enddate);
                        break;
                    case "all":
                        analytics = new Analytics(context);
                        analytics.Load(request.startdate, request.enddate);
                        break;
                    default:
                        break;
                }
                if (analytics != null) result = new WebAnalytics(analytics);

                context.Close();
            } catch (Exception e) {
                context.LogError(this, e.Message, e);
                context.Close();
                throw e;
            }
            return result;
        }

    }
}

