﻿﻿﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Web;
using System.Xml;
using ServiceStack.Common.Web;
using ServiceStack.ServiceHost;
using Terradue.OpenSearch;
using Terradue.OpenSearch.Engine;
using Terradue.OpenSearch.Result;
using Terradue.OpenSearch.Schema;
using Terradue.Portal;
using Terradue.Portal.OpenSearch;
using Terradue.ServiceModel.Ogc.Owc.AtomEncoding;
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
        public object Get2 (ThematicAppSearchRequestTep request)
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

            var settings = MasterCatalogue.OpenSearchFactorySettings;
            var specsettings = (OpenSearchableFactorySettings)settings.Clone();
			if (context.UserId != 0)
			{
				var user = UserTep.FromId(context, context.UserId);
                specsettings.Credentials = new System.Net.NetworkCredential(user.TerradueCloudUsername, user.GetSessionApiKey());
			}

            //foreach community we get the apps link
            foreach (var community in communities.Items) {
                if (community.IsUserJoined(context.UserId)) {
                    var app = community.GetThematicApplication();
                    if (app != null){
                        app.LoadItems();
                        foreach (var item in app.Items) {
                            if (!string.IsNullOrEmpty(item.Location)) {
                                try {
                                    var ios = OpenSearchFactory.FindOpenSearchable(specsettings, new OpenSearchUrl(item.Location));
                                    osentities.Add(ios);
                                    context.LogDebug(this, string.Format("Apps search -- Add '{0}'", item.Location));
                                } catch (Exception e) {
                                    context.LogError(this, e.Message);
                                }
                            }
                        }
                    }
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
                    if (!string.IsNullOrEmpty(item.Location)) {
						try {
                            var ios = OpenSearchFactory.FindOpenSearchable(specsettings, new OpenSearchUrl(item.Location));
							osentities.Add(ios);
                            context.LogDebug(this, string.Format("Apps search -- Add '{0}'", item.Location));
						} catch (Exception e) {
							context.LogError(this, e.Message);
						}
                    }
                }
            }

            MultiGenericOpenSearchable multiOSE = new MultiGenericOpenSearchable(osentities, specsettings);
            var result = ose.Query(multiOSE, Request.QueryString, responseType);

            string sresult = result.SerializeToString();

            //replace usernames in apps
            try{
                var user = UserTep.FromId(context, context.UserId);
                sresult = sresult.Replace("${USERNAME}", user.Username);
                sresult = sresult.Replace("${T2USERNAME}", user.TerradueCloudUsername);
                sresult = sresult.Replace("${T2APIKEY}", user.GetSessionApiKey());
            }catch(Exception e){
                context.LogError (this, e.Message);
            }

            context.Close ();

            return new HttpResult (sresult, result.ContentType);
        }

        public object Get(ThematicAppSearchRequestTep request){
            IfyWebContext context = TepWebContext.GetWebContext(PagePrivileges.EverybodyView);
            context.Open();
            context.LogInfo(this, string.Format("/apps/search GET"));

            var communities = new EntityList<ThematicCommunity>(context);
            if (context.UserId == 0) communities.SetFilter("Kind", (int)DomainKind.Public + "");
            else {
                communities.SetFilter("Kind", (int)DomainKind.Public + "," + (int)DomainKind.Private);
                communities.AddSort("Kind", SortDirection.Ascending);
            }
            communities.Load();
            List<int> ids = new List<int>();
            foreach (var c in communities) ids.Add(c.Id);

            EntityList<ThematicApplicationCached> apps = new EntityList<ThematicApplicationCached>(context);
            //var filterValues = new List<object>{string.Join(",",ids),SpecialSearchValue.Null};
            var filterValues = new List<object>();
            filterValues.Add(string.Join(",",ids));
            filterValues.Add(SpecialSearchValue.Null);
            apps.SetFilter("DomainId",filterValues.ToArray());
            apps.SetGroupFilter("UId");

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

            context.Close();
            return new HttpResult(osr.SerializeToString(), osr.ContentType);
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

        public object Post(ThematicAppAddToCommunityRequestTep request) {
            IfyWebContext context = TepWebContext.GetWebContext(PagePrivileges.UserView);
            context.Open();
            context.LogInfo(this, string.Format("/community/{{domain}}/apps POST domain='{0}', appurl='{1}'", request.Domain, request.AppUrl));
            if (string.IsNullOrEmpty(request.AppUrl)) throw new Exception("Invalid Application Url");

            var domain = ThematicCommunity.FromIdentifier(context, request.Domain);
            if (!domain.CanUserManage(context.UserId)) throw new UnauthorizedAccessException("Action only allowed to manager of the domain");

            var app = domain.GetThematicApplication();
            var res = new RemoteResource(context);
            res.Location = request.AppUrl;
            app.AddResourceItem(res);

            context.Close();
            return new WebResponseBool(true);
        }

        public object Get(ThematicAppCurrentUserSearchRequestTep request) {
            IfyWebContext context = TepWebContext.GetWebContext(PagePrivileges.EverybodyView);
            context.Open();
            context.LogInfo(this, string.Format("/user/current/apps/search GET"));

            OpenSearchEngine ose = MasterCatalogue.OpenSearchEngine;
            Type responseType = OpenSearchFactory.ResolveTypeFromRequest(HttpContext.Current.Request, ose);
            List<Terradue.OpenSearch.IOpenSearchable> osentities = new List<Terradue.OpenSearch.IOpenSearchable>();
            var settings = MasterCatalogue.OpenSearchFactorySettings;
            OpenSearchableFactorySettings specsettings = (OpenSearchableFactorySettings)settings.Clone();

            //get user private thematic app
            if (context.UserId != 0) {
                var user = UserTep.FromId(context, context.UserId);

                specsettings.Credentials = new System.Net.NetworkCredential(user.TerradueCloudUsername, user.GetSessionApiKey());
                var app = user.GetPrivateThematicApp();
                if (app != null) {
                    foreach (var item in app.Items) {
                        if (!string.IsNullOrEmpty(item.Location)) {
                            try {
                                var sgOs = OpenSearchFactory.FindOpenSearchable(specsettings, new OpenSearchUrl(item.Location));
                                osentities.Add(sgOs);
                                context.LogDebug(this, string.Format("Apps search -- Add '{0}'", item.Location));
                            } catch (Exception e) {
                                context.LogError(this, e.Message);
                            }
                        }
                    }
                }
            }
            MultiGenericOpenSearchable multiOSE = new MultiGenericOpenSearchable(osentities, specsettings);
            var result = ose.Query(multiOSE, Request.QueryString, responseType);

            string sresult = result.SerializeToString();

            //replace usernames in apps
            try {
                var user = UserTep.FromId(context, context.UserId);
                sresult = sresult.Replace("${USERNAME}", user.Username);
                sresult = sresult.Replace("${T2USERNAME}", user.TerradueCloudUsername);
                sresult = sresult.Replace("${T2APIKEY}", user.GetSessionApiKey());
            } catch (Exception e) {
                context.LogError(this, e.Message);
            }

            context.Close();

            return new HttpResult(sresult, result.ContentType);
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

            var settings = MasterCatalogue.OpenSearchFactorySettings;
            MultiGenericOpenSearchable multiOSE = new MultiGenericOpenSearchable (osentities, settings);
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

        //public object Post(ThematicAppCreateRequestTep request) {
        //    var context = TepWebContext.GetWebContext(PagePrivileges.UserView);
        //    try {
        //        context.Open();
        //        context.LogInfo(this, string.Format("/apps POST"));

        //        var minLevel = context.GetConfigIntegerValue("appExternalPostUserLevel");
        //        if (context.UserLevel < minLevel) throw new UnauthorizedAccessException("User is not allowed to create a thematic app");

        //        //create atom feed
        //        var feed = new AtomFeed();
        //        var entries = new List<AtomItem>();
        //        var atomEntry = new AtomItem();
        //        var entityType = EntityType.GetEntityType(typeof(ThematicApplication));
        //        Uri id = new Uri(context.BaseUrl + "/" + entityType.Keyword + "/search?id=" + request.Identifier);
        //        atomEntry = new AtomItem(request.Identifier, request.Title, null, id.ToString(), DateTime.UtcNow);
        //        atomEntry.ElementExtensions.Add("identifier", "http://purl.org/dc/elements/1.1/", request.Identifier);
        //        atomEntry.Links.Add(new SyndicationLink(id, "self", request.Title, "application/atom+xml", 0));
        //        if (!string.IsNullOrEmpty(request.Url)) atomEntry.Links.Add(new SyndicationLink(new Uri(request.Url), "alternate", "Thematic app url", "application/html", 0));
        //        if(!string.IsNullOrEmpty(request.Icon)) atomEntry.Links.Add(new SyndicationLink(new Uri(request.Icon), "icon", "Icon url", "image/png", 0));
        //        entries.Add(atomEntry);
        //        feed.Items = entries;

        //        //post to catalogue
        //        CatalogueFactory.PostAtomFeedToIndex(context, feed, request.Index);

        //        context.Close();
        //    } catch (Exception e) {
        //        context.LogError(this, e.Message);
        //        context.Close();
        //        throw e;
        //    }
        //    return new WebResponseBool(true);
        //}

		public object Get(ThematicAppEditorGetRequestTep request) {
            IfyWebContext context = TepWebContext.GetWebContext(PagePrivileges.UserView);
            WebThematicAppEditor result = null;
			context.Open();
            try{
	            context.LogInfo(this, string.Format("/app/editor GET url='{0}'", request.Url));
                if (string.IsNullOrEmpty(request.Url)) throw new Exception("Invalid url");
                if(!request.Url.StartsWith("http")){
                    var urib = new UriBuilder((context.BaseUrl));
                    var path = request.Url.Substring(0,request.Url.IndexOf("?"));
                    var query = request.Url.Substring(request.Url.IndexOf("?") + 1);
                    urib.Path = path;
                    urib.Query = query;
                    request.Url = urib.Uri.AbsoluteUri;
                }

	            HttpWebRequest httpRequest = (HttpWebRequest)HttpWebRequest.Create(request.Url);

	            using (var resp = httpRequest.GetResponse()){
	                using(var stream = resp.GetResponseStream()){
						var sr = XmlReader.Create(stream);
						Atom10FeedFormatter atomFormatter = new Atom10FeedFormatter();
						atomFormatter.ReadFrom(sr);
						sr.Close();
						var feed = new OwsContextAtomFeed(atomFormatter.Feed, true);
						foreach (OwsContextAtomEntry item in feed.Items) {
							if (result == null) result = new WebThematicAppEditor(item);
						}
	                }
	            }
				
				context.Close();
			} catch (Exception e) {
				context.LogError(this, e.Message);
				context.Close();
				throw e;
			}
            return result;
		}

        public object Post(ThematicAppEditorPostRequestTep request) {
            IfyWebContext context = TepWebContext.GetWebContext(PagePrivileges.UserView);
			WebThematicAppEditor result = null;
			context.Open();
            try{
	            context.LogInfo(this, string.Format("/app/editor POST"));

	            var owsentry = request.ToOwsContextAtomEntry(context);

				var feed = new OwsContextAtomFeed();
				var entries = new List<OwsContextAtomEntry>();
				entries.Add(owsentry);
				feed.Items = entries;

	            var user = UserTep.FromId(context, context.UserId);
                var index = string.IsNullOrEmpty(request.Index) ? user.TerradueCloudUsername : request.Index;
                var apikey = string.IsNullOrEmpty(request.ApiKey) ? user.GetSessionApiKey() : request.ApiKey;

                context.LogDebug(this, string.Format("/app/editor POST Identifier='{0}', Index='{1}'", request.Identifier, index));
                if(string.IsNullOrEmpty(index)) throw new Exception("Unable to POST to empty index");
				//post to catalogue
				try {
	                CatalogueFactory.PostAtomFeedToIndex(context, feed, index, user.TerradueCloudUsername, apikey);
				} catch (Exception e) {
					throw new Exception("Unable to POST to " + index + " - " + e.Message);
				}

			    context.Close();
			} catch (Exception e) {
				context.LogError(this, e.Message);
				context.Close();
				throw e;
			}
			return new WebResponseBool(true);
		}

		public object Get(ThematicAppCacheRequestTep request) {
            var context = TepWebContext.GetWebContext(PagePrivileges.AdminOnly);
            context.Open();
			try {
                context.LogInfo(this, string.Format("/apps/cache GET"));
                Actions.RefreshThematicAppsCache(context);
                context.Close();
			} catch (Exception e) {
                context.LogError(this, e.Message);
                context.Close();
                throw e;
            }
            return null;
        }

    }
}

