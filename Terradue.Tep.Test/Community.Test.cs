using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using NUnit.Framework;
using Terradue.Tep;
using Terradue.OpenSearch.Result;
using Terradue.Portal;
using Terradue.ServiceModel.Syndication;

namespace Terradue.Tep.Test {

    [TestFixture]
    public class CommunityTest : BaseTest {

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

            //create communities
            ThematicCommunity community1 = new ThematicCommunity(context);
            community1.Identifier = "community-public-1";
            community1.Kind = DomainKind.Public;
            community1.Store();
            community1.SetOwner(usr2);

            ThematicCommunity community2 = new ThematicCommunity(context);
            community2.Identifier = "community-private-1";
            community2.Kind = DomainKind.Private;
            community2.Store();
            community2.SetOwner(usr2);

            ThematicCommunity community3 = new ThematicCommunity(context);
            community3.Identifier = "community-private-2";
            community3.Kind = DomainKind.Private;
            community3.Store();
            community3.SetOwner(usr2);

            ThematicCommunity community4 = new ThematicCommunity(context);
            community4.Identifier = "community-public-2";
            community4.Kind = DomainKind.Public;
            community4.Store();
            community4.SetOwner(usr2);

        }

        [Test]
        public void GetCommunityOwner() {
            ThematicCommunity community = ThematicCommunity.FromIdentifier(context, "community-public-1");
            var usr = community.Owner;
            Assert.AreEqual("testusr2", usr.Username);

            community = ThematicCommunity.FromIdentifier(context, "community-public-2");
            usr = community.Owner;
            Assert.AreEqual("testusr2", usr.Username);

            community = ThematicCommunity.FromIdentifier(context, "community-private-1");
            usr = community.Owner;
            Assert.AreEqual("testusr2", usr.Username);

            community = ThematicCommunity.FromIdentifier(context, "community-private-2");
            usr = community.Owner;
            Assert.AreEqual("testusr2", usr.Username);
        }

        [Test]
        public void JoinPublicCommunity() {
            context.AccessLevel = EntityAccessLevel.Privilege;
            var usr1 = User.FromUsername(context, "testusr1");
            var usr2 = User.FromUsername(context, "testusr2");
            context.StartImpersonation(usr1.Id);

            try {
                ThematicCommunity community = ThematicCommunity.FromIdentifier(context, "community-public-1");
                var roles = Role.GetUserRolesForDomain(context, usr1.Id, community.Id);

                //user not part of community
                Assert.AreEqual(0, roles.Length);

                community.JoinCurrentUser();
                roles = Role.GetUserRolesForDomain(context, usr1.Id, community.Id);

                //user part of community
                Assert.AreEqual(1, roles.Length);
                Assert.AreEqual(RoleTep.MEMBER, roles [0].Name);

                //check user cannot joins twice
                community.JoinCurrentUser();
                roles = Role.GetUserRolesForDomain(context, usr1.Id, community.Id);
                Assert.AreEqual(1, roles.Length);

                context.EndImpersonation();
                context.StartImpersonation(usr2.Id);

                var role = Role.FromIdentifier(context, RoleTep.STARTER);

                community.SetUserRole(usr1, role);
                roles = Role.GetUserRolesForDomain(context, usr1.Id, community.Id);

                //user part of community
                Assert.AreEqual(1, roles.Length);
                Assert.AreEqual(RoleTep.STARTER, roles [0].Name);

            } catch (Exception e) {
                Assert.Fail(e.Message);
            } finally {
                context.EndImpersonation();
            }
        }

