using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Text;
using System.Web;
using ServiceStack.Common.Web;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface;
using ServiceStack.ServiceInterface.ServiceModel;
using Terradue.Authentication.Umsso;
using Terradue.Portal;
using Terradue.WebService.Model;

namespace Terradue.Tep.WebServer {

    [Route("/user/{id}", "GET", Summary = "GET the user", Notes = "User is found from id")]
    public class UserGetRequestTep : IReturn<WebUserTep> {
        [ApiMember(Name = "id", Description = "User id", ParameterType = "query", DataType = "int", IsRequired = true)]
        public int Id { get; set; }

        [ApiMember (Name = "umsso", Description = "get also umsso info", ParameterType = "query", DataType = "bool", IsRequired = false)]
        public bool umsso { get; set; }
    }

    [Route("/user/current", "GET", Summary = "GET the current user", Notes = "User is the current user")]
    public class UserGetCurrentRequestTep : IReturn<WebUserTep> {
        [ApiMember (Name = "umsso", Description = "get also umsso info", ParameterType = "query", DataType = "bool", IsRequired = false)]
        public bool umsso { get; set; }
    }

    [Route("/user/current/logstatus", "GET", Summary = "GET the status of the current user", Notes = "true = is logged, false = is not logged")]
    public class UserCurrentIsLoggedRequestTep : IReturn<WebResponseBool>
    {
    }

    [Route("/user/current/sso", "GET", Summary = "GET the current user", Notes = "User is the current user")]
    public class UserGetCurrentSSORequestTep : IReturn<WebUserTep> {}

    [Route("/user/sso/{id}", "GET", Summary = "GET the current user", Notes = "User is the current user")]
    public class UserGetSSORequestTep : IReturn<WebUserTep> {
        [ApiMember(Name = "id", Description = "User id", ParameterType = "query", DataType = "int", IsRequired = true)]
        public int Id { get; set; }
    }

    [Route("/user/sso", "PUT", Summary = "GET the current user", Notes = "User is the current user")]
    public class UserUpdateSSORequestTep : IReturn<WebUserTep> {
        [ApiMember(Name = "id", Description = "User id", ParameterType = "query", DataType = "int", IsRequired = true)]
        public int Id { get; set; }

        [ApiMember(Name = "T2Username", Description = "Username", ParameterType = "query", DataType = "string", IsRequired = true)]
        public string T2Username { get; set; }
    }

    [Route("/user/current/sso", "POST", Summary = "POST the current user sso", Notes = "User is the current user")]
    public class UserCurrentCreateSSORequestTep : IReturn<WebUserTep> {
        [ApiMember(Name = "Password", Description = "Password", ParameterType = "query", DataType = "string", IsRequired = true)]
        public string Password { get; set; }
    }


//    [Route("/user/current/sso", "POST", Summary = "GET the current user", Notes = "User is the current user")]
//    public class UserPostCurrentSSORequestTep : IReturn<WebUserTep> {
//        [ApiMember(Name = "t2username", Description = "User name in T2 portal", ParameterType = "query", DataType = "string", IsRequired = false)]
//        public string T2Username { get; set; }
//    }


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

    [Route ("/user/key", "POST", Summary = "Create a new apikey", Notes = "User is contained in the POST data.")]
    public class UserCreateApiKeyRequestTep : IReturn<WebUserTep> { }

    [Route ("/user/key", "DELETE", Summary = "Create a new apikey", Notes = "User is contained in the POST data.")]
    public class UserDeleteApiKeyRequestTep : IReturn<WebUserTep> { }

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

        [ApiMember(Name = "EmailNotification", Description = "User email notification tag", ParameterType = "query", DataType = "bool", IsRequired = false)]
        public bool EmailNotification { get; set; }

        [ApiMember(Name = "UM-SSO Email", Description = "User email in UM-SSO", ParameterType = "query", DataType = "string", IsRequired = false)]
        public string UmssoEmail { get; set; }

        [ApiMember(Name = "t2username", Description = "User name in T2 portal", ParameterType = "query", DataType = "string", IsRequired = false)]
        public string T2Username { get; set; }

        [ApiMember (Name = "apikey", Description = "User apikey", ParameterType = "query", DataType = "string", IsRequired = false)]
        public string ApiKey { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Terradue.Tep.WebServer.WebUserTep"/> class.
        /// </summary>
        public WebUserTep() {}

        /// <summary>
        /// Initializes a new instance of the <see cref="Terradue.Tep.WebServer.WebUserTep"/> class.
        /// </summary>
        /// <param name="entity">Entity.</param>
        public WebUserTep(IfyWebContext context, UserTep entity, bool umsso = false) : base(entity) {
            if (umsso) {
                AuthenticationType umssoauthType = IfyWebContext.GetAuthenticationType (typeof (UmssoAuthenticationType));
                var umssoUser = umssoauthType.GetUserProfile (context, HttpContext.Current.Request, false);
                if (umssoUser != null) this.UmssoEmail = umssoUser.Email;
            }
            this.T2Username = entity.TerradueCloudUsername;
            //only current user can know the api key
            if(context.UserId == entity.Id) this.ApiKey = entity.ApiKey;
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

