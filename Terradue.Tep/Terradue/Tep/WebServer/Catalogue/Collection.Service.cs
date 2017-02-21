using System;
using System.Collections.Generic;
using ServiceStack.ServiceHost;
using Terradue.Portal;
using Terradue.Tep.WebServer;
using Terradue.WebService.Model;
using System.Linq;

namespace Terradue.Tep.WebServer.Services {

    [Api("Tep Terradue webserver")]
    [Restrict(EndpointAttributes.InSecure | EndpointAttributes.InternalNetworkAccess | EndpointAttributes.Json,
              EndpointAttributes.Secure | EndpointAttributes.External | EndpointAttributes.Json)]
    public class CollectionServiceTep : ServiceStack.ServiceInterface.Service {

        private static readonly log4net.ILog log = log4net.LogManager.GetLogger
            (System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        
        /// <summary>
        /// Get the specified request.
        /// </summary>
        /// <param name="request">Request.</param>
        public object Get(GetAllSeries request) {
            List<WebSeries> result = new List<WebSeries>();

            var context = TepWebContext.GetWebContext(PagePrivileges.EverybodyView);
            try {
                context.Open();
                context.LogInfo(this,string.Format("/data/collection GET"));

                EntityList<Series> series = new EntityList<Series>(context);
                series.Load();
                foreach(Series s in series) result.Add(new WebSeries(s));
                context.Close();
            } catch (Exception e) {
                context.LogError(this, e.Message);
                context.Close();
                throw e;
            }
            return result;
        }

        /// <summary>
        /// Get the specified request.
        /// </summary>
        /// <param name="request">Request.</param>
        public object Get(SerieGetRequestTep request) {
            WebDataCollectionTep result;

            var context = TepWebContext.GetWebContext(PagePrivileges.DeveloperView);
            try {
                context.Open();
                context.LogInfo(this,string.Format("/data/collection/{{Id}} GET Id='{0}'", request.Id));

                Collection serie = Collection.FromId(context, request.Id);
                result = new WebDataCollectionTep(serie);
                context.Close();
            } catch (Exception e) {
                context.LogError(this, e.Message);
                context.Close();
                throw e;
            }
            return result;
        }

        /// <summary>
        /// Post the specified request.
        /// </summary>
        /// <param name="request">Request.</param>
        public object Post(CreateSerie request) {
            WebSeries result;

            var context = TepWebContext.GetWebContext(PagePrivileges.AdminOnly);
            try {
                context.Open();

                Series serie = new Series(context);
                serie = request.ToEntity(context, serie);
                serie.Store();
                serie.GrantPermissionsToAll();

                Activity activity = new Activity(context, serie, EntityOperationType.Create);
                activity.Store();

                context.LogInfo(this,string.Format("/data/collection POST Id='{0}'", serie.Id));

                result = new WebSeries(serie);
                context.Close();
            } catch (Exception e) {
                context.LogError(this, e.Message);
                context.Close();
                throw e;
            }
            return result;
        }

        /// <summary>
        /// Put the specified request.
        /// </summary>
        /// <param name="request">Request.</param>
        public object Put(CollectionUpdateRequestTep request) {
            WebSeries result;

            var context = TepWebContext.GetWebContext(PagePrivileges.AdminOnly);
            try {
                context.Open();
                context.LogInfo(this,string.Format("/data/collection PUT Id='{0}'", request.Id));

                Series serie = Series.FromId(context, request.Id);

                if(request.Access != null){
                    switch(request.Access){
                        case "public":
                            serie.GrantPermissionsToAll();
                            Activity activity = new Activity(context, serie, EntityOperationType.Share);
                            activity.Store();
                            break;
                        case "private":
                            serie.GrantPermissionsToAll();
                            break;
                        default:
                            break;
                    }
                } else {
                    serie = request.ToEntity(context, serie);
                    serie.Store();
                    Activity activity = new Activity(context, serie, EntityOperationType.Change);
                    activity.Store();
                }

                result = new WebSeries(serie);
                context.Close();
            } catch (Exception e) {
                context.LogError(this, e.Message);
                context.Close();
                throw e;
            }
            return result;
        }


        /// <summary>
        /// Delete the specified request.
        /// </summary>
        /// <param name="request">Request.</param>
        public object Delete(DeleteSerie request) {
            WebSeries result;

            var context = TepWebContext.GetWebContext(PagePrivileges.AdminOnly);
            try {
                context.Open();
                context.LogInfo(this,string.Format("/data/collection DELETE Id='{0}'", request.Id));

                Series serie = Series.FromId(context, request.Id);
                serie.Delete();
                Activity activity = new Activity(context, serie, EntityOperationType.Delete);
                activity.Store();
                context.Close();
            } catch (Exception e) {
                context.LogError(this, e.Message);
                context.Close();
                throw e;
            }
            return Get(new GetAllSeries());
        }

        /// <summary>
        /// Get the specified request.
        /// </summary>
        /// <param name="request">Request.</param>
        public object Get(CollectionGetGroupsRequestTep request) {
            List<WebGroup> result = new List<WebGroup>();

            var context = TepWebContext.GetWebContext(PagePrivileges.AdminOnly);
            try {
                context.Open();
                context.LogInfo(this,string.Format("/data/collection/{{collId}}/group GET collId='{0}'", request.CollId));

                Collection coll = Collection.FromId(context, request.CollId);
                List<int> ids = coll.GetAuthorizedGroupIds().ToList();

                List<Group> groups = new List<Group>();
                foreach (int id in ids) groups.Add(Group.FromId(context, id));

                foreach(Group grp in groups){
                    result.Add(new WebGroup(grp));
                }

                context.Close();
            } catch (Exception e) {
                context.LogError(this, e.Message);
                context.Close();
                throw e;
            }
            return result;
        }

        /// <summary>
        /// Post the specified request.
        /// </summary>
        /// <param name="request">Request.</param>
        public object Post(CollectionAddGroupRequestTep request) {

            var context = TepWebContext.GetWebContext(PagePrivileges.AdminOnly);
            try {
                context.Open();
                context.LogInfo(this,string.Format("/data/collection/{{collId}}/group POST collId='{0}',Id='{1}'", request.CollId, request.Id));

                Collection serie = Collection.FromId(context, request.CollId);

                List<int> ids = serie.GetAuthorizedGroupIds().ToList();

                List<Group> groups = new List<Group>();
                foreach (int id in ids) groups.Add(Group.FromId(context, id));

                foreach(Group grp in groups){
                    if(grp.Id == request.Id) return new WebResponseBool(false);
                }

                serie.GrantPermissionsToGroups(new int[]{request.Id});

                context.Close();
            } catch (Exception e) {
                context.LogError(this, e.Message);
                context.Close();
                throw e;
            }
            return new WebResponseBool(true);
        }

        /// <summary>
        /// Post the specified request.
        /// </summary>
        /// <param name="request">Request.</param>
        public object Delete(CollectionDeleteGroupRequestTep request) {
            List<WebGroup> result = new List<WebGroup>();

            var context = TepWebContext.GetWebContext(PagePrivileges.AdminOnly);
            try {
                context.Open();
                context.LogInfo(this,string.Format("/data/collection/{{collId}}/group/{{Id}} DELETE collId='{0}',Id='{1}'", request.CollId, request.Id));

                Series serie = Series.FromId(context, request.CollId);

                //TODO: replace once http://project.terradue.com/issues/13954 is resolved
                string sql = String.Format("DELETE FROM series_perm WHERE id_series={0} AND id_grp={1};",request.CollId, request.Id);
                context.Execute(sql);

                context.Close();
            } catch (Exception e) {
                context.LogError(this, e.Message);
                context.Close();
                throw e;
            }
            return new WebResponseBool(true);
        }

    }
}

