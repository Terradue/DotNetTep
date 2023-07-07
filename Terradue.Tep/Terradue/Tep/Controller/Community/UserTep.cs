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
using System.Runtime.Caching;
using Terradue.Portal.Urf;

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

        private static readonly object _Lock = new object();

        /// <summary>
        /// Gets or sets the private domain of the user.
        /// </summary>
        /// <value>The domain.</value>
        public override Domain Domain {
            get {
                if (base.Domain == null) {
                    try {
                        base.Domain = GetPrivateDomain();
                    } catch (Exception) {
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

        public double Credit {
            get {
                return this.GetCredit();
            }            
        }

        private TransactionFactory _transactionFactory;
        private TransactionFactory TransactionFactory {
            get {
                if(_transactionFactory == null) _transactionFactory = new TransactionFactory(context);
                return _transactionFactory;
            }
        }

        public List<List<UrfTep>> LoadASDs()
        {            
            if (string.IsNullOrEmpty(this.TerradueCloudUsername)) this.LoadCloudUsername();
            if (string.IsNullOrEmpty(this.TerradueCloudUsername)) throw new Exception("Impossible to get Terradue username");

            var url = string.Format("{0}?token={1}&username={2}&request=urf",
                                context.GetConfigValue("t2portal-usrinfo-endpoint"),
                                context.GetConfigValue("t2portal-safe-token"),
                                this.TerradueCloudUsername);

            HttpWebRequest t2request = (HttpWebRequest)WebRequest.Create(url);
            t2request.Method = "GET";
            t2request.ContentType = "application/json";
            t2request.Accept = "application/json";
            t2request.Proxy = null;
            
            var urfs = System.Threading.Tasks.Task.Factory.FromAsync<WebResponse>(t2request.BeginGetResponse,
                                                                       t2request.EndGetResponse,
                                                                       null)
            .ContinueWith(task =>
            {
                try{
                    var httpResponse = (HttpWebResponse) task.Result;
                    using (var streamReader = new StreamReader(httpResponse.GetResponseStream())) {
                        string result = streamReader.ReadToEnd();                    
                        return JsonSerializer.DeserializeFromString<List<List<UrfTep>>>(result);                    
                    }
                }catch(Exception e){
                    throw e;
                }
            }).ConfigureAwait(false).GetAwaiter().GetResult();

            if(urfs != null){
                foreach (var urf in urfs) {                    
                    if(urf.Count > 0){                        
                        var asd = urf[0];
                        //check if urf already stored in DB
                        var dburfs = new EntityList<ASD>(context);
                        dburfs.SetFilter("Identifier", asd.UrfInformation.Identifier);
                        dburfs.Load();
                        var items = dburfs.GetItemsAsList();
                        if(items.Count == 0){
                            //not stored in DB
                            var asdToStore = ASD.FromURF(context, asd);
                            asdToStore.Store();
                            asdToStore.AddPermissions(asd);
                        } else {                            
                            var dbasd = items[0];
                            //check if credit updated
                            if(asd.UrfInformation.Credit != dbasd.CreditTotal){
                                dbasd.CreditTotal = asd.UrfInformation.Credit;
                                dbasd.Store();
                            }
                            //check if status updated
                            if(asd.UrfInformation.Status != dbasd.Status){
                                dbasd.CreditTotal = asd.UrfInformation.Credit;
                                dbasd.Store();
                            }
                            asd.UrfCreditInformation.Credit = dbasd.CreditTotal;
                            asd.UrfCreditInformation.CreditRemaining = dbasd.CreditRemaining;
                        }
                    }
                }
            }

            return urfs;
        }

        public double GetCredit(){
            double credit=0;
            
            var dburfs = ASD.FromUsr(context, this.Id);            
            foreach(var item in dburfs){
                if(item.Status == UrfStatus.Activated)
                    credit += item.CreditRemaining;
            }
            
            return credit;
        }

        public void UseCredit(WpsJob job, double cost){
            var dburfs = ASD.FromUsr(context, this.Id);
            foreach(var item in dburfs){                
                var remaining = item.CreditRemaining;
                if(remaining > 0){
                    if(remaining >= cost){
                        item.CreditUsed += cost;
                        item.Store();
                        ASDTransactionFactory.CreateTransaction(context, this.Id, item, job, cost);
                        break;
                    } else {
                        item.CreditUsed = item.CreditTotal;
                        item.Store();
                        cost -= remaining;
                        ASDTransactionFactory.CreateTransaction(context, this.Id, item, job, remaining);
                    }
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Terradue.Tep.Controller.UserTep"/> class.
        /// </summary>
        /// <param name="context">Context.</param>
        public UserTep(IfyContext context) : base(context) {}

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
            if (!string.IsNullOrEmpty(ApiKey)) return String.Format("t.apikey='{0}'", ApiKey);
            if (!string.IsNullOrEmpty(Email)) return String.Format("t.email='{0}'", Email);
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
            if (Domain == null) CreatePrivateDomain();
            if (string.IsNullOrEmpty(this.ApiKey)) GenerateApiKey();
            if (IsNeededTerradueUserInfo()) LoadTerradueUserInfo();            
        }

        public override void Store() {
            bool isnew = (this.Id == 0);
            base.Store();
            if (isnew) {
                //create github profile
                GithubProfile github = new GithubProfile(context, this.Id);
                github.Store();
                //create private domain
                CreatePrivateDomain();
                //send registration email to support
                SendRegistrationEmailToSupport();
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

        public static UserTep FromIdentifier(IfyContext context, string identifier) {
            UserTep user = new UserTep(context);
            user.Identifier = identifier;
            user.Load();
            return user;
        }

        public new static UserTep FromEmail(IfyContext context, string email) {
            UserTep user = new UserTep(context);
            user.Email = email;
            user.Load();
            return user;
        }

        public static UserTep FromApiKey(IfyContext context, string key) {
            UserTep user = new UserTep(context);
            user.ApiKey = key;
            user.Load();
            return user;
        }

        private void SendRegistrationEmailToSupport(){
            try{
                var portalname = string.Format("{0} Portal", context.GetConfigValue("SiteNameShort"));
                var subject = string.Format("[{0}] - User registration on {0} from T2 IdP", portalname);
                var body = string.Format("This is an automatic email to notify that an account has been automatically created on {2} with the username {0} ({1}).\nThe request was performed from a first sign-in session on {2}.", this.Username, this.Email, portalname);
                context.SendMail(context.GetConfigValue("SmtpUsername"), context.GetConfigValue("SmtpUsername"), subject, body);
            } catch(Exception e){
                context.LogError(this,e.Message + " - " + e.StackTrace);
            }
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
            context.Execute(String.Format("UPDATE usr_cloud SET username='{0}' WHERE id={1} AND id_provider={2};", this.TerradueCloudUsername, this.Id, providerId));

            //update data packages (Terradue Cloud username may have change)
            GetPrivateDataPackageCatalogueIndex();
            GetPrivateDataPackageCatalogueProducts();
            GetPrivateDataPackageCatalogueSeries();
            GetPrivateThematicApp();

            //stores also the T2 SSO username
            if(!string.IsNullOrEmpty(context.GetConfigValue("T2-sso-auth-identifier"))){
                context.Execute(String.Format("UPDATE usr_auth SET username='{0}' WHERE id_usr={1} AND id_auth=(SELECT id FROM auth WHERE identifier='{2}');", this.TerradueCloudUsername, this.Id, context.GetConfigValue("T2-sso-auth-identifier")));
            }
        }

        /// <summary>
        /// Is the terradue user info needed.
        /// </summary>
        /// <returns><c>true</c>, if needed terradue user info was ised, <c>false</c> otherwise.</returns>
        public virtual bool IsNeededTerradueUserInfo(){
            if (context.UserId != this.Id || context.UserId == 0) return false;
            if (AccountStatus != AccountStatusType.Enabled) return false;
            if (Level < 2) return false; //User must be at least starter
			if (HttpContext.Current == null || HttpContext.Current.Session == null) return false;//if there is no Current context or session, we won't get the apikey
			if (HttpContext.Current.Session["t2loading"] != null && HttpContext.Current.Session["t2loading"] as string == "true") return false;//if loading the apikey, we cannot get it yet
            var apikey = GetSessionApiKey();
            if (TerradueCloudUsername == null || apikey == null) return true;
            else return false;
		}

        /// <summary>
        /// Loads the terradue user info.
        /// </summary>
        private void LoadTerradueUserInfo(){
            context.LogDebug(this, "Loading Terradue info - " + this.Username);
			if (HttpContext.Current != null && HttpContext.Current.Session != null) HttpContext.Current.Session["t2loading"] = "true";

            HttpContext.Current.Session["t2profileError"] = null;

            try {
                if (TerradueCloudUsername == null) {
                    //no TerradueCloudUsername, we need to load it (+ apikey)
                    var payload = string.Format("username={0}&email={1}&originator={2}{3}",
                        this.Username,
                        this.Email,
                        context.GetConfigValue("SiteNameShort"),
                        this.Level == 2 ? "&plan=" + context.GetConfigValue("t2portal-usr-starterPlan") : this.Level == 3 ? "&plan=" + context.GetConfigValue("t2portal-usr-explorerPlan") : "");
                    System.Text.ASCIIEncoding encoding = new System.Text.ASCIIEncoding();
                    byte[] payloadBytes = encoding.GetBytes(payload);
                    var sso = System.Convert.ToBase64String(payloadBytes);
                    var sig = TepUtility.HashHMAC(context.GetConfigValue("sso-eosso-secret"), sso);

                    var url = string.Format("{0}?payload={1}&sig={2}", context.GetConfigValue("t2portal-usr-endpoint"), sso, sig);
                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                    request.Proxy = null;
                    request.Method = "GET";
                    request.ContentType = "application/json";
                    request.Accept = "application/json";
                    var info = System.Threading.Tasks.Task.Factory.FromAsync<WebResponse>(request.BeginGetResponse,
                                                                       request.EndGetResponse,
                                                                       null)
					.ContinueWith(task =>
					{
						var httpResponse = (HttpWebResponse) task.Result;
						using (var streamReader = new StreamReader(httpResponse.GetResponseStream())) {
							string result = streamReader.ReadToEnd();
							try {
								return JsonSerializer.DeserializeFromString<WebT2ssoUserInfo>(result);
							} catch (Exception e) {
								throw e;
							}
						}
					}).ConfigureAwait(false).GetAwaiter().GetResult();    
                    
                    if (this.TerradueCloudUsername != info.Username) {//we update only if it changed
                        this.TerradueCloudUsername = info.Username;
                        this.StoreCloudUsername();
                    }
                    context.LogDebug(this, "Found Terradue Cloud Username : " + this.TerradueCloudUsername);
                    SetSessionApikey(info.ApiKey);
                    this.Store();
                } else {
                    var apikey = GetSessionApiKey();
                    if (apikey == null){
                        //no TerradueAPIKey, we need to load it (we load only the apikey)
                        apikey = LoadApiKeyFromRemote();
                        SetSessionApikey(apikey);
                    }
                }
            } catch (Exception e) {
                if (HttpContext.Current != null && HttpContext.Current.Session != null) HttpContext.Current.Session["t2profileError"] = e.Message;
            }
            if (HttpContext.Current != null && HttpContext.Current.Session != null) HttpContext.Current.Session["t2loading"] = null;
        }

        public string LoadApiKeyFromRemote() {
            var url = string.Format("{0}?token={1}&username={2}&request=apikey",
                                        context.GetConfigValue("t2portal-usrinfo-endpoint"),
                                        context.GetConfigValue("t2portal-safe-token"),
                                        this.TerradueCloudUsername);

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "GET";
            request.ContentType = "application/json";
            request.Accept = "application/json";
            request.Proxy = null;

            var apikey = System.Threading.Tasks.Task.Factory.FromAsync<WebResponse>(request.BeginGetResponse,
                                                                       request.EndGetResponse,
                                                                       null)
            .ContinueWith(task =>
            {
                var httpResponse = (HttpWebResponse) task.Result;
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream())) {
                    string result = streamReader.ReadToEnd();
                    try {
                        return result.Trim('"');
                    } catch (Exception e) {
                        throw e;
                    }
                }
            }).ConfigureAwait(false).GetAwaiter().GetResult();

            context.LogDebug(this, "Found Terradue API Key for user '" + this.TerradueCloudUsername + "'");
            return apikey;                
        }

        public void LoadProfileFromRemote() {
            var url = string.Format("{0}?token={1}&username={2}&request=profile",
                                        context.GetConfigValue("t2portal-usrinfo-endpoint"),
                                        context.GetConfigValue("t2portal-safe-token"),
                                        this.TerradueCloudUsername);

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "GET";
            request.ContentType = "application/json";
            request.Accept = "application/json";
            request.Proxy = null;

            var json = System.Threading.Tasks.Task.Factory.FromAsync<WebResponse>(request.BeginGetResponse,
                                                                        request.EndGetResponse,
                                                                            null)
            .ContinueWith(task =>
            {
                var httpResponse = (HttpWebResponse) task.Result;
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream())) {
                    return streamReader.ReadToEnd();                    
                }
            }).ConfigureAwait(false).GetAwaiter().GetResult();

            context.LogDebug(this, "Load T2 profile for user '" + this.TerradueCloudUsername + "' : " + json);
            var profileJson = JsonSerializer.DeserializeFromString<Dictionary<string, string>>(json);
            if (profileJson.ContainsKey("firstname") && !string.IsNullOrEmpty(profileJson["firstname"])) this.FirstName = profileJson["firstname"];
            if (profileJson.ContainsKey("lastname") && !string.IsNullOrEmpty(profileJson["lastname"])) this.LastName = profileJson["lastname"];
            if (profileJson.ContainsKey("affiliation_shortname") && !string.IsNullOrEmpty(profileJson["affiliation_shortname"])) this.Affiliation = profileJson["affiliation_shortname"];
            else if (profileJson.ContainsKey("affiliation") && !string.IsNullOrEmpty(profileJson["affiliation"])) this.Affiliation = profileJson["affiliation"];
            if (profileJson.ContainsKey("country") && !string.IsNullOrEmpty(profileJson["country"])) this.Country = profileJson["country"];
            this.Store();
        }

        /// <summary>
        /// Sets the session apikey.
        /// </summary>
        /// <param name="value">Value.</param>
        private void SetSessionApikey(string value){
            context.LogDebug(this, "SESSION - SET t2apikey="+value);
			try {
				HttpContext.Current.Session["t2apikey"] = value;
            } catch (Exception e) { 
				context.LogError(this, "SESSION - SET t2apikey -- " + e.Message);
			}
        }

        /// <summary>
        /// Gets the session API key.
        /// </summary>
        /// <returns>The session API key.</returns>
        public string GetSessionApiKey(){
			var apikey = "";
			try {
				apikey = HttpContext.Current.Session["t2apikey"] as string;
			}catch(Exception e){
				context.LogError(this, "SESSION - GET t2apikey -- " + e.Message);
			}
            context.LogDebug(this, "SESSION - GET t2apikey=" + apikey);
            return apikey;
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
            var username = System.Threading.Tasks.Task.Factory.FromAsync<WebResponse>(request.BeginGetResponse,
                                                                        request.EndGetResponse,
                                                                            null)
            .ContinueWith(task =>
            {
                var httpResponse = (HttpWebResponse) task.Result;
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream())) {
                    return streamReader.ReadToEnd();
                    
                }
            }).ConfigureAwait(false).GetAwaiter().GetResult();            

            this.TerradueCloudUsername = username;
            this.StoreCloudUsername();
            context.LogDebug(this, "Found Terradue Cloud Username : " + this.TerradueCloudUsername);
        }

        /// <summary>
        /// Check if user has T2 notebooks account created
        /// </summary>
        /// <returns></returns>
        public bool HasT2NotebooksAccount() {
            bool hasnotebooks = false;
            var url = string.Format("{0}?token={1}&username={2}&request=jupyterhub",
                                        context.GetConfigValue("t2portal-usrinfo-endpoint"),
                                        context.GetConfigValue("t2portal-safe-token"),
                                        this.TerradueCloudUsername);

            try{
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "GET";
                request.ContentType = "application/json";
                request.Accept = "application/json";
                request.Proxy = null;

                hasnotebooks = System.Threading.Tasks.Task.Factory.FromAsync<WebResponse>(request.BeginGetResponse,
                                                                        request.EndGetResponse,
                                                                            null)
                .ContinueWith(task =>
                {
                    var httpResponse = (HttpWebResponse) task.Result;
                    using (var streamReader = new StreamReader(httpResponse.GetResponseStream())) {
                        string result = streamReader.ReadToEnd();
                        try {                            
                            return result.Trim('"') == "true";
                        } catch (Exception e) {
                            throw e;
                        }
                    }
                }).ConfigureAwait(false).GetAwaiter().GetResult();               
            }catch(Exception e){
                context.LogError(this, e.Message, e);
            }
            return hasnotebooks;
        }

        /// <summary>
        /// Gets the nb of login.
        /// </summary>
        /// <returns>The nb of login.</returns>
        /// <param name="startdate">Startdate.</param>
        /// <param name="enddate">Enddate.</param>
        public int GetNbOfLogin(string startdate, string enddate) {
            string sql = string.Format("SELECT COUNT(id_usr) FROM usrsession WHERE id_usr={0} AND log_time > '{1}' AND log_time < '{2}';", this.Id, startdate, enddate);
            return context.GetQueryIntegerValue(sql);
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

                User resUser = System.Threading.Tasks.Task.Factory.FromAsync<WebResponse>(request.BeginGetResponse,
                                                                        request.EndGetResponse,
                                                                            null)
                .ContinueWith(task =>
                {
                    var httpResponse = (HttpWebResponse) task.Result;
                    using (var streamReader = new StreamReader(httpResponse.GetResponseStream())) {
                        string result = streamReader.ReadToEnd();
                        try {
                            return ServiceStack.Text.JsonSerializer.DeserializeFromString<User>(result);                                    
                        } catch (Exception e) {
                            throw e;
                        }
                    }
                }).ConfigureAwait(false).GetAwaiter().GetResult();

                this.TerradueCloudUsername = resUser.Username;
                context.LogDebug(this, "Terradue Cloud Account created : " + this.TerradueCloudUsername);
                this.StoreCloudUsername();                    
            }
        }

        /// <summary>
        /// Creates the private thematic app.
        /// </summary>
        private ThematicApplication CreatePrivateThematicApp() {
            if (string.IsNullOrEmpty(TerradueCloudUsername)) LoadCloudUsername();
            if (string.IsNullOrEmpty(TerradueCloudUsername)) return null;

            context.LogDebug(this, "Create private Thematic app for user " + this.Username);
            var app = new ThematicApplication(context);
            app.OwnerId = this.Id;
            app.Identifier = Guid.NewGuid().ToString();
            app.AccessKey = Guid.NewGuid().ToString();
            app.Name = "private thematic app";
            app.Domain = this.Domain;
            app.Store();
			var baseurl = context.GetConfigValue("catalog-baseurl");
            var url = baseurl + "/" + this.TerradueCloudUsername + "/series/_apps/description";
            var res = new RemoteResource(context);
            res.Location = url;
            app.AddResourceItem(res);
            return app;
        }

        /// <summary>
        /// Gets the private thematic app.
        /// </summary>
        /// <returns>The private thematic app.</returns>
        public ThematicApplication GetPrivateThematicApp(bool loadItems = true) {
            var items = GetPrivateAppList();

            ThematicApplication app = null;
            if (items.Count == 0) {
                app = CreatePrivateThematicApp();
            } else app = items[0];

            if (loadItems && app != null) {
                app.LoadItems();
                //add apikey to location
                foreach (var item in app.Items) item.Location += "?apikey=" + GetSessionApiKey();
            }
            return app;
        }

        private List<ThematicApplication> GetPrivateAppList() {
            var apps = new EntityList<ThematicApplication>(context);
            apps.SetFilter("Kind", ThematicApplication.KINDRESOURCESETAPPS + "");
            apps.SetFilter("DomainId", this.DomainId + "");
            apps.Load();
            var items = apps.GetItemsAsList();
            context.LogDebug(this, "GetPrivateAppList : found " + items.Count + " items (domain = " + this.DomainId + ")");
            return items;
        }

        /// <summary>
        /// Do private sanity check.
        /// </summary>
        public void PrivateSanityCheck(){
            if (string.IsNullOrEmpty(TerradueCloudUsername)) LoadCloudUsername();

            if (!string.IsNullOrEmpty(TerradueCloudUsername)) {
                GetPrivateThematicApp(false);
                GetPrivateDataPackageCatalogueIndex(false);
                GetPrivateDataPackageCatalogueProducts(false);
                GetPrivateDataPackageCatalogueSeries(false);
            }
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
        /// Creates the private data package.
        /// </summary>
        /// <returns>The private data package.</returns>
        /// <param name="identifier">Identifier.</param>
        /// <param name="name">Name.</param>
        /// <param name="location">Location.</param>
        private DataPackage CreatePrivateDataPackage(string identifier, string name, string location){
            //create data package
            var dp = new DataPackage(context);
            dp.Identifier = identifier;
            dp.Name = name;
            dp.Domain = this.Domain;
            dp.UserId = this.Id;
            dp.OwnerId = this.Id;
            dp.Store();

            //save data package location
            var item = new RemoteResource(context);
            item.Location = location;
            dp.AddResourceItem(item);
            return dp;
        }

        /// <summary>
        /// Gets the private data package.
        /// </summary>
        /// <returns>The private data package.</returns>
        /// <param name="identifier">Identifier.</param>
        /// <param name="name">Name.</param>
        /// <param name="location">Location.</param>
        public DataPackage GetPrivateDataPackage(string identifier, string name, string location, bool loadItems=true) {
            DataPackage dp = null;

            //if dp does not exists, we create it
            try {
                dp = DataPackage.FromIdentifier(context, identifier);
            } catch (Exception) {
                return CreatePrivateDataPackage(identifier, name, location);
            }

            if (loadItems) {
                //if dp exists, we update the location
                try {
                    dp.LoadItems();
                    var dpi = dp.Items.GetItemsAsList()[0];
                    if (!location.Equals(dpi.Location)) {
                        dpi.Location = location;
                        dpi.Store();
                    }
                } catch (Exception) {
                    //if location does not exists, we create it
                    var item = new RemoteResource(context);
                    item.Location = location;
                    dp.AddResourceItem(item);
                }
            }
            return dp;
        }

        /// <summary>
        /// Gets the private data package catalogue index.
        /// </summary>
        /// <returns>The private data package catalogue index.</returns>
        public DataPackage GetPrivateDataPackageCatalogueIndex(bool loadItems=true){
            var identifier = "_index_" + TepUtility.ValidateIdentifier(Username);
            var name = "My Index";
            var location = GetPrivateCatalogueIndexUrl(false);
            return GetPrivateDataPackage(identifier, name, location, loadItems);
        }

        /// <summary>
        /// Gets the private data package catalogue results.
        /// </summary>
        /// <returns>The private data package catalogue results.</returns>
        public DataPackage GetPrivateDataPackageCatalogueProducts(bool loadItems = true) {
            var identifier = "_products_" + TepUtility.ValidateIdentifier(Username);
            var name = "My Products";
            var location = GetPrivateCatalogueProductsUrl(false);
            return GetPrivateDataPackage(identifier, name, location, loadItems);
		}

		/// <summary>
		/// Gets the private data package catalogue series.
		/// </summary>
		/// <returns>The private data package catalogue series.</returns>
        public DataPackage GetPrivateDataPackageCatalogueSeries(bool loadItems = true) {
            var identifier = "_series_" + TepUtility.ValidateIdentifier(Username);
            var name = "My Results";
            var location = GetPrivateCatalogueSeriesUrl(false);
            return GetPrivateDataPackage(identifier, name, location, loadItems);
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

            this.SshPubKey = System.Threading.Tasks.Task.Factory.FromAsync<WebResponse>(request.BeginGetResponse,
                                                                        request.EndGetResponse,
                                                                            null)
            .ContinueWith(task =>
            {
                var httpResponse = (HttpWebResponse) task.Result;
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream())) {
                    string result = streamReader.ReadToEnd();
                    try {
                        return result.Replace("\"", "");;
                    } catch (Exception e) {
                        throw e;
                    }
                }
            }).ConfigureAwait(false).GetAwaiter().GetResult();
            context.LogDebug(this, "Terradue Cloud SSH pubkey found : " + this.SshPubKey);
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
                context.CloseQueryResult(reader, dbConnection);
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
                context.CloseQueryResult(reader, dbConnection);
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
            context.CloseQueryResult(reader, dbConnection);
            return result;
        }

        public override bool IsPostFiltered(NameValueCollection parameters) {
            foreach (var key in parameters.AllKeys) {
                switch (key) {
                case "correlatedTo":
                    return false;
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

        public override object GetFilterForParameter(string parameter, string value) {
            switch (parameter) {
            case "uid":
                return new KeyValuePair<string, string>("Identifier", value);
            case "id":
                return new KeyValuePair<string, string>("Identifier", value);
            case "level":
                return new KeyValuePair<string, string>("Level", value);
            case "affiliation":
                return new KeyValuePair<string, string>("Affiliation", value);
            case "correlatedTo":
                ObjectCache cache = MemoryCache.Default;
				var correlatedPolicy = HttpContext.Current.Request.QueryString["correlatedPolicy"];
                bool permissionOnly = false;
                bool privilegeOnly = false;
                switch (correlatedPolicy)
                {
                    case "permission":
                        permissionOnly = true;
                        break;
                    case "privilege":
                        privilegeOnly = true;
                        break;
                    default:
                        break;
                }
                var cachedItem = cache[value];
                if (cachedItem == null) { // if no cache yet, or is expired
                    lock (_Lock) { // we lock only in this case
                                   // you have to make one more check, another thread might have put item in cache already
                        cachedItem = cache[value];
                        if (cachedItem == null) {
                            var policy = new CacheItemPolicy();
                            policy.Priority = CacheItemPriority.NotRemovable;
                            policy.AbsoluteExpiration = DateTimeOffset.Now.AddMinutes(1);
                            var settings = MasterCatalogue.OpenSearchFactorySettings;
                            cachedItem = new UrlBasedOpenSearchable(context, new OpenSearchUrl(value), settings).Entity;
                            cache.Set(value, cachedItem, policy);
                        }
                    }
                }
                var sharedUsersIds = new List<int>();                
                if (cachedItem is EntityList<WpsJob>) {                    
                    var entitylist = cachedItem as EntityList<WpsJob>;
                    var items = entitylist.GetItemsAsList();
                    if (items.Count > 0)
                    {
                        var job = items[0];
                        sharedUsersIds = job.GetAuthorizedUserIds(permissionOnly, privilegeOnly).ToList();
                        sharedUsersIds.Remove(job.Owner.Id);
                    }
                } else if (cachedItem is EntityList<DataPackage>) {
                    var entitylist = cachedItem as EntityList<DataPackage>;
                    var items = entitylist.GetItemsAsList();
                    if (items.Count > 0) {
                        var dp = items[0];
						sharedUsersIds = dp.GetAuthorizedUserIds(permissionOnly, privilegeOnly).ToList();              
                        sharedUsersIds.Remove(dp.Owner.Id);
                    }
                } else if (cachedItem is EntityList<WpsProcessOffering>) {
                    var entitylist = cachedItem as EntityList<WpsProcessOffering>;
                    var items = entitylist.GetItemsAsList();
                    if (items.Count > 0) {
                        var s = items[0];
                        sharedUsersIds = s.GetAuthorizedUserIds(permissionOnly, privilegeOnly).ToList();              
                        sharedUsersIds.Remove(s.Owner.Id);                        
                    }
                }
                return new KeyValuePair<string, string>("Id", string.Join(",", sharedUsersIds));
            default:
                return base.GetFilterForParameter(parameter, value);
            }
        }


        public override AtomItem ToAtomItem(NameValueCollection parameters) {

            var entityType = EntityType.GetEntityType(typeof(User));
            Uri id = new Uri(context.BaseUrl + "/" + entityType.Keyword + "/search?id=" + this.Identifier);

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
            var icon = this.GetAvatar();
            if (!string.IsNullOrEmpty(icon)) author.ElementExtensions.Add(new SyndicationElementExtension("icon", "http://purl.org/dc/elements/1.1/", icon));
            if (!string.IsNullOrEmpty(FirstName)) author.ElementExtensions.Add(new SyndicationElementExtension("firstname", "http://purl.org/dc/elements/1.1/", FirstName));
            if (!string.IsNullOrEmpty(LastName)) author.ElementExtensions.Add(new SyndicationElementExtension("lastname", "http://purl.org/dc/elements/1.1/", LastName));
            author.ElementExtensions.Add(new SyndicationElementExtension("identifier", "http://purl.org/dc/elements/1.1/", Username));
            result.Authors.Add(author);

            if (context.AccessLevel == EntityAccessLevel.Administrator || context.UserId == this.Id) {
                result.Categories.Add(new SyndicationCategory("balance", null, GetAccountingBalance().ToString()));
                result.Categories.Add(new SyndicationCategory("credit", null, this.Credit + ""));
                result.ElementExtensions.Add("level", "https://www.terradue.com", this.Level);
                result.ElementExtensions.Add("status", "https://www.terradue.com", this.AccountStatus);
                if (string.IsNullOrEmpty(this.TerradueCloudUsername)) LoadCloudUsername();
                if (!string.IsNullOrEmpty(this.TerradueCloudUsername)) result.ElementExtensions.Add("t2username", "https://www.terradue.com", this.TerradueCloudUsername);

                var dpdefault = DataPackage.GetTemporaryForUser(context, this);
                var defaultDPItems = dpdefault.Items.Count;
                result.ElementExtensions.Add("defaultDPItems", "https://www.terradue.com", defaultDPItems);

                var communitiesRoles = new List<WebCommunityRoles>();
                var communities = this.GetUserCommunities();
                foreach (var community in communities) {
                    try {
                        var roles = this.GetUserRoles(community);
                        if (roles.Count > 0) {
                            var rolesS = new List<WebUserRole>();
                            foreach (var r in roles) rolesS.Add(new WebUserRole { Name = r.Name, Description = r.Description });
                            communitiesRoles.Add(new WebCommunityRoles {
                                Community = community.Name,
                                CommunityIdentifier = community.Identifier,
                                Link = string.Format("/#!communities/details/{0}", community.Identifier),
                                Roles = rolesS
                            });
                        }
                    } catch (Exception e) {
                        context.LogError(this, e.Message);
                    }
                }
                result.ElementExtensions.Add("roles", "https://www.terradue.com", communitiesRoles);
            }

            result.Links.Add(new SyndicationLink(id, "self", this.Identifier, "application/atom+xml", 0));

            return result;
        }

        public new NameValueCollection GetOpenSearchParameters() {
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

            CommunityCollection domains = new CommunityCollection(context);
            domains.UserStatus = ThematicCommunity.USERSTATUS_JOINED;
            domains.LoadRestricted(new DomainKind[] { DomainKind.Public, DomainKind.Private });
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

		/// <summary>
        /// Updates the user session end time.
        /// </summary>
        /// <param name="context">Context.</param>
        /// <param name="userId">User identifier.</param>
        public static void UpdateUserSessionEndTime(IfyContext context, int userId) {
            try {
                string sql = string.Format("UPDATE usrsession SET log_end='{0}' WHERE id_usr={1} order by log_time desc LIMIT 1;", context.Now.ToString(@"yyyy\-MM\-dd HH\:mm\:ss"), userId);
                context.Execute(sql);
            } catch (Exception e) {
                context.LogError(context, e.Message);
            }
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

    [DataContract]
    public class WebCommunityRoles {
        [DataMember]
        public string Community { get; set; }

        [DataMember]
        public string CommunityIdentifier { get; set; }

        [DataMember]
        public string Link { get; set; }

        [DataMember]
        public List<WebUserRole> Roles { get; set; }
    }

    [DataContract]
    public class WebUserRole {
        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string Description { get; set; }
    }
}

