using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;
using System.Xml.Serialization;
using OpenGis.Wps;
using ServiceStack.Common.Web;
using ServiceStack.ServiceHost;
using Terradue.OpenSearch;
using Terradue.OpenSearch.Engine;
using Terradue.OpenSearch.GeoJson.Result;
using Terradue.OpenSearch.Result;
using Terradue.OpenSearch.Schema;
using Terradue.Portal;
using Terradue.ServiceModel.Ogc.OwsContext;
using Terradue.ServiceModel.Syndication;
using Terradue.Tep.WebServer;
using Terradue.WebService.Model;

namespace Terradue.Tep.WebServer.Services {

    [Route("/proxy", "GET", Summary = "proxy an url", Notes = "")]
    public class ProxyGetUrlRequestTep {
        [ApiMember(Name="url", Description = "url to be proxied", ParameterType = "query", DataType = "string", IsRequired = true)]
        public string url { get; set; }
    }

    [Route("/proxy/wps/{id}/description", "GET", Summary = "proxy a wps result description", Notes = "")]
    public class ProxyWpsJobDescriptionRequestTep {
        [ApiMember(Name="id", Description = "id of gpod job", ParameterType = "query", DataType = "string", IsRequired = true)]
        public string id { get; set; }
    }

    [Route("/proxy/wps/{id}/search", "GET", Summary = "proxy a wps result search", Notes = "")]
    public class ProxyWpsJobSearchRequestTep {
        [ApiMember(Name="id", Description = "id of gpod job", ParameterType = "query", DataType = "string", IsRequired = true)]
        public string id { get; set; }
    }

     [Api("Tep Terradue webserver")]
    [Restrict(EndpointAttributes.InSecure | EndpointAttributes.InternalNetworkAccess | EndpointAttributes.Json,
              EndpointAttributes.Secure   | EndpointAttributes.External | EndpointAttributes.Json)]
    public class ProxyServiceTep : ServiceStack.ServiceInterface.Service {

