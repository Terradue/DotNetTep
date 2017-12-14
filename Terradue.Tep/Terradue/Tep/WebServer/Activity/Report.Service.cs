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
    }

    [Route("/reports", "GET", Summary = "GET existing reports", Notes = "")]
    public class ReportsGetRequest : IReturn<List<string>>{}

    [Route("/report", "DELETE", Summary = "GET report", Notes = "")]
    public class ReportDeleteRequest : IReturn<WebResponseBool>{
        [ApiMember(Name = "filename", Description = "filename", ParameterType = "query", DataType = "string", IsRequired = true)]
        public string Filename { get; set; }
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
                context.LogError(this, e.Message);
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
                context.LogError(this, e.Message);
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

                System.IO.File.WriteAllText(string.Format("{2}files/GEP-report-{0}-{1}.csv",startdate,enddate,path), csv.ToString());

                context.LogDebug(this,string.Format("Get report {1}-{2} (user Id = {0})", context.UserId, request.startdate, request.enddate));

                context.Close();

            } catch (Exception e) {
                context.LogError(this, e.Message);
                context.Close();
                throw e;
            }
            Response.AddHeader("Content-Disposition", string.Format("attachment;filename=GEP-report-{0}-{1}.csv",startdate,enddate));
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
            csv.Append("GEP statistics reporting" + Environment.NewLine);
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

        private List<int> GetActiveUsers(IfyContext context, string startdate, string enddate, string skipedIds){
            List<int> ids = new List<int>();

            string sql = string.Format("SELECT DISTINCT usr.id FROM usr WHERE " +
                               "id NOT IN (SELECT id_usr FROM usrsession WHERE usrsession.log_time < '{0}' ) " +
                                       "AND id IN (SELECT id_usr FROM usrsession WHERE usrsession.log_time <= '{1}'){2};",
                                       startdate, enddate, string.IsNullOrEmpty(skipedIds) ? "" : " AND id NOT IN ("+skipedIds+")");
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

            List<int> ids = GetActiveUsers(context, startdate, enddate, skipedIds);

            System.Data.IDbConnection dbConnection;
            System.Data.IDataReader reader;

            csv.Append(String.Format("Users signed in between {0} and {1},{2}{3}", startdate, enddate, ids.Count, Environment.NewLine));
            if (ids.Count > 0) {
                csv.Append("Username,Name,Affiliation,First Login date,Terradue cloud username" + Environment.NewLine);
                foreach (int id in ids) {
                    sql = String.Format("SELECT usr.username, CONCAT(usr.firstname,' ',usr.lastname), usr.affiliation, usrsession.log_time, usr_cloud.username FROM usrsession " +
                                        "INNER JOIN usr ON usr.id=usrsession.id_usr " +
                                        "INNER JOIN usr_cloud ON usr_cloud.id = usr.id " +
                                        "WHERE usrsession.id_usr = {0} ORDER BY usrsession.log_time ASC LIMIT 1;", id);
                    dbConnection = context.GetDbConnection();
                    reader = context.GetQueryResult(sql, dbConnection);
                    if (reader.Read()) {
                        if (reader.GetValue(0) != DBNull.Value) {
                            var name = reader.GetValue(1) != DBNull.Value ? reader.GetString(1) : "not set";
                            var aff = reader.GetValue(2) != DBNull.Value ? reader.GetString(2) : "not set";
                            var cloud = reader.GetValue(4) != DBNull.Value ? reader.GetString(4) : "not set";
                            csv.Append(String.Format("{0},{1},{2},{3},{4}{5}", reader.GetString(0), name, aff.Replace(",","\\,"), reader.GetDateTime(3), cloud, Environment.NewLine));
                        }
                    }
                    context.CloseQueryResult(reader, dbConnection);
                }
            }
            csv.Append(Environment.NewLine);

            //Active users
            sql = string.Format("SELECT usr.username, CONCAT(usr.firstname,' ',usr.lastname), COUNT(usrsession.id_usr),usr.affiliation FROM usr INNER JOIN usrsession ON usr.id=usrsession.id_usr " +
                                "WHERE usrsession.log_time > '{0}' AND usrsession.log_time < '{1}' AND usrsession.id_usr NOT IN ({2}) GROUP BY usrsession.id_usr HAVING COUNT(*) >1 " +
                                "ORDER BY COUNT(usrsession.id_usr) DESC;", 
                                startdate, enddate, skipedIds);
            dbConnection = context.GetDbConnection();
            reader = context.GetQueryResult(sql, dbConnection);
            string s = "";
            int nbActU = 0;
            while (reader.Read()) {
                if (reader.GetValue(0) != DBNull.Value) {
                    nbActU ++;
                    var name = reader.GetValue(1) != DBNull.Value ? reader.GetString(1) : "";
                    var aff = reader.GetValue(3) != DBNull.Value ? reader.GetString(3) : "";
                    s += String.Format("{0},{1},{2},{3}{4}",reader.GetString(0), name, aff, reader.GetInt32(2), Environment.NewLine);
                }
            }
            context.CloseQueryResult(reader, dbConnection);
            csv.Append(String.Format("Active users (logged more than once) between {0} and {1},{2}{3}", startdate, enddate, nbActU, Environment.NewLine));
            if (nbActU > 0) {
                csv.Append("Username,Name,Affiliation,Nb of logins" + Environment.NewLine);
                csv.Append(s);
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
                csv.Append("Name,Owner,Creation date,Process name,Status,Nb of results,Shared" + Environment.NewLine);
                foreach (WpsJob job in jobs) {
                    User usr = User.FromId(context, job.OwnerId);
                    string wpsname = "";
                    try {
                        WpsProcessOffering wps = CloudWpsFactory.GetWpsProcessOffering(context, job.ProcessId);
                        wpsname = wps.Name;
                    } catch (Exception) {
                        wpsname = job.ProcessId;
                    }
                    csv.Append(String.Format("{0},{1},{2},{3},{4},{5},{6}{7}",
                                             job.Name.Replace(",", "\\,"), usr.Username, job.CreatedTime.ToString("yyyy-MM-dd"), wpsname.Replace(",", "\\,"), job.StringStatus, job.GetResultCount(), (job.IsPrivate() ? "no" : "yes"), Environment.NewLine));
                }
                csv.Append(Environment.NewLine);
            }

            //Nb of wpsjobs per user
            List<int> idsUsr = GetActiveUsers(context, startdate, enddate, skipedIds);
            if (idsUsr.Count > 0) {
                csv.Append(string.Format("Number of wpsjob created per user between {0} and {1}{2}", startdate, enddate, Environment.NewLine));
                csv.Append("Username,Total,Succeeded,Failed" + Environment.NewLine);
                foreach (var id in idsUsr) {
                    User usr = User.FromId(context, id);
                    Analytics analytic = new Analytics(context, usr);
                    analytic.AnalyseCollections = false;
                    analytic.AnalyseDataPackages = false;
                    analytic.Load(startdate, enddate);
                    csv.Append(String.Format("{0},{1},{2},{3}{4}",
                                             usr.Username,
                                             analytic.WpsJobSubmittedCount,
                                             analytic.WpsJobSuccessCount,
                                             analytic.WpsJobFailedCount,
                                             Environment.NewLine));
                }
            }

            //Nb of wpsjobs per communities
            var domains = new CommunityCollection(context);
            domains.Load();
            if (domains.Count > 0) {
                csv.Append(string.Format("Number of wpsjob created per community between {0} and {1}{2}", startdate, enddate, Environment.NewLine));
                csv.Append("Community,Total,Succeeded,Failed" + Environment.NewLine);
                foreach (var domain in domains) {
                    Analytics analytic = new Analytics(context, domain);
                    analytic.AnalyseCollections = false;
                    analytic.AnalyseDataPackages = false;
                    analytic.SkipIds = skipedIds.Split(",".ToCharArray()).Select(s => int.Parse(s)).ToList();
                    analytic.Load(startdate, enddate);
                    csv.Append(String.Format("{0},{1},{2},{3}{4}",
                                             domain.Name,
                                             analytic.WpsJobSubmittedCount,
                                             analytic.WpsJobSuccessCount,
                                             analytic.WpsJobFailedCount,
                                             Environment.NewLine));
                }
            }

            //Nb of wpsjobs per group
            var grps = new EntityList<Group>(context);
            grps.Load();
            if (grps.Count > 0) {
                csv.Append(string.Format("Number of wpsjob created per group between {0} and {1}{2}", startdate, enddate, Environment.NewLine));
                csv.Append("Group,Total,Succeeded,Failed" + Environment.NewLine);
                foreach (var grp in grps) {
                    Analytics analytic = new Analytics(context, grp);
                    analytic.AnalyseCollections = false;
                    analytic.AnalyseDataPackages = false;
                    analytic.SkipIds = skipedIds.Split(",".ToCharArray()).Select(s => int.Parse(s)).ToList();
                    analytic.Load(startdate, enddate);
                    csv.Append(String.Format("{0},{1},{2},{3}{4}",
                                             grp.Name,
                                             analytic.WpsJobSubmittedCount,
                                             analytic.WpsJobSuccessCount,
                                             analytic.WpsJobFailedCount,
                                             Environment.NewLine));
                }
            }

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
                foreach (var serviceId in services) {
                    var service = Service.FromIdentifier(context, serviceId);
                    Analytics analytic = new Analytics(context, service);
                    analytic.AnalyseCollections = false;
                    analytic.AnalyseDataPackages = false;
                    analytic.SkipIds = skipedIds.Split(",".ToCharArray()).Select(s => int.Parse(s)).ToList();
                    analytic.Load(startdate, enddate);
                    csv.Append(String.Format("{0},{1},{2},{3}{4}",
                                             service.Name,
                                             analytic.WpsJobSubmittedCount,
                                             analytic.WpsJobSuccessCount,
                                             analytic.WpsJobFailedCount,
                                             Environment.NewLine));
                }
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

            //  Data Packages created + shared
            string sql = String.Format("SELECT resourceset.id, usr.username, resourceset.name, resourceset.creation_time from resourceset INNER JOIN usr on usr.id=resourceset.id_usr " +
                                       "WHERE resourceset.creation_time >= '{0}' AND resourceset.creation_time <= '{1}' AND resourceset.id_usr NOT IN ({2}) AND resourceset.kind=0;",
                                       startdate, enddate, skipedIds);
            System.Data.IDbConnection dbConnection = context.GetDbConnection();
            System.Data.IDataReader reader = context.GetQueryResult(sql, dbConnection);
            List<string> lines = new List<string>();
            List<int> ids = new List<int>();
            while (reader.Read()) {
                if (reader.GetValue(0) != DBNull.Value) {
                    ids.Add(reader.GetInt32(0));
                    lines.Add(string.Format("{0},{1},{2}",reader.GetString(1), reader.GetString(2).Replace(",","\\,"), reader.GetDateTime(3)));
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
            List<int> idsUsr = GetActiveUsers(context, startdate, enddate, skipedIds);
            if (idsUsr.Count > 0) {
                csv.Append(string.Format("Number of Data package created/loaded per user between {0} and {1}{2}", startdate, enddate, Environment.NewLine));
                csv.Append("Username,Data packages created,Data packages loaded,Item loaded" + Environment.NewLine);
                foreach (var id in idsUsr) {
                    User usr = User.FromId(context, id);
                    Analytics analytic = new Analytics(context, usr);
                    analytic.AnalyseCollections = false;
                    analytic.AnalyseJobs = false;
                    analytic.Load(startdate, enddate);
                    csv.Append(String.Format("{0},{1},{2},{3}{4}",
                                             usr.Username,
                                             analytic.DataPackageCreatedCount,
                                             analytic.DataPackageLoadCount,
                                             analytic.DataPackageItemsLoadCount,
                                             Environment.NewLine));
                }
            }

            //Nb of data packages per communities
            var domains = new CommunityCollection(context);
            domains.Load();
            if (domains.Count > 0) {
                csv.Append(string.Format("Number of Data package created per community between {0} and {1}{2}", startdate, enddate, Environment.NewLine));
                csv.Append("Community,Data packages created,Data packages loaded,Item loaded" + Environment.NewLine);
                foreach (var domain in domains) {
                    Analytics analytic = new Analytics(context, domain);
                    analytic.AnalyseCollections = false;
                    analytic.AnalyseJobs = false;
                    analytic.SkipIds = skipedIds.Split(",".ToCharArray()).Select(s => int.Parse(s)).ToList();
                    analytic.Load(startdate, enddate);
                    csv.Append(String.Format("{0},{1},{2},{3}{4}",
                                             domain.Name,
                                             analytic.DataPackageCreatedCount,
                                             analytic.DataPackageLoadCount,
                                             analytic.DataPackageItemsLoadCount,
                                             Environment.NewLine));
                }
            }

            //Nb of data packages per group
            var grps = new EntityList<Group>(context);
            grps.Load();
            if (grps.Count > 0) {
                csv.Append(string.Format("Number of Data package created per group between {0} and {1}{2}", startdate, enddate, Environment.NewLine));
                csv.Append("Group,Data packages created,Data packages loaded,Item loaded" + Environment.NewLine);
                foreach (var grp in grps) {
                    Analytics analytic = new Analytics(context, grp);
                    analytic.AnalyseCollections = false;
                    analytic.AnalyseJobs = false;
                    analytic.SkipIds = skipedIds.Split(",".ToCharArray()).Select(s => int.Parse(s)).ToList();
                    analytic.Load(startdate, enddate);
                    csv.Append(String.Format("{0},{1},{2},{3}{4}",
                                             grp.Name,
                                             analytic.DataPackageCreatedCount,
                                             analytic.DataPackageLoadCount,
                                             analytic.DataPackageItemsLoadCount,
                                             Environment.NewLine));
                }
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
                    string dpname = "not set";
                    string dpdate = "not set";
                    try{
                        dp = DataPackage.FromId(context, activity.EntityId);
                        dpname = dp.Name;
                        dpdate = dp.CreationTime.ToString("yyyy-MM-dd");
                    }catch(Exception){}
                    csv.Append(string.Format("{0},{1},{2},{3}{4}",user.Username,dpname.Replace(",","\\,"),dpdate,activity.CreationTime.ToString("yyyy-MM-dd"),Environment.NewLine));
                }
            }
            csv.Append(Environment.NewLine);
        }
    }
}

