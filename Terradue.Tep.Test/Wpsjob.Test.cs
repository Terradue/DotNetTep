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
            CreateUsers();
        }

        private void CreateUsers() {
            User usr1 = new User(context);
            usr1.Username = "testusr1";
            usr1.Store();

            User usr2 = new User(context);
            usr2.Username = "testusr2";
            usr2.Store();
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

        private WpsJob CreateWpsJob(string name, WpsProcessOffering wps, int OwnerId) {
            WpsJob wpsjob = new WpsJob(context);
            wpsjob.Name = name;
            wpsjob.RemoteIdentifier = Guid.NewGuid().ToString();
            wpsjob.Identifier = Guid.NewGuid().ToString();
            wpsjob.OwnerId = OwnerId;
            wpsjob.UserId = OwnerId;
            wpsjob.WpsId = wps.Provider.Identifier;
            wpsjob.ProcessId = wps.Identifier;
            wpsjob.CreatedTime = DateTime.UtcNow;
            wpsjob.Parameters = new List<KeyValuePair<string, string>>();
            wpsjob.StatusLocation = "http://dem.terradue.int:8080/wps/WebProcessingService";
            return wpsjob;
        }

        [Test]
        public void LoadWpsJob() {
            WpsProcessOffering process = CreateProcess(false);

            var usr1 = User.FromUsername(context, "testusr1");
            var usr2 = User.FromUsername(context, "testusr2");

            context.ConsoleDebug = true;

            //Create one wpsjob public
            WpsJob job1 = CreateWpsJob("private-job", process, usr1.Id);
            job1.Store();
            job1.GrantGlobalPermissions();

            //Create one wpsjob restricted
            WpsJob job2 = CreateWpsJob("restricted-job", process, usr1.Id);
            job2.Store();
            job2.GrantPermissionsToUsers(new int [] { usr2.Id });

            //Create one wpsjob private
            WpsJob job3 = CreateWpsJob("public-job", process, usr1.Id);
            job3.Store();

            //Create one wpsjob private
            WpsJob job4 = CreateWpsJob("other-job", process, usr2.Id);
            job4.Store();

            EntityType et = EntityType.GetOrAddEntityType(typeof(WpsJob));
            Console.WriteLine("CLASS {0} {1}", et.ClassType.AssemblyQualifiedName, et.Id);

            context.StartImpersonation(usr1.Id);

            //Test Visibility owned
            EntityList<WpsJob> jobList = new EntityList<WpsJob>(context);
            jobList.AccessLevel = EntityAccessLevel.Privilege;
            jobList.ItemVisibility = EntityItemVisibility.OwnedOnly;
            jobList.Load();
            var items = jobList.GetItemsAsList();
            Assert.AreEqual(3, items.Count);

            //Test Visibility public
            jobList = new EntityList<WpsJob>(context);
            jobList.ItemVisibility = EntityItemVisibility.Public;
            jobList.Load();
            items = jobList.GetItemsAsList();
            Assert.AreEqual(1, items.Count);
            Assert.That(items[0].Name == "public-job");

            //Test Visibility restricted
            jobList = new EntityList<WpsJob>(context);
            jobList.ItemVisibility = EntityItemVisibility.Public;
            jobList.Load();
            items = jobList.GetItemsAsList();
            Assert.AreEqual(1, items.Count);
            Assert.That(items[0].Name == "restricted-job");

            //Test Visibility private
            jobList = new EntityList<WpsJob>(context);
            jobList.ItemVisibility = EntityItemVisibility.Public;
            jobList.Load();
            items = jobList.GetItemsAsList();
            Assert.AreEqual(1, items.Count);
            Assert.That(items[0].Name == "private-job");

            context.EndImpersonation();
        }

    }
}

