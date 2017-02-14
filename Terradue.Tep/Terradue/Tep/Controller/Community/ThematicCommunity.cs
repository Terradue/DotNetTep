using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Terradue.OpenSearch;
using Terradue.OpenSearch.Result;
using Terradue.Portal;
using Terradue.Portal.OpenSearch;
using Terradue.ServiceModel.Syndication;

namespace Terradue.Tep {

    public class ThematicCommunity : Domain {

        public string AppsLink { get; set; }

        private UserTep owner;
        public UserTep Owner {
            get {
                if (owner == null) {
                    owner = GetOwner();
                }
                return owner;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Terradue.Tep.ThematicGroup"/> class.
        /// </summary>
        /// <param name="context">Context.</param>
        public ThematicCommunity(IfyContext context) : base(context) { }

        /// <summary>
        /// Froms the identifier.
        /// </summary>
        /// <returns>The identifier.</returns>
        /// <param name="context">Context.</param>
        /// <param name="id">Identifier.</param>
        public static new ThematicCommunity FromId(IfyContext context, int id) {
            ThematicCommunity result = new ThematicCommunity(context);
            result.Id = id;
            try {
                result.Load();
            } catch (Exception e) {
                throw e;
            }
            return result;
        }

        public static new ThematicCommunity FromIdentifier(IfyContext context, string identifier) {
            ThematicCommunity result = new ThematicCommunity(context);
            result.Identifier = identifier;
            try {
                result.Load();
            } catch (Exception e) {
                throw e;
            }
            return result;
        }

        public override void Load() {
            base.Load();
            AppsLink = LoadAppsLink();
        }

        public override void Store() {
            base.Store();
            StoreAppsLink(AppsLink);
        }

        /// <summary>
        /// Is the user owner.
        /// </summary>
        /// <returns><c>true</c>, if user is owner of the community, <c>false</c> otherwise.</returns>
        /// <param name="userid">Userid.</param>
        public bool IsUserOwner(int userid) {
            return (Owner != null && Owner.Id == userid);
        }

        /// <summary>
        /// Gets the owner (or manager) of the Community
        /// </summary>
        /// <returns>The owner.</returns>
        private UserTep GetOwner() {
            var role = Role.FromIdentifier(context, RoleTep.OWNER);
            var usrs = role.GetUsers(this.Id);
            if (usrs != null && usrs.Length > 0)
                return UserTep.FromId(context, usrs [0]);
            else return null;
        }

        /// <summary>
        /// Sets the owner.
        /// </summary>
        /// <param name="user">User.</param>
        public void SetOwner(UserTep user) {
            //only admin can do this
            if (context.AccessLevel != EntityAccessLevel.Administrator) throw new UnauthorizedAccessException("Only administrators can change the owner of this entity");
            var role = Role.FromIdentifier(context, RoleTep.OWNER);
            role.GrantToUser(user, this);
        }

        /// <summary>
        /// Sets the user role.
        /// </summary>
        /// <param name="user">User.</param>
        /// <param name="role">Role.</param>
        public void SetUserRole(User user, Role role) {
            //only owner can do this
            if (!IsUserOwner(context.UserId)) throw new UnauthorizedAccessException("Only owner can add new users");

            //delete previous roles
            var roles = Role.GetUserRolesForDomain(context, user.Id, this.Id);
            foreach (var r in roles) r.RevokeFromUser(user, this);

            //add new role
            role.GrantToUser(user, this);
        }

        /// <summary>
        /// Joins the current user.
        /// </summary>
        public void JoinCurrentUser() {
            Role role = Role.FromIdentifier(context, RoleTep.MEMBER);

            if (this.Kind == DomainKind.Public) {
                //public community -> user can always join
                role.GrantToUser(context.UserId, this.Id);
            } else {
                //other communities, it means the user has been invited and must be on pending table
                if (!IsUserPending(context.UserId)) throw new UnauthorizedAccessException("Current user not pending in Community");
                SetUserAsDefinitiveMember(context.UserId);
            }
        }

        /// <summary>
        /// Is the user pending.
        /// </summary>
        /// <returns><c>true</c>, if user is pending, <c>false</c> otherwise.</returns>
        /// <param name="usrId">Usr identifier.</param>
        public bool IsUserPending(int usrId) {
            Role role = Role.FromIdentifier(context, RoleTep.PENDING);
            return role.IsGrantedTo(false, usrId, this.Id);
        }

        /// <summary>
        /// Sets user as temporary member.
        /// </summary>
        /// <param name="id">User or Group Identifier.</param>
        /// <param name="roleId">Role identifier.</param>
        public void SetUserAsTemporaryMember(int id, int roleId) {
            //only owner can do this
            if (!IsUserOwner(context.UserId)) throw new UnauthorizedAccessException("Only owner can add new users");

            //to set as temporary user we give a temporary pending role
            Role role = Role.FromIdentifier(context, RoleTep.PENDING);
            role.GrantToUser(id, this.Id);

            //we now put to the role the manager wanted
            role = Role.FromId(context, roleId);
            role.GrantToUser(id, this.Id);
        }

        /// <summary>
        /// Sets the group as temporary member.
        /// </summary>
        /// <param name="grpId">Group identifier.</param>
        /// <param name="roleId">Role identifier.</param>
        public void SetGroupAsTemporaryMember(int grpId, int roleId) {
            Group grp = Group.FromId(context, grpId);
            var users = grp.GetUsers();
            foreach (var usr in users) SetUserAsTemporaryMember(usr.Id, roleId);
        }

        /// <summary>
        /// Sets the user as definitive member.
        /// </summary>
        /// <param name="userId">User identifier.</param>
        private void SetUserAsDefinitiveMember(int userId) {
            User user = User.FromId(context, userId);
            Role role = Role.FromIdentifier(context, RoleTep.PENDING);
            role.RevokeFromUser(user, this);
        }

        /// <summary>
        /// Sets the group as definitive member.
        /// </summary>
        /// <param name="groupId">Group identifier.</param>
        public void SetGroupAsDefinitiveMember(int groupId) {
            Group grp = Group.FromId(context, groupId);
            var users = grp.GetUsers();
            foreach (var usr in users) SetUserAsDefinitiveMember(usr.Id);
        }

        /// <summary>
        /// Removes the user.
        /// </summary>
        /// <param name="user">User.</param>
        public void RemoveUser(User user) {
            if (context.UserId != user.Id && !IsUserOwner(context.UserId)) throw new UnauthorizedAccessException("Only owner can remove users");

            //delete previous role(s)
            var uroles = Role.GetUserRolesForDomain(context, user.Id, this.Id);
            foreach (var r in uroles) r.RevokeFromUser(user, this);
        }

        /// <summary>
        /// Gets the thematic application.
        /// </summary>
        /// <returns>The thematic application.</returns>
        public ThematicApplication GetThematicApplication() {
            var apps = new EntityList<ThematicApplication>(context);
            apps.SetFilter("Kind", ThematicApplication.KINDRESOURCESETAPPS.ToString());
            apps.SetFilter("DomainId", Id.ToString());
            apps.Load();

            var items = apps.GetItemsAsList();
            if (items != null && items.Count > 0) {
                return items [0];
            }
            return null;
        }

        /// <summary>
        /// Gets the apps link.
        /// </summary>
        /// <returns>The apps link.</returns>
        private string LoadAppsLink() {
            var app = GetThematicApplication();
            if (app == null) return string.Empty;

            app.LoadItems();
            var resources = app.Items.GetItemsAsList();
            if (resources != null && resources.Count > 0) {
                //we assume for now that we have only one link per Community
                return resources [0].Location;
            }
            return string.Empty;
        }

        private void StoreAppsLink(string link) {
            var app = GetThematicApplication();
            if (app == null) return;

            //delete old links
            app.LoadItems();
            foreach (var resource in app.Items) {
                resource.Delete();
            }

            //add new link
            var appResource = new RemoteResource(context);
            appResource.Location = link;
            appResource.ResourceSet = app;
            appResource.Store();
        }

        /// <summary>
        /// Shares the entity to the community.
        /// </summary>
        /// <param name="entity">Entity.</param>
        public void ShareEntity(Entity entity) {
            //current user must own the entity
            if (entity.OwnerId != context.UserId) throw new Exception("Only owner can share an entity");

            //current user must have role in community
            var uroles = Role.GetUserRolesForDomain(context, context.UserId, this.Id);
            if (uroles.Length == 0) throw new Exception("Only a member of the community can share an entity");

            entity.DomainId = this.Id;
            entity.Store();

        }

        /// <summary>
        /// Unshare the entity.
        /// </summary>
        /// <param name="entity">Entity.</param>
        public void UnShareEntity(Entity entity) {
            //current user must own the entity
            if (entity.OwnerId != context.UserId) throw new Exception("Only owner can share an entity");

            //current user must have role in community
            var uroles = Role.GetUserRolesForDomain(context, context.UserId, this.Id);
            if (uroles.Length == 0) throw new Exception("Only a member of the community can share an entity");

            var currentUser = User.FromId(context, context.UserId);
            entity.DomainId = currentUser.DomainId;
            entity.Store();
        }


        #region IAtomizable

        public override bool IsPostFiltered(NameValueCollection parameters) {
            foreach (var key in parameters.AllKeys) {
                switch (parameters [key]) {
                case "correlatedTo":
                    return true;
                default:
                    return true;
                }
            }
            return true;
        }

        public new AtomItem ToAtomItem(NameValueCollection parameters) {
            AtomItem result = base.ToAtomItem(parameters);
            if (result == null) return null;

            //TODO: entity keyword specific to Community ?

            //owner
            if (Owner != null) {
                var ownerUri = Owner.GetUserPageLink();
                SyndicationPerson ownerPerson = new SyndicationPerson(Owner.Email, Owner.FirstName + " " + Owner.LastName, ownerUri);
                ownerPerson.ElementExtensions.Add(new SyndicationElementExtension("identifier", "http://purl.org/dc/elements/1.1/", Owner.Username));
                ownerPerson.ElementExtensions.Add(new SyndicationElementExtension("role", "http://purl.org/dc/elements/1.1/", RoleTep.OWNER));
                result.Authors.Add(ownerPerson);
            }

            //check pending users (only for non public communities)
            if (this.Kind != DomainKind.Public) {
                //remove authors
                result.Authors.Clear();
                var roles = new EntityList<Role>(context);
                roles.Load();
                foreach (var role in roles) {
                    if (role.Identifier != RoleTep.PENDING) {
                        var usrs = role.GetUsers(this.Id);
                        foreach (var usrId in usrs) {
                            User usr = User.FromId(context, usrId);
                            SyndicationPerson author = new SyndicationPerson(usr.Email, usr.FirstName + " " + usr.LastName, "");
                            author.ElementExtensions.Add(new SyndicationElementExtension("identifier", "http://purl.org/dc/elements/1.1/", usr.Username));
                            author.ElementExtensions.Add(new SyndicationElementExtension("role", "http://purl.org/dc/elements/1.1/", role.Identifier));
                            if (IsUserPending(usr.Id)) {
                                author.ElementExtensions.Add(new SyndicationElementExtension("status", "http://purl.org/dc/elements/1.1/", RoleTep.PENDING));
                            }
                            result.Authors.Add(author);
                        }
                    }
                }
            }

            AppsLink = LoadAppsLink();
            if (!string.IsNullOrEmpty(AppsLink)) result.Links.Add(new SyndicationLink(new Uri(AppsLink), "via", "", "application/atom+xml", 0));

            if (IsUserPending(context.UserId)) {
                result.Categories.Add(new SyndicationCategory("userStatus", null, "pending"));
            }

            return result;
        }

        public override KeyValuePair<string, string> GetFilterForParameter(string parameter, string value) {
            switch (parameter) {
            case "correlatedTo":
                var entity = new UrlBasedOpenSearchable(context, new OpenSearchUrl(value), MasterCatalogue.OpenSearchEngine).Entity;
                if (entity is EntityList<WpsJob>) {
                    var entitylist = entity as EntityList<WpsJob>;
                    var items = entitylist.GetItemsAsList();
                    if (items.Count > 0) { 
                        return new KeyValuePair<string, string>("Id", items[0].DomainId.ToString());
                    }
                } else if (entity is EntityList<DataPackage>) {
                    var entitylist = entity as EntityList<DataPackage>;
                    var items = entitylist.GetItemsAsList();
                    if (items.Count > 0) {
                        return new KeyValuePair<string, string>("Id", items [0].DomainId.ToString());
                    }
                }
                return new KeyValuePair<string, string>("DomainId", "0");
            default:
                return base.GetFilterForParameter(parameter, value);
            }
        }


        #endregion
    }

    public class ThematicGroupFactory {

        private IfyContext Context;

        public ThematicGroupFactory(IfyContext context) {
            Context = context;
        }

        public void CreateVisitorRole() {
            //Create Role
            var starterRole = new Role(Context);
            starterRole.Identifier = "visitor";
            starterRole.Name = "visitor";
            starterRole.Store();

            //Add Privileges
            //Data Package -- All
            starterRole.IncludePrivileges(Privilege.Get(EntityType.GetEntityType(typeof(DataPackage))));


        }

        public void CreateStarterRole() {

            //Create Role
            var starterRole = new Role(Context);
            starterRole.Identifier = "starter";
            starterRole.Name = "starter";
            starterRole.Store();

            //Add Privileges
            //Data Package -- All
            starterRole.IncludePrivileges(Privilege.Get(EntityType.GetEntityType(typeof(DataPackage))));

        }

        public void CreateExplorerRole() { }

    }
}
