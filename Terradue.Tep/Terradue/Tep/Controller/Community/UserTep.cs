using System;
using Terradue.Portal;
using Terradue.OpenNebula;
using Terradue.Github;
using Terradue.Util;
using Terradue.Cloud;


using System.Collections.Generic;
using System.Web;
using System.Net;
using System.IO;
using Terradue.OpenSearch.Result;
using Terradue.ServiceModel.Syndication;
using Terradue.OpenSearch;
using System.Collections.Specialized;
using Terradue.Portal.OpenSearch;
using System.Linq;
using ServiceStack.Text;
using System.Security.Cryptography;
using System.Runtime.Serialization;

namespace Terradue.Tep {


    /// <summary>
    /// TEP User
    /// </summary>
    /// <description>
    /// A user in the TEP platform has a basic profile (defined in the platform) 
    /// It can also be integrated with third party profiles (Github, Terradue Cloud Platform).
    /// </description>
    /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation" 
    /// \ingroup TepCommunity
    [EntityTable(null, EntityTableConfiguration.Custom, Storage = EntityTableStorage.Above, AllowsKeywordSearch = true)]
    public class UserTep : User {

        /// <summary>
        /// Gets or sets the private domain of the user.
        /// </summary>
        /// <value>The domain.</value>
        public override Domain Domain {
            get {
                if (base.Domain == null) {
                    try {
                        base.Domain = GetPrivateDomain();
                    } catch (Exception e) {
                        //bla
                    }
                }
                return base.Domain;
            }
            set {
                base.Domain = value;
            }
        }

        public override int DomainId {
            get {
                return Domain != null ? Domain.Id : 0;
            }
            set {
                base.DomainId = value;
            }
        }

        /// <summary>
        /// Thematic groups the user belongs to.
        /// </summary>
        /// <value>belongs to a Group</value>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation" 
        public List<GroupTep> Groups {
            get;
            set;
        }

        /// <summary>
        /// Github profile of the user
        /// </summary>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation" 
        public GithubProfile GithubProfile {
            get;
            set;
        }

