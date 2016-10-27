using System;

/*! 
\defgroup TepApplication Thematic Application
@{

This component manages the TEP applications

Thematic Application brings a simple way to define an application of a specific aspect of the thematic.
It specifies together the form of the application, its features such as the map and the layers, its data and services.

Thematic Application over OWS data model
----------------------------------------

This component uses extensively \ref OWSContext model to represent the dataset collections, the services, maps, features layers and combine them together defining therefore:

- data AOI and limits
- data constraints with the processing services
- processing services predefintion (default values, massive processing extent...)
- map backgroungs, feature layers and other functions
- special functions such as benchmarking or special widgets

\xrefitem dep "Dependencies" "Dependencies" delegates \ref TepData for the data management in the application with the definition of the collection and data packages references.

\xrefitem dep "Dependencies" "Dependencies" delegates \ref TepService for the processing service management in the application with the defintion of the service references .

\ingroup Tep

@}
*/
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Web;
using Terradue.OpenSearch;
using Terradue.OpenSearch.Schema;
using Terradue.Portal;

namespace Terradue.Tep
{

    /// <summary>
    /// Thematic Application
    /// </summary>
    /// <description>
    /// Thematic Application object represent a set of features, of \ref Collection and \ref WpsProcessOffering to make a specific
    /// application dedicated to scope or a purpose
    /// </description>
    /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation" 
    /// \ingroup TepApplication
    public class ThematicApplication : DataPackage
    {

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Terradue.Tep.ThematicApplicationSet"/> class.
        /// </summary>
        /// <param name="context">Context.</param>
        public ThematicApplication (IfyContext context) : base (context)
        {
        }

        /// <summary>
        /// Froms the identifier.
        /// </summary>
        /// <returns>The identifier.</returns>
        /// <param name="context">Context.</param>
        /// <param name="identifier">Identifier.</param>
        public static new ThematicApplication FromIdentifier (IfyContext context, string identifier)
        {
            ThematicApplication result = new ThematicApplication (context);
            result.Identifier = identifier;
            try {
                result.Load ();
            } catch (Exception e) {
                throw e;
            }
            return result;
        }

        /// <summary>
        /// Gets the search base URL.
        /// </summary>
        /// <returns>The search base URL.</returns>
        /// <param name="mimeType">MIME type.</param>
        public override OpenSearchUrl GetSearchBaseUrl (string mimeType)
        {
            return GetSearchBaseUrl ();
        }

        public new OpenSearchUrl GetSearchBaseUrl ()
        {
            return new OpenSearchUrl (string.Format ("{0}/" + entityType.Keyword + "/{1}/search", context.BaseUrl, Identifier));
        }

        /// <summary>
        /// Gets the description base URL.
        /// </summary>
        /// <returns>The description base URL.</returns>
        public override OpenSearchUrl GetDescriptionBaseUrl ()
        {
            return new OpenSearchUrl (string.Format ("{0}/" + entityType.Keyword + "/{1}/description", context.BaseUrl, Identifier));
        }

        /// <summary>
        /// Gets the local open search description.
        /// </summary>
        /// <returns>The local open search description.</returns>
        public new OpenSearchDescription GetLocalOpenSearchDescription ()
        {
            OpenSearchDescription osd = base.GetOpenSearchDescription ();

            List<OpenSearchDescriptionUrl> urls = new List<OpenSearchDescriptionUrl> ();
            UriBuilder urlb = new UriBuilder (GetDescriptionBaseUrl ());
            OpenSearchDescriptionUrl url = new OpenSearchDescriptionUrl ("application/opensearchdescription+xml", urlb.ToString (), "self");
            urls.Add (url);

            NameValueCollection query = HttpUtility.ParseQueryString (urlb.Query);

            urlb = new UriBuilder (GetSearchBaseUrl ("application/atom+xml"));
            query = GetOpenSearchParameters ("application/atom+xml");
            NameValueCollection nvc = HttpUtility.ParseQueryString (urlb.Query);
            foreach (var key in nvc.AllKeys) {
                query.Set (key, nvc [key]);
            }

            foreach (var osee in OpenSearchEngine.Extensions.Values) {
                query.Set ("format", osee.Identifier);
                string [] queryString = Array.ConvertAll (query.AllKeys, key => string.Format ("{0}={1}", key, query [key]));
                urlb.Query = string.Join ("&", queryString);
                url = new OpenSearchDescriptionUrl (osee.DiscoveryContentType, urlb.ToString (), "search");
                urls.Add (url);
            }

            osd.Url = urls.ToArray ();

            return osd;
        }

        public new NameValueCollection GetOpenSearchParameters (string mimeType)
        {
            if (mimeType != "application/atom+xml") return null;
            var parameters = OpenSearchFactory.MergeOpenSearchParameters (GetOpenSearchableArray (), mimeType);
            return parameters;
        }
    }
}

