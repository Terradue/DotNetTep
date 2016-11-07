using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Web;
using Terradue.OpenSearch;
using Terradue.OpenSearch.Engine;
using Terradue.OpenSearch.Request;
using Terradue.OpenSearch.Result;
using Terradue.OpenSearch.Schema;
using Terradue.Portal;
using Terradue.ServiceModel.Ogc.Owc.AtomEncoding;
using Terradue.ServiceModel.Syndication;

namespace Terradue.Tep.OpenSearch
{
    public class WpsJobProductOpenSearchable : IProxiedOpenSearchable
    {

        public WpsJob Wpsjob { private set; get; }

        IOpenSearchable openSearchable = null;

        public string Identifier
        {
            get
            {
                return Wpsjob.Identifier;
            }
        }

        public long TotalResults
        {
            get
            {
                return openSearchable.TotalResults;
            }
        }

        public string DefaultMimeType
        {
            get
            {
                return openSearchable.DefaultMimeType;
            }
        }

        public bool CanCache
        {
            get
            {
                return openSearchable.CanCache;
            }
        }

        readonly IfyContext context;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Terradue.Tep.WpsJobOpenSearchable"/> class.
        /// </summary>
        /// <param name="url">URL.</param>
        /// <param name="ose">Ose.</param>
        public WpsJobProductOpenSearchable(WpsJob job, IfyContext context)
        {
            this.context = context;
            this.Wpsjob = job;

            this.openSearchable = job.GetProductOpenSearchable();


        }


        public OpenSearchDescription GetOpenSearchDescription()
        {

            return openSearchable.GetOpenSearchDescription();
        }

        public OpenSearchDescription GetProxyOpenSearchDescription()
        {

            OpenSearchDescription osd = new OpenSearchDescription();

            osd.ShortName = string.IsNullOrEmpty(Wpsjob.Name) ? Wpsjob.Identifier : Wpsjob.Name;
            osd.Attribution = "European Space Agency";
            osd.Contact = "info@esa.int";
            osd.Developer = "Terradue GeoSpatial Development Team";
            osd.SyndicationRight = "open";
            osd.AdultContent = "false";
            osd.Language = "en-us";
            osd.OutputEncoding = "UTF-8";
            osd.InputEncoding = "UTF-8";
            osd.Description = "This Search Service performs queries in the available results of a job process. There are several URL templates that return the results in different formats (RDF, ATOM or KML). This search service is in accordance with the OGC 10-032r3 specification.";

            var searchExtensions = MasterCatalogue.OpenSearchEngine.Extensions;
            List<OpenSearchDescriptionUrl> urls = new List<OpenSearchDescriptionUrl>();

            NameValueCollection parameters = GetOpenSearchParameters(this.DefaultMimeType);

            UriBuilder searchUrl = new UriBuilder(context.BaseUrl);
            searchUrl.Path += string.Format("/job/wps/{0}/products/search", Wpsjob.Identifier);
            NameValueCollection queryString = HttpUtility.ParseQueryString("");
            parameters.AllKeys.FirstOrDefault(k =>
            {
                queryString.Add(k, parameters[k]);
                return false;
            });

            List<OpenSearchDescriptionUrlParameter> paramdesc = OpenSearchFactory.GetDefaultParametersDescription(10);

            foreach (int code in searchExtensions.Keys)
            {

                queryString.Set("format", searchExtensions[code].Identifier);
                string[] queryStrings = Array.ConvertAll(queryString.AllKeys, key => string.Format("{0}={1}", key, queryString[key]));
                searchUrl.Query = string.Join("&", queryStrings);
                var url = new OpenSearchDescriptionUrl(searchExtensions[code].DiscoveryContentType,
                                                      searchUrl.ToString(),
                                                       "results");
                url.Parameters = paramdesc.ToArray();
                urls.Add(url);

            }
            UriBuilder descriptionUrl = new UriBuilder(context.BaseUrl);
            descriptionUrl.Path += string.Format("/job/wps/{0}/products/description", Wpsjob.Identifier);
            urls.Add(new OpenSearchDescriptionUrl("application/opensearchdescription+xml",
                                                  searchUrl.ToString(),
                                                  "self"));
            osd.Url = urls.ToArray();

            return osd;
        }


