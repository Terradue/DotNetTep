using System;
using System.IO;
using System.Net;
using Terradue.OpenSearch.Result;
using Terradue.Portal;
using Terradue.ServiceModel.Ogc.Owc.AtomEncoding;
using Terradue.ServiceModel.Syndication;

namespace Terradue.Tep {
    public class CatalogueFactory {

        public static bool PostAtomFeedToIndex(IfyContext context, AtomFeed feed, string index) {
            var baseurl = context.GetConfigValue("catalog-baseurl");
            var url = index.StartsWith("http://") || index.StartsWith("https://") ? index : baseurl + "/" + index + "/";
            var username = context.GetConfigValue("catalog-admin-username");
            var apikey = context.GetConfigValue("catalog-admin-apikey");
            var request = (HttpWebRequest)WebRequest.Create(url);
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

        //     public static bool PostAtomFeedToIndex(IfyContext context, AtomFeed feed, string index) {
        //var username = context.GetConfigValue("catalog-admin-username");
        //var apikey = context.GetConfigValue("catalog-admin-apikey");
        //    using (var stream = new MemoryStream()) {
        //        feed.SerializeToStream(stream);
        //        stream.Seek(0, SeekOrigin.Begin);
        //        return PostStreamToIndex(context, stream, index, username, apikey);
        //    }
        //}

        public static bool PostAtomFeedToIndex(IfyContext context, OwsContextAtomFeed feed, string index, string username, string apikey) {
            using (var stream = new MemoryStream()) {
                var sw = System.Xml.XmlWriter.Create(stream);
                Atom10FeedFormatter atomFormatter = new Atom10FeedFormatter((SyndicationFeed)feed);
                atomFormatter.WriteTo(sw);
                sw.Flush();
                sw.Close();
                stream.Seek(0, SeekOrigin.Begin);
                return PostStreamToIndex(context, stream, index, username, apikey);
            }
        }

        public static bool PostStreamToIndex(IfyContext context, Stream stream, string index, string username, string apikey) {
            var baseurl = context.GetConfigValue("catalog-baseurl");
            var url = index.StartsWith("http://") || index.StartsWith("https://") ? index : baseurl + "/" + index + "/";
            var request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "POST";
            request.ContentType = "application/atom+xml";
            request.Accept = "application/xml";
            request.Proxy = null;
            request.UserAgent = "curl";
            request.PreAuthenticate = true;
            request.Credentials = new NetworkCredential(username, apikey);
            using (var s = request.GetRequestStream()) {
                stream.CopyTo(s);

                using (var response = (HttpWebResponse)request.GetResponse()) {
                    using (var streamReader = new StreamReader(response.GetResponseStream())) {
                        string result = streamReader.ReadToEnd();
                        context.LogDebug(context, "PostStreamToIndex -- " + result);
                    }
                }
            }
            return true;
        }
    }
}
