using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using NUnit.Framework;
using Terradue.Tep;
using Terradue.OpenSearch.Result;
using Terradue.Portal;
using Terradue.OpenSearch.Engine;

namespace Terradue.Tep.Test {

    [TestFixture]
    public class WpsJobTest : BaseTest {

        private OpenSearchEngine ose;

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

            ose = MasterCatalogue.OpenSearchEngine;

        }

        private void Init() {

            //Create users
            UserTep usr1 = new UserTep(context);
            usr1.Username = "testusr1";
            usr1.Store();

            UserTep usr2 = new UserTep(context);
            usr2.Username = "testusr2";
            usr2.Store();

            UserTep usr3 = new UserTep(context);
            usr3.Username = "testusr3";
            usr3.Store();

            UserTep usr4 = new UserTep(context);
            usr4.Username = "testusr4";
            usr4.Store();

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

            //create community
            ThematicCommunity community1 = new ThematicCommunity(context);
            community1.Identifier = "community-public-1";
            community1.Kind = DomainKind.Public;
            community1.Store();
            community1.SetOwner(usr3);
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
            wpsjob.Identifier = name;
            wpsjob.OwnerId = owner.Id;
            wpsjob.WpsId = wps.Provider.Identifier;
            wpsjob.ProcessId = wps.Identifier;
            wpsjob.CreatedTime = DateTime.UtcNow;
            wpsjob.DomainId = owner.DomainId;
            wpsjob.Parameters = new List<KeyValuePair<string, string>>();
            wpsjob.StatusLocation = "http://dem.terradue.int:8080/wps/WebProcessingService";
            return wpsjob;
        }

        private int NBJOBS_ALL = 11;
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
            var usr4 = User.FromUsername(context, "testusr4");
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
            job.GrantPermissionsToUsers(new int[] { usr2.Id });

            //Create one wpsjob restricted for usr2
            job = CreateWpsJob("restricted-job-usr2-1", process, usr2);
            job.Store();
            job.GrantPermissionsToUsers(new int[] { usr1.Id });

            //Create one wpsjob restricted for usr2
            job = CreateWpsJob("restricted-job-usr2-3", process, usr2);
            job.Store();
            job.GrantPermissionsToUsers(new int[] { usr3.Id });

            //Create one wpsjob private for usr1
            job = CreateWpsJob("private-job-usr1", process, usr1);
            job.Store();

            //Create one wpsjob private for usr2
            job = CreateWpsJob("private-job-usr2", process, usr2);
            job.Store();

            //Create one wpsjob private for usr3
            job = CreateWpsJob("private-job-usr3", process, usr3);
            job.Store();

            //Create one wpsjob private for usr4
            job = CreateWpsJob("private-job-usr4", process, usr4);
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
                jobList.ItemVisibility = EntityItemVisibility.OwnedOnly;
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
                Assert.AreEqual("public-job-usr1", items[0].Name);

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
                Assert.AreEqual("restricted-job-usr1-2", items[0].Name);

                //Test Visibility PRIVATE
                jobList = new EntityList<WpsJob>(context);
                jobList.ItemVisibility = EntityItemVisibility.Private;
                jobList.Load();
                items = jobList.GetItemsAsList();
                Assert.AreEqual(NBJOBS_USR1_PRIVATE, items.Count);
                Assert.AreEqual("private-job-usr1", items[0].Name);

                //Test Visibility PRIVATE | OWNED
                jobList = new EntityList<WpsJob>(context);
                jobList.ItemVisibility = EntityItemVisibility.Private | EntityItemVisibility.OwnedOnly;
                jobList.Load();
                items = jobList.GetItemsAsList();
                Assert.AreEqual(NBJOBS_USR1_PRIVATE, items.Count);
                Assert.AreEqual("private-job-usr1", items[0].Name);
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

