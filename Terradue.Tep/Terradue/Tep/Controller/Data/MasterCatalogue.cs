using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Web;
using Terradue.OpenSearch;
using Terradue.OpenSearch.Engine;
using Terradue.OpenSearch.Engine.Extensions;
using Terradue.OpenSearch.Filters;
using Terradue.OpenSearch.GeoJson.Extensions;
using Terradue.OpenSearch.Request;
using Terradue.OpenSearch.Result;
using Terradue.OpenSearch.Schema;
using Terradue.Portal;
using Terradue.ServiceModel.Syndication;

namespace Terradue.Tep {

    /// <summary>
    /// Master catalogue.
    /// </summary>
    public class MasterCatalogue : IOpenSearchable {

        private static OpenSearchEngine ose;

        private static OpenSearchableFactorySettings settings;

        private static OpenSearchMemoryCache searchCache;

        private IfyContext context;

        /// <summary>URL of the catalogue</summary>
        public string Url { get; protected set; }

        /// <summary>Caption of the catalogue</summary>
        public string Caption { get; protected set; }

        /// <summary>sync with series</summary>
        public bool SyncSeries { get; protected set; }

        //---------------------------------------------------------------------------------------------------------------------

        public MasterCatalogue(IfyContext context) {
            this.context = context;
        }

        //---------------------------------------------------------------------------------------------------------------------  

        /// <summary>
        /// Gets the web server open search description.
        /// </summary>
        /// <returns>The web server open search description.</returns>
        public OpenSearchDescription GetWebServerOpenSearchDescription() {

            OpenSearchDescription OSDD = new OpenSearchDescription();

            OSDD.ShortName = "Terradue Catalogue";
            OSDD.Attribution = "European Space Agency";
            OSDD.Contact = "info@esa.int";
            OSDD.Developer = "Terradue GeoSpatial Development Team";
            OSDD.SyndicationRight = "open";
            OSDD.AdultContent = "false";
            OSDD.Language = "en-us";
            OSDD.OutputEncoding = "UTF-8";
            OSDD.InputEncoding = "UTF-8";
            OSDD.Description = "This Search Service performs queries in the available data packages of Tep catalogue. There are several URL templates that return the results in different formats (RDF, ATOM or KML). This search service is in accordance with the OGC 10-032r3 specification.";

            OSDD.ExtraNamespace.Add("geo", "http://a9.com/-/opensearch/extensions/geo/1.0/");
            OSDD.ExtraNamespace.Add("time", "http://a9.com/-/opensearch/extensions/time/1.0/");
            OSDD.ExtraNamespace.Add("dct", "http://purl.org/dc/terms/");

            // The new URL template list 
            Hashtable newUrls = new Hashtable();
            UriBuilder urib;
            NameValueCollection query = new NameValueCollection();
            string[] queryString;

            urib = new UriBuilder(context.BaseUrl);
            urib.Path += "/data/collection/search";
            query.Add(OpenSearchFactory.GetBaseOpenSearchParameter());

            query.Set("format", "atom");
            queryString = Array.ConvertAll(query.AllKeys, key => string.Format("{0}={1}", key, query[key]));
            urib.Query = string.Join("&", queryString);
            newUrls.Add("application/atom+xml", new OpenSearchDescriptionUrl("application/atom+xml", urib.ToString(), "search", OSDD.ExtraNamespace));

            query.Set("format", "json");
            queryString = Array.ConvertAll(query.AllKeys, key => string.Format("{0}={1}", key, query[key]));
            urib.Query = string.Join("&", queryString);
            newUrls.Add("application/json", new OpenSearchDescriptionUrl("application/json", urib.ToString(), "search", OSDD.ExtraNamespace));

            query.Set("format", "html");
            queryString = Array.ConvertAll(query.AllKeys, key => string.Format("{0}={1}", key, query[key]));
            urib.Query = string.Join("&", queryString);
            newUrls.Add("text/html", new OpenSearchDescriptionUrl("application/html", urib.ToString(), "search", OSDD.ExtraNamespace));

            OSDD.Url = new OpenSearchDescriptionUrl[newUrls.Count];

            newUrls.Values.CopyTo(OSDD.Url, 0);

            return OSDD;
        }

