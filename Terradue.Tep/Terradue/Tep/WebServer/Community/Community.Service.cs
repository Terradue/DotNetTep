using System;
using System.Collections.Generic;
using System.Web;
using ServiceStack.Common.Web;
using ServiceStack.ServiceHost;
using Terradue.OpenSearch;
using Terradue.Portal;
using Terradue.WebService.Model;

namespace Terradue.Tep.WebServer.Services{

    [Route ("/community/user", "POST", Summary = "POST the user into the community", Notes = "")]
    public class CommunityAddUserRequestTep : IReturn<WebResponseBool> {
        [ApiMember (Name = "identifier", Description = "Identifier of the community", ParameterType = "query", DataType = "string", IsRequired = true)]
        public string Identifier { get; set; }
        [ApiMember (Name = "username", Description = "Username of the user (current user if null)", ParameterType = "query", DataType = "string", IsRequired = false)]
        public string Username { get; set; }
        [ApiMember (Name = "role", Description = "Role of the user", ParameterType = "query", DataType = "string", IsRequired = false)]
        public string Role { get; set; }
    }

    [Route ("/community/user", "PUT", Summary = "PUT the user into the community", Notes = "")]
    public class CommunityUpdateUserRequestTep : IReturn<WebResponseBool>
    {
        [ApiMember (Name = "identifier", Description = "Identifier of the community", ParameterType = "query", DataType = "string", IsRequired = true)]
        public string Identifier { get; set; }
        [ApiMember (Name = "username", Description = "Username of the user (current user if null)", ParameterType = "query", DataType = "string", IsRequired = true)]
        public string Username { get; set; }
        [ApiMember (Name = "role", Description = "Role of the user", ParameterType = "query", DataType = "string", IsRequired = true)]
        public string Role { get; set; }
    }

    [Route ("/community/user", "DELETE", Summary = "POST the current user into the community", Notes = "")]
    public class CommunityRemoveUserRequestTep : IReturn<WebResponseBool>
    {
        [ApiMember (Name = "identifier", Description = "Identifier of the community", ParameterType = "query", DataType = "string", IsRequired = true)]
        public string Identifier { get; set; }
        [ApiMember (Name = "username", Description = "Username of the user (current user if null)", ParameterType = "query", DataType = "string", IsRequired = false)]
        public string Username { get; set; }
    }

    [Route ("/job/wps/{id}/community/{cid}", "PUT", Summary = "PUT the wpsjob to the community", Notes = "")]
    public class CommunityAddWpsJobRequestTep : IReturn<WebResponseBool>
    {
        [ApiMember (Name = "id", Description = "Id of the wps job", ParameterType = "query", DataType = "string", IsRequired = true)]
        public int Id { get; set; }

        [ApiMember (Name = "cid", Description = "Id of the community", ParameterType = "query", DataType = "string", IsRequired = true)]
        public int CId { get; set; }
    }

    [Route ("/data/package/{id}/community/{cid}", "PUT", Summary = "PUT the data package to the community", Notes = "")]
    public class CommunityAddDataPackageRequestTep : IReturn<WebResponseBool>
    {
        [ApiMember (Name = "id", Description = "Id of the data package", ParameterType = "query", DataType = "string", IsRequired = true)]
        public int Id { get; set; }

        [ApiMember (Name = "cid", Description = "Id of the community", ParameterType = "query", DataType = "string", IsRequired = true)]
        public int CId { get; set; }
    }

    [Api ("Tep Terradue webserver")]
    [Restrict (EndpointAttributes.InSecure | EndpointAttributes.InternalNetworkAccess | EndpointAttributes.Json,
             EndpointAttributes.Secure | EndpointAttributes.External | EndpointAttributes.Json)]
    public class CommunityServiceTep : ServiceStack.ServiceInterface.Service
    {

        private static readonly log4net.ILog log = log4net.LogManager.GetLogger
            (System.Reflection.MethodBase.GetCurrentMethod ().DeclaringType);

        /// <summary>
        /// Post the specified request.
        /// </summary>
        /// <param name="request">Request.</param>
        public object Post (CommunityAddUserRequestTep request) {
            var context = TepWebContext.GetWebContext (PagePrivileges.DeveloperView);

            try {
                context.Open ();
                if (string.IsNullOrEmpty (request.Identifier)) throw new Exception ("Invalid request - missing community identifier");

                User user = string.IsNullOrEmpty(request.Username) ? User.FromId (context, context.UserId) : User.FromUsername(context, request.Username);
                Role role = Role.FromIdentifier (context, string.IsNullOrEmpty (request.Role) ? ThematicGroup.MEMBER : request.Role);
                context.LogInfo (this, string.Format ("/community/user POST Identifier='{0}', Username='{1}', Role='{2}'", request.Identifier, user.Username, role.Identifier));

                Domain domain = Domain.FromIdentifier (context, request.Identifier);

                if (string.IsNullOrEmpty (request.Username)) { 
                    //current user must be manager of the domain
                    var roles = Role.GetUserRolesForDomain (context, context.UserId, domain.Id);
                    bool ismanager = false;
                    foreach (var r in roles) {
                        if (r.Identifier.Equals (ThematicGroup.MANAGER)) {
                            ismanager = true;
                            break;
                        }
                    }
                    if (!ismanager) throw new UnauthorizedAccessException ("Action only allowed to manager of the domain");
                }

                role.GrantToUser (user, domain);

                context.Close ();
            } catch (Exception e) {
                context.LogError (this, e.Message);
                context.Close ();
                throw e;
            }

