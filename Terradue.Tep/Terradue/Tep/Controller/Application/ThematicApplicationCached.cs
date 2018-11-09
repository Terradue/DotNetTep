using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Xml;
using Terradue.OpenSearch.Result;
using Terradue.Portal;
using Terradue.Portal.OpenSearch;
using Terradue.ServiceModel.Ogc.Owc.AtomEncoding;
using Terradue.ServiceModel.Syndication;
using Terradue.Util;

namespace Terradue.Tep {
    [EntityTable("app_cache", EntityTableConfiguration.Custom, HasDomainReference = true, AllowsKeywordSearch = true)]
    public class ThematicApplicationCached : EntitySearchable {

        [EntityDataField("feed", IsUsedInKeywordSearch = true)]
        public string TextFeed { get; set; }

        [EntityDataField("uid", IsUsedInKeywordSearch = true)]
        public string UId { get; set; }

		[EntityDataField("cat_index")]
        public string Index { get; set; }

		[EntityDataField("last_update")]
		public DateTime LastUpdate { get; set; }

        public OwsContextAtomFeed Feed { get; set; }

        public ThematicApplicationCached(IfyContext context) : base(context){}
            
        /// <summary>
        /// Initializes a new instance of the <see cref="T:Terradue.Tep.ThematicApplicationCached"/> class.
        /// </summary>
        /// <param name="context">Context.</param>
        /// <param name="identifier">Identifier.</param>
        /// <param name="domainid">Domainid.</param>
        /// <param name="feed">Feed.</param>
        public ThematicApplicationCached(IfyContext context, string identifier, int domainid, string feed) : base(context) {
            this.UId = identifier;
            this.DomainId = domainid;
            this.TextFeed = feed;
			this.Feed = ThematicAppCachedFactory.GetOwsContextAtomFeed(feed);
        }

		public static ThematicApplicationCached FromUidAndDomain(IfyContext context, string identifier, int domainid){
			var app = new ThematicApplicationCached(context);
			app.UId = identifier;
			app.DomainId = domainid;
			app.Load();
			return app;
		}
              
        public override string GetIdentifyingConditionSql() {
            if (this.UId == null) return null;
            if (!string.IsNullOrEmpty(this.Index)) return String.Format("t.uid={0} AND t.cat_index={1}", StringUtils.EscapeSql(this.UId), StringUtils.EscapeSql(this.Index));
            else return String.Format("t.uid={0} AND t.id_domain{1}", StringUtils.EscapeSql(this.UId), DomainId == 0 ? " IS NULL" : "=" + DomainId);
        }
  
        public override AtomItem ToAtomItem(NameValueCollection parameters) {
            var atomFormatter = new Atom10FeedFormatter();
            XmlReader xmlreader = XmlReader.Create(new StringReader(TextFeed = this.TextFeed));
            atomFormatter.ReadFrom(xmlreader);
            var feed = new OwsContextAtomFeed(atomFormatter.Feed, true);
            var result = new AtomItem(feed.Items.First());
            return result;
        }

        public bool CanCache {
            get {
                return false;
            }
        }

        #region IEntitySearchable implementation
        public override KeyValuePair<string, string> GetFilterForParameter(string parameter, string value) {
            switch (parameter) {
                case "uid":
                    if (!string.IsNullOrEmpty(value))
                        return new KeyValuePair<string, string>("UId", value);
                    else return new KeyValuePair<string, string>();
                default:
                    return base.GetFilterForParameter(parameter, value);
            }
        }

        #endregion      
    }

	public class ThematicAppCachedFactory {

		public IfyContext context { get; set; }
		public bool ForAgent { get; set; }

		public ThematicAppCachedFactory(IfyContext context){
			this.context = context;
			this.ForAgent = false;
		}

		private void LogDebug(string message){
			if (ForAgent) context.WriteDebug(0,message);
			else context.LogDebug(this, message);
		}
		private void LogInfo(string message) {
            if (ForAgent) context.WriteInfo(message);
            else context.LogInfo(this, message);
        }
		private void LogError(string message) {
            if (ForAgent) context.WriteError(message);
            else context.LogError(this, message);
        }

        /// <summary>
        /// Gets the identifier from feed.
        /// </summary>
        /// <returns>The identifier from feed.</returns>
        /// <param name="entry">Entry.</param>
        public static string GetIdentifierFromFeed(OwsContextAtomEntry entry){
            string uid = "";
            var identifiers = entry.ElementExtensions.ReadElementExtensions<string>("identifier", "http://purl.org/dc/elements/1.1/");
            if (identifiers.Count > 0) uid = identifiers[0];
            return uid;
        }

