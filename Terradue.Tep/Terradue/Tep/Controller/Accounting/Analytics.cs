using System;
using System.Collections.Generic;
using System.Linq;
using Terradue.Portal;

namespace Terradue.Tep {

    public class Analytics {

        private IfyContext Context;
        private Entity Entity;

        /// <summary>
        /// Gets or sets the collection queries count.
        /// </summary>
        /// <value>The collection queries count.</value>
        public int CollectionQueriesCount { get; set; }

        /// <summary>
        /// Gets or sets the data package created count.
        /// </summary>
        /// <value>The data package created count.</value>
        public int DataPackageCreatedCount { get; set; }

        /// <summary>
        /// Gets or sets the data package load count.
        /// </summary>
        /// <value>The data package load count.</value>
        public int DataPackageLoadCount { get; set; }

        /// <summary>
        /// Gets or sets the data package items load count.
        /// </summary>
        /// <value>The data package items load count.</value>
        public int DataPackageItemsLoadCount { get; set; }

        /// <summary>
        /// Gets or sets the wps job submitted count.
        /// </summary>
        /// <value>The wps job submitted count.</value>
        public int WpsJobSubmittedCount { get; set; }

        /// <summary>
        /// Gets or sets the wps job success count.
        /// </summary>
        /// <value>The wps job success count.</value>
        public int WpsJobSuccessCount { get; set; }

        /// <summary>
        /// Gets or sets the wps job failed count.
        /// </summary>
        /// <value>The wps job failed count.</value>
        public int WpsJobFailedCount { get; set; }

        /// <summary>
        /// Gets or sets the wps job ongoing count.
        /// </summary>
        /// <value>The wps job ongoing count.</value>
        public int WpsJobOngoingCount { get; set; }

        /// <summary>
        /// Gets or sets the icon URL.
        /// </summary>
        /// <value>The icon URL.</value>
        public string IconUrl { get; set; }

        /// <summary>
        /// Gets or sets the startdate.
        /// </summary>
        /// <value>The startdate.</value>
        private string startdate { get; set; }

