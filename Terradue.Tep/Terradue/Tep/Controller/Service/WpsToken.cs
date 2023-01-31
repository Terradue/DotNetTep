using System;
using Terradue.Portal;

namespace Terradue.Tep
{
    [EntityTable("service_token", EntityTableConfiguration.Custom, HasOwnerReference = true, IdentifierField = null)]
    /// <summary>
    /// A Wps token is used to process one input of a wps Job
    /// </summary>
    /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
    public class WpsToken : Entity
    {

        // Wps service id
        [EntityDataField("id_service")]
        public int ServiceId { get; set; }

        // [EntityDataField("id_usr")]
        // public int UserId { get; set; }

        private User User { 
            get {
                if(this.OwnerId != 0) return User.FromId(context, this.OwnerId);
                return null;
            }
        }
        public string Username {
            get {
                if(this.User != null) return this.User.Username;
                return null;
            }
        }
        private Group Group { 
            get {
                if(this.GroupId != 0) return Group.FromId(context, this.GroupId);
                return null;
            }
        }
        public string Groupname {
            get {
                if(this.Group != null) return this.Group.Name;
                return null;
            }
        }


        // Group id
        [EntityDataField("id_grp")]
        public int GroupId { get; set; }

        [EntityDataField("end_time")]
        public DateTime EndTime { get; set; }

        [EntityDataField("nb_inputs")]
        public int NbInputs { get; set; }

        [EntityDataField("nb_max")]
        public int NbMax { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Terradue.Tep.WpsToken"/> class.
        /// </summary>
        /// <param name="context">Context.</param>
        public WpsToken(IfyContext context) : base(context) { }

        /// <summary>
        /// Froms the identifier.
        /// </summary>
        /// <returns>The identifier.</returns>
        /// <param name="context">Context.</param>
        /// <param name="id">Identifier.</param>
        public static WpsToken FromId(IfyContext context, int id)
        {
            WpsToken result = new WpsToken(context);
            result.Id = id;
            result.Load();
            return result;
        }

        public static WpsToken FromUserAndService(IfyContext context, int usrid, int wpsid)
        {
            WpsToken result = new WpsToken(context);
            result.OwnerId = usrid;
            result.ServiceId = wpsid;
            result.Load();
            return result;
        }

        public static WpsToken FromGroupAndService(IfyContext context, int grpid, int wpsid)
        {
            WpsToken result = new WpsToken(context);
            result.GroupId = grpid;
            result.ServiceId = wpsid;
            result.Load();
            return result;
        }

        public override string GetIdentifyingConditionSql()
        {
            if (Id == 0 && UserId != 0 && ServiceId != 0) return String.Format("t.id_usr={0} AND t.id_service={1}", UserId, ServiceId);
            if (Id == 0 && GroupId != 0 && ServiceId != 0) return String.Format("t.id_grp={0} AND t.id_service={1}", GroupId, ServiceId);
            return null;
        }
    }
}