        /// <summary>
        /// Gets the ows context atom feed.
        /// </summary>
        /// <returns>The ows context atom feed.</returns>
        /// <param name="s">S.</param>
        public static OwsContextAtomFeed GetOwsContextAtomFeed(string s) {
            var atomFormatter = new Atom10FeedFormatter();
            XmlReader xmlreader = XmlReader.Create(new StringReader(s));
            atomFormatter.ReadFrom(xmlreader);
            var feed = new OwsContextAtomFeed(atomFormatter.Feed, true);
            return feed;
        }

        /// <summary>
        /// Gets the ows context atom feed.
        /// </summary>
        /// <returns>The ows context atom feed.</returns>
        /// <param name="s">S.</param>
        public static OwsContextAtomFeed GetOwsContextAtomFeed(Stream s) {
            var sr = XmlReader.Create(s);
            Atom10FeedFormatter atomFormatter = new Atom10FeedFormatter();
            atomFormatter.ReadFrom(sr);
            sr.Close();
            var feed = new OwsContextAtomFeed(atomFormatter.Feed, true);
            return feed;
        }

        /// <summary>
        /// Gets the ows context atom feed as string.
        /// </summary>
        /// <returns>The ows context atom feed as string.</returns>
        /// <param name="feed">Feed.</param>
        public static string GetOwsContextAtomFeedAsString(OwsContextAtomFeed feed, string format = "atom") {
            string result = "";
            using (MemoryStream stream = new MemoryStream()) {
                var sw = System.Xml.XmlWriter.Create(stream);
                if (format == "json") {

                } else {
                    Atom10FeedFormatter atomFormatter = new Atom10FeedFormatter((SyndicationFeed)feed);
                    atomFormatter.WriteTo(sw);
                    sw.Flush();
                    sw.Close();
                    stream.Seek(0, SeekOrigin.Begin);
                    StreamReader reader = new StreamReader(stream);
                    result = reader.ReadToEnd();
                }
            }
            return result;
        }

		public static string GetIndexFromUrl(string url){
			try {
				var urib = new UriBuilder(url);
				var path = urib.Uri.AbsolutePath;
				path = path.TrimStart('/');
				var index = path.Substring(0, path.IndexOf('/'));
				return index;
			}catch(Exception e){
				return null;
			}
		}

        /// <summary>
        /// Creates the or update app from the url.
        /// </summary>
        /// <returns>The or update app.</returns>
        /// <param name="url">URL.</param>
        /// <param name="domainId">Domain identifier.</param>
		public List<int> CreateOrUpdateCachedAppFromUrl(string url, int domainId){
			List<int> upIds = new List<int>();
			if (string.IsNullOrEmpty(url)) return upIds;

            //TODO: IMPROVE
            url = url.Replace("/description", "/search");
            if (!url.Contains("/search")) return upIds;
			//end TODO

			var urib = new UriBuilder(url);
            var query = HttpUtility.ParseQueryString(urib.Query);

			//add user apikey
			var user = UserTep.FromId(context, context.UserId);
			var apikey = user.GetSessionApiKey();
			if(!string.IsNullOrEmpty(apikey)) query.Set("apikey", apikey);

			//add random for cache
			var random = new Random();
			query.Set("random", random.Next()+"");

			var queryString = Array.ConvertAll(query.AllKeys, key => string.Format("{0}={1}", key, query[key]));
            urib.Query = string.Join("&", queryString);
            url = urib.Uri.AbsoluteUri;

			var index = GetIndexFromUrl(url);
                                    
            try {
                HttpWebRequest httpRequest = (HttpWebRequest)WebRequest.Create(url);
                using (var resp = httpRequest.GetResponse()) {
                    using (var stream = resp.GetResponseStream()) {
                        var feed = GetOwsContextAtomFeed(stream);
                        if (feed.Items != null) {
                            foreach (OwsContextAtomEntry item in feed.Items) {
                                try {
                                    var appcached = CreateOrUpdateCachedApp(item, domainId, index);
                                    if(appcached != null) upIds.Add(appcached.Id);
                                    this.LogInfo(string.Format("ThematicAppCachedFactory -- Cached '{0}' from '{1}'", appcached.UId, url));
                                }catch(Exception e){
                                    this.LogError(string.Format("ThematicAppCachedFactory -- {0} - {1}'", e.Message, e.StackTrace));
                                }
                            }
                        }
                    }
                }
            } catch (Exception e) {
				this.LogError(string.Format("ThematicAppCachedFactory -- {0} - {1}", e.Message, e.StackTrace));
            }

			return upIds;
		}

