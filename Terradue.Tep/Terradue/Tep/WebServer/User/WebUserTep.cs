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
    public class UserCurrentIsLoggedRequestTep : IReturn<WebResponseBool> {}

    [Route("/user/search", "GET", Summary = "GET user as opensearch", Notes = "")]
    public class UserSearchRequestTep : IReturn<HttpResult> { }

    [Route("/user/description", "GET", Summary = "GET user as opensearch", Notes = "")]
    public class UserDescriptionRequestTep : IReturn<HttpResult> { }

    [Route("/user/current/sso", "GET", Summary = "GET the current user", Notes = "User is the current user")]
    public class UserGetCurrentSSORequestTep : IReturn<WebUserTep> {}

    [Route("/user/sso/{id}", "GET", Summary = "GET the current user", Notes = "User is the current user")]
    public class UserGetSSORequestTep : IReturn<WebUserTep> {
        [ApiMember(Name = "id", Description = "User id", ParameterType = "query", DataType = "string", IsRequired = true)]
        public string Identifier { get; set; }
    }

    [Route("/user/sso", "PUT", Summary = "GET the current user", Notes = "User is the current user")]
    public class UserUpdateSSORequestTep : IReturn<WebUserTep> {
        [ApiMember(Name = "identifier", Description = "User id", ParameterType = "query", DataType = "int", IsRequired = true)]
        public string Identifier { get; set; }

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


    [Route("/user/usage", "GET", Summary = "GET the user usage", Notes = "User is found from id")]
    [Route("/user/usage", "GET", Summary = "GET the user usage", Notes = "User is found from id")]
    public class UserGetUsageRequestTep : IReturn<List<KeyValuePair<string, string>>> {
        [ApiMember(Name = "id", Description = "User id", ParameterType = "query", DataType = "int", IsRequired = true)]
        public int Id { get; set; }

        [ApiMember(Name = "identifier", Description = "User identifier", ParameterType = "query", DataType = "string", IsRequired = true)]
        public string Identifier { get; set; }
    }

    [Route("/user", "PUT", Summary = "Update user", Notes = "User is contained in the PUT data. Only non UMSSO data can be updated, e.g redmineApiKey or certField")]
    public class UserUpdateRequestTep : WebUserTep, IReturn<WebUserTep> {}

    [Route("/user/level", "PUT", Summary = "Update user level", Notes = "User is contained in the PUT data. Only non UMSSO data can be updated, e.g redmineApiKey or certField")]
    public class UserUpdateLevelRequestTep : WebUserTep, IReturn<WebUserTep> {}

    [Route("/user/status", "PUT", Summary = "Update user status", Notes = "User is contained in the PUT data. Only non UMSSO data can be updated, e.g redmineApiKey or certField")]
    public class UserUpdateStatusRequestTep : WebUserTep, IReturn<WebUserTep> { }

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
        [ApiMember(Name = "usrId", Description = "id of the user", ParameterType = "query", DataType = "string", IsRequired = true)]
        public string UsrId { get; set; }
    }

    [Route("/user/{usrId}/notebooks", "GET", Summary = "GET user has T2 notebooks", Notes = "")]
    public class UserHasT2NotebooksRequestTep : IReturn<WebResponseBool> {
        [ApiMember(Name = "usrId", Description = "id of the user", ParameterType = "query", DataType = "string", IsRequired = true)]
        public string UsrId { get; set; }
    }

    [Route("/user/csv", "GET", Summary = "GET user list as csv", Notes = "")]
    public class UserCsvListRequestTep : IReturn<string> { }

    [Route("/user/profile", "PUT", Summary = "Update profile from remote")]
    public class UpdateProfileFromRemoteTep : WebUserTep {
    }
    [Route("/users/profile", "PUT", Summary = "Update profile from remote")]
    public class UpdateBulkUsersProfileFromRemoteTep {
        [ApiMember(Name = "identifiers", Description = "ids", ParameterType = "query", DataType = "List<int>", IsRequired = true)]
        public string[] Identifiers { get; set; }
    }
    [Route("/users/level", "PUT", Summary = "Update bulk users level")]
    public class UpdateBulkUsersLevelTep {
        [ApiMember(Name = "identifiers", Description = "ids", ParameterType = "query", DataType = "List<int>", IsRequired = true)]
        public string[] Identifiers { get; set; }

        [ApiMember(Name = "Level", Description = "user level", ParameterType = "query", DataType = "int", IsRequired = true)]
        public int Level { get; set; }
    }
    [Route("/user/{UsrId}/delete", "DELETE", Summary = "Delete user", Notes = "")]
    public class UserDeleteRequestTep : IReturn<WebUserTep> {
        [ApiMember(Name = "usrId", Description = "id of the user", ParameterType = "query", DataType = "string", IsRequired = true)]
        public string UsrId { get; set; }
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

        [ApiMember(Name = "t2profileError", Description = "Error message to get T2 profile", ParameterType = "query", DataType = "string", IsRequired = false)]
        public string T2ProfileError { get; set; }

        [ApiMember (Name = "t2apikey", Description = "T2 User apikey", ParameterType = "query", DataType = "string", IsRequired = false)]
        public string T2ApiKey { get; set; }

        [ApiMember(Name = "apikey", Description = "User apikey", ParameterType = "query", DataType = "string", IsRequired = false)]
		public string ApiKey { get; set; }

        [ApiMember(Name = "balance", Description = "User accounting balance", ParameterType = "query", DataType = "double", IsRequired = false)]
        public double Balance { get; set; }

		[ApiMember(Name = "RegistrationDate", Description = "User registration date", ParameterType = "query", DataType = "DateTime", IsRequired = false)]
		public DateTime RegistrationDate { get; set; }
        
        [ApiMember(Name = "roles", Description = "User accounting balance", ParameterType = "query", DataType = "List<WebCommunityRoles>", IsRequired = false)]
        public List<WebCommunityRoles> Roles { get; set; }

        [ApiMember(Name = "token", Description = "sso token", ParameterType = "query", DataType = "String", IsRequired = true)]
        public String Token { get; set; }

        [ApiMember(Name = "token_expire", Description = "sso token expire", ParameterType = "query", DataType = "String", IsRequired = true)]
        public DateTime TokenExpire { get; set; }
        
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

            //only current user can know the api key
            if (context.UserId == entity.Id) {
                this.ApiKey = entity.ApiKey;
                this.T2ProfileError = HttpContext.Current.Session["t2profileError"] as string;
                if ((string.IsNullOrEmpty(entity.Affiliation) || string.IsNullOrEmpty(entity.Country) || string.IsNullOrEmpty(entity.FirstName) || string.IsNullOrEmpty(entity.LastName)))
                    this.T2ProfileError += (string.IsNullOrEmpty(this.T2ProfileError) ? "" : "\n" ) + "Profile not complete";
                this.T2ApiKey = entity.GetSessionApiKey();
            }

            if (context.UserId == entity.Id || context.UserLevel == UserLevel.Administrator){
                this.T2Username = entity.TerradueCloudUsername;
                if (context.GetConfigBooleanValue("accounting-enabled")) this.Balance = entity.GetAccountingBalance();
                this.Roles = GetUserCommunityRoles(context, entity);
				if (context.UserLevel == UserLevel.Administrator) {
					if (entity.RegistrationDate == DateTime.MinValue) entity.LoadRegistrationInfo();
					this.RegistrationDate = entity.RegistrationDate;
				}
            } else {
                this.Email = null;
                this.Affiliation = null;
                this.Level = 0;
                this.AccountStatus = 0;
                this.DomainId = null;
            }

        }

        /// <summary>
        /// Tos the entity.
        /// </summary>
        /// <returns>The entity.</returns>
        /// <param name="context">Context.</param>
		public UserTep ToEntity(IfyContext context, UserTep input) {
			UserTep user = (input == null ? new UserTep(context) : input);
			user = (UserTep)base.ToEntity(context, user);

            return user;
        }

        public List<WebCommunityRoles> GetUserCommunityRoles(IfyContext context, UserTep entity) { 
            context.LogDebug(this, "GetUserCommunityRoles");
            var communityroles = new List<WebCommunityRoles>();
            try {
                var communities = entity.GetUserCommunities();
                context.LogDebug(this, string.Format("GetUserCommunityRoles - found {0} communities", communities.Count));
                foreach (var community in communities) {
                    try {
                        var roles = entity.GetUserRoles(community);
                        var webroles = new List<WebRole>();
                        foreach (var role in roles) {
                            webroles.Add(new WebRole {
                                Description = role.Description,
                                Identifier = role.Identifier,
                                Name = role.Name
                            });
                        }
                        if (webroles.Count > 0) {
                            communityroles.Add(new WebCommunityRoles {
                                Community = community.Name,
                                CommunityIdentifier = community.Identifier,
                                Link = string.Format("/#!communities/details/{0}",community.Identifier),
                                Roles = webroles
                            });
                        }
                    } catch (Exception e) {
                        context.LogError(this, e.Message, e);
                    }
                }
            } catch (Exception e) {
                context.LogError(this, e.Message, e);
            }
            context.LogDebug(this, string.Format("GetUserCommunityRoles - found {0} communitiesRoles", communityroles.Count));
            return communityroles;
        }
            
    }

    public class WebCommunityRoles {
        [ApiMember(Name = "community", Description = "community name", ParameterType = "query", DataType = "string", IsRequired = false)]
        public string Community { get; set; }

        [ApiMember(Name = "communityIdentifier", Description = "community identifier", ParameterType = "query", DataType = "string", IsRequired = false)]
        public string CommunityIdentifier { get; set; }

        [ApiMember(Name = "link", Description = "community link", ParameterType = "query", DataType = "string", IsRequired = false)]
        public string Link { get; set; }

        [ApiMember(Name = "roles", Description = "community roles", ParameterType = "query", DataType = "List<string>", IsRequired = false)]
        public List<WebRole> Roles { get; set; }
    }
}

