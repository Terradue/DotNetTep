using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using ServiceStack.Common.Web;
using ServiceStack.ServiceHost;
using Terradue.News;
using Terradue.OpenSearch;
using Terradue.OpenSearch.Engine;
using Terradue.OpenSearch.Result;
using Terradue.OpenSearch.Twitter;
using Terradue.Portal;
using Terradue.Tep.WebServer;
using Terradue.WebService.Model;

namespace Terradue.Tep.WebServer.Services {
    [Api("Tep Terradue webserver")]
    [Restrict(EndpointAttributes.InSecure | EndpointAttributes.InternalNetworkAccess | EndpointAttributes.Json,
              EndpointAttributes.Secure | EndpointAttributes.External | EndpointAttributes.Json)]
    public class NewsServiceTep : ServiceStack.ServiceInterface.Service {

        private static readonly log4net.ILog log = log4net.LogManager.GetLogger
            (System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public object Get(SearchNews request) {
            var context = TepWebContext.GetWebContext(PagePrivileges.EverybodyView);
            IOpenSearchResultCollection result = null;
            try{
                context.Open();
                context.LogInfo(this,string.Format("/news/search GET"));

                // Load the complete request
                HttpRequest httpRequest = HttpContext.Current.Request;

                OpenSearchEngine ose = MasterCatalogue.OpenSearchEngine;

                Type type = OpenSearchFactory.ResolveTypeFromRequest(httpRequest, ose);

                List<Terradue.OpenSearch.IOpenSearchable> osentities = new List<Terradue.OpenSearch.IOpenSearchable>();

                try{
                    EntityList<Article> articles = new EntityList<Article>(context);
                    articles.Load();
                    osentities.Add(articles);
                }catch(Exception){}

                try{
                    List<TwitterFeed> twitters = TwitterNews.LoadTwitterFeeds(context);
                    foreach(TwitterFeed twitter in twitters) osentities.Add(twitter);
                }catch(Exception){}

                try{
                    EntityList<RssNews> rsss = new EntityList<RssNews>(context);
                    rsss.Load();
                    foreach(RssNews rss in rsss) osentities.Add(rss);
                }catch(Exception){}

                MultiGenericOpenSearchable multiOSE = new MultiGenericOpenSearchable(osentities, ose);

                result = ose.Query(multiOSE, httpRequest.QueryString, type);


                context.Close ();
            }catch(Exception e) {
                context.LogError(this, e.Message);
                context.Close ();
                throw e;
            }

            return new HttpResult(result.SerializeToString(), result.ContentType);
        }

        public object Get(GetAllNewsFeeds request) {
            List<WebNews> result = new List<WebNews>();

            var context = TepWebContext.GetWebContext(PagePrivileges.EverybodyView);
            try {
                context.Open();
                context.LogInfo(this,string.Format("/news/feeds GET"));

                //get internal news
                try{
                    EntityList<Article> news = new EntityList<Article>(context);
                    news.Load();
                    foreach(Terradue.Portal.Article f in news){
                        if(f.GetType() == typeof(Article))
                            result.Add(new WebNews(f));
                    }
                }catch(Exception){}

                //get twitter news
                try{
                    List<TwitterFeed> twitters = TwitterNews.LoadTwitterFeeds(context);
                    List<TwitterNews> tweetsfeeds = new List<TwitterNews>();
                    foreach(TwitterFeed tweet in twitters) tweetsfeeds.AddRange(TwitterNews.FromFeeds(context, tweet.GetFeeds()));
                    foreach(TwitterNews tweetfeed in tweetsfeeds) result.Add(new WebNews(tweetfeed));
                }catch(Exception){}

                //get rss news
                try{
                    EntityList<RssNews> rsss = new EntityList<RssNews>(context);
                    rsss.Load();
                    List<RssNews> rssfeeds = new List<RssNews>();
                    foreach(RssNews rss in rsss) rssfeeds.AddRange(rss.GetFeeds());
                    foreach(RssNews rssfeed in rssfeeds) result.Add(new WebNews(rssfeed));
                }catch(Exception){}

                context.Close();
            } catch (Exception e) {
                context.LogError(this, e.Message);
                context.Close();
                throw e;
            }
            return result.OrderByDescending(r => r.Date);
        }

        public object Get(GetAllNews request) {
            List<WebNews> result = new List<WebNews>();

            var context = TepWebContext.GetWebContext(PagePrivileges.EverybodyView);
            try {
                context.Open();
                context.LogInfo(this,string.Format("/news GET"));
                //get internal news
                EntityList<Terradue.Portal.Article> news = new EntityList<Terradue.Portal.Article>(context);
                news.Load();
                foreach(Terradue.Portal.Article f in news) result.Add(new WebNews(f));

                context.Close();
            } catch (Exception e) {
                context.LogError(this, e.Message);
                context.Close();
                throw e;
            }
            return result;
        }

        public object Get(GetNews request) {
            WebNews result = null;

            var context = TepWebContext.GetWebContext(PagePrivileges.EverybodyView);
            try {
                context.Open();
                context.LogInfo(this,string.Format("/news/{{Id}} GET Id='{0}'", request.Id));

                result = new WebNews(Terradue.Portal.Article.FromId(context,request.Id));

                context.Close();
            } catch (Exception e) {
                context.LogError(this, e.Message);
                context.Close();
                throw e;
            }
            return result;
        }

        public object Post(CreateNews request) {
            WebNews result = null;

            var context = TepWebContext.GetWebContext(PagePrivileges.AdminOnly);
            try {
                context.Open();

                if (request.Type.Equals(EntityType.GetEntityType(typeof(TwitterNews)).Keyword)){
                    TwitterNews tweet = null;
                    tweet = new TwitterNews(context);
                    tweet = (TwitterNews)request.ToEntity(context, tweet);
                    tweet.Store();
                    result = new WebNews(tweet);
                } else if (request.Type.Equals(EntityType.GetEntityType(typeof(RssNews)).Keyword)){
                    RssNews rss = null;
                    rss = new RssNews(context);
                    rss = (RssNews)request.ToEntity(context, rss);
                    rss.Store();
                    result = new WebNews(rss);
                //} else if (request.Type.Equals(EntityType.GetEntityType(typeof(TumblrNews)).Keyword)){
                //    TumblrNews tumblr = null;
                //    tumblr = new TumblrNews(context);
                //    tumblr = (TumblrNews)request.ToEntity(context, tumblr);
                //    tumblr.Store();
                //    result = new WebNews(tumblr);
                } else {
                    Article article = null;
                    article = new Article(context);
                    article = (Article)request.ToEntity(context, article);
                    article.Store();
                    result = new WebNews(article);
                }

                context.LogInfo(this,string.Format("/news POST Id='{0}'", request.Id));

                context.Close();
            } catch (Exception e) {
                context.LogError(this, e.Message);
                context.Close();
                throw e;
            }
            return result;
        }

        public object Put(UpdateNews request) {
            WebNews result = null;

            var context = TepWebContext.GetWebContext(PagePrivileges.AdminOnly);
            try {
                context.Open();
                context.LogInfo(this,string.Format("/news PUT Id='{0}'", request.Id));

                if (request.Type.Equals(EntityType.GetEntityType(typeof(TwitterNews)).Keyword)){
                    TwitterNews tweet = null;
                    tweet = TwitterNews.FromId(context, request.Id);
                    tweet = (TwitterNews)request.ToEntity(context, tweet);
                    tweet.Store();
                    result = new WebNews(tweet);
                } else if (request.Type.Equals(EntityType.GetEntityType(typeof(RssNews)).Keyword)){
                    RssNews rss = null;
                    rss = RssNews.FromId(context, request.Id);
                    rss = (RssNews)request.ToEntity(context, rss);
                    rss.Store();
                    result = new WebNews(rss);
                //} else if (request.Type.Equals(EntityType.GetEntityType(typeof(TumblrNews)).Keyword)){
                //    TumblrNews tumblr = null;
                //    tumblr = TumblrNews.FromId(context, request.Id);
                //    tumblr = (TumblrNews)request.ToEntity(context, tumblr);
                //    tumblr.Store();
                //    result = new WebNews(tumblr);
                } else {
                    Article article = null;
                    article = Article.FromId(context, request.Id);
                    article = (Article)request.ToEntity(context, article);
                    article.Store();
                    result = new WebNews(article);
                }

                context.Close();
            } catch (Exception e) {
                context.LogError(this, e.Message);
                context.Close();
                throw e;
            }
            return result;
        }

        public object Delete(DeleteNews request) {
            var context = TepWebContext.GetWebContext(PagePrivileges.AdminOnly);
            try {
                context.Open();
                context.LogInfo(this,string.Format("/news/{{Id}} DELETE Id='{0}'", request.Id));
                Terradue.Portal.Article news = Terradue.Portal.Article.FromId(context,request.Id);
                news.Delete();

                context.Close();
            } catch (Exception e) {
                context.LogError(this, e.Message);
                context.Close();
                throw e;
            }
            return new WebResponseBool(true);
        }

    }

    [Route("/news/feeds", "GET", Summary = "GET a list of news feeds", Notes = "")]
    public class GetAllNewsFeeds : IReturn<List<WebNews>>{}
}

