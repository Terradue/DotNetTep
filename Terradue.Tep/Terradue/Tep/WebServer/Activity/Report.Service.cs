using System;
using System.Collections.Generic;
using System.Linq;
using ServiceStack.ServiceHost;
using Terradue.Portal;
using Terradue.Tep.WebServer;
using Terradue.WebService.Model;

namespace Terradue.Tep.WebServer.Services {

    [Route("/report", "GET", Summary = "GET report", Notes = "")]
    public class ReportGetRequest : IReturn<string>{
        [ApiMember(Name = "startdate", Description = "start date", ParameterType = "query", DataType = "datetime", IsRequired = false)]
        public DateTime startdate { get; set; }

        [ApiMember(Name = "enddate", Description = "end date", ParameterType = "query", DataType = "datetime", IsRequired = false)]
        public DateTime enddate { get; set; }

        [ApiMember(Name = "withJobResultsNb", Description = "withJobResultsNb", ParameterType = "query", DataType = "bool", IsRequired = false)]
        public bool withJobResultsNb { get; set; }
    }

    [Route("/report/job", "GET", Summary = "GET report", Notes = "")]
    public class JobReportGetRequest : IReturn<string>{
        [ApiMember(Name = "created", Description = "created date range", ParameterType = "query", DataType = "string", IsRequired = false)]
        public string created { get; set; }

        [ApiMember(Name = "q", Description = "q", ParameterType = "query", DataType = "string", IsRequired = false)]
        public string q { get; set; }

        [ApiMember(Name = "service", Description = "service", ParameterType = "query", DataType = "string", IsRequired = false)]
        public string service { get; set; }

        [ApiMember(Name = "author", Description = "author", ParameterType = "query", DataType = "string", IsRequired = false)]
        public string author { get; set; }

        [ApiMember(Name = "archivestatus", Description = "archivestatus", ParameterType = "query", DataType = "string", IsRequired = false)]
        public string archivestatus { get; set; }

        [ApiMember(Name = "status", Description = "status", ParameterType = "query", DataType = "string", IsRequired = false)]
        public string status { get; set; }

        [ApiMember(Name = "provider", Description = "provider", ParameterType = "query", DataType = "string", IsRequired = false)]
        public string provider { get; set; }

        [ApiMember(Name = "info", Description = "job infos", ParameterType = "query", DataType = "bool", IsRequired = false)]
        public string infos { get; set; }
    }

    [Route("/reports", "GET", Summary = "GET existing reports", Notes = "")]
    public class ReportsGetRequest : IReturn<List<string>>{}

    [Route("/reports/job/monthly", "GET", Summary = "GET existing reports", Notes = "")]
    public class ReportsGetMonthlyJobRequest : IReturn<List<string>>{}

    [Route("/report", "DELETE", Summary = "GET report", Notes = "")]
    public class ReportDeleteRequest : IReturn<WebResponseBool>{
        [ApiMember(Name = "filename", Description = "filename", ParameterType = "query", DataType = "string", IsRequired = true)]
        public string Filename { get; set; }
    }

    [Route("/report/jobs", "GET", Summary = "GET report", Notes = "")]
    public class GetJobsReportingRequest : IReturn<string> {
        
        [ApiMember(Name = "startdate", Description = "start date", ParameterType = "query", DataType = "datetime", IsRequired = false)]
        public string startdate { get; set; }

        [ApiMember(Name = "enddate", Description = "end date", ParameterType = "query", DataType = "datetime", IsRequired = false)]
        public string enddate { get; set; }
        
        [ApiMember(Name = "filename", Description = "filename", ParameterType = "query", DataType = "string", IsRequired = false)]
        public string filename { get; set; }
    }

    [Route("/report/jobs", "POST", Summary = "GET report", Notes = "")]
    public class GetJobReportingRequest : IReturn<string> {
        [ApiMember(Name = "emails", Description = "user emails", ParameterType = "query", DataType = "List<string>", IsRequired = false)]
        public List<string> Emails { get; set; }

        [ApiMember(Name = "startdate", Description = "start date", ParameterType = "query", DataType = "datetime", IsRequired = false)]
        public DateTime startdate { get; set; }

        [ApiMember(Name = "enddate", Description = "end date", ParameterType = "query", DataType = "datetime", IsRequired = false)]
        public DateTime enddate { get; set; }

        [ApiMember(Name = "token", Description = "token", ParameterType = "query", DataType = "string", IsRequired = false)]
        public string token { get; set; }

        [ApiMember(Name = "filename", Description = "filename", ParameterType = "query", DataType = "string", IsRequired = false)]
        public string filename { get; set; }

        [ApiMember(Name = "userinfo", Description = "userinfo", ParameterType = "query", DataType = "List<string>", IsRequired = false)]
        public List<string> userinfo { get; set; }

        [ApiMember(Name = "jobinfo", Description = "userinfo", ParameterType = "query", DataType = "List<string>", IsRequired = false)]
        public List<string> jobinfo { get; set; }
    }


    [Api("Tep Terradue webserver")]
    [Restrict(EndpointAttributes.InSecure | EndpointAttributes.InternalNetworkAccess | EndpointAttributes.Json,
              EndpointAttributes.Secure | EndpointAttributes.External | EndpointAttributes.Json)]
    public class ReportServiceTep : ServiceStack.ServiceInterface.Service {