        private static readonly log4net.ILog log = log4net.LogManager.GetLogger
            (System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public object Get(ProxyGetUrlRequestTep request){

            log.InfoFormat("Proxy URL {0}", request.url);

            var uri = new UriBuilder(request.url);
            var host = uri.Host;
            var domain = host.Substring(host.LastIndexOf('.', host.LastIndexOf('.') - 1) + 1);
            if (!domain.Equals("terradue.int") && !domain.Equals("terradue.com")) throw new Exception("Non Terradue urls are not accepted");

            IfyWebContext context = TepWebContext.GetWebContext(PagePrivileges.EverybodyView);
            HttpResult result = null;
            context.Open();

            OpenSearchEngine ose = MasterCatalogue.OpenSearchEngine;

            if (uri.Path.EndsWith("/description") || uri.Path.EndsWith("/OSDD")) {
                GenericOpenSearchable urlToShare = new GenericOpenSearchable(new OpenSearchUrl(request.url), ose);
                Terradue.OpenSearch.Schema.OpenSearchDescription osd = urlToShare.GetOpenSearchDescription();
                result = new HttpResult(osd, "application/opensearchdescription+xml");
            } else {
                var e = OpenSearchFactory.FindOpenSearchable(ose, new Uri(request.url), "application/atom+xml");
                if (e.DefaultMimeType != "application/atom+xml" && !e.DefaultMimeType.Contains("xml")) {
                    throw new InvalidOperationException("No Url in the OpenSearch Description Document that query fully qualified model");
                }

                var type = OpenSearchFactory.ResolveTypeFromRequest(HttpContext.Current.Request, ose);
                var res = ose.Query(e, new NameValueCollection(), type);

                result = new HttpResult(res.SerializeToString(), res.ContentType);
            }

            context.Close ();
            return result;
        }

        public object Get(ProxyWpsJobDescriptionRequestTep request){

            IfyWebContext context = TepWebContext.GetWebContext(PagePrivileges.EverybodyView);
            context.Open();

            WpsJob wpsjob = WpsJob.FromIdentifier(context, request.id);

            log.InfoFormat("Wps Proxy description for wpsjob {0}", wpsjob.Identifier);

            OpenSearchDescription osd = GetWpsOpenSearchDescription(context, request.id, wpsjob.Name);

            context.Close();
            return new HttpResult(osd, "application/opensearchdescription+xml");
        }

        public object Get(ProxyWpsJobSearchRequestTep request){

            IfyWebContext context = TepWebContext.GetWebContext(PagePrivileges.EverybodyView);
            context.Open();

            WpsJob wpsjob = WpsJob.FromIdentifier(context, request.id);

            log.InfoFormat("Wps Proxy search for wpsjob {0}", wpsjob.Identifier);

            string executeUrl = wpsjob.StatusLocation;

            HttpWebRequest executeHttpRequest = (HttpWebRequest)WebRequest.Create(executeUrl);
            if (executeUrl.Contains("gpod.eo.esa.int")) {
                executeHttpRequest.Headers.Add("X-UserID", context.GetConfigValue("GpodWpsUser"));  
            }
            OpenGis.Wps.ExecuteResponse execResponse = null;
            try{
                log.Debug("Wps proxy - exec response requested");
                execResponse = (OpenGis.Wps.ExecuteResponse)new System.Xml.Serialization.XmlSerializer(typeof(OpenGis.Wps.ExecuteResponse)).Deserialize(executeHttpRequest.GetResponse().GetResponseStream());
                log.Debug("Wps proxy - exec response OK");
            }catch(Exception e){
                log.Error(e);
            }
            if (execResponse == null) throw new Exception("Unable to get execute response from proxied job");
            Uri uri = new Uri(execResponse.statusLocation);
            log.Debug("Proxy WPS - uri: " + uri);

            execResponse.statusLocation = context.BaseUrl + "/wps/RetrieveResultServlet?id=" + wpsjob.Identifier;

            AtomFeed feed = new AtomFeed();
            if(execResponse.ProcessOutputs != null){
                foreach (OutputDataType output in execResponse.ProcessOutputs) {
                    if(output.Identifier != null){
                        if (output.Item != null && ((DataType)(output.Item)).Item != null) {
                            var item = ((DataType)(output.Item)).Item as ComplexDataType;
                            if (item.Reference != null && output.Identifier.Value.Equals("result_metadata")) {
                                var reference = item.Reference as OutputReferenceType;
                                feed = CreateFeedForMetadata(reference.href);
                            } else if (item.Any != null && item.Any[0].LocalName != null) {
                                if (item.Any[0].LocalName.Equals("RDF")) {
                                    feed = CreateFeedForRDF(item.Any[0], request.id, context.BaseUrl);
                                } else if (item.Any[0].LocalName.Equals("metalink")) {
                                    feed = CreateFeedForMetalink(item.Any[0], request.id, context.BaseUrl);
                                }
                            }       
                        }
                    }
                }
            }

            var ose = MasterCatalogue.OpenSearchEngine;
            var type = OpenSearchFactory.ResolveTypeFromRequest(HttpContext.Current.Request, ose);
            var ext = ose.GetFirstExtensionByTypeAbility(type);

            var osfeed = new AtomFeedOpenSearchable(feed);
            HttpRequest httpRequest = HttpContext.Current.Request;
            Type responseType = OpenSearchFactory.ResolveTypeFromRequest(httpRequest, ose);
            IOpenSearchResultCollection osr = ose.Query(osfeed, httpRequest.QueryString, responseType);

            context.Close ();
            return new HttpResult(osr.SerializeToString(), osr.ContentType);

        }

        private AtomFeed CreateFeedForMetadata(string url){
            log.Debug("Wps proxy - metadata");

            HttpWebRequest atomRequest = (HttpWebRequest)WebRequest.Create(url);
            HttpWebResponse atomResponse = (HttpWebResponse)atomRequest.GetResponse();

            Stream atomResponseStream = new MemoryStream();
            atomResponse = (HttpWebResponse)atomRequest.GetResponse();
            atomResponse.GetResponseStream().CopyTo(atomResponseStream);
            atomResponseStream.Seek(0, SeekOrigin.Begin);

            var sr = XmlReader.Create(atomResponseStream);
            Atom10FeedFormatter atomFormatter = new Atom10FeedFormatter();
            atomFormatter.ReadFrom(sr);
            sr.Close();
            return new AtomFeed(atomFormatter.Feed);
        }

        private AtomFeed CreateFeedForMetalink(XmlNode any, string identifier, string baseurl){
            log.Debug("Wps proxy - metalink");

            OwsContextAtomFeed feed = new OwsContextAtomFeed();
            List<OwsContextAtomEntry> entries = new List<OwsContextAtomEntry>();

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(any.OuterXml);
            XmlNamespaceManager xmlns = new XmlNamespaceManager(doc.NameTable);
            xmlns.AddNamespace("ml", "http://www.metalinker.org");

            var onlineResource = doc.SelectNodes("ml:metalink/ml:files/ml:file/ml:resources/ml:url",xmlns);
            var createddate = doc.SelectSingleNode("ml:metalink/ml:files/ml:file/ml:releasedate", xmlns).InnerText;

            feed.Title = new Terradue.ServiceModel.Syndication.TextSyndicationContent("Wps job results");
            feed.Date = new DateTimeInterval {
                StartDate = Convert.ToDateTime(createddate),
                EndDate = Convert.ToDateTime(createddate)
            };

            foreach (XmlNode node in onlineResource) {
                string url = node.InnerText;
                Uri remoteUri = new Uri(url);
                OwsContextAtomEntry entry = new OwsContextAtomEntry();

                //link is an OWS context, we add it as is
                if (url.Contains("/results/") && url.Contains(".atom")) {
                    HttpWebRequest atomRequest = (HttpWebRequest)WebRequest.Create(url);
                    HttpWebResponse atomResponse = (HttpWebResponse)atomRequest.GetResponse();

                    Stream atomResponseStream = new MemoryStream();
                    atomResponse = (HttpWebResponse)atomRequest.GetResponse();
                    atomResponse.GetResponseStream().CopyTo(atomResponseStream);
                    atomResponseStream.Seek(0, SeekOrigin.Begin);

                    var sr = XmlReader.Create(atomResponseStream);
                    Atom10FeedFormatter atomFormatter = new Atom10FeedFormatter();
                    atomFormatter.ReadFrom(sr);
                    sr.Close();
                    return new AtomFeed(atomFormatter.Feed);
                }
                //we build the OWS context
                entry.Id = node.InnerText;
                entry.Title = new Terradue.ServiceModel.Syndication.TextSyndicationContent(remoteUri.AbsoluteUri);
                entry.PublishDate = new DateTimeOffset(Convert.ToDateTime(createddate), new TimeSpan(0));
                entry.LastUpdatedTime = new DateTimeOffset(Convert.ToDateTime(createddate), new TimeSpan(0));
                entry.Date = new DateTimeInterval {
                    StartDate = Convert.ToDateTime(createddate),
                    EndDate = Convert.ToDateTime(createddate)
                };

                entry.ElementExtensions.Add("identifier", OwcNamespaces.Dc, remoteUri.AbsolutePath);
                entry.Links.Add(Terradue.ServiceModel.Syndication.SyndicationLink.CreateMediaEnclosureLink(remoteUri, "application/octet-stream", 0));

                List<OwcOffering> offerings = new List<OwcOffering>();
                OwcOffering offering = new OwcOffering();
                OwcContent content = new OwcContent();
                content.Url = remoteUri.ToString();
                string extension = remoteUri.AbsoluteUri.Substring(remoteUri.AbsoluteUri.LastIndexOf(".") + 1);

                switch (extension.ToLower()) {
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

                List<OwcContent> contents = new List<OwcContent>();
                contents.Add(content);
                offering.Contents = contents.ToArray();
                offerings.Add(offering);
                entry.Offerings = offerings;

                entries.Add(entry);
            }
            feed.Items = entries;

            AtomFeed atomfeed = new AtomFeed(feed);
            atomfeed.Title = feed.Title;
            atomfeed.Description = feed.Description;

            var urib = new UriBuilder(baseurl);
            urib.Path += "/proxy/wps/" + identifier + "/description";
            atomfeed.Links.Add(new Terradue.ServiceModel.Syndication.SyndicationLink(urib.Uri, "search", "OpenSearch Description link", "application/opensearchdescription+xml", 0));

            return atomfeed;
        }

        private AtomFeed CreateFeedForRDF(XmlNode any, string identifier, string baseurl){
            log.Debug("Wps proxy - RDF");
            OwsContextAtomFeed feed = new OwsContextAtomFeed();
            List<OwsContextAtomEntry> entries = new List<OwsContextAtomEntry>();
            XmlDocument doc = new XmlDocument();

            doc.LoadXml(any.OuterXml);
            XmlNamespaceManager xmlns = new XmlNamespaceManager(doc.NameTable);
            xmlns.AddNamespace("rdf", "http://www.w3.org/1999/02/22-rdf-syntax-ns#");
            xmlns.AddNamespace("dclite4g", "http://xmlns.com/2008/dclite4g#");
            xmlns.AddNamespace("dc", "http://purl.org/dc/elements/1.1/");
            xmlns.AddNamespace("dct", "http://purl.org/dc/terms/");

            var onlineResource = doc.SelectNodes("rdf:RDF/dclite4g:DataSet/dclite4g:onlineResource",xmlns);

            var createddate = doc.SelectSingleNode("rdf:RDF/dclite4g:DataSet/dct:created", xmlns).InnerText;
            var modifieddate = doc.SelectSingleNode("rdf:RDF/dclite4g:DataSet/dct:modified", xmlns).InnerText;

            feed.Title = new Terradue.ServiceModel.Syndication.TextSyndicationContent(doc.SelectSingleNode("rdf:RDF/dclite4g:DataSet/dc:title",xmlns).InnerText);
            feed.Description = new Terradue.ServiceModel.Syndication.TextSyndicationContent(doc.SelectSingleNode("rdf:RDF/dclite4g:DataSet/dc:subject",xmlns).InnerText);
            feed.Date = new DateTimeInterval {
                StartDate = Convert.ToDateTime(modifieddate),
                EndDate = Convert.ToDateTime(modifieddate)
            };

            for (int i = 0; i < onlineResource.Count; i++) {
                string id = onlineResource[i].ChildNodes[0].Attributes.GetNamedItem("rdf:about").InnerText;
                Uri remoteUri = new Uri(id);
                OwsContextAtomEntry entry = new OwsContextAtomEntry();
                entry.Id = remoteUri.AbsoluteUri;
                entry.Title = new Terradue.ServiceModel.Syndication.TextSyndicationContent(remoteUri.AbsoluteUri);
                entry.PublishDate = new DateTimeOffset(Convert.ToDateTime(modifieddate), new TimeSpan(0));
                entry.LastUpdatedTime = new DateTimeOffset(Convert.ToDateTime(modifieddate), new TimeSpan(0));
                entry.Date = new DateTimeInterval {
                    StartDate = Convert.ToDateTime(modifieddate),
                    EndDate = Convert.ToDateTime(modifieddate)
                };

                entry.ElementExtensions.Add("identifier", OwcNamespaces.Dc, identifier);
                entry.Links.Add(Terradue.ServiceModel.Syndication.SyndicationLink.CreateMediaEnclosureLink(remoteUri, "application/octet-stream", 0));

                List<OwcOffering> offerings = new List<OwcOffering>();
                OwcOffering offering = new OwcOffering();
                OwcContent content = new OwcContent();
                content.Url = remoteUri.ToString();
                string extension = remoteUri.AbsoluteUri.Substring(remoteUri.AbsoluteUri.LastIndexOf(".") + 1);

                switch (extension.ToLower()) {
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

                List<OwcContent> contents = new List<OwcContent>();
                contents.Add(content);
                offering.Contents = contents.ToArray();
                offerings.Add(offering);
                entry.Offerings = offerings;

                entries.Add(entry);
            }
            feed.Items = entries;

            AtomFeed atomfeed = new AtomFeed(feed);
            atomfeed.Title = feed.Title;
            atomfeed.Description = feed.Description;

            var urib = new UriBuilder(baseurl);
            urib.Path += "/proxy/wps/" + identifier + "/description";
            atomfeed.Links.Add(new Terradue.ServiceModel.Syndication.SyndicationLink(urib.Uri, "search", "OpenSearch Description link", "application/opensearchdescription+xml", 0));

            return atomfeed;
        }

        private OpenSearchDescription GetWpsOpenSearchDescription(IfyContext context, string jobIdentifier, string name) {

            OpenSearchDescription OSDD = new OpenSearchDescription();

            OSDD.ShortName = string.IsNullOrEmpty(name) ? jobIdentifier : name;
            OSDD.Attribution = "European Space Agency";
            OSDD.Contact = "info@esa.int";
            OSDD.Developer = "Terradue GeoSpatial Development Team";
            OSDD.SyndicationRight = "open";
            OSDD.AdultContent = "false";
            OSDD.Language = "en-us";
            OSDD.OutputEncoding = "UTF-8";
            OSDD.InputEncoding = "UTF-8";
            OSDD.Description = "This Search Service performs queries in the available results of Gpod process. There are several URL templates that return the results in different formats (RDF, ATOM or KML). This search service is in accordance with the OGC 10-032r3 specification.";

            // The new URL template list 
            Hashtable newUrls = new Hashtable();
            UriBuilder urib;
            NameValueCollection query = new NameValueCollection();
            string[] queryString;

            urib = new UriBuilder(context.BaseUrl);
            urib.Path += "/proxy/wps/" + jobIdentifier + "/search";
            query.Add(OpenSearchFactory.GetBaseOpenSearchParameter());

            query.Set("format", "json");
            queryString = Array.ConvertAll(query.AllKeys, key => string.Format("{0}={1}", key, query[key]));
            urib.Query = string.Join("&", queryString);
            newUrls.Add("application/json", new OpenSearchDescriptionUrl("application/json", urib.ToString(), "search"));

            OSDD.Url = new OpenSearchDescriptionUrl[newUrls.Count];

            newUrls.Values.CopyTo(OSDD.Url, 0);

            return OSDD;
        }

    }

}