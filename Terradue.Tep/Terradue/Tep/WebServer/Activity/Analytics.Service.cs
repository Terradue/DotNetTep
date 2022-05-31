using System;
using System.Collections.Generic;
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
                analytics.AnalyseCollections = false;
                analytics.AnalyseDataPackages = false;
                analytics.AnalyseJobs = true;
                analytics.AnalyseServices = false;                
                analytics.Load(request.startdate, request.enddate);

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

        public object Get(AnalyticsServicesCurrentUserRequestTep request) {
            var context = TepWebContext.GetWebContext(PagePrivileges.UserView);
            var result = new List<WebAnalyticsService>();
            try {
                context.Open();
                context.LogInfo(this, string.Format("/analytics/service/user/current GET"));

                ServiceAnalytics analytics = new ServiceAnalytics(context);                
                analytics.AddServices(request.startdate, request.enddate, context.UserId);
                foreach(var service in analytics.Services) result.Add(new WebAnalyticsService(service));

                context.Close();
            } catch (Exception e) {
                context.LogError(this, e.Message, e);
                context.Close();
                throw e;
            }
            return result;
        }

        public object Put(AnalyticsServicesCommunityRequestTep request) {
            var context = TepWebContext.GetWebContext(PagePrivileges.UserView);
            var result = new List<WebAnalyticsService>();
            try {
                context.Open();
                context.LogInfo(this, string.Format("/analytics/service/community/{0} PUT", request.Identifier));

                if(string.IsNullOrEmpty(request.Usernames)) return new List<WebAnalyticsService>();
                var usernames = request.Usernames.Split(',');
                var usernamesS = "'" + string.Join("','",usernames) + "'";
                
                string sql = string.Format("SELECT id FROM usr WHERE username IN ({0});", usernamesS);
                context.LogDebug(this, sql);
                var requestids = context.GetQueryIntegerValues(sql);
                context.LogDebug(this, "found " + requestids.Length);

                ServiceAnalytics analytics = new ServiceAnalytics(context);                
                var community = ThematicCommunity.FromIdentifier(context, request.Identifier);
                if(!community.CanUserManage(context.UserId)) return new List<WebAnalyticsService>();
                var userids = community.GetUsersIds();
                context.LogDebug(this, "found " + userids.Count);

                var ids = new List<int>();
                foreach(var id in requestids)                                        
                    if(userids.Contains(id))
                        if(!ids.Contains(id)) ids.Add(id);

                context.LogDebug(this, ids.Count + " in common");

                var apps = new List<string>();
                var cachedapps = community.GetThematicApplicationsCached();
                foreach(var app in cachedapps)
                    apps.Add(app.UId);

                context.LogDebug(this, "found " + apps.Count + " apps");                
                
                analytics.AddServices(request.startdate, request.enddate, ids, apps);
                foreach(var service in analytics.Services) result.Add(new WebAnalyticsService(service));

                context.Close();
            } catch (Exception e) {
                context.LogError(this, e.Message, e);
                context.Close();
                throw e;
            }
            return result;
        }

        public object Get(AnalyticsServicesUserRequestTep request) {
            var context = TepWebContext.GetWebContext(PagePrivileges.AdminOnly);
            var result = new List<WebAnalyticsService>();
            try {
                context.Open();
                context.LogInfo(this, string.Format("/analytics/service/user/{0} GET", request.Identifier));

                ServiceAnalytics analytics = new ServiceAnalytics(context);   
                var user = User.FromUsername(context, request.Identifier)             ;
                analytics.AddServices(request.startdate, request.enddate, user.UserId);
                foreach(var service in analytics.Services) result.Add(new WebAnalyticsService(service));

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

