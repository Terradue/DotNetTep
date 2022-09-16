using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Web;
using System.Xml;
using OpenGis.Wps;
using ServiceStack.Common.Web;
using ServiceStack.ServiceHost;
using Terradue.OpenSearch;
using Terradue.OpenSearch.Engine;
using Terradue.OpenSearch.Result;
using Terradue.OpenSearch.Schema;
using Terradue.Portal;
using Terradue.ServiceModel.Ogc.Owc.AtomEncoding;
using Terradue.ServiceModel.Syndication;

namespace Terradue.Tep.WebServer.Services
{

    [Route ("/proxy", "GET", Summary = "proxy an url", Notes = "")]
    public class ProxyGetUrlRequestTep
    {
        [ApiMember (Name = "url", Description = "url to be proxied", ParameterType = "query", DataType = "string", IsRequired = true)]
        public string url { get; set; }
    }

    [Route ("/proxy/wps/{jobid}/description", "GET", Summary = "proxy a wps result description", Notes = "")]
    public class ProxyWpsJobDescriptionRequestTep
    {
        [ApiMember (Name = "jobid", Description = "id of gpod job", ParameterType = "query", DataType = "string", IsRequired = true)]
        public string jobid { get; set; }
    }

    [Route ("/proxy/wps/{jobid}/search", "GET", Summary = "proxy a wps result search", Notes = "")]
    public class ProxyWpsJobSearchRequestTep
    {
        [ApiMember (Name = "jobid", Description = "id of gpod job", ParameterType = "query", DataType = "string", IsRequired = true)]
        public string jobid { get; set; }
    }

    [Api ("Tep Terradue webserver")]
    [Restrict (EndpointAttributes.InSecure | EndpointAttributes.InternalNetworkAccess | EndpointAttributes.Json,
             EndpointAttributes.Secure | EndpointAttributes.External | EndpointAttributes.Json)]
    public class ProxyServiceTep : ServiceStack.ServiceInterface.Service
    {

        private static readonly log4net.ILog log = log4net.LogManager.GetLogger
            (System.Reflection.MethodBase.GetCurrentMethod ().DeclaringType);

        public object Get (ProxyGetUrlRequestTep request)
        {

            var uri = new UriBuilder (request.url);
            var host = uri.Host;
            if (!((host.EndsWith ("terradue.int") || host.EndsWith ("terradue.com")) || !host.Equals (HttpContext.Current.Request.Url.Host))) throw new Exception ("Non Terradue urls are not accepted");

            var context = TepWebContext.GetWebContext (PagePrivileges.EverybodyView);
            HttpResult result = null;
            context.Open ();
            context.LogInfo (this, string.Format ("/proxy GET url='{0}'", request.url));

            OpenSearchEngine ose = MasterCatalogue.OpenSearchEngine;
            var settings = MasterCatalogue.OpenSearchFactorySettings;

            if (uri.Path.EndsWith ("/description") || uri.Path.EndsWith ("/OSDD")) {
                GenericOpenSearchable urlToShare = new GenericOpenSearchable (new OpenSearchUrl (request.url), settings);
                Terradue.OpenSearch.Schema.OpenSearchDescription osd = urlToShare.GetOpenSearchDescription ();
                result = new HttpResult (osd, "application/opensearchdescription+xml");
            } else {
                var e = OpenSearchFactory.FindOpenSearchable (settings, new Uri (request.url), "application/atom+xml");
                if (e.DefaultMimeType != "application/atom+xml" && !e.DefaultMimeType.Contains ("xml")) {
                    throw new InvalidOperationException ("No Url in the OpenSearch Description Document that query fully qualified model");
                }

                Type type = OpenSearchFactory.ResolveTypeFromRequest(HttpContext.Current.Request.QueryString, HttpContext.Current.Request.Headers, ose);
                var res = ose.Query (e, new NameValueCollection (), type);

                result = new HttpResult (res.SerializeToString (), res.ContentType);
            }

            context.Close ();
            return result;
        }

        public object Get (ProxyWpsJobDescriptionRequestTep request)
        {

            var context = TepWebContext.GetWebContext (PagePrivileges.EverybodyView);
            context.Open ();
            context.LogInfo (this, string.Format ("/proxy/wps/{{jobid}}/description GET jobid='{0}'", request.jobid));

            WpsJob wpsjob = WpsJob.FromIdentifier (context, request.jobid);

            context.LogDebug (this, string.Format ("Wps Proxy description for wpsjob {0}", wpsjob.Identifier));

