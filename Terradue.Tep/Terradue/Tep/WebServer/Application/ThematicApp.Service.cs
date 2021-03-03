using System;
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
                communities.SetFilter("Kind", (int)DomainKind.Public + "," + (int)DomainKind.Hidden + "," + (int)DomainKind.Private);
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
                    var apikey = user.GetSessionApiKey();
                    var t2userid = user.TerradueCloudUsername;
                    if (!string.IsNullOrEmpty(apikey)) {
                        specsettings.Credentials = new System.Net.NetworkCredential(t2userid, apikey);
                    }
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
										context.LogError(this, e.Message, e);
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
								context.LogError(this, e.Message, e);
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
                context.LogError(this, e.Message, e);
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
                context.LogError(this, e.Message, e);
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
                    var apikey = user.GetSessionApiKey();
                    var t2userid = user.TerradueCloudUsername;
                    if (!string.IsNullOrEmpty(apikey)) {
                        specsettings.Credentials = new System.Net.NetworkCredential(t2userid, apikey);
                    }
					var app = user.GetPrivateThematicApp();
					if (app != null) {
						foreach (var item in app.Items) {
							if (!string.IsNullOrEmpty(item.Location)) {
								try {
									var sgOs = OpenSearchFactory.FindOpenSearchable(specsettings, new OpenSearchUrl(item.Location));
									osentities.Add(sgOs);
									context.LogDebug(this, string.Format("Apps search -- Add '{0}'", item.Location));
								} catch (Exception e) {
									context.LogError(this, e.Message, e);
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
					context.LogError(this, e.Message, e);
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
                context.LogError(this, e.Message, e);
                context.Close ();
                throw e;
            }
            HttpResult hr = new HttpResult (osd, "application/opensearchdescription+xml");
            return hr;
        }

        public object Get(ThematicAppCacheRequestTep request) {
            var context = TepWebContext.GetWebContext(PagePrivileges.UserView);
            context.Open();
			try {
                context.LogInfo(this, string.Format("/apps/cache GET"));
				var appFactory = new ThematicAppCachedFactory(context);
                if (!string.IsNullOrEmpty(request.Uid)) {
                    //refresh only a given app
                    appFactory.RefreshCachedApp(request.Uid);
                } else if (!string.IsNullOrEmpty(request.Username)) {
                    //refresh only for private apps
                    var user = UserTep.FromIdentifier(context, request.Username);
                    if (user.Id == context.UserId) appFactory.RefreshCachedAppsForUser(user);
                } else if (!string.IsNullOrEmpty(request.Community)) {
                    //refresh only for community apps
                    var community = ThematicCommunity.FromIdentifier(context, request.Community);
                    if (community.CanUserManage(context.UserId)) appFactory.RefreshCachedAppsForCommunity(community);
                } else {
                    //user should be admin
                    if (context.UserLevel == UserLevel.Administrator) {
                        //case TEP -- we don't want user privates apps to be cached
                        appFactory.RefreshCachedApps(false, true, true);
                    }
                }
                context.Close();
			} catch (Exception e) {
                context.LogError(this, e.Message, e);
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
                context.LogError(this, e.Message, e);
                context.Close();
                throw e;
            }
            return new WebResponseBool(result);
		}

        public object Get(ThematicAppGetRequestTep request) {
            var context = TepWebContext.GetWebContext(PagePrivileges.AdminOnly);
            List<WebThematicAppTep> result = new List<WebThematicAppTep>();
            context.Open();
            try {
                context.LogInfo(this, string.Format("/apps GET"));

                EntityList<ThematicApplicationCached> apps = new EntityList<ThematicApplicationCached>(context);
                apps.Load();

                foreach(var item in apps.GetItemsAsList()) {
                    var app = new WebThematicAppTep(item, context);
                    if (request.services && app.HasServices) {
                        EntityList<WpsProcessOffering> services = new EntityList<WpsProcessOffering>(context);
                        if (!string.IsNullOrEmpty(app.WpsServiceDomain)) {
                            try {
                                var dm = Domain.FromIdentifier(context, app.WpsServiceDomain);
                                services.SetFilter("DomainId", dm.Id.ToString());
                            }catch(Exception e) {
                                services.SetFilter("DomainId", "0");
                            }
                        }
                        if (app.WpsServiceTags != null && app.WpsServiceTags.Count > 0) {
                            IEnumerable<IEnumerable<string>> permutations = WpsProcessOfferingTep.GetPermutations(app.WpsServiceTags, app.WpsServiceTags.Count());
                            var r1 = permutations.Select(subset => string.Join("*", subset.Select(t => t).ToArray())).ToArray();
                            var tagsresult = string.Join(",", r1.Select(t => "*" + t + "*"));
                            services.SetFilter("Tags", tagsresult);
                        }
                        services.SetFilter("Available", "true");
                        services.Load();
                        var appServices = new List<WpsServiceOverview>();
                        foreach (var s in services.GetItemsAsList()) {
                            appServices.Add(new WpsServiceOverview {
                                Identifier = s.Identifier,
                                App = new AppOverview {
                                    Icon = app.Icon ?? "",
                                    Title = app.Title,
                                    Uid = app.Identifier
                                },
                                Name = s.Name,
                                Description = s.Description,
                                Version = s.Version,
                                Icon = s.IconUrl
                            });
                        }
                        app.Services = appServices;
                    }
                    result.Add(app);
                }

                context.Close();
            } catch (Exception e) {
                context.LogError(this, e.Message, e);
                context.Close();
                throw e;
            }
            return result;
        }

    }
}

