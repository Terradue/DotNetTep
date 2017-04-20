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
using Terradue.Portal.OpenSearch;
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

            OpenSearchEngine ose = MasterCatalogue.OpenSearchEngine;
            Type responseType = OpenSearchFactory.ResolveTypeFromRequest(HttpContext.Current.Request, ose);
            List<Terradue.OpenSearch.IOpenSearchable> osentities = new List<Terradue.OpenSearch.IOpenSearchable>();

            //first we get the communities the user can see
            var communities = new EntityList<ThematicCommunity>(context);
            if (context.UserId == 0) communities.SetFilter("Kind", (int)DomainKind.Public + "");
            else {
                communities.SetFilter("Kind", (int)DomainKind.Public + "," + (int)DomainKind.Private);
                communities.AddSort("Kind", SortDirection.Ascending);
            }
            communities.Load();

            //foreach community we get the apps link
            foreach (var community in communities.Items) {
                if (community.Kind == DomainKind.Public || community.IsUserJoined(context.UserId) || community.IsUserPending(context.UserId)) {
                    if (!string.IsNullOrEmpty(community.AppsLink)) 
                        osentities.Add(new SmartGenericOpenSearchable(new OpenSearchUrl(community.AppsLink), ose));
                }
            }

            //get thematic apps without any domain
            var apps = new EntityList<ThematicApplication>(context);
            apps.SetFilter("DomainId", SpecialSearchValue.Null);
            apps.SetFilter("Kind", ThematicApplication.KINDRESOURCESETAPPS + "");
            apps.Load();
            foreach (var app in apps) {
                app.LoadItems();
                foreach (var item in app.Items) {
                    osentities.Add(new SmartGenericOpenSearchable(new OpenSearchUrl(item.Location), ose));
                }
            }

            MultiGenericOpenSearchable multiOSE = new MultiGenericOpenSearchable(osentities, ose);
            var result = ose.Query(multiOSE, Request.QueryString, responseType);

            context.Close ();
            return new HttpResult (result.SerializeToString (), result.ContentType);
        }

        public object Get (ThematicAppByCommunitySearchRequestTep request)
        {
            IfyWebContext context = TepWebContext.GetWebContext (PagePrivileges.EverybodyView);
            context.Open ();
            context.LogInfo (this, string.Format ("/community/{{domain}}/apps/search GET domain='{0}'",request.Domain));

            var domain = Domain.FromIdentifier (context, request.Domain);
            var apps = new EntityList<DataPackage> (context);
            apps.SetFilter ("Kind", ThematicApplication.KINDRESOURCESETAPPS.ToString ());
            apps.SetFilter ("DomainId", domain.Id.ToString());
            apps.Load ();

            // the opensearch cache system uses the query parameters
            // we add to the parameters the filters added to the load in order to avoir wrong cache
            // we use 't2-' in order to not interfer with possibly used query parameters
            var qs = new NameValueCollection(Request.QueryString);
            foreach (var filter in apps.FilterValues) qs.Add("t2-" + filter.Key.FieldName, filter.Value.ToString());

            var result = GetAppsResultCollection (apps, qs);

            context.Close ();
            return new HttpResult (result.SerializeToString (), result.ContentType);
        }

        private IOpenSearchResultCollection GetAppsResultCollection (EntityList<DataPackage> apps, NameValueCollection nvc) { 
            OpenSearchEngine ose = MasterCatalogue.OpenSearchEngine;
            apps.OpenSearchEngine = ose;

            Type responseType = OpenSearchFactory.ResolveTypeFromRequest (HttpContext.Current.Request, ose);
            List<Terradue.OpenSearch.IOpenSearchable> osentities = new List<Terradue.OpenSearch.IOpenSearchable> ();
            foreach (var app in apps.Items) {
                app.OpenSearchEngine = ose;
                //osentities.Add (app);
                osentities.AddRange(app.GetOpenSearchableArray());
            }

            MultiGenericOpenSearchable multiOSE = new MultiGenericOpenSearchable (osentities, ose);
            var result = ose.Query (multiOSE, nvc, responseType);

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

        public object Post(ThematicAppCreateRequestTep request) {
            var context = TepWebContext.GetWebContext(PagePrivileges.UserView);
            try {
                context.Open();
                context.LogInfo(this, string.Format("/apps POST"));

                var minLevel = context.GetConfigIntegerValue("appExternalPostUserLevel");
                if (context.UserLevel < minLevel) throw new UnauthorizedAccessException("User is not allowed to create a thematic app");

                //create atom feed
                var feed = new AtomFeed();
                var entries = new List<AtomItem>();
                var atomEntry = new AtomItem();
                var entityType = EntityType.GetEntityType(typeof(ThematicApplication));
                Uri id = new Uri(context.BaseUrl + "/" + entityType.Keyword + "/search?id=" + request.Identifier);
                atomEntry = new AtomItem(request.Identifier, request.Title, null, id.ToString(), DateTime.UtcNow);
                atomEntry.ElementExtensions.Add("identifier", "http://purl.org/dc/elements/1.1/", request.Identifier);
                atomEntry.Links.Add(new SyndicationLink(id, "self", request.Title, "application/atom+xml", 0));
                if (!string.IsNullOrEmpty(request.Url)) atomEntry.Links.Add(new SyndicationLink(new Uri(request.Url), "alternate", "Thematic app url", "application/html", 0));
                if(!string.IsNullOrEmpty(request.Icon)) atomEntry.Links.Add(new SyndicationLink(new Uri(request.Icon), "icon", "Icon url", "image/png", 0));
                entries.Add(atomEntry);
                feed.Items = entries;

                //post to catalogue
                CatalogueFactory.PostAtomFeedToIndex(context, feed, request.Index);

                context.Close();
            } catch (Exception e) {
                context.LogError(this, e.Message);
                context.Close();
                throw e;
            }
            return new WebResponseBool(true);
        }

    }
}

