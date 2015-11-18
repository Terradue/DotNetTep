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
    public class FeatureServiceTep : ServiceStack.ServiceInterface.Service {

        public object Get(GetFeatures request) {
            List<WebFeature> result = new List<WebFeature>();

            IfyWebContext context = TepWebContext.GetWebContext(PagePrivileges.EverybodyView);
            try {
                context.Open();
                EntityList<Terradue.Portal.Feature> feats = new EntityList<Terradue.Portal.Feature>(context);
                feats.Load();

                List<Terradue.Portal.Feature> features = feats.GetItemsAsList();
                features.Sort();

                foreach(Terradue.Portal.Feature f in features) result.Add(new WebFeature(f));

                context.Close();
            } catch (Exception e) {
                context.Close();
                throw e;
            }
            return result;
        }

        public object Get(GetFeature request) {
            WebFeature result = null;

            IfyWebContext context = TepWebContext.GetWebContext(PagePrivileges.EverybodyView);
            try {
                context.Open();
                Terradue.Portal.Feature feat = Terradue.Portal.Feature.FromId(context,request.Id);
                result = new WebFeature(feat);

                context.Close();
            } catch (Exception e) {
                context.Close();
                throw e;
            }
            return result;
        }

        public object Post(CreateFeature request) {
            WebFeature result = null;

            IfyWebContext context = TepWebContext.GetWebContext(PagePrivileges.UserView);
            try {
                context.Open();

                Terradue.Portal.Feature feat = new Terradue.Portal.Feature(context);
                feat = request.ToEntity(context, feat);
                feat.Store();
                result = new WebFeature(feat);

                context.Close();
            } catch (Exception e) {
                context.Close();
                throw e;
            }
            return result;
        }

        public object Put(UpdateFeature request) {
            WebFeature result = null;

            IfyWebContext context = TepWebContext.GetWebContext(PagePrivileges.UserView);
            try {
                context.Open();

                Terradue.Portal.Feature feat = Terradue.Portal.Feature.FromId(context, request.Id);
                feat = request.ToEntity(context, feat);
                feat.Store();
                result = new WebFeature(feat);

                context.Close();
            } catch (Exception e) {
                context.Close();
                throw e;
            }
            return result;
        }

        public object Put(SortFeature request) {
            WebFeature result = null;

            IfyWebContext context = TepWebContext.GetWebContext(PagePrivileges.UserView);
            try {
                context.Open();

                foreach(WebFeature wfeat in request){
                    Terradue.Portal.Feature feat = Terradue.Portal.Feature.FromId(context, wfeat.Id);
                    feat.Position = wfeat.Position;
                    feat.Store();    
                }

                context.Close();
            } catch (Exception e) {
                context.Close();
                throw e;
            }
            return request;
        }

        public object Delete(DeleteFeature request) {
            IfyWebContext context = TepWebContext.GetWebContext(PagePrivileges.UserView);
            try {
                context.Open();
                Terradue.Portal.Feature feat = Terradue.Portal.Feature.FromId(context,request.Id);
                feat.Delete();

                context.Close();
            } catch (Exception e) {
                context.Close();
                throw e;
            }
            return new WebResponseBool(true);
        }

        public object Post(UploadFeatureImage request){
            IfyWebContext context = TepWebContext.GetWebContext(PagePrivileges.UserView);
            string img = "";
            string uid = Guid.NewGuid().ToString();
            string extension = ".png";

            WebFeature result = null;

            try {
                context.Open();

                if (request.Id == 0)
                {
                    var segments = base.Request.PathInfo.Split(new[] { '/' }, 
                                                               StringSplitOptions.RemoveEmptyEntries);
                    request.Id = System.Int32.Parse(segments[1]);
                }

                Terradue.Portal.Feature feature = Terradue.Portal.Feature.FromId(context, request.Id);
                string oldImg = feature.Image;
                if (this.RequestContext.Files.Length > 0) {
                    var uploadedFile = this.RequestContext.Files[0];
                    extension = uploadedFile.FileName.Substring(uploadedFile.FileName.LastIndexOf("."));
                    img = "/files/" + uid + extension;

                    string path = AppDomain.CurrentDomain.BaseDirectory;
                    if(!path.EndsWith("/")) path += "/";

                    context.LogInfo(this, string.Format("Uploading image to {0}", path + img));
                    uploadedFile.SaveTo(path + img);
                } else {
                    using (var fileStream = File.Create("files/" + uid + extension))
                    {
                        img = "files/" + uid + extension;
                        request.RequestStream.CopyTo(fileStream);
                    }
                }
                feature.Image = img;
                feature.Store();

                result = new WebFeature(feature);

                try{
                    if(oldImg != null) File.Delete("files/"+oldImg);
                }catch(Exception){}

                context.Close();
            }catch(Exception e)
            {
                context.Close ();
                throw e;
            }
            return result;
        }
    }
}

