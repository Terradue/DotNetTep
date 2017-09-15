using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;
using Terradue.OpenSearch;
using Terradue.OpenSearch.Result;
using Terradue.Portal;
using Terradue.Portal.OpenSearch;
using Terradue.ServiceModel.Ogc.Owc.AtomEncoding;
using Terradue.ServiceModel.Syndication;

namespace Terradue.Tep {

    [EntityTable(null, EntityTableConfiguration.Custom, Storage = EntityTableStorage.Above, AllowsKeywordSearch = true)]
    public class ThematicCommunity : Domain {

        private string appslink;
        public string AppsLink { 
            get {
                if (string.IsNullOrEmpty(appslink)) {
                    appslink = LoadAppsLink();
                }
                return appslink;
            }
            set {
                appslink = value;
            }
        }

        [EntityDataField("discuss")]
        public string DiscussCategory { get; set; }

        [EntityDataField("id_role_default")]
        public int DefaultRoleId { get; set; }

        private Role defaultRole;
        public string DefaultRoleName { 
            get {
                if (defaultRole == null && DefaultRoleId != 0) {
                    defaultRole = Role.FromId(context, DefaultRoleId);
                }
                if (defaultRole != null) return defaultRole.Identifier;
                return Role.FromIdentifier(context, RoleTep.MEMBER).Identifier;
            }
            set {
                if (value != null) {
                    defaultRole = Role.FromIdentifier(context, value);
                    DefaultRoleId = defaultRole.Id;
                }
            }
        }
        public string DefaultRoleDescription {
            get {
                if (defaultRole == null && DefaultRoleId != 0) {
                    defaultRole = Role.FromId(context, DefaultRoleId);
                }
                if (defaultRole != null) return defaultRole.Description;
                return Role.FromIdentifier(context, RoleTep.MEMBER).Description;
            }
        }