        /// <summary>
        /// Terradue Cloud Platform identifier
        /// </summary>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation" 
        public string TerradueCloudPlatformId {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the terradue cloud username.
        /// </summary>
        /// <value>The terradue cloud username.</value>
        public string TerradueCloudUsername {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the ssh pub key.
        /// </summary>
        /// <value>The ssh pub key.</value>
        public string SshPubKey {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the API key.
        /// </summary>
        /// <value>The API key.</value>
        [EntityDataField("apikey")]
        public string ApiKey {
            get;
            set;
        }

        private TransactionFactory TransactionFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="Terradue.Tep.Controller.UserTep"/> class.
        /// </summary>
        /// <param name="context">Context.</param>
        public UserTep(IfyContext context) : base(context) {
            TransactionFactory = new TransactionFactory(context);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Terradue.Tep.Controller.UserTep"/> class.
        /// </summary>
        /// <param name="user">User.</param>
        public UserTep(IfyContext context, User user) : this(context) {
            this.Id = user.Id;
            this.Load();

            this.Username = user.Username;
            this.FirstName = user.FirstName;
            this.LastName = user.LastName;
            this.Email = user.Email;
            this.Affiliation = user.Affiliation;
            this.Country = user.Country;
            this.Level = user.Level;
        }

        public override string GetIdentifyingConditionSql() {
            if (!string.IsNullOrEmpty(ApiKey))
                return String.Format("t.apikey='{0}'", ApiKey);
            else return null;
        }

        public static UserTep GetPublicUser(IfyContext context, string identifier) {
            context.AccessLevel = EntityAccessLevel.Administrator;
            UserTep usr = UserTep.FromIdentifier(context, identifier);
            return usr;
        }

        public override void Load() {
            base.Load();
            this.LoadCloudUsername();
            if (string.IsNullOrEmpty(this.ApiKey)) GenerateApiKey();
            if (IsNeededTerradueUserInfo()) LoadTerradueUserInfo();
            if (Domain == null) CreatePrivateDomain();
        }

        public override void Store() {
            bool isnew = (this.Id == 0);
            base.Store();
            if (isnew) {
                //create github profile
                GithubProfile github = new GithubProfile(context, this.Id);
                github.Store();

                CreatePrivateDomain();
                CreatePrivateThematicApp();
                CreatePrivateDataPackageCatalogueIndex();
                CreatePrivateDataPackageCatalogueProducts();
            }
        }

        /// <summary>
        /// Creates a new User instance representing the user with the specified ID.
        /// </summary>
        /// <returns>The identifier.</returns>
        /// <param name="context">Context.</param>
        /// <param name="id">Identifier.</param>
        public new static UserTep FromId(IfyContext context, int id) {
            UserTep user = new UserTep(context);
            user.Id = id;
            user.Load();
            return user;
        }

        public new static UserTep FromIdentifier(IfyContext context, string identifier) {
            UserTep user = new UserTep(context);
            user.Identifier = identifier;
            user.Load();
            return user;
        }

        public static UserTep FromApiKey(IfyContext context, string key) {
            UserTep user = new UserTep(context);
            user.ApiKey = key;
            user.Load();
            return user;
        }

        /// <summary>
        /// Gets the user page link.
        /// </summary>
        /// <returns>The user page link.</returns>
        public string GetUserPageLink() {
            var basepath = new UriBuilder(context.BaseUrl);
            basepath.Path = "user";
            return basepath.Uri.AbsoluteUri + "/" + Username;
        }

        /// <summary>
        /// Loads the cloud username.
        /// </summary>
        public void LoadCloudUsername() {
            LoadCloudUsername(context.GetConfigIntegerValue("One-default-provider"));
        }

        /// <summary>
        /// Gets the cloud username.
        /// </summary>
        /// <returns>The cloud username.</returns>
        /// <param name="providerId">Provider identifier.</param>
        public void LoadCloudUsername(int providerId) {
            if (providerId == 0) return;
            try {
                Terradue.Cloud.CloudUser cusr = Terradue.Cloud.CloudUser.FromIdAndProvider(context, this.Id, providerId);
                this.TerradueCloudUsername = cusr.CloudUsername;
            } catch (Exception) {
                this.TerradueCloudUsername = null;
            }
        }

        /// <summary>
        /// Stores the cloud username.
        /// </summary>
        public void StoreCloudUsername() {
            StoreCloudUsername(context.GetConfigIntegerValue("One-default-provider"));
        }

        /// <summary>
        /// Stores the cloud username.
        /// </summary>
        /// <param name="providerId">Provider identifier.</param>
        /// <param name="cloudusername">Cloudusername.</param>
        public void StoreCloudUsername(int providerId) {
            //In case user has no record in db for the cloud provider
            context.Execute(String.Format("INSERT IGNORE INTO usr_cloud (id, id_provider, username) VALUES ({0},{1},NULL);", this.Id, providerId));

            Terradue.Cloud.CloudUser cusr;
            try {
                cusr = Terradue.Cloud.CloudUser.FromIdAndProvider(context, this.Id, providerId);
            } catch (Exception e) {
                //record does not exist in db
                cusr = new Terradue.Cloud.CloudUser(context);
                cusr.ProviderId = providerId;
                cusr.UserId = this.Id;
            }
            cusr.CloudUsername = this.TerradueCloudUsername;
            cusr.Store();
        }

        /// <summary>
        /// Is the terradue user info needed.
        /// </summary>
        /// <returns><c>true</c>, if needed terradue user info was ised, <c>false</c> otherwise.</returns>
        public bool IsNeededTerradueUserInfo(){
            var isloading = HttpContext.Current.Session["t2loading"] != null && HttpContext.Current.Session["t2loading"] as string == "true";
            if (isloading) return false;
            if (context.UserId == 0) return false;
            if (AccountStatus != AccountStatusType.Enabled) return false;
            if (Level < 2) return false; //User must be at least starter
            var apikey = GetSessionApiKey();
            if (TerradueCloudUsername == null || apikey == null) return true;
            else return false;
		}

        /// <summary>
        /// Loads the terradue user info.
        /// </summary>
        public void LoadTerradueUserInfo(){
            context.LogDebug(this, "Loading Terradue info - " + this.Username);
            HttpContext.Current.Session["t2loading"] = "true";
            try {
                var payload = string.Format("username={0}&email={1}&originator={2}{3}",
                    this.Username,
                    this.Email,
                    context.GetConfigValue("SiteNameShort"),
                    this.Level == 2 ? "&plan=" + context.GetConfigValue("t2portal-usr-starterPlan") : this.Level == 3 ? "&plan=" + context.GetConfigValue("t2portal-usr-explorerPlan") : "");
                System.Text.ASCIIEncoding encoding = new System.Text.ASCIIEncoding();
                byte[] payloadBytes = encoding.GetBytes(payload);
                var sso = System.Convert.ToBase64String(payloadBytes);
                var sig = HashHMAC(context.GetConfigValue("sso-eosso-secret"), sso);

                var url = string.Format("{0}?payload={1}&sig={2}", context.GetConfigValue("t2portal-usr-endpoint"), sso, sig);
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Proxy = null;
                request.Method = "GET";
                request.ContentType = "application/json";
                request.Accept = "application/json";
                using (var httpResponse = (HttpWebResponse)request.GetResponse()) {
                    using (var streamReader = new StreamReader(httpResponse.GetResponseStream())) {
                        var result = streamReader.ReadToEnd();
                        var info = JsonSerializer.DeserializeFromString<WebT2ssoUserInfo>(result);
                        this.TerradueCloudUsername = info.Username;
                        this.StoreCloudUsername(context.GetConfigIntegerValue("One-default-provider"));
                        context.LogDebug(this, "Found Terradue Cloud Username : " + this.TerradueCloudUsername);
                        SetSessionApikey(info.ApiKey);
                        this.Store();
                    }
                }
            }catch(Exception e){
				HttpContext.Current.Session["t2profileError"] = e.Message;
            }
            HttpContext.Current.Session["t2loading"] = null;
        }

        /// <summary>
        /// Sets the session apikey.
        /// </summary>
        /// <param name="value">Value.</param>
        private void SetSessionApikey(string value){
            context.LogDebug(this, "SESSION - SET t2apikey="+value);
            HttpContext.Current.Session["t2apikey"] = value;
        }

        /// <summary>
        /// Gets the session API key.
        /// </summary>
        /// <returns>The session API key.</returns>
        public string GetSessionApiKey(){
            var apikey = HttpContext.Current.Session["t2apikey"] as string;
            context.LogDebug(this, "SESSION - GET t2apikey=" + apikey);
            return apikey;
        }

		private static string HashHMAC(string key, string msg) {
			var encoding = new System.Text.ASCIIEncoding();
			var bkey = encoding.GetBytes(key);
			var bmsg = encoding.GetBytes(msg);
			var hash = new HMACSHA256(bkey);
			var hashmac = hash.ComputeHash(bmsg);
			return BitConverter.ToString(hashmac).Replace("-", "").ToLower();
		}

        /// <summary>
        /// Finds the terradue cloud username.
        /// </summary>
        public void FindTerradueCloudUsername() {
            var url = string.Format("{0}?token={1}&eosso={2}&email={3}",
                                    context.GetConfigValue("t2portal-usr-endpoint"),
                                    context.GetConfigValue("t2portal-sso-token"),
                                    this.Username,
                                    this.Email);
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Proxy = null;
            request.Method = "GET";
            request.ContentType = "application/json";
            request.Accept = "application/json";
            using (var httpResponse = (HttpWebResponse)request.GetResponse()) {
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream())) {
                    this.TerradueCloudUsername = streamReader.ReadToEnd();
                    this.StoreCloudUsername(context.GetConfigIntegerValue("One-default-provider"));
                    context.LogDebug(this, "Found Terradue Cloud Username : " + this.TerradueCloudUsername);
                }
            }
        }

        /// <summary>
        /// Creates the private domain.
        /// </summary>
        public void CreatePrivateDomain() {
            //create new domain with Identifier = Username
            var privatedomain = new Domain(context);
            privatedomain.Identifier = TepUtility.ValidateIdentifier(Username);
            privatedomain.Name = Username;
            privatedomain.Description = "Domain of user " + Username;
            privatedomain.Kind = DomainKind.User;
            privatedomain.Store();

            //set the userdomain
            Domain = privatedomain;

            //Get role owner
            var userRole = Role.FromIdentifier(context, RoleTep.OWNER);

            //Grant role for user
            userRole.GrantToUser(this, Domain);

        }

        /// <summary>
        /// Gets the private domain.
        /// </summary>
        /// <returns>The private domain.</returns>
        public Domain GetPrivateDomain() {
            return Domain.FromIdentifier(context, TepUtility.ValidateIdentifier(Username));
        }

        /// <summary>
        /// Generates the API key.
        /// </summary>
        public void GenerateApiKey() {
            this.ApiKey = Guid.NewGuid().ToString();
        }

        /// <summary>
        /// Creates the SSO account.
        /// </summary>
        /// <param name="password">Password.</param>
        public void CreateSSOAccount(string password) {
            var url = context.GetConfigValue("t2portal-usr-endpoint");
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "POST";
            request.ContentType = "application/json";
            request.Accept = "application/json";
            request.Proxy = null;

            var validatedUserName = this.Username.Replace(" ", "");

            context.LogDebug(this, "Creating Terradue Cloud Account : " + validatedUserName);

            string json = "{" +
                "\"token\":\"" + context.GetConfigValue("t2portal-sso-token") + "\"," +
                "\"username\":\"" + validatedUserName + "\"," +
                "\"eosso\":\"" + this.Username + "\"," +
                "\"email\":\"" + this.Email + "\"," +
                "\"password\":\"" + password + "\"," +
                "\"plan\":\"" + context.GetConfigValue("t2portal-usr-defaultPlan") + "\"" +
                "}";

            using (var streamWriter = new StreamWriter(request.GetRequestStream())) {
                streamWriter.Write(json);
                streamWriter.Flush();
                streamWriter.Close();

                using (var httpResponse = (HttpWebResponse)request.GetResponse()) {
                    using (var streamReader = new StreamReader(httpResponse.GetResponseStream())) {//TODO in case of error
                        var result = streamReader.ReadToEnd();
                        User resUser = ServiceStack.Text.JsonSerializer.DeserializeFromString<User>(result);
                        this.TerradueCloudUsername = resUser.Username;
                        context.LogDebug(this, "Terradue Cloud Account created : " + this.TerradueCloudUsername);
                        this.StoreCloudUsername();
                    }
                }
            }
        }

        /// <summary>
        /// Creates the private thematic app.
        /// </summary>
        private ThematicApplication CreatePrivateThematicApp() {
            context.LogDebug(this, "Create private Thematic app for user " + this.Username);
            var app = new ThematicApplication(context);
            app.Identifier = Guid.NewGuid().ToString();
            app.AccessKey = Guid.NewGuid().ToString();
            app.Name = "private thematic app";
            app.Domain = this.Domain;
            app.Store();
			var baseurl = context.GetConfigValue("catalog-baseurl");
            var url = baseurl + "/" + this.TerradueCloudUsername + "/series/_apps/search";
            var res = new RemoteResource(context);
            res.Location = url;
            app.AddResourceItem(res);
            return app;
        }

        public void PrivateSanityCheck(){
            GetPrivateThematicApp();
            GetPrivateDataPackageCatalogueIndex();
            GetPrivateDataPackageCatalogueProducts();
            GetPrivateDataPackageCatalogueSeries();
        }

        /// <summary>
        /// Gets the private thematic app.
        /// </summary>
        /// <returns>The private thematic app.</returns>
		public ThematicApplication GetPrivateThematicApp() {
            var items = GetPrivateAppList();

            ThematicApplication app = null;
            if (items.Count == 0) {
                app = CreatePrivateThematicApp();
            } else app = items[0];

            app.LoadItems();
            //add apikey to location
            foreach (var item in app.Items) item.Location += "?apikey=" + GetSessionApiKey();
            return app;
		}

        private List<ThematicApplication> GetPrivateAppList(){
			var apps = new EntityList<ThematicApplication>(context);
			apps.SetFilter("Kind", ThematicApplication.KINDRESOURCESETAPPS + "");
			apps.SetFilter("DomainId", this.DomainId + "");
			apps.Load();
			var items = apps.GetItemsAsList();
            context.LogDebug(this, "GetPrivateAppList : found " + items.Count + " items (domain = " + this.DomainId + ")");
            return items;
        }

        /// <summary>
        /// Gets the private catalogue index URL.
        /// </summary>
        /// <returns>The private catalogue index URL.</returns>
        /// <param name="withapikey">If set to <c>true</c> withapikey.</param>
        public string GetPrivateCatalogueIndexUrl(bool withapikey = true){
			var url = context.GetConfigValue("catalog-baseurl") + "/" + this.TerradueCloudUsername + "/description";
            if(withapikey) url += "?apikey=" + GetSessionApiKey();
            return url;
        }

        /// <summary>
        /// Gets the private catalogue results URL.
        /// </summary>
        /// <returns>The private catalogue results URL.</returns>
        /// <param name="withapikey">If set to <c>true</c> withapikey.</param>
		public string GetPrivateCatalogueProductsUrl(bool withapikey = true) {
			var url = context.GetConfigValue("catalog-baseurl") + "/" + this.TerradueCloudUsername + "/series/_products/description";
			if (withapikey) url += "?apikey=" + GetSessionApiKey();
			return url;
		}

		/// <summary>
		/// Gets the private catalogue series URL.
		/// </summary>
		/// <returns>The private catalogue series URL.</returns>
		/// <param name="withapikey">If set to <c>true</c> withapikey.</param>
		public string GetPrivateCatalogueSeriesUrl(bool withapikey = true) {
			var url = context.GetConfigValue("catalog-baseurl") + "/" + this.TerradueCloudUsername + "/series/description";
			if (withapikey) url += "?apikey=" + GetSessionApiKey();
			return url;
		}

        /// <summary>
        /// Creates the private data package catalogue index.
        /// </summary>
        /// <returns>The private data package catalogue index.</returns>
        public DataPackage CreatePrivateDataPackageCatalogueIndex(){
            var dp = new DataPackage(context);
            dp.Identifier = "_index_" + this.Username;
            dp.Name = "My Index";
            dp.Domain = this.Domain;
            dp.Store();
            var item = new RemoteResource(context);
            item.Location = GetPrivateCatalogueIndexUrl(false);
            dp.AddResourceItem(item);
            return dp;
        }

        /// <summary>
        /// Creates the private data package catalogue results.
        /// </summary>
        /// <returns>The private data package catalogue results.</returns>
        public DataPackage CreatePrivateDataPackageCatalogueProducts() {
			var dp = new DataPackage(context);
			dp.Identifier = "_products_" + this.Username;
			dp.Name = "My Products";
			dp.Domain = this.Domain;
			dp.Store();
			var item = new RemoteResource(context);
            item.Location = GetPrivateCatalogueProductsUrl(false);
			dp.AddResourceItem(item);
			return dp;
		}

		/// <summary>
		/// Creates the private data package catalogue series.
		/// </summary>
		/// <returns>The private data package catalogue series.</returns>
		public DataPackage CreatePrivateDataPackageCatalogueSeries() {
			var dp = new DataPackage(context);
			dp.Identifier = "_series_" + this.Username;
			dp.Name = "My Results";
			dp.Domain = this.Domain;
			dp.Store();
			var item = new RemoteResource(context);
			item.Location = GetPrivateCatalogueSeriesUrl(false);
			dp.AddResourceItem(item);
			return dp;
		}

        /// <summary>
        /// Gets the private data package catalogue index.
        /// </summary>
        /// <returns>The private data package catalogue index.</returns>
        public DataPackage GetPrivateDataPackageCatalogueIndex(){
            DataPackage dp = null;
            try{
                dp = DataPackage.FromIdentifier(context, "_index_" + this.Username);
            }catch(Exception){
                dp = CreatePrivateDataPackageCatalogueIndex();
            }
            return dp;
        }

        /// <summary>
        /// Gets the private data package catalogue results.
        /// </summary>
        /// <returns>The private data package catalogue results.</returns>
        public DataPackage GetPrivateDataPackageCatalogueProducts() {
			DataPackage dp = null;
			try {
				dp = DataPackage.FromIdentifier(context, "_products_" + this.Username);
			} catch (Exception) {
				dp = CreatePrivateDataPackageCatalogueProducts();
			}
			return dp;
		}

		/// <summary>
		/// Gets the private data package catalogue series.
		/// </summary>
		/// <returns>The private data package catalogue series.</returns>
		public DataPackage GetPrivateDataPackageCatalogueSeries() {
			DataPackage dp = null;
			try {
				dp = DataPackage.FromIdentifier(context, "_series_" + this.Username);
			} catch (Exception) {
				dp = CreatePrivateDataPackageCatalogueSeries();
			}
			return dp;
		}

        /// <summary>
        /// Loads the SSH pub key.
        /// </summary>
        public void LoadSSHPubKey() {
            if (string.IsNullOrEmpty(this.TerradueCloudUsername)) this.LoadCloudUsername();
            if (string.IsNullOrEmpty(this.TerradueCloudUsername)) return;

            var url = string.Format("{0}?token={1}&username={2}&request=sshPublicKey",
                                    context.GetConfigValue("t2portal-usrinfo-endpoint"),
                                    context.GetConfigValue("t2portal-safe-token"),
                                    this.TerradueCloudUsername);

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "GET";
            request.ContentType = "application/json";
            request.Accept = "application/json";
            request.Proxy = null;

            using (var httpResponse = (HttpWebResponse)request.GetResponse()) {
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream())) {
                    this.SshPubKey = streamReader.ReadToEnd();
                    this.SshPubKey = this.SshPubKey.Replace("\"", "");
                    context.LogDebug(this, "Terradue Cloud SSH pubkey found : " + this.SshPubKey);
                }
            }
        }

