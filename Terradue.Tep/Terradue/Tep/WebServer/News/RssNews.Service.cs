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
using Terradue.Portal;
using Terradue.Tep.WebServer;
using Terradue.WebService.Model;


namespace Terradue.Tep.WebServer.Services {
    [Api("Tep Terradue webserver")]
    [Restrict(EndpointAttributes.InSecure | EndpointAttributes.InternalNetworkAccess | EndpointAttributes.Json,
              EndpointAttributes.Secure | EndpointAttributes.External | EndpointAttributes.Json)]
    public class RssNewsServiceTep : ServiceStack.ServiceInterface.Service {

        private static readonly log4net.ILog log = log4net.LogManager.GetLogger
            (System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        [AddHeader(ContentType="application/atom+xml")]
        public object Get(SearchRssNews request) {
            var context = TepWebContext.GetWebContext(PagePrivileges.EverybodyView);
            IOpenSearchResultCollection result = null;
            try{
                context.Open();
                context.LogInfo(this,string.Format("/news/rss/search GET"));

                // Load the complete request
                HttpRequest httpRequest = HttpContext.Current.Request;

                OpenSearchEngine ose = MasterCatalogue.OpenSearchEngine;

                Type type = OpenSearchFactory.ResolveTypeFromRequest(httpRequest, ose);

                EntityList<RssNews> rss = new EntityList<RssNews>(context);
                rss.Load();

				var settings = new OpenSearchableFactorySettings(ose);
                MultiGenericOpenSearchable multiOSE = new MultiGenericOpenSearchable(rss.Cast<IOpenSearchable>().ToList(), settings);

                result = ose.Query(multiOSE, httpRequest.QueryString, type);

                context.Close ();
            }catch(Exception e) {
                context.LogError(this, e.Message);
                context.Close ();
                throw e;
            }

            return new HttpResult(result.SerializeToString(), result.ContentType);
        }

        public object Get(GetRssNewsFeeds request) {
            var context = TepWebContext.GetWebContext(PagePrivileges.EverybodyView);
            List<WebNews> result = new List<WebNews>();
            try{
                context.Open();
                context.LogInfo(this,string.Format("/news/rss/feeds GET"));

                EntityList<RssNews> rsss = new EntityList<RssNews>(context);
                rsss.Load();

                List<RssNews> rssfeeds = new List<RssNews>();
                foreach(RssNews rss in rsss) rssfeeds.AddRange(rss.GetFeeds());
                foreach(RssNews rssfeed in rssfeeds) result.Add(new WebNews(rssfeed));

                context.Close ();
            }catch(Exception e) {
                context.LogError(this, e.Message);
                context.Close ();
                throw e;
            }

            return result;
        }

        public object Get(GetAllRssNews request) {
            var context = TepWebContext.GetWebContext(PagePrivileges.EverybodyView);
            List<WebNews> result = new List<WebNews>();
            try{
                context.Open();
                context.LogInfo(this,string.Format("/news/rss GET"));

                EntityList<RssNews> articles = new EntityList<RssNews>(context);
                context.ConsoleDebug = true;
                articles.Load();
                foreach(RssNews article in articles) result.Add(new WebNews(article));

                context.Close ();
            }catch(Exception e) {
                context.LogError(this, e.Message);
                context.Close ();
                throw e;
            }

            return result;
        }

        public object Post(CreateRssNews request) {
            var context = TepWebContext.GetWebContext(PagePrivileges.EverybodyView);
            WebNews result = null;
            try{
                context.Open();

                RssNews article = new RssNews(context);
                article = (RssNews)request.ToEntity(context, article);
                article.Store();
                result = new WebNews(article);

                context.LogInfo(this,string.Format("/news/rss POST Id='{0}'", request.Id));

                context.Close ();
            }catch(Exception e) {
                context.LogError(this, e.Message);
                context.Close ();
                throw e;
            }

            return result;
        }

    }

    [Route("/news/rss", "GET", Summary = "GET a list of rss news", Notes = "")]
    public class GetAllRssNews : IReturn<List<WebNews>>{}

    [Route("/news/rss/feeds", "GET", Summary = "GET a list of rss news feeds", Notes = "")]
    public class GetRssNewsFeeds : IReturn<List<WebNews>>{}

    [Route("/news/rss", "POST", Summary = "POST a rss news", Notes = "")]
    public class CreateRssNews : WebNews, IReturn<WebNews>{}

    [Route("/news/rss/search", "GET", Summary = "GET a list of rss news via opensearch", Notes = "")]
    public class SearchRssNews {}
}

