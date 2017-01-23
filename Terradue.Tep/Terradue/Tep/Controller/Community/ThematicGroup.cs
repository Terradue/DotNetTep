using System;
using System.Collections.Specialized;
using Terradue.OpenSearch;
using Terradue.OpenSearch.Result;
using Terradue.Portal;
using Terradue.ServiceModel.Syndication;

namespace Terradue.Tep
{

    public class ThematicGroup : Domain, IAtomizable{

        public const string MANAGER = "manager";

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Terradue.Tep.ThematicGroup"/> class.
        /// </summary>
        /// <param name="context">Context.</param>
        public ThematicGroup (IfyContext context) : base (context) { }

        /// <summary>
        /// Froms the identifier.
        /// </summary>
        /// <returns>The identifier.</returns>
        /// <param name="context">Context.</param>
        /// <param name="id">Identifier.</param>
        public static new ThematicGroup FromId (IfyContext context, int id)
        {
            ThematicGroup result = new ThematicGroup (context);
            result.Id = id;
            try {
                result.Load ();
            } catch (Exception e) {
                throw e;
            }
            return result;
        }

        private UserTep owner;
        public UserTep Owner {
            get {
                if (owner == null) {
                    owner = GetOwner ();
                }
                return owner;
            }
        }

        /// <summary>
        /// Gets the owner.
        /// </summary>
        /// <returns>The owner.</returns>
        private UserTep GetOwner () {
            var role = Role.FromIdentifier (context, RoleTep.OWNER);
            var usrs = role.GetUsers (this.Id);
            if (usrs != null && usrs.Length > 0)
                return UserTep.FromId (context, usrs [0]);
            else return null;
        }

        /// <summary>
        /// Sets the owner.
        /// </summary>
        /// <param name="user">User.</param>
        public void SetOwner (UserTep user) { 
            var role = Role.FromIdentifier (context, RoleTep.OWNER);
            role.GrantToUser (user, this);
        }

        /// <summary>
        /// Sets the user as temporary member.
        /// </summary>
        /// <param name="userId">User identifier.</param>
        /// <param name="roleId">Role identifier.</param>
        public void SetUserAsTemporaryMember (int userId, int roleId) {
            SetAsTemporaryMember (userId, roleId, false);
        }

        /// <summary>
        /// Sets the group as temporary member.
        /// </summary>
        /// <param name="grpId">Group identifier.</param>
        /// <param name="roleId">Role identifier.</param>
        public void SetGroupAsTemporaryMember (int grpId, int roleId) {
            SetAsTemporaryMember (grpId, roleId, true);
        }

        /// <summary>
        /// Sets user or group as temporary member.
        /// </summary>
        /// <param name="id">User or Group Identifier.</param>
        /// <param name="roleId">Role identifier.</param>
        /// <param name="forGroup">If set to <c>true</c> for group.</param>
        private void SetAsTemporaryMember (int id, int roleId, bool forGroup) {
            context.Execute (string.Format ("DELETE FROM rolegrant_pending WHERE {2}={0} AND id_domain={1};", id, this.Id, forGroup ? "id_grp" : "id_usr")); // avoid duplicates
            context.Execute (string.Format ("INSERT INTO rolegrant_pending ({0}, id_role, id_domain) VALUES ({1},{2},{3});", forGroup ? "id_grp" : "id_usr", id, roleId, this.Id));
        }

        /// <summary>
        /// Is the key valid.
        /// </summary>
        /// <returns><c>true</c>, if key is valid, <c>false</c> otherwise.</returns>
        /// <param name="key">Key.</param>
        public bool IsKeyValid (string key) {
            var id = context.GetQueryIntegerValue (string.Format ("SELECT id_role FROM rolegrant_pending WHERE access_key='{0}' AND id_domain={1};", key, this.Id));
            return id != 0;
        }

        /// <summary>
        /// Sets the user as definitive member.
        /// </summary>
        /// <param name="userId">User identifier.</param>
        public void SetUserAsDefinitiveMember (int userId) { 
            var roleId = context.GetQueryIntegerValue (string.Format ("SELECT id_role FROM rolegrant_pending WHERE id_usr={0} AND id_domain={1};", userId, this.Id));
            context.Execute (string.Format ("DELETE FROM rolegrant_pending WHERE id_usr={0} AND id_domain={1};", userId, this.Id)); // remove from pending table

            Role role = Role.FromId (context, roleId);
            User user = User.FromId (context, userId);
            role.GrantToUser (user, this);
        }

        /// <summary>
        /// Sets the group as definitive member.
        /// </summary>
        /// <param name="groupId">Group identifier.</param>
        public void SetGroupAsDefinitiveMember (int groupId) {
            var roleId = context.GetQueryIntegerValue (string.Format ("SELECT id_role FROM rolegrant_pending WHERE id_grp={0} AND id_domain={1};", groupId, this.Id));
            context.Execute (string.Format ("DELETE FROM rolegrant_pending WHERE id_grp={0} AND id_domain={1};", groupId, this.Id)); // remove from pending table

            Role role = Role.FromId (context, roleId);
            Group grp = Group.FromId (context, groupId);
            role.GrantToGroup (grp, this);
        }

