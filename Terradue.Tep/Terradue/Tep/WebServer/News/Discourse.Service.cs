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
            log.InfoFormat("/discourse/c/{{catId}} GET catId='{0}',page='{1}',order='{2}'", request.category, request.page, request.order);
            return GetDiscourseRequest(string.Format("c/{0}.json?page={1}&order={2}", request.category, request.page, request.order ?? "activity")); 
        }

        public object Get(GetDiscourseLatestTopicsPerCategory request){
            log.InfoFormat("/discourse/c/{{catId}}/l/latest GET catId='{0}'", request.category);
            return GetDiscourseRequest(string.Format("c/{0}/l/latest.json", request.category));
        }

        public object Get(GetDiscourseTopTopicsPerCategory request) {
            log.InfoFormat("/discourse/c/{{catId}}/l/top GET catId='{0}'", request.category);
            return GetDiscourseRequest(string.Format("c/{0}/l/top.json", request.category));
        }

        public object Get(GetDiscourseTopic request){
            log.InfoFormat("/discourse/t/{{topicId}} GET topicId='{0}'", request.topicId);
            return GetDiscourseRequest(string.Format("t/{0}.json", request.topicId));
        }

        public object Get(GetDiscourseSearchTopicsPerCategory request){
            log.InfoFormat("/discourse/c/{{catId}} GET catId='{0}'", request.category);
            return GetDiscourseRequest(string.Format("search.json?q={0}%20category%3A{1}", request.q, request.category));
        }
            
        private object GetDiscourseRequest(string query){

            ServicePointManager.ServerCertificateValidationCallback = delegate(
                Object obj, System.Security.Cryptography.X509Certificates.X509Certificate certificate, System.Security.Cryptography.X509Certificates.X509Chain chain, 
                System.Net.Security.SslPolicyErrors errors)
            {
                return (true);
            };

            var discourseBaseUrl = "http://discuss.terradue.com";
            var discourseUrl = string.Format("{0}/{1}", discourseBaseUrl, query);
            log.DebugFormat("Discourse url : {0}",discourseUrl);
            HttpWebRequest httprequest = (HttpWebRequest)WebRequest.Create(discourseUrl);
            httprequest.Proxy = null;
            httprequest.Method = "GET";
            httprequest.ContentType = "application/json";
            httprequest.Accept = "application/json";

            string text = null;
            try{
                using (var httpResponse = (HttpWebResponse)httprequest.GetResponse ()) {
                    using (var stream = httpResponse.GetResponseStream ()){
                        var reader = new StreamReader (stream, System.Text.Encoding.UTF8);
                        text = reader.ReadToEnd ();
                    }
                }
                return text;
            }catch(WebException e){
                if (e.Response != null) {
                    using (var stream = e.Response.GetResponseStream ()) {
                        var reader = new StreamReader (stream, System.Text.Encoding.UTF8);
                        text = reader.ReadToEnd ();
                    }
                    log.ErrorFormat (text);
                }
                throw e;
            }catch(Exception e){
                log.ErrorFormat("{0} - {1}",e.Message, e.StackTrace);
                throw e;
            }
        }

    }

    [Route("/discourse/c/latest", "GET", Summary = "", Notes = "")]
    public class GetDiscourseLatestTopicsPerCategory {
        [ApiMember(Name="category", Description = "request", ParameterType = "query", DataType = "int", IsRequired = true)]
        public string category{ get; set; }
    }

    [Route("/discourse/c/top", "GET", Summary = "", Notes = "")]
    public class GetDiscourseTopTopicsPerCategory {
        [ApiMember(Name = "category", Description = "request", ParameterType = "query", DataType = "int", IsRequired = true)]
        public string category { get; set; }
    }

    [Route("/discourse/c/search", "GET", Summary = "", Notes = "")]
    public class GetDiscourseSearchTopicsPerCategory {
        [ApiMember(Name="category", Description = "request", ParameterType = "query", DataType = "int", IsRequired = true)]
        public string category{ get; set; }

        [ApiMember(Name="q", Description = "query", ParameterType = "query", DataType = "string", IsRequired = false)]
        public string q{ get; set; }
    }

    [Route("/discourse/c", "GET", Summary = "", Notes = "")]
    public class GetDiscourseTopicsPerCategory {
        [ApiMember(Name="category", Description = "request", ParameterType = "query", DataType = "int", IsRequired = true)]
        public string category{ get; set; }

        [ApiMember(Name="page", Description = "request", ParameterType = "query", DataType = "int", IsRequired = false)]
        public int page{ get; set; }

        [ApiMember(Name="order", Description = "request", ParameterType = "query", DataType = "string", IsRequired = false)]
        public string order{ get; set; }
    }

    [Route("/discourse/t/{topicId}", "GET", Summary = "", Notes = "")]
    public class GetDiscourseTopic {
        [ApiMember(Name="topicId", Description = "request", ParameterType = "query", DataType = "int", IsRequired = true)]
        public string topicId{ get; set; }
    }
}

