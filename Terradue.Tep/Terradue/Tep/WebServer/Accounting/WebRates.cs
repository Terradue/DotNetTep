using ServiceStack.ServiceHost;
using Terradue.Portal;
using Terradue.WebService.Model;
using System.Collections.Generic;

namespace Terradue.Tep.WebServer {

    [Route("/service/wps/{serviceIdentifier}/rates", "GET", Summary = "GET service rates as opensearch", Notes = "")]
    public class RatesForServiceRequestTep : IReturn<List<WebRates>>{
        [ApiMember(Name = "serviceIdentifier", Description = "service Identifier", ParameterType = "path", DataType = "string", IsRequired = true)]
        public string ServiceIdentifier { get; set; }
    }

    [Route("/service/wps/{serviceIdentifier}/rates", "POST", Summary = "POST a rate", Notes = "")]
    public class AddRatesForServiceRequestTep : WebRates {
        [ApiMember(Name = "serviceIdentifier", Description = "service Identifier", ParameterType = "path", DataType = "string", IsRequired = false)]
        public string ServiceIdentifier { get; set; }
    }

    [Route("/service/wps/{serviceIdentifier}/rates/{id}", "DELETE", Summary = "DELETE a rate", Notes = "")]
    public class DeleteRatesFromServiceRequestTep {
        [ApiMember(Name = "serviceIdentifier", Description = "service Identifier", ParameterType = "path", DataType = "string", IsRequired = false)]
        public string ServiceIdentifier { get; set; }

        [ApiMember(Name = "id", Description = "rates ID", ParameterType = "path", DataType = "int", IsRequired = false)]
        public int Id { get; set; }
    }

    public class WebRates : WebEntity {
        
        [ApiMember(Name="Unit", Description = "Rate unit", ParameterType = "path", DataType = "long", IsRequired = false)]
        public long Unit { get; set; }

        [ApiMember(Name="Cost", Description = "Rate cost", ParameterType = "path", DataType = "double", IsRequired = false)]
        public double Cost { get; set; }

        public WebRates() {}

        public WebRates(Rates entity) : base(entity){            
            this.Unit = entity.Unit;
            this.Cost = entity.Cost;
        }

        public Rates ToEntity(IfyContext context, Entity entity){
            Rates rate = new Rates(context);

            rate.Identifier = this.Identifier;
            rate.Unit = this.Unit;
            rate.Cost = this.Cost;
            rate.Service = entity;

            return rate;
        }

    }
}


