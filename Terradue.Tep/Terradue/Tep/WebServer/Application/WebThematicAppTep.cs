using System;
using System.Collections.Generic;
using ServiceStack.Common.Web;
using ServiceStack.ServiceHost;
using Terradue.Portal;
using Terradue.WebService.Model;

namespace Terradue.Tep.WebServer {

    [Route("/apps", "GET", Summary = "GET a list of thematic apps", Notes = "")]
    public class ThematicAppGetRequestTep : IReturn<List<WebThematicAppTep>>{}

    [Route("/apps/description", "GET", Summary = "GET OSDD of thematic apps", Notes = "")]
    public class ThematicAppDescriptionRequestTep : IReturn<List<HttpResult>>{
    }

    [Route("/apps/search", "GET", Summary = "search for thematic apps", Notes = "")]
    public class ThematicAppSearchRequestTep : IReturn<List<HttpResult>>{
    }

    public class WebThematicAppTep : WebDataPackageTep {
        
        public WebThematicAppTep() : base() {}

        /// <summary>
        /// Initializes a new instance of the <see cref="Terradue.Tep.WebServer.WebThematicAppTep"/> class.
        /// </summary>
        /// <param name="entity">Entity.</param>
        public WebThematicAppTep(ThematicApplication entity) : base(entity) {
        }

        /// <summary>
        /// Tos the entity.
        /// </summary>
        /// <returns>The entity.</returns>
        /// <param name="context">Context.</param>
        /// <param name="input">Input.</param>
        public ThematicApplication ToEntity(IfyContext context, ThematicApplication input){
            ThematicApplication entity = (ThematicApplication)base.ToEntity (context, input);

            return entity;
        }

    }
}

