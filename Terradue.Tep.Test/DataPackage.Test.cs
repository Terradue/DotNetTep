using System;
using NUnit.Framework;
using Terradue.Portal;
using Terradue.OpenSearch.Engine;
using Terradue.OpenSearch;
using System.Collections.Generic;
using System.Collections.Specialized;
using Terradue.OpenSearch.Result;

namespace Terradue.Tep.Test {

    [TestFixture]
    public class DataPackageTest : BaseTest {

        private OpenSearchEngine ose;

        [TestFixtureSetUp]
        public override void FixtureSetup() {
            base.FixtureSetup();
            context.BaseUrl = "http://localhost:8080/api";
            try {
                context.AccessLevel = EntityAccessLevel.Administrator;
            } catch (Exception e) {
                Console.Error.WriteLine(e.Message);
                throw;
            }
            ose = MasterCatalogue.OpenSearchEngine;
        }

        [Test]
        public void GenerateIdentifier() {
            DataPackage dp = new DataPackage(context);
            dp.Name = "AabB/., %&$\\";
            dp.Kind = RemoteResourceSet.KINDRESOURCESETNORMAL;
            dp.Store();
            Assert.AreEqual("AabB,$", dp.Identifier);
        }

        [Test]
        public void SaveDataPackage() {
            DataPackage def = DataPackage.GetTemporaryForCurrentUser(context);
            var res = new RemoteResource(context);
            res.Location = "https://catalog.terradue.com:443/landsat8/search?format=json&uid=LC08_L1GT_070119_20190306_20190306_01_RT";

            def.AddResourceItem(res);
            def.LoadItems();
            Assert.AreEqual(1, def.Items.Count);

            DataPackage dp = new DataPackage(context);
            dp.Name = "test-dp";
            dp.Kind = RemoteResourceSet.KINDRESOURCESETNORMAL;
            dp.Store();
            dp.LoadItems();
            Assert.AreEqual(0, dp.Items.Count);

            foreach (RemoteResource r in def.Resources) {
                RemoteResource tmpres = new RemoteResource(context);
                tmpres.Location = r.Location;
                dp.AddResourceItem(tmpres);
            }
            dp.LoadItems();
            Assert.AreEqual(1, dp.Items.Count);

            var res2 = new RemoteResource(context);
            res2.Location = "https://catalog.terradue.com:443/sentinel3/search?format=json&uid=S3A_SR_2_LAN____20190307T092733_20190307T093355_20190307T102301_0382_042_136______LN3_O_NR_003";
            def.AddResourceItem(res2);
            def.LoadItems();
            Assert.AreEqual(2, def.Items.Count);

            foreach (var r in dp.Resources) {
                r.Delete();
            }
            dp.Items = new EntityList<RemoteResource>(context);
            foreach (RemoteResource r in def.Resources) {
                RemoteResource tmpres = new RemoteResource(context);
                tmpres.Location = r.Location;
                dp.AddResourceItem(tmpres);
            }
            dp.LoadItems();
            Assert.AreEqual(2, dp.Items.Count);
        }

        [Test]
        public void SearchDataPackage() {
            Terradue.Tep.DataPackage datapackage = DataPackage.GetTemporaryForCurrentUser(context);
            datapackage.LoadItems();
            foreach (RemoteResource res in datapackage.Resources) {
                res.Delete();
            }
            datapackage.LoadItems();
            Assert.AreEqual(0, datapackage.Items.Count);

            var ressourceItem = new RemoteResource(context);
            ressourceItem.Location = "https://catalog.terradue.com:443/sentinel1/search?uid=S1A_EW_OCN__2SDH_20200108T051332_20200108T051348_030704_038509_0C1C";
            datapackage.AddResourceItem(ressourceItem);

            ressourceItem = new RemoteResource(context);
            ressourceItem.Location = "https://catalog.terradue.com:443/sentinel1/search?uid=S1A_IW_RAW__0SSH_20200108T050838_20200108T050910_030704_038508_ED48";
            datapackage.AddResourceItem(ressourceItem);

            ressourceItem = new RemoteResource(context);
            ressourceItem.Location = "https://catalog.terradue.com:443/sentinel1/search?uid=S1A_IW_OCN__2SDV_20200108T043336_20200108T043412_030704_038504_3458";
            datapackage.AddResourceItem(ressourceItem);

            datapackage.LoadItems();
            Assert.AreEqual(3, datapackage.Items.Count);

            datapackage.SetOpenSearchEngine(MasterCatalogue.OpenSearchEngine);
            
            List<Terradue.OpenSearch.IOpenSearchable> osentities = new List<Terradue.OpenSearch.IOpenSearchable>();
            osentities.AddRange(datapackage.GetOpenSearchableArray());

            var settings = MasterCatalogue.OpenSearchFactorySettings;
            MultiGenericOpenSearchable multiOSE = new MultiGenericOpenSearchable(osentities, settings, true);

            var parameters = new NameValueCollection();

            IOpenSearchResultCollection osr = ose.Query(multiOSE, parameters);

            Assert.AreEqual(3, osr.TotalResults);
        }

    }
}

