using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Terradue.Portal;

namespace Terradue.Tep {

    public class Analytics {

        private IfyContext Context;
        private Entity Entity;
		private EntityList<UserTep> Users;

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
		private string StartDate { get; set; }

        /// <summary>
        /// Gets or sets the enddate.
        /// </summary>
        /// <value>The enddate.</value>
		private string EndDate { get; set; }

        /// <summary>
        /// Gets or sets the total users.
        /// </summary>
        /// <value>The total users.</value>
		public NameValueCollection TotalUsers { get; set; }

        /// <summary>
        /// Gets or sets the active users.
        /// </summary>
        /// <value>The active users.</value>
        public NameValueCollection ActiveUsers { get; set; }

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
		public Analytics(IfyContext context) {
            this.Context = context;
            this.AnalyseJobs = true;
            this.AnalyseDataPackages = true;
            this.AnalyseCollections = true;
            this.SkipIds = new List<int> { 0 };
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Terradue.Tep.Analytics"/> class.
        /// </summary>
        /// <param name="context">Context.</param>
        /// <param name="entity">Entity.</param>
		public Analytics(IfyContext context, Entity entity) : this(context) {
            this.Entity = entity;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Terradue.Tep.Analytics"/> class.
        /// </summary>
        /// <param name="context">Context.</param>
        /// <param name="users">Users.</param>
		public Analytics(IfyContext context, EntityList<UserTep> users) {
            this.Context = context;
			this.Users = users;
            this.AnalyseJobs = true;
            this.AnalyseDataPackages = true;
            this.AnalyseCollections = true;
            this.SkipIds = new List<int>();
        }

        /// <summary>
        /// Load the specified startdate and enddate.
        /// </summary>
        /// <param name="startdate">Startdate.</param>
        /// <param name="enddate">Enddate.</param>
        public void Load(string startdate = null, string enddate = null) {
            this.StartDate = startdate;
            this.EndDate = enddate;

			if (Entity != null) {
				if (Entity is UserTep) {
					var user = Entity as UserTep;
					AddUserAnalytics(user);
					IconUrl = user.GetAvatar();
                } else if (Entity is Domain || Entity is ThematicCommunity) {
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
                    var allowedIds = GetAllowedIds();
                    ActiveUsers = GetActiveUsers(allowedIds);
                    TotalUsers = GetNewUsers(allowedIds);
					IconUrl = domain.IconUrl;
				} else if (Entity is Group) {
					var group = Entity as Group;
					foreach (var user in group.GetUsers()) {
						if (!SkipIds.Contains(user.Id)) {
							AddUserAnalytics((UserTep)user);
						}
					}
					IconUrl = "http://upload.wikimedia.org/wikipedia/commons/thumb/b/b9/Group_font_awesome.svg/512px-Group_font_awesome.svg.png";
				} else if (Entity is Service) {
					var service = Entity as Service;
					AddServiceAnalytics(service);
				}
			} else if (Users != null){
				foreach(var user in Users){
					AddUserAnalytics(user);
				}
				IconUrl = "http://upload.wikimedia.org/wikipedia/commons/thumb/b/b9/Group_font_awesome.svg/512px-Group_font_awesome.svg.png";
			} else {
				AddGlobalAnalytics();
				IconUrl = "http://upload.wikimedia.org/wikipedia/commons/thumb/b/b9/Group_font_awesome.svg/512px-Group_font_awesome.svg.png";
			}
        }

		private void AddGlobalAnalytics(){
			AddUserAnalytics(null);

            var allowedIds = GetAllowedIds();
            ActiveUsers = GetActiveUsers(allowedIds);
            TotalUsers = GetNewUsers(allowedIds);
		}

        private void AddUserAnalytics(UserTep user) {

			var userId = user != null ? user.Id : 0;
			var userT2name = user != null ? user.TerradueCloudUsername : null;

            //collection analytics
            if (this.AnalyseCollections) {
				CollectionQueriesCount += GetCollectionQueries(userT2name);
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
				DataPackageCreatedCount += GetDataPackageCreatedCount(userId, StartDate, EndDate);
            }

            //wps jobs analytics
            if (this.AnalyseJobs) {
				WpsJobSuccessCount += GetTotalWpsJobsSucceeded(userId, StartDate, EndDate);
				WpsJobFailedCount += GetTotalWpsJobsFailed(userId, StartDate, EndDate);
                WpsJobOngoingCount += GetTotalWpsJobsOngoing(userId, StartDate, EndDate);
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
            if (user != null) activities.SetFilter("UserId", user.Id + "");
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

        private static string GetDataPackagebCreationDateCondition(string startdate, string enddate) {
            var result = "";
            if (!string.IsNullOrEmpty(startdate)) result += " AND resourceset.creation_time > '" + startdate + "'";
            if (!string.IsNullOrEmpty(startdate)) result += " AND resourceset.creation_time < '" + enddate + "'";
            return result;
        }

        private int GetWpsJobsForUser(int usrId, string statusCondition, string startdate = null, string enddate = null) {
			string sql = string.Format("SELECT COUNT(*) FROM wpsjob WHERE {0}status {1}{2};", (usrId != 0 ? "id_usr="+usrId+" AND " : ""), statusCondition, GetWpsjobCreationDateCondition(startdate, enddate));
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
			string sql = string.Format("SELECT COUNT(*) FROM resourceset WHERE kind=0{0}{1};", (usrId != 0 ? " AND id_usr="+usrId : ""), GetDataPackagebCreationDateCondition(startdate, enddate));
            return Context.GetQueryIntegerValue(sql);
        }

        /// <summary>
        /// Adds the service analytics.
        /// </summary>
        /// <param name="service">Service.</param>
        public void AddServiceAnalytics(Service service){
            WpsJobSuccessCount += GetTotalWpsJobsSucceededForService(service.Identifier, StartDate, EndDate);
            WpsJobFailedCount += GetTotalWpsJobsFailedForService(service.Identifier, StartDate, EndDate);
            WpsJobOngoingCount += GetTotalWpsJobsOngoingForService(service.Identifier, StartDate, EndDate);
            WpsJobSubmittedCount = WpsJobSuccessCount + WpsJobFailedCount + WpsJobOngoingCount;
        }

        public List<int> GetAllowedIds(){
            var allowedIds = new List<int>();
            if (Entity != null) {
                if (Entity is UserTep) {
                    var user = Entity as UserTep;
                    allowedIds.Add(user.Id);
                } else if (Entity is Domain || Entity is ThematicCommunity) {
                    var domain = Entity as Domain;
                    //get all users of domain
                    var roles = new EntityList<Role>(Context);
                    roles.Load();
                    foreach (var role in roles) {
                        if (role.Identifier != RoleTep.PENDING) {
                            allowedIds.AddRange(role.GetUsers(domain.Id).ToList());
                        }
                    }
                } else if (Entity is Group) {
                    var group = Entity as Group;
                    foreach (var user in group.GetUsers()) allowedIds.Add(user.Id);
                }
            } else if (Users != null) {
                foreach (var user in Users) {
                    allowedIds.Add(user.Id);
                }
            }
            return allowedIds;
        }

        /// <summary>
        /// Gets the new users.
        /// </summary>
        /// <returns>The new users.</returns>
        /// <param name="allowedIds">Allowed identifiers.</param>
        public NameValueCollection GetNewUsers(List<int> allowedIds) {
            var allowedids = string.Join(",", allowedIds);         
			var skipids = string.Join(",", SkipIds);
            return GetNewUsers(Context, StartDate, EndDate, skipids, allowedids);
		}

        /// <summary>
        /// Gets the new users.
        /// </summary>
        /// <returns>The new users.</returns>
        /// <param name="context">Context.</param>
        /// <param name="startdate">Startdate.</param>
        /// <param name="enddate">Enddate.</param>
        /// <param name="skipids">Skipids.</param>
        /// <param name="allowedids">Allowedids.</param>
        public static NameValueCollection GetNewUsers(IfyContext context, string startdate, string enddate, string skipids, string allowedids) {
            var ids = new NameValueCollection();

            string sql = string.Format("SELECT DISTINCT usr.id,usr.level FROM usr {0}{1}{2};",
                                       !string.IsNullOrEmpty(startdate) && !string.IsNullOrEmpty(enddate) ? " WHERE id NOT IN (SELECT id_usr FROM usrsession WHERE usrsession.log_time < '" + startdate + "' ) AND id IN (SELECT id_usr FROM usrsession WHERE usrsession.log_time <= '" + enddate + "')" : "",
                                       string.IsNullOrEmpty(skipids) ? "" : (string.IsNullOrEmpty(startdate) || string.IsNullOrEmpty(enddate) ? " WHERE " : " AND ") + "id NOT IN (" + skipids + ")",
                                       string.IsNullOrEmpty(allowedids) ? "" : ((string.IsNullOrEmpty(startdate) || string.IsNullOrEmpty(enddate)) && string.IsNullOrEmpty(skipids) ? " WHERE " : " AND ") + " id IN (" + allowedids + ")");
            System.Data.IDbConnection dbConnection = context.GetDbConnection();
            System.Data.IDataReader reader = context.GetQueryResult(sql, dbConnection);
            while (reader.Read()) {
                if (reader.GetValue(0) != DBNull.Value) {
                    var id = reader.GetInt32(0);
                    var level = reader.GetInt32(1);
                    ids.Set(id + "", level + "");
                }
            }
            context.CloseQueryResult(reader, dbConnection);
            return ids;
        }

        /// <summary>
        /// Gets the active users.
        /// </summary>
        /// <returns>The active users.</returns>
        /// <param name="allowedIds">Allowed identifiers.</param>
        public NameValueCollection GetActiveUsers(List<int> allowedIds) {
            var allowedids = string.Join(",", allowedIds);
            var skipids = string.Join(",", SkipIds);
            return GetActiveUsers(Context, StartDate, EndDate, skipids, allowedids);
        }

        /// <summary>
        /// Gets the active users.
        /// </summary>
        /// <returns>The active users.</returns>
        public static NameValueCollection GetActiveUsers(IfyContext context, string startdate, string enddate, string skipids, string allowedids) {
            var ids = new NameValueCollection();

            int activeThreshold = context.GetConfigIntegerValue("report-activeUsers-threshold");
            string sql = string.Format("SELECT usr.id,usr.level FROM usr WHERE " + 
                                       "id IN (SELECT id_usr FROM usrsession{0} GROUP BY id_usr HAVING COUNT(*) > {1}){2}{3};",
                                        !string.IsNullOrEmpty(startdate) && !string.IsNullOrEmpty(enddate) ? " WHERE usrsession.log_time >= '"+startdate+"' AND usrsession.log_time <= '"+enddate+"'" : "",
                                        activeThreshold,
                                        string.IsNullOrEmpty(skipids) ? "" : " AND id NOT IN (" + skipids + ")",
                                        string.IsNullOrEmpty(allowedids) ? "" : " AND id IN (" + allowedids + ")");
            System.Data.IDbConnection dbConnection = context.GetDbConnection();
			System.Data.IDataReader reader = context.GetQueryResult(sql, dbConnection);
            while (reader.Read()) {
                if (reader.GetValue(0) != DBNull.Value) {
                    var id = reader.GetInt32(0);
                    var level = reader.GetInt32(1);
                    ids.Set(id+"", level+"");
                }
            }
            context.CloseQueryResult(reader, dbConnection);
            return ids;
        }

    }
}
