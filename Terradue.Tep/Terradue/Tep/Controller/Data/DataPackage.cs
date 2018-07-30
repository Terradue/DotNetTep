using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Web;
using Terradue.OpenSearch;
using Terradue.OpenSearch.Engine;
using Terradue.OpenSearch.Schema;
using Terradue.OpenSearch.Result;
using Terradue.Portal;
using Terradue.ServiceModel.Syndication;
using System.Linq;
using Terradue.Portal.OpenSearch;

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

This selection can be done by
- selecting single datasets in the basket;
- or saving a set of filters from a search.

This latter selection way is an important feature especially to define a dynamic subset of data Collection allowing data driven processing.


Persistence
-----------

When a dataset is processed with a remote processing (e.g. WPS), the results of this data may be located in a temporary storage. The user would want to keep that result
and its metadata. The \ref TepData components integrates the function to "copy" the results and its metadata to a persistent storage on one for the files and on a catalogue
index for the metadata.


Analysis and  Visualization
---------------------------

When a dataset is processed with a remote processing (e.g. WPS), the results of this data may be located in a place where there is no other function than downloading the data
directly on its local machine to visualize or analyze it.
This component enables export capability to GeoServer with support to raster and vector files. If the results include standard vector files (e.g. shapefile, geojson, csv with WKT, ...) or raster files such as geolocated images (geotiff, png with world files...),
the \refTepData components shall propose to the user to export them to geoserver that will return a new WMS layer that the web visualization widget shall display.
It also integrates functions to "manipulate" the results and its metadata with an external tools such as GIS functions.


\xrefitem int "Interfaces" "Interfaces" connects \ref GeoServerAPI to export vector or raster data.


@}

