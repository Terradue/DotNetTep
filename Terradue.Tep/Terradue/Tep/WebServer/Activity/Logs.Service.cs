using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Routing;
using ServiceStack.ServiceHost;
using Terradue.Portal;
using Terradue.Tep.WebServer;
using Terradue.WebService.Model;

namespace Terradue.Tep.WebServer.Services {

    [Route("/log", "GET", Summary = "GET report", Notes = "")]
    public class LogGetRequest : IReturn<string>{
        [ApiMember(Name = "filename", Description = "filename", ParameterType = "query", DataType = "string", IsRequired = false)]
        public string filename { get; set; }
    }

    [Route("/logs", "GET", Summary = "GET existing reports", Notes = "")]
    public class LogsGetRequest : IReturn<List<string>>{}


     [Api("Tep Terradue webserver")]
    [Restrict(EndpointAttributes.InSecure | EndpointAttributes.InternalNetworkAccess | EndpointAttributes.Json,
              EndpointAttributes.Secure | EndpointAttributes.External | EndpointAttributes.Json)]
    public class LogServiceTep : ServiceStack.ServiceInterface.Service {
        
        public object Get(LogsGetRequest request){
            IfyWebContext context = TepWebContext.GetWebContext(PagePrivileges.AdminOnly);
            List<string> result = new List<string>();
            try {
                context.Open();

                string path = AppDomain.CurrentDomain.BaseDirectory;
                if(!path.EndsWith("/")) path += "/";

                context.LogDebug(this, path);
                result = System.IO.Directory.GetFiles(path + "../logs","*.log*").ToList();
                result = result.ConvertAll(f => f.Substring(f.LastIndexOf("/") + 1));

                context.Close();
            } catch (Exception e) {
                context.Close();
                throw e;
            }
            return result;
        }

        public object Get(LogGetRequest request){
            IfyWebContext context = TepWebContext.GetWebContext(PagePrivileges.AdminOnly);
            var text = new System.Text.StringBuilder();

            try {
                context.Open();

                string path = AppDomain.CurrentDomain.BaseDirectory;
                if(!path.EndsWith("/")) path += "/";
                var filepath = string.Format("{1}../logs/{0}",request.filename,path);

//                System.IO.File.WriteAllText(string.Format("{1}../logs/{0}",request.filename,path), csv.ToString());
                string[] lines = System.IO.File.ReadAllLines(filepath);

                foreach (string line in lines){
                    text.Append(line);
                    text.AppendLine();
                }


                context.Close();

            } catch (Exception e) {
                context.Close();
                throw e;
            }
//            Response.AddHeader("Content-Disposition", string.Format("attachment;filename={0}",request.filename));
            return text.ToString();
        }

       }
}