        public OpenSearchDescription GetOpenSearchDescription() {
            OpenSearchDescription osd = new OpenSearchDescription();
            osd.ShortName = Identifier;
            osd.Contact = context.GetConfigValue("CompanyEmail");
            osd.SyndicationRight = "open";
            osd.AdultContent = "false";
            osd.Language = "en-us";
            osd.OutputEncoding = "UTF-8";
            osd.InputEncoding = "UTF-8";
            osd.Developer = "Terradue OpenSearch Development Team";
            osd.Attribution = context.GetConfigValue("CompanyName");

            osd.ExtraNamespace.Add("geo", "http://a9.com/-/opensearch/extensions/geo/1.0/");
            osd.ExtraNamespace.Add("time", "http://a9.com/-/opensearch/extensions/time/1.0/");
            osd.ExtraNamespace.Add("dct", "http://purl.org/dc/terms/");

            List<OpenSearchDescriptionUrl> urls = new List<OpenSearchDescriptionUrl>();

            UriBuilder urlb = new UriBuilder(GetDescriptionBaseUrl());

            OpenSearchDescriptionUrl url = new OpenSearchDescriptionUrl("application/opensearchdescription+xml", urlb.ToString(), "self", osd.ExtraNamespace);
            urls.Add(url);

            urlb = new UriBuilder(GetSearchBaseUrl("application/atom+xml"));
            NameValueCollection query = GetOpenSearchParameters("application/atom+xml");

            NameValueCollection nvc = HttpUtility.ParseQueryString(urlb.Query);
            foreach (var key in nvc.AllKeys) {
                query.Set(key, nvc[key]);
            }

            foreach (var osee in OpenSearchEngine.Extensions.Values) {
                query.Set("format", osee.Identifier);

                string[] queryString = Array.ConvertAll(query.AllKeys, key => string.Format("{0}={1}", key, query[key]));
                urlb.Query = string.Join("&", queryString);
                url = new OpenSearchDescriptionUrl(osee.DiscoveryContentType, urlb.ToString(), "search", osd.ExtraNamespace);
                urls.Add(url);
            }

            osd.Url = urls.ToArray();

            return osd;
        }

        #region IOpenSearchable implementation

        public void ApplyResultFilters(OpenSearchRequest request, ref IOpenSearchResultCollection osr, string finalContentType) {
        }

        public QuerySettings GetQuerySettings(OpenSearchEngine ose) {
            IOpenSearchEngineExtension osee = ose.GetExtensionByContentTypeAbility(this.DefaultMimeType);
            if (osee == null)
                return null;
            return new QuerySettings(this.DefaultMimeType, osee.ReadNative);
        }


        public string DefaultMimeType {
            get {
                return "application/atom+xml";
            }
        }

        public virtual OpenSearchUrl GetSearchBaseUrl(string mimetype) {
            return new OpenSearchUrl(string.Format("{0}/{1}/search", context.BaseUrl, Identifier));
        }

        public virtual OpenSearchUrl GetDescriptionBaseUrl() {
            return new OpenSearchUrl(string.Format("{0}/{1}/description", context.BaseUrl, Identifier));
        }

