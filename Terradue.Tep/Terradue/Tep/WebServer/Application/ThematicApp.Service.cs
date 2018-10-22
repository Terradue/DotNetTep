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
        public object Get (ThematicAppSearchRequestTep request)
        {
            IfyWebContext context = TepWebContext.GetWebContext (PagePrivileges.EverybodyView);
            context.Open ();
			context.LogInfo (this, string.Format ("/apps/search GET -- cache = {0}", request.cache));

			IOpenSearchResultCollection result;
			OpenSearchEngine ose = MasterCatalogue.OpenSearchEngine;
            HttpRequest httpRequest = HttpContext.Current.Request;         
            Type responseType = OpenSearchFactory.ResolveTypeFromRequest(httpRequest, ose);         

			//first we get the communities the user can see
			var communities = new EntityList<ThematicCommunity>(context);
            if (context.UserId == 0) communities.SetFilter("Kind", (int)DomainKind.Public + "");
            else {
                communities.SetFilter("Kind", (int)DomainKind.Public + "," + (int)DomainKind.Private + "," + (int)DomainKind.Restricted);
                communities.AddSort("Kind", SortDirection.Ascending);
            }
            communities.Load();

			if (request.cache) {

				List<int> ids = new List<int>();
				foreach (var c in communities) {
					if (c.IsUserJoined(context.UserId)) ids.Add(c.Id);
				}

				EntityList<ThematicApplicationCached> appsCached = new EntityList<ThematicApplicationCached>(context);
				var filterValues = new List<object>();
				filterValues.Add(string.Join(",", ids));
				filterValues.Add(SpecialSearchValue.Null);
				appsCached.SetFilter("DomainId", filterValues.ToArray());
				appsCached.SetGroupFilter("UId");
				appsCached.AddSort("LastUpdate", SortDirection.Descending);

				result = ose.Query(appsCached, httpRequest.QueryString, responseType);
				OpenSearchFactory.ReplaceOpenSearchDescriptionLinks(appsCached, result);            
                
			} else {
				
				List<Terradue.OpenSearch.IOpenSearchable> osentities = new List<Terradue.OpenSearch.IOpenSearchable>();

				var settings = MasterCatalogue.OpenSearchFactorySettings;
				var specsettings = (OpenSearchableFactorySettings)settings.Clone();
				if (context.UserId != 0) {
					var user = UserTep.FromId(context, context.UserId);
					specsettings.Credentials = new System.Net.NetworkCredential(user.TerradueCloudUsername, user.GetSessionApiKey());
				}

				//get apps link from the communities the user can see
				foreach (var community in communities.Items) {
					if (community.IsUserJoined(context.UserId)) {
						var app = community.GetThematicApplication();
						if (app != null) {
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
				result = ose.Query(multiOSE, httpRequest.QueryString, responseType);
			}

			var sresult = result.SerializeToString();

            //replace usernames in apps
            try {
                var user = UserTep.FromId(context, context.UserId);
                sresult = sresult.Replace("${USERNAME}", user.Username);
                sresult = sresult.Replace("${T2USERNAME}", user.TerradueCloudUsername);
                sresult = sresult.Replace("${T2APIKEY}", user.GetSessionApiKey());
            } catch (Exception e) {
                context.LogError(this, e.Message);
            }
            
			context.Close ();         
            return new HttpResult (sresult, result.ContentType);         
        }
  
        public object Get (ThematicAppByCommunitySearchRequestTep request)
        {
            IfyWebContext context = TepWebContext.GetWebContext (PagePrivileges.EverybodyView);
            context.Open ();
            context.LogInfo (this, string.Format ("/community/{{domain}}/apps/search GET domain='{0}'",request.Domain));

            var domain = ThematicCommunity.FromIdentifier(context, request.Domain);
			IOpenSearchResultCollection result;
            OpenSearchEngine ose = MasterCatalogue.OpenSearchEngine;
            HttpRequest httpRequest = HttpContext.Current.Request;         
            Type responseType = OpenSearchFactory.ResolveTypeFromRequest(httpRequest, ose);         

			if (request.cache) {

                bool isjoined = domain.IsUserJoined(context.UserId);

                    EntityList<ThematicApplicationCached> appsCached = new EntityList<ThematicApplicationCached>(context);
                if (isjoined) {
                    appsCached.SetFilter("DomainId", domain.Id.ToString());
                } else {
                    appsCached.SetFilter("DomainId", "-1");//if user is not joined we dont want him to see results
                }
                appsCached.AddSort("LastUpdate", SortDirection.Descending);
                result = ose.Query(appsCached, httpRequest.QueryString, responseType);
                OpenSearchFactory.ReplaceOpenSearchDescriptionLinks(appsCached, result);
            
			} else {
                
				var apps = new EntityList<DataPackage>(context);
				apps.SetFilter("Kind", ThematicApplication.KINDRESOURCESETAPPS.ToString());
				apps.SetFilter("DomainId", domain.Id.ToString());
				apps.Load();

				// the opensearch cache system uses the query parameters
				// we add to the parameters the filters added to the load in order to avoir wrong cache
				// we use 't2-' in order to not interfer with possibly used query parameters
				var qs = new NameValueCollection(Request.QueryString);
				foreach (var filter in apps.FilterValues) qs.Add("t2-" + filter.Key.FieldName, filter.Value.ToString());
                   
                apps.OpenSearchEngine = ose;
     
                List<Terradue.OpenSearch.IOpenSearchable> osentities = new List<Terradue.OpenSearch.IOpenSearchable>();
                foreach (var app in apps.Items) {
                    app.OpenSearchEngine = ose;
                    osentities.AddRange(app.GetOpenSearchableArray());
                }

                var settings = MasterCatalogue.OpenSearchFactorySettings;
                MultiGenericOpenSearchable multiOSE = new MultiGenericOpenSearchable(osentities, settings);
				result = ose.Query(multiOSE, httpRequest.QueryString, responseType);
			}

			var sresult = result.SerializeToString();

            //replace usernames in apps
            try {
                var user = UserTep.FromId(context, context.UserId);
                sresult = sresult.Replace("${USERNAME}", user.Username);
                sresult = sresult.Replace("${T2USERNAME}", user.TerradueCloudUsername);
                sresult = sresult.Replace("${T2APIKEY}", user.GetSessionApiKey());
            } catch (Exception e) {
                context.LogError(this, e.Message);
            }
            
            context.Close ();         
            return new HttpResult (sresult, result.ContentType);         
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

			IOpenSearchResultCollection result;
			OpenSearchEngine ose = MasterCatalogue.OpenSearchEngine;
			Type responseType = OpenSearchFactory.ResolveTypeFromRequest(HttpContext.Current.Request, ose);
			List<Terradue.OpenSearch.IOpenSearchable> osentities = new List<Terradue.OpenSearch.IOpenSearchable>();
			var settings = MasterCatalogue.OpenSearchFactorySettings;
			OpenSearchableFactorySettings specsettings = (OpenSearchableFactorySettings)settings.Clone();

			UserTep user = null;

			if (context.UserId != 0) {
				user = UserTep.FromId(context, context.UserId);            
				if (request.cache) {               
					var domain = user.GetPrivateDomain();
					EntityList<ThematicApplicationCached> appsCached = new EntityList<ThematicApplicationCached>(context);
                    appsCached.SetFilter("DomainId", domain.Id);
                    appsCached.AddSort("LastUpdate", SortDirection.Descending);               

					result = ose.Query(appsCached, Request.QueryString, responseType);
					OpenSearchFactory.ReplaceOpenSearchDescriptionLinks(appsCached, result);               
				} else {               
					//get user private thematic app
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
					MultiGenericOpenSearchable multiOSE = new MultiGenericOpenSearchable(osentities, specsettings);
                    result = ose.Query(multiOSE, Request.QueryString, responseType);
				}            
			} else {
				result = ose.Query(new MultiGenericOpenSearchable(osentities, specsettings), Request.QueryString, responseType);
			}

            string sresult = result.SerializeToString();

			//replace usernames in apps
			if (user != null) {
				try {
					sresult = sresult.Replace("${USERNAME}", user.Username);
					sresult = sresult.Replace("${T2USERNAME}", user.TerradueCloudUsername);
					sresult = sresult.Replace("${T2APIKEY}", user.GetSessionApiKey());
				} catch (Exception e) {
					context.LogError(this, e.Message);
				}
			}

            context.Close();

            return new HttpResult(sresult, result.ContentType);
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
              
		public object Get(ThematicAppEditorGetRequestTep request) {
            IfyWebContext context = TepWebContext.GetWebContext(PagePrivileges.UserView);
            WebThematicAppEditor result = null;
			context.Open();
            try{
	            context.LogInfo(this, string.Format("/app/editor GET url='{0}'", request.Url));
                if (string.IsNullOrEmpty(request.Url)) throw new Exception("Invalid url");

				OwsContextAtomFeed feed = null;

				if(!request.Url.StartsWith("http") || request.Url.StartsWith(context.BaseUrl)){
					if (!request.Url.StartsWith("http")) {
						var urib = new UriBuilder((context.BaseUrl));
						var path = request.Url.Substring(0, request.Url.IndexOf("?"));
						var query = request.Url.Substring(request.Url.IndexOf("?") + 1);
						urib.Path = path;
						urib.Query = query;
						request.Url = urib.Uri.AbsoluteUri;
					}

					var url = new Uri(request.Url);
     
					var r = new System.Text.RegularExpressions.Regex(@"^\/t2api\/community\/(?<community>[a-zA-Z0-9_\-]+)\/apps\/search");
					var m = r.Match(url.AbsolutePath);
					if (m.Success) {
						var community = m.Result("${community}");
						var nvc = HttpUtility.ParseQueryString(url.Query);
						var uid = nvc["uid"];

						var domain = Domain.FromIdentifier(context, community);

						if (!string.IsNullOrEmpty(nvc["cache"]) && nvc["cache"] == "true") {
							var app = ThematicApplicationCached.FromUidAndDomain(context, uid, domain.Id);
							feed = ThematicAppCachedFactory.GetOwsContextAtomFeed(app.TextFeed);
						} else {
							OpenSearchEngine ose = MasterCatalogue.OpenSearchEngine;
							Type responseType = OpenSearchFactory.ResolveTypeFromRequest(HttpContext.Current.Request, ose);
							var apps = new EntityList<DataPackage>(context);
							apps.SetFilter("Kind", ThematicApplication.KINDRESOURCESETAPPS.ToString());
							apps.SetFilter("DomainId", domain.Id.ToString());
							apps.Load();

							// the opensearch cache system uses the query parameters
							// we add to the parameters the filters added to the load in order to avoir wrong cache
							// we use 't2-' in order to not interfer with possibly used query parameters
							var qs = new NameValueCollection(Request.QueryString);
							foreach (var filter in apps.FilterValues) qs.Add("t2-" + filter.Key.FieldName, filter.Value.ToString());

							apps.OpenSearchEngine = ose;

							List<Terradue.OpenSearch.IOpenSearchable> osentities = new List<Terradue.OpenSearch.IOpenSearchable>();
							foreach (var app in apps.Items) {
								app.OpenSearchEngine = new OpenSearchEngine();
								osentities.AddRange(app.GetOpenSearchableArray());
							}

							nvc.Set("format", "atom");

							var settings = MasterCatalogue.OpenSearchFactorySettings;
							MultiGenericOpenSearchable multiOSE = new MultiGenericOpenSearchable(osentities, settings);
							var response = ose.Query(multiOSE, nvc, typeof(AtomFeed));
							feed = ThematicAppCachedFactory.GetOwsContextAtomFeed(response.SerializeToString());
						}
					}
				} else {
					var urib = new UriBuilder(request.Url);
					var nvc = HttpUtility.ParseQueryString(urib.Query);
					nvc.Set("format", "atom");
					var queryString = Array.ConvertAll(nvc.AllKeys, key => string.Format("{0}={1}", key, nvc[key]));
                    urib.Query = string.Join("&", queryString);               
					HttpWebRequest httpRequest = (HttpWebRequest)WebRequest.Create(request.Url);
                    using (var resp = httpRequest.GetResponse()) {
                        using (var stream = resp.GetResponseStream()) {
							feed = ThematicAppCachedFactory.GetOwsContextAtomFeed(stream);
                        }
                    }                
                }
                
				if(feed != null){
					if (feed.Items != null) {
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
                if (request.UpdateCache && !string.IsNullOrEmpty(request.UpdateCacheDomain)) {
                    try {
                        var appFactory = new ThematicAppCachedFactory(context);
                        var community = ThematicCommunity.FromIdentifier(context, request.UpdateCacheDomain);
                        appFactory.RefreshCachedAppsForCommunity(community);
                    } catch (Exception e) {
                        context.LogError(this, string.Format("Unable to cache app -- " + e.Message));
                    }
                }

			    context.Close();
			} catch (Exception e) {
				context.LogError(this, e.Message);
				context.Close();
				throw e;
			}
			return new WebResponseBool(true);
		}

        public object Delete(ThematicAppEditorDeleteRequestTep request) {
            IfyWebContext context = TepWebContext.GetWebContext(PagePrivileges.UserView);
            context.Open();
            try {
                context.LogInfo(this, string.Format("/app?index={0}&uid={1} DELETE",request.Index, request.Uid));

                var user = UserTep.FromId(context, context.UserId);
                var index = string.IsNullOrEmpty(request.Index) ? user.TerradueCloudUsername : request.Index;
                if (index != user.TerradueCloudUsername) throw new Exception("It is currently only possible to delete entries from your own index!");
                if (string.IsNullOrEmpty(index)) throw new Exception("Unable to DELETE from empty index");
                if (string.IsNullOrEmpty(request.Uid)) throw new Exception("Invalid entry identifier");
                var apikey = user.GetSessionApiKey();

                //delete entry from catalogue
                try {
                    CatalogueFactory.DeleteEntryFromIndex(context, index, request.Uid, user.Username, apikey);
                } catch (Exception e) {
                    throw new Exception("Unable to DELETE entry " + request.Uid + " from " + index + " - " + e.Message);
                }

                //update cache apps
                request.UpdateCacheDomain = user.Username;//TEMPORARY, for now we only allow to delete in current user's index
                if (request.UpdateCache && !string.IsNullOrEmpty(request.UpdateCacheDomain)) {
                    try {
                        var appFactory = new ThematicAppCachedFactory(context);
                        var community = ThematicCommunity.FromIdentifier(context, request.UpdateCacheDomain);
                        appFactory.RefreshCachedAppsForCommunity(community);
                    } catch (Exception e) {
                        context.LogError(this, string.Format("Unable to cache app -- " + e.Message));
                    }
                }

                context.Close();
            } catch (Exception e) {
                context.LogError(this, e.Message);
                context.Close();
                throw e;
            }
            return new WebResponseBool(true);
        }

        public object Post(ThematicAppEditorSaveAsFileRequestTep request) {
            IfyWebContext context = TepWebContext.GetWebContext(PagePrivileges.UserView);
            string result = null;
            context.Open();
            try {
                context.LogInfo(this, string.Format("/app/editor/xml POST"));

                var owsentry = request.ToOwsContextAtomEntry(context);
                var feed = new OwsContextAtomFeed();
                var entries = new List<OwsContextAtomEntry>();
                entries.Add(owsentry);
                feed.Items = entries;

                var stream = new System.IO.MemoryStream();
                var sw = XmlWriter.Create(stream, new XmlWriterSettings() { Indent = true, NamespaceHandling = NamespaceHandling.OmitDuplicates });
                Atom10FeedFormatter atomFormatter = new Atom10FeedFormatter(feed);
                atomFormatter.WriteTo(sw);
                sw.Flush();
                sw.Close();
                stream.Seek(0, System.IO.SeekOrigin.Begin);
                var reader = new System.IO.StreamReader(stream);
                result = reader.ReadToEnd();
                context.Close();
            } catch (Exception e) {
                context.LogError(this, e.Message);
                context.Close();
                throw e;
            }
            return new WebResponseString(result);
        }   


        public object Get(ThematicAppCacheRequestTep request) {
            var context = TepWebContext.GetWebContext(PagePrivileges.AdminOnly);
            context.Open();
			try {
                context.LogInfo(this, string.Format("/apps/cache GET"));
				var appFactory = new ThematicAppCachedFactory(context);
				//case TEP -- we don't want user privates apps to be cached
				appFactory.RefreshCachedApps(false, true, true);
                context.Close();
			} catch (Exception e) {
                context.LogError(this, e.Message);
                context.Close();
                throw e;
            }
			return new WebResponseBool(true);
        }

		public object Get(ThematicAppCheckAvailabilityRequest request){
			var context = TepWebContext.GetWebContext(PagePrivileges.UserView);
			bool result = false;
            context.Open();
            try {
                context.LogInfo(this, string.Format("/apps/available GET"));
				var user = UserTep.FromId(context, context.UserId);
				var apikey = user.GetSessionApiKey();
				result = !CatalogueFactory.CheckIdentifierExists(context, request.Index, request.Uid, apikey);
                context.Close();
            } catch (Exception e) {
                context.LogError(this, e.Message);
                context.Close();
                throw e;
            }
            return new WebResponseBool(result);
		}

        public object Post(LoadAppFromFile request) {
            var context = TepWebContext.GetWebContext(PagePrivileges.UserView);
            WebThematicAppEditor result = null;
            try {
                context.Open();
                context.LogInfo(this, string.Format("/app/editor/image POST"));

                OwsContextAtomFeed feed = null;

                if (this.RequestContext.Files.Length > 0) {
                    var uploadedFile = this.RequestContext.Files[0];
                    using (var stream = uploadedFile.InputStream) {
                        stream.Seek(0, System.IO.SeekOrigin.Begin);
                        feed = ThematicAppCachedFactory.GetOwsContextAtomFeed(stream);
                        if (feed != null) {
                            if (feed.Items != null) {
                                foreach (OwsContextAtomEntry item in feed.Items) {
                                    if (result == null) result = new WebThematicAppEditor(item);
                                }
                            }
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

    }
}