        [Test]
        public void JoinPrivateCommunity() {
            context.AccessLevel = EntityAccessLevel.Privilege;
            var usr1 = User.FromUsername(context, "testusr1");
            var usr2 = User.FromUsername(context, "testusr2");
            var ose = MasterCatalogue.OpenSearchEngine;
            var parameters = new NameValueCollection();

            try {
                context.StartImpersonation(usr1.Id);

                ThematicCommunity community = ThematicCommunity.FromIdentifier(context, "community-private-1");
                Role role = Role.FromIdentifier(context, RoleTep.MEMBER);

                //check how many communities user can see
                EntityList<ThematicCommunity> communities = new EntityList<ThematicCommunity>(context);
                communities.Identifier = "community";
                communities.OpenSearchEngine = ose;
                IOpenSearchResultCollection osr = ose.Query(communities, parameters);
                Assert.AreEqual(NBCOMMUNITY_PUBLIC, osr.TotalResults);

                context.EndImpersonation();
                context.StartImpersonation(usr2.Id);

                //add user in community
                community.SetUserAsTemporaryMember(usr1.Id, role.Id);
                Assert.True(community.IsUserPending(usr1.Id));

                context.EndImpersonation();
                context.StartImpersonation(usr1.Id);

                //check how many communities user can see
                communities = new EntityList<ThematicCommunity>(context);
                communities.Identifier = "community";
                communities.OpenSearchEngine = ose;
                osr = ose.Query(communities, parameters);
                Assert.AreEqual(NBCOMMUNITY_PUBLIC + 1, osr.TotalResults);

                //check visibility is private + pending
                var items = osr.Items;
                bool isprivate = false, isVisibilityPending = false, ispublic = false, isUserPending = false;
                foreach (var item in items) {
                    if (item.Title.Text == "community-private-1") {
                        foreach (var cat in item.Categories) {
                            if (cat.Name == "visibility") {
                                if (cat.Label == "private") isprivate = true;
                                else if (cat.Label == "public") ispublic = true;
                            } else if (cat.Name == "userStatus" && cat.Label == "pending") isVisibilityPending = true;
                        }
                        foreach (var author in item.Authors) {
                            bool isUsr1 = false;
                            var elements = author.ElementExtensions.ReadElementExtensions<string>("identifier", "http://purl.org/dc/elements/1.1/");
                            if (elements.Count == 1 && elements [0] == usr1.Username) isUsr1 = true;
                            if (isUsr1) {
                                elements = author.ElementExtensions.ReadElementExtensions<string>("status", "http://purl.org/dc/elements/1.1/");
                                if (elements.Count == 1 && elements [0] == RoleTep.PENDING) isUserPending = true;
                            }
                        }
                    }
                }
                Assert.True(isprivate);
                Assert.True(isVisibilityPending);
                Assert.True(isUserPending);
                Assert.False(ispublic);

                //usr1 validates
                community.JoinCurrentUser();
                Assert.False(community.IsUserPending(usr1.Id));

                //check how many communities user can see
                communities = new EntityList<ThematicCommunity>(context);
                communities.Identifier = "community";
                communities.OpenSearchEngine = ose;
                osr = ose.Query(communities, parameters);
                Assert.AreEqual(NBCOMMUNITY_PUBLIC + 1, osr.TotalResults);

                //check visibility is private only
                items = osr.Items;
                isprivate = false;
                isVisibilityPending = false;
                ispublic = false;
                isUserPending = false;
                foreach (var item in items) {
                    if (item.Title.Text == "community-private-1") {
                        foreach (var cat in item.Categories) {
                            if (cat.Name == "visibility") {
                                if (cat.Label == "private") isprivate = true;
                                else if (cat.Label == "public") ispublic = true;
                            } else if (cat.Name == "userStatus" && cat.Label == "pending") isVisibilityPending = true;
                        }
                        foreach (var author in item.Authors) {
                            bool isUsr1 = false;
                            var elements = author.ElementExtensions.ReadElementExtensions<string>("identifier", "http://purl.org/dc/elements/1.1/");
                            if (elements.Count == 1 && elements [0] == usr1.Username) isUsr1 = true;
                            if (isUsr1) {
                                elements = author.ElementExtensions.ReadElementExtensions<string>("status", "http://purl.org/dc/elements/1.1/");
                                if (elements.Count == 1 && elements [0] == RoleTep.PENDING) isUserPending = true;
                            }
                        }
                    }
                }
                Assert.True(isprivate);
                Assert.False(isVisibilityPending);
                Assert.False(isUserPending);
                Assert.False(ispublic);

                //remove from community
                community.RemoveUser(usr1);

                //check how many communities user can see
                communities = new EntityList<ThematicCommunity>(context);
                communities.Identifier = "community";
                communities.OpenSearchEngine = ose;
                osr = ose.Query(communities, parameters);
                Assert.AreEqual(NBCOMMUNITY_PUBLIC, osr.TotalResults);

            } catch (Exception e) {
                Assert.Fail(e.Message);
            } finally {
                context.EndImpersonation();
            }
        }


    }
}