        private List<UserTep> owners;
        public List<UserTep> Owners {
            get {
                if (owners == null) {
                    owners = GetOwners();
                }
                return owners;
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
        }

        public override void Store() {
            if (string.IsNullOrEmpty(this.Identifier) && !string.IsNullOrEmpty(this.Name))
                this.Identifier = TepUtility.ValidateIdentifier(this.Name);
            base.Store();
            StoreAppsLink(AppsLink);
        }

        /// <summary>
        /// Is the user owner.
        /// </summary>
        /// <returns><c>true</c>, if user is owner of the community, <c>false</c> otherwise.</returns>
        /// <param name="userid">Userid.</param>
        public bool IsUserOwner(int userid) {
            if (Owners == null || Owners.Count == 0) return false;
            foreach (var own in Owners)
                if (own.Id == userid) return true;
            return false;
        }

        /// <summary>
        /// Can the user manage.
        /// </summary>
        /// <returns><c>true</c>, if user can manage, <c>false</c> otherwise.</returns>
        /// <param name="userid">Userid.</param>
        public bool CanUserManage(int userid) {
            return IsUserOwner(userid) || (context.AccessLevel == EntityAccessLevel.Administrator);
        }

        /// <summary>
        /// Can the user manage collection.
        /// </summary>
        /// <returns><c>true</c>, if user can manage collection, <c>false</c> otherwise.</returns>
        /// <param name="userid">Userid.</param>
        public bool CanUserManageCollection(int userid){
            var user = UserTep.FromId(context, userid);
            var roles = user.GetUserRoles(this);
            foreach(var role in roles){
                var perms = role.GetPrivileges();
                foreach(var p in perms){
                    if (p.Identifier == "series-m") return true;
                }
            }
            return false;
        }

		/// <summary>
		/// Can the user manage wps services.
		/// </summary>
		/// <returns><c>true</c>, if user can manage wps services, <c>false</c> otherwise.</returns>
		/// <param name="userid">Userid.</param>
		public bool CanUserManageService(int userid) {
			var user = UserTep.FromId(context, userid);
			var roles = user.GetUserRoles(this);
			foreach (var role in roles) {
				var perms = role.GetPrivileges();
				foreach (var p in perms) {
					if (p.Identifier == "service-m") return true;
				}
			}
			return false;
		}

        /// <summary>
        /// Gets the owner (or manager) of the Community
        /// </summary>
        /// <returns>The owner.</returns>
        private List<UserTep> GetOwners() {
            var role = Role.FromIdentifier(context, RoleTep.MANAGER);
            var usrs = role.GetUsers(this.Id);
            if (usrs != null && usrs.Length > 0) {
                List<UserTep> users = new List<UserTep>();
                foreach (var usr in usrs) users.Add(UserTep.FromId(context, usr));
                return users;
            }
            else return null;
        }

        /// <summary>
        /// Sets the owner.
        /// </summary>
        /// <param name="user">User.</param>
        public void SetOwner(UserTep user) {
            //only admin can do this
            if (context.AccessLevel != EntityAccessLevel.Administrator) throw new UnauthorizedAccessException("Only administrators can change the owner of this entity");
            var role = Role.FromIdentifier(context, RoleTep.MANAGER);
            role.GrantToUser(user, this);

            context.LogInfo(this, string.Format("Set owner ({0}) of community {1}", user.Username, this.Identifier));
        }

        /// <summary>
        /// Sets the user role.
        /// </summary>
        /// <param name="user">User.</param>
        /// <param name="role">Role.</param>
        public void SetUserRole(User user, Role role) {
            //only owner can do this
            if (!CanUserManage(context.UserId)) throw new UnauthorizedAccessException("Only owner can add new users");

            context.LogInfo(this, string.Format("Set role {0} to user {1} for community {2}", role.Identifier, user.Username, this.Identifier));

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
            
            if (this.Kind == DomainKind.Public) {
                //public community -> user can always join
                context.LogInfo(this, string.Format("Joining user {0} to PUBLIC community {1}", context.Username, this.Identifier));
                Role role = Role.FromIdentifier(context, this.DefaultRoleName);
                role.GrantToUser(context.UserId, this.Id);
            } else {
                //other communities, it means the user has been invited and must be on pending table
                if (!IsUserPending(context.UserId)) throw new UnauthorizedAccessException("Current user not pending in Community");
                context.LogInfo(this, string.Format("Joining user {0} to PRIVATE community {1}", context.Username, this.Identifier));

                SetUserAsDefinitiveMember(context.UserId);
            }
        }

        /// <summary>
        /// Is the user pending.
        /// </summary>
        /// <returns><c>true</c>, if user is pending, <c>false</c> otherwise.</returns>
        /// <param name="usrId">Usr identifier.</param>
        public bool IsUserPending(int usrId) {
            if (usrId == 0) return false;
            Role role = Role.FromIdentifier(context, RoleTep.PENDING);
            return role.IsGrantedTo(false, usrId, this.Id);
        }

        /// <summary>
        /// Is the user joined.
        /// </summary>
        /// <returns><c>true</c>, if user is joined, <c>false</c> otherwise.</returns>
        /// <param name="usrId">Usr identifier.</param>
        public bool IsUserJoined(int usrId) {
            if (usrId == 0 || IsUserPending(usrId)) return false;

            var uroles = Role.GetUserRolesForDomain(context, usrId, this.Id);
            return uroles.Length > 0;
        }

        /// <summary>
        /// Sets the user as temporary member.
        /// </summary>
        /// <param name="user">User.</param>
        /// <param name="roleId">Role identifier.</param>
        public void SetUserAsTemporaryMember(User user, int roleId) {
            //only owner can do this
            if (!CanUserManage(context.UserId)) throw new UnauthorizedAccessException("Only owner can add new users");

            //to set as temporary user we give a temporary pending role
            Role role = Role.FromIdentifier(context, RoleTep.PENDING);
            role.GrantToUser(user.Id, this.Id);

            context.LogInfo(this, string.Format("User {0} set pending for community {1} with role {2}", user.Username, this.Identifier, role.Identifier));

            //we now put to the role the manager wanted
            role = Role.FromId(context, roleId);
            role.GrantToUser(user.Id, this.Id);

            try {
                string emailFrom = context.GetConfigValue("MailSenderAddress");
                string subject = context.GetConfigValue("CommunityJoinEmailSubject");
                subject = subject.Replace("$(SITENAME)", context.GetConfigValue("SiteName"));
                subject = subject.Replace("$(COMMUNITY)", this.Name);
                string body = context.GetConfigValue("CommunityJoinEmailBody");
                body = body.Replace("$(COMMUNITY)", this.Name);
                body = body.Replace("$(LINK)", context.GetConfigValue("CommunityPageUrl"));
                context.SendMail(emailFrom, user.Email, subject, body);
            } catch (Exception) { }
        }

        /// <summary>
        /// Sets the group as temporary member.
        /// </summary>
        /// <param name="grpId">Group identifier.</param>
        /// <param name="roleId">Role identifier.</param>
        public void SetGroupAsTemporaryMember(int grpId, int roleId) {
            Group grp = Group.FromId(context, grpId);
            var users = grp.GetUsers();
            foreach (var usr in users) SetUserAsTemporaryMember(usr, roleId);
        }

        /// <summary>
        /// Sets the user as definitive member.
        /// </summary>
        /// <param name="userId">User identifier.</param>
        private void SetUserAsDefinitiveMember(int userId) {
            User user = User.FromId(context, userId);
            Role role = Role.FromIdentifier(context, RoleTep.PENDING);
            role.RevokeFromUser(user, this);

            context.LogInfo(this, string.Format("User {0} set as definitive member for community {1}", user.Username, this.Identifier));
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
            if (context.UserId != user.Id && !CanUserManage(context.UserId)) throw new UnauthorizedAccessException("Only owner can remove users");

            //delete previous role(s)
            var uroles = Role.GetUserRolesForDomain(context, user.Id, this.Id);
            foreach (var r in uroles) r.RevokeFromUser(user, this);

            context.LogInfo(this, string.Format("User {0} removed from community {1} (all roles revoked)", user.Username, this.Identifier));

        }

        /// <summary>
        /// Gets the users.
        /// </summary>
        /// <returns>The users.</returns>
        public List<UserTep> GetUsers(){
            List<UserTep> users = new List<UserTep>();

			var roles = new EntityList<Role>(context);
			roles.Load();

			foreach (var role in roles) {
				if (role.Identifier != RoleTep.PENDING) {
					var usersIds = role.GetUsers(this.Id).ToList();
					if (usersIds.Count > 0) {
						foreach (var usrId in usersIds) {
							var user = UserTep.FromId(context, usrId);
							users.Add(user);
						}
					}
				}
			}

            return users;
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
                return items[0];
            }

            //the Thematic Application does not exists, we create it
            ThematicApplication app = new ThematicApplication(context);
            app.Kind = ThematicApplication.KINDRESOURCESETAPPS;
            app.Identifier = Guid.NewGuid().ToString();
            app.DomainId = this.Id;
            app.Store();
            return app;
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
                return resources[0].Location;
            }
            return string.Empty;
        }

