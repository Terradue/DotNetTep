using System;
using Terradue.Portal;

namespace Terradue.Tep {
    public class Actions {

        /// <summary>
        /// Updates the wps providers.
        /// </summary>
        /// <param name="context">Context.</param>
        public static void UpdateWpsProviders(IfyContext context) {
            try {
                var wpsProviders = new EntityList<WpsProvider>(context);
                wpsProviders.Load();

                foreach (var provider in wpsProviders.GetItemsAsList()) {
                    if (provider.AutoSync) {
                        try {
                            User user = null;
                            if (provider.DomainId != 0) {
                                var role = Role.FromIdentifier(context, RoleTep.OWNER);
                                var usrs = role.GetUsers(provider.DomainId);
                                if (usrs != null && usrs.Length > 0) {
                                    user = User.FromId(context, usrs[0]);//we take only the first owner
                                }
                            }
                            provider.CanCache = false;
                            provider.UpdateProcessOfferings(true, user);
                            context.WriteInfo(string.Format("UpdateWpsProviders -- Auto synchro done for WPS {0}", provider.Name));
                        } catch (Exception e) {
                            context.WriteError(string.Format("UpdateWpsProviders -- {0} - {1}", e.Message, e.StackTrace));
                        }
                    }   
                }
            } catch (Exception e) {
                context.WriteError(string.Format("UpdateWpsProviders -- {0} - {1}", e.Message, e.StackTrace));
            }
        }

        /// <summary>
        /// Cleans the deposit without transaction for more than x days
        /// </summary>
        /// <param name="context">Context.</param>
        public static void CleanDeposit(IfyContext context) {
            var lifeTimeDays = context.GetConfigDoubleValue("accounting-deposit-maxDays");

            var factory = new TransactionFactory(context);

            //get all deposits
            var deposits = factory.GetDepositTransactions();

            foreach (var deposit in deposits) {
                try {
                    if (deposit.Kind != TransactionKind.ClosedDeposit && deposit.LogTime.AddDays(lifeTimeDays) < DateTime.Now) { //the deposit is created for more than lifeTimeDays days and is not yet closed
                        var transactions = factory.GetTransactionsByReference(deposit.Identifier);
                        if (transactions.Count == 1) { //means there is only the deposit as transaction
                            deposit.Kind = TransactionKind.ClosedDeposit;
                            deposit.Store();
                            context.WriteInfo(string.Format("CleanDeposit -- Deposit '{0}' closed", deposit.Identifier));
                        }
                    }
                } catch (Exception e) {
                    context.WriteError(string.Format("CleanDeposit -- {0} - {1}", e.Message, e.StackTrace));
                }
            }
        }
    }
}
