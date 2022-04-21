using System;
using System.Collections.Generic;
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
            jobs.SetFilter("Status",(int)WpsJobStatus.ACCEPTED + "," + (int)WpsJobStatus.NONE + "," + (int)WpsJobStatus.PAUSED + "," + (int)WpsJobStatus.STARTED);
            var jobsPoolSize = context.GetConfigIntegerValue("action-jobPoolSize");
            var maxDaysJobRefresh = context.GetConfigIntegerValue("action-maxDaysJobRefresh");
            jobs.ItemsPerPage = jobsPoolSize;
            jobs.Load();
            context.WriteInfo(string.Format("RefreshWpjobStatus -- found {0} jobs (total result = {1})", jobs.Count, jobs.TotalResults));
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

            var startdateString = DateTime.Today.AddMonths(-1).ToString("yyyy-MM");
            var enddateString = DateTime.Today.ToString("yyyy-MM");
            var monthString = DateTime.Today.AddMonths(-1).ToString("MMMM");

            context.WriteInfo(string.Format("CreateJobMonthlyReport ({0}) - {1} -> {2}", monthString, startdateString, enddateString));

            //generate new file
            var csv = new System.Text.StringBuilder();
            var csvHeader = new System.Text.StringBuilder();
            var csvBody = new System.Text.StringBuilder();            
            var header = context.GetConfigValue("agent-jobreport-headerfile");
            if(string.IsNullOrEmpty(header)) return;
            var headers = header.Split(',');
            var headerLength = headers.Length + 1;
            csvHeader.Append(header);
            csvHeader.Length --;
            csvHeader.Append(Environment.NewLine);

            string sql = String.Format("SELECT id from wpsjob WHERE created_time > '{0}' AND created_time < '{1}';", startdateString, enddateString);
            var ids = context.GetQueryIntegerValues(sql);

            context.WriteInfo(string.Format("CreateJobMonthlyReport {0} jobs found", ids.Length));
            
            List<WpsJob> jobs = new List<WpsJob>();
            foreach (int id in ids) {
                var job = WpsJob.FromId(context, id);
                var user = job.Owner;
                bool registration = false;
                if (headers != null && headerLength != 0){
                    foreach (var h in headers) {
                        switch(h){
                            //user part
                            case "usr_email":
                                csvBody.Append(user != null ? user.Email : "").Append(",");
                                break;
                            case "usr_username":
                                csvBody.Append(user != null ? user.Username : "").Append(",");                                
                                break;
                            case "usr_level":
                                var level = "";
                                if(user != null){
                                    switch(user.Level){
                                        case 0:
                                            level = "visitor";
                                        break;
                                        case 1:
                                            level = "member";
                                        break;
                                        case 2:
                                            level = "stakeholder";
                                        break;
                                        case 4:
                                            level = "administrator";
                                        break;                                    
                                    }                
                                }            
                                csvBody.Append(level).Append(",");                                
                                break;
                            case "usr_affiliation":
                                csvBody.Append(user != null ? user.Affiliation : "").Append(",");                                
                                break;
                            case "usr_creation":
                                if(user != null && !registration){ 
                                    user.LoadRegistrationInfo();
                                    registration = true;
                                }
                                csvBody.Append(user != null ? user.RegistrationDate.ToString("yyyy-MM-ddTHH:mm:ss") : "").Append(",");
                                break;
                            case "usr_login":
                                if(user != null && !registration){ 
                                    user.LoadRegistrationInfo();
                                    registration = true;
                                }
                                csvBody.Append(user != null ? user.GetLastLoginDate().ToString("yyyy-MM-ddTHH:mm:ss") : "").Append(",");
                                break;                                                        
                            //wpsjob part
                            case "job_id":
                                csvBody.Append(job.Id).Append(",");
                            break;
                            case "job_identifier":
                                csvBody.Append(job.Identifier).Append(",");
                            break;
                            case "job_status_url":
                                csvBody.Append(job.StatusLocation).Append(",");
                                break;
                            case "job_status":
                                csvBody.Append((int)job.Status).Append(",");
                                break;
                            case "job_store_url":
                                csvBody.Append(job.StatusLocation.Replace("https://recast.terradue.com/t2api/describe/","https://store.terradue.com/")).Append(",");
                                break;
                            case "job_creation":
                                csvBody.Append(job.CreatedTime.ToString("yyyy-MM-ddTHH:mm:ss")).Append(",");
                                break;
                            case "job_end":
                                csvBody.Append(job.EndTime != DateTime.MinValue ? job.EndTime.ToString("yyyy-MM-ddTHH:mm:ss") : "").Append(",");
                                break;
                            case "job_nbinput":
                                int totalDataProcessed = 0;
                                if (job.Parameters != null) {
                                    foreach (var parameter in job.Parameters) {
                                        if (!string.IsNullOrEmpty(parameter.Value) && (parameter.Value.StartsWith("http://") || parameter.Value.StartsWith("https://"))) {
                                            var url = parameter.Value;
                                            totalDataProcessed++;
                                        }
                                    }
                                }
                                csvBody.Append(totalDataProcessed).Append(",");
                                break;
                            case "job_shared":
                                csvBody.Append((job.IsPrivate()?"false":"true")).Append(",");
                                break;                            
                            case "job_wps":
                                if (!string.IsNullOrEmpty(job.WpsName))
                                    csvBody.Append(job.WpsName.Replace(",", "\\,")).Append(",");
                                else {
                                    string wpsname = "";
                                    try {
                                        WpsProcessOffering wps = CloudWpsFactory.GetWpsProcessOffering(context, job.ProcessId);
                                        wpsname = wps.Name;
                                    } catch (Exception) {
                                        wpsname = job.ProcessId;
                                    }
                                    csvBody.Append(wpsname.Replace(",", "\\,")).Append(",");
                                }
                                break;
                            case "job_duration":
                                var processingTime = job.EndTime == DateTime.MinValue || job.EndTime < job.CreatedTime ? 0 : (job.EndTime - job.CreatedTime).Minutes;
                                csvBody.Append(processingTime).Append(",");
                                break;
                            case "job_app":
                                csvBody.Append(job.AppIdentifier).Append(",");
                                break;
                            case "job_stack_name":                
                                var stack = "";
                                if(job.Parameters != null){
                                    foreach(var p in job.Parameters){
                                        if(p.Key == "stack_name") stack = p.Value;                                        
                                    }
                                }
                                csvBody.Append(stack).Append(",");
                                break;
                            case "na":
                                csvBody.Append("na").Append(",");
                                break;
                            default:
                                csvBody.Append(",");
                                break;                                    
                        }
                    }
                }
            }
            csv.Append(csvHeader).Append(csvBody);

            var filename = string.Format("{0}/files/{1}-job-report-{2}.csv", "", context.GetConfigValue("siteNameShort"),startdateString);            
            System.IO.File.WriteAllText(filename, csv.ToString());               
        }

    }
}