            return new WebResponseBool(true);
        }

        /// <summary>
        /// Put the specified request.
        /// </summary>
        /// <param name="request">Request.</param>
        public object Put (CommunityUpdateUserRequestTep request)
        {
            var context = TepWebContext.GetWebContext (PagePrivileges.DeveloperView);

            try {
                context.Open ();
                if (string.IsNullOrEmpty (request.Identifier)) throw new Exception ("Invalid request - missing community identifier");
                if (string.IsNullOrEmpty (request.Username)) throw new Exception ("Invalid request - missing username");
                if (string.IsNullOrEmpty (request.Role)) throw new Exception ("Invalid request - missing role");

                User user = User.FromUsername (context, request.Username);
                Role role = Role.FromIdentifier (context, request.Role);
                context.LogInfo (this, string.Format ("/community/user PUT Identifier='{0}', Username='{1}', Role='{2}'", request.Identifier, user.Username, role.Identifier));

                Domain domain = Domain.FromIdentifier (context, request.Identifier);

                //current user must be manager of the domain
                var roles = Role.GetUserRolesForDomain (context, context.UserId, domain.Id);
                bool ismanager = false;
                foreach (var r in roles) {
                    if (r.Identifier.Equals (ThematicGroup.MANAGER)) {
                        ismanager = true;
                        break;
                    }
                }
                if (!ismanager) throw new UnauthorizedAccessException ("Action only allowed to manager of the domain");

                //delete previous role(s)
                roles = Role.GetUserRolesForDomain (context, user.Id, domain.Id);
                foreach (var r in roles) r.RevokeFromUser (user, domain);

                //add new role
                role.GrantToUser (user, domain);

                context.Close ();
            } catch (Exception e) {
                context.LogError (this, e.Message);
                context.Close ();
                throw e;
            }

            return new WebResponseBool (true);
        }

        public object Delete (CommunityRemoveUserRequestTep request)
        {
            var context = TepWebContext.GetWebContext (PagePrivileges.DeveloperView);

            try {
                context.Open ();
                if (string.IsNullOrEmpty (request.Identifier)) throw new Exception ("Invalid request - missing community identifier");

                User user = string.IsNullOrEmpty (request.Username) ? User.FromId (context, context.UserId) : User.FromUsername (context, request.Username);
                context.LogInfo (this, string.Format ("/community/user DELETE Identifier='{0}', Username='{1}'", request.Identifier, request.Username));

                Domain domain = Domain.FromIdentifier (context, request.Identifier);

                if (string.IsNullOrEmpty (request.Username)) {
                    //current user must be manager of the domain
                    var roles = Role.GetUserRolesForDomain (context, context.UserId, domain.Id);
                    bool ismanager = false;
                    foreach (var r in roles) {
                        if (r.Identifier.Equals (ThematicGroup.MANAGER)) {
                            ismanager = true;
                            break;
                        }
                    }
                    if (!ismanager) throw new UnauthorizedAccessException ("Action only allowed to manager of the domain");
                }

                //delete previous role(s)
                var uroles = Role.GetUserRolesForDomain (context, user.Id, domain.Id);
                foreach (var r in uroles) r.RevokeFromUser (user, domain);

                context.Close ();
            } catch (Exception e) {
                context.LogError (this, e.Message);
                context.Close ();
                throw e;
            }

            return new WebResponseBool (true);
        }

        public object Put (CommunityAddWpsJobRequestTep request)
        {
            var context = TepWebContext.GetWebContext (PagePrivileges.DeveloperView);

            try {
                context.Open ();
                context.LogInfo (this, string.Format ("/job/wps/{{Id}}/community/{{cId}} PUT Id='{0}', CId='{1}", request.Id, request.CId));

                var wpsjob = WpsJob.FromId (context, request.Id);
                wpsjob.DomainId = request.CId;
                wpsjob.Store ();

                context.Close ();
            } catch (Exception e) {
                context.LogError (this, e.Message);
                context.Close ();
                throw e;
            }

            return new WebResponseBool (true);
        }

        public object Put (CommunityAddDataPackageRequestTep request)
        {
            var context = TepWebContext.GetWebContext (PagePrivileges.DeveloperView);

            try {
                context.Open ();
                context.LogInfo (this, string.Format ("/data/package/{{Id}}/community/{{cId}} PUT Id='{0}', CId='{1}", request.Id, request.CId));

                var dp = DataPackage.FromId (context, request.Id);
                dp.DomainId = request.CId;
                dp.Store ();

                context.Close ();
            } catch (Exception e) {
                context.LogError (this, e.Message);
                context.Close ();
                throw e;
            }

            return new WebResponseBool (true);
        }
    }
}