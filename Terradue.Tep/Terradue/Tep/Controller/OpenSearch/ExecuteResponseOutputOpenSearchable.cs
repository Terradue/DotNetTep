using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Web;
using OpenGis.Wps;
using Terradue.OpenSearch;
using Terradue.OpenSearch.Engine;
using Terradue.OpenSearch.Engine.Extensions;
using Terradue.OpenSearch.Request;
using Terradue.OpenSearch.Result;
using Terradue.OpenSearch.Schema;
using Terradue.Portal;

namespace Terradue.Tep.OpenSearch
{
    class ExecuteResponseOutputOpenSearchable : IOpenSearchable
    {
        ExecuteResponse execResponse;
        readonly IfyContext context;

        public ExecuteResponseOutputOpenSearchable(ExecuteResponse execResponse, IfyContext context)
        {
            this.context = context;
            this.execResponse = execResponse;
        }

        public QuerySettings GetQuerySettings(Terradue.OpenSearch.Engine.OpenSearchEngine ose)
        {
            return new QuerySettings(this.DefaultMimeType, new AtomOpenSearchEngineExtension().ReadNative);
        }

        public Terradue.OpenSearch.Request.OpenSearchRequest Create(QuerySettings querySettings, System.Collections.Specialized.NameValueCollection parameters)
        {
            UriBuilder builder = new UriBuilder("http://" + System.Environment.MachineName);
            string[] queryString = Array.ConvertAll(parameters.AllKeys, key => string.Format("{0}={1}", key, parameters[key]));
            builder.Query = string.Join("&", queryString);
            AtomOpenSearchRequest request = new AtomOpenSearchRequest(new OpenSearchUrl(builder.ToString()), GenerateSyndicationFeed);

            return request;
        }

        public Terradue.OpenSearch.Schema.OpenSearchDescription GetOpenSearchDescription()
        {
            OpenSearchDescription osd = new OpenSearchDescription();

            osd.ShortName = "WPSOutput";
            osd.Attribution = "Terradue";
            osd.Contact = "info@terradue.com";
            osd.Developer = "Terradue GeoSpatial Development Team";
            osd.SyndicationRight = "open";
            osd.AdultContent = "false";
            osd.Language = "en-us";
            osd.OutputEncoding = "UTF-8";
            osd.InputEncoding = "UTF-8";
            osd.Description = "This Search Service performs queries in the WPS ExecuteResponse Output elements. There are several URL templates that return the results in different formats. " +
                                            "This search service is in accordance with the OGC 10-032r3 specification.";

            List<OpenSearchDescriptionUrl> urls = new List<OpenSearchDescriptionUrl>();

            NameValueCollection parameters = GetOpenSearchParameters(this.DefaultMimeType);

            UriBuilder searchUrl = new UriBuilder(context.BaseUrl);
            searchUrl.Path += "/wps/job/output/search";
            NameValueCollection queryString = HttpUtility.ParseQueryString("");
            parameters.AllKeys.FirstOrDefault(k =>
            {
                queryString.Add(k, parameters[k]);
                return false;
            });


            queryString.Set("format", "atom");
            string[] queryStrings = Array.ConvertAll(queryString.AllKeys, key => string.Format("{0}={1}", key, queryString[key]));
            searchUrl.Query = string.Join("&", queryStrings);
            urls.Add(new OpenSearchDescriptionUrl("application/atom+xml",
                                                  searchUrl.ToString(),
                                                  "results"));

            UriBuilder descriptionUrl = new UriBuilder(context.BaseUrl);
            descriptionUrl.Path += "/description";
            urls.Add(new OpenSearchDescriptionUrl("application/opensearchdescription+xml",
                                                  searchUrl.ToString(),
                                                  "self"));
            osd.Url = urls.ToArray();

            return osd;
        }

        public System.Collections.Specialized.NameValueCollection GetOpenSearchParameters(string mimeType)
        {
            NameValueCollection nvc = OpenSearchFactory.GetBaseOpenSearchParameter();
            nvc.Set("uid", "{geo:uid?}");
            return nvc;
        }

        public string Identifier
        {
            get
            {
                return "wpsOutput";
            }
        }

        public long TotalResults
        {
            get
            {
                return execResponse.ProcessOutputs.Count();
            }
        }

        public string DefaultMimeType
        {
            get
            {
                return "application/atom+xml";
            }
        }

        private AtomFeed GenerateSyndicationFeed(NameValueCollection parameters)
        {

            AtomFeed feed = new AtomFeed("Discovery feed for WPS output",
                                         "This OpenSearch Service allows the discovery of the different items which are part of the local CIOP results. " +
                                         "This search service is in accordance with the OGC 10-032r3 specification.",
                                         new Uri(context.BaseUrl), context.BaseUrl.ToString(), DateTimeOffset.UtcNow);



            feed.Generator = "Terradue OpenSearch WPS Output generator";

            List<AtomItem> items = new List<AtomItem>();

            var pds = new Terradue.OpenSearch.Request.PaginatedList<ExecuteResponseOutput>();



            if (execResponse.ProcessOutputs != null || execResponse.ProcessOutputs.Count() > 0)
            {

                List<OutputDataType> outputs = new List<OutputDataType>();

                if (!string.IsNullOrEmpty(parameters["q"]))
                {
                    string q = parameters["q"];
                    outputs = execResponse.ProcessOutputs.Where(p => p.Abstract.Value.ToLower().Contains(q.ToLower()) || (p.Identifier.Value.ToLower().Contains(q.ToLower()))).ToList();
                }

                if (!string.IsNullOrEmpty(parameters["uid"]))
                    outputs = outputs.Where(p => p.Identifier.Value == parameters["uid"]).ToList();
                

                pds.AddRange(outputs.Select(o => new ExecuteResponseOutput(o, context)));
            }

            pds.StartIndex = 1;
            if (!string.IsNullOrEmpty(parameters["startIndex"])) pds.StartIndex = int.Parse(parameters["startIndex"]);

            pds.PageNo = 1;
            if (!string.IsNullOrEmpty(parameters["startPage"])) pds.PageNo = int.Parse(parameters["startPage"]);

            pds.PageSize = 20;
            if (!string.IsNullOrEmpty(parameters["count"])) pds.PageSize = int.Parse(parameters["count"]);

            if (this.Identifier != null) feed.ElementExtensions.Add("identifier", "http://purl.org/dc/elements/1.1/", this.Identifier);

            foreach (var o in pds.GetCurrentPage())
            {
                AtomItem item = (o as IAtomizable).ToAtomItem(parameters);
                if (item != null) items.Add(item);
            }

            feed.Items = items;

            return feed;
        }

        public bool CanCache
        {
            get
            {
                return false;
            }
        }

        public void ApplyResultFilters(OpenSearchRequest request, ref IOpenSearchResultCollection osr, string finalContentType)
        {
        }
    }
}