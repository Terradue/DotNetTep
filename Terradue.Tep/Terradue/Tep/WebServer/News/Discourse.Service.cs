using System;
using System.IO;
using System.Net;
using ServiceStack.ServiceHost;

namespace Terradue.Tep.WebServer.Services {
    [Api("Tep Terradue webserver")]
    [Restrict(EndpointAttributes.InSecure | EndpointAttributes.InternalNetworkAccess | EndpointAttributes.Json,
              EndpointAttributes.Secure | EndpointAttributes.External | EndpointAttributes.Json)]
    public class DiscourseServiceTep : ServiceStack.ServiceInterface.Service {

        private static readonly log4net.ILog log = log4net.LogManager.GetLogger
            (System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public object Get(GetDiscourseTopicsPerCategory request){
            log.InfoFormat("/discourse/c/{{catId}} GET catId='{0}',page='{1}',order='{2}'", request.catId, request.page, request.order);
            return GetDiscourseRequest(string.Format("c/{0}.json?page={1}&order={2}", request.catId, request.page, request.order ?? "activity")); 
        }

        public object Get(GetDiscourseLatestTopicsPerCategory request){
            log.InfoFormat("/discourse/c/{{catId}}/l/latest GET catId='{0}'", request.catId);
            return GetDiscourseRequest(string.Format("c/{0}/l/latest.json", request.catId));
        }

        public object Get(GetDiscourseTopic request){
            log.InfoFormat("/discourse/t/{{topicId}} GET topicId='{0}'", request.topicId);
            return GetDiscourseRequest(string.Format("t/{0}.json", request.topicId));
        }

        public object Get(GetDiscourseSearchTopicsPerCategory request){
            log.InfoFormat("/discourse/c/{{catId}} GET catId='{0}'", request.catId);
            return GetDiscourseRequest(string.Format("search.json?q={0}%20category%3A{1}", request.q, request.catId));
        }
            
        private object GetDiscourseRequest(string query){

            ServicePointManager.ServerCertificateValidationCallback = delegate(
                Object obj, System.Security.Cryptography.X509Certificates.X509Certificate certificate, System.Security.Cryptography.X509Certificates.X509Chain chain, 
                System.Net.Security.SslPolicyErrors errors)
            {
                return (true);
            };

            var discourseBaseUrl = "https://discuss.terradue.com";
            var discourseUrl = string.Format("{0}/{1}", discourseBaseUrl, query);
            log.DebugFormat("Discourse url : {0}",discourseUrl);
            HttpWebRequest httprequest = (HttpWebRequest)WebRequest.Create(discourseUrl);
            httprequest.Method = "GET";
            httprequest.ContentType = "application/json";
            httprequest.Accept = "application/json";

            Stream stream;
            try{
                using (var httpResponse = (HttpWebResponse)httprequest.GetResponse ()) {
                    stream = httpResponse.GetResponseStream ();
                }
            }catch(WebException e){
                var reader = new System.IO.StreamReader(e.Response.GetResponseStream());
                string text = reader.ReadToEnd();
                log.ErrorFormat(text);
                throw e;
            }catch(Exception e){
                log.ErrorFormat("{0} - {1}",e.Message, e.StackTrace);
                throw e;
            }
            return stream;
        }

    }

    [Route("/discourse/c/{catId}/l/latest", "GET", Summary = "", Notes = "")]
    public class GetDiscourseLatestTopicsPerCategory {
        [ApiMember(Name="catId", Description = "request", ParameterType = "query", DataType = "int", IsRequired = true)]
        public int catId{ get; set; }
    }

    [Route("/discourse/c/{catId}/search", "GET", Summary = "", Notes = "")]
    public class GetDiscourseSearchTopicsPerCategory {
        [ApiMember(Name="catId", Description = "request", ParameterType = "query", DataType = "int", IsRequired = true)]
        public int catId{ get; set; }

        [ApiMember(Name="q", Description = "query", ParameterType = "query", DataType = "string", IsRequired = false)]
        public string q{ get; set; }
    }

    [Route("/discourse/c/{catId}", "GET", Summary = "", Notes = "")]
    public class GetDiscourseTopicsPerCategory {
        [ApiMember(Name="catId", Description = "request", ParameterType = "query", DataType = "int", IsRequired = true)]
        public int catId{ get; set; }

        [ApiMember(Name="page", Description = "request", ParameterType = "query", DataType = "int", IsRequired = false)]
        public int page{ get; set; }

        [ApiMember(Name="order", Description = "request", ParameterType = "query", DataType = "string", IsRequired = false)]
        public string order{ get; set; }
    }

    [Route("/discourse/t/{topicId}", "GET", Summary = "", Notes = "")]
    public class GetDiscourseTopic {
        [ApiMember(Name="topicId", Description = "request", ParameterType = "query", DataType = "int", IsRequired = true)]
        public int topicId{ get; set; }
    }
}

