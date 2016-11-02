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

            foreach (int code in searchExtensions.Keys)
            {

                queryString.Set("format", searchExtensions[code].Identifier);
                string[] queryStrings = Array.ConvertAll(queryString.AllKeys, key => string.Format("{0}={1}", key, queryString[key]));
                searchUrl.Query = string.Join("&", queryStrings);
                urls.Add(new OpenSearchDescriptionUrl(searchExtensions[code].DiscoveryContentType,
                                                      searchUrl.ToString(),
                                                      "results"));

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
            var parameters = new NameValueCollection ();
            request.Parameters.AllKeys.FirstOrDefault (k => {
                parameters.Add (k, request.Parameters [k]);
                return false;
            });
            request.OriginalParameters.AllKeys.FirstOrDefault (k => {
                parameters.Add (k, request.OriginalParameters [k]);
                return false;
            });


            if (finalContentType != null)
            {
                ReplaceEnclosureLinks(this, parameters, osr, TerradueEntryEnclosureLinkTemplate, finalContentType);
            }
            else {
                ReplaceEnclosureLinks(this, parameters, osr, TerradueEntryEnclosureLinkTemplate, osr.ContentType);
            }
        }

        public virtual SyndicationLink TerradueEntryEnclosureLinkTemplate(SyndicationLink link, IOpenSearchResultItem item, string contentType)
        {
            return DataGatewayFactory.SubstituteEnclosure(link, openSearchable, item);
        }

        /// <summary>
        /// Replaces the enclosure links.
        /// </summary>
        /// <param name="entity">Entity.</param>
        /// <param name="parameters">Parameters.</param>
        /// <param name="osr">Osr.</param>
        /// <param name="entryEnclosureLinkTemplate">Entry enclosure link template.</param>
        /// <param name="contentType">Content type.</param>
        public void ReplaceEnclosureLinks(IOpenSearchable entity, NameValueCollection parameters, IOpenSearchResultCollection osr, Func<SyndicationLink, IOpenSearchResultItem, string, SyndicationLink> entryEnclosureLinkTemplate, string contentType)
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

                SyndicationLink[] array = (from l in osr.Links
                                           where l.RelationshipType == "enclosure"
                                           select l).ToArray<SyndicationLink>();
                SyndicationLink[] array2 = array;
                if (strict)
                {
                    for (int i = 0; i < array2.Length; i++)
                    {
                        SyndicationLink item = array2[i];
                        osr.Links.Remove(item);
                    }
                }



                foreach (IOpenSearchResultItem item in osr.Items)
                {
                    array = (from l in item.Links
                             where l.RelationshipType == "enclosure"
                             select l).ToArray<SyndicationLink>();
                    SyndicationLink[] array3 = array;
                    if (strict)
                    {
                        for (int j = 0; j < array3.Length; j++)
                        {
                            SyndicationLink previous = array3[j];
                            item.Links.Remove(previous);
                        }
                    }

                    var tmpLinks = new List<SyndicationLink> ();
                    tmpLinks.AddRange(item.Links);
                    foreach (var enclosureLink in tmpLinks.Where(l => l.RelationshipType == "enclosure"))
                    {
                        if (origin.Contains("terradue") || isDomain)
                        {
                            SyndicationLink enclosureUrl = entryEnclosureLinkTemplate(enclosureLink, item, contentType);
                            if (enclosureUrl != null)
                            {
                                item.Links.Insert(0, enclosureUrl);
                            }
                        }

                    }
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
            var parameters =  openSearchable.GetOpenSearchParameters(mimeType);
            parameters.Set("do", "{t2:downloadOrigin?}");
            return parameters;
        }
    }
}
