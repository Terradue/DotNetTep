using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using NUnit.Framework;
using Terradue.Tep;
using Terradue.OpenSearch.Result;
using Terradue.Portal;

namespace Terradue.Tep.Test {

    [TestFixture]
    public class WpsJobTest : BaseTest {

        [TestFixtureSetUp]
        public override void FixtureSetup() {
            base.FixtureSetup();
            context.BaseUrl = "http://localhost:8080/api";

            try {
                context.AccessLevel = EntityAccessLevel.Administrator;
                Init();
                CreateWpsJobs();
            } catch (Exception e) {
                Console.Error.WriteLine(e.Message);
                throw;
            }

            context.ConsoleDebug = true;

        }

        private void Init() {

            //Create users
            UserTep usr1 = new UserTep(context);
            usr1.Username = "testusr1";
            usr1.Store();
            usr1.CreatePrivateDomain();

            UserTep usr2 = new UserTep(context);
            usr2.Username = "testusr2";
            usr2.Store();
            usr2.CreatePrivateDomain();

            UserTep usr3 = new UserTep(context);
            usr3.Username = "testusr3";
            usr3.Store();
            usr3.CreatePrivateDomain();

            //create domains
            Domain domain = new Domain(context);
            domain.Identifier = "myDomainTest";
            domain.Kind = DomainKind.Public;
            domain.Store();

            Domain domain2 = new Domain(context);
            domain2.Identifier = "otherDomainTest";
            domain2.Kind = DomainKind.Private;
            domain2.Store();

            Role role = new Role(context);
            role.Identifier = "member-test";
            role.Store();

            role.IncludePrivilege(Privilege.FromIdentifier(context, "wpsjob-v"));
            role.IncludePrivilege(Privilege.FromIdentifier(context, "wpsjob-s"));

            //Add users in the domain
            role.GrantToUser(usr1, domain);
            role.GrantToUser(usr2, domain);
            role.GrantToUser(usr3, domain);
            role.GrantToUser(usr3, domain2);
        }

        private WpsProvider CreateProvider(string identifier, string name, string url, bool proxy) {
            WpsProvider provider;
            provider = new WpsProvider(context);
            provider.Identifier = identifier;
            provider.Name = name;
            provider.Description = name;
            provider.BaseUrl = url;
            provider.Proxy = proxy;
            try {
                provider.Store();
            } catch (Exception e) {
                throw e;
            }
            return provider;
        }

        private WpsProcessOffering CreateProcess(WpsProvider provider, string identifier, string name) {
            WpsProcessOffering process = new WpsProcessOffering(context);
            process.Name = name;
            process.Description = name;
            process.RemoteIdentifier = identifier;
            process.Identifier = Guid.NewGuid().ToString();
            process.Url = provider.BaseUrl;
            process.Version = "1.0.0";
            process.Provider = provider;
            return process;
        }

        private WpsProcessOffering CreateProcess(bool proxy) {
            WpsProvider provider = CreateProvider("test-wps-" + proxy.ToString(), "test provider " + (proxy ? "p" : "np"), "http://dem.terradue.int:8080/wps/WebProcessingService", proxy);
            WpsProcessOffering process = CreateProcess(provider, "com.test.provider", "test provider " + (proxy ? "p" : "np"));
            return process;
        }

        private WpsJob CreateWpsJob(string name, WpsProcessOffering wps, User owner) {
            WpsJob wpsjob = new WpsJob(context);
            wpsjob.Name = name;
            wpsjob.RemoteIdentifier = Guid.NewGuid().ToString();
            wpsjob.Identifier = Guid.NewGuid().ToString();
            wpsjob.OwnerId = owner.Id;
            wpsjob.UserId = owner.Id;
            wpsjob.WpsId = wps.Provider.Identifier;
            wpsjob.ProcessId = wps.Identifier;
            wpsjob.CreatedTime = DateTime.UtcNow;
            wpsjob.DomainId = owner.DomainId;
            wpsjob.Parameters = new List<KeyValuePair<string, string>>();
            wpsjob.StatusLocation = "http://dem.terradue.int:8080/wps/WebProcessingService";
            return wpsjob;
        }

