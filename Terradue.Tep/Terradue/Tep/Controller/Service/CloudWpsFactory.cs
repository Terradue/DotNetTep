using System;
using Terradue.Portal;
using Terradue.OpenNebula;
using System.Collections.Generic;
using System.Xml;
using Terradue.Cloud;
using System.Linq;
using Terradue.OpenSearch;
using System.Collections.Specialized;
using Terradue.OpenSearch.Engine;
using Terradue.OpenSearch.Request;
using Terradue.OpenSearch.Result;
using Terradue.OpenSearch.Schema;
using System.Web;


/*!
\defgroup CloudWpsFactory WPS Factory
@{

This component enables Cloud Appliance that exposes a WPS service to be exposed as a processing service automatically.

\ingroup Cloud

Using the interface to the cloud controller, it retrieves the VMs that exposes a WPS interface in the cloud.

\xrefitem dep "Dependencies" "Dependencies" uses \ref CoreWPS to analyse and manage the WPS services.

\xrefitem int "Interfaces" "Interfaces" connects \ref OpenNebulaXMLRPC to discover the dynamic WPS providers in the Cloud.



@}
 */

namespace Terradue.Tep {

    /// \ingroup CloudWpsFactory
    /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
    public class CloudWpsFactory : IOpenSearchable {

