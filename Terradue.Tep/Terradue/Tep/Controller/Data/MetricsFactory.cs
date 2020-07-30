using System;
using System.IO;
using System.Net;
using System.Runtime.Serialization;
using ServiceStack.Text;
using Terradue.Portal;

namespace Terradue.Tep {
    public class MetricsFactory {

        public static void SendPublishNotification(IfyContext context, string identifier, string username, string joburl, string statusLocation, bool publish) {
            var metricsUrl = context.GetConfigValue("metrics-job-publish-url");

            //if url not set, we do nothing
            if (string.IsNullOrEmpty(metricsUrl)) return;
            
            try {
                var request = (HttpWebRequest)WebRequest.Create(metricsUrl);
                request.Method = "PUT";
                request.ContentType = "application/json";
                request.Accept = "application/json";

                var jobInfo = new MetricsJobPublishInfo {
                    portal = context.GetConfigValue("BaseUrl"),
                    jobId = identifier,
                    username = username,
                    url = joburl,
                    result_url = statusLocation,
                    publish = publish
                };

                string json = JsonSerializer.SerializeToString<MetricsJobPublishInfo>(jobInfo);
                            
                using (var streamWriter = new StreamWriter(request.GetRequestStream())) {
                    streamWriter.Write(json);
                    streamWriter.Flush();
                    streamWriter.Close();

                    using (var httpResponse = (HttpWebResponse)request.GetResponse()) {
                        using (var streamReader = new StreamReader(httpResponse.GetResponseStream())) {
                            string result = streamReader.ReadToEnd();
                            context.LogInfo(context, "Response from Metrics reporting system: " + result);
                        }
                    }
                }
            }catch(Exception e) {
                context.LogError(context, "Response from Metrics reporting system: " + e.Message);
            }
        }
    }

    [DataContract]
    public class MetricsJobPublishInfo {
        [DataMember]
        public string portal { get; set; }
        [DataMember]
        public string jobId { get; set; }
        [DataMember]
        public string username { get; set; }
        [DataMember]
        public string url { get; set; }
        [DataMember]
        public string result_url { get; set; }
        [DataMember]
        public bool publish { get; set; }
    }
}
