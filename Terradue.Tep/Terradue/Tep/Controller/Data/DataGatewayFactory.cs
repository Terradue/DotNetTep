using System;
using System.Net;
using System.Runtime.Caching;
using System.Text.RegularExpressions;
using Terradue.OpenSearch;
using Terradue.OpenSearch.Result;
using Terradue.ServiceModel.Syndication;
using Terradue.Tep.OpenSearch;
using System.Runtime.Serialization;
using System.Collections.Generic;
using ServiceStack.Text;
using System.IO;
using Terradue.Artifactory.Response;
using Terradue.Artifactory;

namespace Terradue.Tep
{
    public class DataGatewayFactory
    {

        private static readonly log4net.ILog log = log4net.LogManager.GetLogger
            (System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        static MemoryCache downloadUriCache = new MemoryCache("downloadUriCache");
        static System.Collections.Specialized.NameValueCollection AppSettings = System.Configuration.ConfigurationManager.AppSettings;
        static string dataGatewayBaseUrl = AppSettings["DataGatewayBaseUrl"];
        static string dataGatewayShareUrl = AppSettings["DataGatewayShareUrl"];
        static string dataGatewaySecretKey = AppSettings["DataGatewaySecretKey"];
        static string dataGatewayApikey = AppSettings["DataGatewayApikey"];

        public static SyndicationLink SubstituteEnclosure(SyndicationLink link, IOpenSearchable os, IOpenSearchResultItem item)
        {

            if (os is SandboxOpenSearchable)
            {
                return SubstituteSandboxEnclosure(link, os as SandboxOpenSearchable, item);
            }
            return SubstituteExternalEnclosure(link);

        }

        public static SyndicationLink SubstituteExternalEnclosure(SyndicationLink link)
        {
            var uri = link.BaseUri ?? link.Uri;
            UriBuilder urib = new UriBuilder(uri);

            urib.Path = "";
            urib.Query = null;
            urib.Fragment = null;

            var substUri = FindSubstituteUri(urib.Uri);

            if (substUri == null)
                return null;

            urib = new UriBuilder(substUri);
            urib.Path += RewriteExternalPath (uri);

            return new SyndicationLink(urib.Uri, "enclosure", link.Title + " via Data Gateway", link.MediaType, link.Length);

        }

        public static Uri SubstituteUrlApi(Uri url, IOpenSearchable openSearchable, IOpenSearchResultItem item)
        {
            UriBuilder urib = new UriBuilder(url);

            urib.Path = "";
            urib.Query = null;
            urib.Fragment = null;

            var substUri = FindSubstituteUri(urib.Uri, true);

            if (substUri == null)
                return null;

            urib = new UriBuilder(substUri);
            if (openSearchable is SandboxOpenSearchable) {
                urib.Path += RewritePath (url.AbsolutePath, openSearchable);
            } else 
                urib.Path += RewriteExternalPath (url);


            return urib.Uri;
        }

        static Uri FindSubstituteUri(Uri baseUri, bool api = false)
        {

            var cacheItem = downloadUriCache.GetCacheItem(baseUri.ToString());
            Uri url = null;

            if (cacheItem != null)
                url = (Uri)cacheItem.Value;
            else
                url = SearchAndCacheDataGatewayRepo(baseUri);

            if (url != null & !api)
            {
                UriBuilder urib = new UriBuilder(url);
                urib.Path = urib.Path.Replace("api/", "");
                url = urib.Uri;
            }

            return url;

        }

        static Uri SearchAndCacheDataGatewayRepo(Uri baseUri)
        {
            UriBuilder urib = new UriBuilder(dataGatewayBaseUrl);

            urib.Path += string.Format("/api/{0}/", baseUri.Host);

            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(urib.Uri);
            request.Headers.Add("X-JFrog-Art-Api", "AKCp2V68vr2SNikpe5FoXFoxDk2PwkZoRGXCWi56yUDDa4S4c5U1yi6qUJKZXYxP9imviGUwf");

            try
            {
                System.Threading.Tasks.Task.Factory.FromAsync<WebResponse>(request.BeginGetResponse,
                                                                        request.EndGetResponse,
                                                                            null)
                .ContinueWith(task =>
                {
                    var httpResponse = (HttpWebResponse)task.Result;                            
                }).ConfigureAwait(false).GetAwaiter().GetResult();
            }
            catch (Exception e)
            {
                log.WarnFormat("{0} not in data gateway at {1} : {2}", baseUri.Host, urib, e.Message);
                return null;
            }

            urib = new UriBuilder(dataGatewayBaseUrl);
            urib.Path += string.Format("/api/{0}/", baseUri.Host);

            downloadUriCache.Set(new CacheItem(baseUri.ToString(), urib.Uri), new CacheItemPolicy() { AbsoluteExpiration = DateTimeOffset.Now.AddHours(1) });

            return urib.Uri;
        }

        static SyndicationLink SubstituteSandboxEnclosure(SyndicationLink link, SandboxOpenSearchable sandboxOpenSearchable, IOpenSearchResultItem item)
        {
            UriBuilder urib = new UriBuilder(link.Uri.AbsoluteUri);
            urib.Path = "";
            urib.Query = null;
            urib.Fragment = null;

            var substUri = FindSubstituteUri(urib.Uri);

            if (substUri == null)
                return null;

            urib = new UriBuilder(substUri);
            urib.Path += RewritePath(link.Uri.AbsolutePath, sandboxOpenSearchable);


            return new SyndicationLink(urib.Uri, "enclosure", link.Title + " via Data Gateway", link.MediaType, link.Length);
        }

        public static string RewritePath(string path, IOpenSearchable openSearchable)
        {
            if (openSearchable is SandboxOpenSearchable)
            {
                var sandboxOpenSearchable = openSearchable as SandboxOpenSearchable;
                var match = Regex.Match(path, string.Format(".*\\/{0}\\/_results\\/(?'relativeFilename'.*)", sandboxOpenSearchable.SandboxOpenSearchInformation.RunId));
                if (match.Success)
                    return path = string.Format("/production/workflows/{0}/runs/{1}/{2}", sandboxOpenSearchable.SandboxOpenSearchInformation.Workflow,
                                              sandboxOpenSearchable.SandboxOpenSearchInformation.RunId,
                                              match.Groups["relativeFilename"].Value);
            }

            return path;
        }

        /// <summary>
        /// Rewrites the external path.
        /// </summary>
        /// <returns>The external path.</returns>
        /// <param name="uri">URI.</param>
        public static string RewriteExternalPath (Uri uri) {
            var path = uri.AbsolutePath;

            if (AppSettings["DataGatewaySubstitutions"] == null) return path;

            List<DataGatewaySubstitution> dataGatewaySubstitutions = JsonSerializer.DeserializeFromString<List<DataGatewaySubstitution>> (AppSettings ["DataGatewaySubstitutions"]);
            foreach (var sub in dataGatewaySubstitutions) {
                if (uri.Host.Equals(sub.host) && path.Contains(sub.oldvalue)) {
                    return path.Replace (sub.oldvalue, sub.substitute);
                }
            }
            return path;
        }

        /// <summary>
        /// Shares on store.
        /// </summary>
        /// <param name="identifier">Identifier.</param>
        /// <param name="type">Type.</param>
        /// <param name="users">Users.</param>
        /// <param name="communities">Communities.</param>
        public static void ShareOnStore(string origin, string identifier, string type, string visibility, List<string> users = null, List<ThematicCommunity> communities = null){
            var shareInput = new StoreShareRequest { 
                origin = origin,
                type = type,
                identifier = identifier,
                visibility = visibility
            };

            if(users != null){
                shareInput.users = new List<StoreShareUser>();
                foreach(var usr in users){
                    shareInput.users.Add(new StoreShareUser{ username = usr });
                }
            }
			if (communities != null) {
                shareInput.communities = new List<StoreShareCommunity>();
				foreach (var c in communities) {
                    var cUsers = c.GetUsers();
                    var scUsers = new List<StoreShareUser>();
                    foreach(var usr in cUsers){
                        if(!c.IsUserPending(usr.Id)){
                            if (string.IsNullOrEmpty(usr.TerradueCloudUsername)) usr.LoadCloudUsername();
                            if (!string.IsNullOrEmpty(usr.TerradueCloudUsername)) scUsers.Add(new StoreShareUser { username = usr.TerradueCloudUsername });
                        }
                    }
                    shareInput.communities.Add(new StoreShareCommunity { identifier = c.Identifier, users = scUsers });
				}
			}
            var payload = JsonSerializer.SerializeToString<StoreShareRequest>(shareInput);			

            ShareRequest shareRequest = ShareRequest.LoadFromJsonString(payload, new ArtifactoryBaseUrl(dataGatewayBaseUrl, dataGatewayApikey));
            shareRequest.ExecuteAsync(new System.Threading.CancellationToken());

        }

    }

    [DataContract]
    public class DataGatewaySubstitution
    {
        [DataMember]
        public string host { get; set; }
        [DataMember]
        public string oldvalue { get; set; }
        [DataMember]
        public string substitute { get; set; }
    }

    [DataContract]
	public class StoreShareUser {
		[DataMember]
        public string username { get; set; }
	}

    [DataContract]
	public class StoreShareCommunity {
        [DataMember]
		public string identifier { get; set; }
        [DataMember]
		public List<StoreShareUser> users { get; set; }
	}

    [DataContract]
	public class StoreShareRequest {
		[DataMember]
		public string origin { get; set; }
        [DataMember]
		public string type { get; set; }
        [DataMember]
		public string identifier { get; set; }
		[DataMember]
		public string visibility { get; set; }
        [DataMember]
		public List<StoreShareCommunity> communities { get; set; }
        [DataMember]
		public List<StoreShareUser> users { get; set; }
	}

}