        /// <summary>
        /// Create the specified querySettings and parameters.
        /// </summary>
        /// <param name="querySettings">Query settings.</param>
        /// <param name="parameters">Parameters.</param>
        public OpenSearchRequest Create(QuerySettings querySettings, NameValueCollection parameters) {

            UriBuilder url = new UriBuilder(context.BaseUrl);
            url.Path += "/data/collection/";
            var array = (from key in parameters.AllKeys
                         from value in parameters.GetValues(key)
                         select string.Format("{0}={1}", HttpUtility.UrlEncode(key), HttpUtility.UrlEncode(value)))
                .ToArray();
            url.Query = string.Join("&", array);

            AtomOpenSearchRequest request = new AtomOpenSearchRequest(new OpenSearchUrl(url.ToString()), GenerateCatalogueAtomFeed);

            return request;
        }
        /// <summary>
        /// Gets the open search parameters.
        /// </summary>
        /// <returns>The open search parameters.</returns>
        /// <param name="mimeType">MIME type.</param>
        public NameValueCollection GetOpenSearchParameters(string mimeType) {
            if (mimeType != "application/atom+xml") return null;
            return OpenSearchFactory.GetBaseOpenSearchParameter();
        }
        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name {
            get {
                return "data/collection";
            }
        }
        public string Identifier {
            get {
                return "data/collection";
            }
        }

        public bool CanCache {
            get {
                return false;
            }
        }

        public long TotalResults {
            get {
                return 0;
            }
        }
        #endregion
        /// <summary>
        /// Gets the open search engine.
        /// </summary>
        /// <value>The open search engine.</value>
        public static OpenSearchEngine OpenSearchEngine {

            get {
                if (ose == null) {
                    ose = GetNewOpenSearchEngine();
                }

                return ose;
            }
        }

        public static OpenSearchEngine ClearOpenSearchEngine() {

            ose = GetNewOpenSearchEngine();
            return ose;
        }

        public static OpenSearchEngine GetNewOpenSearchEngine() {
            var newOse = new OpenSearchEngine();
            AtomOpenSearchEngineExtension aosee = new AtomOpenSearchEngineExtension();
            newOse.RegisterExtension(aosee);
            FeatureCollectionOpenSearchEngineExtension ngosee = new FeatureCollectionOpenSearchEngineExtension();
            newOse.RegisterExtension(ngosee);

            var slidingExpiration = "30";
            
            if (!string.IsNullOrEmpty(System.Configuration.ConfigurationManager.AppSettings["Opensearch.Cache.SlidingExpiration"])){
                slidingExpiration = System.Configuration.ConfigurationManager.AppSettings["Opensearch.Cache.SlidingExpiration"];
            }
             
            if(slidingExpiration != "0"){            
                NameValueCollection nvc = new NameValueCollection();
                nvc.Add("SlidingExpiration", slidingExpiration);
                searchCache = new OpenSearchMemoryCache("cache", nvc);
                newOse.RegisterPreSearchFilter(searchCache.TryReplaceWithCacheRequest);
                newOse.RegisterPostSearchFilter(searchCache.CacheResponse);
            }
            return newOse;
        }