        /// <summary>
        /// Gets the first login date.
        /// </summary>
        /// <returns>The first login date.</returns>
        public DateTime GetFirstLoginDate() {
            DateTime value = DateTime.MinValue;
            try {
                System.Data.IDbConnection dbConnection = context.GetDbConnection();
                string sql = String.Format("SELECT log_time FROM usrsession WHERE id_usr={0} ORDER BY log_time ASC LIMIT 1;", this.Id);
                System.Data.IDataReader reader = context.GetQueryResult(sql, dbConnection);
                if (reader.Read()) value = context.GetDateTimeValue(reader, 0);
                reader.Close();
            } catch (Exception) { }
            return value;
        }

        /// <summary>
        /// Gets the last login date.
        /// </summary>
        /// <returns>The last login date.</returns>
        public DateTime GetLastLoginDate() {
            DateTime value = DateTime.MinValue;
            try {
                System.Data.IDbConnection dbConnection = context.GetDbConnection();
                string sql = String.Format("SELECT log_time FROM usrsession WHERE id_usr={0} ORDER BY log_time DESC LIMIT 1;", this.Id);
                System.Data.IDataReader reader = context.GetQueryResult(sql, dbConnection);
                if (reader.Read()) value = context.GetDateTimeValue(reader, 0);
                reader.Close();
            } catch (Exception) { }
            return value;
        }