        private int NBJOBS_ALL = 10;
        private int NBJOBS_PUBLIC = 2;
        private int NBJOBS_USR1_ALL = 6;
        private int NBJOBS_USR1_OWNED = 3;
        private int NBJOBS_USR1_OWNED_USR2 = 3;
        private int NBJOBS_USR1_OWNED_USR3 = 0;
        private int NBJOBS_USR1_PUBLIC = 1;
        private int NBJOBS_USR1_RESTRICTED = 2;
        private int NBJOBS_USR1_RESTRICTED_OWNED = 1;
        private int NBJOBS_USR1_PRIVATE = 1;
        private int NBJOBS_USR1_DOMAIN = 1;

        private void CreateWpsJobs() {

            WpsProcessOffering process = CreateProcess(false);
            var usr1 = User.FromUsername(context, "testusr1");
            var usr2 = User.FromUsername(context, "testusr2");
            var usr3 = User.FromUsername(context, "testusr3");
            var domain = Domain.FromIdentifier(context, "myDomainTest");
            var domain2 = Domain.FromIdentifier(context, "otherDomainTest");

            //Create one wpsjob public for usr1 --- all should see it
            WpsJob job = CreateWpsJob("public-job-usr1", process, usr1);
            job.Store();
            job.GrantGlobalPermissions();

            //Create one wpsjob with domain where usr1 is member
            job = CreateWpsJob("domain1-job-usr2", process, usr2);
            job.Domain = domain;
            job.Store();

            //Create one wpsjob with domain where usr1 is not member
            job = CreateWpsJob("domain2-job-usr3", process, usr3);
            job.Domain = domain2;
            job.Store();

            //Create one wpsjob public for usr2 --- all should see it
            job = CreateWpsJob("public-job-usr2", process, usr2);
            job.Store();
            job.GrantGlobalPermissions();

            //Create one wpsjob restricted for usr1
            job = CreateWpsJob("restricted-job-usr1-2", process, usr1);
            job.Store();
            job.GrantPermissionsToUsers(new int [] { usr2.Id });

            //Create one wpsjob restricted for usr2
            job = CreateWpsJob("restricted-job-usr2-1", process, usr2);
            job.Store();
            job.GrantPermissionsToUsers(new int [] { usr1.Id });

            //Create one wpsjob restricted for usr2
            job = CreateWpsJob("restricted-job-usr2-3", process, usr2);
            job.Store();
            job.GrantPermissionsToUsers(new int [] { usr3.Id });

            //Create one wpsjob private for usr1
            job = CreateWpsJob("private-job-usr1", process, usr1);
            job.Store();

            //Create one wpsjob private for usr2
            job = CreateWpsJob("private-job-usr2", process, usr2);
            job.Store();

            //Create one wpsjob private for usr3
            job = CreateWpsJob("private-job-usr3", process, usr3);
            job.Store();

        }

        [Test]
        public void LoadWpsJobAsAdmin() {
            context.AccessLevel = EntityAccessLevel.Administrator;
            EntityList<WpsJob> jobList = new EntityList<WpsJob>(context);
            jobList.Load();
            var items = jobList.GetItemsAsList();
            Assert.AreEqual(NBJOBS_ALL, items.Count);
        }