        /// <summary>
        /// Gets the open search factory settings.
        /// </summary>
        /// <value>The open search factory settings.</value>
        public static OpenSearchableFactorySettings OpenSearchFactorySettings{
            get {
                if(settings == null){
                    settings = new OpenSearchableFactorySettings(MasterCatalogue.OpenSearchEngine);
                    settings.MergeFilters = Terradue.Metadata.EarthObservation.Helpers.GeoTimeOpenSearchHelper.MergeGeoTimeFilters;

                    settings.ParametersKeywordsTable.Add("update", "{http://purl.org/dc/terms/}modified");
                    settings.ParametersKeywordsTable.Add("updated", "{http://purl.org/dc/terms/}modified");
                    settings.ParametersKeywordsTable.Add("modified", "{http://purl.org/dc/terms/}modified");
                    settings.ParametersKeywordsTable.Add("do", "{http://www.terradue.com/opensearch}downloadOrigin");
                    settings.ParametersKeywordsTable.Add("from", "{http://a9.com/-/opensearch/extensions/eo/1.0/}accessedFrom");
                    settings.ParametersKeywordsTable.Add("start", "{http://a9.com/-/opensearch/extensions/time/1.0/}start");
                    settings.ParametersKeywordsTable.Add("stop", "{http://a9.com/-/opensearch/extensions/time/1.0/}end");
                    settings.ParametersKeywordsTable.Add("end", "{http://a9.com/-/opensearch/extensions/time/1.0/}end");
                    settings.ParametersKeywordsTable.Add("trel", "{http://a9.com/-/opensearch/extensions/time/1.0/}relation");
                    settings.ParametersKeywordsTable.Add("box", "{http://a9.com/-/opensearch/extensions/geo/1.0/}box");
                    settings.ParametersKeywordsTable.Add("bbox", "{http://a9.com/-/opensearch/extensions/geo/1.0/}box");
                    settings.ParametersKeywordsTable.Add("geom", "{http://a9.com/-/opensearch/extensions/geo/1.0/}geometry");
                    settings.ParametersKeywordsTable.Add("geometry", "{http://a9.com/-/opensearch/extensions/geo/1.0/}geometry");
                    settings.ParametersKeywordsTable.Add("uid", "{http://a9.com/-/opensearch/extensions/geo/1.0/}uid");
                    settings.ParametersKeywordsTable.Add("id", "{http://purl.org/dc/terms/}identifier");
                    settings.ParametersKeywordsTable.Add("rel", "{http://a9.com/-/opensearch/extensions/geo/1.0/}relation");
                    settings.ParametersKeywordsTable.Add("cat", "{http://purl.org/dc/terms/}subject");
                    settings.ParametersKeywordsTable.Add("pt", "{http://a9.com/-/opensearch/extensions/eo/1.0/}productType");
                    settings.ParametersKeywordsTable.Add("psn", "{http://a9.com/-/opensearch/extensions/eo/1.0/}platform");
                    settings.ParametersKeywordsTable.Add("psi", "{http://a9.com/-/opensearch/extensions/eo/1.0/}platformSerialIdentifier");
                    settings.ParametersKeywordsTable.Add("isn", "{http://a9.com/-/opensearch/extensions/eo/1.0/}instrument");
                    settings.ParametersKeywordsTable.Add("sensor", "{http://a9.com/-/opensearch/extensions/eo/1.0/}sensorType");
                    settings.ParametersKeywordsTable.Add("st", "{http://a9.com/-/opensearch/extensions/eo/1.0/}sensorType");
                    settings.ParametersKeywordsTable.Add("od", "{http://a9.com/-/opensearch/extensions/eo/1.0/}orbitDirection");
                    settings.ParametersKeywordsTable.Add("ot", "{http://a9.com/-/opensearch/extensions/eo/1.0/}orbitType");
                    settings.ParametersKeywordsTable.Add("title", "{http://a9.com/-/opensearch/extensions/eo/1.0/}title");
                    settings.ParametersKeywordsTable.Add("track", "{http://a9.com/-/opensearch/extensions/eo/1.0/}track");
                    settings.ParametersKeywordsTable.Add("frame", "{http://a9.com/-/opensearch/extensions/eo/1.0/}frame");
                    settings.ParametersKeywordsTable.Add("swath", "{http://a9.com/-/opensearch/extensions/eo/1.0/}swathIdentifier");
                    settings.ParametersKeywordsTable.Add("cc", "{http://a9.com/-/opensearch/extensions/eo/1.0/}cloudCover");
                    settings.ParametersKeywordsTable.Add("lc", "{http://www.terradue.com/opensearch}landCover");
                    settings.ParametersKeywordsTable.Add("dcg", "{http://www.terradue.com/opensearch}doubleCheckGeometry");

                }
                return settings;
            }
        }