        public List<int> GetGroups() {
            List<int> result = new List<int>();

            System.Data.IDbConnection dbConnection = context.GetDbConnection();
            string sql = String.Format("SELECT id_grp FROM usr_grp WHERE id_usr={0};", this.Id);
            System.Data.IDataReader reader = context.GetQueryResult(sql, dbConnection);
            while (reader.Read()) {
                if (reader.GetValue(0) != DBNull.Value)
                    result.Add(reader.GetInt32(0));
            }
            reader.Close();

            return result;
        }

        public override bool IsPostFiltered(NameValueCollection parameters) {
            foreach (var key in parameters.AllKeys) {
                switch (parameters[key]) {
                case "correlatedTo":
                    return true;
                default:
                    break;
                }
            }
            return false;
        }

        //public override int GetEntityListTotalResults(IfyContext context, NameValueCollection parameters) {
        //    foreach (var key in parameters.AllKeys) {
        //        switch (parameters[key]) {
        //        case "correlatedTo":

        //        default:
        //            break;
        //        }
        //    }
        //    var sql = string.Format("SELECT count(DISTINCT id) FROM domain AS d LEFT JOIN rolegrant AS rg ON d.id=rg.id_domain WHERE d.kind={0} OR (d.kind != {1} AND rg.id_usr={2});", (int)DomainKind.Public, (int)DomainKind.User, context.UserId);
        //    var count = context.GetQueryIntegerValue(sql);
        //    return count;
        //}