            OpenSearchDescription osd = GetWpsOpenSearchDescription (context, request.jobid, wpsjob.Name);

            context.Close ();
            return new HttpResult (osd, "application/opensearchdescription+xml");
        }

        public object Get (ProxyWpsJobSearchRequestTep request)
        {

            var context = TepWebContext.GetWebContext (PagePrivileges.EverybodyView);
            context.Open ();
            context.LogInfo (this, string.Format ("/proxy/wps/{{jobid}}/search GET jobid='{0}'", request.jobid));

            WpsJob wpsjob = WpsJob.FromIdentifier (context, request.jobid);

            if (string.IsNullOrEmpty (wpsjob.StatusLocation)) throw new Exception ("Invalid Status Location");

            context.LogDebug (this, string.Format ("Wps Proxy search for wpsjob {0}", wpsjob.Identifier));

            HttpWebRequest executeHttpRequest;
            if (wpsjob.Provider != null)
                executeHttpRequest = wpsjob.Provider.CreateWebRequest (wpsjob.StatusLocation);
            else {
                NetworkCredential credentials = null;
                var uri = new UriBuilder (wpsjob.StatusLocation);
                if (!string.IsNullOrEmpty (uri.UserName) && !string.IsNullOrEmpty (uri.Password)) credentials = new NetworkCredential (uri.UserName, uri.Password);
                executeHttpRequest = WpsProvider.CreateWebRequest (wpsjob.StatusLocation, credentials, context.Username);
            }

            if (wpsjob.StatusLocation.Contains ("gpod.eo.esa.int")) {
                executeHttpRequest.Headers.Add ("X-UserID", context.GetConfigValue ("GpodWpsUser"));
            }
            OpenGis.Wps.ExecuteResponse execResponse = null;
            try {
                context.LogDebug (this, string.Format ("Wps proxy - exec response requested - {0}", executeHttpRequest.Address.AbsoluteUri));
                execResponse = System.Threading.Tasks.Task.Factory.FromAsync<WebResponse>(executeHttpRequest.BeginGetResponse,executeHttpRequest.EndGetResponse,null)
                .ContinueWith(task =>
                {
                    var httpResponse = (HttpWebResponse)task.Result;
                    using (var stream = httpResponse.GetResponseStream()) 
                    {
                        return (OpenGis.Wps.ExecuteResponse)new System.Xml.Serialization.XmlSerializer (typeof (OpenGis.Wps.ExecuteResponse)).Deserialize (stream);
                    }
                }).ConfigureAwait(false).GetAwaiter().GetResult();

                context.LogDebug (this, string.Format ("Wps proxy - exec response OK"));
            } catch (Exception e) {
                context.LogError (this, e.Message, e);
            }
            if (execResponse == null) throw new Exception ("Unable to get execute response from proxied job");

            execResponse.statusLocation = context.BaseUrl + "/wps/RetrieveResultServlet?id=" + wpsjob.Identifier;
            context.LogDebug (this, string.Format ("Proxy WPS - uri: " + execResponse.statusLocation));

            AtomFeed feed = new AtomFeed ();
            if (execResponse.ProcessOutputs != null) {
                foreach (OutputDataType output in execResponse.ProcessOutputs) {
                    if (output.Identifier != null && output.Identifier.Value != null && output.Identifier.Value.Equals ("result_metadata")) {
                        context.LogDebug (this, string.Format ("Wps proxy - metadata"));
                        if (output.Item is OutputReferenceType) {
                            var reference = output.Item as OutputReferenceType;
                            HttpWebRequest atomRequest;
                            if (wpsjob.Provider != null)
                                atomRequest = wpsjob.Provider.CreateWebRequest (reference.href);
                            else {
                                NetworkCredential credentials = null;
                                var uri = new UriBuilder (reference.href);
                                if (!string.IsNullOrEmpty (uri.UserName) && !string.IsNullOrEmpty (uri.Password)) credentials = new NetworkCredential (uri.UserName, uri.Password);
                                atomRequest = WpsProvider.CreateWebRequest (reference.href, credentials, context.Username);
                            }
                            feed = CreateFeedForMetadata (atomRequest);
                        } else if (output.Item is DataType) {
                            var item = ((DataType)(output.Item)).Item as ComplexDataType;
                            var reference = item.Reference as OutputReferenceType;
                            HttpWebRequest atomRequest;
                            if (wpsjob.Provider != null)
                                atomRequest = wpsjob.Provider.CreateWebRequest (reference.href);
                            else {
                                NetworkCredential credentials = null;
                                var uri = new UriBuilder (reference.href);
                                if (!string.IsNullOrEmpty (uri.UserName) && !string.IsNullOrEmpty (uri.Password)) credentials = new NetworkCredential (uri.UserName, uri.Password);
                                atomRequest = WpsProvider.CreateWebRequest (reference.href, credentials, context.Username);
                            }
                            feed = CreateFeedForMetadata (atomRequest);
                        }
                    } else {
                        if (output.Item is DataType && ((DataType)(output.Item)).Item != null) {
                            var item = ((DataType)(output.Item)).Item as ComplexDataType;
                            if (item.Any != null && item.Any [0].LocalName != null) {
                                if (item.Any [0].LocalName.Equals ("RDF")) {
                                    context.LogDebug (this, string.Format ("Wps proxy - RDF"));
                                    feed = CreateFeedForRDF (item.Any [0], request.jobid, context.BaseUrl);
                                } else if (item.Any [0].LocalName.Equals ("metalink")) {
                                    context.LogDebug (this, string.Format ("Wps proxy - metalink"));
                                    feed = CreateFeedForMetalink (item.Any [0], request.jobid, context.BaseUrl, context);
                                }
                            }
                        }
                    }
                }
            }

