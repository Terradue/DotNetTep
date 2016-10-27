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
            IOpenSearchResultCollection result = null;
            context.Open ();
            context.LogInfo (this, string.Format ("/apps/search GET"));

            ThematicApplication apps;
            apps = ThematicApplication.FromIdentifier (context, "_apps");

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

