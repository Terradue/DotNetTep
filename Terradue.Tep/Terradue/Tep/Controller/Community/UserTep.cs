using System;
using Terradue.Portal;
using Terradue.OpenNebula;
using Terradue.Github;
using Terradue.Util;
using Terradue.Cloud;


using System.Collections.Generic;

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


        #region T2Certificate

        private const int CERTIFICATE_PENDING_STATUS = 0;
        private const int CERTIFICATE_DEFAULT_STATUS = 1;

        /// <summary>
        /// Gets the cert subject.
        /// </summary>
        /// <value>The cert subject.</value>
        private string certsubject { get; set; }
        /// \xrefitem uml "UML" "UML Diagram"
        public string CertSubject {
            get {
                if (certsubject == null) {
                    try {
                        log.Info(String.Format("Certificate loaded from CA for user {0}", this.Username));
                        Terradue.Security.Certification.CertificateUser certUser = Terradue.Security.Certification.CertificateUser.FromId(context, Id);
                        certsubject = certUser.CertificateSubject;
                    } catch (EntityNotFoundException e) {
                        log.Error(String.Format("Error loading Certificate from CA for user {0}", this.Username));
                        certsubject = null;
                    }
                }
                return certsubject;
            }
        }

        /// <summary>
        /// Gets the x509 certificate.
        /// </summary>
        /// <value>The x509 certificate.</value>
        /// \xrefitem uml "UML" "UML Diagram"
        public System.Security.Cryptography.X509Certificates.X509Certificate2 X509Certificate {
            get {
                try {
                    log.Info(String.Format("Certificate X509 loaded from CA for user {0}", this.Username));
                    Terradue.Security.Certification.CertificateUser certUser = Terradue.Security.Certification.CertificateUser.FromId(context,Id);
                    return certUser.X509Certificate;
                }
                catch ( EntityNotFoundException e ){
                    log.Error(String.Format("Error loading Certificate X509 from CA for user {0}", this.Username));
                    return null;
                }
            }
        }
            
        /// <summary>
        /// Gets or sets the cert status.
        /// </summary>
        /// <value>The cert status.</value>
        public int CertStatus { get; protected set; }

        #endregion

        #region OpenNebula

        /// <summary>
        /// Gets or sets the one password.
        /// </summary>
        /// <value>The one password.</value>
        /// \xrefitem uml "UML" "UML Diagram"
        public string OnePassword { 
            get{ 
                if (onepwd == null) {
                    if (oneId == 0) {
                        try{
                            USER_POOL oneUsers = oneClient.UserGetPoolInfo();
                            foreach (object item in oneUsers.Items) {
                                if (item is USER_POOLUSER) {
                                    USER_POOLUSER oneUser = item as USER_POOLUSER;
                                    if (oneUser.NAME == this.Email) {
                                        oneId = Int32.Parse(oneUser.ID);
                                        onepwd = oneUser.PASSWORD;
                                        break;
                                    }
                                }
                            }
                        }catch(Exception e){
                            return null;
                        }
                    } else {
                        USER oneUser = oneClient.UserGetInfo(oneId);
                        onepwd = oneUser.PASSWORD;
                    }
                }
                return onepwd;
            } 
            set{
                onepwd = value;
            } 
        }
        private string onepwd { get; set; }
        private int oneId { get; set; }
        private OneClient oneClient { get; set; }

        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="Terradue.Tep.Controller.UserTep"/> class.
        /// </summary>
        /// <param name="context">Context.</param>
        public UserTep(IfyContext context) : base(context) {
            OneCloudProvider oneCloud = (OneCloudProvider)CloudProvider.FromId(context, context.GetConfigIntegerValue("One-default-provider"));
            oneClient = oneCloud.XmlRpc;
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

        public static UserTep GetPublicUser(IfyContext context, string identifier){
            context.RestrictedMode = false;
            UserTep usr = new UserTep(context, User.FromUsername(context, identifier));
            usr.certsubject = null;
            usr.OnePassword = null;
            return usr;
        }

        public override void Load(){
            base.Load();
            CertStatus = context.GetQueryIntegerValue(String.Format("SELECT status from usrcert WHERE id_usr={0}", this.Id));
        }

        public override void Store(){
            bool isnew = (this.Id == 0);
            base.Store();
            if (isnew) {
                //create github profile
                GithubProfile github = new GithubProfile(context, this.Id);
                github.Store();
                //create certificate record
                context.Execute(String.Format("INSERT INTO usrcert (id_usr) VALUES ({0});", this.Id));
                //create cloud user profile
                EntityList<CloudProvider> provs = new EntityList<CloudProvider>(context);
                provs.Load();
                foreach (CloudProvider prov in provs) {
                    context.Execute(String.Format("INSERT INTO usr_cloud (id, id_provider, username) VALUES ({0},{1},{2});", this.Id, prov.Id, StringUtils.EscapeSql(this.Email)));
                }
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

        /// <summary>
        /// Removes the certificate.
        /// </summary>
        /// \xrefitem uml "UML" "UML Diagram"
        public void RemoveCertificate() {
            
            string sql = String.Format("UPDATE usrcert SET cert_subject=NULL, cert_content_pem=NULL, status={1} WHERE id_usr={0};",this.Id, CERTIFICATE_PENDING_STATUS);
            context.Execute(sql);
            this.CertStatus = CERTIFICATE_PENDING_STATUS;
        }

        /// <summary>
        /// Resets the certificate status.
        /// </summary>
        public void ResetCertificateStatus(){
            string sql = String.Format("UPDATE usrcert SET status={1} WHERE id_usr={0};",this.Id, CERTIFICATE_DEFAULT_STATUS);
            context.Execute(sql);
            this.CertStatus = CERTIFICATE_DEFAULT_STATUS;
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

