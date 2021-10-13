using System;
using Terradue.Portal;
using System.Collections;
using System.Collections.Specialized;
using Terradue.OpenSearch;
using Terradue.OpenSearch.Schema;
using Terradue.OpenSearch.Request;
using Terradue.OpenSearch.Result;
using Terradue.ServiceModel.Syndication;

namespace Terradue.Tep {

    /// <summary>
    /// Data Collection
    /// </summary>
    /// <description>
    /// A Collection is the extension of dataset \ref Series for the TEP. It can be an item defined in a \ref ThematicApplication
    /// as input dataset for the service or for visualization.
    /// </description>
    /// \ingroup TepData
    /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation" 
    [EntityTable(null, EntityTableConfiguration.Custom, HasPermissionManagement = true, Storage = EntityTableStorage.Above)]
    public class Collection : Series, IProxiedOpenSearchable, IAtomizable {

        /// <summary>
        /// Initializes a new instance of the <see cref="Terradue.Tep.Controller.DataSeries"/> class.
        /// </summary>
        /// <param name="context">Context.</param>
        public Collection (IfyContext context) : base (context){
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Terradue.Tep.Controller.DataSeries"/> class.
        /// </summary>
        /// <param name="context">Context.</param>
        /// <param name="s">Serie</param>
        public Collection (IfyContext context, Series s) : base (context){
            this.Id = s.Id;
            this.Identifier = s.Identifier;
            this.Name = s.Name;
            this.Description = s.Description;
            this.CatalogueDescriptionUrl = s.CatalogueDescriptionUrl;
        }

        /// <summary>
        /// Creates a new Series instance representing the series with the specified ID.
        /// </summary>
        /// <returns>The identifier.</returns>
        /// <param name="context">Context.</param>
        /// <param name="id">Identifier.</param>
        public new static Collection FromId(IfyContext context, int id) {
            Collection result = new Collection(context);
            result.Id = id;
            try{
                result.Load();
            }catch(Exception){
                Series s = Series.FromId(context, id);
                result = new Collection(context, s);
            }
            return result;
        }

        /// <summary>
        /// Creates a new Series instance representing the series with the specified unique identifier.
        /// </summary>
        /// <returns>The identifier.</returns>
        /// <param name="context">Context.</param>
        /// <param name="identifier">Identifier.</param>
        public new static Collection FromIdentifier(IfyContext context, string identifier) {
            Collection result = new Collection(context);
            result.Identifier = identifier;
            try{
                result.Load();
            }catch(Exception){
                Series s = Series.FromIdentifier(context, identifier);
                result = new Collection(context, s);
            }
            return result;
        }

        /// <summary>
        /// Determines whether this instance is public.
        /// </summary>
        /// <returns><c>true</c> if this instance is public; otherwise, <c>false</c>.</returns>
        public bool IsPublic(){
            return DoesGrantPermissionsToAll();
        }

        /// <summary>
        /// Optional function that apply to the result after the search and before the result is returned by OpenSearchEngine.
        /// </summary>
        /// <param name="osr">IOpenSearchResult cotnaing the result of the a search</param>
        /// <param name="request">Request.</param>
        public new void ApplyResultFilters(OpenSearchRequest request, ref IOpenSearchResultCollection osr, string finalContentType) {
            base.ApplyResultFilters(request, ref osr, finalContentType);
            //MasterCatalogue.ProxyOpenSearchResult(this, request, osr);
        }

        public OpenSearchUrl GetSearchBaseUrl ()
        {
            return new OpenSearchUrl (string.Format ("{0}/data/collection/{1}/search", context.BaseUrl, this.Identifier));
        }

        public OpenSearchDescriptionUrl GetDescriptionBaseUrl (string mimeType)
        {
            var extraNs = new System.Xml.Serialization.XmlSerializerNamespaces();
            extraNs.Add("geo", "http://a9.com/-/opensearch/extensions/geo/1.0/");
            extraNs.Add("time", "http://a9.com/-/opensearch/extensions/time/1.0/");
            extraNs.Add("dct", "http://purl.org/dc/terms/");
            return new OpenSearchDescriptionUrl(mimeType, string.Format ("{0}/data/collection/{1}/description", context.BaseUrl, this.Identifier), "search", extraNs);
        }

        /// <summary>
        /// Gets the local open search description.
        /// </summary>
        /// <returns>The local open search description.</returns>
        public OpenSearchDescription GetProxyOpenSearchDescription() {

            OpenSearchDescription OSDD = base.GetOpenSearchDescription();

            OSDD.ShortName = "E-CEO Catalogue";
            OSDD.Attribution = "European Space Agency";
            OSDD.Contact = "info@esa.int";
            OSDD.Developer = "Terradue GeoSpatial Development Team";
            OSDD.SyndicationRight = "open";
            OSDD.AdultContent = "false";
            OSDD.Language = "en-us";
            OSDD.OutputEncoding = "UTF-8";
            OSDD.InputEncoding = "UTF-8";
            OSDD.Description = "This Search Service performs queries in the available data series of Terradue catalogue. There are several URL templates that return the results in different formats (RDF, ATOM or KML). This search service is in accordance with the OGC 10-032r3 specification.";

            OSDD.ExtraNamespace.Add("geo", "http://a9.com/-/opensearch/extensions/geo/1.0/");
            OSDD.ExtraNamespace.Add("time", "http://a9.com/-/opensearch/extensions/time/1.0/");
            OSDD.ExtraNamespace.Add("dct", "http://purl.org/dc/terms/");

            // The new URL template list 
            Hashtable newUrls = new Hashtable();
            UriBuilder urib;
            NameValueCollection query = OpenSearchFactory.GetOpenSearchParameters(OpenSearchFactory.GetOpenSearchUrlByType(OSDD, "application/atom+xml"));
            string[] queryString = Array.ConvertAll(query.AllKeys, key => string.Format("{0}={1}", key, query[key]));

            urib = new UriBuilder(context.BaseUrl);
            urib.Path += string.Format("/data/collection/{0}/search", this.Identifier);

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

        public override AtomItem ToAtomItem(NameValueCollection parameters) {
			string identifier = this.Identifier;
			string name = (this.Name != null ? this.Name : this.Identifier);
			string text = (this.TextContent != null ? this.TextContent : name);

			AtomItem atomEntry = null;
			var entityType = EntityType.GetEntityType(typeof(Collection));
			Uri id = new Uri(context.BaseUrl + "/" + entityType.Keyword + "/search?id=" + this.Identifier);
			try {
				atomEntry = new AtomItem(name, text, null, id.ToString(), DateTime.UtcNow);
			} catch (Exception) {
				atomEntry = new AtomItem();
			}

			atomEntry.ElementExtensions.Add("identifier", "http://purl.org/dc/elements/1.1/", this.Identifier);

			atomEntry.Links.Add(new SyndicationLink(id, "self", name, "application/atom+xml", 0));

            //add description link
            UriBuilder search = new UriBuilder(context.BaseUrl + "/" + entityType.Keyword +"/" + this.Identifier + "/description");
            atomEntry.Links.Add(new SyndicationLink(search.Uri, "search", name, "application/atom+xml", 0));

            //add search link
            search = new UriBuilder(context.BaseUrl + "/" + entityType.Keyword + "/" + identifier + "/search");
            atomEntry.Links.Add(new SyndicationLink(search.Uri, "public", name, "application/atom+xml", 0));

            //add alternate link
            search = new UriBuilder(this.CatalogueDescriptionUrl);
            atomEntry.Links.Add(new SyndicationLink(search.Uri, "alternate", name, "application/atom+xml", 0));
            			
            //add via link
			Uri share = new Uri(context.BaseUrl + "/share?url=" + id.AbsoluteUri);
			atomEntry.Links.Add(new SyndicationLink(share, "via", name, "application/atom+xml", 0));
			atomEntry.ReferenceData = this;

			var basepath = new UriBuilder(context.BaseUrl);
			basepath.Path = "user";
			
			return atomEntry;
        }

        public new NameValueCollection GetOpenSearchParameters() {
            return OpenSearchFactory.GetBaseOpenSearchParameter();
        }
    }
}