        private static readonly log4net.ILog log = log4net.LogManager.GetLogger
            (System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public object Delete(ReportDeleteRequest request){
            var context = TepWebContext.GetWebContext(PagePrivileges.AdminOnly);
            List<string> result = new List<string>();
            try {
                context.Open();
                context.LogInfo(this,string.Format("/report DELETE filename='{0}'", request.Filename));


                string path = AppDomain.CurrentDomain.BaseDirectory;
                if(!path.EndsWith("/")) path += "/";

                System.IO.File.Delete(path + "files/" + request.Filename);

                context.Close();
            } catch (Exception e) {
                context.LogError(this, e.Message, e);
                context.Close();
                throw e;
            }
            return new WebResponseBool(true);
        }

        public object Get(ReportsGetRequest request){
            var context = TepWebContext.GetWebContext(PagePrivileges.AdminOnly);
            List<string> result = new List<string>();
            try {
                context.Open();
                context.LogInfo(this,string.Format("/reports GET"));

                string path = AppDomain.CurrentDomain.BaseDirectory;
                if(!path.EndsWith("/")) path += "/";

                context.LogDebug(this, path);
                result = System.IO.Directory.GetFiles(path + "files","*.csv").ToList();
                result = result.ConvertAll(f => f.Substring(f.LastIndexOf("/") + 1));

                context.LogInfo(this,string.Format("Get list of Reports"));

                context.Close();
            } catch (Exception e) {
                context.LogError(this, e.Message, e);
                context.Close();
                throw e;
            }
            return result;
        }
        public object Get(ReportsGetMonthlyJobRequest request){
            var context = TepWebContext.GetWebContext(PagePrivileges.AdminOnly);
            List<string> result = new List<string>();
            try {
                context.Open();
                context.LogInfo(this,string.Format("/reports/job/monthly GET"));

                string path = AppDomain.CurrentDomain.BaseDirectory;
                if(!path.EndsWith("/")) path += "/";

                context.LogDebug(this, path);
                result = System.IO.Directory.GetFiles(path + "files",string.Format("{0}-monthly-job-report-*.csv", context.GetConfigValue("siteNameShort"))).ToList();
                result = result.ConvertAll(f => f.Substring(f.LastIndexOf("/") + 1));

                context.LogInfo(this,string.Format("Get list of Reports"));

                context.Close();
            } catch (Exception e) {
                context.LogError(this, e.Message, e);
                context.Close();
                throw e;
            }
            return result;
        }        

        public object Get(ReportGetRequest request){
            var context = TepWebContext.GetWebContext(PagePrivileges.AdminOnly);
            var csv = new System.Text.StringBuilder();

            var startdate = (request.startdate != DateTime.MinValue ? request.startdate.ToString("yyyy-MM-dd") : "2014-01-01");
            var enddate = (request.enddate != DateTime.MinValue ? request.enddate.ToString("yyyy-MM-dd") : DateTime.UtcNow.ToString("yyyy-MM-dd"));

            try {
                context.Open();
                context.LogInfo(this,string.Format("/report GET startdate='{0}',enddate='{1}'", request.startdate, request.enddate));

                var skipedIds = context.GetConfigValue("report-ignored-ids");
                if (string.IsNullOrEmpty(skipedIds)) skipedIds = "0";

                GenerateCsvHeader(context, csv, startdate, enddate, skipedIds);
                csv.Append(Environment.NewLine);
                GenerateCsvUsersPart(context, csv, startdate, enddate, skipedIds);
                csv.Append(Environment.NewLine);
                GenerateCsvWpsJobPart(context, csv, startdate, enddate, skipedIds);
                csv.Append(Environment.NewLine);
                GenerateCsvDataPackagePart(context, csv, startdate, enddate, skipedIds);

                string path = AppDomain.CurrentDomain.BaseDirectory;
                if(!path.EndsWith("/")) path += "/";

                System.IO.File.WriteAllText(string.Format("{2}files/{3}-report-{0}-{1}.csv",startdate,enddate,path,context.GetConfigValue("SiteNameShort")), csv.ToString());

                context.LogDebug(this,string.Format("Get report {1}-{2} (user Id = {0})", context.UserId, request.startdate, request.enddate));

                context.Close();

            } catch (Exception e) {
                context.LogError(this, e.Message, e);
                context.Close();
                throw e;
            }
            Response.AddHeader("Content-Disposition", string.Format("attachment;filename={2}-report-{0}-{1}.csv",startdate,enddate,context.GetConfigValue("SiteNameShort")));
            return csv.ToString();
        }

        public object Post(GetJobReportingRequest request) {
            TepWebContext context = null;
            var csv = new System.Text.StringBuilder();
            try { 
                context = TepWebContext.GetWebContext(PagePrivileges.EverybodyView);
                context.Open();

                if (!(request.token == context.GetConfigValue("t2portal-safe-token"))) throw new Exception("Unauthorized access");

                context.AccessLevel = EntityAccessLevel.Administrator;
                
                var startdate = (request.startdate != DateTime.MinValue ? request.startdate.ToString("yyyy-MM-dd") : "2014-01-01");
                var enddate = (request.enddate != DateTime.MinValue ? request.enddate.ToString("yyyy-MM-dd") : DateTime.UtcNow.ToString("yyyy-MM-dd"));

                //Create Header
                var csvHeader = new System.Text.StringBuilder();
                if (request.userinfo != null){
                    foreach (var userinfo in request.userinfo) {
                        switch(userinfo){
                            case "email":
                                csvHeader.Append("User email,");
                            break;
                            case "identifier":
                                csvHeader.Append("User identifier,");
                            break;
                            case "level":
                                csvHeader.Append("User level,");
                            break;
                            case "affiliation":
                                csvHeader.Append("User affiliation,");
                            break;
                            case "creation":
                                csvHeader.Append("User creation,");
                            break;
                            case "login":
                                csvHeader.Append("User last login,");
                            break;
                        }
                    }                        
                }
                if (request.jobinfo != null){
                    foreach (var jobinfo in request.jobinfo) {
                        switch(jobinfo){
                            case "id":
                                csvHeader.Append("Job id,");
                            break;
                            case "url":
                                csvHeader.Append("Job url,");
                            break;
                            case "status":
                                csvHeader.Append("Job status,");
                            break;
                            case "shared":
                                csvHeader.Append("Job shared,");
                            break;
                            case "creation":
                                csvHeader.Append("Job creation time,");
                            break;
                            case "end":
                                csvHeader.Append("Job end time,");
                                break;
                            case "wps":
                                csvHeader.Append("Job wps,");
                            break;
                            case "duration":
                                csvHeader.Append("Job duration,");
                            break;
                            case "app":
                                csvHeader.Append("Job app,");
                            break;
                            case "nbinput":
                                csvHeader.Append("Job nb inputs,");
                            break;
                        }
                    }                        
                }
                csvHeader.Length --;
                csvHeader.Append(Environment.NewLine);

                //Create body
                var csvBody = new System.Text.StringBuilder();
                var users = new List<UserTep>();
                if (request.Emails != null) {
                    foreach (var email in request.Emails) {
                        try{
                            users.Add(UserTep.FromEmail(context, email));
                        }catch(Exception){}
                    }
                } else {
                    var dbusers = new EntityList<UserTep>(context);
                    dbusers.Load();
                    users.AddRange(dbusers.GetItemsAsList());
                }

                foreach(var user in users){
                    //get list of jobs
                    string sql = String.Format("SELECT wpsjob.id from wpsjob WHERE wpsjob.id_usr={0} AND wpsjob.created_time > '{1}' AND wpsjob.created_time < '{2}';", user.Id, startdate, enddate);
                    var ids = context.GetQueryIntegerValues(sql);
                    foreach (var id in ids) {
                        var job = WpsJob.FromId(context, id);
                        if (request.userinfo != null){
                            foreach (var userinfo in request.userinfo) {
                                switch(userinfo){
                                    case "email":
                                        csvBody.Append(user.Email+",");
                                    break;
                                    case "identifier":
                                        csvBody.Append(user.Username+",");
                                    break;
                                    case "level":
                                        csvBody.Append(user.Level+",");
                                    break;
                                    case "affiliation":
                                        csvBody.Append(user.Affiliation+",");
                                    break;
                                    case "creation":
                                        user.LoadRegistrationInfo();            
                                        csvBody.Append(user.RegistrationDate+",");
                                    break;
                                    case "login":
                                        user.LoadRegistrationInfo();
                                        csvBody.Append(user.GetLastLoginDate()+",");
                                    break;
                                }
                            }                        
                        }
                        if(request.jobinfo != null){
                            foreach (var jobinfo in request.jobinfo) {
                                switch(jobinfo){
                                    case "id":
                                        csvBody.Append(job.Id+",");
                                    break;
                                    case "url":
                                        csvBody.Append(job.StatusLocation+",");
                                    break;
                                    case "status":
                                        bool succeeded = false;
                                        switch (job.Status) {
                                            case WpsJobStatus.SUCCEEDED:
                                            case WpsJobStatus.STAGED:
                                            case WpsJobStatus.COORDINATOR:
                                                succeeded = true;
                                                break;
                                            default:
                                                break;
                                        }

                                        csvBody.Append((succeeded?"succeeded":"failed")+",");
                                    break;
                                    case "shared":
                                        csvBody.Append((job.IsPrivate()?"false":"true")+",");
                                    break;
                                    case "creation":
                                        csvBody.Append(job.CreatedTime.ToString("yyyy-MM-ddTHH:mm:ss")+",");
                                    break;
                                    case "end":
                                        csvBody.Append(job.EndTime != DateTime.MinValue ? job.EndTime.ToString("yyyy-MM-ddTHH:mm:ss") : "" + ",");
                                    break;
                                    case "wps":
                                        if (!string.IsNullOrEmpty(job.WpsName))
                                            csvBody.Append(job.WpsName.Replace(",", "\\,") + ",");
                                        else {
                                            string wpsname = "";
                                            try {
                                                WpsProcessOffering wps = CloudWpsFactory.GetWpsProcessOffering(context, job.ProcessId);
                                                wpsname = wps.Name;
                                            } catch (Exception) {
                                                wpsname = job.ProcessId;
                                            }
                                            csvBody.Append(wpsname.Replace(",", "\\,") + ",");
                                        }
                                        break;
                                    case "duration":
                                        var processingTime = job.EndTime == DateTime.MinValue || job.EndTime < job.CreatedTime ? 0 : (job.EndTime - job.CreatedTime).Minutes;
                                        csvBody.Append(processingTime+",");
                                    break;
                                    case "app":
                                        csvBody.Append(job.AppIdentifier+",");
                                    break;
                                    case "nbinput":
                                        int totalDataProcessed = 0;
                                        if (job.Parameters != null) {
                                            foreach (var parameter in job.Parameters) {
                                                if (!string.IsNullOrEmpty(parameter.Value) && (parameter.Value.StartsWith("http://") || parameter.Value.StartsWith("https://"))) {
                                                    var url = parameter.Value;
                                                    totalDataProcessed++;
                                                }
                                            }
                                        }
                                        csvBody.Append(totalDataProcessed+",");
                                    break;
                                }
                            }
                        }
                        csvBody.Length--;
                        csvBody.Append(Environment.NewLine);
                    }                        
                }
                csv.Append(csvHeader).Append(csvBody);
                
            } catch (Exception e) {
                context.LogError(this, e.Message, e);
                context.Close();
                throw e;
            }

            if (!string.IsNullOrEmpty(request.filename)){
                string path = AppDomain.CurrentDomain.BaseDirectory;
                if (!path.EndsWith("/")) path += "/";
                var filename = string.Format("{0}files/{1}", path, request.filename);
                System.IO.File.WriteAllText(filename, csv.ToString());

                path = context.GetConfigValue("BaseUrl");
                if (!path.EndsWith("/")) path += "/";
                filename = string.Format("{0}files/{1}", path, request.filename);                
                return filename;
            } else {
                return csv.ToString();
            }
        }

        public object Get(JobReportGetRequest request){             
            var context = TepWebContext.GetWebContext(PagePrivileges.AdminOnly);

            if (string.IsNullOrEmpty(request.created)) return null;
            if (request.infos == null) request.infos = "job-id,job-identifier,job-status,job-creation,job-end,job-url,job-nbinputs,job-wps,job-shared,user-username,user-email,user-affiliation,user-level,user-creation,user-login";

            var startdate = request.created.Replace("[","").Replace("]","").Split(',')[0];
            var enddate = request.created.Replace("[","").Replace("]","").Split(',')[1];

            //add more tests to not have too long queries


            string result = "";

            try {
                context.Open();
                context.LogInfo(this,string.Format("/job/report GET startdate='{0}',enddate='{1}'", startdate, enddate));

                var csv = new System.Text.StringBuilder();

                //Create Header
                var csvHeader = new System.Text.StringBuilder();            
                foreach (var info in request.infos.Split(',')) {
                    switch(info){
                        case "user-email":
                            csvHeader.Append("User email,");
                        break;
                        case "user-identifier":
                            csvHeader.Append("User identifier,");
                        break;
                        case "user-level":
                            csvHeader.Append("User level,");
                        break;
                        case "user-affiliation":
                            csvHeader.Append("User affiliation,");
                        break;
                        case "user-creation":
                            csvHeader.Append("User creation,");
                        break;
                        case "user-login":
                            csvHeader.Append("User last login,");
                        break;                
                        case "job-id":
                            csvHeader.Append("Job id,");
                        break;
                        case "job-identifier":
                            csvHeader.Append("Job identifier,");
                        break;
                        case "job-url":
                            csvHeader.Append("Job url,");
                        break;
                        case "job-status":
                            csvHeader.Append("Job status,");
                        break;
                        case "job-shared":
                            csvHeader.Append("Job shared,");
                        break;
                        case "job-creation":
                            csvHeader.Append("Job creation time,");
                        break;
                        case "job-end":
                            csvHeader.Append("Job end time,");
                            break;
                        case "job-wps":
                            csvHeader.Append("Job wps,");
                        break;
                        case "job-duration":
                            csvHeader.Append("Job duration,");
                        break;
                        case "job-app":
                            csvHeader.Append("Job app,");
                        break;
                        case "job-nbinput":
                            csvHeader.Append("Job nb inputs,");
                        break;
                    }                        
                }
                csvHeader.Length --;//remove the coma
                csvHeader.Append(Environment.NewLine);

                var userIdSearch = 0;
                if(!string.IsNullOrEmpty(request.author)){
                    var usr = User.FromUsername(context, request.author);
                    userIdSearch = usr.Id;                    
                }

                // Create Body
                var csvBody = new System.Text.StringBuilder();
                string sql = String.Format("SELECT id as job_id, identifier as job_identifier, status as job_status, created_time as job_creation, end_time as job_end, job_storeurl, job_nbinputs, job_wps, MAX(shared) as job_shared, username as usr_username, email as user_email, affiliation as usr_affiliation, usr_level, MIN(usrlogin) as usr_firstlogin, MAX(usrlogin) as usr_lastlogin FROM " +
                    "(SELECT wpsjob.id, wpsjob.identifier, wpsjob.status, wpsjob.created_time, wpsjob.end_time, " +
                    "REPLACE(wpsjob.status_url,'https://recast.terradue.com/t2api/describe/','https://store.terradue.com/') as job_storeurl, " +
                    "((CEILING((LENGTH(wpsjob.params) - LENGTH(REPLACE(wpsjob.params, 'http://', '')))/7)) + CEILING((LENGTH(wpsjob.params) - LENGTH(REPLACE(wpsjob.params, 'https://', '')))/8)) AS job_nbinputs, " +
                    "service.name as job_wps, " +
                    "usr.username, usr.email, usr.affiliation, " +
                    "CASE WHEN usr.level = 0 THEN 'visitor' ELSE " +
                        "CASE WHEN usr.level = 1 THEN 'member' ELSE " +
                            "CASE WHEN usr.level = 2 THEN 'stakeholder' ELSE 'administrator' END END END AS usr_level, " +
                    "usrsession.log_time as usrlogin," +
                    "CASE WHEN (p.id_usr IS NOT NULL AND p.id_usr != wpsjob.id_usr) OR p.id_grp IS NOT NULL OR (p.id_usr IS NULL AND p.id_grp IS NULL AND p.id_wpsjob IS NOT NULL) THEN 1 ELSE 0 END AS shared " +
                    "FROM wpsjob " +
                    "INNER JOIN usr on wpsjob.id_usr = usr.id " +
                    "INNER JOIN usrsession ON usr.id = usrsession.id_usr " +
                    "LEFT JOIN service ON service.identifier = wpsjob.process " +
                    "LEFT JOIN wpsjob_perm AS p ON wpsjob.id = p.id_wpsjob " +
                    "WHERE {0}" + "{1}" + "{2}" + "{3}" + "{4}" + "{5}" + "{6}" + "{7}" + ") AS Q1 " +
                    "GROUP BY id;", 
                    string.Format("created_time >= STR_TO_DATE('{0}','%Y-%m-%d') ",startdate),
                    (!string.IsNullOrEmpty(enddate) ? string.Format("AND created_time <= STR_TO_DATE('{0}','%Y-%m-%d') ",enddate) : ""),
                    !string.IsNullOrEmpty(request.q) ? string.Format("AND identifier LIKE '%{0}%' AND name LIKE '%{0}%' ",request.q) : "",
                    !string.IsNullOrEmpty(request.archivestatus) ? string.Format("AND archive_status IN ({0}) ",request.archivestatus) : "", 
                    !string.IsNullOrEmpty(request.status) ? string.Format("AND status IN ({0}) ",request.status) : "", 
                    !string.IsNullOrEmpty(request.service) ? string.Format("AND wps_name LIKE '%{0}%' ",request.service) : "",
                    !string.IsNullOrEmpty(request.provider) ? string.Format("AND wps LIKE '%{0}%' ",request.provider) : "",
                    userIdSearch != 0 ? string.Format("AND id_usr = {0}", userIdSearch) : ""
                );

                context.LogInfo(this, sql);

                System.Data.IDbConnection dbConnection = context.GetDbConnection();
                System.Data.IDataReader reader = context.GetQueryResult(sql, dbConnection);
                while (reader.Read()) {
                    if (reader.GetValue(0) == DBNull.Value) continue;
                    
                    var job_id = reader.GetInt32(0);
                    var job_identifier = reader.GetString(1);
                    var job_status = reader.GetString(2);
                    var job_start = reader.GetString(3);
                    var job_end = reader.GetValue(4) != DBNull.Value ? reader.GetString(4) : "";
                    var job_storeurl = reader.GetString(5);
                    var job_nbinputs = reader.GetInt32(6);
                    var job_wps = reader.GetValue(7) != DBNull.Value ? reader.GetString(7) : "";
                    var job_shared = reader.GetInt32(8);
                    var usr_username = reader.GetValue(9) != DBNull.Value ? reader.GetString(9) : "";
                    var usr_email = reader.GetValue(10) != DBNull.Value ? reader.GetString(10) : "";
                    var usr_affiliation = reader.GetValue(11) != DBNull.Value ? reader.GetString(11) : "";
                    var usr_level = reader.GetValue(12) != DBNull.Value ? reader.GetString(12) : "";
                    var usr_first_login = reader.GetValue(13) != DBNull.Value ? reader.GetString(13) : "";
                    var usr_last_login = reader.GetValue(14) != DBNull.Value ? reader.GetString(14) : "";

                    foreach (var info in request.infos.Split(',')) {
                        switch(info){
                            case "user-email":
                                csvBody.Append(usr_email + ",");
                            break;
                            case "user-identifier":
                                csvBody.Append(usr_username + ",");
                            break;
                            case "user-level":
                                csvBody.Append(usr_level + ",");
                            break;
                            case "user-affiliation":
                                csvBody.Append(usr_affiliation + ",");
                            break;
                            case "user-creation":
                                csvBody.Append(usr_first_login + ",");
                            break;
                            case "user-login":
                                csvBody.Append(usr_last_login + ",");
                            break;                
                            case "job-id":
                                csvBody.Append(job_id + ",");
                            break;
                            case "job-identifier":
                                csvBody.Append(job_identifier + ",");
                            break;
                            case "job-url":
                                csvBody.Append(job_storeurl + ",");
                            break;
                            case "job-status":
                                csvBody.Append(job_status + ",");
                            break;
                            case "job-shared":
                                csvBody.Append(job_shared + ",");
                            break;
                            case "job-creation":
                                csvBody.Append(job_start + ",");
                            break;
                            case "job-end":
                                csvBody.Append(job_end + ",");
                                break;
                            case "job-wps":
                                csvBody.Append(job_wps + ",");
                            break;                                       
                            case "job-nbinput":
                                csvBody.Append(job_nbinputs + ",");
                            break;
                        }                        
                    }
                    csvBody.Length--;
                    csvBody.Append(Environment.NewLine);
                }
                context.CloseQueryResult(reader, dbConnection);
                csv.Append(csvHeader).Append(csvBody);

                result = csv.ToString();      

                context.Close();
            } catch (Exception e) {
                context.LogError(this, e.Message, e);
                context.Close();
                throw e;
            }
            var filename = (!string.IsNullOrEmpty(startdate) && !string.IsNullOrEmpty(enddate)) ? string.Format("{2}-jobreport-{0}-{1}.csv", startdate, enddate, context.GetConfigValue("SiteNameShort")) : string.Format("{0}-jobreport.csv", context.GetConfigValue("SiteNameShort"));
            Response.AddHeader("Content-Disposition", string.Format("attachment;filename={0}",filename));
            return result;
        }

        /// <summary>
        /// Generates the csv header.
        /// </summary>
        /// <param name="context">Context.</param>
        /// <param name="csv">Csv.</param>
        /// <param name="startdate">Startdate.</param>
        /// <param name="enddate">Enddate.</param>
        /// <param name="skipedIds">Skiped identifiers.</param>
        private void GenerateCsvHeader(IfyContext context, System.Text.StringBuilder csv, string startdate, string enddate, string skipedIds){
            csv.Append(context.GetConfigValue("SiteName") + " statistics reporting" + Environment.NewLine);
            csv.Append("Date of creation," + DateTime.UtcNow.ToString("yyyy-MM-dd") + Environment.NewLine);
            csv.Append("Parameters" + Environment.NewLine);
            csv.Append("Start Date," + startdate + Environment.NewLine);
            csv.Append("End Date," + enddate + Environment.NewLine);

            //Users not in stats
            csv.Append("Users skipped for report" + Environment.NewLine);
            string sql = string.Format("SELECT username FROM usr WHERE id IN ({0});", skipedIds);
            System.Data.IDbConnection dbConnection = context.GetDbConnection();
            System.Data.IDataReader reader = context.GetQueryResult(sql, dbConnection);
            while (reader.Read()) {
                if (reader.GetValue(0) != DBNull.Value) {
                    csv.Append(reader.GetString(0) + Environment.NewLine);
                }
            }
            context.CloseQueryResult(reader, dbConnection);
            csv.Append(Environment.NewLine);
        }

        private List<int> GetNewUsers(IfyContext context, string startdate, string enddate, string skipedIds) {
            List<int> ids = new List<int>();

            string sql = string.Format("SELECT DISTINCT usr.id FROM usr WHERE " +
                               "id NOT IN (SELECT id_usr FROM usrsession WHERE usrsession.log_time < '{0}' ) " +
                                       "AND id IN (SELECT id_usr FROM usrsession WHERE usrsession.log_time <= '{1}'){2};",
                                       startdate, enddate, string.IsNullOrEmpty(skipedIds) ? "" : " AND id NOT IN (" + skipedIds + ")");
            System.Data.IDbConnection dbConnection = context.GetDbConnection();
            System.Data.IDataReader reader = context.GetQueryResult(sql, dbConnection);
            while (reader.Read()) {
                if (reader.GetValue(0) != DBNull.Value) {
                    ids.Add(reader.GetInt32(0));
                }
            }
            context.CloseQueryResult(reader, dbConnection);
            return ids;
        }
  
        /// <summary>
        /// Generates the csv users part.
        /// </summary>
        /// <param name="context">Context.</param>
        /// <param name="csv">Csv.</param>
        /// <param name="startdate">Startdate.</param>
        /// <param name="enddate">Enddate.</param>
        /// <param name="skipedIds">Skiped identifiers.</param>
        private void GenerateCsvUsersPart(IfyContext context, System.Text.StringBuilder csv, string startdate, string enddate, string skipedIds){
            //Total number of users
            string sql = string.Format("SELECT COUNT(*) FROM usr WHERE " +
                                       "id NOT IN ({0}) " +
                                       "AND id IN (SELECT id_usr FROM usrsession WHERE usrsession.log_time < '{1}' );"
                                       , skipedIds, startdate);
            var totalUsers = context.GetQueryIntegerValue(sql);
            csv.Append(string.Format("Total number of users at {0},{1}{2}{2}",startdate,totalUsers,Environment.NewLine));
            csv.Append(Environment.NewLine);

            List<int> ids = GetNewUsers(context, startdate, enddate, skipedIds);
                     
            csv.Append(String.Format("Users signed in between {0} and {1},{2}{3}", startdate, enddate, ids.Count, Environment.NewLine));
            if (ids.Count > 0) {
                csv.Append("Username,Name,Affiliation,First Login date,Terradue cloud username" + Environment.NewLine);
                foreach (int id in ids) {
                    var usr = UserTep.FromId(context, id);
                    csv.Append(String.Format("{0},{1},\"{2}\",{3},{4}{5}", usr.Username, usr.FirstName + " " + usr.LastName, string.IsNullOrEmpty(usr.Affiliation) ? "n/a" : usr.Affiliation.Replace(",", "\\,"), usr.GetFirstLoginDate(), usr.TerradueCloudUsername, Environment.NewLine));
                }
            }
            csv.Append(Environment.NewLine);

            //Active users         
			var nvc = Analytics.GetActiveUsers(context, startdate, enddate, skipedIds, null);
            csv.Append(String.Format("Active users between {0} and {1},{2}{3}", startdate, enddate, nvc.AllKeys.Length, Environment.NewLine));
            if (nvc.AllKeys.Length > 0) {
				csv.Append("Username,Name,Affiliation,Nb of logins,Average session (min)" + Environment.NewLine);
                var analytics = new List<ReportAnalytic>();
                foreach (string id in nvc.AllKeys) {
                    var usr = UserTep.FromId(context, Int32.Parse(id));
                    var name = string.Format("{0},{1},\"{2}\"", usr.Username, usr.FirstName + " " + usr.LastName, string.IsNullOrEmpty(usr.Affiliation) ? "n/a" : usr.Affiliation.Replace(",", "\\,"));

					//get average session time
                    sql = string.Format("SELECT log_time,log_end FROM usrsession WHERE id_usr={0} AND log_time > '{1}' AND log_time < '{2}' AND log_end IS NOT NULL order by log_time desc;",id,startdate,enddate);
					System.Data.IDbConnection dbConnection = context.GetDbConnection();
                    System.Data.IDataReader reader = context.GetQueryResult(sql, dbConnection);
					int totalsession = 0;
					int i = 1;
					while (reader.Read()) {
						DateTime logStart = reader.GetDateTime(0);
						DateTime logEnd = reader.GetDateTime(1);
						int session = (int)Math.Round((logEnd - logStart).TotalMinutes);
						totalsession += session;
						i++;
					}
					context.CloseQueryResult(reader, dbConnection);
					int averagesession = totalsession / i;               
					analytics.Add(new ReportAnalytic{name = name, Total = usr.GetNbOfLogin(startdate, enddate), Analytic1 = averagesession });
                }
                foreach (var analytic in analytics) {
					csv.Append(String.Format("{0},{1},{2}{3}",
                                             analytic.name,
                                             analytic.Total,
					                         analytic.Analytic1,
                                             Environment.NewLine));
                }
                csv.Append(Environment.NewLine);
            }
            csv.Append(Environment.NewLine);
        }

        private void GenerateCsvWpsJobPart(IfyContext context, System.Text.StringBuilder csv, string startdate, string enddate, string skipedIds){
            //Runs of processing jobs on the Portal: username, wpsjob name, job creation date (ORDERED BY CREATION DATE)
            string sql = String.Format("SELECT usr.username, wpsjob.id from wpsjob INNER JOIN usr ON wpsjob.id_usr=usr.id WHERE wpsjob.created_time > '{0}' AND wpsjob.created_time < '{1}' AND wpsjob.id_usr NOT IN ({2}) ORDER BY username;", 
                                       startdate, enddate, skipedIds);
            System.Data.IDbConnection dbConnection = context.GetDbConnection();
            System.Data.IDataReader reader = context.GetQueryResult(sql, dbConnection);
            List<int> ids = new List<int>();
            while (reader.Read()) {
                ids.Add(reader.GetInt32(1));
            }
            context.CloseQueryResult(reader, dbConnection);

            int success = 0;
            int failed = 0;
            List<WpsJob> jobs = new List<WpsJob>();
            foreach (int id in ids) {
                var job = WpsJob.FromId(context, id);
                jobs.Add(job);
                switch(job.Status){
                    case WpsJobStatus.SUCCEEDED:
                    case WpsJobStatus.STAGED:
                    case WpsJobStatus.COORDINATOR:
                        success++;
                        break;
                    case WpsJobStatus.FAILED:
                        failed++;
                        break;
                    default:
                        break;
                }
            }

            //list all jobs created
            if (jobs.Count > 0) {
                csv.Append(String.Format("Wps jobs created between {0} and {1},{2} ({3} succeeded | {4} failed){5}", startdate, enddate, ids.Count, success, failed, Environment.NewLine));
                csv.Append("Name,Owner,Creation date,Process name,Status,Processing time (minutes),Nb results,Shared,link" + Environment.NewLine);//csv.Append("Name,Owner,Creation date,Process name,Status,Nb of results,Shared" + Environment.NewLine);
                int totalProcessingTime = 0;
                foreach (WpsJob job in jobs) {
                    User usr = User.FromId(context, job.OwnerId);
                    string wpsname = "";
                    try {
                        WpsProcessOffering wps = CloudWpsFactory.GetWpsProcessOffering(context, job.ProcessId);
                        wpsname = wps.Name;
                    } catch (Exception) {
                        wpsname = job.ProcessId;
                    }
                    var statuslocation = context.BaseUrl + "/wps/RetrieveResultServlet?id=" + job.Identifier;
                    var processingTime = job.EndTime == DateTime.MinValue || job.EndTime < job.CreatedTime ? 0 : (job.EndTime - job.CreatedTime).Minutes;
                    totalProcessingTime += processingTime;
                    csv.Append(String.Format("\"{0}\",{1},{2},\"{3}\",{4},{5},{6},{7},{8}{9}", job.Name.Replace(",", "\\,"), usr.Username, job.CreatedTime.ToString("yyyy-MM-ddTHH:mm:ss"), wpsname.Replace(",", "\\,"), job.StringStatus,processingTime,job.NbResults,(job.IsPrivate() ? "no" : "yes"),statuslocation, Environment.NewLine));
                }
                csv.Append(Environment.NewLine);
                TimeSpan span = TimeSpan.FromMinutes(totalProcessingTime);
                string totalProcessingTimeString = string.Format("{0}{1}", span.Hours > 0 ? span.Hours+" h " : "", span.Minutes + " minutes");
                csv.Append(String.Format("Total processing time for wpsjobs created between {0} and {1},{2}{3}", startdate, enddate, totalProcessingTimeString, Environment.NewLine));
                csv.Append(Environment.NewLine);
            }

            var analytics = new List<ReportAnalytic>();

            //Nb of wpsjobs per user         
			var idsUsr = Analytics.GetActiveUsers(context, startdate, enddate, skipedIds, null);
            if (idsUsr.AllKeys.Length > 0) {
                csv.Append(string.Format("Number of wpsjobs created per user between {0} and {1}{2}", startdate, enddate, Environment.NewLine));
                csv.Append("Username,Total,Succeeded,Failed" + Environment.NewLine);
                analytics = new List<ReportAnalytic>();
                foreach (string id in idsUsr.AllKeys) {
                    User usr = User.FromId(context, Int32.Parse(id));
                    Analytics analytic = new Analytics(context, usr);
                    analytic.AnalyseCollections = false;
                    analytic.AnalyseDataPackages = false;
                    analytic.Load(startdate, enddate);
                    if(analytic.WpsJobSubmittedCount > 0)
                        analytics.Add(new ReportAnalytic { name = usr.Username, Total = analytic.WpsJobSubmittedCount, Analytic1 = analytic.WpsJobSuccessCount, Analytic2 = analytic.WpsJobFailedCount });
                }
                analytics.Sort();
                analytics.Reverse();
                foreach (var analytic in analytics) {
                    csv.Append(String.Format("{0},{1},{2},{3}{4}",
                                             analytic.name,
                                             analytic.Total,
                                             analytic.Analytic1,
                                             analytic.Analytic2,
                                             Environment.NewLine));
                }
                csv.Append(Environment.NewLine);
            }

            //Nb of wpsjobs per communities
            var domains = new CommunityCollection(context);
            domains.Load();
            if (domains.Count > 0) {
                csv.Append(string.Format("Number of wpsjobs created per community between {0} and {1}{2}", startdate, enddate, Environment.NewLine));
                csv.Append("Community,Total,Succeeded,Failed" + Environment.NewLine);
                analytics = new List<ReportAnalytic>();
                foreach (var domain in domains) {
                    Analytics analytic = new Analytics(context, domain);
                    analytic.AnalyseCollections = false;
                    analytic.AnalyseDataPackages = false;
                    analytic.SkipIds = skipedIds.Split(",".ToCharArray()).Select(s => int.Parse(s)).ToList();
                    analytic.Load(startdate, enddate);
                    if (analytic.WpsJobSubmittedCount > 0)
                        analytics.Add(new ReportAnalytic { name = domain.Name, Total = analytic.WpsJobSubmittedCount, Analytic1 = analytic.WpsJobSuccessCount, Analytic2 = analytic.WpsJobFailedCount });
                }
                analytics.Sort();
                analytics.Reverse();
                foreach (var analytic in analytics) {
                    csv.Append(String.Format("{0},{1},{2},{3}{4}",
                                             analytic.name,
                                             analytic.Total,
                                             analytic.Analytic1,
                                             analytic.Analytic2,
                                             Environment.NewLine));
                }
                csv.Append(Environment.NewLine);
            }

            //Nb of wpsjobs per group
            var grps = new EntityList<Group>(context);
            grps.Load();
            if (grps.Count > 0) {
                csv.Append(string.Format("Number of wpsjob created per group between {0} and {1}{2}", startdate, enddate, Environment.NewLine));
                csv.Append("Group,Total,Succeeded,Failed" + Environment.NewLine);
                analytics = new List<ReportAnalytic>();
                foreach (var grp in grps) {
                    Analytics analytic = new Analytics(context, grp);
                    analytic.AnalyseCollections = false;
                    analytic.AnalyseDataPackages = false;
                    analytic.SkipIds = skipedIds.Split(",".ToCharArray()).Select(s => int.Parse(s)).ToList();
                    analytic.Load(startdate, enddate);
                    if (analytic.WpsJobSubmittedCount > 0)
                        analytics.Add(new ReportAnalytic { name = grp.Name, Total = analytic.WpsJobSubmittedCount, Analytic1 = analytic.WpsJobSuccessCount, Analytic2 = analytic.WpsJobFailedCount });
                }
            }
            analytics.Sort();
            analytics.Reverse();
            foreach (var analytic in analytics) {
                csv.Append(String.Format("{0},{1},{2},{3}{4}",
                                         analytic.name,
                                         analytic.Total,
                                         analytic.Analytic1,
                                         analytic.Analytic2,
                                         Environment.NewLine));
            }
            csv.Append(Environment.NewLine);

            //Nb of wpsjobs per service
            sql = String.Format("SELECT wpsjob.process FROM wpsjob WHERE wpsjob.created_time > '{0}' AND wpsjob.created_time < '{1}' AND wpsjob.id_usr NOT IN ({2}) GROUP BY wpsjob.process;",
                                startdate, enddate, skipedIds);
            dbConnection = context.GetDbConnection();
            reader = context.GetQueryResult(sql, dbConnection);
            List<string> services = new List<string>();
            while (reader.Read()) {
                if (reader.GetValue(0) != DBNull.Value) {
                    services.Add(reader.GetString(0));
                }
            }
            context.CloseQueryResult(reader, dbConnection);
            if (services.Count > 0) {
                csv.Append(string.Format("Number of wpsjob created per service between {0} and {1}{2}", startdate, enddate, Environment.NewLine));
                csv.Append("Service,Total,Succeeded,Failed" + Environment.NewLine);
                analytics = new List<ReportAnalytic>();
                foreach (var serviceId in services) {
                    Service service = null;
                    try {
                        service = Service.FromIdentifier(context, serviceId);
                    }catch(Exception e){
                        context.LogError(this, e.Message, e);
                        service = new WpsProcessOffering(context);
                        service.Identifier = serviceId;
                    }
                    Analytics analytic = new Analytics(context, service);
                    analytic.AnalyseCollections = false;
                    analytic.AnalyseDataPackages = false;
                    analytic.SkipIds = skipedIds.Split(",".ToCharArray()).Select(s => int.Parse(s)).ToList();
                    analytic.Load(startdate, enddate);
                    if (analytic.WpsJobSubmittedCount > 0)
                        analytics.Add(new ReportAnalytic { name = service.Name != null ? service.Name.Replace(",", "\\,") : service.Identifier, Total = analytic.WpsJobSubmittedCount, Analytic1 = analytic.WpsJobSuccessCount, Analytic2 = analytic.WpsJobFailedCount });
                }
                analytics.Sort();
                analytics.Reverse();
                foreach (var analytic in analytics) {
                    csv.Append(String.Format("\"{0}\",{1},{2},{3}{4}",
                                             analytic.name,
                                             analytic.Total,
                                             analytic.Analytic1,
                                             analytic.Analytic2,
                                             Environment.NewLine));
                }
                csv.Append(Environment.NewLine);
            }

            //Data processed by jobs
            if (jobs.Count > 0) {
                int totalDataProcessed = 0;
                Dictionary<string, int> dictionary = new Dictionary<string, int>();
                foreach (WpsJob job in jobs) {
                    foreach(var parameter in job.Parameters){
                        if(!string.IsNullOrEmpty(parameter.Value) && (parameter.Value.StartsWith("http://") || parameter.Value.StartsWith("https://"))){
                            var url = parameter.Value;
                            totalDataProcessed++;
                            var urib = new UriBuilder(url);
                            urib.Path = urib.Path.Trim('/');
                            var url2 = urib.Uri.AbsoluteUri;
                            if (dictionary.ContainsKey(url2)) dictionary[url2]++;
                            else dictionary.Add(url2, 1);
                        }
                    }
                }
                var l = dictionary.OrderBy(key => key.Key);
                var dic = l.ToDictionary((keyItem) => keyItem.Key, (valueItem) => valueItem.Value);
                csv.Append(String.Format("Data processed by Wps jobs created between {0} and {1},{2}{3}", startdate, enddate, totalDataProcessed, Environment.NewLine));
                csv.Append("Catalog URL,Number" + Environment.NewLine);
                foreach(var key in dic.Keys){
                    csv.Append(String.Format("\"{0}\",{1}{2}", key.Replace(",", "\\,"), dic[key], Environment.NewLine));
                }
                csv.Append(Environment.NewLine);
            }
        }

        /// <summary>
        /// Generates the csv data package part.
        /// </summary>
        /// <param name="context">Context.</param>
        /// <param name="csv">Csv.</param>
        /// <param name="startdate">Startdate.</param>
        /// <param name="enddate">Enddate.</param>
        /// <param name="skipedIds">Skiped identifiers.</param>
        private void GenerateCsvDataPackagePart(IfyContext context, System.Text.StringBuilder csv, string startdate, string enddate, string skipedIds){
            var analytics = new List<ReportAnalytic>();
            //  Data Packages created + shared
            string sql = String.Format("SELECT resourceset.id, usr.username, resourceset.name, resourceset.creation_time from resourceset INNER JOIN usr on usr.id=resourceset.id_usr " +
                                       "WHERE resourceset.creation_time >= '{0}' AND resourceset.creation_time <= '{1}' AND resourceset.id_usr NOT IN ({2}) AND resourceset.kind=0 " +
                                       "AND resourceset.identifier NOT LIKE '_series_%' AND resourceset.identifier NOT LIKE '_products_%' AND resourceset.identifier NOT LIKE '_index_%';",
                                       startdate, enddate, skipedIds);
            System.Data.IDbConnection dbConnection = context.GetDbConnection();
            System.Data.IDataReader reader = context.GetQueryResult(sql, dbConnection);
            List<string> lines = new List<string>();
            List<int> ids = new List<int>();
            while (reader.Read()) {
                if (reader.GetValue(0) != DBNull.Value) {
                    ids.Add(reader.GetInt32(0));
                    lines.Add(string.Format("{0},\"{1}\",{2}", reader.GetString(1), reader.GetString(2).Replace(",", "\\,"), reader.GetDateTime(3)));
                }
            }
            context.CloseQueryResult(reader, dbConnection);

            if (ids.Count > 0) {
                csv.Append(String.Format("Data packages created between {0} and {1},{2}{3}", startdate, enddate, ids.Count, Environment.NewLine));
                csv.Append("Username,Data package name,Data package creation date, Shared" + Environment.NewLine);
                for (int i = 0; i < ids.Count; i++) {
                    sql = string.Format("SELECT COUNT(*) FROM resourceset_perm WHERE id_resourceset={0} AND id_usr IS NULL AND id_grp IS NULL;", ids[i]);
                    csv.Append(lines[i]);
                    csv.Append("," + (context.GetQueryIntegerValue(sql) > 0 ? "yes" : "no"));
                    csv.Append(Environment.NewLine);
                }
                csv.Append(Environment.NewLine);
            }

            //Nb of data packages per user
			var idsUsr = Analytics.GetActiveUsers(context, startdate, enddate, skipedIds, null);
            if (idsUsr.AllKeys.Length > 0) {
                csv.Append(string.Format("Number of Data package created/loaded per user between {0} and {1}{2}", startdate, enddate, Environment.NewLine));
                csv.Append("Username,Data packages created,Data packages loaded,Item loaded" + Environment.NewLine);
                analytics = new List<ReportAnalytic>();
                foreach (string id in idsUsr.AllKeys) {
                    User usr = User.FromId(context, Int32.Parse(id));
                    Analytics analytic = new Analytics(context, usr);
                    analytic.AnalyseCollections = false;
                    analytic.AnalyseJobs = false;
                    analytic.Load(startdate, enddate);
                    if (analytic.DataPackageCreatedCount > 0)
                        analytics.Add(new ReportAnalytic{ name = usr.Username, Total = analytic.DataPackageCreatedCount, Analytic1 = analytic.DataPackageLoadCount, Analytic2 = analytic.DataPackageItemsLoadCount });
                }
                analytics.Sort();
                analytics.Reverse();
                foreach(var analytic in analytics){
                    csv.Append(String.Format("{0},{1},{2},{3}{4}",
                                             analytic.name,
                                             analytic.Total,
                                             analytic.Analytic1,
                                             analytic.Analytic2,
                                             Environment.NewLine));
                }
                csv.Append(Environment.NewLine);
            }

            //Nb of data packages per communities
            var domains = new CommunityCollection(context);
            domains.Load();
            if (domains.Count > 0) {
                csv.Append(string.Format("Number of Data package created per community between {0} and {1}{2}", startdate, enddate, Environment.NewLine));
                csv.Append("Community,Data packages created,Data packages loaded,Item loaded" + Environment.NewLine);
                analytics = new List<ReportAnalytic>();
                foreach (var domain in domains) {
                    Analytics analytic = new Analytics(context, domain);
                    analytic.AnalyseCollections = false;
                    analytic.AnalyseJobs = false;
                    analytic.SkipIds = skipedIds.Split(",".ToCharArray()).Select(s => int.Parse(s)).ToList();
                    analytic.Load(startdate, enddate);
                    if (analytic.DataPackageCreatedCount > 0)
                        analytics.Add(new ReportAnalytic { name = domain.Name, Total = analytic.DataPackageCreatedCount, Analytic1 = analytic.DataPackageLoadCount, Analytic2 = analytic.DataPackageItemsLoadCount });
                }
                analytics.Sort();
                analytics.Reverse();
                foreach (var analytic in analytics) {
                    csv.Append(String.Format("{0},{1},{2},{3}{4}",
                                             analytic.name,
                                             analytic.Total,
                                             analytic.Analytic1,
                                             analytic.Analytic2,
                                             Environment.NewLine));
                }
                csv.Append(Environment.NewLine);
            }

            //Nb of data packages per group
            var grps = new EntityList<Group>(context);
            grps.Load();
            if (grps.Count > 0) {
                csv.Append(string.Format("Number of Data package created per group between {0} and {1}{2}", startdate, enddate, Environment.NewLine));
                csv.Append("Group,Data packages created,Data packages loaded,Item loaded" + Environment.NewLine);
                analytics = new List<ReportAnalytic>();
                foreach (var grp in grps) {
                    Analytics analytic = new Analytics(context, grp);
                    analytic.AnalyseCollections = false;
                    analytic.AnalyseJobs = false;
                    analytic.SkipIds = skipedIds.Split(",".ToCharArray()).Select(s => int.Parse(s)).ToList();
                    analytic.Load(startdate, enddate);
                    if (analytic.DataPackageCreatedCount > 0)
                        analytics.Add(new ReportAnalytic { name = grp.Name, Total = analytic.DataPackageCreatedCount, Analytic1 = analytic.DataPackageLoadCount, Analytic2 = analytic.DataPackageItemsLoadCount });
                }
                analytics.Sort();
                analytics.Reverse();
                foreach (var analytic in analytics) {
                    csv.Append(String.Format("{0},{1},{2},{3}{4}",
                                             analytic.name,
                                             analytic.Total,
                                             analytic.Analytic1,
                                             analytic.Analytic2,
                                             Environment.NewLine));
                }
                csv.Append(Environment.NewLine);
            }

            //shared data packages
            EntityType entityType = EntityType.GetEntityType(typeof(DataPackage));
            Privilege priv = Privilege.Get(entityType, EntityOperationType.Share);
            sql = String.Format("SELECT id FROM activity WHERE id_type={0} AND id_priv={1} AND log_time >= '{2}' AND log_time <= '{3}' AND id_owner NOT IN ({4});",
                                entityType.Id, priv.Id, startdate, enddate, skipedIds);
            dbConnection = context.GetDbConnection();
            reader = context.GetQueryResult(sql, dbConnection);
            ids = new List<int>();
            while (reader.Read()) {
                if (reader.GetValue(0) != DBNull.Value) {
                    ids.Add(reader.GetInt32(0));
                }
            }
            context.CloseQueryResult(reader, dbConnection);
            if (ids.Count > 0) {
                csv.Append(String.Format("Data packages shared between {0} and {1},{2}{3}", startdate, enddate, ids.Count, Environment.NewLine));
                csv.Append("Username,Data package name,Data package creation date, Data package shared date" + Environment.NewLine);
                foreach (int id in ids) {
                    Activity activity = Activity.FromId(context, id);
                    User user = User.FromId(context, activity.OwnerId);
                    DataPackage dp;
                    string dpname = "n/a";
                    string dpdate = "n/a";
                    try{
                        dp = DataPackage.FromId(context, activity.EntityId);
                        dpname = dp.Name;
                        dpdate = dp.CreationTime.ToString("yyyy-MM-dd");
                    }catch(Exception){}
                    csv.Append(string.Format("{0},\"{1}\",{2},{3}{4}", user.Username,dpname.Replace(",","\\,"),dpdate,activity.CreationTime.ToString("yyyy-MM-dd"),Environment.NewLine));
                }
            }
            csv.Append(Environment.NewLine);
        }

        public object Get(GetJobsReportingRequest request){
            var context = TepWebContext.GetWebContext(PagePrivileges.AdminOnly);

            // var startdate = request.startdate.ToString("yyyy-MM-dd");
            // var enddate = request.enddate.ToString("yyyy-MM-dd");

            try {
                context.Open();
                context.LogInfo(this,string.Format("/report/jobs GET startdate='{0}',enddate='{1}'", request.startdate, request.enddate));

                if (string.IsNullOrEmpty(request.filename)) 
                    request.filename = string.Format("{0}-monthly-job-report-{1}-{2}", context.GetConfigValue("siteNameShort"),request.startdate,request.enddate);
                var filename = string.Format("{0}/{1}.csv", context.GetConfigValue("path.files"), request.filename);
                WebServer.Services.ReportServiceTep.GetJobReport(context, filename, request.startdate, request.enddate);

                context.Close();

            } catch (Exception e) {
                context.LogError(this, e.Message, e);
                context.Close();
                throw e;
            }
                        
            return new WebResponseBool(true);
        }


        public static void GetJobReport(IfyContext context, string filename, string startdate, string enddate){
            //generate new file
            var csv = new System.Text.StringBuilder();
            var csvHeader = new System.Text.StringBuilder();
            var csvBody = new System.Text.StringBuilder();            
            var header = context.GetConfigValue("agent-jobreport-headerfile");
            if(string.IsNullOrEmpty(header)) return;
            var headers = header.Split(',');
            var headerLength = headers.Length + 1;
            csvHeader.Append(header);            

            string sql = String.Format("SELECT id from wpsjob WHERE created_time > '{0}' AND created_time < '{1}';", startdate, enddate);
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
                                csvBody.Append('"' + (user != null ? user.Affiliation : "") + '"').Append(",");                                
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
                            string wpsname = "";
                                if (!string.IsNullOrEmpty(job.WpsName))
                                    wpsname = job.WpsName;                                    
                                else {                                    
                                    try {
                                        WpsProcessOffering wps = CloudWpsFactory.GetWpsProcessOffering(context, job.ProcessId);
                                        wpsname = wps.Name;
                                    } catch (Exception) {
                                        wpsname = job.ProcessId;
                                    }                                    
                                }
                                if(job.Parameters != null){
                                    bool tio = false;
                                    foreach(var param in job.Parameters) {
                                        if(param.Key == "do_invertion" && param.Value == "true"){
                                            tio = true;
                                            break;
                                        }
                                    }
                                    if(tio) wpsname += "_TIO";
                                }
                                csvBody.Append('"' + wpsname + '"').Append(",");
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
                                csvBody.Append('"' + stack + '"').Append(",");
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
                csvBody.Append(Environment.NewLine);
            }
            csv.Append(csvHeader).Append(Environment.NewLine).Append(csvBody);            
            System.IO.File.WriteAllText(filename, csv.ToString());  
        }
    }

    public class ReportAnalytic : IComparable<ReportAnalytic> {
        public string name { get; set; }
        public int Total { get; set; }
        public int Analytic1 { get; set; }
        public int Analytic2 { get; set; }

        int IComparable<ReportAnalytic>.CompareTo(ReportAnalytic other) {
            if (other == null)
                return 1;
            else if (this.Total != other.Total)
                return this.Total.CompareTo(other.Total);
            else if (this.Analytic1 != other.Analytic1)
                return this.Analytic1.CompareTo(other.Analytic1);
            else if (this.Analytic2 != other.Analytic2)
                return this.Analytic2.CompareTo(other.Analytic2);
            else return 1;
        }
    }
}

