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

    [Route("/reports", "GET", Summary = "GET existing reports", Notes = "")]
    public class ReportsGetRequest : IReturn<List<string>>{}

    [Route("/report", "DELETE", Summary = "GET report", Notes = "")]
    public class ReportDeleteRequest : IReturn<WebResponseBool>{
        [ApiMember(Name = "filename", Description = "filename", ParameterType = "query", DataType = "string", IsRequired = true)]
        public string Filename { get; set; }
    }

    [Route("/report/jobs", "POST", Summary = "GET report", Notes = "")]
    public class GetJobReportingRequest : IReturn<string> {
        [ApiMember(Name = "emails", Description = "user emails", ParameterType = "query", DataType = "List<string>", IsRequired = false)]
        public List<string> Emails { get; set; }

        [ApiMember(Name = "token", Description = "token", ParameterType = "query", DataType = "string", IsRequired = false)]
        public string token { get; set; }

        [ApiMember(Name = "startdate", Description = "start date", ParameterType = "query", DataType = "datetime", IsRequired = false)]
        public DateTime startdate { get; set; }

        [ApiMember(Name = "enddate", Description = "end date", ParameterType = "query", DataType = "datetime", IsRequired = false)]
        public DateTime enddate { get; set; }
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

                csv.Append("Owner Email,Creation date,Process name,Status,Processing time (minutes),Thematic App name, Nb Input" + Environment.NewLine);

                foreach (var email in request.Emails) {
                    var user = User.FromEmail(context, email);

                    //get list of jobs
                    string sql = String.Format("SELECT wpsjob.id from wpsjob WHERE wpsjob.id_usr={0} AND wpsjob.created_time > '{1}' AND wpsjob.created_time < '{2}';", user.Id, startdate, enddate);
                    var ids = context.GetQueryIntegerValues(sql);

                    foreach (var id in ids) {
                        var job = WpsJob.FromId(context, id);

                        //status
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

                        //wps name
                        string wpsname = "";
                        try {
                            WpsProcessOffering wps = CloudWpsFactory.GetWpsProcessOffering(context, job.ProcessId);
                            wpsname = wps.Name;
                        } catch (Exception) {
                            wpsname = job.ProcessId;
                        }

                        //processing time
                        var processingTime = job.EndTime == DateTime.MinValue || job.EndTime < job.CreatedTime ? 0 : (job.EndTime - job.CreatedTime).Minutes;

                        //processed data
                        int totalDataProcessed = 0;
                        if (job.Parameters != null) {
                            foreach (var parameter in job.Parameters) {
                                if (!string.IsNullOrEmpty(parameter.Value) && (parameter.Value.StartsWith("http://") || parameter.Value.StartsWith("https://"))) {
                                    var url = parameter.Value;
                                    totalDataProcessed++;
                                }
                            }
                        }

                        csv.Append(String.Format("{0},{1},\"{2}\",{3},{4},{5},{6}{7}",
                            email,
                            job.CreatedTime.ToString("yyyy-MM-ddTHH:mm:ss"),
                            wpsname.Replace(",", "\\,"),
                            succeeded ? "succeeded" : "failed",
                            processingTime,
                            job.AppIdentifier,
                            totalDataProcessed,
                            Environment.NewLine
                        ));
                    }
                }

            } catch (Exception e) {
                context.LogError(this, e.Message, e);
                context.Close();
                throw e;
            }
            return csv.ToString();
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

