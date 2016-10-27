using System;
using System.Collections;
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

namespace Terradue.Tep
{
    public class WpsJobOpenSearchable : GenericOpenSearchable{

        private WpsJob Wpsjob;
        private string Index;
        private string Workflow;
        private string RunId;
        private bool IsOpensearchable;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="T:Terradue.Tep.WpsJobOpenSearchable"/> class.
        /// </summary>
        /// <param name="url">URL.</param>
        /// <param name="ose">Ose.</param>
        public WpsJobOpenSearchable(OpenSearchUrl newurl, OpenSearchEngine ose, WpsJob job) : base(newurl, ose) {

            this.Wpsjob = job;

            //check if the url ends with /search or /description
            //if so it can be queried using opensearch, otherise it means that it points to an AtomFeed file
            if (this.url.AbsolutePath.EndsWith ("/search") || this.url.AbsolutePath.EndsWith ("/description")) {
                IsOpensearchable = true;

                //TEMPORARY: if T2 sandbox (/sbws), use new path (/sbws/production)
                var r = new System.Text.RegularExpressions.Regex (@"^\/sbws\/wps\/(?<workflow>[a-zA-Z0-9_\-]+)\/(?<runid>[a-zA-Z0-9_\-]+)\/results\/description");
                var m = r.Match (this.url.AbsolutePath);
                if (m.Success) {
                    Workflow = m.Result ("${workflow}");
                    RunId = m.Result ("${runid}");
                    var replacement = string.Format ("/sbws/production/run/{0}/{1}/products/description", Workflow, RunId);
                    this.url = new OpenSearchUrl (this.url.AbsoluteUri.Replace (this.url.AbsolutePath, replacement));
                }

                //Get hostname of the run VM
                r = new System.Text.RegularExpressions.Regex (@"^https?:\/\/(?<hostname>[a-zA-Z0-9_\-\.]+)\/sbws\/production\/run\/(?<workflow>[a-zA-Z0-9_\-]+)\/(?<runid>[a-zA-Z0-9_\-]+)\/products\/description");
                m = r.Match (this.url.AbsoluteUri);
                if (m.Success) {
                    Index = m.Result ("${hostname}");
                    Workflow = m.Result ("${workflow}");
                    RunId = m.Result ("${runid}");
                }

            } else { 
                IsOpensearchable = false;
            }

            this.osd = ose.AutoDiscoverFromQueryUrl (this.url);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Terradue.Tep.WpsJobOpenSearchable"/> class.
        /// </summary>
        /// <param name="osd">Osd.</param>
        /// <param name="ose">Ose.</param>
        public WpsJobOpenSearchable(OpenSearchDescription osd, OpenSearchEngine ose) : base(osd, ose) {}

        public NameValueCollection GetParameters () {
            return this.url.SearchAttributes;
        }


        public new OpenSearchDescription GetOpenSearchDescription () {
            if (!IsOpensearchable) return GetProxiedOpenSearchDescription ();

            var osdd = base.GetOpenSearchDescription ();

            foreach (var link in osdd.Url) {
                var baseUri = new UriBuilder (HttpContext.Current.Request.Url.AbsoluteUri);
                baseUri.Query = "";

                if (link.Relation.Equals ("results")) { 
                    link.Template = link.Template.Replace ("/description", "/search");
                    //add do if not present
                    if (!link.Template.Contains ("do=")) link.Template += (link.Template.Contains ("?") ? "&" : "?") + "do={t2:downloadOrigin?}";
                }

                if (link.Template.Contains ("?")) {
                    link.Template = baseUri.Uri.AbsoluteUri + link.Template.Substring (link.Template.IndexOf ("?"));
                    if (link.Relation.Equals ("results")) link.Template = link.Template.Replace ("/description", "/search");
                } else {
                    link.Template = baseUri.Uri.AbsoluteUri;
                }
            }

            return osdd;
        }

        private OpenSearchDescription GetProxiedOpenSearchDescription ()
        {

            OpenSearchDescription OSDD = new OpenSearchDescription ();

            OSDD.ShortName = string.IsNullOrEmpty (Wpsjob.Name) ? Wpsjob.Identifier : Wpsjob.Name;
            OSDD.Attribution = "European Space Agency";
            OSDD.Contact = "info@esa.int";
            OSDD.Developer = "Terradue GeoSpatial Development Team";
            OSDD.SyndicationRight = "open";
            OSDD.AdultContent = "false";
            OSDD.Language = "en-us";
            OSDD.OutputEncoding = "UTF-8";
            OSDD.InputEncoding = "UTF-8";
            OSDD.Description = "This Search Service performs queries in the available results of a job process. There are several URL templates that return the results in different formats (RDF, ATOM or KML). This search service is in accordance with the OGC 10-032r3 specification.";

            // The new URL template list 
            Hashtable newUrls = new Hashtable ();
            UriBuilder urib;
            NameValueCollection query = new NameValueCollection ();
            string [] queryString;

            urib = new UriBuilder (HttpContext.Current.Request.Url.Host);
            urib.Path += "/t2api/job/wps/" + Wpsjob.Identifier + "/products/search";
            query.Add (OpenSearchFactory.GetBaseOpenSearchParameter ());

            queryString = Array.ConvertAll (query.AllKeys, key => string.Format ("{0}={1}", key, query [key]));
            urib.Query = string.Join ("&", queryString);
            newUrls.Add ("application/atom+xml", new OpenSearchDescriptionUrl ("application/atom+xml", urib.ToString (), "search"));

            query.Set ("format", "json");
            queryString = Array.ConvertAll (query.AllKeys, key => string.Format ("{0}={1}", key, query [key]));
            urib.Query = string.Join ("&", queryString);
            newUrls.Add ("application/json", new OpenSearchDescriptionUrl ("application/json", urib.ToString (), "search"));

            query.Set ("format", "html");
            queryString = Array.ConvertAll (query.AllKeys, key => string.Format ("{0}={1}", key, query [key]));
            urib.Query = string.Join ("&", queryString);
            newUrls.Add ("text/html", new OpenSearchDescriptionUrl ("application/html", urib.ToString (), "search"));

            OSDD.Url = new OpenSearchDescriptionUrl [newUrls.Count];

            newUrls.Values.CopyTo (OSDD.Url, 0);

            return OSDD;
        }


        /// <summary>
        /// Applies the result filters.
        /// </summary>
        /// <param name="request">Request.</param>
        /// <param name="osr">Osr.</param>
        /// <param name="finalContentType">Final content type.</param>
        public new void ApplyResultFilters (OpenSearchRequest request, ref IOpenSearchResultCollection osr, string finalContentType = null)
        {
            if (finalContentType != null) {
                ReplaceEnclosureLinks (this, request.Parameters, osr, EntryEnclosureLinkTemplate, finalContentType);
            } else { 
                ReplaceEnclosureLinks (this, request.Parameters, osr, EntryEnclosureLinkTemplate, osr.ContentType);
            }
        }

        public void ApplyResultFilters (NameValueCollection parameters, ref IOpenSearchResultCollection osr, string finalContentType = null)
        {
            if (finalContentType != null) {
                ReplaceEnclosureLinks (this, parameters, osr, EntryEnclosureLinkTemplate, finalContentType);
            } else {
                ReplaceEnclosureLinks (this, parameters, osr, EntryEnclosureLinkTemplate, osr.ContentType);
            }
        }

        public virtual SyndicationLink EntryEnclosureLinkTemplate (IOpenSearchResultItem item, string mimeType, Uri baseUrl)
        {
            UriBuilder enclosureUrl = new UriBuilder (baseUrl);

            enclosureUrl.Path += string.Format ("/{0}/workflows/{1}/runs/{2}", this.Index, this.Workflow, this.RunId);

            return new SyndicationLink (enclosureUrl.Uri, "enclosure", "Download via Data Gateway", "application/octet-stream", 0);
        }

        /// <summary>
        /// Replaces the enclosure links.
        /// </summary>
        /// <param name="entity">Entity.</param>
        /// <param name="parameters">Parameters.</param>
        /// <param name="osr">Osr.</param>
        /// <param name="entryEnclosureLinkTemplate">Entry enclosure link template.</param>
        /// <param name="contentType">Content type.</param>
        public static void ReplaceEnclosureLinks (IOpenSearchable entity, NameValueCollection parameters, IOpenSearchResultCollection osr, Func<IOpenSearchResultItem, string, Uri, SyndicationLink> entryEnclosureLinkTemplate, string contentType)
        {
            if (!string.IsNullOrEmpty (parameters ["do"])) {

                bool strict = false, isDomain = false;
                string origin = parameters ["do"];

                if (parameters ["do"].StartsWith ("[") && parameters ["do"].EndsWith ("]")) {
                    strict = true;
                    origin = parameters ["do"].TrimStart ('[').TrimEnd (']');
                }

                try {
                    Dns.GetHostAddresses (origin);
                    isDomain = true;
                } catch { }

                SyndicationLink [] array = (from l in osr.Links
                                            where l.RelationshipType == "enclosure"
                                            select l).ToArray<SyndicationLink> ();
                SyndicationLink [] array2 = array;
                if (strict) {
                    for (int i = 0; i < array2.Length; i++) {
                        SyndicationLink item = array2 [i];
                        osr.Links.Remove (item);
                    }
                }
                foreach (IOpenSearchResultItem item in osr.Items) {
                    array = (from l in item.Links
                             where l.RelationshipType == "enclosure"
                             select l).ToArray<SyndicationLink> ();
                    SyndicationLink [] array3 = array;
                    if (strict) {
                        for (int j = 0; j < array3.Length; j++) {
                            SyndicationLink previous = array3 [j];
                            item.Links.Remove (previous);
                        }
                    }

                    if (array3.Count () > 0 && origin.Contains ("terradue") || isDomain) {
                        SyndicationLink enclosureUrl = entryEnclosureLinkTemplate (item, contentType, new Uri ("https://store.terradue.com/"));
                        if (enclosureUrl != null) {
                            item.Links.Insert (0, enclosureUrl);
                        }
                    }
                }
            }
        }
    }
}
