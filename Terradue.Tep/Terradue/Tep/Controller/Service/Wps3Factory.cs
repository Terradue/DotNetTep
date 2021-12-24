using System;
using OpenGis.Wps;
using Terradue.Portal;
using System.Collections.Generic;
using System.Net;
using System.IO;
using ServiceStack.Common.Web;
using System.Xml.Serialization;
using System.Runtime.Caching;
using System.Linq;

namespace Terradue.Tep {

    public interface IWps3Factory {
        string GetResultDescriptionFromS3Link(IfyContext context, WpsJob job, string s3link);
    }
    public class Wps3Factory : IWps3Factory {

        protected IfyContext context;
        public Wps3Factory(IfyContext context){
            this.context = context;
        }

        public string GetResultDescriptionFromS3Link(IfyContext context, WpsJob job, string s3link){
            var resultdescription = s3link;
            if (System.Configuration.ConfigurationManager.AppSettings["SUPERVISOR_WPS_STAGE_URL"] != null && !string.IsNullOrEmpty(s3link)) {
                var url = System.Configuration.ConfigurationManager.AppSettings["SUPERVISOR_WPS_STAGE_URL"];
                HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(url);                            
                if (!string.IsNullOrEmpty(System.Configuration.ConfigurationManager.AppSettings["ProxyHost"])) webRequest.Proxy = TepUtility.GetWebRequestProxy();
                var access_token = DBCookie.LoadDBCookie(context, System.Configuration.ConfigurationManager.AppSettings["SUPERVISOR_COOKIE_TOKEN_ACCESS"]).Value;
                webRequest.Headers.Set(HttpRequestHeader.Authorization, "Bearer " + access_token);
                webRequest.Timeout = 10000;
                webRequest.Method = "POST";
                webRequest.ContentType = "application/json";

                var shareUri = job.GetJobShareUri(job.AppIdentifier);
                var publishlink = new Wps3Utils.SyndicationLink {
                    Href = shareUri.AbsoluteUri,
                    Rel = "external",
                    Type = "text/html",
                    Title = "Producer Link",
                    Attributes = new List<KeyValuePair<string, string>> { new KeyValuePair<string, string>("level", "primary") }
                };
                context.LogDebug(job, string.Format("publish request to supervisor - s3link = {0} ; jobUrl = {1} ; index = {2}", s3link, shareUri.AbsoluteUri, job.Owner.Username));
                string authBasicHeader = null;
                try {
                    var apikey = job.Owner.LoadApiKeyFromRemote();
                    authBasicHeader = "Basic " + Convert.ToBase64String(System.Text.Encoding.Default.GetBytes(job.Owner.Username + ":" + apikey));
                }catch(Exception e) {
                    context.LogError(this, "Error get apikey : " + e.Message);
                }

                var jsonurl = new SupervisorPublish
                {
                    Url = s3link,
                    AuthorizationHeader = authBasicHeader,
                    Index = job.Owner.Username,
                    Categories = new List<Wps3Utils.SyndicationCategory>{
                        new Wps3Utils.SyndicationCategory { Name = "appId", Label = job.AppIdentifier }
                    },
                    Links = new List<Wps3Utils.SyndicationLink>{
                        publishlink
                    }
                };

                var json = ServiceStack.Text.JsonSerializer.SerializeToString(jsonurl);
                context.LogDebug(this, string.Format("publish request to supervisor - json = {0}", json));

                try {
                    using (var streamWriter = new StreamWriter(webRequest.GetRequestStream())) {
                        streamWriter.Write(json);
                        streamWriter.Flush();
                        streamWriter.Close();

                        using (var httpResponse = (HttpWebResponse)webRequest.GetResponse()) {
                            using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                            {
                                var location = httpResponse.Headers["Location"];
                                if (!string.IsNullOrEmpty(location))
                                {
                                    context.LogDebug(this, "location = " + location);
                                    resultdescription = new Uri(location, UriKind.RelativeOrAbsolute).AbsoluteUri;
                                }
                            }
                        }
                    }
                } catch (Exception e) {
                    context.LogError(job, "Error Create user product request to supervisor: " + e.Message);
                }                
            }
            return resultdescription;
        }

        // public string GetResultDescriptionFromS3Link(IfyContext context, WpsJob job, string s3link){
        //     var resultdescription = s3link;
        //     if (System.Configuration.ConfigurationManager.AppSettings["SUPERVISOR_WPS_STAGE_URL"] != null && !string.IsNullOrEmpty(s3link)) {
        //         var url = System.Configuration.ConfigurationManager.AppSettings["SUPERVISOR_WPS_STAGE_URL"].Replace("{USER}", job.Owner.Username);
        //         HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(url);                            
        //         if (!string.IsNullOrEmpty(System.Configuration.ConfigurationManager.AppSettings["ProxyHost"])) webRequest.Proxy = TepUtility.GetWebRequestProxy();
        //         var access_token = DBCookie.LoadDBCookie(context, System.Configuration.ConfigurationManager.AppSettings["SUPERVISOR_COOKIE_TOKEN_ACCESS"]).Value;
        //         webRequest.Headers.Set(HttpRequestHeader.Authorization, "Bearer " + access_token);
        //         webRequest.Timeout = 10000;
        //         webRequest.Method = "POST";
        //         webRequest.ContentType = "application/json";

        //         var shareUri = job.GetJobShareUri(job.AppIdentifier);

        //         var importProduct = new SupervisorUserImportProduct {
        //             Url = s3link,
        //             ActivationId = int.Parse(job.AppIdentifier.Substring(job.AppIdentifier.LastIndexOf("-") + 1)),//TODO: to be changed later
        //             AdditionalLinks = new List<SupervisorUserImportProductLink> {
        //                 new SupervisorUserImportProductLink {
        //                     Href = shareUri.AbsoluteUri,
        //                     Rel = "external",
        //                     Type = "text/html",
        //                     Title = "Producer Link"
        //                 }
        //             }
        //         };
        //         var json = Newtonsoft.Json.JsonConvert.SerializeObject(importProduct, Newtonsoft.Json.Formatting.None);

        //         context.LogDebug(job, string.Format("Create user product request to supervisor - s3link = {0} ; username = {1}", s3link, job.Owner.Username));                            
        //         context.LogDebug(job, string.Format("send request to supervisor - json = {0}", json));

        //         try {
        //             using (var streamWriter = new StreamWriter(webRequest.GetRequestStream())) {
        //                 streamWriter.Write(json);
        //                 streamWriter.Flush();
        //                 streamWriter.Close();

        //                 using (var httpResponse = (HttpWebResponse)webRequest.GetResponse()) {
        //                     using (var stream = httpResponse.GetResponseStream()) {
        //                         var stacItem = ServiceStack.Text.JsonSerializer.DeserializeFromStream<StacItem>(stream);                                            
        //                         var stacLink = stacItem.Links.First(l => l.Rel == "alternate");
        //                         resultdescription = stacLink.Href;
        //                     }
        //                 }
        //             }
        //         } catch (Exception e) {
        //             context.LogError(job, "Error Create user product request to supervisor: " + e.Message);
        //         }                
        //     }
        //     return resultdescription;
        // }
    }
}

