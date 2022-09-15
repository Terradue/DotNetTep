using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using IO.Swagger.Model;
using OpenGis.Wps;
using Terradue.OpenSearch;
using Terradue.Portal;
using Terradue.ServiceModel.Ogc.Owc.AtomEncoding;

namespace Terradue.Tep {
    [EntityTable(null, EntityTableConfiguration.Custom, Storage = EntityTableStorage.Above, AllowsKeywordSearch = true)]
    public class WpsProviderTep : WpsProvider {

        public WpsProviderTep(IfyContext context) : base(context) {
        }

        public static new WpsProviderTep FromIdentifier(IfyContext context, string identifier) {
            var p = new WpsProviderTep(context);
            p.Identifier = identifier;
            p.Load();
            return p;
        }

        /// <summary>
        /// Gets the wps process offerings from URL.
        /// </summary>
        /// <returns>The wps process offerings from URL.</returns>
        /// <param name="baseurl">Baseurl.</param>
        /// <param name="updateProviderInfo">If set to <c>true</c> update provider info.</param>
        public new List<WpsProcessOffering> GetWpsProcessOfferingsFromRemote(bool updateProviderInfo = false, string username = null) {
            List<WpsProcessOffering> wpsProcessList = new List<WpsProcessOffering>();

            if(!IsWPS3()){
                return base.GetWpsProcessOfferingsFromRemote(updateProviderInfo, username);
            } else {                
                List<Process> wps3List = new List<Process>();
                HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(this.BaseUrl);
                webRequest.Method = "GET";
                webRequest.Accept = "application/json";

                wps3List = System.Threading.Tasks.Task.Factory.FromAsync<WebResponse>(webRequest.BeginGetResponse,webRequest.EndGetResponse,null)
                .ContinueWith(task =>
                {
                    var httpResponse = (HttpWebResponse)task.Result;
                    using (var streamReader = new StreamReader(httpResponse.GetResponseStream())) 
                    {
                        string result = streamReader.ReadToEnd();
                        try {
                            return ServiceStack.Text.JsonSerializer.DeserializeFromString<List<Process>>(result);
                        } catch (System.Exception e) {
                            throw e;
                        }
                    }
                }).ConfigureAwait(false).GetAwaiter().GetResult();

                if (wps3List != null)
                {
                    foreach (Process wps3 in wps3List)
                    {
                        WpsProcessOffering process = new WpsProcessOffering(context);
                        process.Identifier = Guid.NewGuid().ToString();
                        process.RemoteIdentifier = wps3.Id;
                        process.Name = wps3.Title;
                        process.Description = wps3.Abstract ?? wps3.Title;
                        process.Version = wps3.Version;
                        process.Url = new Uri(this.BaseUrl + "/" + wps3.Id).AbsoluteUri;
                        wpsProcessList.Add(process);
                    }
                }
            }

            return wpsProcessList;
        }

        /***********/
        /* WPS 3.0 */
        /***********/

        /// <summary>
        /// Is this Service WPS 3.0
        /// </summary>
        /// <returns></returns>
        public bool IsWPS3() {
            return IsWPS3(this.BaseUrl);
        }


        public static bool IsWPS3(string url) {
            if (url == null) return false;
            return url.Contains("/wps3/");
        }
    }
}