        #region IAtomizable

        public NameValueCollection GetOpenSearchParameters ()
        {
            return OpenSearchFactory.GetBaseOpenSearchParameter ();
        }

        public AtomItem ToAtomItem (NameValueCollection parameters)
        {
            bool ispublic = this.Kind == DomainKind.Public;
            bool isprivate = this.Kind == DomainKind.Private;

            //we only want thematic groups domains (public or private)
            if (!ispublic && !isprivate) return null;

            //if private, lets check the current user can access it (have a role in the domain)
            if (isprivate) {
                var proles = Role.GetUserRolesForDomain (context, context.UserId, this.Id);
                if (proles == null || proles.Length == 0) return null;
            }

            var entityType = EntityType.GetEntityType (typeof (Domain));
            Uri id = new Uri (context.BaseUrl + "/" + entityType.Keyword + "/search?id=" + this.Identifier);

            if (!string.IsNullOrEmpty (parameters ["q"])) {
                string q = parameters ["q"].ToLower ();
                if (!this.Identifier.ToLower ().Contains (q) && !(Description != null && Description.ToLower ().Contains (q)))
                    return null;
            }

            AtomItem result = new AtomItem ();

            result.Id = id.ToString ();
            result.Title = new TextSyndicationContent (Identifier);
            result.Content = new TextSyndicationContent (Identifier);

            result.ElementExtensions.Add ("identifier", "http://purl.org/dc/elements/1.1/", this.Identifier);
            result.Summary = new TextSyndicationContent (Description);
            result.ReferenceData = this;

            result.PublishDate = new DateTimeOffset (DateTime.UtcNow);

            //owner
            if (Owner != null) {
                var ownerUri = Owner.GetUserPageLink ();
                SyndicationPerson ownerPerson = new SyndicationPerson(Owner.Email, Owner.FirstName + " " + Owner.LastName, ownerUri);
                ownerPerson.ElementExtensions.Add (new SyndicationElementExtension ("identifier", "http://purl.org/dc/elements/1.1/", Owner.Username));
                ownerPerson.ElementExtensions.Add (new SyndicationElementExtension ("role", "http://purl.org/dc/elements/1.1/", RoleTep.OWNER));
                result.Authors.Add (ownerPerson);

            }
            //members
            var roles = new EntityList<Role> (context);
            roles.Load ();
            foreach (var role in roles) {
                var usrs = role.GetUsers (this.Id);
                foreach (var usrId in usrs) {
                    UserTep usr = UserTep.FromId (context, usrId);
                    var usrUri = usr.GetUserPageLink ();
                    SyndicationPerson author = new SyndicationPerson (usr.Email, usr.FirstName + " " + usr.LastName, usrUri);
                    author.ElementExtensions.Add (new SyndicationElementExtension ("identifier", "http://purl.org/dc/elements/1.1/", usr.Username));
                    author.ElementExtensions.Add (new SyndicationElementExtension ("role", "http://purl.org/dc/elements/1.1/", role.Name));
                    result.Authors.Add (author);
                }
            }

            result.Links.Add (new SyndicationLink (id, "self", Identifier, "application/atom+xml", 0));
            if (!string.IsNullOrEmpty (IconUrl)) {

                Uri uri;
                if (IconUrl.StartsWith ("http")) {
                    uri = new Uri (IconUrl);
                } else {
                    var urib = new UriBuilder (System.Web.HttpContext.Current.Request.Url);
                    urib.Path = IconUrl;
                    uri = urib.Uri;
                }

                result.Links.Add (new SyndicationLink (uri, "icon", "", GetImageMimeType(IconUrl), 0));
            }

            result.Categories.Add (new SyndicationCategory ("visibility", null, ispublic ? "public" : "private"));

            return result;
        }

        private string GetImageMimeType (string filename) { 
            string extension = filename.Substring (filename.LastIndexOf (".") + 1);
            string result;

            switch (extension.ToLower ()) {
            case "gif":
                result = "image/gif";
                break;
            case "gtiff":
                result = "image/tiff";
                break;
            case "jpeg":
                result = "image/jpg";
                break;
            case "png":
                result = "image/png";
                break;
            default:
                result = "application/octet-stream";
                break;
            }
            return result;
        }
        #endregion
    }

    public class ThematicGroupFactory {

        private IfyContext Context;

        public ThematicGroupFactory (IfyContext context) 
        {
            Context = context;
        }

        public void CreateVisitorRole () {
            //Create Role
            var starterRole = new Role (Context);
            starterRole.Identifier = "visitor";
            starterRole.Name = "visitor";
            starterRole.Store ();

            //Add Privileges
            //Data Package -- All
            starterRole.IncludePrivileges (Privilege.Get (EntityType.GetEntityType (typeof (DataPackage))));


        }

        public void CreateStarterRole () {

            //Create Role
            var starterRole = new Role (Context);
            starterRole.Identifier = "starter";
            starterRole.Name = "starter";
            starterRole.Store ();

            //Add Privileges
            //Data Package -- All
            starterRole.IncludePrivileges(Privilege.Get(EntityType.GetEntityType (typeof (DataPackage))));

        }

        public void CreateExplorerRole () { }

    }
}
