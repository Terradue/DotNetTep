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
using System.Linq;
using System.Runtime.Serialization;
using System.Web;
using Terradue.OpenSearch;
using Terradue.OpenSearch.Result;
using Terradue.OpenSearch.Schema;
using Terradue.Portal;
using Terradue.ServiceModel.Ogc.Owc.AtomEncoding;
using Terradue.ServiceModel.Syndication;

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

        public static readonly int KINDRESOURCESETAPPS = 2;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Terradue.Tep.ThematicApplicationSet"/> class.
        /// </summary>
        /// <param name="context">Context.</param>
        public ThematicApplication (IfyContext context) : base (context){
            this.Kind = KINDRESOURCESETAPPS;
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

        public new void Store () {
            this.Kind = KINDRESOURCESETAPPS;
            base.Store ();
        }

        /// <summary>
        /// Gets the search base URL.
        /// </summary>
        /// <returns>The search base URL.</returns>
        /// <param name="mimeType">MIME type.</param>
        public override OpenSearchUrl GetSearchBaseUrl (string mimeType)
        {
            return new OpenSearchUrl(string.Format("{0}/" + entityType.Keyword + "/search", context.BaseUrl));
        }

        /// <summary>
        /// Gets the description base URL.
        /// </summary>
        /// <returns>The description base URL.</returns>
        public override OpenSearchUrl GetDescriptionBaseUrl ()
        {
            return new OpenSearchUrl (string.Format ("{0}/" + entityType.Keyword + "/description", context.BaseUrl));
        }

        /// <summary>
        /// Gets the local open search description.
        /// </summary>
        /// <returns>The local open search description.</returns>
        public override OpenSearchDescription GetLocalOpenSearchDescription ()
        {
            OpenSearchDescription osd = base.GetOpenSearchDescription ();

            List<OpenSearchDescriptionUrl> urls = new List<OpenSearchDescriptionUrl> ();
            UriBuilder urlb = new UriBuilder (GetDescriptionBaseUrl ());
            OpenSearchDescriptionUrl url = new OpenSearchDescriptionUrl ("application/opensearchdescription+xml", urlb.ToString (), "self", osd.ExtraNamespace);
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
                url = new OpenSearchDescriptionUrl (osee.DiscoveryContentType, urlb.ToString (), "search", osd.ExtraNamespace);
                urls.Add (url);
            }

            osd.Url = urls.ToArray ();

            return osd;
        }
    }

    public class ThematicAppFactory {
        public static List<CollectionOverview> GetDataCollectionOverview(IfyContext context,IOpenSearchResultItem item){
            var appUid = item.Identifier.Trim();
            var appTitle = item.Title != null ? item.Title.Text.Trim() : appUid;
            var appIconLink = item.Links.FirstOrDefault(l => l.RelationshipType == "icon");
            string appIcon = "";
            if (appIconLink != null) appIcon = appIconLink.Uri.AbsoluteUri;
            var offerings = item.ElementExtensions.ReadElementExtensions<OwcOffering>("offering", OwcNamespaces.Owc, new System.Xml.Serialization.XmlSerializer(typeof(OwcOffering)));
            var offering = offerings.First(p => p.Code == "http://www.terradue.com/spec/owc/1.0/req/atom/opensearch");
            return GetDataCollectionOverview(context, offering, appUid, appTitle, appIcon);
        }
        public static List<CollectionOverview> GetDataCollectionOverview(IfyContext context,OwsContextAtomEntry entry){
            var appUid = GetExtensionFromFeed(entry,"identifier", "http://purl.org/dc/elements/1.1/");
            var appTitle = entry.Title != null ? entry.Title.Text.Trim() : appUid;
            var appIconLink = entry.Links.FirstOrDefault(l => l.RelationshipType == "icon");
            string appIcon = "";
            if (appIconLink != null) appIcon = appIconLink.Uri.AbsoluteUri;
            var offerings = entry.ElementExtensions.ReadElementExtensions<OwcOffering>("offering", OwcNamespaces.Owc, new System.Xml.Serialization.XmlSerializer(typeof(OwcOffering)));
            var offering = offerings.First(p => p.Code == "http://www.terradue.com/spec/owc/1.0/req/atom/opensearch");
            return GetDataCollectionOverview(context, offering, appUid, appTitle, appIcon);
        }
        private static List<CollectionOverview> GetDataCollectionOverview(IfyContext context, OwcOffering offering, string appUid, string appTitle, string appIcon) {
            List<CollectionOverview> collectionOverviews = new List<CollectionOverview>();
            if (offering != null) {
                if (offering.Operations != null) {
                    foreach (var ops in offering.Operations) {
                        if (ops.Any == null || ops.Any[0] == null || ops.Any[0].InnerText == null) continue;

                        if (ops.Code == "ListSeries") {
                            EntityList<Collection> collections = new EntityList<Collection>(context);
                            Terradue.OpenSearch.Engine.OpenSearchEngine ose = MasterCatalogue.OpenSearchEngine;
                            var uri = new Uri(ops.Href);
                            var nvc = HttpUtility.ParseQueryString(uri.Query);
                            var resultColl = ose.Query(collections, nvc);
                            foreach (var itemColl in resultColl.Items) {
                                var itemCollIdTrim = itemColl.Identifier.Trim().Replace(" ", "");
                                var any = ops.Any[0].InnerText.Trim();
                                var anytrim = any.Replace(" ", "").Replace("*", itemCollIdTrim);
                                any = any.Replace("*", itemColl.Identifier);
                                var url = new UriBuilder(context.GetConfigValue("BaseUrl"));
                                url.Path = "/geobrowser/";
                                url.Query = "id=" + appUid + "#!context=" + System.Web.HttpUtility.UrlEncode(anytrim);
                                if (any != string.Empty) {
                                    collectionOverviews.Add(new CollectionOverview {
                                        Name = any,
                                        App = new AppOverview {
                                            Icon = appIcon ?? "",
                                            Title = appTitle,
                                            Uid = appUid
                                        },
                                        Url = url.Uri.AbsoluteUri
                                    });
                                }
                            }
                        } else {
                            var any = ops.Any[0].InnerText.Trim();
                            var anytrim = any.Replace(" ", "");
                            var url = new UriBuilder(context.GetConfigValue("BaseUrl"));
                            url.Path = "/geobrowser/";
                            url.Query = "id=" + appUid + "#!context=" + System.Web.HttpUtility.UrlEncode(anytrim);
                            if (any != string.Empty) {
                                collectionOverviews.Add(new CollectionOverview {
                                    Name = any,
                                    App = new AppOverview {
                                        Icon = appIcon ?? "",
                                        Title = appTitle,
                                        Uid = appUid
                                    },
                                    Url = url.Uri.AbsoluteUri
                                });
                            }
                        }
                    }
                }
            }
            return collectionOverviews;
        }

        public static string GetExtensionFromFeed(OwsContextAtomEntry entry, string extension, string ns){
            string val = "";
            var vals = entry.ElementExtensions.ReadElementExtensions<string>(extension, ns);
            if (vals.Count > 0) val = vals[0];
            return val;
        }

        public static List<WpsServiceOverview> GetWpsServiceOverviews(IfyContext context, IOpenSearchResultItem item){
            var appUid = item.Identifier.Trim();
            var appTitle = item.Title != null ? item.Title.Text.Trim() : appUid;
            var appIconLink = item.Links.FirstOrDefault(l => l.RelationshipType == "icon");
            string appIcon = "";
            if (appIconLink != null) appIcon = appIconLink.Uri.AbsoluteUri;
            var offerings = item.ElementExtensions.ReadElementExtensions<OwcOffering>("offering", OwcNamespaces.Owc, new System.Xml.Serialization.XmlSerializer(typeof(OwcOffering)));
            var offering = offerings.First(p => p.Code == "http://www.opengis.net/spec/owc/1.0/req/atom/wps");
            return GetWpsServiceOverview(context, offering, appUid, appTitle, appIcon);
        }
        public static List<WpsServiceOverview> GetWpsServiceOverviews(IfyContext context, OwsContextAtomEntry entry){
            var appUid = GetExtensionFromFeed(entry,"identifier", "http://purl.org/dc/elements/1.1/");
            var appTitle = entry.Title != null ? entry.Title.Text.Trim() : appUid;
            var appIconLink = entry.Links.FirstOrDefault(l => l.RelationshipType == "icon");
            string appIcon = "";
            if (appIconLink != null) appIcon = appIconLink.Uri.AbsoluteUri;
            var offerings = entry.ElementExtensions.ReadElementExtensions<OwcOffering>("offering", OwcNamespaces.Owc, new System.Xml.Serialization.XmlSerializer(typeof(OwcOffering)));
            var offering = offerings.First(p => p.Code == "http://www.opengis.net/spec/owc/1.0/req/atom/wps");
            return GetWpsServiceOverview(context, offering, appUid, appTitle, appIcon);
        }

        private static List<WpsServiceOverview> GetWpsServiceOverview(IfyContext context, OwcOffering offering, string appUid, string appTitle, string appIcon) {
            List<WpsServiceOverview> wpsOverviews = new List<WpsServiceOverview>();
            if (offering != null) {
                if (offering.Operations != null) {
                    foreach (var ops in offering.Operations) {
                        if (ops.Code == "ListProcess") {
                            var href = ops.Href;
                            //replace usernames in apps
                            try {
                                var user = UserTep.FromId(context, context.UserId);
                                href = href.Replace("${USERNAME}", user.Username);
                                href = href.Replace("${T2USERNAME}", user.TerradueCloudUsername);
                                href = href.Replace("${T2APIKEY}", user.GetSessionApiKey());
                            } catch (Exception e) {
                                context.LogError(context, e.Message);
                            }
                            var uri = new Uri(href.Replace("file://", context.BaseUrl));
                            var nvc = HttpUtility.ParseQueryString(uri.Query);
                            nvc.Set("count", "100");
                            Terradue.OpenSearch.Engine.OpenSearchEngine ose = MasterCatalogue.OpenSearchEngine;
                            var responseType = ose.GetExtensionByExtensionName("atom").GetTransformType();
                            EntityList<WpsProcessOffering> wpsProcesses = new EntityList<WpsProcessOffering>(context);
                            wpsProcesses.SetFilter("Available", "true");
                            wpsProcesses.OpenSearchEngine = ose;
                            wpsProcesses.Identifier = string.Format("servicewps-{0}", context.Username);

                            CloudWpsFactory wpsOneProcesses = new CloudWpsFactory(context);
                            wpsOneProcesses.OpenSearchEngine = ose;

                            wpsProcesses.Identifier = "service/wps";
                            var entities = new List<IOpenSearchable> { wpsProcesses, wpsOneProcesses };

                            var settings = MasterCatalogue.OpenSearchFactorySettings;
                            MultiGenericOpenSearchable multiOSE = new MultiGenericOpenSearchable(entities, settings);
                            IOpenSearchResultCollection osr = ose.Query(multiOSE, nvc, responseType);

                            OpenSearchFactory.ReplaceOpenSearchDescriptionLinks(wpsProcesses, osr);

                            foreach (var itemWps in osr.Items) {
                                string uid = "";
                                var identifiers = itemWps.ElementExtensions.ReadElementExtensions<string>("identifier", "http://purl.org/dc/elements/1.1/");
                                if (identifiers.Count > 0) uid = identifiers[0];
                                string description = "";
                                if(itemWps.Content is TextSyndicationContent){
                                    var content = itemWps.Content as TextSyndicationContent;
                                    description = content.Text;
                                }
                                string version = "";
                                var versions = itemWps.ElementExtensions.ReadElementExtensions<string>("version", "https://www.terradue.com/");
                                if (versions.Count > 0) version = versions[0];
                                string publisher = "";
                                var publishers = itemWps.ElementExtensions.ReadElementExtensions<string>("publisher", "http://purl.org/dc/elements/1.1/");
                                if (publishers.Count > 0) publisher = publishers[0];
                                var serviceUrl = new UriBuilder(context.GetConfigValue("BaseUrl"));
                                serviceUrl.Path = "/t2api/service/wps/search";
                                serviceUrl.Query = "id=" + uid;
                                var url = new UriBuilder(context.GetConfigValue("BaseUrl"));
                                url.Path = "t2api/share";
                                url.Query = "url=" + HttpUtility.UrlEncode(serviceUrl.Uri.AbsoluteUri) + "&id=" + appUid;
                                var icon = itemWps.Links.FirstOrDefault(l => l.RelationshipType == "icon");
                                //entry.Links.Add(new SyndicationLink(new Uri(this.IconUrl), "icon", null, null, 0));
                                wpsOverviews.Add(new WpsServiceOverview { 
                                    Identifier = uid,
                                    App = new AppOverview{
                                        Icon = appIcon ?? "",
                                        Title = appTitle,
                                        Uid = appUid
                                    },
                                    Name = itemWps.Title != null ? itemWps.Title.Text : uid,
                                    Description = description,
                                    Version = version,
                                    Provider = publisher,
                                    Url = url.Uri.AbsoluteUri,
                                    Icon = icon != null ? icon.Uri.AbsoluteUri : ""
                                });
                            }
                        }
                    }
                }
            }
            return wpsOverviews;
        }
    }

     [DataContract]
    public class AppOverview {
        [DataMember]
        public string Uid { get; set; }
        [DataMember]
        public string Title { get; set; }
        [DataMember]
        public string Icon { get; set; }
    }

    [DataContract]
    public class CommunityOverview {
        [DataMember]
        public string Identifier { get; set; }
        [DataMember]
        public string Title { get; set; }
        [DataMember]
        public string Icon { get; set; }        
    }
    [DataContract]
    public class CollectionOverview {
        [DataMember]
        public string Name { get; set; }
        [DataMember]
        public AppOverview App { get; set; }
        [DataMember]
        public string Url { get; set; }

        public CollectionOverview() { }
    }

    [DataContract]
    public class WpsServiceOverview {
        [DataMember]
        public string Identifier { get; set; }
        [DataMember]
        public string Name { get; set; }
        [DataMember]
        public string Description { get; set; }
        [DataMember]
        public string Version { get; set; }
        [DataMember]
        public string Provider { get; set; }
        [DataMember]
        public AppOverview App { get; set; }
        [DataMember]
        public string Url { get; set; }
        [DataMember]
        public string Icon { get; set; }

        public WpsServiceOverview() { }
    }
}

