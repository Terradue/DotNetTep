﻿using System;
using OpenGis.Wps;
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
                            provider.UpdateProcessOfferings(true, user, true);
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

        /// <summary>
        /// Refreshs the wpjob status.
        /// </summary>
        /// <param name="context">Context.</param>
        public static void RefreshWpsjobStatus(IfyContext context) {
            var jobs = new EntityList<WpsJob>(context);
            jobs.SetFilter("Status",(int)WpsJobStatus.ACCEPTED + "," + (int)WpsJobStatus.NONE + "," + (int)WpsJobStatus.PAUSED + "," + (int)WpsJobStatus.STARTED);
            var jobsPoolSize = context.GetConfigIntegerValue("action-jobPoolSize");
            jobs.ItemsPerPage = jobsPoolSize;
            jobs.Load();
            context.WriteInfo(string.Format("RefreshWpjobStatus -- found {0} jobs (total result = {1})", jobs.Count, jobs.TotalResults));
            foreach(var job in jobs){
                string status = job.StringStatus;
                try {
                    var jobresponse = job.GetStatusLocationContent();
                    if (jobresponse is ExecuteResponse) {
                        var execResponse = jobresponse as ExecuteResponse;
                        job.UpdateStatusFromExecuteResponse(execResponse);
                        job.Store();
                    } else {
                        if(jobresponse is ExceptionReport){
                            job.Status = WpsJobStatus.FAILED;
                            job.Store();
                        } else if (DateTime.UtcNow.AddMonths(-1) > job.CreatedTime) {//if job is older than a month and makes an exception, we set as failed
                            job.Status = WpsJobStatus.FAILED;
                            job.Store();
                        }
                    }
                }catch(WpsProxyException e){
                    context.WriteError(string.Format("RefreshWpjobStatus -- '{0}'", e.Message));
                    if (DateTime.UtcNow.AddMonths(-1) > job.CreatedTime) {//if job is older than a month and makes an exception, we set as failed
                        job.Status = WpsJobStatus.FAILED;
                        job.Store();
                    } else {
                    }
                }catch(Exception e){
                    context.WriteError(string.Format("RefreshWpjobStatus -- '{0}'", e.Message));
                }
                context.WriteInfo(string.Format("RefreshWpjobStatus -- job '{0}' -- status = {1} -> {2}", job.Identifier, status, job.StringStatus));
            }
        }

        /// <summary>
        /// Refreshs the wpjob result nb.
        /// </summary>
        /// <param name="context">Context.</param>
        public static void RefreshWpsjobResultNb(IfyContext context) {
            var jobs = new EntityList<WpsJob>(context);
            jobs.SetFilter("NbResults", "-1");
            var jobsPoolSize = context.GetConfigIntegerValue("action-jobPoolSize");
            jobs.ItemsPerPage = jobsPoolSize;
            jobs.Load();
            context.WriteInfo(string.Format("RefreshWpjobResultNb -- found {0} jobs (total result = {1})", jobs.Count, jobs.TotalResults));
            foreach (var job in jobs) {
                bool noset = false, forced = false;
                try{
                    job.UpdateResultCount();
                } catch (WpsProxyException e) {
                    if (DateTime.UtcNow.AddMonths(-1) > job.CreatedTime) {//if job is older than a month and makes an exception, we set result to 0
                        job.NbResults = 0;
                        job.Store();
                        forced = true;
                    } else {
                        noset = true;
                        context.WriteError(string.Format("RefreshWpjobResultNb -- '{0}'", e.Message + "-" + e.StackTrace));
                    }
                } catch (Exception e) {
                    context.WriteError(string.Format("RefreshWpjobResultNb -- '{0}'", e.Message + "-" + e.StackTrace));
                }
                context.WriteInfo(string.Format("RefreshWpjobResultNb -- job '{0}' -- status = {1} -> {2} results{3}", job.Identifier, job.StringStatus, noset ? "no" : job.NbResults + "", forced ? " (forced)" : ""));
            }
             jobs = new EntityList<WpsJob>(context);
            jobs.SetFilter("Status", (int)WpsJobStatus.COORDINATOR + "");
            jobs.Load();
            context.WriteInfo(string.Format("RefreshWpjobResultNb -- found {0} coordinators", jobs.Count));
            foreach (var job in jobs) {
                try{
                    job.UpdateResultCount();
                } catch (Exception e) {
                    context.WriteError(string.Format("RefreshWpjobResultNb -- '{0}'", e.Message));
                }
                context.WriteInfo(string.Format("RefreshWpjobResultNb -- job '{0}' -- status = {1} -> {2} results", job.Identifier, job.StringStatus, job.NbResults));
            }
        }

    }
}
