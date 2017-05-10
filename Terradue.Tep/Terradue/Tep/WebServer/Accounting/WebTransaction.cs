using System;
using System.Collections.Generic;
using ServiceStack.Common.Web;
using ServiceStack.ServiceHost;

namespace Terradue.Tep.WebServer {

    [Route("/transaction", "GET", Summary = "GET transactions as opensearch", Notes = "")]
    public class TransactionsGetRequestTep : IReturn<HttpResult> { 
        [ApiMember(Name = "user", Description = "user Identifier", ParameterType = "path", DataType = "string", IsRequired = false)]
        public string User { get; set; }
    }

    [Route("/transaction/search", "GET", Summary = "GET transactions as opensearch", Notes = "")]
    public class TransactionsSearchRequestTep : IReturn<HttpResult>{}

    [Route("/transaction/description", "GET", Summary = "GET transactions as opensearch", Notes = "")]
    public class TransactionsDescriptionRequestTep : IReturn<HttpResult>{}

    [Route("/transaction/user", "POST", Summary = "POST the current user sso", Notes = "User is the current user")]
    public class UserAddTransactionRequestTep : WebTransaction, IReturn<WebUserTep> {
        [ApiMember(Name = "identifier", Description = "user Identifier", ParameterType = "path", DataType = "string", IsRequired = false)]
        public string Identifier { get; set; }
    }

    public class WebTransaction {

        [ApiMember(Name="Reference", Description = "Transaction reference", ParameterType = "path", DataType = "string", IsRequired = false)]
        public string Reference { get; set; }

        [ApiMember(Name="HumanReadableReference", Description = "Transaction reference (Human readable)", ParameterType = "path", DataType = "string", IsRequired = false)]
        public string HumanReadableReference { get; set; }

        [ApiMember(Name="LogTime", Description = "Transaction log time", ParameterType = "path", DataType = "DateTime", IsRequired = false)]
        public DateTime LogTime { get; set; }

        [ApiMember(Name = "Balance", Description = "Transaction Balance", ParameterType = "path", DataType = "double", IsRequired = false)]
        public double Balance { get; set; }

        [ApiMember(Name="Deposit", Description = "Transaction deposit", ParameterType = "path", DataType = "double", IsRequired = false)]
        public double Deposit { get; set; }

        [ApiMember(Name = "RealCost", Description = "Transaction real cost", ParameterType = "path", DataType = "double", IsRequired = false)]
        public double RealCost { get; set; }

        [ApiMember(Name = "Kind", Description = "Transaction kind", ParameterType = "path", DataType = "int", IsRequired = false)]
        public int Kind { get; set; }

        public WebTransaction() {}

        public WebTransaction(AggregatedTransaction entity){
            this.Reference = entity.Identifier;
            this.HumanReadableReference = entity.GetHumanReadableReference();
            this.LogTime = entity.LogTime;
            this.Balance = entity.Balance;
            this.Kind = (int)entity.Kind;

            switch (entity.Kind) {
                case TransactionKind.Debit:
                RealCost = entity.RealCost;
                break;
                case TransactionKind.ActiveDeposit:
                case TransactionKind.ResolvedDeposit:
                Deposit = entity.Balance;
                RealCost = entity.RealCost;RealCost = entity.RealCost;
                break;
                default:
                break;
            }

        }

    }
}


