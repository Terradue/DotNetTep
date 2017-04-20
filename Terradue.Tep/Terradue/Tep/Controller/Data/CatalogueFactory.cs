using System;
using System.IO;
using System.Net;
using Terradue.OpenSearch.Result;
using Terradue.Portal;

namespace Terradue.Tep {
    public class CatalogueFactory {

        public static bool PostAtomFeedToIndex(IfyContext context, AtomFeed feed, string index) {
            var baseurl = context.GetConfigValue("catalog-baseurl");
            var username = context.GetConfigValue("catalog-admin-username");
            var apikey = context.GetConfigValue("catalog-admin-apikey");
            var request = (HttpWebRequest)WebRequest.Create(baseurl + "/" + index + "/");
            request.Method = "POST";
            request.ContentType = "application/atom+xml";
            request.Accept = "application/xml";
            request.Proxy = null;
            request.UserAgent = "curl";
            request.PreAuthenticate = true;
            request.Credentials = new NetworkCredential(username, apikey);

            using (var stream = request.GetRequestStream()) {
                feed.SerializeToStream(stream);

                using (var response = (HttpWebResponse)request.GetResponse()) {
                    using (var streamReader = new StreamReader(response.GetResponseStream())) {
                        string result = streamReader.ReadToEnd();
                        context.LogDebug(context, "PostAtomFeedToIndex -- " + result);
                    }
                }
            }
            return true;
        }
    }
}
