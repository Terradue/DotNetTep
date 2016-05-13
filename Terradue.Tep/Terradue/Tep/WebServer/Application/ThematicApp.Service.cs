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
using Terradue.WebService.Model;

namespace Terradue.Tep.WebServer.Services {

     [Api("Tep Terradue webserver")]
    [Restrict(EndpointAttributes.InSecure | EndpointAttributes.InternalNetworkAccess | EndpointAttributes.Json,
              EndpointAttributes.Secure | EndpointAttributes.External | EndpointAttributes.Json)]
    public class ThematicAppServiceTep : ServiceStack.ServiceInterface.Service {

        private static readonly log4net.ILog log = log4net.LogManager.GetLogger
            (System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public object Get(ThematicAppGetRequestTep request) {
            IfyWebContext context = TepWebContext.GetWebContext(PagePrivileges.DeveloperView);
            List<WebWpsJobTep> result = new List<WebWpsJobTep>();
            try {
                context.Open();

                EntityList<WpsJob> services = new EntityList<WpsJob>(context);
                services.OwnedItemsOnly = true;
                services.Load();

                foreach (WpsJob job in services) {
                    result.Add(new WebWpsJobTep(job));
                }

                context.Close();
            } catch (Exception e) {
                context.Close();
                throw e;
            }

            return result;
        }

        public object Get(ThematicAppSearchRequestTep request) {
            IfyWebContext context = TepWebContext.GetWebContext(PagePrivileges.EverybodyView);
            object result;
            context.Open();

            EntityList<WpsJob> tmp = new EntityList<WpsJob>(context);
            tmp.Load();

            List<WpsJob> jobs = tmp.GetItemsAsList();
            jobs.Sort();
            jobs.Reverse();

            EntityList<WpsJob> wpsjobs = new EntityList<WpsJob>(context);
            foreach (WpsJob job in jobs) wpsjobs.Include(job);

            // Load the complete request
            HttpRequest httpRequest = HttpContext.Current.Request;

            OpenSearchEngine ose = MasterCatalogue.OpenSearchEngine;

            string format;
            if (Request.QueryString["format"] == null)
                format = "atom";
            else
                format = Request.QueryString["format"];

            Type responseType = OpenSearchFactory.ResolveTypeFromRequest(httpRequest, ose);
            IOpenSearchResultCollection osr = ose.Query(wpsjobs, httpRequest.QueryString, responseType);

            OpenSearchFactory.ReplaceOpenSearchDescriptionLinks(wpsjobs, osr);
//            OpenSearchFactory.ReplaceSelfLinks(wpsjobs, httpRequest.QueryString, osr.Result, EntrySelfLinkTemplate);

            context.Close();
            return new HttpResult(osr.SerializeToString(), osr.ContentType);
        }

    }
}