		/// <summary>
        /// Creates or update the cached app
        /// </summary>
        /// <returns>The or update.</returns>
        /// <param name="entry">Entry.</param>
        /// <param name="domainid">Domainid.</param>
        public ThematicApplicationCached CreateOrUpdateCachedApp(OwsContextAtomEntry entry, int domainid, string index = null) {

            var appCategory = entry.Categories.FirstOrDefault(l => l.Label == "App");
            if (appCategory == null) return null;

            var identifier = GetIdentifierFromFeed(entry);
            var appcached = new ThematicApplicationCached(context);
            appcached.UId = identifier;

            if (index != null) appcached.Index = index;
            else appcached.DomainId = domainid;

            try {
                appcached.Load();
            } catch (Exception e) { }

            var feed = new OwsContextAtomFeed();
            feed.Items = new List<OwsContextAtomEntry> { entry };

			appcached.Index = index;
            appcached.Feed = feed;
            appcached.TextFeed = GetOwsContextAtomFeedAsString(feed);
            appcached.LastUpdate = entry.LastUpdatedTime.DateTime;
            appcached.Store();

            return appcached;
        }

		/// <summary>
        /// Gets the cached apps from domain.
        /// </summary>
        /// <returns>The cached apps from domain.</returns>
        /// <param name="domainid">Domainid.</param>
        public EntityList<ThematicApplicationCached> GetCachedAppsFromDomain(int domainid) {
            EntityList<ThematicApplicationCached> result = new EntityList<ThematicApplicationCached>(context);
            if (domainid == 0) result.SetFilter("DomainId", SpecialSearchValue.Null);
            else result.SetFilter("DomainId", domainid + "");
            result.Load();
            return result;
        }
  
        /// <summary>
        /// Creates the or update community apps.
        /// </summary>
        /// <returns>The or update community apps.</returns>
        /// <param name="community">Community.</param>
        public List<int> CreateOrUpdateCachedAppsFromCommunity(ThematicCommunity community) {
            List<int> upIds = new List<int>();
            var links = community.AppsLinks;
            if (links != null) {
                foreach (var link in links) {
					var ids = CreateOrUpdateCachedAppFromUrl(link, community.Id);
					upIds.AddRange(ids);
                }
            }
            return upIds;
        }

        /// <summary>
        /// Creates the or update cached apps from user.
        /// </summary>
        /// <returns>The or update cached apps from user.</returns>
        /// <param name="user">User.</param>
		public List<int> CreateOrUpdateCachedAppsFromUser(UserTep user) {
            List<int> upIds = new List<int>();
			var app = user.GetPrivateThematicApp();         
			var domain = user.GetPrivateDomain();
            foreach (var appItem in app.Items) {
                var ids = CreateOrUpdateCachedAppFromUrl(appItem.Location, domain.Id);
                upIds.AddRange(ids);
            }
            return upIds;
        }