            /* Proxy id + add self */
            foreach (var item in feed.Items) {
                var self = context.BaseUrl + "/proxy/wps/" + wpsjob.Identifier + "/search?uid=" + HttpUtility.UrlEncode (item.Id);
                var search = context.BaseUrl + "/proxy/wps/" + wpsjob.Identifier + "/description";
                item.Id = self;
                item.Links.Add (Terradue.ServiceModel.Syndication.SyndicationLink.CreateSelfLink (new Uri (self), "application/atom+xml"));
                item.Links.Add (new Terradue.ServiceModel.Syndication.SyndicationLink (new Uri (search), "search", "OpenSearch Description link", "application/opensearchdescription+xml", 0));
            }

            feed.Id = wpsjob.StatusLocation;

            var ose = MasterCatalogue.OpenSearchEngine;
            Type responseType = OpenSearchFactory.ResolveTypeFromRequest(HttpContext.Current.Request.QueryString, HttpContext.Current.Request.Headers, ose);
            var ext = ose.GetFirstExtensionByTypeAbility (responseType);

            var osfeed = new AtomFeedOpenSearchable (feed);
            IOpenSearchResultCollection osr = ose.Query (osfeed, HttpContext.Current.Request.QueryString, responseType);

            var osrDesc = new Uri (context.BaseUrl + "/proxy/wps/" + wpsjob.Identifier + "/description");
            osr.Links.Add (new Terradue.ServiceModel.Syndication.SyndicationLink (osrDesc, "search", "OpenSearch Description link", "application/opensearchdescription+xml", 0));

            context.Close ();
            return new HttpResult (osr.SerializeToString (), osr.ContentType);

        }

        private AtomFeed CreateFeedForMetadata (HttpWebRequest atomRequest)
        {
            Atom10FeedFormatter atomFormatter = new Atom10FeedFormatter ();
            using (var atomResponseStream = new MemoryStream ()) {
                System.Threading.Tasks.Task.Factory.FromAsync<WebResponse>(atomRequest.BeginGetResponse,atomRequest.EndGetResponse,null)
                .ContinueWith(task =>
                {
                    var httpResponse = (HttpWebResponse)task.Result;
                    using (var stream = httpResponse.GetResponseStream()) 
                    {
                        stream.CopyTo (atomResponseStream);
                    }
                }).ConfigureAwait(false).GetAwaiter().GetResult();

                atomResponseStream.Seek (0, SeekOrigin.Begin);
                var sr = XmlReader.Create (atomResponseStream);
                atomFormatter.ReadFrom (sr);
                sr.Close ();
            }
            
            return new AtomFeed (atomFormatter.Feed);
        }

