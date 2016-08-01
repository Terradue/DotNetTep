using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Web;
using System.Xml;
using System.Xml.Linq;
using Terradue.OpenSearch;
using Terradue.OpenSearch.Engine;
using Terradue.OpenSearch.Engine.Extensions;
using Terradue.OpenSearch.Filters;
using Terradue.OpenSearch.GeoJson.Extensions;
using Terradue.OpenSearch.GeoJson.Result;
using Terradue.OpenSearch.Request;
using Terradue.OpenSearch.Response;
using Terradue.OpenSearch.Result;
using Terradue.OpenSearch.Schema;
using Terradue.Portal;
using Terradue.ServiceModel.Syndication;
using Terradue.Util;
using Terradue.Metadata.EarthObservation;


using Terradue.Metadata.EarthObservation.OpenSearch;
using Terradue.OpenSearch.RdfEO.Extensions;





namespace Terradue.Tep
{

    /// <summary>
    /// Master catalogue.
    /// </summary>
    public class MasterCatalogue : IOpenSearchable
    {

        private static OpenSearchEngine ose;

        private static OpenSearchMemoryCache searchCache;

        private IfyContext context;

        /// <summary>URL of the catalogue</summary>
        public string Url { get; protected set; }

        /// <summary>Caption of the catalogue</summary>
        public string Caption { get; protected set; }

        /// <summary>sync with series</summary>
        public bool SyncSeries { get; protected set; }

        //---------------------------------------------------------------------------------------------------------------------

        public MasterCatalogue(IfyContext context){
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
            newUrls.Add("application/atom+xml", new OpenSearchDescriptionUrl("application/atom+xml", urib.ToString(), "search"));

            query.Set("format", "json");
            queryString = Array.ConvertAll(query.AllKeys, key => string.Format("{0}={1}", key, query[key]));
            urib.Query = string.Join("&", queryString);
            newUrls.Add("application/json", new OpenSearchDescriptionUrl("application/json", urib.ToString(), "search"));

            query.Set("format", "html");
            queryString = Array.ConvertAll(query.AllKeys, key => string.Format("{0}={1}", key, query[key]));
            urib.Query = string.Join("&", queryString);
            newUrls.Add("text/html", new OpenSearchDescriptionUrl("application/html", urib.ToString(), "search"));

            OSDD.Url = new OpenSearchDescriptionUrl[newUrls.Count];

            newUrls.Values.CopyTo(OSDD.Url, 0);

            return OSDD;
        }

        public OpenSearchDescription GetOpenSearchDescription() {
            OpenSearchDescription OSDD = GetWebServerOpenSearchDescription();

            OSDD.Url = new OpenSearchDescriptionUrl[1];
            UriBuilder uri = new UriBuilder(context.BaseUrl);
            uri.Path += "/data/collection/search";
            OpenSearchDescriptionUrl osdu = new OpenSearchDescriptionUrl("application/atom+xml", uri.ToString(), "search");
            OSDD.Url[0] = osdu;

            return OSDD;
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

        /// <summary>
        /// Create the specified type and parameters.
        /// </summary>
        /// <param name="type">Type.</param>
        /// <param name="parameters">Parameters.</param>
        public OpenSearchRequest Create(string type, NameValueCollection parameters) {

            UriBuilder url = new UriBuilder(context.BaseUrl);
            url.Path += "/data/collection/";
            var array = (from key in parameters.AllKeys
                         from value in parameters.GetValues(key)
                         select string.Format("{0}={1}", HttpUtility.UrlEncode(key), HttpUtility.UrlEncode(value)))
                .ToArray();
            url.Query = string.Join("&", array);

            MemoryOpenSearchRequest request = new MemoryOpenSearchRequest(new OpenSearchUrl(url.ToString()), type);

            Stream input = request.MemoryInputStream;

            GenerateCatalogueAtomFeed(input, parameters);

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
                    ose = new OpenSearchEngine();
                    AtomOpenSearchEngineExtension aosee = new AtomOpenSearchEngineExtension();
                    ose.RegisterExtension(aosee);
                    FeatureCollectionOpenSearchEngineExtension ngosee = new FeatureCollectionOpenSearchEngineExtension();
                    ose.RegisterExtension(ngosee);
                    RdfOpenSearchEngineExtension rosee = new RdfOpenSearchEngineExtension();
                    ose.RegisterExtension(rosee);

                    NameValueCollection nvc = new NameValueCollection();
                    nvc.Add("SlidingExpiration", "30");
                    searchCache = new OpenSearchMemoryCache("cache", nvc);
                    ose.RegisterPreSearchFilter(searchCache.TryReplaceWithCacheRequest);
                    ose.RegisterPostSearchFilter(searchCache.CacheResponse);
                }

                return ose;
            }
        }

        /// <summary>
        /// Generates the catalogue syndication feed.
        /// </summary>
        /// <param name="stream">Stream.</param>
        /// <param name="parameters">Parameters.</param>
        public void GenerateCatalogueAtomFeed(Stream stream, NameValueCollection parameters) {
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

            //Atomizable.SerializeToStream ( res, stream.OutputStream );
            var sw = XmlWriter.Create(stream);
            Atom10FeedFormatter atomFormatter = new Atom10FeedFormatter(feed.Feed);
            atomFormatter.WriteTo(sw);
            sw.Flush();
            sw.Close();

        }

        public static void ProxyOpenSearchResult(IOpenSearchable entity, OpenSearchRequest request, IOpenSearchResultCollection osr) {

            if (!(entity is IProxiedOpenSearchable)) return;

            OpenSearchFactory.ReplaceSelfLinks(entity, request, osr, EarthObservationOpenSearchResultHelpers.EntrySelfLinkTemplate);
            OpenSearchFactory.ReplaceOpenSearchDescriptionLinks(entity, osr);                    

        }

    }

}