        /// <summary>
        /// Gets or sets the enddate.
        /// </summary>
        /// <value>The enddate.</value>
        private string enddate { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="T:Terradue.Tep.Analytics"/> analyse jobs.
        /// </summary>
        /// <value><c>true</c> if analyse jobs; otherwise, <c>false</c>.</value>
        public bool AnalyseJobs { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="T:Terradue.Tep.Analytics"/> analyse data packages.
        /// </summary>
        /// <value><c>true</c> if analyse data packages; otherwise, <c>false</c>.</value>
        public bool AnalyseDataPackages { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="T:Terradue.Tep.Analytics"/> analyse collections.
        /// </summary>
        /// <value><c>true</c> if analyse collections; otherwise, <c>false</c>.</value>
        public bool AnalyseCollections { get; set; }

        /// <summary>
        /// Gets or sets the skip identifiers.
        /// </summary>
        /// <value>The skip identifiers.</value>
        public List<int> SkipIds { get; set; }

        /*-----------------------------------------------------------------------------------------------------------------------------------------*/
        /*-----------------------------------------------------------------------------------------------------------------------------------------*/
        /*-----------------------------------------------------------------------------------------------------------------------------------------*/

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Terradue.Tep.Analytics"/> class.
        /// </summary>
        /// <param name="context">Context.</param>
        /// <param name="entity">Entity.</param>
        public Analytics(IfyContext context, Entity entity) {
            this.Context = context;
            this.Entity = entity;
            this.AnalyseJobs = true;
            this.AnalyseDataPackages = true;
            this.AnalyseCollections = true;
            this.SkipIds = new List<int>();
        }

        public void Load(string startdate = null, string enddate = null) {
            this.startdate = startdate;
            this.enddate = enddate;

            if (Entity is UserTep) {
                var user = Entity as UserTep;
                AddUserAnalytics(user);
                IconUrl = user.GetAvatar();
            } else if (Entity is Domain) {
                var domain = Entity as Domain;
                //get all users of domain
                var roles = new EntityList<Role>(Context);
                roles.Load();
                foreach (var role in roles) {
                    if (role.Identifier != RoleTep.PENDING) {
                        var usersIds = role.GetUsers(domain.Id).ToList();
                        if (usersIds.Count > 0) {
                            foreach (var usrId in usersIds) {
                                if (!SkipIds.Contains(usrId)) {
                                    var user = UserTep.FromId(Context, usrId);
                                    AddUserAnalytics(user);
                                }
                            }
                        }
                    }
                }
                IconUrl = domain.IconUrl;
            } else if (Entity is Group) { 
                var group = Entity as Group;
                foreach (var user in group.GetUsers()) {
                    if (!SkipIds.Contains(user.Id)) {
                        AddUserAnalytics((UserTep)user);
                    }
                }
                IconUrl = "http://upload.wikimedia.org/wikipedia/commons/thumb/b/b9/Group_font_awesome.svg/512px-Group_font_awesome.svg.png";
            } else if (Entity is Service){
                var service = Entity as Service;
                AddServiceAnalytics(service);
            }
        }

        private void AddUserAnalytics(UserTep user) {

            //collection analytics
            if (this.AnalyseCollections) {
                CollectionQueriesCount += GetCollectionQueries(user.TerradueCloudUsername);
            }

            //data package analytics
            if (this.AnalyseDataPackages) {
                var dpActivities = GetDataPackageActivities(user);
                DataPackageLoadCount += dpActivities.Count;
                foreach (var dpa in dpActivities) {
                    var nvc = dpa.GetParams();
                    int itemsCount = nvc["items"] != null ? Int32.Parse(nvc["items"]) : 0;
                    DataPackageItemsLoadCount += itemsCount;
                }
                DataPackageCreatedCount += GetDataPackageCreatedCount(user.Id, startdate, enddate);
            }

            //wps jobs analytics
            if (this.AnalyseJobs) {
                WpsJobSuccessCount += GetTotalWpsJobsSucceeded(user.Id, startdate, enddate);
                WpsJobFailedCount += GetTotalWpsJobsFailed(user.Id, startdate, enddate);
                WpsJobOngoingCount += GetTotalWpsJobsOngoing(user.Id, startdate, enddate);
                WpsJobSubmittedCount = WpsJobSuccessCount + WpsJobFailedCount + WpsJobOngoingCount;
            }
        }

        private int GetCollectionQueries(string username) {
            return 0;
        }

        private List<ActivityTep> GetDataPackageActivities(User user) {
            var etype = EntityType.GetEntityType(typeof(DataPackage));
            var priv = Privilege.Get(EntityType.GetEntityTypeFromId(etype.Id), Privilege.GetOperationType(((char)EntityOperationType.View).ToString()));
            EntityList<ActivityTep> activities = new EntityList<ActivityTep>(Context);
            activities.SetFilter("UserId", user.Id + "");
            activities.SetFilter("EntityTypeId", etype.Id + "");
            activities.SetFilter("PrivilegeId", priv.Id + "");
            activities.Load();
            return activities.GetItemsAsList();
        }

        private static string GetWpsjobCreationDateCondition(string startdate, string enddate){
            var result = "";
            if (!string.IsNullOrEmpty(startdate)) result += " AND wpsjob.created_time > '" + startdate + "'";
            if (!string.IsNullOrEmpty(startdate)) result += " AND wpsjob.created_time < '" + enddate + "'";
            return result;
        }

        private int GetWpsJobsForUser(int usrId, string statusCondition, string startdate = null, string enddate = null) {
            string sql = string.Format("SELECT COUNT(*) FROM wpsjob WHERE id_usr={0} AND status {1}{2};", usrId, statusCondition, GetWpsjobCreationDateCondition(startdate, enddate));
            return Context.GetQueryIntegerValue(sql);
        }

        private int GetTotalWpsJobs(int usrId, string startdate = null, string enddate = null) {
            return GetWpsJobsForUser(usrId, string.Format("NOT IN ({0})", (int)WpsJobStatus.NONE), startdate, enddate);
        }

        private int GetTotalWpsJobsSucceeded(int usrId, string startdate = null, string enddate = null) {
            return GetWpsJobsForUser(usrId, string.Format("IN ({0})", (int)WpsJobStatus.SUCCEEDED + "," + (int)WpsJobStatus.STAGED + "," + (int)WpsJobStatus.COORDINATOR), startdate, enddate);
        }

        private int GetTotalWpsJobsFailed(int usrId, string startdate = null, string enddate = null) {
            return GetWpsJobsForUser(usrId, string.Format("IN ({0})", (int)WpsJobStatus.FAILED), startdate, enddate);
        }

        private int GetTotalWpsJobsOngoing(int usrId, string startdate = null, string enddate = null) {
            return GetWpsJobsForUser(usrId, string.Format("IN ({0})", (int)WpsJobStatus.ACCEPTED + "," + (int)WpsJobStatus.PAUSED + "," + (int)WpsJobStatus.STARTED), startdate, enddate);
        }

        private int GetWpsJobsForService(string serviceIdentifier, string statusCondition, string startdate = null, string enddate = null) {
            string sql = string.Format("SELECT COUNT(*) FROM wpsjob WHERE process='{0}' AND id_usr NOT IN ({1}) AND status {2}{3};", serviceIdentifier, string.Join(",",SkipIds), statusCondition, GetWpsjobCreationDateCondition(startdate, enddate));
            return Context.GetQueryIntegerValue(sql);
        }

        private int GetTotalWpsJobsForService(string serviceIdentifier, string startdate = null, string enddate = null) {
            return GetWpsJobsForService(serviceIdentifier, string.Format("NOT IN ({0})", (int)WpsJobStatus.NONE), startdate, enddate);
        }

        private int GetTotalWpsJobsSucceededForService(string serviceIdentifier, string startdate = null, string enddate = null) {
            return GetWpsJobsForService(serviceIdentifier, string.Format("IN ({0})", (int)WpsJobStatus.SUCCEEDED + "," + (int)WpsJobStatus.STAGED + "," + (int)WpsJobStatus.COORDINATOR), startdate, enddate);
        }

        private int GetTotalWpsJobsFailedForService(string serviceIdentifier, string startdate = null, string enddate = null) {
            return GetWpsJobsForService(serviceIdentifier, string.Format("IN ({0})", (int)WpsJobStatus.FAILED), startdate, enddate);
        }

        private int GetTotalWpsJobsOngoingForService(string serviceIdentifier, string startdate = null, string enddate = null) {
            return GetWpsJobsForService(serviceIdentifier, string.Format("IN ({0})", (int)WpsJobStatus.ACCEPTED + "," + (int)WpsJobStatus.PAUSED + "," + (int)WpsJobStatus.STARTED), startdate, enddate);
        }

        private int GetDataPackageCreatedCount(int usrId, string startdate = null, string enddate = null){
            string sql = string.Format("SELECT COUNT(*) FROM resourceset WHERE kind=0 AND id_usr={0}{1};", usrId, GetWpsjobCreationDateCondition(startdate, enddate));
            return Context.GetQueryIntegerValue(sql);
        }

        /// <summary>
        /// Adds the service analytics.
        /// </summary>
        /// <param name="service">Service.</param>
        public void AddServiceAnalytics(Service service){
            WpsJobSuccessCount += GetTotalWpsJobsSucceededForService(service.Identifier, startdate, enddate);
            WpsJobFailedCount += GetTotalWpsJobsFailedForService(service.Identifier, startdate, enddate);
            WpsJobOngoingCount += GetTotalWpsJobsOngoingForService(service.Identifier, startdate, enddate);
            WpsJobSubmittedCount = WpsJobSuccessCount + WpsJobFailedCount + WpsJobOngoingCount;
        }



    }
}