        /// <summary>
        /// Applies the result filters.
        /// </summary>
        /// <param name="request">Request.</param>
        /// <param name="osr">Osr.</param>
        /// <param name="finalContentType">Final content type.</param>
        public void ApplyResultFilters(OpenSearchRequest request, ref IOpenSearchResultCollection osr, string finalContentType = null)
        {
            if (finalContentType != null)
            {
                ReplaceEnclosureLinks(this, request.OriginalParameters, osr, finalContentType);
            }
            else {
                ReplaceEnclosureLinks(this, request.OriginalParameters, osr, osr.ContentType);
            }
        }



        /// <summary>
        /// Replaces the enclosure links.
        /// </summary>
        /// <param name="entity">Entity.</param>
        /// <param name="parameters">Parameters.</param>
        /// <param name="osr">Osr.</param>
        /// <param name="entryEnclosureLinkTemplate">Entry enclosure link template.</param>
        /// <param name="contentType">Content type.</param>
        public void ReplaceEnclosureLinks(IOpenSearchable entity, NameValueCollection parameters, IOpenSearchResultCollection osr, string contentType)
        {
            if (!string.IsNullOrEmpty(parameters["do"]))
            {

                bool strict = false, isDomain = false;
                string origin = parameters["do"];

                if (parameters["do"].StartsWith("[") && parameters["do"].EndsWith("]"))
                {
                    strict = true;
                    origin = parameters["do"].TrimStart('[').TrimEnd(']');
                }

                try
                {
                    Dns.GetHostAddresses(origin);
                    isDomain = true;
                }
                catch { }



                foreach (IOpenSearchResultItem item in osr.Items)
                {

                    foreach (var enclosureLink in item.Links.Where(l => l.RelationshipType == "enclosure").ToArray())
                    {
                        if (origin.Contains("terradue") || isDomain)
                        {
                            SyndicationLink newEnclosureLink = DataGatewayFactory.SubstituteEnclosure(enclosureLink, openSearchable, item);
                            if (newEnclosureLink != null)
                            {
                                item.Links.Insert(item.Links.IndexOf(enclosureLink), newEnclosureLink);
                                item.Links.Remove(enclosureLink);
                            }
                            else if (strict)
                                item.Links.Remove(enclosureLink);
                        }

                    }


                    item.ElementExtensions = new SyndicationElementExtensionCollection(
                        item.ElementExtensions.Select<SyndicationElementExtension, SyndicationElementExtension>(ext =>
                    {
                        if (ext.OuterName != "offering" || ext.OuterNamespace != "http://www.opengis.net/owc/1.0")
                            return ext;

                        var offering = (OwcOffering)OwcContextHelper.OwcOfferingSerializer.Deserialize(ext.GetReader());

                        if (offering.Contents != null)
                        {
                            foreach (var content in offering.Contents)
                            {
                                if (content.Url != null)
                                {
                                    var newUrl = DataGatewayFactory.SubstituteUrlApi(content.Url, openSearchable, item);
                                    if (newUrl != null)
                                        content.Url = newUrl;
                                }
                            }
                        }

                        return new SyndicationElementExtension(offering.CreateReader());
                    }));
                }
            }
        }



        public QuerySettings GetQuerySettings(OpenSearchEngine ose)
        {
            return openSearchable.GetQuerySettings(ose);
        }

        public OpenSearchRequest Create(QuerySettings querySettings, NameValueCollection parameters)
        {
            return openSearchable.Create(querySettings, parameters);
        }

        public NameValueCollection GetOpenSearchParameters(string mimeType)
        {
            var parameters = openSearchable.GetOpenSearchParameters(mimeType);
            parameters.Set("do", "{t2:downloadOrigin?}");
            return parameters;
        }
    }
}
