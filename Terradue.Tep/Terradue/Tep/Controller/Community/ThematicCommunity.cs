﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Web;
using ServiceStack.Text;
using Terradue.OpenSearch;
using Terradue.OpenSearch.Result;
using Terradue.Portal;
using Terradue.Portal.OpenSearch;
using Terradue.ServiceModel.Ogc.Owc.AtomEncoding;
using Terradue.ServiceModel.Syndication;

namespace Terradue.Tep {

    [EntityTable(null, EntityTableConfiguration.Custom, Storage = EntityTableStorage.Above, AllowsKeywordSearch = true)]
    public class ThematicCommunity : Domain {

        public const string USERSTATUS_JOINED = "joined";
        public const string USERSTATUS_UNJOINED = "unjoined";
        public const string USERSTATUS_PENDING = "pending";
        public const string USERSTATUS_ALL = "all";

        private List<string> appslinks;
        public List<string> AppsLinks { 
            get {
                if (appslinks == null) {
                    appslinks = new List<string>();
                    var app = GetThematicApplication();
                    if (app != null) {
                        app.LoadItems();
                        foreach (var item in app.Items) {
                            if (!string.IsNullOrEmpty(item.Location)) {
                                appslinks.Add(item.Location);
                            }
                        }
                    }
                }
                return appslinks;
            }
            set {
                appslinks = value;
            }
        }

        private List<RemoteResource> links;
        public List<RemoteResource> Links { 
            get {
                if (links == null) {
                    links = new List<RemoteResource>();
                    var link = GetDomainLinks();
                    if (link != null) {
                        link.LoadItems();
                        links.AddRange(link.Items);
                    }
                }
                return links;
            }
            set {
                links = value;
            }
        }

        [EntityDataField("discuss")]
        public string DiscussCategory { get; set; }

        [EntityDataField("id_role_default")]
        public int DefaultRoleId { get; set; }

        [EntityDataField("email_notification")]
        public bool EmailNotification { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="T:Terradue.Tep.ThematicCommunity"/> enable a user to request to join.
        /// </summary>
        /// <value><c>true</c> if enable user can request to join; otherwise, <c>false</c>.</value>
        [EntityDataField("enable_join")]
        public bool EnableJoinRequest { get; set; }

        [EntityDataField("contributor")]
        public string Contributor { get; set; }

        [EntityDataField("contributor_icon_url")]
        public string ContributorIcon { get; set; }

        [EntityDataField("usersync_identifier")]
        public string UserSyncIdentifier { get; set; }

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
        }

        /// <summary>
        /// Updates the apps links.
        /// </summary>
        /// <param name="apps">Apps.</param>
        public void UpdateAppsLinks(List<string> apps) {
            var app = this.GetThematicApplication();
            //delete old links
            app.LoadItems();
            foreach (var resource in app.Items) {
                resource.Delete();
            }
            app.LoadItems();
            //add new links
            foreach (var link in apps) {
                var res = new RemoteResource(context);
                res.Location = link;
                app.AddResourceItem(res);
            }
        }

        public void UpdateDomainsLinks(List<RemoteResource> links) {
            //store links
            var domainLinks = this.GetDomainLinks();
            if (domainLinks != null) {
                //delete old links
                domainLinks.LoadItems();
                foreach (var resource in domainLinks.Items) {
                    resource.Delete();
                }
                domainLinks.LoadItems();
            }

            //add new links
            if (links != null) {
                if (domainLinks == null) domainLinks = this.CreateDomainLinks();
                foreach (RemoteResource res in links) {
                    try {
                        new Uri(res.Location);//to validate the location
                        domainLinks.AddResourceItem(res);
                    } catch (Exception) { }
                }
            }
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

            SyncUserAdd(user);
        }

