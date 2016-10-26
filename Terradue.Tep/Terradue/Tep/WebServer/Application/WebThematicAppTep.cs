using System;
using System.Collections.Generic;
using ServiceStack.Common.Web;
using ServiceStack.ServiceHost;
using Terradue.Portal;
using Terradue.WebService.Model;

namespace Terradue.Tep.WebServer {

    [Route("/apps", "GET", Summary = "GET a list of thematic apps", Notes = "")]
    public class ThematicAppGetRequestTep : IReturn<List<WebThematicAppTep>>{}

<<<<<<< HEAD
    [Route("/apps/search", "GET", Summary = "GET a list of thematic apps", Notes = "")]
    public class ThematicAppSearchRequestTep : IReturn<List<HttpResult>>{}

    public class WebThematicAppTep : WebEntity {
        
        public WebThematicAppTep() {}
=======
    [Route ("/apps/description", "GET", Summary = "GET a list of thematic apps", Notes = "")]
    public class ThematicAppsDescriptionRequestTep : IReturn<List<HttpResult>> { }

    [Route ("/apps/search", "GET", Summary = "GET a list of thematic apps", Notes = "")]
    public class ThematicAppsSearchRequestTep : IReturn<List<HttpResult>> { }

    [Route("/apps/{identifier}/description", "GET", Summary = "GET a list of thematic apps", Notes = "")]
    public class ThematicAppDescriptionRequestTep : IReturn<List<HttpResult>>{
        [ApiMember (Name = "identifier", Description = "Identifier", ParameterType = "query", DataType = "string", IsRequired = true)]
        public string Identifier { get; set; }
    }

    [Route("/apps/{identifier}/search", "GET", Summary = "GET a list of thematic apps", Notes = "")]
    public class ThematicAppSearchRequestTep : IReturn<List<HttpResult>>{
        [ApiMember (Name = "identifier", Description = "Identifier", ParameterType = "query", DataType = "string", IsRequired = true)]
        public string Identifier { get; set; }
    }

    public class WebThematicAppTep : WebDataPackageTep {
        
        public WebThematicAppTep() : base() {}
>>>>>>> origin/feature/apps

        /// <summary>
        /// Initializes a new instance of the <see cref="Terradue.Tep.WebServer.WebThematicAppTep"/> class.
        /// </summary>
        /// <param name="entity">Entity.</param>
<<<<<<< HEAD
        public WebThematicAppTep(ThematicApplication entity) : base(entity) {
=======
        public WebThematicAppTep(ThematicApplicationSet entity) : base(entity) {
>>>>>>> origin/feature/apps
        }

        /// <summary>
        /// Tos the entity.
        /// </summary>
        /// <returns>The entity.</returns>
        /// <param name="context">Context.</param>
        /// <param name="input">Input.</param>
<<<<<<< HEAD
        public ThematicApplication ToEntity(IfyContext context, WpsJob input){
//            ThematicApplication entity = (input == null ? new ThematicApplication(context) : input);
            ThematicApplication entity = null;
=======
        public ThematicApplicationSet ToEntity(IfyContext context, ThematicApplicationSet input){
            ThematicApplicationSet entity = (ThematicApplicationSet)base.ToEntity (context, input);
>>>>>>> origin/feature/apps

            return entity;
        }

    }
}

