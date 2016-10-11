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
        /// /apps GET
        public object Get(ThematicAppGetRequestTep request) {
            IfyWebContext context = TepWebContext.GetWebContext(PagePrivileges.DeveloperView);
            List<WebThematicAppTep> result = new List<WebThematicAppTep>();
            try {
                context.Open();
                context.LogInfo (this, string.Format ("/apps GET"));

                var services = new EntityList<ThematicApplicationSet>(context);
                services.OwnedItemsOnly = true;
                context.ConsoleDebug = true;
                services.Load();

                foreach (ThematicApplicationSet job in services) {
                    result.Add(new WebThematicAppTep(job));
                }

                context.Close();
            } catch (Exception e) {
                context.Close();
                throw e;
            }

            return result;
        }

        /// <summary>
        /// Get the specified request.
        /// </summary>
        /// <param name="request">Request.</param>
        /// /apps/search GET
        public object Get(ThematicAppsSearchRequestTep request) {
            IfyWebContext context = TepWebContext.GetWebContext(PagePrivileges.EverybodyView);
            object result;
            context.Open();
            context.LogInfo (this, string.Format ("/apps/search GET"));

            EntityList<ThematicApplicationSet> tmp = new EntityList<ThematicApplicationSet>(context);
            tmp.Load();

            List<ThematicApplicationSet> appset = tmp.GetItemsAsList();
            appset.Sort();
            appset.Reverse();

            EntityList<ThematicApplicationSet> apps = new EntityList<ThematicApplicationSet>(context);
            foreach (ThematicApplicationSet app in appset) apps.Include(app);

            // Load the complete request
            HttpRequest httpRequest = HttpContext.Current.Request;

            OpenSearchEngine ose = MasterCatalogue.OpenSearchEngine;

            string format;
            if (Request.QueryString["format"] == null)
                format = "atom";
            else
                format = Request.QueryString["format"];

            Type responseType = OpenSearchFactory.ResolveTypeFromRequest(httpRequest, ose);
            IOpenSearchResultCollection osr = ose.Query(apps, httpRequest.QueryString, responseType);

            OpenSearchFactory.ReplaceOpenSearchDescriptionLinks(apps, osr);
//            OpenSearchFactory.ReplaceSelfLinks(wpsjobs, httpRequest.QueryString, osr.Result, EntrySelfLinkTemplate);

            context.Close();
            return new HttpResult(osr.SerializeToString(), osr.ContentType);
        }

        /// <summary>
        /// Get the specified request.
        /// </summary>
        /// <param name="request">Request.</param>
        /// /apps/description GET
        public object Get (ThematicAppsDescriptionRequestTep request)
        {
           OpenSearchDescription OSDD;
            var context = TepWebContext.GetWebContext (PagePrivileges.DeveloperView);
            try {
                context.Open ();
                context.LogInfo (this, string.Format ("/apps/description GET"));

                EntityList<ThematicApplicationSet> apps = new EntityList<ThematicApplicationSet> (context);
                OSDD = apps.GetOpenSearchDescription ();

                context.Close ();
            } catch (Exception e) {
                context.LogError (this, e.Message);
                context.Close ();
                throw e;
            }
            HttpResult hr = new HttpResult (OSDD, "application/opensearchdescription+xml");
            return hr;
        }

        /// <summary>
        /// Get the specified request.
        /// </summary>
        /// <param name="request">Request.</param>
        /// /apps/{identifier}/search GET
        public object Get (ThematicAppSearchRequestTep request)
        {
            IfyWebContext context = TepWebContext.GetWebContext (PagePrivileges.EverybodyView);
            IOpenSearchResultCollection result = null;
            context.Open ();
            context.LogInfo (this, string.Format ("/apps/{0}/search GET", request.Identifier));

            ThematicApplicationSet apps;
            apps = ThematicApplicationSet.FromIdentifier (context, request.Identifier);

            apps.SetOpenSearchEngine (MasterCatalogue.OpenSearchEngine);
            OpenSearchEngine ose = MasterCatalogue.OpenSearchEngine;

            Type responseType = OpenSearchFactory.ResolveTypeFromRequest (HttpContext.Current.Request, ose);
            result = ose.Query (apps, Request.QueryString, responseType);
           
            var openSearchDescription = apps.GetLocalOpenSearchDescription ();
            var uri_s = apps.GetSearchBaseUrl ();
            OpenSearchDescriptionUrl openSearchUrlByRel = OpenSearchFactory.GetOpenSearchUrlByRel (openSearchDescription, "self");
            Uri uri_d;
            if (openSearchUrlByRel != null) {
                uri_d = new Uri (openSearchUrlByRel.Template);
            } else {
                uri_d = openSearchDescription.Originator;
            }
            if (uri_d != null) {
                result.Links.Add (new SyndicationLink (uri_d, "search", "OpenSearch Description link", "application/opensearchdescription+xml", 0));
            }
            if (uri_s != null) {
                result.Links.Add (new SyndicationLink (uri_s, "self", "OpenSearch Search link", "application/atom+xml", 0));
            }

            context.Close ();
            return new HttpResult (result.SerializeToString (), result.ContentType);
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
                context.LogInfo (this, string.Format ("/apps/{0}/description GET", request.Identifier));

                ThematicApplicationSet apps;
                apps = ThematicApplicationSet.FromIdentifier (context, request.Identifier);

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

