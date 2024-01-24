using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using OpenGis.Wps;
using ServiceStack.Text;
using Terradue.Portal;

namespace Terradue.Tep {
    public class ASDTransactionFactory {
        private IfyContext context;

        public ASDTransactionFactory(IfyContext context) {
            this.context = context;
        }

        /// <summary>
        /// Gets the user transactions.
        /// </summary>
        /// <returns>The user transactions.</returns>
        /// <param name="usrid">Usrid.</param>
        /// <param name="withref">If set to <c>true</c> with referenced items.</param>
        /// <param name="withnoref">If set to <c>true</c> with unreferenced items.</param>
        public List<Transaction> GetUserTransactions(int usrid) {
            EntityList<Transaction> transactions = new EntityList<Transaction>(context);            
            transactions.SetFilter("OwnerId", usrid + "");
            
            transactions.Load();
            return transactions.GetItemsAsList();
        }

        public List<Transaction> GetUserTransactionsForASD(int usrid, int asdid) {
            EntityList<Transaction> transactions = new EntityList<Transaction>(context);            
            transactions.SetFilter("OwnerId", usrid + "");
            transactions.SetFilter("ProviderId", asdid + "");
            
            transactions.Load();
            return transactions.GetItemsAsList();
        }

        public List<Transaction> GetUserJobTransactionsForASD(int usrid, int asdid) {
            EntityList<Transaction> transactions = new EntityList<Transaction>(context);            
            transactions.SetFilter("OwnerId", usrid + "");
            transactions.SetFilter("ProviderId", asdid + "");
            transactions.SetFilter("EntityTypeId", EntityType.GetEntityType(new WpsJob(context).GetType()).Id);
            transactions.Load();
            return transactions.GetItemsAsList();
        }

        public List<Transaction> GetJobTransactionsForASD(int asdid) {
            EntityList<Transaction> transactions = new EntityList<Transaction>(context);            
            transactions.SetFilter("ProviderId", asdid + "");
            transactions.SetFilter("EntityTypeId", EntityType.GetEntityType(new WpsJob(context).GetType()).Id);
            transactions.Load();
            return transactions.GetItemsAsList();
        }

        public List<ASDTransaction> GetServiceTransactionsForASD(int asdid) {
            List<Transaction> transactions = GetJobTransactionsForASD(asdid);
            var result = new List<ASDTransaction>();

            foreach(var transaction in transactions){
                var job = WpsJob.FromIdentifier(context, transaction.Identifier);
                bool exists = false;
                foreach(var res in result){
                    if(res.Name == job.WpsName){
                        res.Balance += transaction.Balance;
                        if(!res.Owners.Contains(job.Owner.Username)) res.Owners.Add(job.Owner.Username);
                        exists = true;
                        continue;
                    }
                }
                if(!exists){
                    var t = new ASDTransaction()
                    {
                        Identifier = job.ProcessId,
                        Name = job.WpsName,
                        Balance = transaction.Balance,
                        Owners = new List<string>{job.Owner.Username}
                    };
                    result.Add(t);
                }
            }
            return result;
        }

        public static void CreateTransaction(IfyContext context, int usrid, ASD asd, WpsJob job, double cost) {
            var transaction = new Transaction(context)
            {
                Identifier = job.Identifier,
                OwnerId = usrid,
                Kind = TransactionKind.Debit,
                Entity = job,
                ProviderId = asd.Id,
                Name = job.Name,
                Balance = cost
            };
            if (transaction.EntityId == 0) transaction.EntityId = job.Id;
            transaction.Store();
        }
    }

    public class ASDTransaction {
        public List<string> Owners { get; set; }
        public int AsdId { get; set; }
        public string Identifier { get; set; }
        public string Name { get; set; }
        public double Balance { get; set; }
        public DateTime LogTime { get; set; }

        public ASDTransaction(){}
        public ASDTransaction(Transaction transaction){
            // try{
            //     this.Owner = context.GetQueryStringValue(string.Format("SELECT username FROM usr WHERE id={0};",transaction.OwnerId));
            // }catch(Exception e){}
            this.Identifier = transaction.Identifier;
            // try{
            //     this.Name = context.GetQueryStringValue(string.Format("SELECT name FROM wpsjob WHERE identifier='{0}';",transaction.Identifier));
            // }catch(Exception e){}
            this.AsdId = transaction.ProviderId;
            this.Balance = transaction.Balance;
            this.LogTime = transaction.LogTime;
        }
    }
}
