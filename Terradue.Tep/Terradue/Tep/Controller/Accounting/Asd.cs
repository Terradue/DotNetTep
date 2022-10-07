using System;
using Terradue.Portal;
using Terradue.Portal.Urf;

namespace Terradue.Tep
{
    [EntityTable("asd", EntityTableConfiguration.Custom, HasOwnerReference = true, IdentifierField = "identifier", NameField = "name", HasPermissionManagement = true)]
    
    public class ASD : Entity
    {
        
        [EntityDataField("status")]
        public UrfStatus Status { get; set; }

        [EntityDataField("credit_total")]
        public int CreditTotal { get; set; }

        [EntityDataField("credit_used")]
        public int CreditUsed { get; set; }

        [EntityDataField("created_time")]
        public DateTime CreatedTime { get; set; }

        [EntityDataField("end_time")]
        public DateTime EndTime { get; set; }

        private User User { 
            get {
                if(this.OwnerId != 0) return User.FromId(context, this.OwnerId);
                return null;
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
    }
}


