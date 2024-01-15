using System;
using System.Collections.Generic;
using System.Linq;
using OpenGis.Wps;
using Terradue.Portal;

namespace Terradue.Tep {
    public class Actions {

        /**********************************************************************/
        /**********************************************************************/

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
                            provider.UpdateProcessOfferings(true, user, null, true);
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

        /**********************************************************************/
        /**********************************************************************/

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

        /**********************************************************************/
        /**********************************************************************/

        /// <summary>
        /// Refreshs the wpjob status.
        /// </summary>
        /// <param name="context">Context.</param>
        public static void RefreshWpsjobStatus(IfyContext context) {
            var jobs = new EntityList<WpsJob>(context);
            jobs.SetFilter("Status",(int)WpsJobStatus.ACCEPTED + "," + (int)WpsJobStatus.NONE + "," + (int)WpsJobStatus.PAUSED + "," + (int)WpsJobStatus.STARTED + "," + (int)WpsJobStatus.PUBLISHING);
            var jobsPoolSize = context.GetConfigIntegerValue("action-jobPoolSize");
            var maxDaysJobRefresh = context.GetConfigIntegerValue("action-maxDaysJobRefresh");
            jobs.ItemsPerPage = jobsPoolSize;
            jobs.Load();
            context.WriteInfo(string.Format("RefreshWpjobStatus --- found {0} jobs (total result = {1})", jobs.Count, jobs.TotalResults));
            foreach(var job in jobs){
                string status = job.StringStatus;
                if(job.Status != WpsJobStatus.FAILED && job.Status != WpsJobStatus.STAGED && job.Status != WpsJobStatus.COORDINATOR){
                    try {
                        var jobresponse = job.UpdateStatus();
                        if (jobresponse is ExecuteResponse) {
                            var execResponse = jobresponse as ExecuteResponse;

                            //if job status not updated and job is older than the max time allowed, we set as failed
                            if (status == job.StringStatus && DateTime.UtcNow.AddDays(-maxDaysJobRefresh) > job.CreatedTime){
                                job.Status = WpsJobStatus.FAILED;
                                job.Logs = "Job did not complete before the max allowed time";
                                EventFactory.LogWpsJob(context, job, job.Logs);
                            }
                            job.Store();
                        } else {
                            //if job is an exception or older than the max time allowed, we set as failed
                            if(jobresponse is ExceptionReport || DateTime.UtcNow.AddDays(- maxDaysJobRefresh) > job.CreatedTime) {
                                job.Status = WpsJobStatus.FAILED;
                                if (jobresponse is ExceptionReport) job.Logs = "Unknown exception";
                                else job.Logs = "Job did not complete before the max allowed time";
                                job.Store();
                                EventFactory.LogWpsJob(context, job, job.Logs);
                            }
                        }
                    }catch(WpsProxyException e){
                        context.WriteError(string.Format("RefreshWpjobStatus -- job '{1}'-- '{0}'", e.Message, job.Identifier));
                        if (DateTime.UtcNow.AddDays(- maxDaysJobRefresh) > job.CreatedTime) {//if job is older than a month and makes an exception, we set as failed
                            job.Status = WpsJobStatus.FAILED;
                            job.Logs = "Job did not complete before the max allowed time";
                            EventFactory.LogWpsJob(context, job, job.Logs);
                            job.Store();
                        } else {
                        }
                    }catch(Exception e){
                        context.WriteError(string.Format("RefreshWpjobStatus -- job '{1}'-- '{0}'", e.Message, job.Identifier));
                    }
                    context.WriteInfo(string.Format("RefreshWpjobStatus -- job '{0}' -- status = {1} -> {2}", job.Identifier, status, job.StringStatus));
                }
            }
        }

        /**********************************************************************/
        /**********************************************************************/

        /// <summary>
        /// Refreshs the wpjob result nb.
        /// </summary>
        /// <param name="context">Context.</param>
        public static void RefreshWpsjobResultNb(IfyContext context) {
            var jobs = new EntityList<WpsJob>(context);
            jobs.SetFilter("NbResults", "-1");
            var jobsPoolSize = context.GetConfigIntegerValue("action-jobPoolSize");
            var maxDaysJobRefresh = context.GetConfigIntegerValue("action-maxDaysJobRefresh");
            jobs.ItemsPerPage = jobsPoolSize;
            jobs.Load();
            context.WriteInfo(string.Format("RefreshWpjobResultNb -- found {0} jobs (total result = {1})", jobs.Count, jobs.TotalResults));
            foreach (var job in jobs) {
                bool noset = false, forced = false;
                try {
                    job.UpdateResultCount();
                } catch (Exception e) {
                    if (DateTime.UtcNow.AddDays(- maxDaysJobRefresh) > job.CreatedTime) {//if job is older than a month and makes an exception, we set result to 0
                        job.NbResults = 0;
                        job.Store();
                        forced = true;
                    } else {
                        noset = true;
                        context.WriteError(string.Format("RefreshWpjobResultNb -- job '{1}' -- '{0}'", e.Message + "-" + e.StackTrace, job.Identifier));
                    }
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
                    context.WriteError(string.Format("RefreshWpjobResultNb -- job '{1}'-- '{0}'", e.Message, job.Identifier));
                }
                context.WriteInfo(string.Format("RefreshWpjobResultNb -- job '{0}' -- status = {1} -> {2} results", job.Identifier, job.StringStatus, job.NbResults));
            }
        }

