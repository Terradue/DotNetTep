using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Web;
using Terradue.OpenSearch;
using Terradue.OpenSearch.Engine;
using Terradue.OpenSearch.Schema;
using Terradue.Portal;

/*! 
\defgroup TepData Data
@{

\ingroup Tep

This component is in charge of all the data management in the platform.

It implements the mechanism to search for Collection and Data Packages via an \ref OpenSearchable interface.

\xrefitem dep "Dependencies" "Dependencies" uses \ref Authorisation to manage the users in the groups with their roles and their access accordingly.

\xrefitem dep "Dependencies" "Dependencies" uses \ref Series to delegates the dataset series persistence and search mechanism.

User Data Packages
------------------

Each user of the platform may define a \ref DataPackage to save a set of dataset that he preselected.
The 2 following state diagram shows the lifecycle of those data packages in creation and update.

\startuml

User -> WebPortal: Select data
WebPortal -> WebServer: Stores in a temporary data package
WebServer -> Database: Save a temporary data package
User -> WebPortal: Save the data package \nwith name/identifier
WebPortal -> WebServer: Request the storage \nof the current temporary data package \nwith given name/identifier
WebServer -> Database: Save the data package \nwith associated opensearch urls
WebServer -> WebPortal: Return new data package
WebPortal -> User: Data package successfully created

footer
TEP Data Package creation state diagram
(c) Terradue Srl
endfooter

\enduml

\startuml

User -> WebPortal: Load existing data package
WebPortal -> WebServer: Stores the data package in the temporary data package
WebPortal -> User: Displays the items from the data package
User -> WebPortal: Remove items or add new ones in the temporary data package

alt user is owner of the data package
User -> WebPortal: Request the storage of the current temporary data package \nwith given name (update existing one)
WebServer -> Database: Save the data package \nwith associated opensearch urls
WebServer -> WebPortal: Return new data package
WebPortal -> User: Data package successfully updated
else user is not the owner of the data package
WebPortal -> User: Displays an error message to the user
end

footer
TEP Data Package update state diagram
(c) Terradue Srl
endfooter

\enduml

@}
*/

using Terradue.OpenSearch.Result;
using Terradue.ServiceModel.Syndication;
using System.Linq;

namespace Terradue.Tep.Controller {

    /// <summary>
    /// Data package.
    /// </summary>
    /// <description>
    /// It represents a container for datasets, owned by a user. This container manages remote resources by reference. 
    /// Therefore, it can represent static datasets list or a dynamic set via search query.
    /// A Data Package is OpenSearchable and thus can be queried via an opensearch interface.
    /// </description>
    /// \ingroup TepData
    /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation" 
    [EntityTable(null, EntityTableConfiguration.Custom, Storage = EntityTableStorage.Above)]
    public class DataPackage : RemoteResourceSet, IAtomizable {