        private void StoreAppsLink(string link) {
            var app = GetThematicApplication();

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
            context.LogInfo(this, string.Format("Share entity {0} ({2}) to community {1}", entity.Identifier, this.Identifier, entity.GetType().ToString()));
 
            //current user must have role in community
            var uroles = Role.GetUserRolesForDomain(context, context.UserId, this.Id);
            if (uroles.Length == 0) throw new Exception("Only a member of the community can share an entity");

            entity.DomainId = this.Id;
            entity.Store();

        }

        #region IAtomizable

        public override bool IsPostFiltered(NameValueCollection parameters) {
            foreach (var key in parameters.AllKeys) {
                switch (key) {
                case "status":
                    return true;
                default:
                    break;
                }
            }
            return false;
        }

        public override int GetEntityListTotalResults(IfyContext context, NameValueCollection parameters) {
            //get all domains public + domains where current user has role which are not User private domains
            var sql = string.Format("SELECT count(DISTINCT id) FROM domain AS d LEFT JOIN rolegrant AS rg ON d.id=rg.id_domain WHERE d.kind={0} OR (d.kind != {1} AND rg.id_usr={2});", (int)DomainKind.Public, (int)DomainKind.User, context.UserId);
            var count = context.GetQueryIntegerValue(sql);
            return count;
        }

