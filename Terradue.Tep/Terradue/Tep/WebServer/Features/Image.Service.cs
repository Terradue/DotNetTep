using System;
using System.Collections.Generic;
using System.IO;
using ServiceStack.ServiceHost;
using Terradue.Portal;
using Terradue.Tep.WebServer;
using Terradue.WebService.Model;

namespace Terradue.Tep.WebServer.Services {
    [Api("Tep Terradue webserver")]
    [Restrict(EndpointAttributes.InSecure | EndpointAttributes.InternalNetworkAccess | EndpointAttributes.Json,
              EndpointAttributes.Secure | EndpointAttributes.External | EndpointAttributes.Json)]
    public class ImageServiceTep : ServiceStack.ServiceInterface.Service {

        public object Get(GetImages request) {
            List<WebImage> result = new List<WebImage>();

            IfyWebContext context = TepWebContext.GetWebContext(PagePrivileges.EverybodyView);
            try {
                context.Open();
                EntityList<Image> img = new EntityList<Image>(context);

                img.Load();
                foreach(Image f in img) result.Add(new WebImage(f));

                context.Close();
            } catch (Exception e) {
                context.Close();
                throw e;
            }
            return result;
        }

        public object Get(GetImage request) {
            WebImage result = null;

            IfyWebContext context = TepWebContext.GetWebContext(PagePrivileges.EverybodyView);
            try {
                context.Open();

                Image img = Image.FromId(context,request.Id);
                result = new WebImage(img);

                context.Close();
            } catch (Exception e) {
                context.Close();
                throw e;
            }
            return result;
        }

        public object Post(CreateImage request) {
            WebImage result = null;

            IfyWebContext context = TepWebContext.GetWebContext(PagePrivileges.UserView);
            try {
                context.Open();

                Image img = new Image(context);
                img = request.ToEntity(context, img);
                img.Store();
                result = new WebImage(img);

                context.Close();
            } catch (Exception e) {
                context.Close();
                throw e;
            }
            return result;
        }

        public object Put(UpdateImage request) {
            WebImage result = null;

            IfyWebContext context = TepWebContext.GetWebContext(PagePrivileges.UserView);
            try {
                context.Open();

                Image img = Image.FromId(context, request.Id);
                img = request.ToEntity(context, img);
                img.Store();
                result = new WebImage(img);

                context.Close();
            } catch (Exception e) {
                context.Close();
                throw e;
            }
            return result;
        }

        public object Delete(DeleteImage request) {
            IfyWebContext context = TepWebContext.GetWebContext(PagePrivileges.UserView);
            try {
                context.Open();
                Image img = Image.FromId(context,request.Id);
                img.Delete();

                context.Close();
            } catch (Exception e) {
                context.Close();
                throw e;
            }
            return new WebResponseBool(true);
        }

        public object Post(UploadImage request){
            IfyWebContext context = TepWebContext.GetWebContext(PagePrivileges.UserView);
            string img = "";
            try {
                context.Open();
                string uid = Guid.NewGuid().ToString();

                if (request.Id == 0)
                {
                    var segments = base.Request.PathInfo.Split(new[] { '/' }, 
                                                               StringSplitOptions.RemoveEmptyEntries);
                    request.Id = System.Int32.Parse(segments[1]);
                }

                Image image = Image.FromId(context, request.Id);
                string oldImg = image.Url;
                if (this.RequestContext.Files.Length > 0)
                {
                    var uploadedFile = this.RequestContext.Files[0];
                    img = uid + uploadedFile.FileName.Substring(uploadedFile.FileName.LastIndexOf("."));
                    uploadedFile.SaveTo("files/" + img);
                }
                else{
                    using (var fileStream = File.Create("files/"+uid+".png"))
                    {
                        img = uid + ".png";
                        request.RequestStream.CopyTo(fileStream);
                    }
                }
                image.Url = img;
                image.Store();

                try{
                    if(oldImg != null) File.Delete("files/"+oldImg);
                }catch(Exception){}

                context.Close();
            }catch(Exception e)
            {
                context.Close ();
                throw e;
            }
            return new WebResponseBool(true);
        }
    }
}