        private AtomFeed CreateFeedForMetalink (XmlNode any, string identifier, string baseurl, IfyContext context)
        {

            OwsContextAtomFeed feed = new OwsContextAtomFeed ();
            List<OwsContextAtomEntry> entries = new List<OwsContextAtomEntry> ();

            XmlDocument doc = new XmlDocument ();
            doc.LoadXml (any.OuterXml);
            XmlNamespaceManager xmlns = new XmlNamespaceManager (doc.NameTable);
            xmlns.AddNamespace ("ml", "http://www.metalinker.org");

            var onlineResource = doc.SelectNodes ("ml:metalink/ml:files/ml:file/ml:resources/ml:url", xmlns);
            var createddate = doc.SelectSingleNode ("ml:metalink/ml:files/ml:file/ml:releasedate", xmlns).InnerText;

            var self = baseurl + "/proxy/wps/" + identifier + "/search";
            var search = baseurl + "/proxy/wps/" + identifier + "/description";

            feed.Id = self;
            feed.Title = new Terradue.ServiceModel.Syndication.TextSyndicationContent ("Wps job results");
            feed.Date = new DateTimeInterval {
                StartDate = Convert.ToDateTime (createddate),
                EndDate = Convert.ToDateTime (createddate)
            };

            foreach (XmlNode node in onlineResource) {
                string url = node.InnerText;
                context.LogDebug (this, "Url = " + url);
                Uri remoteUri = new Uri (url);
                OwsContextAtomEntry entry = new OwsContextAtomEntry ();

                //link is an OWS context, we add it as is
                Atom10FeedFormatter atomFormatter = new Atom10FeedFormatter ();
                if (url.Contains (".atom")) {
                    NetworkCredential credentials = null;
                    var uri = new UriBuilder (url);
                    if (!string.IsNullOrEmpty (uri.UserName) && !string.IsNullOrEmpty (uri.Password)) credentials = new NetworkCredential (uri.UserName, uri.Password);
                    HttpWebRequest atomRequest = WpsProvider.CreateWebRequest (url, credentials, context.Username);
                    atomRequest.Accept = "*/*";
                    atomRequest.UserAgent = "Terradue TEP";

                    try {
                        using (var atomResponseStream = new MemoryStream ()) {
                            System.Threading.Tasks.Task.Factory.FromAsync<WebResponse>(atomRequest.BeginGetResponse,atomRequest.EndGetResponse,null)
                            .ContinueWith(task =>
                            {
                                var httpResponse = (HttpWebResponse)task.Result;
                                using (var stream = httpResponse.GetResponseStream()) 
                                {
                                    stream.CopyTo (atomResponseStream);
                                }
                            }).ConfigureAwait(false).GetAwaiter().GetResult();

                            atomResponseStream.Seek (0, SeekOrigin.Begin);
                            var sr = XmlReader.Create (atomResponseStream);
                            atomFormatter.ReadFrom (sr);
                            sr.Close ();                            
                        }
                    } catch (Exception e) {
                        context.LogError(this, e.Message, e);
                        throw e;
                    }
                    return new AtomFeed (atomFormatter.Feed);
                }
                //we build the OWS context
                entry.Id = node.InnerText.Contains ("/") ? node.InnerText.Substring (node.InnerText.LastIndexOf ("/") + 1) : node.InnerText;
                entry.Title = new Terradue.ServiceModel.Syndication.TextSyndicationContent (remoteUri.AbsoluteUri);

                entry.PublishDate = new DateTimeOffset(Convert.ToDateTime(createddate));
                entry.LastUpdatedTime = new DateTimeOffset(Convert.ToDateTime(createddate));

                entry.Date = new DateTimeInterval {
                    StartDate = Convert.ToDateTime (createddate),
                    EndDate = Convert.ToDateTime (createddate)
                };

                entry.ElementExtensions.Add ("identifier", OwcNamespaces.Dc, entry.Id);
                entry.Links.Add (Terradue.ServiceModel.Syndication.SyndicationLink.CreateMediaEnclosureLink (remoteUri, "application/octet-stream", 0));

                List<OwcOffering> offerings = new List<OwcOffering> ();
                OwcOffering offering = new OwcOffering ();
                OwcContent content = new OwcContent ();
                content.Url = remoteUri;
                string extension = remoteUri.AbsoluteUri.Substring (remoteUri.AbsoluteUri.LastIndexOf (".") + 1);

                switch (extension.ToLower ()) {
                case "gif":
                    content.Type = "image/gif";
                    offering.Code = "http://www.opengis.net/spec/owc-atom/1.0/req/gif";
                    break;
                case "gtiff":
                    content.Type = "image/tiff";
                    offering.Code = "http://www.opengis.net/spec/owc-atom/1.0/req/geotiff";
                    break;
                case "jpeg":
                    content.Type = "image/jpg";
                    offering.Code = "http://www.opengis.net/spec/owc-atom/1.0/req/jpg";
                    break;
                case "png":
                    content.Type = "image/png";
                    offering.Code = "http://www.opengis.net/spec/owc-atom/1.0/req/png";
                    break;
                case "tgz":
                    content.Type = "application/tar+gzip";
                    offering.Code = "http://www.opengis.net/spec/owc-atom/1.0/req/tgz";
                    break;
                default:
                    content.Type = "application/octet-stream";
                    offering.Code = "";
                    break;
                }

                List<OwcContent> contents = new List<OwcContent> ();
                contents.Add (content);
                offering.Contents = contents.ToArray ();
                offerings.Add (offering);
                entry.Offerings = offerings;

                entries.Add (entry);
            }
            feed.Items = entries;

            AtomFeed atomfeed = new AtomFeed (feed);
            atomfeed.Title = feed.Title;
            atomfeed.Description = feed.Description;

            var urib = new UriBuilder (baseurl);
            urib.Path += "/proxy/wps/" + identifier + "/description";
            atomfeed.Links.Add (new Terradue.ServiceModel.Syndication.SyndicationLink (urib.Uri, "search", "OpenSearch Description link", "application/opensearchdescription+xml", 0));

            return atomfeed;
        }

