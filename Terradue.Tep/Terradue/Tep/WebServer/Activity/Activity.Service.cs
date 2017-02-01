using System;
using System.Collections.Generic;
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
    
    [Route("/activity/search", "GET", Summary = "GET activity as opensearch", Notes = "")]
    public class ActivitySearchRequestTep : IReturn<HttpResult>{
        [ApiMember(Name="nologin", Description = "dont get login activities", ParameterType = "query", DataType = "bool", IsRequired = false)]
        public bool nologin { get; set; }
    }

    [Route("/activity/description", "GET", Summary = "GET activity description", Notes = "")]
    public class ActivityDescriptionRequestTep : IReturn<HttpResult>{}

    [Api("Tep Terradue webserver")]
    [Restrict(EndpointAttributes.InSecure | EndpointAttributes.InternalNetworkAccess | EndpointAttributes.Json,
              EndpointAttributes.Secure | EndpointAttributes.External | EndpointAttributes.Json)]
    public class ActivityServiceTep : ServiceStack.ServiceInterface.Service {

        private static readonly log4net.ILog log = log4net.LogManager.GetLogger
            (System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public object Get(ActivitySearchRequestTep request) {
            var context = TepWebContext.GetWebContext(PagePrivileges.UserView);
            context.AccessLevel = EntityAccessLevel.Administrator;
            context.Open();
            context.LogInfo(this,string.Format("/activity/search GET nologin='{0}'", request.nologin));

            var domain = Domain.FromId(context, 32);

            EntityList<Activity> activities = new EntityList<Activity>(context);

            // Load the complete request
            HttpRequest httpRequest = HttpContext.Current.Request;
            OpenSearchEngine ose = MasterCatalogue.OpenSearchEngine;

            Type responseType = OpenSearchFactory.ResolveTypeFromRequest(httpRequest, ose);
            IOpenSearchResultCollection osr = ose.Query (activities, httpRequest.QueryString, responseType);

            activities.Identifier = "activity";

            OpenSearchFactory.ReplaceOpenSearchDescriptionLinks(activities, osr);

            context.Close();
            return new HttpResult(osr.SerializeToString(), osr.ContentType);
        }
            
        public object Get(ActivityDescriptionRequestTep request) {
            var context = TepWebContext.GetWebContext(PagePrivileges.EverybodyView);
            try {
                context.Open();
                context.LogInfo(this,string.Format("/activity/description GET"));

                EntityList<Activity> activities = new EntityList<Activity>(context);
                activities.OpenSearchEngine = MasterCatalogue.OpenSearchEngine;

                OpenSearchDescription osd = activities.GetOpenSearchDescription();

                context.Close();

                return new HttpResult(osd, "application/opensearchdescription+xml");
            } catch (Exception e) {
                context.LogError(this, e.Message);
                context.Close();
                throw e;
            }
        }

    }
}

