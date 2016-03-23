using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Text;
using System.Web;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface;
using ServiceStack.ServiceInterface.ServiceModel;
using Terradue.Authentication.Umsso;
using Terradue.Certification.WebService;
using Terradue.Portal;
using Terradue.WebService.Model;

namespace Terradue.Tep.WebServer {

    [Route("/user/{id}", "GET", Summary = "GET the user", Notes = "User is found from id")]
    public class UserGetRequestTep : IReturn<WebUserTep> {
        [ApiMember(Name = "id", Description = "User id", ParameterType = "query", DataType = "int", IsRequired = true)]
        public int Id { get; set; }
    }

    [Route("/user/current", "GET", Summary = "GET the current user", Notes = "User is the current user")]
    public class UserGetCurrentRequestTep : IReturn<WebUserTep> {}

    [Route("/user/current/logstatus", "GET", Summary = "GET the status of the current user", Notes = "true = is logged, false = is not logged")]
    public class UserCurrentIsLoggedRequestTep : IReturn<WebResponseBool>
    {
    }

    [Route("/user/{id}/usage", "GET", Summary = "GET the user usage", Notes = "User is found from id")]
    public class UserGetUsageRequestTep : IReturn<List<KeyValuePair<string, string>>> {
        [ApiMember(Name = "id", Description = "User id", ParameterType = "query", DataType = "int", IsRequired = true)]
        public int Id { get; set; }
    }

    [Route("/user", "PUT", Summary = "Update user", Notes = "User is contained in the PUT data. Only non UMSSO data can be updated, e.g redmineApiKey or certField")]
    public class UserUpdateRequestTep : WebUserTep, IReturn<WebUserTep> {}

    [Route("/user/level", "PUT", Summary = "Update user level", Notes = "User is contained in the PUT data. Only non UMSSO data can be updated, e.g redmineApiKey or certField")]
    public class UserUpdateLevelRequestTep : WebUserTep, IReturn<WebUserTep> {}

    [Route("/user", "POST", Summary = "Create a new user", Notes = "User is contained in the POST data.")]
    public class UserCreateRequestTep : WebUserTep, IReturn<WebUserTep> {}

    [Route("/user/cert", "PUT", Summary = "Update user cert", Notes = "User is contained in the PUT data. Only non UMSSO data can be updated, e.g redmineApiKey or certField")]
    public class UserUpdateCertificateRequestTep : WebUserTep, IReturn<WebUserTep> {}

    [Route("/user/{usrId}/group", "GET", Summary = "GET list of groups associated to a user", Notes = "")]
    public class UserGetGroupsRequestTep : IReturn<List<WebGroup>>
    {
        [ApiMember(Name = "usrId", Description = "id of the user", ParameterType = "query", DataType = "int", IsRequired = true)]
        public int UsrId { get; set; }
    }

    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// User.
    /// </summary>
    public class WebUserTep : WebUser{

        [ApiMember(Name = "onepassword", Description = "User password on OpenNebula", ParameterType = "query", DataType = "String", IsRequired = false)]
        public String OnePassword { get; set; }

        [ApiMember(Name = "certsubject", Description = "User certificate subject", ParameterType = "query", DataType = "String", IsRequired = false)]
        public String CertSubject { get; set; }

        [ApiMember(Name = "certstatus", Description = "User certificate status", ParameterType = "query", DataType = "int", IsRequired = false)]
        public int CertStatus { get; set; }

        [ApiMember(Name = "EmailNotification", Description = "User email notification tag", ParameterType = "query", DataType = "bool", IsRequired = false)]
        public bool EmailNotification { get; set; }

        [ApiMember(Name = "UM-SSO Email", Description = "User email in UM-SSO", ParameterType = "query", DataType = "string", IsRequired = false)]
        public string UmssoEmail { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Terradue.Tep.WebServer.WebUserTep"/> class.
        /// </summary>
        public WebUserTep() {}

        /// <summary>
        /// Initializes a new instance of the <see cref="Terradue.Tep.WebServer.WebUserTep"/> class.
        /// </summary>
        /// <param name="entity">Entity.</param>
        public WebUserTep(IfyWebContext context, UserTep entity) : base(entity) {
            this.OnePassword = entity.OnePassword;
            this.CertSubject = WebUserCertificate.TransformInOpenNebulaFormat(entity.CertSubject);
            AuthenticationType umssoauthType = IfyWebContext.GetAuthenticationType(typeof(UmssoAuthenticationType));
            var umssoUser = umssoauthType.GetUserProfile(context, HttpContext.Current.Request, false);
            if (umssoUser != null) this.UmssoEmail = umssoUser.Email;
            this.CertStatus = entity.CertStatus;
        }

        /// <summary>
        /// Tos the entity.
        /// </summary>
        /// <returns>The entity.</returns>
        /// <param name="context">Context.</param>
		public UserTep ToEntity(IfyContext context, UserTep input) {
			UserTep user = (input == null ? new UserTep(context) : input);
			base.ToEntity(context, user);

            return user;
        }
            
    }
}

