using System;
using System.Collections.Generic;
using System.Web;
using ServiceStack.Common.Web;
using ServiceStack.ServiceHost;
using Terradue.OpenSearch;
using Terradue.OpenSearch.Result;
using Terradue.OpenSearch.Schema;
using Terradue.Portal;

namespace Terradue.Tep.WebServer.Services {
    
    [Api("Tep Terradue webserver")]
    [Restrict(EndpointAttributes.InSecure | EndpointAttributes.InternalNetworkAccess | EndpointAttributes.Json,
              EndpointAttributes.Secure | EndpointAttributes.External | EndpointAttributes.Json)]
    public class TransactionServiceTep : ServiceStack.ServiceInterface.Service {
        
        public object Post(UserAddTransactionRequestTep request) {
            var context = TepWebContext.GetWebContext(PagePrivileges.AdminOnly);
            WebUserTep result;
            try {
                context.Open();
                context.LogInfo(this, string.Format("/transaction/user POST Identifier='{0}', Balance='{1}'", request.Identifier, request.Balance));

                UserTep user = UserTep.FromIdentifier(context, request.Identifier);
                user.AddAccountingTransaction(request.Balance);

                result = new WebUserTep(context, user);
                context.Close();
            } catch (Exception e) {
                context.LogError(this, e.Message);
                context.Close();
                throw e;
            }
            return result;
        }

        public object Get(TransactionsSearchRequestTep request) {
            var context = TepWebContext.GetWebContext(PagePrivileges.UserView);
            IOpenSearchResultCollection result;
            try {
                context.Open();
                context.LogInfo(this, string.Format("/transaction/search GET"));

                // Load the complete request
                var httpRequest = HttpContext.Current.Request;

                EntityList<Transaction> transactions = new EntityList<Transaction>(context);
                if (context.AccessLevel != EntityAccessLevel.Administrator || httpRequest.QueryString["author"] == null) {
                    //Only admin can see others transactions
                    transactions.SetFilter("OwnerId", context.UserId + "");
                } else {
                    var user = UserTep.FromIdentifier(context, httpRequest.QueryString["author"]);
                    transactions.SetFilter("OwnerId", user.Id + "");
                }
                transactions.AddSort("LogTime", SortDirection.Descending);

                var ose = MasterCatalogue.OpenSearchEngine;

                Type responseType = OpenSearchFactory.ResolveTypeFromRequest(httpRequest, ose);
                result = ose.Query(transactions, httpRequest.QueryString, responseType);

                OpenSearchFactory.ReplaceOpenSearchDescriptionLinks(transactions, result);

                context.Close();
            } catch (Exception e) {
                context.LogError(this, e.Message);
                context.Close();
                throw e;
            }
            return new HttpResult(result.SerializeToString(), result.ContentType);
        }

        public object Get(TransactionsDescriptionRequestTep request) {
            var context = TepWebContext.GetWebContext(PagePrivileges.UserView);
            try {
                context.Open();
                context.LogInfo(this, string.Format("/transaction/description GET"));

                EntityList<Transaction> transactions = new EntityList<Transaction>(context);
                transactions.OpenSearchEngine = MasterCatalogue.OpenSearchEngine;

                var osd = transactions.GetOpenSearchDescription();

                context.Close();

                return new HttpResult(osd, "application/opensearchdescription+xml");
            } catch (Exception e) {
                context.LogError(this, e.Message);
                context.Close();
                throw e;
            }
        }
               
    }
}