        /// <summary>
        /// Sets the user role.
        /// </summary>
        /// <param name="user">User.</param>
        /// <param name="role">Role.</param>
        public void SetUserRole(User user, Role role) {
            //only owner can do this
            if (!CanUserManage(context.UserId)) throw new UnauthorizedAccessException("Only owner can add new users");

            if (!string.IsNullOrEmpty(this.UserSyncIdentifier) && (string.IsNullOrEmpty(user.Affiliation) || string.IsNullOrEmpty(user.Country)))
                throw new Exception("User cannot be added to the community. Profile is not complete.");

            context.LogInfo(this, string.Format("Set role {0} to user {1} for community {2}", role.Identifier, user.Username, this.Identifier));

            //delete previous roles
            var roles = Role.GetUserRolesForDomain(context, user.Id, this.Id);
            bool ispending = false;
            foreach (var r in roles) {
                if (r.Identifier == RoleTep.PENDING) ispending = true;
                r.RevokeFromUser(user, this);
            }

            //add new role
            role.GrantToUser(user, this);

            string emailTo = user.Email;
            string emailFrom = context.GetConfigValue("MailSenderAddress");
            string subject = ispending ? context.GetConfigValue("CommunityPendingJoinConfirmationEmailSubject") : context.GetConfigValue("CommunityJoinConfirmationEmailSubject");
            subject = subject.Replace("$(SITENAME)", context.GetConfigValue("SiteName"));
            subject = subject.Replace("$(COMMUNITY)", this.Name);
            string body = ispending ? context.GetConfigValue("CommunityPendingJoinConfirmationEmailBody") : context.GetConfigValue("CommunityJoinConfirmationEmailBody");
            body = body.Replace("$(COMMUNITY)", this.Name);
            body = body.Replace("$(ASD_LINK)", context.GetConfigValue("asd_link"));
            body = body.Replace("$(LINK)", context.GetConfigValue("CommunityDetailPageUrl") + this.Identifier);
            body = body.Replace("$(SITENAME_SHORT)", context.GetConfigValue("SiteNameShort"));              
            context.SendMail(emailFrom, emailTo, subject, body);
        
            SyncUserAdd(user);            
        }