            try {
                EntityList<WpsJob> jobList = new EntityList<WpsJob>(context);
                jobList.SetFilter("DomainId", domain.Id.ToString());
                jobList.Load();
                var items = jobList.GetItemsAsList();
                Assert.AreEqual(NBJOBS_USR1_DOMAIN, items.Count);
                Assert.AreEqual("domain1-job-usr2", items[0].Name);

            } catch (Exception e) {
                Assert.Fail(e.Message);
            } finally {
                context.EndImpersonation();
            }
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
                jobList.SetFilter("OwnerId", usr1.Id.ToString());
                jobList.Load();
                var items = jobList.GetItemsAsList();
                Assert.AreEqual(NBJOBS_USR1_OWNED, items.Count);

                jobList = new EntityList<WpsJob>(context);
                jobList.SetFilter("OwnerId", usr2.Id.ToString());
                jobList.Load();
                items = jobList.GetItemsAsList();
                Assert.AreEqual(NBJOBS_USR1_OWNED_USR2, items.Count);

                jobList = new EntityList<WpsJob>(context);
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
        public void SearchWpsJobsByIdentifier() {
            context.AccessLevel = EntityAccessLevel.Privilege;
            var usr1 = User.FromUsername(context, "testusr1");

            try {
                context.StartImpersonation(usr1.Id);
                EntityList<WpsJob> wpsjobs = new EntityList<WpsJob>(context);

                var parameters = new NameValueCollection();
                parameters.Set("uid", "private-job-usr1");
                IOpenSearchResultCollection osr = ose.Query(wpsjobs, parameters);
                Assert.AreEqual(NBJOBS_USR1_PRIVATE, osr.TotalResults);

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

        [Test]
        public void ShareWpsJobToCommunity() {
            context.AccessLevel = EntityAccessLevel.Administrator;
            ThematicCommunity community = ThematicCommunity.FromIdentifier(context, "community-public-1");
            var wpsjob = WpsJob.FromIdentifier(context, "private-job-usr1");
            var wpsjob2 = WpsJob.FromIdentifier(context, "private-job-usr2");

            context.AccessLevel = EntityAccessLevel.Privilege;

            var usr1 = User.FromUsername(context, "testusr1");
            context.StartImpersonation(usr1.Id);

            try {

                //share as non owner
                try {
                    community.ShareEntity(wpsjob2);
                    Assert.Fail("Cannot share as non owner");
                } catch (Exception) { }

                //share as owner and not member of community
                try {
                    community.ShareEntity(wpsjob2);
                    Assert.Fail("Cannot share as non member");
                } catch (Exception) { }

                community.JoinCurrentUser();

                //share as owner and member of community
                community.ShareEntity(wpsjob);

                Assert.True(wpsjob.IsSharedToCommunity());

                EntityList<WpsJob> wpsjobs = new EntityList<WpsJob>(context);
                var parameters = new NameValueCollection();
                parameters.Set("q", "private-job-usr1");
                IOpenSearchResultCollection osr = ose.Query(wpsjobs, parameters);
                Assert.AreEqual(1, osr.TotalResults);
                bool hasSharedLink = false;
                foreach (var item in osr.Items) {
                    if (item.Identifier == "private-job-usr1") {
                        foreach (var link in item.Links) {
                            if (link.RelationshipType == "results") hasSharedLink = true;
                        }
                    }
                }
                Assert.True(hasSharedLink);

                //unshare the job
                wpsjob.RevokePermissionsFromAll(true, false);
                wpsjob.DomainId = wpsjob.Owner.DomainId;
                wpsjob.Store();

                Assert.False(wpsjob.IsSharedToCommunity());

                wpsjobs = new EntityList<WpsJob>(context);
                parameters = new NameValueCollection();
                parameters.Set("q", "private-job-usr1");
                osr = ose.Query(wpsjobs, parameters);
                Assert.AreEqual(1, osr.TotalResults);
                hasSharedLink = false;
                foreach (var item in osr.Items) {
                    if (item.Identifier == "private-job-usr1") {
                        foreach (var link in item.Links) {
                            if (link.RelationshipType == "results") hasSharedLink = true;
                        }
                    }
                }
                Assert.False(hasSharedLink);

            } catch (Exception e) {
                Assert.Fail(e.Message);
            } finally {
                context.EndImpersonation();
            }
        }

        [Test]
        public void ShareWpsJobToUser() {
            context.AccessLevel = EntityAccessLevel.Administrator;
            var wpsjob = WpsJob.FromIdentifier(context, "restricted-job-usr1-2");
            var usr1 = User.FromUsername(context, "testusr1");

            context.AccessLevel = EntityAccessLevel.Privilege;
            context.StartImpersonation(usr1.Id);

            try {
                Assert.True(wpsjob.IsSharedToUser());

                EntityList<WpsJob> wpsjobs = new EntityList<WpsJob>(context);
                var parameters = new NameValueCollection();
                parameters.Set("q", "restricted-job-usr1-2");
                IOpenSearchResultCollection osr = ose.Query(wpsjobs, parameters);
                Assert.AreEqual(1, osr.TotalResults);
                bool hasSharedLink = false;
                foreach (var item in osr.Items) {
                    if (item.Identifier == "restricted-job-usr1-2") {
                        foreach (var link in item.Links) {
                            if (link.RelationshipType == "results") hasSharedLink = true;
                        }
                    }
                }
                Assert.True(hasSharedLink);

            } catch (Exception e) {
                Assert.Fail(e.Message);
            } finally {
                context.EndImpersonation();
            }
        }

        [Test]
        public void SearchCommunitiesForWpsJob() {
            context.AccessLevel = EntityAccessLevel.Administrator;
            ThematicCommunity community = ThematicCommunity.FromIdentifier(context, "community-public-1");
            var wpsjob = WpsJob.FromIdentifier(context, "private-job-usr1");

            context.AccessLevel = EntityAccessLevel.Privilege;

            var usr1 = User.FromUsername(context, "testusr1");
            context.StartImpersonation(usr1.Id);

            try {
                community.JoinCurrentUser();

                //share as owner and member of community
                community.ShareEntity(wpsjob);

                var communities = new EntityList<ThematicCommunity>(context);
                var parameters = new NameValueCollection();
                parameters.Set("correlatedTo", string.Format("{0}/job/wps/search?uid={1}", context.BaseUrl, "private-job-usr1"));
                IOpenSearchResultCollection osr = ose.Query(communities, parameters);
                Assert.AreEqual(1, osr.TotalResults);

                //unshare the job
                wpsjob.RevokePermissionsFromAll(true, false);
                wpsjob.DomainId = wpsjob.Owner.DomainId;
                wpsjob.Store();

                Assert.False(wpsjob.IsSharedToCommunity());

            } catch (Exception e) {
                Assert.Fail(e.Message);
            } finally {
                context.EndImpersonation();
            }
        }

        [Test]
        public void RefreshWpsjobResultNb(){
            var usr4 = User.FromUsername(context, "testusr4");
            context.StartImpersonation(usr4.Id);
            try{
                var job = WpsJob.FromIdentifier(context, "private-job-usr4");
                job.Status = WpsJobStatus.STAGED;
                job.StatusLocation = "https://recast.terradue.com/t2api/describe/truongvananhhunre/_results/workflows/hydrology_tep_dcs_small_water_body_mapping_small_water_bodies_2_0_8/run/0000204-170920182652224-oozie-oozi-W/456a390e-b542-11e7-97b3-0242ac110002";
                job.Store();

                Assert.AreEqual(job.NbResults, -1);
                Actions.RefreshWpsjobResultNb(context);
                Assert.AreEqual(job.NbResults,0);
            } catch (Exception e) {
                Assert.Fail(e.Message);
            } finally {
                context.EndImpersonation();
            }
        }

    }
}

