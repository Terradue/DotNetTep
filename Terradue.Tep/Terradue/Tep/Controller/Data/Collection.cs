using System;
using Terradue.Portal;
using System.Collections;
using System.Collections.Specialized;
using Terradue.OpenSearch;
using Terradue.OpenSearch.Schema;
using Terradue.OpenSearch.Result;
using System.Collections.Generic;

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
    [EntityTable(null, EntityTableConfiguration.Custom, HasPrivilegeManagement = true, Storage = EntityTableStorage.Above)]
    public class Collection : Series, IProxiedOpenSearchable {

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
            }catch(Exception e){
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
            }catch(Exception e){
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
            return DoesGrantGlobalPermission();
        }

        /// <summary>
        /// Optional function that apply to the result after the search and before the result is returned by OpenSearchEngine.
        /// </summary>
        /// <param name="osr">IOpenSearchResult cotnaing the result of the a search</param>
        /// <param name="request">Request.</param>
        public void ApplyResultFilters(Terradue.OpenSearch.Request.OpenSearchRequest request, ref IOpenSearchResultCollection osr, string finalContentType) {
            base.ApplyResultFilters(request, ref osr, finalContentType);
            MasterCatalogue.ProxyOpenSearchResult(this, request, osr);
        }

        public OpenSearchUrl GetOpensearchSearchrl ()
        {
            return new OpenSearchUrl (string.Format ("{0}/data/collection/{1}/search", context.BaseUrl, this.Identifier));
        }

        public OpenSearchDescriptionUrl GetOpenSearchDescriptionUrl (string mimeType)
        {
            return new OpenSearchDescriptionUrl(mimeType, string.Format ("{0}/data/collection/{1}/description", context.BaseUrl, this.Identifier), "search");
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

    }
}