        public override AtomItem ToAtomItem(NameValueCollection parameters) {
            bool ispublic = this.Kind == DomainKind.Public;
            bool isprivate = this.Kind == DomainKind.Private;

            AtomItem result = new AtomItem();

            //we only want thematic groups domains (public or private)
            if (!ispublic && !isprivate) return null;

            bool isJoined = IsUserJoined(context.UserId);
            bool isPending = IsUserPending(context.UserId);

            if (!string.IsNullOrEmpty(parameters["status"])) {
                if (parameters["status"] == "joined" && !isJoined) return null;
                else if (parameters["status"] == "pending" && !isPending) return null;
                else if (parameters["status"] == "unjoined" && isJoined) return null;
            }
                
            if (isPending) {
                result.Categories.Add(new SyndicationCategory("status", null, "pending"));
            } else if (isJoined) {
                result.Categories.Add(new SyndicationCategory("status", null, "joined"));
            } else if (!ispublic) return null;

            if (string.IsNullOrEmpty(this.Name)) this.Name = this.Identifier;

            var entityType = EntityType.GetEntityType(typeof(ThematicCommunity));
            Uri id = new Uri(context.BaseUrl + "/" + entityType.Keyword + "/search?id=" + this.Identifier);

            result.Id = id.ToString();
            result.Title = new TextSyndicationContent(Name);
            result.Content = new TextSyndicationContent(Name);

            result.ElementExtensions.Add("identifier", "http://purl.org/dc/elements/1.1/", this.Identifier);
            result.Summary = new TextSyndicationContent(Description);
            result.ReferenceData = this;
            result.PublishDate = new DateTimeOffset(DateTime.UtcNow);

            //owner
            if (Owners != null) {
                foreach (var own in Owners) {
                    var ownerUri = own.GetUserPageLink();
                    SyndicationPerson ownerPerson = new SyndicationPerson(null, own.FirstName + " " + own.LastName, ownerUri);
                    ownerPerson.ElementExtensions.Add(new SyndicationElementExtension("identifier", "http://purl.org/dc/elements/1.1/", own.Username));
                    result.Authors.Add(ownerPerson);
                }
            }

            result.Links.Add(new SyndicationLink(id, "self", Identifier, "application/atom+xml", 0));
            if (!string.IsNullOrEmpty(IconUrl)) {
                Uri uri;
                if (IconUrl.StartsWith("http")) {
                    uri = new Uri(IconUrl);
                } else {
                    var urib = new UriBuilder(System.Web.HttpContext.Current.Request.Url);
                    urib.Path = IconUrl;
                    uri = urib.Uri;
                }

                result.Links.Add(new SyndicationLink(uri, "icon", "", base.GetImageMimeType(IconUrl), 0));
            }

            result.Categories.Add(new SyndicationCategory("visibility", null, ispublic ? "public" : "private"));
            result.Categories.Add(new SyndicationCategory("defaultRole", null, DefaultRoleName));
            result.Categories.Add(new SyndicationCategory("defaultRoleDescription", null, DefaultRoleDescription));

            //overview
            var roles = new EntityList<Role>(context);
            roles.Load();
            var rolesOverview = new List<RoleOverview>();
            foreach (var role in roles) {
                if (role.Identifier != RoleTep.PENDING) {
                    var usersIds = role.GetUsers(this.Id).ToList();
                    if (usersIds.Count > 0) {
                        rolesOverview.Add(new RoleOverview { Count = usersIds.Count, Value = role.Identifier });
                    }
                }
            }
            result.ElementExtensions.Add("overview", "https://standards.terradue.com", rolesOverview);

            //we show these info only for owner and only for specific id view
            if (!string.IsNullOrEmpty(parameters["uid"]) || !string.IsNullOrEmpty(parameters["id"])) {
                if (isJoined) {
                    if (!string.IsNullOrEmpty(AppsLink)) {
                        result.Links.Add(new SyndicationLink(new Uri(AppsLink), "related", "apps", "application/atom+xml", 0));

                        //get all data collections
                        var settings = new OpenSearchableFactorySettings(MasterCatalogue.OpenSearchEngine);
                        try {
                            var apps = MasterCatalogue.OpenSearchEngine.Query(new GenericOpenSearchable(new OpenSearchUrl(AppsLink), settings), new NameValueCollection(), typeof(AtomFeed));
                            foreach (IOpenSearchResultItem item in apps.Items) {
                                try {
                                    var offerings = item.ElementExtensions.ReadElementExtensions<OwcOffering>("offering", OwcNamespaces.Owc, new System.Xml.Serialization.XmlSerializer(typeof(OwcOffering)));
                                    if (offerings != null) {
                                        foreach (var off in offerings) {
                                            if (off.Operations != null) {
                                                foreach (var ops in off.Operations) {
                                                    if (ops.Any == null || ops.Any[0] == null || ops.Any[0].InnerText == null) continue;
                                                    var appTitle = item.Title != null ? item.Title.Text : item.Identifier;
                                                    if (ops.Code == "ListSeries") {
                                                        EntityList<Collection> collections = new EntityList<Collection>(context);
                                                        Terradue.OpenSearch.Engine.OpenSearchEngine ose = MasterCatalogue.OpenSearchEngine;
                                                        var uri = new Uri(ops.Href);
                                                        var nvc = HttpUtility.ParseQueryString(uri.Query);
                                                        var resultColl = ose.Query(collections, nvc);
                                                        foreach (var itemColl in resultColl.Items) {
                                                            var itemCollIdTrim = itemColl.Identifier.Trim().Replace(" ", "");
                                                            var any = ops.Any[0].InnerText.Trim();
                                                            var anytrim = any.Replace(" ", "").Replace("*", itemCollIdTrim);
                                                            any = any.Replace("*", itemColl.Identifier);
                                                            var url = context.GetConfigValue("BaseUrl") + "/geobrowser/?id=" + item.Identifier.Trim() + "#!context=" + System.Web.HttpUtility.UrlEncode(anytrim);
                                                            var sLink = new SyndicationLink(new Uri(url), "related", any + " (" + appTitle + ")", "application/atom+xml", 0);
                                                            if (any != string.Empty && !result.Links.Contains(sLink)) result.Links.Add(sLink);
                                                        }
                                                    } else {
                                                        var any = ops.Any[0].InnerText.Trim();
                                                        var anytrim = any.Replace(" ", "");
                                                        var url = context.GetConfigValue("BaseUrl") + "/geobrowser/?id=" + item.Identifier.Trim() + "#!context=" + System.Web.HttpUtility.UrlEncode(anytrim);
                                                        var sLink = new SyndicationLink(new Uri(url), "related", any + " (" + appTitle + ")", "application/atom+xml", 0);
                                                        if (any != string.Empty && !result.Links.Contains(sLink)) result.Links.Add(sLink);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }catch(Exception e){
                                    context.LogError(this, e != null ? e.Message : "Error while getting thematic applications of community " + this.Name);
                                }
                            }
                        }catch(Exception e){
                            context.LogError(this, e !=null ? e.Message : "Error while getting thematic applications of community " + this.Name);
                        }
                    }
                    if (!string.IsNullOrEmpty(DiscussCategory)) result.ElementExtensions.Add("discussCategory", "https://standards.terradue.com", DiscussCategory);
                    var usersCommunity = new List<UserRole>();
                    foreach (var role in roles) {
                        if (role.Identifier != RoleTep.PENDING) {
                            var usersIds = role.GetUsers(this.Id).ToList();
                            if (usersIds.Count > 0) {
                                foreach (var usrId in usersIds) {
                                    var user = UserTep.FromId(context, usrId);
                                    usersCommunity.Add(new UserRole {
                                        Username = user.Username,
                                        Name = user.FirstName + " " + user.LastName,
                                        Role = role.Name ?? role.Identifier,
                                        RoleDescription = role.Description,
                                        Status = IsUserPending(usrId) ? "pending" : "joined",
                                        Avatar = user.GetAvatar()
                                    });
                                }
                            }
                        }
                    }
                    result.ElementExtensions.Add("users", "https://standards.terradue.com", usersCommunity);
                }
            }

            return result;
        }

        public override KeyValuePair<string, string> GetFilterForParameter(string parameter, string value) {
            switch (parameter) {
            case "uid":
                return new KeyValuePair<string, string>("Identifier", value);
            case "id":
                return new KeyValuePair<string, string>("Identifier", value);
            case "correlatedTo":
                var settings = new OpenSearchableFactorySettings(MasterCatalogue.OpenSearchEngine);
                    var entity = new UrlBasedOpenSearchable(context, new OpenSearchUrl(value), settings).Entity;
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
                        return new KeyValuePair<string, string>("Id", items[0].DomainId.ToString());
                    }
                }
                return new KeyValuePair<string, string>("DomainId", "0");
            default:
                return base.GetFilterForParameter(parameter, value);
            }
        }

        public new NameValueCollection GetOpenSearchParameters() {
            NameValueCollection nvc = base.GetOpenSearchParameters();
            nvc.Add("status", "{t2:status?}");
            return nvc;
        }



        #endregion
    }

    [DataContract]
    public class RoleOverview {
        [DataMember]
        public int Count { get; set; }
        [DataMember]
        public string Value { get; set; }

        public RoleOverview() { }

    }

    [DataContract]
    public class UserRole {
        [DataMember]
        public string Username { get; set; }
        [DataMember]
        public string Name { get; set; }
        [DataMember]
        public string Email { get; set; }
        [DataMember]
        public string Role { get; set; }
        [DataMember]
        public string RoleDescription { get; set; }
        [DataMember]
        public string Status { get; set; }
        [DataMember]
        public string Avatar { get; set; }

        public UserRole() { }

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

    public class CommunityCollection : EntityDictionary<ThematicCommunity> {

        private EntityType entityType;

        /// <summary>Indicates or decides whether the standard query is used for this domain collection.</summary>
        /// <remarks>If the value is true, a call to <see cref="Load">Load</see> produces a list containing all domains in which the user has a role and domains that are public. The default is <c><false</c>, which means that the normal behaviour of EntityCollection applies.</remarks>
        public bool UseNormalSelection { get; set; }

        public CommunityCollection(IfyContext context) : base(context) {
            this.entityType = GetEntityStructure();
            this.UseNormalSelection = false;
        }

        public override void Load() {
            if (UseNormalSelection) base.Load();
            else LoadRestricted();
        }

        /// <summary>Loads a collection of domains restricted by kinds and a user's roles.</summary>
        /// <param name="includedKinds">The domain kinds of domains on which a user has no explicit role but should in any case be included in the collection.</param>
        public void LoadRestricted(DomainKind[] includedKinds = null) {

            int[] kindIds;
            if (includedKinds == null) {
                kindIds = new int[] { (int)DomainKind.Public };
            } else {
                kindIds = new int[includedKinds.Length];
                for (int i = 0; i < includedKinds.Length; i++) kindIds[i] = (int)includedKinds[i];
            }

            int[] domainIds = Domain.GetGrantScope(context, UserId, null, null);

            //we want private communities in which User has a role OR public communities
            string condition = String.Format("((t.id IN ({0}) AND t.kind IN ({1})) OR t.kind IN ({2}))",
                                             domainIds.Length == 0 ? "0" : String.Join(",", domainIds),
                                             (int)DomainKind.Private,
                                             kindIds.Length == 0 ? "-1" : String.Join(",", kindIds)
            );

            Clear();

            object[] queryParts = entityType.GetListQueryParts(context, this, UserId, null, condition);
            string sql = entityType.GetCountQuery(queryParts);
            if (context.ConsoleDebug) Console.WriteLine("SQL (COUNT): " + sql);
            TotalResults = context.GetQueryLongIntegerValue(sql);

            sql = entityType.GetQuery(queryParts);
            if (context.ConsoleDebug) Console.WriteLine("SQL: " + sql);

            IDbConnection dbConnection = context.GetDbConnection();
            IDataReader reader = context.GetQueryResult(sql, dbConnection);
            IsLoading = true;
            while (reader.Read()) {
                ThematicCommunity item = entityType.GetEntityInstance(context) as ThematicCommunity;
                item.Load(entityType, reader, AccessLevel);
                IncludeInternal(item);
            }
            IsLoading = false;
            context.CloseQueryResult(reader, dbConnection);
        }

    }
}