        public override KeyValuePair<string, string> GetFilterForParameter(string parameter, string value) {
            switch (parameter) {
            case "uid":
                return new KeyValuePair<string, string>("Identifier", value);
            case "id":
                return new KeyValuePair<string, string>("Identifier", value);
            default:
                return base.GetFilterForParameter(parameter, value);
            }
        }


        public override AtomItem ToAtomItem(NameValueCollection parameters) {

            var entityType = EntityType.GetEntityType(typeof(User));
            Uri id = new Uri(context.BaseUrl + "/" + entityType.Keyword + "/search?id=" + this.Identifier);

            if (!string.IsNullOrEmpty(parameters["correlatedTo"])) {
                var self = parameters["correlatedTo"];
                var settings = new OpenSearchableFactorySettings(MasterCatalogue.OpenSearchEngine);
                var entity = new UrlBasedOpenSearchable(context, new OpenSearchUrl(self), settings).Entity;

                if (entity is EntityList<WpsJob>) {
                    var entitylist = entity as EntityList<WpsJob>;
                    var items = entitylist.GetItemsAsList();
                    if (items.Count > 0) {
                        var job = items[0];
                        if (job.Owner.Id == this.Id || !job.IsSharedToUser(this.Id)) return null;
                    }
                } else if (entity is EntityList<DataPackage>) {
                    var entitylist = entity as EntityList<DataPackage>;
                    var items = entitylist.GetItemsAsList();
                    if (items.Count > 0) {
                        var dp = items[0];
                        if (dp.Owner.Id == this.Id || !dp.IsSharedToUser(this.Id)) return null;
                    }
                }

            }

            AtomItem result = new AtomItem();

            result.Id = id.ToString();
            result.Title = new TextSyndicationContent(this.Identifier);
            result.Content = new TextSyndicationContent(this.FirstName + " " + this.LastName);

            result.ElementExtensions.Add("identifier", "http://purl.org/dc/elements/1.1/", this.Identifier);
            if (!string.IsNullOrEmpty(Country)) result.ElementExtensions.Add(new SyndicationElementExtension("country", "http://purl.org/dc/elements/1.1/", Country));
            if (!string.IsNullOrEmpty(Affiliation)) result.ElementExtensions.Add(new SyndicationElementExtension("affiliation", "http://purl.org/dc/elements/1.1/", Affiliation));
            result.ReferenceData = this;

            this.LoadRegistrationInfo();
            var lastlogin = GetLastLoginDate();

            if (this.RegistrationDate != DateTime.MinValue) result.PublishDate = new DateTimeOffset(this.RegistrationDate);
            if (lastlogin != DateTime.MinValue) result.LastUpdatedTime = new DateTimeOffset(lastlogin);

            var basepath = new UriBuilder(context.BaseUrl);
            basepath.Path = "#!/" + entityType.Keyword + "/details/" + Username;
            string usrUri = basepath.Uri.AbsoluteUri;
            string usrName = (!String.IsNullOrEmpty(FirstName) && !String.IsNullOrEmpty(LastName) ? FirstName + " " + LastName : Username);
            SyndicationPerson author = new SyndicationPerson(context.UserLevel == UserLevel.Administrator ? Email : null, usrName, usrUri);
            author.ElementExtensions.Add(new SyndicationElementExtension("identifier", "http://purl.org/dc/elements/1.1/", Username));

            result.Authors.Add(author);

            if (context.AccessLevel == EntityAccessLevel.Administrator || context.UserId == this.Id) {
                result.Categories.Add(new SyndicationCategory("balance", null, GetAccountingBalance().ToString()));
            }

            result.Links.Add(new SyndicationLink(id, "self", this.Identifier, "application/atom+xml", 0));

            return result;
        }

