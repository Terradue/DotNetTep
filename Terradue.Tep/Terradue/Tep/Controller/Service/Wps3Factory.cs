using System;
using OpenGis.Wps;
using Terradue.Portal;
using System.Collections.Generic;
using System.Net;
using System.IO;
using ServiceStack.Common.Web;
using System.Xml.Serialization;
using System.Runtime.Caching;
using System.Linq;

namespace Terradue.Tep {

    public interface IWps3Factory {
        string GetResultDescriptionFromS3Link(IfyContext context, WpsJob job, string s3link);
    }
    public class Wps3Factory : IWps3Factory {

        protected IfyContext context;
        public Wps3Factory(IfyContext context){
            this.context = context;
        }

        public string GetResultDescriptionFromS3Link(IfyContext context, WpsJob job, string s3link){
            if(!string.IsNullOrEmpty(job.PublishType) && !string.IsNullOrEmpty(job.PublishUrl)){                
                job.StatusLocation = s3link;
                job.Publish(job.PublishUrl, job.PublishType);
                return job.StatusLocation;
            }

            return job.StatusLocation;
        }

    }
}

