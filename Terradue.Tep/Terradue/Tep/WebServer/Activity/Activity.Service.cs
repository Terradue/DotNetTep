using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Web;
using ServiceStack.Common.Web;
using ServiceStack.ServiceHost;
using Terradue.OpenSearch;
using Terradue.OpenSearch.Engine;
using Terradue.OpenSearch.Result;
using Terradue.OpenSearch.Schema;
using Terradue.Portal;
using Terradue.Tep.WebServer;
using Terradue.WebService.Model;

namespace Terradue.Tep.WebServer.Services {
    
    [Route("/activity/search", "GET", Summary = "GET activity as opensearch", Notes = "")]
    public class ActivitySearchRequestTep : IReturn<HttpResult>{}

    [Route("/activity/description", "GET", Summary = "GET activity description", Notes = "")]
    public class ActivityDescriptionRequestTep : IReturn<HttpResult>{}

    [Api("Tep Terradue webserver")]
    [Restrict(EndpointAttributes.InSecure | EndpointAttributes.InternalNetworkAccess | EndpointAttributes.Json,
              EndpointAttributes.Secure | EndpointAttributes.External | EndpointAttributes.Json)]
    public class ActivityServiceTep : ServiceStack.ServiceInterface.Service {

        public object Get(ActivitySearchRequestTep request) {
            IfyWebContext context = TepWebContext.GetWebContext(PagePrivileges.UserView);
            context.RestrictedMode = false;
            object result;
            context.Open();

            List<Terradue.OpenSearch.IOpenSearchable> osentities = new List<Terradue.OpenSearch.IOpenSearchable>();

            EntityList<Activity> activities = new EntityList<Activity>(context);
            activities.Load();
            osentities.Add(activities);

            // Load the complete request
            HttpRequest httpRequest = HttpContext.Current.Request;
            OpenSearchEngine ose = MasterCatalogue.OpenSearchEngine;

            MultiGenericOpenSearchable multiOSE = new MultiGenericOpenSearchable(osentities, ose);

            Type responseType = OpenSearchFactory.ResolveTypeFromRequest(httpRequest, ose);
            IOpenSearchResultCollection osr = ose.Query(multiOSE, httpRequest.QueryString, responseType);

            multiOSE.Identifier = "activity";

            OpenSearchFactory.ReplaceOpenSearchDescriptionLinks(multiOSE, osr);

            context.Close();
            return new HttpResult(osr.SerializeToString(), osr.ContentType);
        }
            
        public object Get(ActivityDescriptionRequestTep request) {
            IfyWebContext context = TepWebContext.GetWebContext(PagePrivileges.EverybodyView);
            try {
                context.Open();
                EntityList<WpsJob> wpsjobs = new EntityList<WpsJob>(context);
                wpsjobs.OpenSearchEngine = MasterCatalogue.OpenSearchEngine;

                OpenSearchDescription osd = wpsjobs.GetOpenSearchDescription();

                context.Close();

                return new HttpResult(osd, "application/opensearchdescription+xml");
            } catch (Exception e) {
                context.Close();
                throw e;
            }
        }

    }
}

