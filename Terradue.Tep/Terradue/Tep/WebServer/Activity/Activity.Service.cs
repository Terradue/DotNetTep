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

            List<Terradue.OpenSearch.IOpenSearchable> osentities = new List<Terradue.OpenSearch.IOpenSearchable>();

            EntityList<Activity> activities = new EntityList<Activity>(context);
            activities.Load();

            List<Activity> tmplist = new List<Activity>();
            tmplist = activities.GetItemsAsList();
            tmplist.Sort();
            tmplist.Reverse();

            activities = new EntityList<Activity>(context);
            activities.Identifier = "activity";
            foreach (Activity item in tmplist) {
                if(!request.nologin || 
                   (item.Privilege != null && item.Privilege.EntityType != null && item.Privilege.EntityType.Id != EntityType.GetEntityType(typeof(UserTep)).Id && !item.Privilege.Operation.Equals("l")))
//                    EntityType.GetEntityTypeFromKeyword("users").Id && !item.Privilege.Operation.Equals("l")))
                    activities.Include(item);
            }
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
            var context = TepWebContext.GetWebContext(PagePrivileges.EverybodyView);
            try {
                context.Open();
                context.LogInfo(this,string.Format("/activity/description GET"));

                EntityList<WpsJob> wpsjobs = new EntityList<WpsJob>(context);
                wpsjobs.OpenSearchEngine = MasterCatalogue.OpenSearchEngine;

                OpenSearchDescription osd = wpsjobs.GetOpenSearchDescription();

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