        public NameValueCollection GetOpenSearchParameters() {
            return OpenSearchFactory.GetBaseOpenSearchParameter();
        }

        /// <summary>
        /// Gets the avatar based on discuss.
        /// </summary>
        /// <returns>The avatar.</returns>
        public string GetAvatar() {
            LoadCloudUsername();
            var avatarusername = String.IsNullOrEmpty(TerradueCloudUsername) ? Username : TerradueCloudUsername;
            avatarusername = avatarusername.Replace(" ", "");
            if (avatarusername.Contains("@") || avatarusername.Contains("?") || avatarusername.Contains("&")) avatarusername = "na";
            return string.Format("{0}/user_avatar/discuss.terradue.com/{1}/50/45_1.png", context.GetConfigValue("discussBaseUrl"), avatarusername);
        }

        /// <summary>
        /// Gets the user communities.
        /// </summary>
        /// <returns>The user communities.</returns>
        public List<ThematicCommunity> GetUserCommunities() { 
            if (context.UserId == 0) return new List<ThematicCommunity>();

            //CommunityCollection domains = new CommunityCollection(context);
            //domains.LoadRestricted(new DomainKind[] { DomainKind.Public, DomainKind.Private });
            //return domains.GetItemsAsList();

            EntityList<ThematicCommunity> domains = new EntityList<ThematicCommunity>(context);
            domains.SetFilter("Kind", (int)DomainKind.Public + "," + (int)DomainKind.Private);
            domains.Load();
            return domains.GetItemsAsList();
        }

