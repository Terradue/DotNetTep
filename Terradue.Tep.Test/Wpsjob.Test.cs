using System;
using System.Collections.Generic;
using NUnit.Framework;
using Terradue.Portal;

namespace Terradue.Tep.Test {

    [TestFixture]
    public class WpsJobTest : BaseTest {

        [TestFixtureSetUp]
        public override void FixtureSetup() {
            base.FixtureSetup();
            context.BaseUrl = "http://localhost:8080/api";
            context.AccessLevel = EntityAccessLevel.Administrator;
            try{
                Init ();
            } catch (Exception e) {
                Console.Error.WriteLine (e.Message);
                throw;
            }

            context.ConsoleDebug = true;
            EntityType et = EntityType.GetOrAddEntityType (typeof (WpsJob));
            Console.WriteLine ("CLASS {0} {1}", et.ClassType.AssemblyQualifiedName, et.Id);
        }

        private void Init() {

            //Create users
            UserTep usr1 = new UserTep(context);
            usr1.Username = "testusr1";
            usr1.Store();
            usr1.CreatePrivateDomain ();

            UserTep usr2 = new UserTep(context);
            usr2.Username = "testusr2";
            usr2.Store();
            usr2.CreatePrivateDomain ();

            UserTep usr3 = new UserTep (context);
            usr3.Username = "testusr3";
            usr3.Store ();
            usr3.CreatePrivateDomain ();

            //create domains
            Domain domain = new Domain (context);
            domain.Name = "myDomainTest";
            domain.Kind = DomainKind.Public;
            domain.Store ();

            Domain domain2 = new Domain (context);
            domain2.Name = "otherDomainTest";
            domain2.Kind = DomainKind.Private;
            domain2.Store ();

            Role role = new Role (context);
            role.Identifier = "member";
            role.Store ();

            role.IncludePrivilege (Privilege.FromIdentifier (context, "wpsjob-v"));
            role.IncludePrivilege (Privilege.FromIdentifier (context, "wpsjob-s"));

            //Add users in the domain
            role.GrantToUser (usr1, domain);
            role.GrantToUser (usr2, domain);
            role.GrantToUser (usr3, domain);
            role.GrantToUser (usr3, domain2);
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

        [Test]
        public void CreateWpsJobs () { 
            
            WpsProcessOffering process = CreateProcess (false);
            var usr1 = User.FromUsername (context, "testusr1");
            var usr2 = User.FromUsername (context, "testusr2");
            var usr3 = User.FromUsername (context, "testusr3");
            var domain = Domain.FromIdentifier (context, "myDomainTest");
            var domain2 = Domain.FromIdentifier (context, "otherDomainTest");

            //Create one wpsjob public for usr1 --- all should see it
            WpsJob job = CreateWpsJob ("public-job-usr1", process, usr1);
            job.Store ();
            job.GrantGlobalPermissions ();

            //Create one wpsjob with domain where usr1 is member
            job = CreateWpsJob ("public-job-d", process, usr2);
            job.Domain = domain;
            job.Store ();

            //Create one wpsjob with domain where usr1 is not member
            job = CreateWpsJob ("public-job-d", process, usr3);
            job.Domain = domain2;
            job.Store ();

            //Create one wpsjob public for usr2 --- all should see it
            job = CreateWpsJob ("public-job-usr2", process, usr2);
            job.Store ();
            job.GrantGlobalPermissions ();

            //Create one wpsjob restricted for usr1
            job = CreateWpsJob ("restricted-job-usr1-2", process, usr1);
            job.Store ();
            job.GrantPermissionsToUsers (new int [] { usr2.Id });

            //Create one wpsjob restricted for usr2
            job = CreateWpsJob ("restricted-job-usr2-1", process, usr2);
            job.Store ();
            job.GrantPermissionsToUsers (new int [] { usr1.Id });

            //Create one wpsjob restricted for usr2
            job = CreateWpsJob ("restricted-job-usr2-3", process, usr2);
            job.Store ();
            job.GrantPermissionsToUsers (new int [] { usr3.Id });

            //Create one wpsjob private for usr1
            job = CreateWpsJob ("private-job-usr1", process, usr1);
            job.Store ();

            //Create one wpsjob private for usr2
            job = CreateWpsJob ("private-job-usr2", process, usr2);
            job.Store ();

            //Create one wpsjob private for usr3
            job = CreateWpsJob ("private-job-usr3", process, usr3);
            job.Store ();

            context.AccessLevel = EntityAccessLevel.Administrator;
            EntityList<WpsJob> jobList = new EntityList<WpsJob> (context);
            jobList.Load ();
            var items = jobList.GetItemsAsList ();
            Assert.AreEqual (10, items.Count);
        }

        [Test]
        public void LoadWpsJobByVisibility() {
            
            context.AccessLevel = EntityAccessLevel.Privilege;            

            var usr1 = User.FromUsername (context, "testusr1");
            context.StartImpersonation(usr1.Id);

            //Test Visibility OWNED
            EntityList<WpsJob> jobList = new EntityList<WpsJob>(context);
            jobList.AccessLevel = EntityAccessLevel.Privilege;
            jobList.ItemVisibility = EntityItemVisibility.OwnedOnly;
            jobList.Load();
            var items = jobList.GetItemsAsList();
            Assert.AreEqual(3, items.Count);

            //Test Visibility PUBLIC
            jobList = new EntityList<WpsJob>(context);
            jobList.ItemVisibility = EntityItemVisibility.Public;
            jobList.Load();
            items = jobList.GetItemsAsList();
            Assert.AreEqual(2, items.Count);
            foreach (var item in items) Assert.That (item.Name.Contains ("public"));

            //Test Visibility PUBLIC | OWNED 
            jobList = new EntityList<WpsJob> (context);
            jobList.ItemVisibility = EntityItemVisibility.Public | EntityItemVisibility.OwnedOnly;
            jobList.Load ();
            items = jobList.GetItemsAsList ();
            Assert.AreEqual (1, items.Count);
            Assert.That (items[0].Name == "public-job-usr1");

            //Test Visibility RESTRICTED
            jobList = new EntityList<WpsJob>(context);
            jobList.ItemVisibility = EntityItemVisibility.Restricted;
            jobList.Load();
            items = jobList.GetItemsAsList();
            Assert.AreEqual (2, items.Count);
            foreach (var item in items) Assert.That (item.Name.Contains ("restricted"));

            //Test Visibility RESTRICTED | OWNED
            jobList = new EntityList<WpsJob> (context);
            jobList.ItemVisibility = EntityItemVisibility.Restricted | EntityItemVisibility.OwnedOnly;
            jobList.Load ();
            items = jobList.GetItemsAsList ();
            Assert.AreEqual (1, items.Count);
            Assert.That (items [0].Name == "restricted-job-usr1");

            //Test Visibility PRIVATE
            jobList = new EntityList<WpsJob>(context);
            jobList.ItemVisibility = EntityItemVisibility.Private;
            jobList.Load();
            items = jobList.GetItemsAsList();
            Assert.AreEqual(1, items.Count);
            Assert.That(items[0].Name == "private-job-usr1");

            //Test Visibility PRIVATE | OWNED
            jobList = new EntityList<WpsJob> (context);
            jobList.ItemVisibility = EntityItemVisibility.Private | EntityItemVisibility.OwnedOnly;
            jobList.Load ();
            items = jobList.GetItemsAsList ();
            Assert.AreEqual (1, items.Count);
            Assert.That (items [0].Name == "private-job-usr1");

            context.EndImpersonation();
        }

        [Test]
        public void LoadWpsJobByDomain (){ 
            context.AccessLevel = EntityAccessLevel.Privilege;

            var usr1 = User.FromUsername (context, "testusr1");
            context.StartImpersonation (usr1.Id);
            var domain = Domain.FromIdentifier (context, "myDomainTest");

            EntityList<WpsJob> jobList = new EntityList<WpsJob> (context);
            jobList.DomainId = domain.Id;
            jobList.Load ();
            var items = jobList.GetItemsAsList ();
            Assert.AreEqual (1, items.Count);
            Assert.That (items [0].Name == "public-job-d");
        }

        [Test]
        public void LoadWpsJobByOwner ()
        {
            context.AccessLevel = EntityAccessLevel.Privilege;

            var usr1 = User.FromUsername (context, "testusr1");
            var usr2 = User.FromUsername (context, "testusr2");
            var usr3 = User.FromUsername (context, "testusr3");
            context.StartImpersonation (usr1.Id);

            EntityList<WpsJob> jobList = new EntityList<WpsJob> (context);
            jobList.UserId = usr1.Id;
            jobList.Load ();
            var items = jobList.GetItemsAsList ();
            Assert.AreEqual (3, items.Count);

            jobList = new EntityList<WpsJob> (context);
            jobList.UserId = usr2.Id;
            jobList.Load ();
            items = jobList.GetItemsAsList ();
            Assert.AreEqual (2, items.Count);

            jobList = new EntityList<WpsJob> (context);
            jobList.UserId = usr3.Id;
            jobList.Load ();
            items = jobList.GetItemsAsList ();
            Assert.AreEqual (0, items.Count);
        }

    }
}

