using System;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Hosting;
using Terradue.OpenSearch;
using Terradue.OpenSearch.Engine;
using Terradue.OpenSearch.Response;
using Terradue.OpenSearch.Schema;
using Terradue.Portal;
using Terradue.OpenSearch.Request;
using Terradue.OpenSearch.Result;
using Terradue.Portal.OpenSearch;
using System.Collections.Generic;

namespace Terradue.Tep {

    public class UrlBasedOpenSearchable : IOpenSearchable {
        IfyContext context;
        IOpenSearchable entity;
        OpenSearchEngine ose;
        OpenSearchUrl url;
        OpenSearchDescription osd;
        OpenSearchableFactorySettings settings;

        public IEnumerable<IOpenSearchResultItem> Items;

        public UrlBasedOpenSearchable(IfyContext context, OpenSearchUrl url, OpenSearchableFactorySettings settings) {

            this.context = context;
            this.url = url;
            this.settings = settings;

        }

        public UrlBasedOpenSearchable(IfyContext context, OpenSearchDescription osd, OpenSearchableFactorySettings settings) {
            this.context = context;
            this.osd = osd;
            this.settings = settings;
        }

        public IOpenSearchable Entity {
            get {
                if (entity == null) {
                    if (url.ToString().StartsWith(context.BaseUrl)) {

                        Match match = Regex.Match(url.LocalPath.Replace(new Uri(context.BaseUrl).LocalPath, ""), @"(/.*)(/?.*)/search");

                        if (match.Success) {

                            if (match.Groups[1].Value == "/data/collection") {
                                string seriesId = match.Groups[2].Value;
                                Series ds = Series.FromIdentifier(context, seriesId);
                                entity = ds;
                            }

                            if (match.Groups[1].Value == "/data/package") {
                                EntityList<DataPackage> list = new EntityList<DataPackage>(context);
                                IOpenSearchResultCollection osr = ose.Query(list, url.SearchAttributes);
                                entity = list;
                            }

                            if (match.Groups[1].Value == "/service/wps") {
								//EntityList<WpsProcessOffering> wpsProcesses = new EntityList<WpsProcessOffering>(context);
								//CloudWpsFactory wpsOneProcesses = new CloudWpsFactory(context);
								//var entities = new List<IOpenSearchable> { wpsProcesses, wpsOneProcesses };
								//MultiGenericOpenSearchable list = new MultiGenericOpenSearchable(entities, ose);
								//IOpenSearchResultCollection osr = ose.Query(list, url.SearchAttributes);
								//entity = list;
								EntityList<WpsProcessOffering> wpsProcesses = new EntityList<WpsProcessOffering>(context);
								wpsProcesses.SetFilter("Available", "true");
								wpsProcesses.OpenSearchEngine = ose;
                                CloudWpsFactory wpsOneProcesses = new CloudWpsFactory(context);
								wpsOneProcesses.OpenSearchEngine = ose;
								wpsProcesses.Identifier = "service/wps";
								var entities = new List<IOpenSearchable> { wpsProcesses, wpsOneProcesses };

                                var settings = new OpenSearchableFactorySettings(ose);
                                MultiGenericOpenSearchable multiOSE = new MultiGenericOpenSearchable(entities, settings);
                                IOpenSearchResultCollection osr = ose.Query(multiOSE, url.SearchAttributes);
                                entity = multiOSE;
                                Items = osr.Items;
                            }

                            if (match.Groups[1].Value == "/cr/wps") {
                                EntityList<WpsProvider> list = new EntityList<WpsProvider>(context);
                                IOpenSearchResultCollection osr = ose.Query(list, url.SearchAttributes);
                                entity = list;
                            }

                            if (match.Groups[1].Value == "/job/wps") {
                                EntityList<WpsJob> list = new EntityList<WpsJob>(context);
                                IOpenSearchResultCollection osr = ose.Query(list, url.SearchAttributes);
                                entity = list;
                            }

                            if (match.Groups[1].Value == "/user") {
                                EntityList<UserTep> list = new EntityList<UserTep>(context);
                                IOpenSearchResultCollection osr = ose.Query(list, url.SearchAttributes);
                                entity = list;
                            }

                            if (match.Groups[1].Value == "/community") {
                                EntityList<ThematicCommunity> list = new EntityList<ThematicCommunity>(context);
                                IOpenSearchResultCollection osr = ose.Query(list, url.SearchAttributes);
                                entity = list;
                            }

                        }

                    }
                    if (entity == null) entity = new SmartGenericOpenSearchable(url, ose);
                }
                return entity;
            }
        }

