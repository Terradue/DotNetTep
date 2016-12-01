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
    [EntityTable(null, EntityTableConfiguration.Custom, Storage = EntityTableStorage.Above)]
    public class UserTep : User {

        private static readonly log4net.ILog log = log4net.LogManager.GetLogger
            (System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Gets or sets the private domain of the user.
        /// </summary>
        /// <value>The domain.</value>
        public override Domain Domain { 
            get {
                if (base.Domain == null) {
                    try {
                        base.Domain = Domain.FromIdentifier (context, Username);
                    } catch (Exception e) {}
                }
                return base.Domain;
            }
            set {
                base.Domain = value;
            }
        }

        public override int DomainId {
            get {
                return Domain.Id;
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
        [EntityDataField ("apikey")]
        public string ApiKey {
            get;
            set;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Terradue.Tep.Controller.UserTep"/> class.
        /// </summary>
        /// <param name="context">Context.</param>
        public UserTep(IfyContext context) : base(context) {
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
            if (!string.IsNullOrEmpty (ApiKey))
                return String.Format ("t.apikey='{0}'", ApiKey);
            else return null;
        }

        public static UserTep GetPublicUser(IfyContext context, int id){
            context.AccessLevel = EntityAccessLevel.Administrator;
            UserTep usr = new UserTep(context, User.FromId(context, id));
            return usr;
        }

        public override void Load(){
            base.Load();
            this.LoadCloudUsername();

            if (Domain == null) CreatePrivateDomain ();
        }

        public override void Store(){
            bool isnew = (this.Id == 0);
            base.Store();
            if (isnew) {
                //create github profile
                GithubProfile github = new GithubProfile(context, this.Id);
                github.Store();
            }
        }

        /// <summary>
        /// Creates a new User instance representing the user with the specified ID.
        /// </summary>
        /// <returns>The identifier.</returns>
        /// <param name="context">Context.</param>
        /// <param name="id">Identifier.</param>
        public new static UserTep FromId(IfyContext context, int id){
            UserTep user = new UserTep(context);
            user.Id = id;
            user.Load();
            return user;
        }

        public new static UserTep FromApiKey (IfyContext context, string key)
        {
            UserTep user = new UserTep (context);
            user.ApiKey = key;
            user.Load ();
            return user;
        }

        /// <summary>
        /// Loads the cloud username.
        /// </summary>
        public void LoadCloudUsername(){
            LoadCloudUsername(context.GetConfigIntegerValue("One-default-provider"));
        }

        /// <summary>
        /// Gets the cloud username.
        /// </summary>
        /// <returns>The cloud username.</returns>
        /// <param name="providerId">Provider identifier.</param>
        public void LoadCloudUsername(int providerId){
            if (providerId == 0) return;
            try{
                Terradue.Cloud.CloudUser cusr = Terradue.Cloud.CloudUser.FromIdAndProvider(context, this.Id, providerId);
                this.TerradueCloudUsername = cusr.CloudUsername;
            }catch(Exception){
                this.TerradueCloudUsername = null;
            }
        }

        /// <summary>
        /// Stores the cloud username.
        /// </summary>
        public void StoreCloudUsername(){
            StoreCloudUsername(context.GetConfigIntegerValue("One-default-provider"));
        }

        /// <summary>
        /// Stores the cloud username.
        /// </summary>
        /// <param name="providerId">Provider identifier.</param>
        /// <param name="cloudusername">Cloudusername.</param>
        public void StoreCloudUsername(int providerId){
            //In case user has no record in db for the cloud provider
            context.Execute (String.Format ("INSERT IGNORE INTO usr_cloud (id, id_provider, username) VALUES ({0},{1},NULL);", this.Id, providerId));

            Terradue.Cloud.CloudUser cusr;
            try{
                cusr = Terradue.Cloud.CloudUser.FromIdAndProvider(context, this.Id, providerId);
            }catch(Exception e){
                //record does not exist in db
                cusr = new Terradue.Cloud.CloudUser(context);
                cusr.ProviderId = providerId;
                cusr.UserId = this.Id;
            }
            cusr.CloudUsername = this.TerradueCloudUsername;
            cusr.Store();
        }

        /// <summary>
        /// Finds the terradue cloud username.
        /// </summary>
        public void FindTerradueCloudUsername(){
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
            using (var httpResponse = (HttpWebResponse)request.GetResponse ()) {
                using (var streamReader = new StreamReader (httpResponse.GetResponseStream ())) {
                    this.TerradueCloudUsername = streamReader.ReadToEnd ();
                    this.StoreCloudUsername (context.GetConfigIntegerValue ("One-default-provider"));
                    context.LogDebug (this, "Found Terradue Cloud Username : " + this.TerradueCloudUsername);
                }
            }
        }

        /// <summary>
        /// Creates the private domain.
        /// </summary>
        public void CreatePrivateDomain () {
            //create new domain with Identifier = Username
            var privatedomain = new Domain (context);
            privatedomain.Identifier = Username;
            privatedomain.Description = "Domain of user " + Username;
            privatedomain.Kind = DomainKind.User;
            privatedomain.Store ();

            //set the userdomain
            Domain = privatedomain;

            //Get role owner
            var userRole = Role.FromIdentifier (context, "owner");

            //Grant role for user
            userRole.GrantToUser (this, Domain);

        }

        /// <summary>
        /// Generates the API key.
        /// </summary>
        public void GenerateApiKey () {
            this.ApiKey = Guid.NewGuid ().ToString ();
        }

        /// <summary>
        /// Creates the SSO account.
        /// </summary>
        /// <param name="password">Password.</param>
        public void CreateSSOAccount(string password){
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
                "\"password\":\"" + password + "\"" +
                "}";

            using (var streamWriter = new StreamWriter(request.GetRequestStream())) {
                streamWriter.Write(json);
                streamWriter.Flush();
                streamWriter.Close();

                using (var httpResponse = (HttpWebResponse)request.GetResponse ()) {
                    using (var streamReader = new StreamReader (httpResponse.GetResponseStream ())) {//TODO in case of error
                        var result = streamReader.ReadToEnd ();
                        User resUser = ServiceStack.Text.JsonSerializer.DeserializeFromString<User> (result);
                        this.TerradueCloudUsername = resUser.Username;
                        context.LogDebug (this, "Terradue Cloud Account created : " + this.TerradueCloudUsername);
                        this.StoreCloudUsername ();
                    }
                }
            }
        }

        /// <summary>
        /// Loads the SSH pub key.
        /// </summary>
        public void LoadSSHPubKey(){
            if(string.IsNullOrEmpty(this.TerradueCloudUsername)) this.LoadCloudUsername();
            if(string.IsNullOrEmpty(this.TerradueCloudUsername)) return;

            var url = string.Format("{0}?token={1}&username={2}&request=sshPublicKey",
                                    context.GetConfigValue("t2portal-usrinfo-endpoint"),
                                    context.GetConfigValue("t2portal-safe-token"),
                                    this.TerradueCloudUsername);

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "GET";
            request.ContentType = "application/json";
            request.Accept = "application/json";
            request.Proxy = null;

            using (var httpResponse = (HttpWebResponse)request.GetResponse ()){
                using (var streamReader = new StreamReader (httpResponse.GetResponseStream ())) {
                    this.SshPubKey = streamReader.ReadToEnd ();
                    this.SshPubKey = this.SshPubKey.Replace ("\"", "");
                    context.LogDebug (this, "Terradue Cloud SSH pubkey found : " + this.SshPubKey);
                }
            }
        }

        /// <summary>
        /// Gets the first login date.
        /// </summary>
        /// <returns>The first login date.</returns>
        public DateTime GetFirstLoginDate(){
            DateTime value = DateTime.MinValue;
            try{
                System.Data.IDbConnection dbConnection = context.GetDbConnection();
                string sql = String.Format("SELECT log_time FROM usrsession WHERE id_usr={0} ORDER BY log_time ASC LIMIT 1;",this.Id);
                System.Data.IDataReader reader = context.GetQueryResult(sql, dbConnection);
                if (reader.Read()) value = context.GetDateTimeValue(reader, 0);
                reader.Close();
            }catch(Exception){}
            return value;
        }

        /// <summary>
        /// Gets the last login date.
        /// </summary>
        /// <returns>The last login date.</returns>
        public DateTime GetLastLoginDate(){
            DateTime value = DateTime.MinValue;
            try{
                System.Data.IDbConnection dbConnection = context.GetDbConnection();
                string sql = String.Format("SELECT log_time FROM usrsession WHERE id_usr={0} ORDER BY log_time DESC LIMIT 1;",this.Id);
                System.Data.IDataReader reader = context.GetQueryResult(sql, dbConnection);
                if (reader.Read()) value = context.GetDateTimeValue(reader, 0);
                reader.Close();
            }catch(Exception){}
            return value;
        }

        public List<int> GetGroups(){
            List<int> result = new List<int>();

            System.Data.IDbConnection dbConnection = context.GetDbConnection();
            string sql = String.Format("SELECT id_grp FROM usr_grp WHERE id_usr={0};",this.Id);
            System.Data.IDataReader reader = context.GetQueryResult(sql, dbConnection);
            while (reader.Read()) {
                if (reader.GetValue(0) != DBNull.Value)
                    result.Add(reader.GetInt32(0));
            }
            reader.Close();

            return result;
        }

    }
}

