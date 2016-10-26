using System;
using System.Collections.Specialized;
using System.Linq;
using System.Web;
using Terradue.OpenSearch;
using Terradue.OpenSearch.Engine;
using Terradue.OpenSearch.Request;
using Terradue.OpenSearch.Schema;

namespace Terradue.Tep
{
    public class WpsJobOpenSearchable : GenericOpenSearchable{

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Terradue.Tep.WpsJobOpenSearchable"/> class.
        /// </summary>
        /// <param name="url">URL.</param>
        /// <param name="ose">Ose.</param>
        public WpsJobOpenSearchable(OpenSearchUrl newurl, OpenSearchEngine ose) : base(newurl, ose) {

            //TEMPORARY: if T2 sandbox (/sbws), use new path (/sbws/production)
            var r = new System.Text.RegularExpressions.Regex (@"^\/sbws\/wps\/(?<workflow>[a-zA-Z0-9_-]+)\/(?<runid>[a-zA-Z0-9_-]+)\/results\/description");
            var m = r.Match (this.url.AbsolutePath);
            if (m.Success) {
                var workflow = m.Result ("${workflow}");
                var runid = m.Result ("${runid}");
                var replacement = string.Format ("/sbws/production/run/{0}/{1}/products/search", workflow, runid);
                this.url = new OpenSearchUrl (this.url.AbsoluteUri.Replace (this.url.AbsolutePath, replacement));
            }

            //If url does not contains the Download origin, we add it
            if (!this.url.AbsoluteUri.Contains ("do=")){
                var replacement = new UriBuilder (this.url.AbsoluteUri);
                if (!string.IsNullOrEmpty (replacement.Query)) replacement.Query += "&";
                replacement.Query += "do=terradue";
                this.url = new OpenSearchUrl (replacement.Uri.AbsoluteUri);
            }

            this.osd = ose.AutoDiscoverFromQueryUrl (this.url);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Terradue.Tep.WpsJobOpenSearchable"/> class.
        /// </summary>
        /// <param name="osd">Osd.</param>
        /// <param name="ose">Ose.</param>
        public WpsJobOpenSearchable(OpenSearchDescription osd, OpenSearchEngine ose) : base(osd, ose) {}


        public new OpenSearchDescription GetOpenSearchDescription () {
            var osdd = base.GetOpenSearchDescription ();

            foreach (var link in osdd.Url) {
                var baseUri = new UriBuilder (HttpContext.Current.Request.Url.AbsoluteUri);
                baseUri.Query = "";

                if (link.Template.Contains ("?")) {
                    link.Template = baseUri.Uri.AbsoluteUri + link.Template.Substring (link.Template.IndexOf ("?"));
                    if (link.Relation.Equals ("results")) link.Template = link.Template.Replace ("/description", "/search");
                } else {
                    link.Template = baseUri.Uri.AbsoluteUri;
                }
            }

            return osdd;
        }

    }
}