\defgroup GeoServerAPI GeoServer API
@{

 GeoServer provides a RESTful interface through which clients can retrieve information about an instance and make configuration changes. Using the REST interface’s simple HTTP calls, clients can configure GeoServer without needing to use the Web Administration Interface. 

 \xrefitem cptype_int "Interfaces" "Interfaces"

 \xrefitem api "API" "API" [GeoServer REST configuration API](http://docs.geoserver.org/stable/en/user/rest/api/)

@}

\defgroup GeoNodeAPI GeoNode API
@{

 GeoNode provides JSON API which currently support the GET method. The API are also used as main serch engine.

 \xrefitem cptype_int "Interfaces" "Interfaces"

 \xrefitem api "API" "API" [GeoNode ah-hoc API](http://docs.geonode.org/en/master/reference/api.html)

@}

*/

namespace Terradue.Tep {

    /// <summary>
    /// Data package.
    /// </summary>
    /// <description>
    /// It represents a container for datasets, owned by a user. This container manages remote datasets by reference. 
    /// It acts as a view over the \ref Collection.
    /// Therefore, it can represent static datasets list or a dynamic set via search query.
    /// A Data Package is OpenSearchable and thus can be queried via an opensearch interface.
    /// </description>
    /// \ingroup TepData
    /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation" 
    [EntityTable(null, EntityTableConfiguration.Custom, Storage = EntityTableStorage.Above, AllowsKeywordSearch = true)]
    public class DataPackage : RemoteResourceSet, IAtomizable {
        
        protected EntityType entitytype { get; set; }
        protected EntityType entityType { 
            get{
                if (entitytype == null) {
                    var type = this.GetType();
                    entitytype = EntityType.GetEntityType(type);
                }
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
        /// <value>is owned by a \ref User</value>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation" 
        private UserTep owner;
        public UserTep Owner {
            get {
                if (owner == null) {
                    if (OwnerId != 0) owner = UserTep.FromId (context, OwnerId);
                }
                return owner;
            }
        }

        /// <summary>
        /// Collections included in the Data Package
        /// </summary>
        /// <value>is a view over one or more collections</value>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation" 
        public List<Collection> Collections {
            get;
            set;
        }
            

        /// <summary>
        /// Gets or sets the resources.
        /// </summary>
        /// <value>The resources.</value>
        public override RemoteResourceEntityCollection Resources {
            get {
                RemoteResourceEntityCollection result = new RemoteResourceEntityCollection(context);
                result.Template.ResourceSet = this;
                if (Items == null)
                    LoadItems();
                foreach (RemoteResource item in Items)
                    result.Include(item);
                return result;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Terradue.Tep.DataPackage"/> class.
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

		public static DataPackage FromNameAndOwner(IfyContext context, string name, int userid) {
			DataPackage result = new DataPackage(context);
			result.Name = name;
            result.OwnerId = userid;
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
            User user = User.FromId (context, context.UserId);
            return GetTemporaryForUser(context, user);
        }

        public static DataPackage GetTemporaryForUser(IfyContext context, User user){
            DataPackage result = new DataPackage(context);
            result.OwnerId = user.Id;
            result.Kind = KINDRESOURCESETUSER;
            try {
                result.Load();
            } catch (Exception e) {
                //we create it
                result.Identifier = Guid.NewGuid().ToString();
                result.Name = "temporary workspace";
                result.CreationTime = DateTime.UtcNow;
                result.DomainId = user.Domain.Id;
                result.Kind = KINDRESOURCESETUSER;
                result.Store();
            }
            return result;
        }

        public override string GetIdentifyingConditionSql (){
            if (OwnerId != 0 && !string.IsNullOrEmpty(Name)) return String.Format("t.id_usr={0} AND t.name='{1}'", OwnerId, Name);
            if (OwnerId != 0 && Kind == KINDRESOURCESETUSER) return String.Format("t.id_usr={0} AND t.kind={1}",OwnerId,Kind);
            return null;
        }

        /// <summary>
        /// Adds the resource item.
        /// </summary>
        /// <param name="item">Item.</param>
        public void AddResourceItem(RemoteResource item) {
            item.ResourceSet = this;
            var res = Resources;
            res.AllowDuplicates = false;
            res.Include(item);
            res.StoreNew ();
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
            if (DomainId == 0) DomainId = Owner.Domain.Id;
            if (DomainId == -1) DomainId = 0;
            bool isNew = this.Id == 0;
            try {
                if (isNew){
                    this.AccessKey = Guid.NewGuid().ToString();
                    this.CreationTime = DateTime.UtcNow;
                    if(string.IsNullOrEmpty(this.Identifier)) this.Identifier = GetUniqueIdentifier(this.Name);
                }
                base.Store();

                if (Kind == KINDRESOURCESETUSER)
                    this.GrantPermissionsToUsers (new int [] { Owner.Id }, true);

                Resources.StoreExactly();
                LoadItems();
                context.Commit();
            } catch (Exception e) {
                context.Rollback();
                throw e;
            }
        }

        /// <summary>
        /// Gets the unique identifier.
        /// </summary>
        /// <returns>The unique identifier.</returns>
        /// <param name="name">Name.</param>
        public string GetUniqueIdentifier(string name){
            var identifier = string.IsNullOrEmpty(name) ? this.Identifier : TepUtility.ValidateIdentifier(name);
            try {
                DataPackage.FromIdentifier(context, identifier);
            } catch (EntityUnauthorizedException) {
                //next
            } catch (EntityNotFoundException e) {
                return identifier;
            }
            for (int i = 0; i < 1000; i++){
				var uname = string.Format("{0}{1}", identifier, i == 0 ? "" : "-" + i);
                try{
                    DataPackage.FromIdentifier(context, uname);
                } catch (EntityUnauthorizedException) {
					//next
                }catch(EntityNotFoundException e){
                    return uname;
                }
			}
			throw new Exception("Sorry, we were not able to find a valid data package name");
        }

        /// <summary>
        /// Allows the user.
        /// </summary>
        /// <param name="usrId">Usr identifier.</param>
        public void AllowUser(int usrId) {
            String sql = String.Format("INSERT IGNORE INTO resourceset_perm (id_resourceset, id_usr) VALUES ({0},{1});", this.Id, usrId);
            context.Execute(sql);
        }

        /// <summary>
        /// Removes the user.
        /// </summary>
        /// <param name="usrId">Usr identifier.</param>
        public void RemoveUser(int usrId) {
            String sql = String.Format("DELETE FROM resourceset_perm WHERE id_resourceset={0} AND id_usr={1};", this.Id, usrId);
            context.Execute(sql);
        }

        public bool IsPublic(){
            return DoesGrantGlobalPermission();
        }

        public bool IsPrivate(){
            return !IsPublic() && !IsRestricted();
        }

        /// <summary>
        /// Is the data package shared to community.
        /// </summary>
        /// <returns><c>true</c>, if shared to community, <c>false</c> otherwise.</returns>
        public bool IsSharedToCommunity() {
            return (this.Owner != null && this.DomainId != this.Owner.DomainId);
        }

        /// <summary>
        /// Is the data package shared to user.
        /// </summary>
        /// <returns><c>true</c>, if shared to community, <c>false</c> otherwise.</returns>
        public bool IsSharedToUser() {
            var sharedUsersIds = this.GetAuthorizedUserIds();
            return sharedUsersIds != null && (sharedUsersIds.Length > 1 || !sharedUsersIds.Contains(this.Id));
        }

        /// <summary>
        /// Is the data package shared to user.
        /// </summary>
        /// <returns><c>true</c>, if shared to community, <c>false</c> otherwise.</returns>
        /// <param name="id">Identifier.</param>
		/// <param name="policy">Policy of sharing (direct = permission directly given to the user, role = permission only given via role and privilege, none = one of both previous cases ).</param>
		public bool IsSharedToUser(int id, string policy = "none") {
            bool permissionOnly = false;
            bool privilegeOnly = false;
            switch (policy) {
                case "permission":
                    permissionOnly = true;
                    break;
                case "privilege":
                    privilegeOnly = true;
                    break;
                default:
                    break;
            }
            var sharedUsersIds = this.GetAuthorizedUserIds(permissionOnly, privilegeOnly);
            return sharedUsersIds != null && (sharedUsersIds.Contains(id));
        }

        public bool IsRestricted(){
			string sql = String.Format("SELECT COUNT(*) FROM resourceset_perm WHERE id_resourceset={0} AND ((id_usr IS NOT NULL AND id_usr != {1}) OR id_grp IS NOT NULL);", this.Id, this.OwnerId);
            return context.GetQueryIntegerValue(sql) > 0;
        }

        public void SetOpenSearchEngine(OpenSearchEngine ose) {
            this.ose = ose;
        }

        public override OpenSearchUrl GetSearchBaseUrl(string mimeType) {
            return new OpenSearchUrl (string.Format("{0}/"+entityType.Keyword+"/{1}/search?key={2}", context.BaseUrl, (Kind == KINDRESOURCESETUSER ? "default" : this.Identifier), this.AccessKey));
        }

        public override OpenSearchUrl GetDescriptionBaseUrl() {
            return new OpenSearchUrl (string.Format("{0}/"+entityType.Keyword+"/{1}/description?key={2}", context.BaseUrl, (Kind == KINDRESOURCESETUSER ? "default" : this.Identifier), this.AccessKey));
        }

        /// <summary>
        /// Gets the local open search description.
        /// </summary>
        /// <returns>The local open search description.</returns>
        public virtual OpenSearchDescription GetLocalOpenSearchDescription() {
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

            if (Kind == KINDRESOURCESETUSER) {
                osd.ShortName = "Default datapackage";
            }

            return osd;
        }

        public override IOpenSearchable[] GetOpenSearchableArray() {

			List<IOpenSearchable> osResources = new List<IOpenSearchable>(Resources.Count);

            var settings = MasterCatalogue.OpenSearchFactorySettings;

			string apikey = null;
            string t2userid = null;
            if (context.UserId != 0) {
                var user = UserTep.FromId(context, context.UserId);
                apikey = user.GetSessionApiKey();
                t2userid = user.TerradueCloudUsername;
            }

			foreach (RemoteResource res in Resources)
			{
                OpenSearchableFactorySettings specsettings = (OpenSearchableFactorySettings)settings.Clone();
                // For Terradue resources, use the API key
				if (res.Location.StartsWith(context.GetConfigValue("catalog-baseurl")) && !string.IsNullOrEmpty(apikey))
				{
                    specsettings.Credentials = new System.Net.NetworkCredential(t2userid, apikey);
				}
                try {
                    IOpenSearchable entity;
                    try {
                        entity = OpenSearchFactory.FindOpenSearchable(specsettings, new Uri(res.Location), "application/atom+xml");
                    }catch(Exception e){
                        entity = null;
                    }
                    if(entity == null) entity = OpenSearchFactory.FindOpenSearchable(specsettings, new Uri(res.Location));

				    osResources.Add(entity);
				}
				catch (Exception e)
				{
					context.LogError(this, e.Message);
				}
			}

			return osResources.ToArray();

        }

        #region IOpenSearchable implementation

        public OpenSearchUrl GetSearchBaseUrl() {
            return new OpenSearchUrl(string.Format("{0}/data/package/{1}/search", context.BaseUrl, this.Identifier));
        }

        #endregion

        #region IAtomizable implementation

        public override AtomItem ToAtomItem(NameValueCollection parameters) {

            string identifier = this.Identifier;
            string name = (this.Name != null ? this.Name : this.Identifier);
            string text = (this.TextContent != null ? this.TextContent : "");

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

            UriBuilder search = new UriBuilder(context.BaseUrl + "/" + entityType.Keyword + "/" + (Kind == KINDRESOURCESETUSER ? "default" : identifier) + "/description");
            atomEntry.Links.Add(new SyndicationLink(search.Uri, "search", name, "application/atom+xml", 0));

            search = new UriBuilder(context.BaseUrl + "/" + entityType.Keyword + "/" + identifier + "/search");
            search.Query = "key=" + this.AccessKey;

            atomEntry.Links.Add(new SyndicationLink(search.Uri, "public", name, "application/atom+xml", 0));
            atomEntry.Links.Add(new SyndicationLink(search.Uri, "alternate", name, "application/atom+xml", 0));

            Uri share = new Uri(context.BaseUrl + "/share?url=" +id.AbsoluteUri);
            atomEntry.Links.Add(new SyndicationLink(share, "via", name, "application/atom+xml", 0));
            atomEntry.ReferenceData = this;

            atomEntry.PublishDate = new DateTimeOffset(this.CreationTime);

            if (Owner != null) {
                var basepath = new UriBuilder(context.BaseUrl);
                basepath.Path = "user";
                string usrUri = basepath.Uri.AbsoluteUri + "/" + Owner.Username;
                string usrName = (!String.IsNullOrEmpty(owner.FirstName) && !String.IsNullOrEmpty(Owner.LastName) ? Owner.FirstName + " " + Owner.LastName : Owner.Username);
                SyndicationPerson author = new SyndicationPerson(null, usrName, usrUri);
                author.ElementExtensions.Add(new SyndicationElementExtension("identifier", "http://purl.org/dc/elements/1.1/", Owner.Username));
                atomEntry.Authors.Add(author);
                atomEntry.Categories.Add(new SyndicationCategory("visibility", null, IsPublic() ? "public" : (IsRestricted() ? "restricted" : "private")));
                if (Kind == KINDRESOURCESETUSER) {
                    atomEntry.Categories.Add(new SyndicationCategory("default", null, "true"));
                }

                if (Owner.Id == context.UserId) {
                    //for owner only, we give the link to know with who the data package is shared
                    Uri sharedUrl = null;
                    //if shared with users
                    if (IsSharedToUser()) {
                        sharedUrl = new Uri(string.Format("{0}/user/search?correlatedTo={1}", context.BaseUrl, HttpUtility.UrlEncode(id.AbsoluteUri)));
                    } else if (IsSharedToCommunity()) {
                        sharedUrl = new Uri(string.Format("{0}/community/search?correlatedTo={1}", context.BaseUrl, HttpUtility.UrlEncode(id.AbsoluteUri)));
                    }
                    if (sharedUrl != null) atomEntry.Links.Add(new SyndicationLink(sharedUrl, "results", name, "application/atom+xml", 0));
                }
            }

            return atomEntry;
        }

        #endregion

        #region IEntitySearchable implementation
        public override KeyValuePair<string, string> GetFilterForParameter(string parameter, string value) {
            switch (parameter) {
                case "correlatedTo":
                    var settings = MasterCatalogue.OpenSearchFactorySettings;
                    var entity = new UrlBasedOpenSearchable(context, new OpenSearchUrl(value), settings).Entity;
                    if (entity is EntityList<ThematicCommunity>) {
                        var entitylist = entity as EntityList<ThematicCommunity>;
                        var items = entitylist.GetItemsAsList();
                        if (items.Count > 0) {
                            return new KeyValuePair<string, string>("DomainId", items[0].Id.ToString());
                        }
                    }
                    return new KeyValuePair<string, string>();
                default:
                    return base.GetFilterForParameter(parameter, value);
            }
        }

        #endregion
    }
}

