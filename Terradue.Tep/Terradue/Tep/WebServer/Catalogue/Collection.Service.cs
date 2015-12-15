﻿using System;
using System.Collections.Generic;
using ServiceStack.ServiceHost;
using Terradue.Portal;
using Terradue.Tep.WebServer;
using Terradue.WebService.Model;

namespace Terradue.Tep.WebServer.Services {

    [Api("Tep Terradue webserver")]
    [Restrict(EndpointAttributes.InSecure | EndpointAttributes.InternalNetworkAccess | EndpointAttributes.Json,
              EndpointAttributes.Secure | EndpointAttributes.External | EndpointAttributes.Json)]
    public class CollectionServiceTep : ServiceStack.ServiceInterface.Service {
        /// <summary>
        /// Get the specified request.
        /// </summary>
        /// <param name="request">Request.</param>
        public object Get(GetAllSeries request) {
            List<WebSeries> result = new List<WebSeries>();

            IfyWebContext context = TepWebContext.GetWebContext(PagePrivileges.EverybodyView);
            try {
                context.Open();
                EntityList<Series> series = new EntityList<Series>(context);
                series.Load();
                foreach(Series s in series) result.Add(new WebSeries(s));
                context.Close();
            } catch (Exception e) {
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

            IfyWebContext context = TepWebContext.GetWebContext(PagePrivileges.UserView);
            try {
                context.Open();
                Collection serie = Collection.FromId(context, request.Id);
                result = new WebDataCollectionTep(serie);
                context.Close();
            } catch (Exception e) {
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

            IfyWebContext context = TepWebContext.GetWebContext(PagePrivileges.AdminOnly);
            try {
                context.Open();
                Series serie = new Series(context);
                serie = request.ToEntity(context, serie);
                serie.Store();
                serie.StoreGlobalPrivileges();
                result = new WebSeries(serie);
                context.Close();
            } catch (Exception e) {
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

            IfyWebContext context = TepWebContext.GetWebContext(PagePrivileges.AdminOnly);
            try {
                context.Open();
                Series serie = Series.FromId(context, request.Id);

                if(request.Access != null){
                    switch(request.Access){
                        case "public":
                            serie.StoreGlobalPrivileges();
                            break;
                        case "private":
                            serie.RemoveGlobalPrivileges();
                            break;
                        default:
                            break;
                    }
                } else {
                    serie = request.ToEntity(context, serie);
                    serie.Store();
                }

                result = new WebSeries(serie);
                context.Close();
            } catch (Exception e) {
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

            IfyWebContext context = TepWebContext.GetWebContext(PagePrivileges.AdminOnly);
            try {
                context.Open();
                Series serie = Series.FromId(context, request.Id);
                serie.Delete();
                context.Close();
            } catch (Exception e) {
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

            IfyWebContext context = TepWebContext.GetWebContext(PagePrivileges.AdminOnly);
            try {
                context.Open();

                Collection coll = Collection.FromId(context, request.CollId);
                List<int> ids = coll.GetGroupsWithPrivileges();

                List<Group> groups = new List<Group>();
                foreach (int id in ids) groups.Add(Group.FromId(context, id));

                foreach(Group grp in groups){
                    result.Add(new WebGroup(grp));
                }

                context.Close();
            } catch (Exception e) {
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

            IfyWebContext context = TepWebContext.GetWebContext(PagePrivileges.AdminOnly);
            try {
                context.Open();
                Collection serie = Collection.FromId(context, request.CollId);

                List<int> ids = serie.GetGroupsWithPrivileges();

                List<Group> groups = new List<Group>();
                foreach (int id in ids) groups.Add(Group.FromId(context, id));

                foreach(Group grp in groups){
                    if(grp.Id == request.Id) return new WebResponseBool(false);
                }

                serie.StorePrivilegesForGroups(new int[]{request.Id});

                context.Close();
            } catch (Exception e) {
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

            IfyWebContext context = TepWebContext.GetWebContext(PagePrivileges.AdminOnly);
            try {
                context.Open();
                Series serie = Series.FromId(context, request.CollId);

                //TODO: replace once http://project.terradue.com/issues/13954 is resolved
                string sql = String.Format("DELETE FROM series_priv WHERE id_series={0} AND id_grp={1};",request.CollId, request.CollId);
                context.Execute(sql);

                context.Close();
            } catch (Exception e) {
                context.Close();
                throw e;
            }
            return new WebResponseBool(true);
        }

    }
}