        /// <summary>
        /// Gets the user roles.
        /// </summary>
        /// <returns>The user roles.</returns>
        /// <param name="domain">Domain.</param>
        public List<Role> GetUserRoles(Domain domain) {
            return Role.GetUserRolesForDomain(context, this.Id, domain.Id).ToList();
        }

        #region ACCOUNTING

        /// <summary>
        /// Gets the accounting balance.
        /// </summary>
        /// <returns>The accounting balance.</returns>
        public double GetAccountingBalance() {
            if (!context.GetConfigBooleanValue("accounting-enabled")) return 0;
            return TransactionFactory.GetUserBalance(this);
        }

        /// <summary>
        /// Adds the accounting transaction.
        /// </summary>
        /// <param name="balance">Balance.</param>
        public void AddAccountingTransaction(double balance, TransactionKind kind) {
            var transaction = new Transaction(context);
            transaction.OwnerId = this.Id;
            transaction.LogTime = DateTime.UtcNow;
            transaction.Balance = Math.Abs(balance);
            transaction.Kind = kind;
            transaction.Store();
        }

        #endregion
    }

    [DataContract]
    public class WebT2ssoUserInfo{
        [DataMember]
        public string Username { get; set; }
        [DataMember]
        public string ApiKey { get; set; }
    }
}

