using System;
using System.Collections.Generic;
using ServiceStack.Common.Web;
using ServiceStack.ServiceHost;
using Terradue.Portal;
using Terradue.WebService.Model;

namespace Terradue.Tep.WebServer {

    [Route("/transaction/search", "GET", Summary = "GET transactions as opensearch", Notes = "")]
    public class TransactionsSearchRequestTep : IReturn<List<WebTransaction>>{}

    [Route("/transaction/description", "GET", Summary = "GET transactions as opensearch", Notes = "")]
    public class TransactionsDescriptionRequestTep : IReturn<List<WebTransaction>>{}

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

        [ApiMember(Name="Balance", Description = "Transaction balance", ParameterType = "path", DataType = "double", IsRequired = false)]
        public double Balance { get; set; }

        public WebTransaction() {}

        public WebTransaction(Transaction entity){
            this.Reference = entity.Identifier;
            this.HumanReadableReference = entity.GetHumanReadableReference();
            this.LogTime = entity.LogTime;
            this.Balance = entity.Balance;
        }

    }
}