        [Test]
        public void LoadWpsJobByVisibility() {

            context.AccessLevel = EntityAccessLevel.Privilege;

            var usr1 = User.FromUsername(context, "testusr1");
            context.StartImpersonation(usr1.Id);

            try {

                //Test Visibility ALL
                EntityList<WpsJob> jobList = new EntityList<WpsJob>(context);
                jobList.ItemVisibility = EntityItemVisibility.All;
                jobList.Load();
                var items = jobList.GetItemsAsList();
                Assert.AreEqual(NBJOBS_USR1_ALL, items.Count);

                //Test Visibility OWNED
                jobList = new EntityList<WpsJob>(context);
                jobList.ItemVisibility = EntityItemVisibility.All | EntityItemVisibility.OwnedOnly;
                jobList.Load();
                items = jobList.GetItemsAsList();
                Assert.AreEqual(NBJOBS_USR1_OWNED, items.Count);
                foreach (var item in items) Assert.That(item.Name.Contains("usr1"));

                //Test Visibility PUBLIC
                jobList = new EntityList<WpsJob>(context);
                jobList.ItemVisibility = EntityItemVisibility.Public;
                jobList.Load();
                items = jobList.GetItemsAsList();
                Assert.AreEqual(NBJOBS_PUBLIC, items.Count);
                foreach (var item in items) Assert.That(item.Name.Contains("public"));

                //Test Visibility PUBLIC | OWNED 
                jobList = new EntityList<WpsJob>(context);
                jobList.ItemVisibility = EntityItemVisibility.Public | EntityItemVisibility.OwnedOnly;
                jobList.Load();
                items = jobList.GetItemsAsList();
                Assert.AreEqual(NBJOBS_USR1_PUBLIC, items.Count);
                Assert.AreEqual("public-job-usr1", items [0].Name);

                //Test Visibility RESTRICTED
                jobList = new EntityList<WpsJob>(context);
                jobList.ItemVisibility = EntityItemVisibility.Restricted;
                jobList.Load();
                items = jobList.GetItemsAsList();
                Assert.AreEqual(NBJOBS_USR1_RESTRICTED, items.Count);
                foreach (var item in items) Assert.That(item.Name.Contains("restricted"));

                //Test Visibility RESTRICTED | OWNED
                jobList = new EntityList<WpsJob>(context);
                jobList.ItemVisibility = EntityItemVisibility.Restricted | EntityItemVisibility.OwnedOnly;
                jobList.Load();
                items = jobList.GetItemsAsList();
                Assert.AreEqual(NBJOBS_USR1_RESTRICTED_OWNED, items.Count);
                Assert.AreEqual("restricted-job-usr1", items [0].Name);

                //Test Visibility PRIVATE
                jobList = new EntityList<WpsJob>(context);
                jobList.ItemVisibility = EntityItemVisibility.Private;
                jobList.Load();
                items = jobList.GetItemsAsList();
                Assert.AreEqual(NBJOBS_USR1_PRIVATE, items.Count);
                Assert.AreEqual("private-job-usr1", items [0].Name);

                //Test Visibility PRIVATE | OWNED
                jobList = new EntityList<WpsJob>(context);
                jobList.ItemVisibility = EntityItemVisibility.Private | EntityItemVisibility.OwnedOnly;
                jobList.Load();
                items = jobList.GetItemsAsList();
                Assert.AreEqual(NBJOBS_USR1_PRIVATE, items.Count);
                Assert.AreEqual("private-job-usr1", items [0].Name);
            } catch (Exception e) {
                Assert.Fail(e.Message);
            } finally {
                context.EndImpersonation();
            }
        }

        [Test]
        public void LoadWpsJobByDomain() {
            context.AccessLevel = EntityAccessLevel.Privilege;

            var usr1 = User.FromUsername(context, "testusr1");
            context.StartImpersonation(usr1.Id);
            var domain = Domain.FromIdentifier(context, "myDomainTest");

            EntityList<WpsJob> jobList = new EntityList<WpsJob>(context);
            jobList.SetFilter("DomainId", domain.Id.ToString());
            jobList.Load();
            var items = jobList.GetItemsAsList();
            Assert.AreEqual(NBJOBS_USR1_DOMAIN, items.Count);
            Assert.That(items [0].Name == "public-job-d");

            context.EndImpersonation();
        }

        [Test]
        public void LoadWpsJobByOwner() {
            context.AccessLevel = EntityAccessLevel.Privilege;

            var usr1 = User.FromUsername(context, "testusr1");
            var usr2 = User.FromUsername(context, "testusr2");
            var usr3 = User.FromUsername(context, "testusr3");
            context.StartImpersonation(usr1.Id);

            try {
                EntityList<WpsJob> jobList = new EntityList<WpsJob>(context);
                //jobList.UserId = usr1.Id;
                jobList.SetFilter("OwnerId", usr1.Id.ToString());
                jobList.Load();
                var items = jobList.GetItemsAsList();
                Assert.AreEqual(NBJOBS_USR1_OWNED, items.Count);

                jobList = new EntityList<WpsJob>(context);
                //jobList.UserId = usr2.Id;
                jobList.SetFilter("OwnerId", usr2.Id.ToString());
                jobList.Load();
                items = jobList.GetItemsAsList();
                Assert.AreEqual(NBJOBS_USR1_OWNED_USR2, items.Count);

                jobList = new EntityList<WpsJob>(context);
                //jobList.UserId = usr3.Id;
                jobList.SetFilter("OwnerId", usr3.Id.ToString());
                jobList.Load();
                items = jobList.GetItemsAsList();
                Assert.AreEqual(NBJOBS_USR1_OWNED_USR3, items.Count);
            } catch (Exception e) {
                Assert.Fail(e.Message);
            } finally {
                context.EndImpersonation();
            }
        }