        /// <summary>
        /// Joins the current user.
        /// </summary>
        public void TryJoinCurrentUser(string objectives = "") {

            var user = User.FromId(context, context.UserId);

            if (!string.IsNullOrEmpty(this.UserSyncIdentifier) && (string.IsNullOrEmpty(user.Affiliation) || string.IsNullOrEmpty(user.Country)))
                throw new Exception("User cannot be added to the community. Profile is not complete.");

            if (this.Kind == DomainKind.Public) {
                //public community -> user can always join
                context.LogInfo(this, string.Format("Joining user {0} to PUBLIC community {1}", context.Username, this.Identifier));
                Role role = Role.FromIdentifier(context, this.DefaultRoleName);
                role.GrantToUser(context.UserId, this.Id);

                SyncUserAdd(user);

            } else {
                //private communities, we add user in pending table and request manager to add him
                this.SetUserAsTemporaryMember(user);

                //automatic confirmation email about pending review sent to user
                try {
                    string emailTo = user.Email;
                    string emailFrom = context.GetConfigValue("MailSenderAddress");
                    string subject = context.GetConfigValue("CommunityPendingEmailSubject");
                    subject = subject.Replace("$(SITENAME)", context.GetConfigValue("SiteName"));
                    subject = subject.Replace("$(COMMUNITY)", this.Name);
                    string body = context.GetConfigValue("CommunityPendingEmailBody");
                    body = body.Replace("$(COMMUNITY)", this.Name);                    
                    body = body.Replace("$(SITENAME_SHORT)", context.GetConfigValue("SiteNameShort"));                    
                    context.SendMail(emailFrom, emailTo, subject, body);
                } catch (Exception e) {
                    context.LogError(this, e.Message);
                }

                if(this.EmailNotification){
                    try {
                        var managers = GetOwners();
                        foreach (var owner in managers) {
                            string emailFrom = user.Email;
                            string subject = context.GetConfigValue("CommunityJoinEmailSubject");
                            subject = subject.Replace("$(SITENAME)", context.GetConfigValue("SiteName"));
                            subject = subject.Replace("$(COMMUNITY)", this.Name);
                            string body = context.GetConfigValue("CommunityJoinEmailBody");
                            body = body.Replace("$(COMMUNITY)", this.Name);
                            body = body.Replace("$(USERNAME)", user.Username);
                            body = body.Replace("$(USERMAIL)", user.Email);
                            body = body.Replace("$(LINK)", context.GetConfigValue("CommunityDetailPageUrl") + this.Identifier);
                            body = body.Replace("$(USER_REQUEST)", objectives);
                            context.SendMail(emailFrom, owner.Email, subject, body);
                        }
                    } catch (Exception e) {
                        context.LogError(this, e.Message);
                    }
                }
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
        public void SetUserAsTemporaryMember(User user) {
           
            //to set as temporary user we give a temporary pending role
            Role role = Role.FromIdentifier(context, RoleTep.PENDING);
            role.GrantToUser(user.Id, this.Id);

            context.LogInfo(this, string.Format("User {0} set pending for community {1}", user.Username, this.Identifier));
        }

        /// <summary>
        /// Sets the group as temporary member.
        /// </summary>
        /// <param name="grpId">Group identifier.</param>
        /// <param name="roleId">Role identifier.</param>
        public void SetGroupAsTemporaryMember(int grpId) {
            Group grp = Group.FromId(context, grpId);
            var users = grp.GetUsers();
            foreach (var usr in users) SetUserAsTemporaryMember(usr);
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

            SyncUserAdd(user);
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
        /// /// <param name="reason">Reason why the user is removed.</param>
        public void RemoveUser(User user, string reason = null) {
            if (context.UserId != user.Id && !CanUserManage(context.UserId)) throw new UnauthorizedAccessException("Only owner can remove users");

            bool isPending = IsUserPending(user.Id);

            //delete previous role(s)
            var uroles = Role.GetUserRolesForDomain(context, user.Id, this.Id);
            foreach (var r in uroles) r.RevokeFromUser(user, this);

            context.LogInfo(this, 
            isPending ? string.Format("Pending user {0} denied from community {1}", user.Username, this.Identifier)
                      : string.Format("User {0} removed from community {1} (all roles revoked)", user.Username, this.Identifier));
            
            if (!string.IsNullOrEmpty(reason)) {
                try {
                    string emailTo = user.Email;
                    string emailFrom = context.GetConfigValue("MailSenderAddress");
                    string subject = context.GetConfigValue(isPending ? "CommunityPendingRemoveEmailSubject" : "CommunityRemoveEmailSubject");
                    subject = subject.Replace("$(SITENAME)", context.GetConfigValue("SiteName"));
                    subject = subject.Replace("$(COMMUNITY)", this.Name);
                    string body = context.GetConfigValue(isPending ? "CommunityPendingRemoveEmailBody" : "CommunityRemoveEmailBody");
                    body = body.Replace("$(COMMUNITY)", this.Name);
                    body = body.Replace("$(REASON)", reason);
                    body = body.Replace("$(SITENAME_SHORT)", context.GetConfigValue("SiteNameShort"));
                    body = body.Replace("$(ASD_LINK)", context.GetConfigValue("asd_link"));
                    context.SendMail(emailFrom, emailTo, subject, body);
                } catch (Exception e) {
                    context.LogError(this, e.Message);
                }
            }

            SyncUserRemove(user);

        }

        /// <summary>
        /// Gets the users.
        /// </summary>
        /// <returns>The users.</returns>
        public List<UserTep> GetUsers(){
            List<UserTep> users = new List<UserTep>();
            var ids = GetUsersIds();
            foreach(var id in ids){
                users.Add(UserTep.FromId(context, id));
            }

            return users;
        }

        public List<int> GetUsersIds(){
            List<int> ids = new List<int>();
            var roles = new EntityList<Role>(context);
			roles.Load();

			foreach (var role in roles) {
				if (role.Identifier != RoleTep.PENDING) {
					var usersIds = role.GetUsers(this.Id).ToList();
                    foreach(var id in usersIds) 
                        if(!ids.Contains(id)) ids.Add(id);
                }
            }
            return ids;
        }

        public void SyncExistingUsersAdd(string syncIdentifier) {
            SyncExistingUsers(syncIdentifier, "POST");
        }
        public void SyncExistingUsersRemove(string syncIdentifier) {
            SyncExistingUsers(syncIdentifier, "DELETE");
        }
        private void SyncExistingUsers(string syncIdentifier, string action) {            
            foreach(var user in GetUsers()) {
                SyncUser(user, action, syncIdentifier);
            }
        }
        
        private void SyncUserAdd(User user) {
            if (string.IsNullOrEmpty(UserSyncIdentifier)) return;
            SyncUser(user, "POST");
        }
        private void SyncUserRemove(User user) {
            if (string.IsNullOrEmpty(UserSyncIdentifier)) return;
            SyncUser(user, "DELETE");
        }
        private void SyncUser(User user, string action) {
            if (string.IsNullOrEmpty(UserSyncIdentifier)) return;

            var syncidentifiers = UserSyncIdentifier.Split(',');
            foreach (var syncIdentifier in syncidentifiers) {
                SyncUser(user, action, syncIdentifier);
            }
        }

        private void SyncUser(User user, string action, string syncIdentifier) {
            if (string.IsNullOrEmpty(syncIdentifier)) return;
            try {
                if (action == "DELETE") {
                    //first we check that the user is not in another community that can synchronize with the same endpoint
                    var sql = string.Format("SELECT COUNT(*) FROM rolegrant " +
                        "WHERE id_usr={0} " +
                        "AND id_domain IN (SELECT id FROM domain WHERE (usersync_identifier = '{1}' OR usersync_identifier LIKE '%,{1}' OR usersync_identifier LIKE '{1},%' OR usersync_identifier LIKE '%,{1},%') AND id != {2}) " +
                        "AND id_role != (SELECT id FROM role WHERE identifier='pending');", user.Id, syncIdentifier, this.Id);
                    var count = context.GetQueryIntegerValue(sql);
                    if (count > 0) return;
                }

                var token = context.GetConfigValue(syncIdentifier + "-token");
                var syncUrl = context.GetConfigValue(syncIdentifier + "-sync-url");

                var a2sUser = new UserSync { email = user.Email, lastname = user.LastName, firstname = user.FirstName, company = user.Affiliation, platform = context.GetConfigValue("SiteNameShort") };
                var a2sInput = new UserSyncRequest { token = token, user = a2sUser, action = action };

                var request = (HttpWebRequest)WebRequest.Create(syncUrl);
                request.Proxy = null;
                request.Method = "PUT";
                request.ContentType = "application/json";
                request.Accept = "application/json";

                var payload = JsonSerializer.SerializeToString(a2sInput);

                using (var streamWriter = new StreamWriter(request.GetRequestStream())) {
                    streamWriter.Write(payload);
                    streamWriter.Flush();
                    streamWriter.Close();

                    System.Threading.Tasks.Task.Factory.FromAsync<WebResponse>(request.BeginGetResponse,
                                                                       request.EndGetResponse,
                                                                       null)
					.ContinueWith(task =>
					{
						var httpResponse = (HttpWebResponse) task.Result;						
					}).ConfigureAwait(false).GetAwaiter().GetResult();
                }

            } catch (Exception e) {
                context.LogError(this, "Sync A2sHpc user - " + e.Message, e);
            }
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

        public List<ThematicApplicationCached> GetThematicApplicationsCached() {
            var apps = new EntityList<ThematicApplicationCached>(context);            
            apps.SetFilter("DomainId", Id.ToString());
            apps.Load();

            var items = apps.GetItemsAsList();
            return items;
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

        public DomainLinks GetDomainLinks(){
            var links = new EntityList<DomainLinks>(context);
            links.SetFilter("Kind", DomainLinks.KINDRESOURCESETLINKS.ToString());
            links.SetFilter("DomainId", Id.ToString());
            links.Load();

            var items = links.GetItemsAsList();
            if (items != null && items.Count > 0) {
                return items[0];
            }

            return null;
        }

        public DomainLinks CreateDomainLinks(){
            //the Thematic Application does not exists, we create it
            DomainLinks link = new DomainLinks(context);
            link.Kind = DomainLinks.KINDRESOURCESETLINKS;
            link.Identifier = Guid.NewGuid().ToString();
            link.DomainId = this.Id;
            link.Store();
            return link;
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
            bool ishidden = this.Kind == DomainKind.Hidden;
            bool isprivate = this.Kind == DomainKind.Private;
            bool canusermanage = CanUserManage(context.UserId);

            AtomItem result = new AtomItem();

            //we only want thematic groups domains (public or private)
            if (!ispublic && !isprivate && !ishidden) return null;

            bool isJoined = IsUserJoined(UserId != 0 ? UserId : context.UserId);
            bool isPending = IsUserPending(UserId != 0 ? UserId : context.UserId);

            if (!string.IsNullOrEmpty(parameters["status"])) {
                if (parameters["status"] == USERSTATUS_JOINED && !isJoined) return null;
                else if (parameters["status"] == USERSTATUS_PENDING && !isPending) return null;
                else if (parameters["status"] == USERSTATUS_UNJOINED && (isJoined || isPending)) return null;
            }
            bool searchAll = false;
            if (!string.IsNullOrEmpty(parameters["visibility"])) { 
                switch(parameters["visibility"]){
                    case "public":
                        if (this.Kind != DomainKind.Public) return null;
                        break;
                    case "private":
                        if (this.Kind != DomainKind.Private) return null;
                        break;
                    case "hidden":
                        if (this.Kind != DomainKind.Hidden) return null;
                        break;
                    case "owned":
                        if (!canusermanage) return null;
                        break;
                    case "all":
                        searchAll = true;
                        break;
                }
            }

            if (isPending) {
                result.Categories.Add(new SyndicationCategory("status", null, USERSTATUS_PENDING));
            } else if (isJoined) {
                result.Categories.Add(new SyndicationCategory("status", null, USERSTATUS_JOINED));
            } else if (ishidden && !searchAll) return null;

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

            //DomainLinks
            if (Links != null && Links.Count > 0){
                foreach(var link in Links){
                    var uri = new Uri(link.Location);
                    result.Links.Add(new SyndicationLink(uri, "via", link.Name, "application/html", 0));
                }
            }

            if(!string.IsNullOrEmpty(Contributor) || !string.IsNullOrEmpty(ContributorIcon)){
                var a = new ServiceModel.Syndication.SyndicationPerson {
                    Name = Contributor,
                };
                if(!string.IsNullOrEmpty(ContributorIcon)) a.ElementExtensions.Add("icon", "http://www.terradue.com", ContributorIcon);
                a.ElementExtensions.Add("contributor", "http://www.terradue.com", "true");
                result.Authors.Add(a);
            }

            result.Categories.Add(new SyndicationCategory("visibility", null, ispublic ? "public" : (isprivate ? "private" : "hidden")));
            result.Categories.Add(new SyndicationCategory("defaultRole", null, DefaultRoleName));
            result.Categories.Add(new SyndicationCategory("defaultRoleDescription", null, DefaultRoleDescription));
            result.Categories.Add(new SyndicationCategory("emailNotification", null, EmailNotification ? "true" : "false"));
            result.Categories.Add(new SyndicationCategory("enableJoinRequest", null, EnableJoinRequest ? "true" : "false"));
            if (canusermanage && !string.IsNullOrEmpty(this.UserSyncIdentifier)) result.Categories.Add(new SyndicationCategory("usersync", null, this.UserSyncIdentifier));

            //overview
            var roles = new EntityList<Role>(context);
            roles.Load();
            var rolesOverview = new List<RoleOverview>();
            foreach (var role in roles) {
                if (canusermanage || role.Identifier != RoleTep.PENDING) {
                    var usersIds = role.GetUsers(this.Id).ToList();
                    if (usersIds.Count > 0) {
                        rolesOverview.Add(new RoleOverview { Count = usersIds.Count, Value = role.Identifier });
                    }
                }
            }
            result.ElementExtensions.Add("overview", "https://standards.terradue.com", rolesOverview);

            //we show these info only for member and only for specific id view
            if (((!string.IsNullOrEmpty(parameters["uid"]) || !string.IsNullOrEmpty(parameters["id"])) && isJoined)) {                
                //get all apps info
                result = GetInfoFromApps(result, AppsLinks);

                if (!string.IsNullOrEmpty(DiscussCategory)) result.ElementExtensions.Add("discussCategory", "https://standards.terradue.com", DiscussCategory);
                var usersCommunity = new List<UserRole>();
                foreach (var role in roles) {
                    //if (role.Identifier != RoleTep.PENDING) {
                        var usersIds = role.GetUsers(this.Id).ToList();
                        if (usersIds.Count > 0) {
                            foreach (var usrId in usersIds) {
                                var user = UserTep.FromId(context, usrId);
                                usersCommunity.Add(new UserRole {
                                    Username = user.Username,
                                    Name = user.FirstName + " " + user.LastName,
                                    Email = CanUserManage(context.UserId) ? (!string.IsNullOrEmpty(user.Email) ? user.Email : "") : "",
                                    Affiliation = IsUserJoined(context.UserId) ? (!string.IsNullOrEmpty(user.Affiliation) ? user.Affiliation : "") : "",
                                    Country = IsUserJoined(context.UserId) ? (!string.IsNullOrEmpty(user.Country) ? user.Country : "") : "",
                                    Role = role.Name ?? role.Identifier,
                                    RoleDescription = role.Description,
                                    Status = IsUserPending(usrId) ? USERSTATUS_PENDING : USERSTATUS_JOINED,
                                    Level = CanUserManage(context.UserId) ? user.Level : 0,
                                    Avatar = user.GetAvatar()
                                });
                            }
                        }
                    //}
                }
                result.ElementExtensions.Add("users", "https://standards.terradue.com", usersCommunity);                
            }

            return result;
        }

        public AtomItem GetInfoFromApps(AtomItem result, List<string> appsLinks, bool collection = true, bool wps = true){
            List<CollectionOverview> collectionsOverviews = new List<CollectionOverview>();
            List<WpsServiceOverview> wpssOverviews = new List<WpsServiceOverview>();
            if (appsLinks != null) {
                foreach (var appslink in appsLinks) {
                    result.Links.Add(new SyndicationLink(new Uri(appslink), "related", "apps", "application/atom+xml", 0));
                    try {
                        var settings = MasterCatalogue.OpenSearchFactorySettings;
                        var apps = MasterCatalogue.OpenSearchEngine.Query(new GenericOpenSearchable(new OpenSearchUrl(appslink), settings), new NameValueCollection(), typeof(AtomFeed));
                        foreach (IOpenSearchResultItem item in apps.Items) {

                            if(collection){
                                //get data collections                            
                                var collectionOverviews = ThematicAppFactory.GetDataCollectionOverview(context, item);
                                collectionsOverviews.AddRange(collectionOverviews);
                            }

                            if(wps){
                                //get wps services                            
                                var wpsOverviews = ThematicAppFactory.GetWpsServiceOverviews(context, item);
                                wpssOverviews.AddRange(wpsOverviews);
                            }
                        }
                    } catch (Exception e) {
                        context.LogError(this, e != null ? e.Message : "Error while getting thematic applications of community " + this.Name);
                    }
                }
            }
            if(wpssOverviews.Count > 0) result.ElementExtensions.Add("wps", "https://standards.terradue.com", wpssOverviews);
            if (collectionsOverviews.Count > 0) result.ElementExtensions.Add("collection", "https://standards.terradue.com", collectionsOverviews);
            return result;
        }

        public override object GetFilterForParameter(string parameter, string value) {
            switch (parameter) {
            case "uid":
                return new KeyValuePair<string, string>("Identifier", value);
            case "id":
                return new KeyValuePair<string, string>("Identifier", value);
            case "correlatedTo":
                    var settings = MasterCatalogue.OpenSearchFactorySettings;
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
                } else if (entity is EntityList<WpsProcessOffering>) {
                    var entitylist = entity as EntityList<WpsProcessOffering>;
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
        public string Affiliation { get; set; }
        [DataMember]
        public string Country { get; set; }
        [DataMember]
        public string Role { get; set; }
        [DataMember]
        public string RoleDescription { get; set; }
        [DataMember]
        public string Status { get; set; }
        [DataMember]
        public string Avatar { get; set; }
        [DataMember]
        public int Level { get; set; }

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

        public string UserStatus { get; set; }

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

            //get list of joined / pending domains ids
            var pendingRole = Role.FromIdentifier(context, RoleTep.PENDING);
            int[] domainIds = Domain.GetGrantScope(context, UserId, null, null);
            int[] pendingDomainIds = Domain.GetGrantScope(context, UserId, null, new int[] { pendingRole.Id });
            List<int> joinedDomainIds = new List<int>();
            //remove pendings from joined
            if (pendingDomainIds.Length > 0) {
                foreach (var did in domainIds) {
                    if (!pendingDomainIds.Contains(did)) joinedDomainIds.Add(did);
                }
            } else joinedDomainIds = domainIds.ToList();

            string condition = "";
            switch(UserStatus){
                case ThematicCommunity.USERSTATUS_JOINED:
                    condition = string.Format("t.kind IN ({0}) AND t.id IN ({1})", 
                                              (int)DomainKind.Public + "," + (int)DomainKind.Private + "," + (int)DomainKind.Hidden, //all kind of communities
                                              joinedDomainIds.Count == 0 ? "0" : String.Join(",", joinedDomainIds)); //where user has a role (not pending)
                    break;
                case ThematicCommunity.USERSTATUS_UNJOINED:
                    condition = string.Format("t.kind IN ({0}) AND t.id NOT IN ({1})",
                                              (int)DomainKind.Public + "," + (int)DomainKind.Private, //all kind of communities except Hidden
                                              joinedDomainIds.Count == 0 ? "0" : String.Join(",", joinedDomainIds)); //where user does not have a role
                    break;
                case ThematicCommunity.USERSTATUS_PENDING:
                    condition = string.Format("t.kind IN ({0}) AND t.id IN ({1})",
                                              (int)DomainKind.Private, //can be pending only in Private
                                              pendingDomainIds.Length == 0 ? "0" : String.Join(",", pendingDomainIds)); //where user has a pending role
                    break;
                case ThematicCommunity.USERSTATUS_ALL:
                default:
                    condition = string.Format("(t.kind IN ({0}) AND t.id IN ({1})) OR t.kind IN ({2})",
                                              (int)DomainKind.Hidden, //hidden
                                              domainIds.Length == 0 ? "0" : String.Join(",", domainIds),//where user has a role
                                              (int)DomainKind.Public + "," + (int)DomainKind.Private); //all kind of communities except Hidden
                    break;
            }

            var sortexpression = string.Format("CASE WHEN t.id IN ({0}) THEN 0 WHEN t.id IN ({1}) THEN 1 ELSE 2 END",
                    joinedDomainIds.Count == 0 ? "0" : String.Join(",", joinedDomainIds),
                    pendingDomainIds.Length == 0 ? "0" : String.Join(",", pendingDomainIds));

            this.AddSortExpression(sortexpression, SortDirection.Ascending);
            this.AddSort("Kind", SortDirection.Descending);
            this.AddSort("EnableJoinRequest", SortDirection.Descending);

            Clear();

            object[] queryParts = entityType.GetListQueryParts(context, this, UserId, null, condition);
            string sql = entityType.GetCountQuery(queryParts);
            if (context.ConsoleDebug) Console.WriteLine("SQL (COUNT): " + sql);
            // context.LogDebug(this, "ThematicCommunity - LoadRestricted - SQL(COUNT): " + sql);
            TotalResults = context.GetQueryLongIntegerValue(sql);

            sql = entityType.GetQuery(queryParts);
            if (context.ConsoleDebug) Console.WriteLine("SQL: " + sql);
            // context.LogDebug(this, "ThematicCommunity - LoadRestricted - SQL: " + sql);
            IDbConnection dbConnection = context.GetDbConnection();
            IDataReader reader = context.GetQueryResult(sql, dbConnection);
            IsLoading = true;
            var ids = new List<int>();
            while (reader.Read()) {
                if(reader.GetValue(0) != DBNull.Value)
                    ids.Add(reader.GetInt32(0));
            }
            foreach (var id in ids){
                IncludeInternal(ThematicCommunity.FromId(context, id));
            }

            // while (reader.Read()) {
            //     try{
            //         context.LogDebug(this, "test3");
            //         ThematicCommunity item = entityType.GetEntityInstance(context) as ThematicCommunity;
            //         context.LogDebug(this, "test4");
            //         item.Load(entityType, reader, AccessLevel);
            //         context.LogDebug(this, "test5");
            //         if (UserId != 0) item.UserId = UserId;
            //         context.LogDebug(this, "test5");
            //         IncludeInternal(item);
            //         context.LogDebug(this, "test7");
            //     }catch(Exception e){
            //         context.LogError(this, e.Message);
            //     }
            // }
            IsLoading = false;
            context.CloseQueryResult(reader, dbConnection);
        }
    }

    [DataContract]
    public class UserSyncRequest {
        [DataMember]
        public string token { get; set; }
        [DataMember]
        public UserSync user { get; set; }
        [DataMember]
        public string action { get; set; }
    }

    [DataContract]
    public class UserSync {
        [DataMember]
        public int id { get; set; }
        [DataMember]
        public string lastname { get; set; }
        [DataMember]
        public string firstname { get; set; }
        [DataMember]
        public string email { get; set; }
        [DataMember]
        public string company { get; set; }
        [DataMember]
        public string platform { get; set; }
    }
}
