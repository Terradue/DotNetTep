﻿using System;
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
    public class TwitterNewsServiceTep : ServiceStack.ServiceInterface.Service {

        private static readonly log4net.ILog log = log4net.LogManager.GetLogger
            (System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public object Get(SearchTwitterNews request) {
            var context = TepWebContext.GetWebContext(PagePrivileges.EverybodyView);
            IOpenSearchResultCollection result = null;
            try{
                context.Open();
                context.LogInfo(this,string.Format("/news/twitter/search GET"));

                // Load the complete request
                HttpRequest httpRequest = HttpContext.Current.Request;

                OpenSearchEngine ose = MasterCatalogue.OpenSearchEngine;

                Type type = OpenSearchFactory.ResolveTypeFromRequest(httpRequest.QueryString, httpRequest.Headers, ose);

                List<Terradue.OpenSearch.IOpenSearchable> osentities = new List<Terradue.OpenSearch.IOpenSearchable>();

                try {
                    var twitters = TwitterNews.LoadTwitterCollection(context);
                    osentities.Add(twitters);
                } catch (Exception) { }

                var settings = MasterCatalogue.OpenSearchFactorySettings;
                MultiGenericOpenSearchable multiOSE = new MultiGenericOpenSearchable(osentities, settings);
                result = ose.Query(multiOSE, httpRequest.QueryString, type);

                context.Close ();
            }catch(Exception e) {
                context.LogError(this, e.Message, e);
                context.Close ();
                throw e;
            }

            return new HttpResult(result.SerializeToString(), result.ContentType);
        }

        public object Get(GetTwitterNewsFeeds request) {
            var context = TepWebContext.GetWebContext(PagePrivileges.EverybodyView);
            List<WebNews> result = new List<WebNews>();
            try{
                context.Open();
                context.LogInfo(this,string.Format("/news/twitter/feeds GET"));

                var twitterCollection = TwitterNews.LoadTwitterCollection(context);
                var twitters = twitterCollection.GetFeeds(new System.Collections.Specialized.NameValueCollection());
                List<TwitterNews> tweetsfeeds = new List<TwitterNews>();
                tweetsfeeds.AddRange(TwitterNews.FromFeeds(context, twitters));
                foreach (TwitterNews tweetfeed in tweetsfeeds) result.Add(new WebNews(tweetfeed));
 
                context.Close ();
            }catch(Exception e) {
                context.LogError(this, e.Message, e);
                context.Close ();
                throw e;
            }

            return result;
        }

        public object Get(GetAllTwitterNews request) {
            var context = TepWebContext.GetWebContext(PagePrivileges.EverybodyView);
            List<WebNews> result = new List<WebNews>();
            try{
                context.Open();
                context.LogInfo(this,string.Format("/news/twitter GET"));

                EntityList<TwitterNews> articles = new EntityList<TwitterNews>(context);
                articles.Load();
                foreach(TwitterNews article in articles) result.Add(new WebNews(article));

                context.Close ();
            }catch(Exception e) {
                context.LogError(this, e.Message, e);
                context.Close ();
                throw e;
            }

            return result;
        }

        public object Post(CreateTwitterNews request) {
            var context = TepWebContext.GetWebContext(PagePrivileges.EverybodyView);
            WebNews result = null;
            try{
                context.Open();
                context.LogInfo(this,string.Format("/news/twitter POST Id='{0}'", request.Id));

                TwitterNews article = new TwitterNews(context);
                article = (TwitterNews)request.ToEntity(context, article);
                article.Store();
                result = new WebNews(article);

                context.Close ();
            }catch(Exception e) {
                context.LogError(this, e.Message, e);
                context.Close ();
                throw e;
            }

            return result;
        }

    }

    [Route("/news/twitter", "GET", Summary = "GET a list of twitter news", Notes = "")]
    public class GetAllTwitterNews : IReturn<List<WebNews>>{}

    [Route("/news/twitter/feeds", "GET", Summary = "GET a list of twitter news", Notes = "")]
    public class GetTwitterNewsFeeds : IReturn<List<WebNews>>{}

    [Route("/news/twitter", "POST", Summary = "POST a twitter news", Notes = "")]
    public class CreateTwitterNews : WebNews, IReturn<WebNews>{}

    [Route("/news/twitter/search", "GET", Summary = "GET a list of twitter news via opensearch", Notes = "")]
    public class SearchTwitterNews {}

}

