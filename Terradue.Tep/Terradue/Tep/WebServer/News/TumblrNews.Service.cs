using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using ServiceStack.Common.Web;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface;
using Terradue.News;
using Terradue.OpenSearch;
using Terradue.OpenSearch.Engine;
using Terradue.OpenSearch.Result;
using Terradue.OpenSearch.Tumblr;
using Terradue.Portal;
using Terradue.Tep.WebServer;
using Terradue.WebService.Model;

namespace Terradue.Tep.WebServer.Services {
    [Api("Tep Terradue webserver")]
    [Restrict(EndpointAttributes.InSecure | EndpointAttributes.InternalNetworkAccess | EndpointAttributes.Json,
              EndpointAttributes.Secure | EndpointAttributes.External | EndpointAttributes.Json)]
    public class TumblrNewsServiceTep : ServiceStack.ServiceInterface.Service {

        private static readonly log4net.ILog log = log4net.LogManager.GetLogger
            (System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        [AddHeader(ContentType="application/atom+xml")]
        public object Get(SearchTumblrNews request) {
            var context = TepWebContext.GetWebContext(PagePrivileges.EverybodyView);
            IOpenSearchResultCollection result = null;
            try{
                context.Open();
                context.LogInfo(this,string.Format("/news/tumblr/search GET"));

                // Load the complete request
                HttpRequest httpRequest = HttpContext.Current.Request;

                OpenSearchEngine ose = MasterCatalogue.OpenSearchEngine;

                Type type = OpenSearchFactory.ResolveTypeFromRequest(httpRequest, ose);

                List<TumblrFeed> tumblrs = TumblrNews.LoadTumblrFeeds(context);

                MultiGenericOpenSearchable multiOSE = new MultiGenericOpenSearchable(tumblrs.Cast<IOpenSearchable>().ToList(), ose);

                result = ose.Query(multiOSE, httpRequest.QueryString, type);


                context.Close ();
            }catch(Exception e) {
                context.LogError(this, e.Message);
                context.Close ();
                throw e;
            }

            return new HttpResult(result.SerializeToString(), result.ContentType);
        }

        public object Get(GetTumblrNewsFeeds request) {
            var context = TepWebContext.GetWebContext(PagePrivileges.EverybodyView);
            List<WebNews> result = new List<WebNews>();
            try{
                context.Open();
                context.LogInfo(this,string.Format("/news/tumblr/feeds GET"));

                List<TumblrFeed> tumblrs = TumblrNews.LoadTumblrFeeds(context);
                List<TumblrNews> tumblrfeeds = new List<TumblrNews>();
                foreach(TumblrFeed tumblr in tumblrs) tumblrfeeds.AddRange(TumblrNews.FromFeeds(context, tumblr.GetFeeds()));
                int i=0;
                foreach(TumblrNews feed in tumblrfeeds){
                    WebNews wn = new WebNews(feed);
                    wn.Id = i++;
                    result.Add(wn);
                }
                
                context.Close ();
            }catch(Exception e) {
                context.LogError(this, e.Message);
                context.Close ();
                throw e;
            }

            return result;
        }

        public object Get(GetAllTumblrNews request) {
            var context = TepWebContext.GetWebContext(PagePrivileges.EverybodyView);
            List<WebNews> result = new List<WebNews>();
            try{
                context.Open();
                context.LogInfo(this,string.Format("/news/tumblr GET"));

                EntityList<TumblrNews> articles = new EntityList<TumblrNews>(context);
                articles.Load();
                foreach(TumblrNews article in articles) result.Add(new WebNews(article));

                context.Close ();
            }catch(Exception e) {
                context.LogError(this, e.Message);
                context.Close ();
                throw e;
            }

            return result;
        }

        public object Post(CreateTumblrNews request) {
            var context = TepWebContext.GetWebContext(PagePrivileges.EverybodyView);
            WebNews result = null;
            try{
                context.Open();
                context.LogInfo(this,string.Format("/news/tumblr POST Id='{0}'", request.Id));

                TumblrNews article = new TumblrNews(context);
                article = (TumblrNews)request.ToEntity(context, article);
                article.Store();
                result = new WebNews(article);

                context.Close ();
            }catch(Exception e) {
                context.LogError(this, e.Message);
                context.Close ();
                throw e;
            }

            return result;
        }

    }

    [Route("/news/tumblr", "GET", Summary = "GET a list of tumblr news", Notes = "")]
    public class GetAllTumblrNews : IReturn<List<WebNews>>{}

    [Route("/news/tumblr/feeds", "GET", Summary = "GET a list of tumblr news feeds", Notes = "")]
    public class GetTumblrNewsFeeds : IReturn<List<WebNews>>{}

    [Route("/news/tumblr", "POST", Summary = "POST a tumblr news", Notes = "")]
    public class CreateTumblrNews : WebNews, IReturn<WebNews>{}

    [Route("/news/tumblr/search", "GET", Summary = "GET a list of tumblr news via opensearch", Notes = "")]
    public class SearchTumblrNews {}

}

