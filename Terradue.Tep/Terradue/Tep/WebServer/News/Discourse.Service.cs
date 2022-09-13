using System;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using ServiceStack.ServiceHost;
using Terradue.OpenSearch;
using Terradue.Portal;

namespace Terradue.Tep.WebServer.Services {
    [Api("Tep Terradue webserver")]
    [Restrict(EndpointAttributes.InSecure | EndpointAttributes.InternalNetworkAccess | EndpointAttributes.Json,
              EndpointAttributes.Secure | EndpointAttributes.External | EndpointAttributes.Json)]
    public class DiscourseServiceTep : ServiceStack.ServiceInterface.Service {

        private static readonly log4net.ILog log = log4net.LogManager.GetLogger
            (System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public object Get(GetDiscourseTopicsPerCategory request){
            log.InfoFormat("/discourse/c/{{catId}} GET catId='{0}',page='{1}',order='{2}'", request.category, request.page, request.order);
            return GetDiscourseRequest(string.Format("c/{0}.json", request.category), request.page, request.order); 
        }

        public object Get(GetDiscourseLatestTopicsPerCategory request){
            log.InfoFormat("/discourse/c/{{catId}}/l/latest GET catId='{0}',page='{1}',order='{2}'", request.category, request.page, request.order);
            return GetDiscourseRequest(string.Format("c/{0}/l/latest.json", request.category), request.page, request.order);
        }

        public object Get(GetDiscourseTopTopicsPerCategory request) {
            log.InfoFormat("/discourse/c/{{catId}}/l/top GET catId='{0}',page='{1}',order='{2}'", request.category, request.page, request.order);
            return GetDiscourseRequest(string.Format("c/{0}/l/top.json", request.category), request.page, request.order);
        }

        public object Get(GetDiscourseTopic request){
            log.InfoFormat("/discourse/t/{{topicId}} GET topicId='{0}'", request.topicId);
            return GetDiscourseRequest(string.Format("t/{0}.json", request.topicId));
        }

        public object Get(GetDiscourseSearchTopicsPerCategory request){
            log.InfoFormat("/discourse/c/{{catId}} GET catId='{0}'", request.category);
            return GetDiscourseRequest(string.Format("search.json?q={0}%20category%3A{1}", request.q, request.category));
        }
            
        private object GetDiscourseRequest(string jsonPath, string page = null, string order = null){

            ServicePointManager.ServerCertificateValidationCallback = delegate(
                Object obj, System.Security.Cryptography.X509Certificates.X509Certificate certificate, System.Security.Cryptography.X509Certificates.X509Chain chain, 
                System.Net.Security.SslPolicyErrors errors)
            {
                return (true);
            };

            var context = TepWebContext.GetWebContext(PagePrivileges.EverybodyView);
            context.Open();
            var discourseBaseUrl = context.GetConfigValue("discussBaseUrl");
            context.Close();
            var urib = new UriBuilder(string.Format("{0}/{1}", discourseBaseUrl, jsonPath));
            var nvc = new NameValueCollection();
            nvc.Set("page", page);
            nvc.Set("order", order);
            urib.Query = string.Join("&", nvc.AllKeys.Where(key => !string.IsNullOrWhiteSpace(nvc[key])).Select(key => string.Format("{0}={1}", HttpUtility.UrlEncode(key), HttpUtility.UrlEncode(nvc[key]))));
            var discourseUrl = urib.Uri.AbsoluteUri;
            log.DebugFormat("Discourse url : {0}",discourseUrl);
            HttpWebRequest httprequest = (HttpWebRequest)WebRequest.Create(discourseUrl);
            httprequest.Proxy = null;
            httprequest.Method = "GET";
            httprequest.ContentType = "application/json";
            httprequest.Accept = "application/json";

            string text = null;
            try{
                text = System.Threading.Tasks.Task.Factory.FromAsync<WebResponse>(httprequest.BeginGetResponse,httprequest.EndGetResponse,null)
                .ContinueWith(task =>
                {
                    var httpResponse = (HttpWebResponse)task.Result;
                    using (var stream = httpResponse.GetResponseStream ()){
                        var reader = new StreamReader (stream, System.Text.Encoding.UTF8);
                        return reader.ReadToEnd ();
                    }
                }).ConfigureAwait(false).GetAwaiter().GetResult();
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

        public object Post(PostDiscourseTopic request) {
            var context = TepWebContext.GetWebContext(PagePrivileges.UserView);
            string result;
            try {
                context.Open();
                context.LogInfo(this, string.Format("/discourse/posts POST community='{0}'{1}{2}", 
                               request.communityIdentifier, 
                               !string.IsNullOrEmpty(request.subject) ? ", subject='"+request.subject+"'" : "",
                               !string.IsNullOrEmpty(request.body) ? ", body='" + request.body + "'" : ""
                                                   ));

                if (string.IsNullOrEmpty(request.subject)) throw new Exception("Unable to post new topic, subject is null");
                if (string.IsNullOrEmpty(request.body)) throw new Exception("Unable to post new topic, body is null");

                var community = ThematicCommunity.FromIdentifier(context, request.communityIdentifier);
                var discussCategory = community.DiscussCategory;
                if (string.IsNullOrEmpty(discussCategory)) throw new Exception("Unable to post new topic, the selected community has no Discuss category associated");

                var user = UserTep.FromId(context, context.UserId);
                if (string.IsNullOrEmpty(user.TerradueCloudUsername)) throw new Exception("Unable to post new topic, please set first your Terradue Cloud username");

                var discussClient = new DiscussClient(context.GetConfigValue("discussBaseUrl"), context.GetConfigValue("discussApiKey"), user.TerradueCloudUsername);
                var category = discussClient.GetCategory(discussCategory);
                if (category == null) throw new Exception("Unable to post new topic, the selected community has no valid Discuss category associated");
                var catId = category.id;

                var response = discussClient.PostTopic(catId, request.subject, request.body);                                                                 
                result = string.Format("{0}/t/{1}/{2}", discussClient.Host, response.topic_slug, response.topic_id);
            } catch (Exception e) {
                context.LogError(this, e.Message, e);
                context.Close ();
                throw e;
            }
            return new WebService.Model.WebResponseString(result);
        }

    }

    [Route("/discourse/c/latest", "GET", Summary = "", Notes = "")]
    public class GetDiscourseLatestTopicsPerCategory {
        [ApiMember(Name="category", Description = "request", ParameterType = "query", DataType = "string", IsRequired = true)]
        public string category{ get; set; }

        [ApiMember(Name="page", Description = "request", ParameterType = "query", DataType = "int", IsRequired = false)]
        public string page { get; set; }

        [ApiMember(Name="order", Description = "request", ParameterType = "query", DataType = "string", IsRequired = false)]
        public string order{ get; set; }
    }

    [Route("/discourse/c/top", "GET", Summary = "", Notes = "")]
    public class GetDiscourseTopTopicsPerCategory {
        [ApiMember(Name = "category", Description = "request", ParameterType = "query", DataType = "string", IsRequired = true)]
        public string category { get; set; }

        [ApiMember(Name="page", Description = "request", ParameterType = "query", DataType = "int", IsRequired = false)]
        public string page { get; set; }

        [ApiMember(Name="order", Description = "request", ParameterType = "query", DataType = "string", IsRequired = false)]
        public string order{ get; set; }
    }

    [Route("/discourse/c/search", "GET", Summary = "", Notes = "")]
    public class GetDiscourseSearchTopicsPerCategory {
        [ApiMember(Name="category", Description = "request", ParameterType = "query", DataType = "string", IsRequired = true)]
        public string category{ get; set; }

        [ApiMember(Name="q", Description = "query", ParameterType = "query", DataType = "string", IsRequired = false)]
        public string q{ get; set; }
    }

    [Route("/discourse/c", "GET", Summary = "", Notes = "")]
    public class GetDiscourseTopicsPerCategory {
        [ApiMember(Name="category", Description = "request", ParameterType = "query", DataType = "string", IsRequired = true)]
        public string category{ get; set; }

        [ApiMember(Name="page", Description = "request", ParameterType = "query", DataType = "int", IsRequired = false)]
        public string page { get; set; }

        [ApiMember(Name="order", Description = "request", ParameterType = "query", DataType = "string", IsRequired = false)]
        public string order{ get; set; }
    }

    [Route("/discourse/t/{topicId}", "GET", Summary = "", Notes = "")]
    public class GetDiscourseTopic {
        [ApiMember(Name="topicId", Description = "request", ParameterType = "query", DataType = "string", IsRequired = true)]
        public string topicId{ get; set; }
    }

    [Route("/discourse/posts", "POST", Summary = "", Notes = "")]
    public class PostDiscourseTopic {
        [ApiMember(Name = "communityIdentifier", Description = "request", ParameterType = "query", DataType = "string", IsRequired = true)]
        public string communityIdentifier { get; set; }

        //[ApiMember(Name = "entitySelf", Description = "request", ParameterType = "query", DataType = "string", IsRequired = true)]
        //public string entitySelf { get; set; }

        [ApiMember(Name = "subject", Description = "request", ParameterType = "query", DataType = "string", IsRequired = true)]
        public string subject { get; set; }

        [ApiMember(Name = "body", Description = "request", ParameterType = "query", DataType = "string", IsRequired = true)]
        public string body { get; set; }
    }
}

