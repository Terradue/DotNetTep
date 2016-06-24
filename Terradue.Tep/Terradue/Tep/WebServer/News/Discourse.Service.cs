using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using ServiceStack.ServiceHost;
using System.Net;

namespace Terradue.Tep.WebServer.Services {
    [Api("Tep Terradue webserver")]
    [Restrict(EndpointAttributes.InSecure | EndpointAttributes.InternalNetworkAccess | EndpointAttributes.Json,
              EndpointAttributes.Secure | EndpointAttributes.External | EndpointAttributes.Json)]
    public class DiscourseServiceTep : ServiceStack.ServiceInterface.Service {

        public object Get(GetDiscourseTopicsPerCategory request){
            return GetDiscourseRequest(string.Format("c/{0}.json?page={1}&order={2}", request.catId, request.page, request.order ?? "activity")); 
        }

        public object Get(GetDiscourseLatestTopicsPerCategory request){
            return GetDiscourseRequest(string.Format("c/{0}/l/latest.json", request.catId));
        }

        public object Get(GetDiscourseTopic request){
            return GetDiscourseRequest(string.Format("t/{0}.json", request.topicId));
        }

        private object GetDiscourseRequest(string query){
            var discourseBaseUrl = "https://discuss.terradue.com";
            var discourseUrl = string.Format("{0}/{1}", discourseBaseUrl, query);
            HttpWebRequest httprequest = (HttpWebRequest)WebRequest.Create(discourseUrl);
            httprequest.Method = "GET";
            httprequest.ContentType = "application/json";
            httprequest.Accept = "application/json";

            HttpWebResponse httpResponse = (HttpWebResponse)httprequest.GetResponse();
            return httpResponse.GetResponseStream();
        }

    }

    [Route("/discourse/c/{catId}/l/latest", "GET", Summary = "", Notes = "")]
    public class GetDiscourseLatestTopicsPerCategory {
        [ApiMember(Name="catId", Description = "request", ParameterType = "query", DataType = "int", IsRequired = true)]
        public int catId{ get; set; }
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

