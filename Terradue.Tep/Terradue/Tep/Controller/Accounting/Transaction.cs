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
        [EntityDataField("kind")]
        public TransactionKind Kind { get; set; }

        public Transaction(IfyContext context) : base(context) {
        }

        /// <summary>
        /// Gets the human readable reference.
        /// </summary>
        /// <returns>The human readable reference.</returns>
        public string GetHumanReadableReference() {
            if (Entity != null && EntityId != 0) {
                if (Entity is WpsJob) {
                    var job = WpsJob.FromId(context, EntityId);
                    return string.Format("Wpsjob '{0}'", job.Name);
                } else if (Entity is DataPackage) { 
                    return string.Format("Datapackage '{0}'", Entity.Name);
                }
            } 
            return Identifier;
        }

        /// <summary>
        /// Gets the transaction balance.
        /// </summary>
        /// <returns>The transaction balance.</returns>
        public double GetTransactionBalance() {
            switch (Kind) { 
                case TransactionKind.Credit:
                return Balance;
                case TransactionKind.ActiveDeposit:
                case TransactionKind.Debit:
                return -Balance;
                case TransactionKind.ResolvedDeposit:
                default:
                return 0;
            }
        }

        /// <summary>
        /// Is a deposit.
        /// </summary>
        /// <returns><c>true</c>, if transaction is a deposit, <c>false</c> otherwise.</returns>
        public bool IsDeposit() {
            return Kind == TransactionKind.ActiveDeposit || Kind == TransactionKind.ResolvedDeposit || Kind == TransactionKind.ClosedDeposit;
        }

        public override AtomItem ToAtomItem(NameValueCollection parameters) {
            AtomItem result = new AtomItem();
            if (Identifier != null) {
                var entityType = EntityType.GetEntityType(typeof(Transaction));
                Uri id = new Uri(context.BaseUrl + "/" + entityType.Keyword + "/search?id=" + this.Identifier);
                result.Id = id.ToString();
                result.Title = new TextSyndicationContent(GetHumanReadableReference());
                result.ElementExtensions.Add("identifier", "http://purl.org/dc/elements/1.1/", this.Identifier);
            } else { 
                result.Title = new TextSyndicationContent("n/a");
            }
            result.ElementExtensions.Add("balance", "http://purl.org/dc/elements/1.1/", this.Balance);
            result.PublishDate = new DateTimeOffset(this.LogTime);
            result.Categories.Add(new SyndicationCategory("kind", null, (int)Kind + ""));

            return result;
        }
    }

    public enum TransactionKind { 
        Debit = 0, //the transaction is a debit (decrease the balance)
        Credit = 1, //the transaction is a credit (increase the balance)
        ActiveDeposit = 2, //the transaction is a deposit and is not resolved (we take the deposit into account in the balance)
        ResolvedDeposit = 3, //the transaction is a deposit ans is resolved (we dont take the deposit into account anymore)
        ClosedDeposit = 4 //the transaction is a deposit ans is resolved (we dont take the deposit into account anymore)            
    }

    /*****************************************************************************/
    /*****************************************************************************/
    /*****************************************************************************/

    public class AggregatedTransaction : Transaction, IComparable<AggregatedTransaction>  {

        public double RealCost { get; set; }
        public double Deposit { get; set; }

        public AggregatedTransaction(IfyContext context) : base(context) { }

        public AggregatedTransaction(IfyContext context, Transaction transaction) : base(context){
            this.EntityId = transaction.EntityId;
            this.ProviderId = transaction.ProviderId;
            this.EntityTypeId = transaction.EntityTypeId;
            this.Identifier = transaction.Identifier;
            this.LogTime = transaction.LogTime;
            this.Balance = transaction.Balance;
            this.Kind = transaction.Kind;
            if(Kind == TransactionKind.Debit) this.RealCost = transaction.Balance;
        }

        public void AggregateTransaction(Transaction transaction) { 
            if (transaction.Kind == TransactionKind.Debit) {
                Balance += transaction.Balance;
            }
        }

        #region IComparable implementation

        public int CompareTo(AggregatedTransaction other) {
            if (other == null)
                return 1;
            else
                return this.LogTime.CompareTo(other.LogTime);
        }

        #endregion
    }

    /*****************************************************************************/
    /*****************************************************************************/
    /*****************************************************************************/


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

    /*****************************************************************************/
    /*****************************************************************************/
    /*****************************************************************************/

    [DataContract]
    public class ETTimestamp {
        [DataMember]
        public string from { get; set; }
        [DataMember]
        public string to { get; set; }
    }

    [DataContract]
    public class ETRange {
        [DataMember(Name = "@timestamp")]
        public ETTimestamp timestamp { get; set; }
    }

    [DataContract]
    public class ETTerm {
        [DataMember(Name = "account.userName")]
        public string accountUserName { get; set; }
    }

    [DataContract]
    public class ETMust {
        [DataMember]
        public ETRange range { get; set; }
        [DataMember]
        public ETTerm term { get; set; }
    }

    [DataContract]
    public class ETTerms {
        [DataMember]
        public string field { get; set; }
    }

    //public class ETSum {
    //    public string field { get; set; }
    //}

    [DataContract]
    public class ETTotal {
        [DataMember]
        public ETTerms sum { get; set; }
    }

    [DataContract]
    public class ETAggs4 {
        [DataMember]
        public ETTotal total { get; set; }
    }

    [DataContract]
    public class ETQuantities {
        [DataMember]
        public ETTerms terms { get; set; }
        [DataMember]
        public ETAggs4 aggs { get; set; }
    }

    [DataContract]
    public class ETAggs3 {
        [DataMember]
        public ETQuantities quantities { get; set; }
    }

    [DataContract]
    public class ETAccountRef {
        [DataMember]
        public ETTerms terms { get; set; }
        [DataMember]
        public ETAggs3 aggs { get; set; }
    }

    [DataContract]
    public class ETAggs2 {
        [DataMember]
        public ETAccountRef account_ref { get; set; }
    }

    [DataContract]
    public class ETUser {
        [DataMember]
        public ETTerms terms { get; set; }
        [DataMember]
        public ETAggs2 aggs { get; set; }
    }

    [DataContract]
    public class ETAggs {
        [DataMember]
        public ETUser user { get; set; }
    }

    [DataContract]
    public class ElasticTransactionSearchRequest {
        [DataMember]
        public int size { get; set; }
        [DataMember]
        public ETQuery query { get; set; }
        [DataMember]
        public ETAggs aggs { get; set; }
    }

    [DataContract]
    public class ETQuery {
        [DataMember]
        public ETConstantScore constant_score { get; set; }
    }

    [DataContract]
    public class ETConstantScore {
        [DataMember]
        public ETFilter filter { get; set; }
    }

    [DataContract]
    public class ETBool {
        [DataMember]
        public List<ETMust> must { get; set; }
    }

    [DataContract]
    public class ETFilter {
        [DataMember(Name = "bool")]
        public ETBool etbool { get; set; }
    }

    /*****************************************************************************/
    /*****************************************************************************/
    /*****************************************************************************/

    public class ETShards {
        public int total { get; set; }
        public int successful { get; set; }
        public int failed { get; set; }
    }

    public class ETHits {
        public int total { get; set; }
        public int max_score { get; set; }
        public List<object> hits { get; set; }
    }

    public class ETTotal2 {
        public int value { get; set; }
    }

    public class ETBucket3 {
        public string key { get; set; }
        public int doc_count { get; set; }
        public ETTotal2 total { get; set; }
    }

    public class ETQuantities2 {
        public int doc_count_error_upper_bound { get; set; }
        public int sum_other_doc_count { get; set; }
        public List<ETBucket3> buckets { get; set; }
    }

    public class ETBucket2 {
        public string key { get; set; }
        public int doc_count { get; set; }
        public ETQuantities2 quantities { get; set; }
    }

    public class ETAccountRef2 {
        public int doc_count_error_upper_bound { get; set; }
        public int sum_other_doc_count { get; set; }
        public List<ETBucket2> buckets { get; set; }
    }

    public class ETBucket {
        public string key { get; set; }
        public int doc_count { get; set; }
        public ETAccountRef2 account_ref { get; set; }
    }

    public class ETUser2 {
        public int doc_count_error_upper_bound { get; set; }
        public int sum_other_doc_count { get; set; }
        public List<ETBucket> buckets { get; set; }
    }

    public class ETAggregations {
        public ETUser2 user { get; set; }
    }

    public class ElasticTransactionSearchResponse {
        public int took { get; set; }
        public bool timed_out { get; set; }
        public ETShards _shards { get; set; }
        public ETHits hits { get; set; }
        public ETAggregations aggregations { get; set; }
    }
}
