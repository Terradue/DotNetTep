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

    [Route ("/community/{domain}/activity/search", "GET", Summary = "search activities per community", Notes = "")]
    public class ActivityByCommunitySearchRequestTep : IReturn<List<HttpResult>>
    {
        [ApiMember (Name = "domain", Description = "identifier of the domain", ParameterType = "query", DataType = "string", IsRequired = true)]
        public string Domain { get; set; }
    }


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

            EntityList<ActivityTep> activities = new EntityList<ActivityTep>(context);

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

        public object Get (ActivityByCommunitySearchRequestTep request)
        {
            var context = TepWebContext.GetWebContext (PagePrivileges.UserView);
            context.AccessLevel = EntityAccessLevel.Administrator;
            context.Open ();
            context.LogInfo (this, string.Format ("/community/{{community}}/activity/search GET - community='{0}'", request.Domain));

            ThematicCommunity domain = ThematicCommunity.FromIdentifier (context, request.Domain);

            //We only want some Privileges
            var privlist = new List<int> ();

            var privs = Privilege.Get (EntityType.GetEntityType (typeof (WpsJob)));
            foreach (var priv in privs) privlist.Add (priv.Id);

            privs = Privilege.Get (EntityType.GetEntityType (typeof (DataPackage)));
            foreach (var priv in privs) privlist.Add (priv.Id);

            privs = Privilege.Get (EntityType.GetEntityType (typeof (Series)));
            foreach (var priv in privs) privlist.Add (priv.Id);

            EntityList<ActivityTep> activities = new EntityList<ActivityTep> (context);
            activities.AddSort ("CreationTime", SortDirection.Descending);
            activities.SetFilter ("PrivilegeId", string.Join (",", privlist));
            activities.SetFilter ("DomainId",domain.Id.ToString());
            activities.Identifier = "activity";
            activities.Load ();

            // Load the complete request
            HttpRequest httpRequest = HttpContext.Current.Request;
            OpenSearchEngine ose = MasterCatalogue.OpenSearchEngine;

            Type responseType = OpenSearchFactory.ResolveTypeFromRequest (httpRequest, ose);
            IOpenSearchResultCollection osr = ose.Query (activities, httpRequest.QueryString, responseType);

            OpenSearchFactory.ReplaceOpenSearchDescriptionLinks (activities, osr);

            context.Close ();
            return new HttpResult (osr.SerializeToString (), osr.ContentType);
        }

        private IOpenSearchResultCollection GetActivityResultCollection (EntityList<Activity> activities) { 
            
            // Load the complete request
            HttpRequest httpRequest = HttpContext.Current.Request;
            OpenSearchEngine ose = MasterCatalogue.OpenSearchEngine;

            Type responseType = OpenSearchFactory.ResolveTypeFromRequest (httpRequest, ose);
            IOpenSearchResultCollection osr = ose.Query (activities, httpRequest.QueryString, responseType);

            OpenSearchFactory.ReplaceOpenSearchDescriptionLinks (activities, osr);

            return osr;
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

