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
using Terradue.ServiceModel.Syndication;
using Terradue.Tep.WebServer;
using Terradue.WebService.Model;

namespace Terradue.Tep.WebServer.Services {

     [Api("Tep Terradue webserver")]
    [Restrict(EndpointAttributes.InSecure | EndpointAttributes.InternalNetworkAccess | EndpointAttributes.Json,
              EndpointAttributes.Secure | EndpointAttributes.External | EndpointAttributes.Json)]
    public class ThematicAppServiceTep : ServiceStack.ServiceInterface.Service {

        private static readonly log4net.ILog log = log4net.LogManager.GetLogger
            (System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Get the specified request.
        /// </summary>
        /// <param name="request">Request.</param>
        /// /apps/{identifier}/search GET
        public object Get (ThematicAppSearchRequestTep request)
        {
            IfyWebContext context = TepWebContext.GetWebContext (PagePrivileges.EverybodyView);
            context.Open ();
            context.LogInfo (this, string.Format ("/apps/search GET"));


            var apps = new EntityList<ThematicApplication> (context);
            apps.Template.Kind = ThematicApplication.KINDRESOURCESETAPPS;
            apps.Load ();
            var result = GetAppsResultCollection (apps);

            context.Close ();
            return new HttpResult (result.SerializeToString (), result.ContentType);
        }

        public object Get (ThematicAppByCommunitySearchRequestTep request)
        {
            IfyWebContext context = TepWebContext.GetWebContext (PagePrivileges.EverybodyView);
            context.Open ();
            context.LogInfo (this, string.Format ("/community/{{domain}}/apps/search GET domain='{0}'",request.Domain));

            var domain = Domain.FromIdentifier (context, request.Domain);
            var apps = new EntityList<ThematicApplication> (context);
            apps.SetFilter ("Kind", ThematicApplication.KINDRESOURCESETAPPS.ToString ());
            apps.SetFilter ("DomainId", domain.Id.ToString());
            apps.Load ();
            var result = GetAppsResultCollection (apps);

            context.Close ();
            return new HttpResult (result.SerializeToString (), result.ContentType);
        }

        private IOpenSearchResultCollection GetAppsResultCollection (EntityList<ThematicApplication> apps) { 
            OpenSearchEngine ose = MasterCatalogue.OpenSearchEngine;
            apps.OpenSearchEngine = ose;

            Type responseType = OpenSearchFactory.ResolveTypeFromRequest (HttpContext.Current.Request, ose);
            List<Terradue.OpenSearch.IOpenSearchable> osentities = new List<Terradue.OpenSearch.IOpenSearchable> ();
            foreach (var app in apps.Items) {
                app.OpenSearchEngine = ose;
                osentities.Add (app);
            }
            MultiGenericOpenSearchable multiOSE = new MultiGenericOpenSearchable (osentities, ose);
            var result = ose.Query (multiOSE, Request.QueryString, responseType);

            //var openSearchDescription = apps.GetLocalOpenSearchDescription ();
            //var uri_s = apps.GetSearchBaseUrl ();
            //OpenSearchDescriptionUrl openSearchUrlByRel = OpenSearchFactory.GetOpenSearchUrlByRel (openSearchDescription, "self");
            //Uri uri_d;
            //if (openSearchUrlByRel != null) {
            //    uri_d = new Uri (openSearchUrlByRel.Template);
            //} else {
            //    uri_d = openSearchDescription.Originator;
            //}
            //if (uri_d != null) {
            //    result.Links.Add (new SyndicationLink (uri_d, "search", "OpenSearch Description link", "application/opensearchdescription+xml", 0));
            //}
            //if (uri_s != null) {
            //    result.Links.Add (new SyndicationLink (uri_s, "self", "OpenSearch Search link", "application/atom+xml", 0));
            //}
            return result;
        }

        /// <summary>
        /// Thematic app service tep.
        /// </summary>
        /// /apps/{identifier}/description GET
        public object Get (ThematicAppDescriptionRequestTep request)
        {
            OpenSearchDescription osd;
            var context = TepWebContext.GetWebContext (PagePrivileges.EverybodyView);
            try {
                context.Open ();
                context.LogInfo (this, string.Format ("/apps/description GET"));

                ThematicApplication apps;
                apps = ThematicApplication.FromIdentifier (context, "_apps");

                apps.SetOpenSearchEngine (MasterCatalogue.OpenSearchEngine);
                osd = apps.GetLocalOpenSearchDescription ();

                context.Close ();
            } catch (Exception e) {
                context.LogError (this, e.Message);
                context.Close ();
                throw e;
            }
            HttpResult hr = new HttpResult (osd, "application/opensearchdescription+xml");
            return hr;
        }

    }
}

