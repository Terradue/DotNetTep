using System;
using System.Collections.Generic;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface;
using Terradue.OpenNebula;
using Terradue.Portal;
using Terradue.Tep.WebServer;
using Terradue.WebService.Model;

namespace Terradue.Tep.WebServer.Services {
    [Api("Tep Terradue webserver")]
    [Restrict(EndpointAttributes.InSecure | EndpointAttributes.InternalNetworkAccess | EndpointAttributes.Json,
              EndpointAttributes.Secure | EndpointAttributes.External | EndpointAttributes.Json)]
    public class RoleServiceTep : ServiceStack.ServiceInterface.Service {

        private static readonly log4net.ILog log = log4net.LogManager.GetLogger
            (System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        
        /// <summary>
        /// Get the specified request.
        /// </summary>
        /// <param name="request">Request.</param>
        public object Get(RoleGetRequest request) {
            WebRole result;

            var context = TepWebContext.GetWebContext(PagePrivileges.AdminOnly);
            try {
                context.Open();
                context.LogInfo(this,string.Format("/role/{{Id}} GET Id='{0}'", request.Id));
                Role role = Role.FromId(context, request.Id);
                result = new WebRole(role, true);

                context.Close();
            } catch (Exception e) {
                context.LogError(this, e.Message, e);
                context.Close();
                throw e;
            }
            return result;
        }

        /// <summary>
        /// Get the specified request.
        /// </summary>
        /// <param name="request">Request.</param>
        public object Get(RolesGetRequest request) {
            List<WebRole> result = new List<WebRole>();

            var context = TepWebContext.GetWebContext(PagePrivileges.EverybodyView);
            try {
                context.Open();
                context.LogInfo(this,string.Format("/role GET"));
                EntityList<Role> roles = new EntityList<Role>(context);
                roles.Load();
                foreach(Role g in roles) result.Add(new WebRole(g,true));

                context.Close();
            } catch (Exception e) {
                context.LogError(this, e.Message, e);
                context.Close();
                throw e;
            }
            return result;
        }

        /// <summary>
        /// Put the specified request.
        /// </summary>
        /// <param name="request">Request.</param>
        public object Put(RoleUpdateRequest request) {
            var context = TepWebContext.GetWebContext(PagePrivileges.AdminOnly);
            WebRole result;
            try {
                context.Open();
                context.LogInfo(this,string.Format("/role PUT Id='{0}'", request.Id));
                Role role = Role.FromId(context, request.Id);
                role = request.ToEntity(context, role);
                role.Store();
                context.LogDebug(this,string.Format("Role {0} updated by user {1}", role.Identifier, User.FromId(context, context.UserId).Username));
                result = new WebRole(role,true);
                context.Close();
            } catch (Exception e) {
                context.LogError(this, e.Message, e);
                context.Close();
                throw e;
            }
            return result;
        }

        /// <summary>
        /// Post the specified request.
        /// </summary>
        /// <param name="request">Request.</param>
        public object Post(RoleCreateRequest request)
        {
            var context = TepWebContext.GetWebContext(PagePrivileges.AdminOnly);
            WebRole result;
            try{
                context.Open();
                Role role = (request.Id == 0 ? null : Role.FromId(context, request.Id));
				role = request.ToEntity(context, role);
                role.Store();
                result = new WebRole(role);
                context.LogInfo(this,string.Format("/role POST Id='{0}'", request.Id));
                context.LogDebug(this,string.Format("Role {0} created by user {1}", role.Identifier, User.FromId(context, context.UserId).Username));
                context.Close ();
            }catch(Exception e) {
                context.LogError(this, e.Message, e);
                context.Close ();
                throw e;
            }
            return result;
        }

        /// <summary>
        /// Delete the specified request.
        /// </summary>
        /// <param name="request">Request.</param>
        public object Delete(RoleDeleteRequest request) {
            var context = TepWebContext.GetWebContext(PagePrivileges.AdminOnly);
            try {
                context.Open();
                context.LogInfo(this,string.Format("/role/{{Id}} DELETE Id='{0}'", request.Id));
                Role role = Role.FromId(context, request.Id);
                if (context.UserLevel == UserLevel.Administrator) role.Delete();
                else throw new UnauthorizedAccessException(CustomErrorMessages.ADMINISTRATOR_ONLY_ACTION);
                context.LogDebug(this,string.Format("Role {0} deleted by user {1}", role.Identifier, User.FromId(context, context.UserId).Username));
                context.Close();
            } catch (Exception e) {
                context.LogError(this, e.Message, e);
                context.Close();
                throw e;
            }
            return new WebResponseBool(true);
        }

        /// <summary>
        /// Put the specified request.
        /// </summary>
        /// <param name="request">Request.</param>
        public object Put (RoleUpdatePrivilegesRequest request) {
            var context = TepWebContext.GetWebContext (PagePrivileges.AdminOnly);
            WebRole result;
            try {
                context.Open ();
                if (request.Privileges == null) throw new Exception ("Invalid list of privileges");
                context.LogInfo (this, string.Format ("/role/priv PUT Id='{0}'", request.Id));

                Role role = Role.FromId (context, request.Id);
                role.IncludePrivileges (request.Privileges, true);
                result = new WebRole (role);
                context.Close ();
            } catch (Exception e) {
                context.LogError(this, e.Message, e);
                context.Close ();
                throw e;
            }
            return result;
        }

        public object Get (RolesGrantGetRequest request) {
            List<WebRoleGrant> result = new List<WebRoleGrant> ();
            var context = TepWebContext.GetWebContext (PagePrivileges.AdminOnly);
            try {
                context.Open ();
                context.LogInfo (this, string.Format ("/role/grant GET"));

                Domain domain = Domain.FromId (context, request.DomainId);
                var webdomain = new WebDomain (domain);
                EntityList<Role> roles = new EntityList<Role> (context);
                roles.Load ();

                foreach (var role in roles) {
                    var webrole = new WebRole (role);
                    //get users
                    var usrs = role.GetUsers (domain.Id);
                    foreach (var usrid in usrs) {
                        var webuser = new WebUser (User.FromId(context, usrid));
                        result.Add (new WebRoleGrant {
                            Domain = webdomain,
                            Role = webrole,
                            User = webuser
                        });
                    }
                    //get groups
                    var grps = role.GetGroups (domain.Id);
                    foreach (var grpid in grps) {
                        var webgroup = new WebGroup (Group.FromId (context, grpid));
                        result.Add (new WebRoleGrant {
                            Domain = webdomain,
                            Role = webrole,
                            Group = webgroup
                        });
                    }
                }

                context.Close ();
            } catch (Exception e) {
                context.LogError(this, e.Message, e);
                context.Close ();
                throw e;
            }
            return result;
        }

        /// <summary>
        /// Post the specified request.
        /// </summary>
        /// <param name="request">Request.</param>
        public object Post (RoleGrantRequest request)
        {
            var context = TepWebContext.GetWebContext (PagePrivileges.AdminOnly);
            try {
                context.Open ();
                context.LogInfo (this, string.Format ("/role/grant POST RoleId='{0}', UserId='{1}', GroupId='{2}', DomainId='{3}'", request.RoleId, request.UserId, request.GroupId, request.DomainId));
                Role role = Role.FromId (context, request.RoleId);
                Domain domain = request.DomainId != 0 ? Domain.FromId (context, request.DomainId) : null;
                if (request.UserId != 0) {
                    if (request.GroupId != 0) throw new Exception ("Select only one amongst User and Group");
                    User usr = User.FromId (context, request.UserId);
                    role.GrantToUser (usr, domain);
                    context.LogDebug (this, string.Format ("Role {0} granted for user {1} for domain {2}", role.Identifier, usr.Username, domain != null ? domain.Name : "n/a"));
                } else if (request.GroupId != 0) { 
                    if (request.UserId != 0) throw new Exception ("Select only one amongst User and Group");
                    Group grp = Group.FromId (context, request.GroupId);
                    role.GrantToGroup (grp, domain);
                    context.LogDebug (this, string.Format ("Role {0} granted for group {1} for domain {2}", role.Identifier, grp.Identifier, domain != null ? domain.Name : "n/a"));
                } else throw new Exception ("Select one amongst User and Group");

                context.Close ();
            } catch (Exception e) {
                context.LogError(this, e.Message, e);
                context.Close ();
                throw e;
            }
            return new WebResponseBool(true);
        }

        /// <summary>
        /// Delete the specified request.
        /// </summary>
        /// <param name="request">Request.</param>
        public object Delete (RoleGrantDeleteRequest request)
        {
            var context = TepWebContext.GetWebContext (PagePrivileges.AdminOnly);
            try {
                context.Open ();
                context.LogInfo (this, string.Format ("/role/grant DELETE RoleId='{0}', UserId='{1}', GroupId='{2}', DomainId='{3}'", request.RoleId, request.UserId, request.GroupId, request.DomainId));
                Role role = Role.FromId (context, request.RoleId);
                Domain domain = request.DomainId != 0 ? Domain.FromId (context, request.DomainId) : null;
                if (request.UserId != 0) {
                    if (request.GroupId != 0) throw new Exception ("Select only one amongst User and Group");
                    User usr = User.FromId (context, request.UserId);
                    role.RevokeFromUser (usr, domain);
                    context.LogDebug (this, string.Format ("Role {0} ungranted for user {1} for domain {2}", role.Identifier, usr.Username, domain != null ? domain.Name : "n/a"));
                } else if (request.GroupId != 0) {
                    if (request.UserId != 0) throw new Exception ("Select only one amongst User and Group");
                    Group grp = Group.FromId (context, request.GroupId);
                    role.RevokeFromGroup (grp, domain);
                    context.LogDebug (this, string.Format ("Role {0} ungranted for group {1} for domain {2}", role.Identifier, grp.Identifier, domain != null ? domain.Name : "n/a"));
                } else throw new Exception ("Select one amongst User and Group");

                context.Close ();
            } catch (Exception e) {
                context.LogError(this, e.Message, e);
                context.Close ();
                throw e;
            }
            return new WebResponseBool (true);
        }

    }
}

