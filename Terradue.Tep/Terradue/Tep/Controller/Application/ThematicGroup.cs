using System;
using System.Collections.Specialized;
using Terradue.OpenSearch;
using Terradue.OpenSearch.Result;
using Terradue.Portal;
using Terradue.ServiceModel.Syndication;

namespace Terradue.Tep
{

    public class ThematicGroup : Domain, IAtomizable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="T:Terradue.Tep.ThematicGroup"/> class.
        /// </summary>
        /// <param name="context">Context.</param>
        public ThematicGroup (IfyContext context) : base (context) { }

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
                if (!(Name.ToLower ().Contains (q) || this.Identifier.ToLower ().Contains (q)))
                    return null;
            }

            AtomItem result = new AtomItem ();

            result.Id = id.ToString ();
            result.Title = new TextSyndicationContent (Identifier);
            result.Content = new TextSyndicationContent (Name);

            result.ElementExtensions.Add ("identifier", "http://purl.org/dc/elements/1.1/", this.Identifier);
            result.Summary = new TextSyndicationContent (Description);
            result.ReferenceData = this;

            result.PublishDate = new DateTimeOffset (DateTime.UtcNow);

            //authors
            var roles = new EntityList<Role> (context);
            roles.Load ();
            var basepath = new UriBuilder (context.BaseUrl);
            basepath.Path = "user";
            foreach (var role in roles) {
                var usrs = role.GetUsers (this.Id);
                foreach (var usrId in usrs) {
                    User usr = User.FromId (context, usrId);
                    string usrUri = basepath.Uri.AbsoluteUri + "/" + usr.Username;
                    SyndicationPerson author = new SyndicationPerson (usr.Email, usr.Name, usrUri);
                    author.ElementExtensions.Add (new SyndicationElementExtension ("identifier", "http://purl.org/dc/elements/1.1/", usr.Username));
                    author.ElementExtensions.Add (new SyndicationElementExtension ("role", "http://purl.org/dc/elements/1.1/", role.Name));
                    result.Authors.Add (author);
                }
            }

            result.Links.Add (new SyndicationLink (id, "self", Name, "application/atom+xml", 0));
            if (!string.IsNullOrEmpty (IconUrl)) {
                result.Links.Add (new SyndicationLink (new Uri (context.BaseUrl + "/files/" + IconUrl), "icon", "", GetImageMimeType(IconUrl), 0));
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
    }

    public class ThematicGroupFactory {

        private IfyContext Context;

        public ThematicGroupFactory (IfyContext context) 
        {
            Context = context;
        }

        public void CreateThematicGroup (string identifier, string name) {
            ThematicGroup tg = new ThematicGroup (Context);
            tg.Identifier = identifier;
            tg.Name = name;
            tg.Store ();
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