        [Test]
        public void SearchAllWpsJobs() {
            context.AccessLevel = EntityAccessLevel.Privilege;
            var usr1 = User.FromUsername(context, "testusr1");
            context.StartImpersonation(usr1.Id);

            try {
                EntityList<WpsJob> wpsjobs = new EntityList<WpsJob>(context);
                var ose = MasterCatalogue.OpenSearchEngine;

                //get all jobs
                var parameters = new NameValueCollection();
                IOpenSearchResultCollection osr = ose.Query(wpsjobs, parameters);
                Assert.AreEqual(NBJOBS_USR1_ALL, osr.TotalResults);

            } catch (Exception e) {
                Assert.Fail(e.Message);
            } finally {
                context.EndImpersonation();
            }
        }

        [Test]
        public void SearchWpsJobsByQ() {
            context.AccessLevel = EntityAccessLevel.Privilege;
            var usr1 = User.FromUsername(context, "testusr1");
            context.StartImpersonation(usr1.Id);

            try {
                EntityList<WpsJob> wpsjobs = new EntityList<WpsJob>(context);
                var ose = MasterCatalogue.OpenSearchEngine;
                wpsjobs.OpenSearchEngine = ose;

                var parameters = new NameValueCollection();
                parameters.Set("q", "usr1");
                IOpenSearchResultCollection osr = ose.Query(wpsjobs, parameters);
                Assert.AreEqual(NBJOBS_USR1_OWNED, osr.TotalResults);

            } catch (Exception e) {
                Assert.Fail(e.Message);
            } finally {
                context.EndImpersonation();
            }
        }

        [Test]
        public void SearchWpsJobsByDomain() {
            context.AccessLevel = EntityAccessLevel.Privilege;
            var usr1 = User.FromUsername(context, "testusr1");
            context.StartImpersonation(usr1.Id);

            try {
                EntityList<WpsJob> wpsjobs = new EntityList<WpsJob>(context);
                var ose = MasterCatalogue.OpenSearchEngine;
                wpsjobs.OpenSearchEngine = ose;

                var parameters = new NameValueCollection();
                parameters.Set("domain", "myDomainTest");
                IOpenSearchResultCollection osr = ose.Query(wpsjobs, parameters);
                Assert.AreEqual(NBJOBS_USR1_DOMAIN, osr.TotalResults);

            } catch (Exception e) {
                Assert.Fail(e.Message);
            } finally {
                context.EndImpersonation();
            }
        }

        [Test]
        public void SearchWpsJobsByVisibility() {
            context.AccessLevel = EntityAccessLevel.Privilege;
            var usr1 = User.FromUsername(context, "testusr1");
            var ose = MasterCatalogue.OpenSearchEngine;

            try {
                context.StartImpersonation(usr1.Id);
                EntityList<WpsJob> wpsjobs = new EntityList<WpsJob>(context);

                var parameters = new NameValueCollection();
                parameters.Set("visibility", "owned");
                IOpenSearchResultCollection osr = ose.Query(wpsjobs, parameters);
                Assert.AreEqual(NBJOBS_USR1_OWNED, osr.TotalResults);

                parameters = new NameValueCollection();
                parameters.Set("visibility", "public");
                osr = ose.Query(wpsjobs, parameters);
                Assert.AreEqual(NBJOBS_PUBLIC, osr.TotalResults);

            } catch (Exception e) {
                Assert.Fail(e.Message);
            } finally {
                context.EndImpersonation();
            }
        }

    }
}

