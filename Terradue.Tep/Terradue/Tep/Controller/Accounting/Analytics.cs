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
        }

        public void Load() {
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
                                var user = UserTep.FromId(Context, usrId);
                                AddUserAnalytics(user);
                            }
                        }
                    }
                }
                IconUrl = domain.IconUrl;
            } else if (Entity is Group) { 
                var group = Entity as Group;
                foreach (var user in group.GetUsers()) { 
                    AddUserAnalytics((UserTep)user);
                }
                IconUrl = "http://upload.wikimedia.org/wikipedia/commons/thumb/b/b9/Group_font_awesome.svg/512px-Group_font_awesome.svg.png";
            }
        }

        private void AddUserAnalytics(UserTep user) {

            //collection analytics
            CollectionQueriesCount += GetCollectionQueries(user.TerradueCloudUsername);

            //data package analytics
            var dpActivities = GetDataPackageActivities(user);
            DataPackageLoadCount += dpActivities.Count;
            foreach (var dpa in dpActivities) {
                var nvc = dpa.GetParams();
                int itemsCount = nvc["items"] != null ? Int32.Parse(nvc["items"]) : 0;
                DataPackageItemsLoadCount += itemsCount;
            }

            //wps jobs analytics
            WpsJobSuccessCount = GetTotalWpsJobsSucceeded(user.Id);
            WpsJobFailedCount = GetTotalWpsJobsFailed(user.Id);
            WpsJobOngoingCount = GetTotalWpsJobsOngoing(user.Id);
            WpsJobSubmittedCount = WpsJobSuccessCount + WpsJobFailedCount + WpsJobOngoingCount;
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

        private int GetTotalWpsJobs(User user) {
            string sql = string.Format("SELECT COUNT(*) FROM wpsjob WHERE id_usr={0} AND status NOT IN ({1});", user.Id, (int)WpsJobStatus.NONE);
            return Context.GetQueryIntegerValue(sql);
        }

        private int GetTotalWpsJobsSucceeded(int usrId) {
            string sql = string.Format("SELECT COUNT(*) FROM wpsjob WHERE id_usr={0} AND status IN ({1});", usrId, (int)WpsJobStatus.SUCCEEDED + "," + (int)WpsJobStatus.STAGED + "," + (int)WpsJobStatus.COORDINATOR) ;
            return Context.GetQueryIntegerValue(sql);
        }

        private int GetTotalWpsJobsFailed(int usrId) {
            string sql = string.Format("SELECT COUNT(*) FROM wpsjob WHERE id_usr={0} AND status IN ({1});", usrId, (int)WpsJobStatus.FAILED);
            return Context.GetQueryIntegerValue(sql);
        }

        private int GetTotalWpsJobsOngoing(int usrId) {
            string sql = string.Format("SELECT COUNT(*) FROM wpsjob WHERE id_usr={0} AND status IN ({1});", usrId, (int)WpsJobStatus.ACCEPTED + "," + (int)WpsJobStatus.PAUSED + "," + (int)WpsJobStatus.STARTED);
            return Context.GetQueryIntegerValue(sql);
        }

        //private List<ActivityTep> GetWpsJobsActivities(User user) {
        //    var etype = EntityType.GetEntityType(typeof(WpsJob));
        //    var priv = Privilege.Get(EntityType.GetEntityTypeFromId(etype.Id), Privilege.GetOperationType(((char)EntityOperationType.Create).ToString()));
        //    EntityList<ActivityTep> activities = new EntityList<ActivityTep>(Context);
        //    activities.SetFilter("UserId", user.Id + "");
        //    activities.SetFilter("EntityTypeId", etype.Id + "");
        //    activities.SetFilter("PrivilegeId", priv.Id + "");
        //    activities.Load();
        //    return activities.GetItemsAsList();
        //}



    }
}
