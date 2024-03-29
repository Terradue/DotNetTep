using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Terradue.Portal;
using Terradue.Portal.Urf;

namespace Terradue.Tep
{
    [EntityTable("asd", EntityTableConfiguration.Custom, HasOwnerReference = true, IdentifierField = "identifier", NameField = "name", HasPermissionManagement = true)]
    
    public class ASD : Entity
    {
        
        [EntityDataField("status")]
        public UrfStatus Status { get; set; }

        [EntityDataField("overspending")]
        public string Overspending { get; set; }

        public bool OverspendingAllowed { 
            get {
                return !string.IsNullOrEmpty(this.Overspending);
            }              
        }

        [EntityDataField("credit_total")]
        public double CreditTotal { get; set; }

        [EntityDataField("credit_used")]
        public double CreditUsed { get; set; }

        [EntityDataField("startdate")]
        public DateTime StartDate { get; set; }

        [EntityDataField("enddate")]
        public DateTime EndDate { get; set; }

        private User User { 
            get {
                if(this.OwnerId != 0) return User.FromId(context, this.OwnerId);
                return null;
            }
        }

        public double CreditRemaining {
            get {
                return (CreditTotal - CreditUsed);
            }
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="T:Terradue.Tep.ASD"/> class.
        /// </summary>
        /// <param name="context">Context.</param>
        public ASD(IfyContext context) : base(context) { }

        /// <summary>
        /// Froms the identifier.
        /// </summary>
        /// <returns>The identifier.</returns>
        /// <param name="context">Context.</param>
        /// <param name="id">Identifier.</param>
        public static ASD FromId(IfyContext context, int id)
        {
            ASD result = new ASD(context);
            result.Id = id;
            result.Load();
            return result;
        }

        public static ASD FromIdentifier(IfyContext context, string identifier)
        {
            ASD result = new ASD(context);
            result.Identifier = identifier;
            result.Load();
            return result;
        }

        public static ASD FromURF(IfyContext context, Urf urf)
        {
            ASD result = new ASD(context);
            if(urf != null && urf.UrfInformation != null){
                result.Identifier = urf.UrfInformation.Identifier;
                result.Name = urf.UrfInformation.Title;
                result.StartDate = urf.UrfInformation.ActivityStartDate;
                result.EndDate = urf.UrfInformation.ActivityEndDate;
                result.CreditTotal = urf.UrfInformation.Credit;     
                result.Status = urf.UrfInformation.Status;           

                try{
                    var usr = User.FromEmail(context, urf.UrfInformation.Contacts.First(c => c.Primary == true).ContactEmail);
                    result.OwnerId = usr.Id;
                }catch(Exception e){                    
                    context.LogError(context, e.Message);
                }
            }
            return result;
        }

        public static List<ASD> FromUsr(IfyContext context, int id_usr){
            List<ASD> asds = new List<ASD>();
            List<int> ids = new List<int>();
            var sql = string.Format("SELECT id FROM asd WHERE id_usr={0};", id_usr);
            ids.AddRange(context.GetQueryIntegerValues(sql).ToList<int>());
            sql = string.Format("SELECT id_asd FROM asd_perm WHERE id_usr={0};", id_usr);
            ids.AddRange(context.GetQueryIntegerValues(sql).ToList<int>());

            ids = ids.Distinct().ToList();

            foreach(var id in ids){
                asds.Add(ASD.FromId(context, id));
            }
            return asds;
        }

        public void AddPermissions(Urf urf)
        {   
            var deleteSql = String.Format("DELETE FROM asd_perm WHERE id_asd={0};", this.Id);            
            context.LogDebug(this, deleteSql);                
            context.Execute(deleteSql);
            
            foreach(var contact in urf.UrfInformation.Contacts){
                var usr = User.FromEmail(context, contact.ContactEmail);        
                context.LogDebug(this, string.Format("Add usr perm for ASD {0} : {1} - {2}", this.Id, contact.ContactEmail, usr.Id));                
                if(usr.Id != 0){
                    var insertSql = String.Format("INSERT INTO asd_perm (id_asd,id_usr) VALUES ({0},{1});", this.Id, usr.Id);
                    context.LogDebug(this, insertSql);                
                    context.Execute(insertSql);
                }
            }
        }

        public void SyncUsers(Urf urf){
            List<string> emails = new List<string>();
            foreach(var contact in urf.UrfInformation.Contacts){
                emails.Add(contact.ContactEmail);
                var sqlcount = string.Format("SELECT count(*) FROM asd_perm WHERE id_asd = {0} AND id_usr=(SELECT id FROM usr WHERE email='{1}');",this.Id, contact.ContactEmail);
                if(context.GetQueryIntegerValue(sqlcount) == 0){
                    //we must insert the user
                    var insertSql = String.Format("INSERT INTO asd_perm (id_asd,id_usr) SELECT {0},id FROM usr WHERE email='{1}';", this.Id, contact.ContactEmail);
                    context.LogInfo(this, insertSql);                
                    context.Execute(insertSql);
                }
            }
            //check users not present anymore
            var sqllistids = string.Format("SELECT id_usr FROM asd_perm WHERE id_asd={0} and id_usr NOT IN (SELECT id FROM usr WHERE email IN ('{1}'));", this.Id, string.Join("','",emails));
            var ids = context.GetQueryIntegerValues(sqllistids);
            if(ids != null){
                foreach(var id in ids){
                    var sqlDelete = string.Format("DELETE FROM asd_perm WHERE id_asd={0} AND id_usr={1};",this.Id, id);
                    context.LogInfo(this, sqlDelete);                
                    context.Execute(sqlDelete);
                }
            }
        }

        public List<User> GetUsers(){
            var users = new List<User>();
            if(this.OwnerId != 0) users.Add(User.FromId(this.context, this.OwnerId));
            
            var sql = string.Format("SELECT id_usr FROM asd_perm WHERE id_asd={0};", this.Id);
            var ids = context.GetQueryIntegerValues(sql);
            if(ids != null){
                foreach(var id in ids){
                    if(id != 0) users.Add(User.FromId(this.context, id));
                }
            }

            return users;
        }
    }

    [DataContract]
    public class ASDBasic {

        [DataMember]
        public int Id { get; set; }

        [DataMember]
        public string Identifier {get; set;}
        
        [DataMember]
        public UrfContact[] Contacts { get; set; }

        public ASDBasic(){}
    }
}


