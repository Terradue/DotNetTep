using System;
using System.Net;
using System.Runtime.Caching;
using Terradue.OpenSearch;
using Terradue.OpenSearch.Result;
using Terradue.ServiceModel.Syndication;
using Terradue.Tep.OpenSearch;

namespace Terradue.Tep
{
    public class DataGatewayFactory
    {


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

        static Uri FindSubstituteUri(Uri baseUri)
        {

            var cacheItem = downloadUriCache.GetCacheItem(baseUri.ToString());

            if (cacheItem != null)
                return (Uri)cacheItem.Value;

            return SearchAndCacheDataGatewayRepo(baseUri);

        }

        static Uri SearchAndCacheDataGatewayRepo(Uri baseUri)
        {
            UriBuilder urib = new UriBuilder(dataGatewayBaseUrl);

            urib.Path += string.Format("/{0}", baseUri.Host);

            HttpWebRequest httpRequest = (HttpWebRequest)HttpWebRequest.Create(urib.Uri);

            try
            {
                using (var resp = httpRequest.GetResponse())
                {
                }
            }
            catch (Exception)
            {
                return null;
            }

            downloadUriCache.Set(new CacheItem(baseUri.ToString(), urib.Uri), new CacheItemPolicy() { AbsoluteExpiration = DateTimeOffset.Now.AddHours(1) });

            return urib.Uri;
        }

        static SyndicationLink SubstituteSandboxEnclosure(SyndicationLink link, SandboxOpenSearchable sandboxOpenSearchable, IOpenSearchResultItem item)
        {
            UriBuilder urib = new UriBuilder (link.Uri.AbsoluteUri);
            urib.Path = "";
            urib.Query = null;
            urib.Fragment = null;

            var substUri = FindSubstituteUri(urib.Uri);

            if (substUri == null)
                return null;

            urib = new UriBuilder(substUri);
            urib.Path += string.Format("/workflows/{0}/runs/{1}", sandboxOpenSearchable.SandboxOpenSearchInformation.Workflow,
                                       item.Identifier);


            return new SyndicationLink(urib.Uri, "enclosure", link.Title + " via Data Gateway", link.MediaType, link.Length);
        }
    }
}