        /// <summary>
        /// Refreshs the cached apps.
        /// </summary>
        /// <param name="withUserPrivateApps">If set to <c>true</c> with user private apps.</param>
        /// <param name="withCommunitiesApps">If set to <c>true</c> with communities apps.</param>
        /// <param name="withPublicApps">If set to <c>true</c> with public apps.</param>
		public void RefreshCachedApps(bool withUserPrivateApps, bool withCommunitiesApps, bool withPublicApps){

			//Get all existing apps
			EntityList<ThematicApplicationCached> existingApps = new EntityList<ThematicApplicationCached>(context);
			List<int> existingIds = new List<int>();
			if (withUserPrivateApps || withCommunitiesApps) {
				var domains = new EntityList<Domain>(context);
                string kind = (withUserPrivateApps ? (int)DomainKind.User + "" : "") + (withCommunitiesApps ? (withUserPrivateApps ? "," : "") + (int)DomainKind.Public + "," + (int)DomainKind.Private + "," + (int)DomainKind.Hidden : "");
				domains.SetFilter("Kind", kind);
                domains.Load();
				foreach (var d in domains) existingIds.Add(d.Id);
			}
            var filterValues = new List<object>();
			filterValues.Add(string.Join(",", existingIds));
			if (withPublicApps) filterValues.Add(SpecialSearchValue.Null);
			existingApps.SetFilter("DomainId", filterValues.ToArray());
            existingApps.Load();

            //will be filled with all updated ids
            List<int> upIds = new List<int>();

			// get the apps from the users
			if (withUserPrivateApps) {
				this.LogInfo(string.Format("RefreshCachedApps -- Get the apps from users"));
                var communities = new EntityList<ThematicCommunity>(context);
				communities.SetFilter("Kind", (int)DomainKind.User);
                communities.Load();
                foreach (var community in communities.Items) {
                    var ids = CreateOrUpdateCachedAppsFromCommunity(community);
                    upIds.AddRange(ids);
                }
            }

			// get the apps from the communities
			if (withCommunitiesApps) {
				this.LogInfo(string.Format("RefreshCachedApps -- Get the apps from communities"));
				var communities = new EntityList<ThematicCommunity>(context);
                communities.SetFilter("Kind", (int)DomainKind.Public + "," + (int)DomainKind.Private + "," + (int)DomainKind.Hidden);
				communities.Load();
				foreach (var community in communities.Items) {
					var ids = CreateOrUpdateCachedAppsFromCommunity(community);
					upIds.AddRange(ids);
				}
			}

			//get public apps 
			if (withPublicApps) {
				this.LogInfo(string.Format("RefreshCachedApps -- Get public apps"));
				var apps = new EntityList<ThematicApplication>(context);
				apps.SetFilter("DomainId", SpecialSearchValue.Null);
				apps.SetFilter("Kind", ThematicApplication.KINDRESOURCESETAPPS + "");
				apps.Load();
				foreach (var app in apps) {
					app.LoadItems();
					foreach (var appItem in app.Items) {
						var ids = CreateOrUpdateCachedAppFromUrl(appItem.Location, 0);
						upIds.AddRange(ids);
					}
				}
			}

            //delete apps not updated
            foreach (var app in existingApps) {
                if (!upIds.Contains(app.Id)) {
					this.LogInfo(string.Format("RefreshThematicAppsCache -- Delete not updated app '{0}' from domain {1}", app.UId, app.DomainId));
                    app.Delete();
                }
            }
		}

        /// <summary>
        /// Refreshs the cached apps for user.
        /// </summary>
        /// <param name="user">User.</param>
		public void RefreshCachedAppsForUser(UserTep user){
			//Get all existing apps for the user
            EntityList<ThematicApplicationCached> existingApps = new EntityList<ThematicApplicationCached>(context);
            List<int> existingIds = new List<int>();
			var domain = user.GetPrivateDomain();
			existingApps.SetFilter("DomainId", domain.Id);
            existingApps.Load();

			//will be filled with all updated ids
			List<int> upIds = CreateOrUpdateCachedAppsFromUser(user);

			//delete apps not updated
            foreach (var app in existingApps) {
                if (!upIds.Contains(app.Id)) {
                    this.LogInfo(string.Format("RefreshThematicAppsCache -- Delete not updated app '{0}' from domain {1}", app.UId, app.DomainId));
                    app.Delete();
                }
            }
   
		}

        /// <summary>
        /// Refreshs the cached apps for community.
        /// </summary>
        /// <param name="community">Community.</param>
		public void RefreshCachedAppsForCommunity(ThematicCommunity community) {
            //Get all existing apps for the community
            EntityList<ThematicApplicationCached> existingApps = new EntityList<ThematicApplicationCached>(context);
			List<int> existingIds = new List<int>();
			existingApps.SetFilter("DomainId", community.Id);
            existingApps.Load();

            //will be filled with all updated ids
			List<int> upIds = CreateOrUpdateCachedAppsFromCommunity(community);

            //delete apps not updated
            foreach (var app in existingApps) {
                if (!upIds.Contains(app.Id)) {
                    this.LogInfo(string.Format("RefreshThematicAppsCache -- Delete not updated app '{0}' from domain {1}", app.UId, app.DomainId));
                    app.Delete();
                }
            }

        }

        public void RefreshCachedAppsForIndexes(List<string> indexes){
            //Get all existing apps
            EntityList<ThematicApplicationCached> existingApps = new EntityList<ThematicApplicationCached>(context);

            existingApps.SetFilter("Index", string.Join(",", indexes));
            existingApps.Load();

            //will be filled with all updated ids
            List<int> upIds = new List<int>();

            foreach(var index in indexes){
                var baseurl = context.GetConfigValue("catalog-baseurl");
                var url = baseurl + "/" + index + "/search";
                var ids = CreateOrUpdateCachedAppFromUrl(url, 0);
                upIds.AddRange(ids);
            }

            //delete apps not updated
            foreach (var app in existingApps) {
                if (!upIds.Contains(app.Id)) {
                    this.LogInfo(string.Format("RefreshThematicAppsCache -- Delete not updated app '{0}' from index {1}", app.UId, app.Index));
                    app.Delete();
                }
            }
        }

    }
}
