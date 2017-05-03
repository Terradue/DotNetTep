using System;
using System.Collections.Generic;
using ServiceStack.ServiceHost;
using Terradue.Portal;

namespace Terradue.Tep {

    [Route("/dbconfig", "GET", Summary = "GET list of all db configs", Notes = "")]
    public class GetDBConfigRequestTep : IReturn<List<WebDBConfig>> {}

    [Route("/dbconfig", "POST", Summary = "POST db config", Notes = "")]
    public class CreateDBConfigRequestTep : WebDBConfig, IReturn<List<WebDBConfig>> {}

    [Route("/dbconfig", "PUT", Summary = "POST db config", Notes = "")]
    public class UpdateDBConfigRequestTep : WebDBConfig, IReturn<List<WebDBConfig>> { }

    [Route("/dbconfig", "DELETE", Summary = "DELETE db config", Notes = "")]
    public class DeleteDBConfigRequestTep : WebDBConfig, IReturn<List<WebDBConfig>> { }

    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// User.
    /// </summary>
    public class WebDBConfig {

        [ApiMember(Name = "name", Description = "config name", ParameterType = "query", DataType = "string", IsRequired = false)]
        public string Name { get; set; }

        [ApiMember(Name = "value", Description = "config value", ParameterType = "query", DataType = "string", IsRequired = false)]
        public string Value { get; set; }

        [ApiMember(Name = "type", Description = "config type", ParameterType = "query", DataType = "string", IsRequired = false)]
        public string Type { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Terradue.Tep.WebServer.WebUserTep"/> class.
        /// </summary>
        public WebDBConfig() { }

    }
}
