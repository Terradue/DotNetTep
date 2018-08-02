using System;
using System.IO;
using System.Net;
using Terradue.OpenSearch.Result;
using Terradue.Portal;
using Terradue.ServiceModel.Ogc.Owc.AtomEncoding;
using Terradue.ServiceModel.Syndication;

namespace Terradue.Tep {
    public class CatalogueFactory {

        /// <summary>
        /// Posts the index of the atom feed to.
        /// </summary>
        /// <returns><c>true</c>, if atom feed to index was posted, <c>false</c> otherwise.</returns>
        /// <param name="context">Context.</param>
        /// <param name="feed">Feed.</param>
        /// <param name="index">Index.</param>
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

        /// <summary>
        /// Posts the index of the atom feed to.
        /// </summary>
        /// <returns><c>true</c>, if atom feed to index was posted, <c>false</c> otherwise.</returns>
        /// <param name="context">Context.</param>
        /// <param name="feed">Feed.</param>
        /// <param name="index">Index.</param>
        /// <param name="username">Username.</param>
        /// <param name="apikey">Apikey.</param>
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

        /// <summary>
        /// Posts the index of the stream to.
        /// </summary>
        /// <returns><c>true</c>, if stream to index was posted, <c>false</c> otherwise.</returns>
        /// <param name="context">Context.</param>
        /// <param name="stream">Stream.</param>
        /// <param name="index">Index.</param>
        /// <param name="username">Username.</param>
        /// <param name="apikey">Apikey.</param>
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

        /// <summary>
        /// Checks the identifier availability on index.
        /// </summary>
        /// <returns><c>true</c>, if identifier exists on index, <c>false</c> otherwise.</returns>
        /// <param name="context">Context.</param>
        /// <param name="index">Index.</param>
        /// <param name="identifier">Identifier.</param>
        /// <param name="apikey">Apikey.</param>
		public static bool CheckIdentifierExists(IfyContext context, string index, string identifier, string apikey){
			var baseurl = context.GetConfigValue("catalog-baseurl");
			var url = (index.StartsWith("http://") || index.StartsWith("https://") ? index : baseurl + "/" + index) + "/search?uid=" + identifier + "&apikey=" + apikey;
            
			bool result = false;
			try {
                HttpWebRequest httpRequest = (HttpWebRequest)WebRequest.Create(url);
                using (var resp = httpRequest.GetResponse()) {
                    using (var stream = resp.GetResponseStream()) {
						var feed = ThematicAppCachedFactory.GetOwsContextAtomFeed(stream);
                        if (feed.Items != null) {
                            foreach (OwsContextAtomEntry item in feed.Items) {
								result = true;
                            }
                        }
                    }
                }
            } catch (Exception e) {
				context.LogError(context, string.Format("CheckIdentifierExists -- {0} - {1}", e.Message, e.StackTrace));
            }
			return result;
		}

        /// <summary>
        /// Ises the URL opensearch description.
        /// </summary>
        /// <returns><c>true</c>, if URL opensearch description was ised, <c>false</c> otherwise.</returns>
        /// <param name="url">URL.</param>
		public static bool IsUrlOpensearchDescription(string url) {
            if (string.IsNullOrEmpty(url)) return false;
            if (url.Contains("/description") || url.Contains("/describe")) return true;
            return false;
		}

        /// <summary>
        /// Ises the URL opensearch search.
        /// </summary>
        /// <returns><c>true</c>, if URL opensearch search was ised, <c>false</c> otherwise.</returns>
        /// <param name="url">URL.</param>
        public static bool IsUrlOpensearchSearch(string url) {
            if (string.IsNullOrEmpty(url)) return false;
            if (url.Contains("/search")) return true;
            return false;
        }

        /// <summary>
        /// Ises the URL list series.
        /// </summary>
        /// <returns><c>true</c>, if URL list series was ised, <c>false</c> otherwise.</returns>
        /// <param name="url">URL.</param>
        public static bool IsUrlListSeries(string url) {
            if (string.IsNullOrEmpty(url)) return false;
            if (url.StartsWith("file:///t2api") && url.Contains("/search")) return true;
            return false;
        }
	}
}
