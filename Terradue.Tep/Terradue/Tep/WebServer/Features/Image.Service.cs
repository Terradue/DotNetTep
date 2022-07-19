using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Web;
using ServiceStack.ServiceHost;
using Terradue.Portal;
using Terradue.Tep.WebServer;
using Terradue.WebService.Model;

namespace Terradue.Tep.WebServer.Services {

    [Route("/image", "GET", Summary = "GET a list of images", Notes = "")]
    public class GetImages : IReturn<List<string>>{
        [ApiMember(Name="q", Description = "q", ParameterType = "query", DataType = "string", IsRequired = true)]
        public string q { get; set; }
    }

    [Route("/image", "POST", Summary = "POST Image file")]
    public class UploadImage : IRequiresRequestStream, IReturn<WebResponseBool>
    {
        public System.IO.Stream RequestStream { get; set; }

        [ApiMember(Name="overwrite", Description = "overwrite", ParameterType = "query", DataType = "bool", IsRequired = true)]
        public bool overwrite { get; set; }
    }

    [Route("/image", "DELETE", Summary = "DELETE Image file")]
    public class DeleteImage : IReturn<WebResponseBool>{
        [ApiMember(Name="filename", Description = "filename", ParameterType = "query", DataType = "string", IsRequired = true)]
        public string filename { get; set; }
    }


    [Api("Tep Terradue webserver")]
    [Restrict(EndpointAttributes.InSecure | EndpointAttributes.InternalNetworkAccess | EndpointAttributes.Json,
              EndpointAttributes.Secure | EndpointAttributes.External | EndpointAttributes.Json)]
    public class ImageServiceTep : ServiceStack.ServiceInterface.Service {

        public object Get(GetImages request) {
            List<string> result = new List<string>();

            var context = TepWebContext.GetWebContext(PagePrivileges.EverybodyView);
            try {
                context.Open();

                var imgpath = context.GetConfigValue ("path.img");                
                // var rootpath = imgpath.Substring(imgpath.LastIndexOf("/root") + 5);                                
                // result = System.IO.Directory.GetFiles(imgpath, !string.IsNullOrEmpty(request.q) ? "*" + request.q + "*" : "*").ToList();
                // result = result.ConvertAll(f => rootpath + "/" + f.Substring(f.LastIndexOf("/") + 1));

                var allresult = System.IO.Directory.GetFiles(imgpath, "*").ToList();
                if(!string.IsNullOrEmpty(request.q)){
                    foreach(var f in allresult){
                        var filename = f.Substring(f.LastIndexOf("/") + 1);
                        if(filename.ToLower().Contains(request.q.ToLower())) result.Add(f);
                    }
                } else 
                    result = allresult;
                
                var rootpath = context.GetConfigValue("BaseUrl").TrimEnd('/') + "/" + imgpath.Substring(imgpath.LastIndexOf("/root/") + 6);                                ;
                result = result.ConvertAll(f => rootpath + "/" + f.Substring(f.LastIndexOf("/") + 1));

                context.Close();
            } catch (Exception e) {
                context.Close();
                throw e;
            }
            return result;
        }

        public object Post(UploadImage request){
            var context = TepWebContext.GetWebContext(PagePrivileges.AdminOnly);            
            try {
                context.Open();
                string uid = Guid.NewGuid().ToString();

                if (this.RequestContext.Files.Length == 0) throw new Exception("File not sent");
                var uploadedFile = this.RequestContext.Files[0];                    
                var filename = uploadedFile.FileName;

                //check does not already exists
                // if (System.IO.Directory.GetFiles(imgpath) != null && request.overwrite == false)
                    // throw new Exception("File already exists");
                
                // try{
                //     if(oldImg != null) File.Delete(imgpath);
                // }catch(Exception){}
                
                uploadedFile.SaveTo(context.GetConfigValue ("path.img") + "/" + filename);
                
                context.Close();
            }catch(Exception e)
            {
                context.Close ();
                throw e;
            }
            return new WebResponseBool(true);
        }

        public object Delete(DeleteImage request) {
            var context = TepWebContext.GetWebContext(PagePrivileges.AdminOnly);
            try {
                context.Open();
                try{
                    var imgpath = context.GetConfigValue ("path.img").TrimEnd('/') + "/" + request.filename;
                    File.Delete(imgpath);
                }catch(Exception){}

                context.Close();
            } catch (Exception e) {
                context.Close();
                throw e;
            }
            return new WebResponseBool(true);
        }

    }
}