        /**********************************************************************/
        /**********************************************************************/
        
        public static void RefreshThematicAppsCache(IfyContext context) {
			var appFactory = new ThematicAppCachedFactory(context);
			appFactory.ForAgent = true;         
			appFactory.RefreshCachedApps(false, true, true);
        }

        /**********************************************************************/
        /**********************************************************************/

        public static void CreateJobMonthlyReport(IfyContext context) {
            var lastmonthdate = DateTime.Today.AddMonths(-1);
            var startdateString = lastmonthdate.ToString("yyyy-MM");                        
            var enddateString = DateTime.Today.ToString("yyyy-MM");
            var monthString = lastmonthdate.ToString("MMMM");

            context.WriteInfo(string.Format("CreateJobMonthlyReport ({0}) - {1} -> {2}", monthString, startdateString, enddateString));

            var filename = string.Format("{0}/{1}-monthly-job-report-{2}.csv", context.GetConfigValue("path.files"), context.GetConfigValue("siteNameShort"),startdateString);
            WebServer.Services.ReportServiceTep.GetJobReport(context, filename, startdateString, enddateString);
        }

        /**********************************************************************/

        /**********************************************************************/
        public static void MonthlyInactiveUserAlert(IfyContext context) {
            var lastmonthdate = DateTime.Today.AddMonths(-1);
            var date = lastmonthdate.ToString("yyyy-MM-dd");    

            context.WriteInfo(string.Format("MonthlyInactiveUserAlert"));

            //get users not actives in the last month (we only want user with level > 1)
            string sql = String.Format("SELECT email FROM usr WHERE id IN (SELECT id_usr FROM (SELECT id_usr, MAX(log_time) as lastlog FROM usrsession GROUP BY id_usr) as t1 WHERE t1.lastlog < '{0}') AND level > 1;", date);
            var emails = context.GetQueryStringValues(sql).ToList();

            var urfs = ASDFactory.GetActiveASDsFromUseremails(context, emails.ToList());            
            var list = new Dictionary<string,string>();                    
            
            foreach(var urf in urfs){
                try{
                    var asd = ASD.FromIdentifier(context, urf.Identifier);
                    foreach(var urfusr in urf.Contacts){
                        try{
                            if(emails.Contains(urfusr.ContactEmail)){
                                var usr = UserTep.FromIdentifier(context, urfusr.ContactEmail);
                                var asdrecord = string.Format("/nASD Identifier: {0} ({1}/{2} euros remaining)", asd.Identifier, asd.CreditRemaining, asd.CreditTotal);
                                if(list[urfusr.ContactEmail] != null){
                                    list[urfusr.ContactEmail] += asdrecord;
                                } else {
                                    var usrLink = string.Format("{0}/#!user/admin/{1}", context.GetConfigValue("BaseUrl"), usr.Username);                        
                                    list[urfusr.ContactEmail] = string.Format("{0} ({1})\nT2 Username: {2}\n",usr.Username, usrLink, usr.TerradueCloudUsername);
                                    list[urfusr.ContactEmail] += asdrecord;                                
                                    context.WriteInfo(string.Format("Inactive user: {0}", usr.Username));
                                }
                            }        
                        } catch(Exception e) {                            
                            context.WriteError(string.Format("Unable to load user {0} for ASD {1} -- {2}", urfusr.ContactEmail, urf.Identifier, e.Message));
                        }
                    }                
                } catch(Exception e) {                    
                    context.WriteError(string.Format("Unable to get ASD {0} -- {1}", urf.Identifier, e.Message));
                }
            }

            var subject = context.GetConfigValue("MonthlyInactiveUserAlertSubject");
            subject = subject.Replace("$(SITENAME)",context.GetConfigValue("SiteNameShort"));
            var body = context.GetConfigValue("MonthlyInactiveUserAlertBody");
            body = body.Replace("$(RECORDS)", string.Join("\n ", list));
            context.SendMail(context.GetConfigValue("SmtpUsername"), context.GetConfigValue("SmtpUsername"), subject, body);
        }
    }
}
