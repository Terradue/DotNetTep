using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using NUnit.Framework;
using Terradue.Tep;
using Terradue.News;
using Terradue.OpenSearch.Result;
using Terradue.Portal;
using Terradue.ServiceModel.Syndication;
using Terradue.OpenSearch.Twitter;
using Terradue.OpenSearch;

namespace Terradue.Tep.Test {

    [TestFixture]
    public class NewsTest : BaseTest {

        [TestFixtureSetUp]
        public override void FixtureSetup() {
            base.FixtureSetup();
            context.BaseUrl = "http://localhost:8080/api";

            try {
                context.AccessLevel = EntityAccessLevel.Administrator;
                Init();
            } catch (Exception e) {
                Console.Error.WriteLine(e.Message);
                throw;
            }

            context.ConsoleDebug = true;

        }

        private int NBCOMMUNITY_PUBLIC = 2;

        private void Init() {

            //Create twitter news
            var tweet1 = new TwitterNews(context);
            tweet1.Identifier = "1";
            tweet1.Title = "Twitter ESA";
            tweet1.Author = "ESA";
            tweet1.Store();

            var tweet2 = new TwitterNews(context);
            tweet2.Identifier = "2";
            tweet2.Title = "Twitter Terradue";
            tweet2.Author = "terradue";
            tweet2.Store();

            //create rss news
            var rss1 = new RssNews(context);
            rss1.Identifier = "3";
            rss1.Title = "RSS worldbank";
            rss1.Url = "http://blogs.worldbank.org/water/rss.xml";
            rss1.Store();
        }


        [Test]
        public void SearchTwitterFeeds() {
            var ose = MasterCatalogue.OpenSearchEngine;
            var settings = new OpenSearchableFactorySettings(ose);
            var parameters = new NameValueCollection();
            parameters.Set("count", "20");

            try {
                var twitters = TwitterNews.LoadTwitterCollection(context);
                IOpenSearchResultCollection osr = ose.Query(twitters, parameters);
                Assert.That(osr.TotalResults > 0);
            } catch (Exception e) {
                Assert.Fail(e.Message);
            } finally {
                context.EndImpersonation();
            }
        }

        [Test]
        public void SearchRssFeeds() {
            var ose = MasterCatalogue.OpenSearchEngine;
            var settings = new OpenSearchableFactorySettings(ose);
            List<Terradue.OpenSearch.IOpenSearchable> osentities = new List<Terradue.OpenSearch.IOpenSearchable>();
            var parameters = new NameValueCollection();
            parameters.Set("count", "20");

            try {
                EntityList<RssNews> rsss = new EntityList<RssNews>(context);
                rsss.Load();
                if (rsss != null) foreach (RssNews rss in rsss) osentities.Add(rss);
                MultiGenericOpenSearchable multiOSE = new MultiGenericOpenSearchable(osentities, settings);
                IOpenSearchResultCollection osr = ose.Query(multiOSE, parameters);
                Assert.That(osr.TotalResults > 0);
            } catch (Exception e) {
                Assert.Fail(e.Message);
            } finally {
                context.EndImpersonation();
            }
        }

        [Test]
        public void SearchAllFeeds() {
            var ose = MasterCatalogue.OpenSearchEngine;
            var settings = new OpenSearchableFactorySettings(ose);
            List<Terradue.OpenSearch.IOpenSearchable> osentities = new List<Terradue.OpenSearch.IOpenSearchable>();
            var parameters = new NameValueCollection();
            parameters.Set("count", "20");

            context.AccessLevel = EntityAccessLevel.Privilege;
            try {

                EntityList<Article> articles = new EntityList<Article>(context);
                articles.Load();
                osentities.Add(articles);

                var twitters = TwitterNews.LoadTwitterCollection(context);
                osentities.Add(twitters);

                EntityList<RssNews> rsss = new EntityList<RssNews>(context);
                rsss.Load();
                if (rsss != null) foreach (RssNews rss in rsss) osentities.Add(rss);

                MultiGenericOpenSearchable multiOSE = new MultiGenericOpenSearchable(osentities, settings);

                IOpenSearchResultCollection osr = ose.Query(multiOSE, parameters);
                Assert.That(osr.TotalResults > 0);

            } catch (Exception e) {
                Assert.Fail(e.Message);
            } finally {
                context.EndImpersonation();
            }
        }
    }
}