        private AtomFeed CreateFeedForRDF (XmlNode any, string identifier, string baseurl)
        {
            OwsContextAtomFeed feed = new OwsContextAtomFeed ();
            List<OwsContextAtomEntry> entries = new List<OwsContextAtomEntry> ();
            XmlDocument doc = new XmlDocument ();

            doc.LoadXml (any.OuterXml);
            XmlNamespaceManager xmlns = new XmlNamespaceManager (doc.NameTable);
            xmlns.AddNamespace ("rdf", "http://www.w3.org/1999/02/22-rdf-syntax-ns#");
            xmlns.AddNamespace ("dclite4g", "http://xmlns.com/2008/dclite4g#");
            xmlns.AddNamespace ("dc", "http://purl.org/dc/elements/1.1/");
            xmlns.AddNamespace ("dct", "http://purl.org/dc/terms/");

            var onlineResource = doc.SelectNodes ("rdf:RDF/dclite4g:DataSet/dclite4g:onlineResource", xmlns);

            var createddate = doc.SelectSingleNode ("rdf:RDF/dclite4g:DataSet/dct:created", xmlns).InnerText;
            var modifieddate = doc.SelectSingleNode ("rdf:RDF/dclite4g:DataSet/dct:modified", xmlns).InnerText;

            feed.Title = new Terradue.ServiceModel.Syndication.TextSyndicationContent (doc.SelectSingleNode ("rdf:RDF/dclite4g:DataSet/dc:title", xmlns).InnerText);
            feed.Description = new Terradue.ServiceModel.Syndication.TextSyndicationContent (doc.SelectSingleNode ("rdf:RDF/dclite4g:DataSet/dc:subject", xmlns).InnerText);
            feed.Date = new DateTimeInterval {
                StartDate = Convert.ToDateTime (modifieddate),
                EndDate = Convert.ToDateTime (modifieddate)
            };

            for (int i = 0; i < onlineResource.Count; i++) {
                string id = onlineResource [i].ChildNodes [0].Attributes.GetNamedItem ("rdf:about").InnerText;
                Uri remoteUri = new Uri (id);
                OwsContextAtomEntry entry = new OwsContextAtomEntry ();
                entry.Id = remoteUri.AbsoluteUri;
                entry.Title = new Terradue.ServiceModel.Syndication.TextSyndicationContent (remoteUri.AbsoluteUri);

                entry.PublishDate = new DateTimeOffset(Convert.ToDateTime(modifieddate));
                entry.LastUpdatedTime = new DateTimeOffset(Convert.ToDateTime(modifieddate));

                entry.Date = new DateTimeInterval {
                    StartDate = Convert.ToDateTime (modifieddate),
                    EndDate = Convert.ToDateTime (modifieddate)
                };

                entry.ElementExtensions.Add ("identifier", OwcNamespaces.Dc, identifier);
                entry.Links.Add (Terradue.ServiceModel.Syndication.SyndicationLink.CreateMediaEnclosureLink (remoteUri, "application/octet-stream", 0));

                List<OwcOffering> offerings = new List<OwcOffering> ();
                OwcOffering offering = new OwcOffering ();
                OwcContent content = new OwcContent ();
                content.Url = remoteUri;;
                string extension = remoteUri.AbsoluteUri.Substring (remoteUri.AbsoluteUri.LastIndexOf (".") + 1);

                switch (extension.ToLower ()) {
                case "gif":
                    content.Type = "image/gif";
                    offering.Code = "http://www.opengis.net/spec/owc-atom/1.0/req/gif";
                    break;
                case "gtiff":
                    content.Type = "image/tiff";
                    offering.Code = "http://www.opengis.net/spec/owc-atom/1.0/req/geotiff";
                    break;
                case "jpeg":
                    content.Type = "image/jpg";
                    offering.Code = "http://www.opengis.net/spec/owc-atom/1.0/req/jpg";
                    break;
                case "png":
                    content.Type = "image/png";
                    offering.Code = "http://www.opengis.net/spec/owc-atom/1.0/req/png";
                    break;
                default:
                    content.Type = "application/octet-stream";
                    offering.Code = null;
                    break;
                }

                List<OwcContent> contents = new List<OwcContent> ();
                contents.Add (content);
                offering.Contents = contents.ToArray ();
                offerings.Add (offering);
                entry.Offerings = offerings;

                entries.Add (entry);
            }
            feed.Items = entries;

            AtomFeed atomfeed = new AtomFeed (feed);
            atomfeed.Title = feed.Title;
            atomfeed.Description = feed.Description;

            var urib = new UriBuilder (baseurl);
            urib.Path += "/proxy/wps/" + identifier + "/description";
            atomfeed.Links.Add (new Terradue.ServiceModel.Syndication.SyndicationLink (urib.Uri, "search", "OpenSearch Description link", "application/opensearchdescription+xml", 0));

            return atomfeed;
        }