        private static readonly log4net.ILog log = log4net.LogManager.GetLogger
            (System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private EntityType entitytype { get; set; }
        private EntityType entityType { 
            get{ 
                if(entitytype == null) entitytype = EntityType.GetEntityType(typeof(DataPackage));
                return entitytype;
            } 
        }

        /// <summary>
        /// Gets or sets the items.
        /// </summary>
        /// <value>The items.</value>
        public EntityList<RemoteResource> Items { get;	set; }

        /// <summary>
        /// Owner of the data package
        /// </summary>
        /// <value>The owner.</value>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation" 
        public UserTep Owner {
            get;
            set;
        }

        /// <summary>
        /// Collections included in the Data Package
        /// </summary>
        /// <value>The colelctions.</value>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation" 
        public List<Collection> Collections {
            get;
            set;
        }
            

        /// <summary>
        /// Gets or sets the resources.
        /// </summary>
        /// <value>The resources.</value>
        public override EntityList<RemoteResource> Resources {
            get {
                EntityList<RemoteResource> result = new EntityList<RemoteResource>(context);
                result.Template.ResourceSet = this;
                if (Items == null)
                    LoadItems();
                foreach (RemoteResource item in Items)
                    result.Include(item);
                return result;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Terradue.Contest.DataPackage"/> class.
        /// </summary>
        /// <param name="context">Context.</param>
        public DataPackage(IfyContext context) : base(context) {
        }

        /// <summary>
        /// Froms the identifier.
        /// </summary>
        /// <returns>The identifier.</returns>
        /// <param name="context">Context.</param>
        /// <param name="identifier">Identifier.</param>
        public static new DataPackage FromIdentifier(IfyContext context, string identifier) {
            DataPackage result = new DataPackage(context);
            result.Identifier = identifier;
            try {
                result.Load();
            } catch (Exception e) {
                throw e;
            }
            return result;
        }

        /// <summary>
        /// Froms the identifier.
        /// </summary>
        /// <returns>The identifier.</returns>
        /// <param name="context">Context.</param>
        /// <param name="id">Identifier.</param>
        public static new DataPackage FromId(IfyContext context, int id) {
            DataPackage result = new DataPackage(context);
            result.Id = id;
            try {
                result.Load();
            } catch (Exception e) {
                throw e;
            }
            return result;
        }

        public static DataPackage GetTemporaryForCurrentUser(IfyContext context){
            DataPackage result = new DataPackage(context);
            result.OwnerId = context.UserId;
            result.IsDefault = true;
            try {
                result.Load();
            } catch (Exception e) {
                //we create it
                result.Identifier = Guid.NewGuid().ToString();
                result.Name = "temporary workspace";
                result.CreationTime = DateTime.UtcNow;
                result.Store();
            }
            return result;
        }

        public override string AlternativeIdentifyingCondition{
            get { 
                if (OwnerId != 0 && IsDefault) return String.Format("t.id_usr={0} AND t.is_default={1}",OwnerId,IsDefault); 
                return null;
            }
        }

        /// <summary>
        /// Adds the resource item.
        /// </summary>
        /// <param name="item">Item.</param>
        public void AddResourceItem(RemoteResource item) {
            if (string.IsNullOrEmpty(item.Location)) return;
            //temporary, waiting for http://project.terradue.com/issues/13297 to be solved
            foreach (var it in Resources) {
                if (it.Location.Equals(item.Location))
                    return;
            }
            item.ResourceSet = this;
            item.Store();
            Items.Include(item);
        }

        /// <summary>
        /// Reads the information of an item from the database.
        /// </summary>
        public override void Load() {
            base.Load();
            LoadItems();
        }

        /// <summary>
        /// Loads the items.
        /// </summary>
        public void LoadItems() {
            Items = new EntityList<RemoteResource>(context);
            Items.Template.ResourceSet = this;
            Items.Load();
        }

        /// <summary>
        /// Writes the item to the database.
        /// </summary>
        public override void Store() {
            context.StartTransaction();
            bool isNew = this.Id == 0;
            try {
                if (isNew){
                    this.AccessKey = Guid.NewGuid().ToString();
                    this.CreationTime = DateTime.UtcNow;
                }
                base.Store();

                if (IsDefault)
                    this.StorePrivilegesForUsers(null, true);

                Resources.StoreExactly();
                LoadItems();
                context.Commit();
            } catch (Exception e) {
                context.Rollback();
                throw e;
            }

        }

        /// <summary>
        /// Allows the user.
        /// </summary>
        /// <param name="usrId">Usr identifier.</param>
        public void AllowUser(int usrId) {
            String sql = String.Format("INSERT IGNORE INTO resourceset_priv (id_resourceset, id_usr) VALUES ({0},{1});", this.Id, usrId);
            context.Execute(sql);
        }

        /// <summary>
        /// Removes the user.
        /// </summary>
        /// <param name="usrId">Usr identifier.</param>
        public void RemoveUser(int usrId) {
            String sql = String.Format("DELETE FROM resourceset_priv WHERE id_resourceset={0} AND id_usr={1};", this.Id, usrId);
            context.Execute(sql);

            //\todo: Problem if user need to access data package from another contest
        }

        public bool IsPublic(){
            return HasGlobalPrivilege();
        }

        public bool IsPrivate(){
            return !IsPublic() && !IsRestricted();
        }

        public bool IsRestricted(){
			string sql = String.Format("SELECT COUNT(*) FROM resourceset_priv WHERE id_resourceset={0} AND ((id_usr IS NOT NULL AND id_usr != {1}) OR id_grp IS NOT NULL);", this.Id, this.OwnerId);
            return context.GetQueryIntegerValue(sql) > 0;
        }

        public void SetOpenSearchEngine(OpenSearchEngine ose) {
            this.ose = ose;
        }

        public virtual OpenSearchUrl GetSearchBaseUrl(string mimeType) {
            return new OpenSearchUrl (string.Format("{0}/"+entityType.Keyword+"/{1}/search?key={2}", context.BaseUrl, (IsDefault ? "default" : this.Identifier), this.AccessKey));
        }

        public virtual OpenSearchUrl GetDescriptionBaseUrl() {
            return new OpenSearchUrl (string.Format("{0}/"+entityType.Keyword+"/{1}/description?key={2}", context.BaseUrl, (IsDefault ? "default" : this.Identifier), this.AccessKey));
        }

        /// <summary>
        /// Gets the local open search description.
        /// </summary>
        /// <returns>The local open search description.</returns>
        public OpenSearchDescription GetLocalOpenSearchDescription() {
            OpenSearchDescription osd = base.GetOpenSearchDescription();

            List<OpenSearchDescriptionUrl> urls = new List<OpenSearchDescriptionUrl>();
            UriBuilder urlb = new UriBuilder(GetDescriptionBaseUrl());
            OpenSearchDescriptionUrl url = new OpenSearchDescriptionUrl("application/opensearchdescription+xml", urlb.ToString(), "self");
            urls.Add(url);

            NameValueCollection query = HttpUtility.ParseQueryString(urlb.Query);

            urlb = new UriBuilder(GetSearchBaseUrl("application/atom+xml"));
            query = GetOpenSearchParameters("application/atom+xml");
            NameValueCollection nvc = HttpUtility.ParseQueryString(urlb.Query);
            foreach (var key in nvc.AllKeys) {
                query.Set(key, nvc[key]);
            }

            foreach (var osee in OpenSearchEngine.Extensions.Values) {
                query.Set("format", osee.Identifier);
                string[] queryString = Array.ConvertAll(query.AllKeys, key => string.Format("{0}={1}", key, query[key]));
                urlb.Query = string.Join("&", queryString);
                url = new OpenSearchDescriptionUrl(osee.DiscoveryContentType, urlb.ToString(), "search");
                urls.Add(url);
            }

            osd.Url = urls.ToArray();

            if (this.IsDefault) {
                osd.ShortName = "Default datapackage";
            }

            return osd;
        }

        public override IOpenSearchable[] GetOpenSearchableArray() {
            List<UrlBasedOpenSearchable> osResources = new List<UrlBasedOpenSearchable>(Resources.Count);

            foreach (RemoteResource res in Resources) {
                var entity = new UrlBasedOpenSearchable(context, new OpenSearchUrl(res.Location), ose);
                var eosd = entity.GetOpenSearchDescription();
                if (eosd.DefaultUrl != null && eosd.DefaultUrl.Type == "application/json") {
                    var atomUrl = eosd.Url.FirstOrDefault(u => u.Type == "application/atom+xml");
                    if (atomUrl != null)
                        eosd.DefaultUrl = atomUrl;
                }

                osResources.Add(entity);
            }

            return osResources.ToArray();
        }

        #region IOpenSearchable implementation

        public OpenSearchUrl GetSearchBaseUrl() {
            return new OpenSearchUrl(string.Format("{0}/data/package/{1}/search", context.BaseUrl, this.Identifier));
        }

        #endregion

        #region IAtomizable implementation

        public Terradue.OpenSearch.Result.AtomItem ToAtomItem(NameValueCollection parameters) {

            string identifier = this.Identifier;
            string name = (this.Name != null ? this.Name : this.Identifier);
            string description = null;
            string text = (this.TextContent != null ? this.TextContent : "");

            if (parameters["q"] != null) {
                string q = parameters["q"].ToLower();
                if (!(name.ToLower().Contains(q) || this.Identifier.ToLower().Contains(q) || text.ToLower().Contains(q)))
                    return null;
            }

            AtomItem atomEntry = null;
            var entityType = EntityType.GetEntityType(typeof(DataPackage));
            Uri id = new Uri(context.BaseUrl + "/" + entityType.Keyword + "/search?id=" + this.Identifier);
            try {
                atomEntry = new AtomItem(identifier, name, null, id.ToString(), DateTime.UtcNow);
            } catch (Exception e) {
                atomEntry = new AtomItem();
            }

            atomEntry.ElementExtensions.Add("identifier", "http://purl.org/dc/elements/1.1/", this.Identifier);

            atomEntry.Links.Add(new SyndicationLink(id, "self", name, "application/atom+xml", 0));

            UriBuilder search = new UriBuilder(context.BaseUrl + "/" + entityType.Keyword + "/" + (IsDefault ? "default" : identifier) + "/description");
            atomEntry.Links.Add(new SyndicationLink(search.Uri, "search", name, "application/atom+xml", 0));

            search = new UriBuilder(context.BaseUrl + "/" + entityType.Keyword + "/" + identifier + "/search");
            search.Query = "key=" + this.AccessKey;

            atomEntry.Links.Add(new SyndicationLink(search.Uri, "public", name, "application/atom+xml", 0));

            Uri share = new Uri(context.BaseUrl + "/share?url=" +id.AbsoluteUri);
            atomEntry.Links.Add(new SyndicationLink(share, "via", name, "application/atom+xml", 0));
            atomEntry.ReferenceData = this;
            atomEntry.PublishDate = this.CreationTime;
            User owner = User.FromId(context, this.OwnerId);
            var basepath = new UriBuilder(context.BaseUrl);
            basepath.Path = "user";
            string usrUri = basepath.Uri.AbsoluteUri + "/" + owner.Username ;
            string usrName = (!String.IsNullOrEmpty(owner.FirstName) && !String.IsNullOrEmpty(owner.LastName) ? owner.FirstName + " " + owner.LastName : owner.Username);
            SyndicationPerson author = new SyndicationPerson(owner.Email, usrName, usrUri);
            author.ElementExtensions.Add(new SyndicationElementExtension("identifier", "http://purl.org/dc/elements/1.1/", owner.Username));
            atomEntry.Authors.Add(author);
            atomEntry.Categories.Add(new SyndicationCategory("visibility", null, IsPublic() ? "public" : (IsRestricted() ? "restricted" : "private")));
            if (IsDefault) {
                atomEntry.Categories.Add(new SyndicationCategory("default", null, "true"));
            }

            return atomEntry;
        }


        public NameValueCollection GetOpenSearchParameters() {
            return OpenSearchFactory.GetBaseOpenSearchParameter();
        }

        #endregion
    }
}

