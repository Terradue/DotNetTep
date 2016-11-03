using System;
using System.Net;
using System.Runtime.Caching;
using System.Text.RegularExpressions;
using Terradue.OpenSearch;
using Terradue.OpenSearch.Result;
using Terradue.ServiceModel.Syndication;
using Terradue.Tep.OpenSearch;

namespace Terradue.Tep
{
    public class DataGatewayFactory
    {

        private static readonly log4net.ILog log = log4net.LogManager.GetLogger
            (System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        static string dataGatewayBaseUrl = "https://store.terradue.com";
        static MemoryCache downloadUriCache = new MemoryCache("downloadUriCache");


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
            UriBuilder urib = new UriBuilder(link.BaseUri);

            urib.Path = "";
            urib.Query = null;
            urib.Fragment = null;

            var substUri = FindSubstituteUri(urib.Uri);

            if (substUri == null)
                return null;

            urib = new UriBuilder(substUri);
            urib.Path += link.BaseUri.AbsolutePath;

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
            urib.Path += RewritePath(url.AbsolutePath, openSearchable);

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

            HttpWebRequest httpRequest = (HttpWebRequest)HttpWebRequest.Create(urib.Uri);
            httpRequest.Headers.Add("X-JFrog-Art-Api", "AKCp2V68vr2SNikpe5FoXFoxDk2PwkZoRGXCWi56yUDDa4S4c5U1yi6qUJKZXYxP9imviGUwf");

            try
            {
                using (var resp = httpRequest.GetResponse())
                {
                }
            }
            catch (Exception e)
            {
                log.WarnFormat("{0} not in data gateway at {1} : {2}", baseUri.Host, urib, e.Message);
                return null;
            }

            urib = new UriBuilder(dataGatewayBaseUrl);
            urib.Path += string.Format("/api/production/");

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
                    return path = string.Format("/workflows/{0}/runs/{1}/{2}", sandboxOpenSearchable.SandboxOpenSearchInformation.Workflow,
                                              sandboxOpenSearchable.SandboxOpenSearchInformation.RunId,
                                              match.Groups["relativeFilename"].Value);
            }

            return path;
        }

    }
}