        private OpenSearchDescription GetWpsOpenSearchDescription (IfyContext context, string jobIdentifier, string name)
        {

            OpenSearchDescription OSDD = new OpenSearchDescription ();

            OSDD.ShortName = string.IsNullOrEmpty (name) ? jobIdentifier : name;
            OSDD.Attribution = "European Space Agency";
            OSDD.Contact = "info@esa.int";
            OSDD.Developer = "Terradue GeoSpatial Development Team";
            OSDD.SyndicationRight = "open";
            OSDD.AdultContent = "false";
            OSDD.Language = "en-us";
            OSDD.OutputEncoding = "UTF-8";
            OSDD.InputEncoding = "UTF-8";
            OSDD.Description = "This Search Service performs queries in the available results of Gpod process. There are several URL templates that return the results in different formats (RDF, ATOM or KML). This search service is in accordance with the OGC 10-032r3 specification.";

            OSDD.ExtraNamespace.Add("geo", "http://a9.com/-/opensearch/extensions/geo/1.0/");
            OSDD.ExtraNamespace.Add("time", "http://a9.com/-/opensearch/extensions/time/1.0/");
            OSDD.ExtraNamespace.Add("dct", "http://purl.org/dc/terms/");

            // The new URL template list 
            Hashtable newUrls = new Hashtable ();
            UriBuilder urib;
            NameValueCollection query = new NameValueCollection ();
            string [] queryString;

            urib = new UriBuilder (context.BaseUrl);
            urib.Path += "/proxy/wps/" + jobIdentifier + "/search";
            query.Add (OpenSearchFactory.GetBaseOpenSearchParameter ());

            queryString = Array.ConvertAll (query.AllKeys, key => string.Format ("{0}={1}", key, query [key]));
            urib.Query = string.Join ("&", queryString);
            newUrls.Add ("application/atom+xml", new OpenSearchDescriptionUrl ("application/atom+xml", urib.ToString (), "search", OSDD.ExtraNamespace));

            query.Set ("format", "json");
            queryString = Array.ConvertAll (query.AllKeys, key => string.Format ("{0}={1}", key, query [key]));
            urib.Query = string.Join ("&", queryString);
            newUrls.Add ("application/json", new OpenSearchDescriptionUrl ("application/json", urib.ToString (), "search", OSDD.ExtraNamespace));

            query.Set ("format", "html");
            queryString = Array.ConvertAll (query.AllKeys, key => string.Format ("{0}={1}", key, query [key]));
            urib.Query = string.Join ("&", queryString);
            newUrls.Add ("text/html", new OpenSearchDescriptionUrl ("application/html", urib.ToString (), "search", OSDD.ExtraNamespace));

            OSDD.Url = new OpenSearchDescriptionUrl [newUrls.Count];

            newUrls.Values.CopyTo (OSDD.Url, 0);

            return OSDD;
        }

    }

}