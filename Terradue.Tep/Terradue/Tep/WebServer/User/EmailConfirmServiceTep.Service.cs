using System;
using System.Web;
using ServiceStack.Common.Web;
using ServiceStack.ServiceHost;
using Terradue.Authentication.Umsso;
using Terradue.Portal;
using Terradue.Tep.WebServer;
using Terradue.WebService.Model;

namespace Terradue.Tep.WebServer.Services {

    /// <summary>
    /// Email confirmation service
    /// </summary>
    [Api("Tep Terradue webserver")]
    [Restrict(EndpointAttributes.InSecure | EndpointAttributes.InternalNetworkAccess | EndpointAttributes.Json,
              EndpointAttributes.Secure | EndpointAttributes.External | EndpointAttributes.Json)]
    public class EmailConfirmServiceTep : ServiceStack.ServiceInterface.Service {

        private static readonly log4net.ILog log = log4net.LogManager.GetLogger
            (System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);


        /// <summary>
        /// This method allows user to confirm its email adress with a token key
        /// </summary>
        /// <param name="request">Request.</param>
        public object Get(ConfirmUserEmail request) {

            IfyWebContext context = TepWebContext.GetWebContext(PagePrivileges.UserView);
            // Let's try to open context
            try {
                context.Open();
                log.Error(string.Format("Email already confirmed for user {0}", context.Username));
                context.Close();
                return new HttpError(System.Net.HttpStatusCode.MethodNotAllowed, new InvalidOperationException("Email already confirmed"));
            
                // The user is pending activation
            } catch (PendingActivationException e) {
                log.Info(string.Format("Pending activation for user {0}", context.Username));
                AuthenticationType authType = IfyWebContext.GetAuthenticationType(typeof(TokenAuthenticationType));
                AuthenticationType umssoauthType = IfyWebContext.GetAuthenticationType(typeof(UmssoAuthenticationType));

                var umssoUser = umssoauthType.GetUserProfile(context, HttpContext.Current.Request, false);

                if (umssoUser == null) {
                    log.Error(string.Format("User not logged in UMSSO"));
                    return new HttpError(System.Net.HttpStatusCode.BadRequest, new UnauthorizedAccessException("Not logged in UM-SSO"));
                }

                // User is logged, now we confirm the email with the token
                log.Debug(string.Format("User now logged -- Confirm email with token"));
                var tokenUser = ((TokenAuthenticationType)authType).AuthenticateUser(context, request.Token);

                // We must check that the logged user if the one that received the email
                // If not, we rollback to previous status
                if (tokenUser.Email != Request.Headers["Umsso-Person-Email"]) {
                    tokenUser.AccountStatus = AccountStatusType.PendingActivation;
                    tokenUser.Store();
                    log.Error(string.Format("Confirmation email and UM-SSO email do not match"));
                    return new HttpError(System.Net.HttpStatusCode.BadRequest, new UnauthorizedAccessException("Confirmation email and UM-SSO email do not match"));
                }

                log.Debug(string.Format("User now logged -- Email confirmed"));

                //send an email to Support to warn them
                string emailFrom = context.GetConfigValue("MailSenderAddress");
                string subject = string.Format("[{0}] - Email verification for user {1}", context.GetConfigValue("SiteName"), umssoUser.Username);
                string body = string.Format("Dear support,\n\nThis is an automatic email to inform you that user {0} has just confirmed his email address ({1}) on the geohazard platform.\n", umssoUser.Username, umssoUser.Email);
                context.SendMail(emailFrom, emailFrom, subject, body);
            }

            context.Close();
            return new WebResponseBool(true);

        }

        /// <summary>
        /// This method allows user to request the confirmation email
        /// </summary>
        /// <param name="request">Request.</param>
        public object Post(SendUserEmailConfirmationEmail request) {

            IfyWebContext context = TepWebContext.GetWebContext(PagePrivileges.UserView);

            try {
                context.Open();
                log.Error(string.Format("Email already confirmed for user {0}", context.Username));
                return new HttpError(System.Net.HttpStatusCode.BadRequest, new InvalidOperationException("Account does not require email confirmation"));

            } catch (PendingActivationException e) {
                log.Info(string.Format("Pending activation for user {0}", context.Username));
                AuthenticationType umssoauthType = IfyWebContext.GetAuthenticationType(typeof(UmssoAuthenticationType));
                var umssoUser = umssoauthType.GetUserProfile(context, HttpContext.Current.Request, false);
                if (umssoUser == null) {
                    log.Error(string.Format("User not logged in UMSSO"));
                    return new HttpError(System.Net.HttpStatusCode.BadRequest, new UnauthorizedAccessException("Not logged in UM-SSO"));
                }

                if (Request.Headers["Umsso-Person-Email"] != umssoUser.Email) {
                    umssoUser.Email = Request.Headers["Umsso-Person-Email"];
                    umssoUser.Store();
                    log.Error(string.Format("Confirmation email and UM-SSO email do not match"));
                }

                string emailFrom = context.GetConfigValue("MailSenderAddress");
                string subject = context.GetConfigValue("RegistrationMailSubject");
                subject = subject.Replace("$(SITENAME)", context.GetConfigValue("SiteName"));

                string activationToken = context.GetQueryStringValue(String.Format("SELECT token FROM usrreg WHERE id_usr={0};", umssoUser.Id));
                string confirmUrl = context.GetConfigValue("EmailConfirmationUrl").Replace("$(BASEURL)", context.HostUrl).Replace("$(TOKEN)", activationToken);
                string body = context.GetConfigValue("RegistrationMailBody");
                body = body.Replace("$(USERNAME)", umssoUser.Username);
                body = body.Replace("$(SITENAME)", context.GetConfigValue("SiteName"));
                body = body.Replace("$(ACTIVATIONURL)", confirmUrl);

                context.SendMail(emailFrom, umssoUser.Email, subject, body);

                return new HttpResult(new EmailConfirmationMessage(){ Status = "sent", Email = umssoUser.Email });
            }
        }

    }

    public class EmailConfirmationMessage {
        public string Status { get; set; }

        public string Email { get; set; }
    }
}