        public bool IsProduct {
            get {
                if (Entity is DataSet) {
                    try {
                        if (String.IsNullOrEmpty(HttpUtility.ParseQueryString(url.Query)["id"])) return false;
                    } catch (Exception) {
                        return false;
                    }
                    return true;
                }
                return false;
            }
        }

        #region IOpenSearchable implementation

        public OpenSearchRequest Create(QuerySettings querySettings, NameValueCollection parameters) {
            NameValueCollection nvc = new NameValueCollection(parameters);
            NameValueCollection query = HttpUtility.ParseQueryString(url.Query);
            foreach (var key in query.AllKeys) {
                nvc.Set(key, query[key]);
            }
            return Entity.Create(querySettings, nvc);
        }

        public QuerySettings GetQuerySettings(OpenSearchEngine ose) {
            IOpenSearchEngineExtension osee = ose.GetExtensionByContentTypeAbility(this.DefaultMimeType);
            if (osee == null)
                return null;
            return new QuerySettings(this.DefaultMimeType, osee.ReadNative);
        }

        public string DefaultMimeType {
            get {
                return Entity.DefaultMimeType;
            }
        }

        public string Name {
            get {
                return GetOpenSearchDescription().ShortName;
            }
        }
        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Identifier {
            get {
                return Entity.Identifier;
            }
        }

        public OpenSearchDescription GetOpenSearchDescription() {
            if (osd == null) {

                osd = Entity.GetOpenSearchDescription();
                return osd;
            }
            return osd;
        }

        public NameValueCollection GetOpenSearchParameters(string mimeType) {
            foreach (OpenSearchDescriptionUrl url in this.GetOpenSearchDescription().Url) {
                if (url.Type == mimeType) return HttpUtility.ParseQueryString(new Uri(url.Template).Query);
            }

            return null;
        }

        public long TotalResults {
            get {
                if (IsProduct) return 1;
                NameValueCollection nvc = new NameValueCollection();
                nvc.Set("count", "0");
                IOpenSearchResultCollection osr = ose.Query(Entity, nvc, typeof(AtomFeed));
                AtomFeed feed = (AtomFeed)osr;
                try {
                    string s = feed.ElementExtensions.ReadElementExtensions<string>("totalResults", "http://a9.com/-/spec/opensearch/1.1/").Single();
                    return long.Parse(s);
                } catch (Exception e) {}
                return 0;
            }
        }

        public void ApplyResultFilters(OpenSearchRequest request, ref IOpenSearchResultCollection osr, string finalContentType) {
            Entity.ApplyResultFilters(request, ref osr, finalContentType);
        }
            
        public bool CanCache {
            get {
                return Entity.CanCache;
            }
        }
        #endregion
    }

    public class UrlBasedOpenSearchableFactory : IOpenSearchableFactory {
        IfyContext context;

        public UrlBasedOpenSearchableFactory(IfyContext context, OpenSearchableFactorySettings settings){
            this.context = context;
            Settings = (OpenSearchableFactorySettings)settings.Clone();
        }

        public OpenSearchableFactorySettings Settings { get; private set; }

        #region IOpenSearchableFactory implementation
        public IOpenSearchable Create(OpenSearchUrl url) {
            return new UrlBasedOpenSearchable(context, url, Settings);
        }

        public IOpenSearchable Create(OpenSearchDescription osd) {
            return new UrlBasedOpenSearchable(context, osd, Settings);
        }
        #endregion
    }
}

