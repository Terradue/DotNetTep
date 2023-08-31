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

        public static void CreateTransaction(IfyContext context, int usrid, ASD asd, WpsJob job, double cost) {
            var transaction = new Transaction(context);
            transaction.OwnerId = usrid;
            transaction.Kind = TransactionKind.Debit;
            transaction.Entity = job;
            transaction.ProviderId = asd.Id;
            transaction.Name = job.Name;
            transaction.Balance = cost;
            transaction.Store();
        }
    }
}
