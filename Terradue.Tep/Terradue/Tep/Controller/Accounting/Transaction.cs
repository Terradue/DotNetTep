using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Runtime.Serialization;
using ServiceStack.Text;
using Terradue.OpenSearch.Result;
using Terradue.Portal;
using Terradue.Portal.OpenSearch;
using Terradue.ServiceModel.Syndication;

namespace Terradue.Tep {
    [EntityTable("transaction", EntityTableConfiguration.Custom, IdentifierField = "reference", HasOwnerReference = true)]
    public class Transaction : EntitySearchable {

        /// <summary>
        /// The entity.
        /// </summary>
        private Entity entity;
        public Entity Entity {
            get {
                if (entity == null) {
                    var etype = EntityType.GetEntityTypeFromId(EntityTypeId);
                    entity = etype.GetEntityInstanceFromId(context, this.EntityId);
                }
                return entity;
            }
            set {
                entity = value;
                this.EntityId = entity.Id;
                this.EntityTypeId = EntityType.GetEntityType(entity.GetType()).Id;
            }
        }

        /// <summary>
        /// Gets or sets the entity identifier.
        /// </summary>
        /// <value>The entity identifier.</value>
        [EntityDataField("id_entity")]
        public int EntityId { get; set; }

        /// <summary>
        /// Gets or sets the provider identifier.
        /// </summary>
        /// <value>The provider identifier.</value>
        [EntityDataField("id_provider")]
        public int ProviderId { get; set; }

        /// <summary>
        /// Gets or sets the entity type identifier.
        /// </summary>
        /// <value>The entity type identifier.</value>
        [EntityDataField("id_type")]
        public int EntityTypeId { get; set; }

        /// <summary>
        /// Gets or sets the timestamp.
        /// </summary>
        /// <value>The timestamp.</value>
        [EntityDataField("log_time")]
        public DateTime LogTime { get; set; }

        /// <summary>
        /// Gets or sets the balance.
        /// </summary>
        /// <value>The balance.</value>
        [EntityDataField("balance")]
        public double Balance { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="T:Terradue.Tep.Transaction"/> is deposit.
        /// </summary>
        /// <value><c>true</c> if deposit; otherwise, <c>false</c>.</value>
        [EntityDataField("deposit")]
        public bool Deposit { get; set; }

        public Transaction(IfyContext context) : base(context) {
        }

        /// <summary>
        /// Gets the human readable reference.
        /// </summary>
        /// <returns>The human readable reference.</returns>
        public string GetHumanReadableReference() {
            if (Entity != null) {
                if (Entity is WpsJob) {
                    var job = WpsJob.FromId(context, EntityId);
                    return string.Format("Wpsjob '{0}'", job.Name);
                } else if (Entity is DataPackage) { 
                    return string.Format("Datapackage '{0}'", Entity.Name);
                }
            } 
            return Identifier;
        }

        public override AtomItem ToAtomItem(NameValueCollection parameters) {
            AtomItem result = new AtomItem();
            if (Identifier != null) {
                var entityType = EntityType.GetEntityType(typeof(Transaction));
                Uri id = new Uri(context.BaseUrl + "/" + entityType.Keyword + "/search?id=" + this.Identifier);
                result.Id = id.ToString();
                result.Title = new TextSyndicationContent(GetHumanReadableReference());
                result.ElementExtensions.Add("identifier", "http://purl.org/dc/elements/1.1/", this.Identifier);
            }
            result.ElementExtensions.Add("balance", "http://purl.org/dc/elements/1.1/", this.Balance);
            result.PublishDate = new DateTimeOffset(this.LogTime);
            result.Categories.Add(new SyndicationCategory("deposit", null, Deposit.ToString()));

            return result;
        }
    }

    [DataContract]
    public class T2AccountingAccount {
        [DataMember]
        public string platform { get; set; }
        [DataMember]
        public string username { get; set; }
        [DataMember(Name="ref")]
        public string reference { get; set; }
    }

    [DataContract]
    public class T2AccountingCompound {
        [DataMember]
        public string id { get; set; }
        [DataMember]
        public string name { get; set; }
        [DataMember]
        public string type { get; set; }
        [DataMember]
        public object any { get; set; }
    }

    [DataContract]
    public class T2AccountingQuantity {
        [DataMember]
        public string id { get; set; }
        [DataMember]
        public long value { get; set; }
    }

    [DataContract]
    public class T2AccountingLocation {
        [DataMember]
        public List<double> coordinates { get; set; }
    }

    [DataContract]
    public class T2Accounting {
        [DataMember]
        public string id { get; set; }
        [DataMember]
        public T2AccountingAccount account { get; set; }
        [DataMember]
        public T2AccountingCompound compound { get; set; }
        [DataMember]
        public List<T2AccountingQuantity> quantity { get; set; }
        [DataMember]
        public string hostname { get; set; }
        [DataMember]
        public DateTime timestamp { get; set; }
        [DataMember]
        public string status { get; set; }
        [DataMember]
        public T2AccountingLocation location { get; set; }
    }

}
