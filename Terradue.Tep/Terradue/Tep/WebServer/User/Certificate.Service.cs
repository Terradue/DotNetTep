using System;
using System.Collections.Generic;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface;
using Terradue.Certification.WebService;
using Terradue.OpenNebula;
using Terradue.Portal;
using Terradue.Security.Certification;
using Terradue.Tep.WebServer;
using Terradue.WebService.Model;

namespace Terradue.Tep.WebServer.Services {
    [Api("Tep Terradue webserver")]
    [Restrict(EndpointAttributes.InSecure | EndpointAttributes.InternalNetworkAccess | EndpointAttributes.Json,
              EndpointAttributes.Secure | EndpointAttributes.External | EndpointAttributes.Json)]
    public class CertificateServiceTep : ServiceStack.ServiceInterface.Service {

        private static readonly log4net.ILog log = log4net.LogManager.GetLogger
            (System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        
        /// <summary>
        /// Post the specified request (upload certificate).
        /// </summary>
        /// <param name="request">Request.</param>
        public object Post(UploadCertificate request) {
            CertificateUser user;
            IfyWebContext context = TepWebContext.GetWebContext(PagePrivileges.DeveloperView);
            WebUserCertificate userCert;
            try {
                context.Open();

                user = CertificateUser.FromId(context, context.UserId);
                user.StoreCertificate(request.RequestStream);

                log.Info(string.Format("Certificate uploaded by user '{0}'", user.Username));

                //send an email to Support to warn them
                string emailFrom = context.GetConfigValue("MailSenderAddress");

                string subject = context.GetConfigValue("EmailCertificateUploadSubject");
                subject = subject.Replace("$(SITENAME)", context.GetConfigValue("SiteName"));

                string body = context.GetConfigValue("EmailCertificateUploadBody");
                body = body.Replace("$(USERNAME)", user.Username);

                context.SendMail(emailFrom, emailFrom, subject, body);

                userCert = new WebUserCertificate(user);
            } catch (Exception e) {
                context.LogError(request, e.Message, e);
                context.Close();
                throw e;
            }
            return userCert;
        }

        public object Post(RequestCertificate request) {
            IfyWebContext context = TepWebContext.GetWebContext(PagePrivileges.DeveloperView);
            CertificateUser certUser;
            WebUserCertificate userCert = null;

            try {
                context.Open();
                try {
                    certUser = (CertificateUser)CertificateUser.FromId(context, context.UserId);
                    log.Info(string.Format("Certificate requested by user '{0}'", certUser.Username));
                } catch (EntityNotFoundException e) {
                    certUser = new CertificateUser(context);
                } catch (Exception e) {
                    throw e;
                }

                try{
                    certUser.RequestCertificate(request.password);
                    log.Debug(string.Format("Certificate request went fine"));
                } catch (Exception e) {
                    if (e.Message.Contains("Certificate request failed: a certificate request has been already performed for the username <username> and is currently on approval.")) {
                        string msg = string.Format("Certificate request failed: a certificate request has been already performed for the username {0} and is currently on approval. " +
                            "If you created the request and want to cancel it, please contact the support team at ca@terradue.com",userCert.Username);
                        log.Error(msg);
                        throw new Exception(msg, e);
                    } else {
                        log.Error(e.Message);
                        throw e;
                    }
                }
                userCert = new WebUserCertificate(certUser);
                context.Close();
            } catch (Exception e) {
                context.LogError(request, e.Message, e);
                context.Close();
                throw e;
            }

            return userCert;
        }

        public object Delete(DeleteUserCertificate request) {
            IfyWebContext context = TepWebContext.GetWebContext(PagePrivileges.DeveloperView);
            UserTep user;

            try {
                context.Open();
                user = UserTep.FromId(context, context.UserId);

                log.Info(string.Format("Certificate removal requested by user '{0}'", user.Username));
                user.RemoveCertificate();
                log.Debug(string.Format("Certificate removal went fine"));


                //send an email to Support to warn them
                string emailFrom = context.GetConfigValue("MailSenderAddress");
                string subject = context.GetConfigValue("EmailCertificateRemovalSubject");
                subject = subject.Replace("$(SITENAME)", context.GetConfigValue("SiteName"));
                subject = subject.Replace("$(USERNAME)", user.Username);

                string url = context.GetConfigValue("UrlCertificateReset");
                url = url.Replace("$(BASEURL)",context.HostUrl);
                url = url.Replace("$(USERNAME)",user.Username);

                string body = context.GetConfigValue("EmailCertificateRemovalBody");
                body = body.Replace("$(USERNAME)", user.Username);
                body = body.Replace("$(URL)", url);

                context.SendMail(emailFrom, emailFrom, subject, body);

                context.Close();
            } catch (Exception e) {
                context.LogError(request, e.Message, e);
                context.Close();
                throw e;
            }

            return new WebResponseBool(true);
        }

        public object Put(UserUpdateCertificateRequestTep request) {
            IfyWebContext context = TepWebContext.GetWebContext(PagePrivileges.AdminOnly);
            WebUserTep result;
            try {
                context.Open();
                UserTep user = (UserTep)UserTep.FromUsername(context, request.Username);
                log.Info(string.Format("Certificate reset requested for user '{0}'", user.Username));
                user.ResetCertificateStatus();
                log.Debug(string.Format("Certificate reset went fine"));
                result = new WebUserTep(context, user);

                //send an email to Support to warn them
                string emailFrom = context.GetConfigValue("MailSenderAddress");
                string subject = context.GetConfigValue("EmailCertificateResetSubject");
                subject = subject.Replace("$(SITENAME)", context.GetConfigValue("SiteName"));

                string url = context.GetConfigValue("UrlCertificate");
                url = url.Replace("$(BASEURL)", context.HostUrl);

                string body = context.GetConfigValue("EmailCertificateResetBody");
                body = body.Replace("$(URL)", url);

                context.SendMail(emailFrom, user.Email, subject, body);

                context.Close();
            } catch (Exception e) {
                context.LogError(request, e.Message, e);
                context.Close();
                throw e;
            }

            return result;
        }

        public object Get(GetUserCertificate request) {
            IfyWebContext context = TepWebContext.GetWebContext(PagePrivileges.DeveloperView);
            CertificateUser certUser;
            WebUserCertificate userCert;

            try {

                context.Open();
                certUser = CertificateUser.FromId(context, context.UserId);

                try {
                    if (certUser.IsUnderApproval()) certUser.TryDownloadAndStoreCertificate();
                } catch (ResourceNotFoundException) {}
                    
                userCert = new WebUserCertificate(certUser);

                context.Close();
            } catch (Exception e) {
                context.LogError(request, e.Message, e);
                context.Close();
                throw e;
            }

            return userCert;
        }
    }
}