        private static readonly log4net.ILog log = log4net.LogManager.GetLogger
            (System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private string cloudusername { get; set; }

        private OneClient oneclient { get; set; }
        private OneClient oneClient { 
            get{ 
                if (oneclient == null) {
                    oneclient = oneCloud.XmlRpc;
                }
                oneclient.StartDelegate(cloudusername);
                return oneclient;
            } 
        }
        private OneCloudProvider onecloud { get; set; }
        private OneCloudProvider oneCloud { 
            get{ 
                if(onecloud==null) onecloud = (OneCloudProvider)CloudProvider.FromId(context, context.GetConfigIntegerValue("One-default-provider"));
                return onecloud;
            }
        }

        private IfyContext context { get; set; }

        private string keyword;

        public OpenSearchEngine OpenSearchEngine { get; set; }

        public string Identifier {
            get {
                if(keyword == null) keyword = new WpsProcessOffering(context).EntityType.Keyword + context.Username;
                return keyword;
            }
        }

        public List<WpsProcessOffering> Items { get; set; }

        public long TotalResults {
            get {
                return 0;
            }
        }

        public string DefaultMimeType {
            get {
                return "application/atom+xml";
            }
        }

        public bool CanCache {
            get {
                return false;
            }
        }

        public void StartDelegate(int idusr){
            CloudUser cuser = CloudUser.FromIdAndProvider(context, idusr, oneCloud.Id);
            cloudusername = cuser.CloudUsername;
        }

        public void StartDelegate(string username){
            cloudusername = username;
        }

        public void EndDelegate(){
            CloudUser cuser = CloudUser.FromIdAndProvider(context, context.UserId, oneCloud.Id);
            cloudusername = cuser.CloudUsername;
        }

        public CloudWpsFactory(IfyContext context) {
            this.context = context;
            this.Items = new List<WpsProcessOffering>();
        }

        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        public List<WpsProvider> GetWPSFromVMs(NameValueCollection parameters = null){
            if (context.UserId == 0) return new List<WpsProvider>();
            if (!(parameters != null && !string.IsNullOrEmpty(parameters["cloud"]) && parameters["cloud"] == "true")) return new List<WpsProvider>();

            List<WpsProvider> result = new List<WpsProvider>();
            try{
                StartDelegate(context.UserId);
                context.LogDebug(this,string.Format("Get VM Pool for user {0}",cloudusername));
                VM_POOL pool = oneClient.VMGetPoolInfo(-2, -1, -1, 3);
                if(pool != null && pool.VM != null){
                    context.LogDebug(this,string.Format("{0} VM found",pool.VM != null ? pool.VM.Length : 0));
                    foreach (VM vm in pool.VM) {
                        if(vm.USER_TEMPLATE == null) continue;
                        try{
                            var wps = CreateWpsProviderForOne (context, vm, parameters);
                            if (wps != null) {
                                if (parameters != null && (!string.IsNullOrEmpty(parameters["uid"]) || !string.IsNullOrEmpty(parameters["id"]))){
                                    var uid = !string.IsNullOrEmpty(parameters["uid"]) ? parameters["uid"] : parameters["id"];
                                    if (uid == wps.Identifier) result.Add(wps);
                                } else result.Add(wps);
                            }
                        }catch(Exception e){

                        }
                    }
                }
            }catch(System.Net.WebException e){
            }
            return result;
        }

        /// <summary>
        /// Creates the wps provider from VM id (OpenNebula).
        /// </summary>
        /// <returns>The wps provider</returns>
        /// <param name="id">Id of the Virtual Machine.</param>
        public WpsProvider CreateWpsProviderForOne(string id){
            StartDelegate(oneCloud.AdminUsr);
            VM vm = oneClient.VMGetInfo(Int32.Parse(id));
            EndDelegate();
            return CreateWpsProviderForOne(context, vm);
        }

        /// <summary>
        /// Creates the wps provider from VM (OpenNebula).
        /// </summary>
        /// <returns>The wps provider</returns>
        /// <param name="vm">Virtual machine object.</param>
        public static WpsProvider CreateWpsProviderForOne(IfyContext context, VM vm, NameValueCollection parameters = null){
            context.LogDebug (context, "VM id = " + vm.ID);
            context.LogDebug (context, "VM name = " + vm.GNAME);
            WpsProvider wps = new WpsProvider(context);
            wps.Name = vm.NAME;
            wps.Identifier = "one-" + vm.ID;
            wps.Description = vm.NAME + " by " + vm.UNAME + " from laboratory " + vm.GNAME;
            wps.Proxy = true;
            wps.IsSandbox = true;
            wps.Tags = "";

            try {
                var user = UserTep.FromId(context, context.UserId);
                wps.Domain = user.GetPrivateDomain();
            } catch (Exception e) {
                context.LogError(wps, e.Message);
            }

            XmlNode[] user_template = (XmlNode[])vm.USER_TEMPLATE;
            bool isWPS = false;
            string wpsPort = "8080", wpsPath = "wps/WebProcessingService", nic = "";

            context.LogDebug(wps, "Loading user template");
            foreach (XmlNode nodeUT in user_template) {
                switch (nodeUT.Name) { 
                    case "WPS":
                    context.LogDebug(wps, string.Format("WPS found : {0} - {1}", vm.ID, vm.GNAME));
                    isWPS = true;
                    break;
                    case "OPERATIONAL":
                    context.LogDebug(wps, string.Format("Operational VM found : {0} - {1}", vm.ID, vm.GNAME));
                    wps.IsSandbox = false;
                    break;
                    case "TAGS":
                    context.LogDebug(wps, string.Format("Tags found : {0} - {1} : {2}", vm.ID, vm.GNAME, nodeUT.InnerText));
                    var tags = nodeUT.InnerText.Split(',').ToList();
                    if (tags != null) {
                        foreach (var tag in tags) {
                            wps.AddTag(tag);
                        }
                    }
                    break;
                    case "LOGO":
                    context.LogDebug(wps, string.Format("Logo found : {0} - {1} : {2}", vm.ID, vm.GNAME, nodeUT.InnerText));
                    wps.IconUrl = nodeUT.InnerText;
                    break;
                    case "WPS_PORT":
                    context.LogDebug(wps, string.Format("Wps Port found : {0} - {1} : {2}", vm.ID, vm.GNAME, nodeUT.InnerText));
                    wpsPort = nodeUT.InnerText;
                    break;
                    case "WPS_PATH":
                    context.LogDebug(wps, string.Format("Wps Path found : {0} - {1} : {2}", vm.ID, vm.GNAME, nodeUT.InnerText));
                    wpsPath= nodeUT.InnerText;
                    break;
                    default:
                    break;
                }
            }

            context.LogDebug(wps, "Loading template");
            foreach (XmlNode nodeT in (XmlNode[])vm.TEMPLATE) {
                context.LogDebug(context, "node name = " + nodeT.Name);
                switch (nodeT.Name) { 
                    case "NIC":
                    nic = nodeT["IP"].InnerText;
                    break;
                    default:
                    break;
                }
            }

            wps.BaseUrl = String.Format("http://{0}:{1}/{2}", nic, wpsPort, wpsPath);
            context.LogDebug(context, "wpsbaseurl = " + wps.BaseUrl);

            if (isWPS) {
                //check query parameters
                if (parameters != null) {
                    //case sandbox
                    if (parameters["sandbox"] != null) {
                        switch (parameters["sandbox"]) {
                        case "true":
                            if (!wps.IsSandbox) return null;
                            break;
                        case "false":
                            if (wps.IsSandbox) return null;
                            break;
                        default:
                            break;
                        }
                    }
                    //case hostname
                    if (parameters["hostname"] != null) {
                        var uriHost = new UriBuilder(nic);
                        var r = new System.Text.RegularExpressions.Regex(parameters["hostname"]);
                        var m = r.Match(uriHost.Host);
                        if (!m.Success) return null;
                    }
                    //case tags
                    if (parameters["tag"] != null) {
                        if (wps.Tags == null || !wps.Tags.Contains(parameters["tag"])) return null;
                    }
                }
            
            }
            return wps;
        }

        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        public WpsProcessOffering CreateWpsProcessOfferingForOne(string vmId, string processId){
            WpsProvider wps = this.CreateWpsProviderForOne(vmId);

            foreach (WpsProcessOffering process in wps.GetWpsProcessOfferingsFromRemote()) {
                context.LogDebug (this, "Get process -- " + process.RemoteIdentifier);
                if (process.RemoteIdentifier.Equals(processId)) {
                    if (process.Domain == null && wps.Domain != null) process.Domain = wps.Domain;
                    process.Available = true;
                    return process;
                }
            }

            return null;
        }

        /// <summary>
        /// Gets the wps process offering from DB or from cloud
        /// </summary>
        /// <returns>The wps process offering.</returns>
        /// <param name="identifier">Identifier.</param>
        public static WpsProcessOffering GetWpsProcessOffering(IfyContext context, string identifier){
            WpsProcessOffering wps = null;
            try {
                //wps is stored in DB
                wps = (WpsProcessOffering)WpsProcessOffering.FromIdentifier (context, identifier);
            } catch (UnauthorizedAccessException e) {
                throw e;
            } catch (Exception e) {
                //wps is not stored in DB
                string[] identifierParams = identifier.Split("-".ToCharArray());
                if (identifierParams.Length == 3) {
                    switch (identifierParams[0]) {
                        //wps is stored in OpenNebula Cloud Provider
                        case "one":
                            wps = new CloudWpsFactory(context).CreateWpsProcessOfferingForOne(identifierParams[1], identifierParams[2]);
                            break;
                        default:
                            break;
                    }
                }
            }
            if (wps == null) throw new Exception("Unknown identifier");
            return wps;
        }

        public QuerySettings GetQuerySettings(OpenSearchEngine ose) {
            IOpenSearchEngineExtension osee = ose.GetExtensionByContentTypeAbility(this.DefaultMimeType);
            if (osee == null) return null;
            return new QuerySettings(this.DefaultMimeType, osee.ReadNative);
        }

        public OpenSearchRequest Create(QuerySettings querySettings, NameValueCollection parameters) {
            UriBuilder url = new UriBuilder(context.BaseUrl);
            url.Path += "/" + this.Identifier;
            parameters.Add("t2-onewps", "true");
            var array = (from key in parameters.AllKeys
                         from value in parameters.GetValues(key)
                         select string.Format("{0}={1}", HttpUtility.UrlEncode(key), HttpUtility.UrlEncode(value)))
                .ToArray();
            url.Query = string.Join("&", array);

            var request = new AtomOpenSearchRequest(new OpenSearchUrl(url.Uri), OneWpsGenerateSyndicationFeed);

            return request;
        }

        public OpenSearchDescription GetOpenSearchDescription() {
            OpenSearchDescription osd = new OpenSearchDescription();
            osd.Contact = context.GetConfigValue("CompanyEmail");
            osd.SyndicationRight = "open";
            osd.AdultContent = "false";
            osd.Language = "en-us";
            osd.OutputEncoding = "UTF-8";
            osd.InputEncoding = "UTF-8";
            osd.Developer = "Terradue OpenSearch Development Team";
            osd.Attribution = context.GetConfigValue("CompanyName");

            osd.ExtraNamespace.Add("geo", "http://a9.com/-/opensearch/extensions/geo/1.0/");
            osd.ExtraNamespace.Add("time", "http://a9.com/-/opensearch/extensions/time/1.0/");
            osd.ExtraNamespace.Add("dct", "http://purl.org/dc/terms/");
            osd.ExtraNamespace.Add("t2", "http://www.terradue.com/opensearch");
            osd.ExtraNamespace.Add("cor", "http://a9.com/-/opensearch/extensions/correlation/1.0/");

            List<OpenSearchDescriptionUrl> urls = new List<OpenSearchDescriptionUrl>();

            UriBuilder urlb = new UriBuilder(GetDescriptionBaseUrl());

            OpenSearchDescriptionUrl url = new OpenSearchDescriptionUrl("application/opensearchdescription+xml", urlb.ToString(), "self", osd.ExtraNamespace);
            urls.Add(url);

            urlb = new UriBuilder(GetSearchBaseUrl("application/atom+xml"));
            NameValueCollection query = GetOpenSearchParameters("application/atom+xml");

            NameValueCollection nvc = HttpUtility.ParseQueryString(urlb.Query);
            foreach (var key in nvc.AllKeys) {
                query.Set(key, nvc[key]);
            }

            foreach (var osee in OpenSearchEngine.Extensions.Values) {
                query.Set("format", osee.Identifier);

                string[] queryString = Array.ConvertAll(query.AllKeys, key => string.Format("{0}={1}", key, query[key]));
                urlb.Query = string.Join("&", queryString);
                url = new OpenSearchDescriptionUrl(osee.DiscoveryContentType, urlb.ToString(), "search", osd.ExtraNamespace);
                urls.Add(url);
            }

            osd.Url = urls.ToArray();

            return osd;
        }

        public virtual OpenSearchUrl GetSearchBaseUrl(string mimetype) {
            return new OpenSearchUrl(string.Format("{0}/{1}/search", context.BaseUrl, keyword));
        }

        public virtual OpenSearchUrl GetDescriptionBaseUrl() {
                return new OpenSearchUrl(string.Format("{0}/{1}/description", context.BaseUrl, keyword));
        }

        public NameValueCollection GetOpenSearchParameters(string mimeType) {
            var nvc = OpenSearchFactory.GetBaseOpenSearchParameter();

            //add EntityCollections parameters
            nvc.Set("id", "{t2:uid?}");
            nvc.Set("sl", "{t2:sl?}");
            nvc.Set("disableCache", "{t2:cache?}");
            nvc.Set("domain", "{t2:domain?}");
            nvc.Set("author", "{t2:author?}");
            nvc.Set("visibility", "{t2:visibility?}");
            nvc.Add("correlatedTo", "{cor:with?}");
            return nvc;
        }

        public void ApplyResultFilters(OpenSearchRequest request, ref IOpenSearchResultCollection osr, string finalContentType) {
        }

        private AtomFeed OneWpsGenerateSyndicationFeed(NameValueCollection parameters) {
            UriBuilder myUrl = new UriBuilder(context.BaseUrl + "/" + this.Identifier);
            string[] queryString = Array.ConvertAll(parameters.AllKeys, key => String.Format("{0}={1}", key, parameters[key]));
            myUrl.Query = string.Join("&", queryString);

            AtomFeed feed = new AtomFeed("Discovery feed for " + this.Identifier,
                                                       "This OpenSearch Service allows the discovery of the different items which are part of the " + this.Identifier + " collection. " +
                                                       "This search service is in accordance with the OGC 10-032r3 specification.",
                                                       myUrl.Uri, myUrl.ToString(), DateTimeOffset.UtcNow);

            feed.Generator = "Terradue Web Server";

            List<AtomItem> items = new List<AtomItem>();

            var processes = GetWPSprocessesFromVMs(parameters);
            foreach (WpsProcessOffering process in processes) {
                try{
                    AtomItem item = process.ToAtomItem(parameters);
                    if (item != null) {
                        Items.Add(process);
                        items.Add(item);
                    }
				} catch (Exception e) {
					//we do nothing, we just dont add the process
				}
            }

            // Load all avaialable Datasets according to the context

            if (this.Identifier != null) feed.ElementExtensions.Add("identifier", "http://purl.org/dc/elements/1.1/", this.Identifier);

            feed.Items = items;
            feed.TotalResults = items.Count;


            return feed;

        }

        /// <summary>
        /// Gets the WPS processes from VM.
        /// </summary>
        /// <returns>The WPS processes from VM.</returns>
        /// <param name="parameters">Parameters.</param>
        public List<WpsProcessOffering> GetWPSprocessesFromVMs(NameValueCollection parameters = null){
            List<WpsProcessOffering> processes = new List<WpsProcessOffering>();
			var wpss = GetWPSFromVMs(parameters);
			foreach (WpsProvider wps in wpss) {
				try {
                    foreach (WpsProcessOffering process in wps.GetWpsProcessOfferingsFromRemote()) {
                        processes.Add(process);
                    }
                }catch (Exception e) {
					//we do nothing, we just dont add the process
				}
			}
            return processes;
        }
    }
}