        /// <summary>
        /// Generates the catalogue syndication feed.
        /// </summary>
        /// <param name="stream">Stream.</param>
        /// <param name="parameters">Parameters.</param>
        public AtomFeed GenerateCatalogueAtomFeed(NameValueCollection parameters) {
            UriBuilder myUrl = new UriBuilder(context.BaseUrl + "/" + Name);
            string[] queryString = Array.ConvertAll(parameters.AllKeys, key => String.Format("{0}={1}", key, parameters[key]));
            myUrl.Query = string.Join("&", queryString);

            AtomFeed feed = new AtomFeed("Discovery feed for Tep QuickWin",
                                                       "This OpenSearch Service allows the discovery of the Tep Quickwin Digital Repository" +
                                                       "This search service is in accordance with the OGC 10-032r3 specification.",
                                                       myUrl.Uri, myUrl.ToString(), DateTimeOffset.UtcNow);

            feed.Generator = "Terradue Web Server";

            List<AtomItem> items = new List<AtomItem>();

            // Load all avaialable Datasets according to the context
            EntityList<Series> series = new EntityList<Series>(context);
            series.Load();
            var pds = new Terradue.OpenSearch.Request.PaginatedList<Series>();

            int startIndex = 0;
            if (parameters["startIndex"] != null) startIndex = int.Parse(parameters["startIndex"]);

            pds.AddRange(series);

            pds.PageNo = 1;
            if (parameters["startPage"] != null) pds.PageNo = int.Parse(parameters["startPage"]);

            pds.PageSize = 20;
            if (parameters["count"] != null) pds.PageSize = int.Parse(parameters["count"]);

            pds.StartIndex = startIndex;

            feed.ElementExtensions.Add("identifier", "http://purl.org/dc/elements/1.1/", "data/collection");

            foreach (Series s in pds.GetCurrentPage()) {

                Uri alternate = new Uri(context.BaseUrl + "/" + Name + "/" + s.Identifier + "/search?count=0");
                Uri id = new Uri(context.BaseUrl + "/" + Name + "/" + s.Identifier);

                AtomItem entry = new AtomItem(s.Name, s.Description, alternate, id.ToString(), DateTime.UtcNow);

                entry.Summary = new TextSyndicationContent(s.Name);
                entry.ElementExtensions.Add("identifier", "http://purl.org/dc/elements/1.1/", s.Identifier);

                UriBuilder search = new UriBuilder(context.BaseUrl + "/" + Name + "/" + s.Identifier + "/description");
                entry.Links.Add(new SyndicationLink(search.Uri, "search", s.Name, "application/atom+xml", 0));

                items.Add(entry);
            }

            feed.Items = items;

            return feed;
        }

        public static void ProxyOpenSearchResult(IOpenSearchable entity, OpenSearchRequest request, IOpenSearchResultCollection osr) {

            if (!(entity is IProxiedOpenSearchable)) return;

            OpenSearchFactory.ReplaceSelfLinks(entity, request, osr, Terradue.Metadata.EarthObservation.OpenSearch.Helpers.OpenSearchParametersHelper.EntrySelfLinkTemplate);
            OpenSearchFactory.ReplaceOpenSearchDescriptionLinks(entity, osr);                    

        }

        public static OpenSearchMemoryCache SearchCache
        {
            get
            {
                return searchCache;
            }
        }

        public static void ReplaceSelfLinksFormat(IOpenSearchResultCollection result, NameValueCollection queryString) {
            foreach (IOpenSearchResultItem item in result.Items) {
                var matchLinks = item.Links.Where(l => l.RelationshipType == "self").ToArray();
                string self = "";
                foreach (var link in matchLinks) {
                    self = link.Uri.AbsoluteUri;
                    item.Links.Remove(link);
                }

                if (self != null) {
                    UriBuilder urib = new UriBuilder(self);
                    var nvc = HttpUtility.ParseQueryString(urib.Query);
                    if (queryString["format"] != null) nvc.Set("format", queryString["format"]);
                    urib.Query = string.Join("&", nvc.AllKeys.Where(key => !string.IsNullOrWhiteSpace(nvc[key])).Select(key => string.Format("{0}={1}", HttpUtility.UrlEncode(key), HttpUtility.UrlEncode(nvc[key]))));
                    item.Links.Add(new SyndicationLink(urib.Uri, "self", "Reference link", result.ContentType, 0));
                }
            }

        }

    }

}

