using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft;
using Newtonsoft.Json;

namespace Terradue.Tep { 

    public class Link {
        [DataMember(Name = "rel")]
        public string Rel { get; set; }

        [DataMember(Name = "type")]
        public string Type { get; set; }

        [DataMember(Name = "title")]
        public string Title { get; set; }

        [DataMember(Name = "href")]
        public string Href { get; set; }
    }

    public class ValueDefinition {
        [DataMember(Name = "anyValue")]
        public bool AnyValue { get; set; }
        
        [DataMember(Name = "allowedValues")]
        public List<string> AllowedValues { get; set; }
    }

    public class LiteralDataDomain {
        [DataMember(Name = "dataType")]
        public NameReferenceType DataType { get; set; }

        [DataMember(Name = "valueDefinition")]
        public ValueDefinition ValueDefinition { get; set; }

        /// <summary>
        /// Gets or Sets DefaultValue
        /// </summary>
        [DataMember(Name = "defaultValue", EmitDefaultValue = false)]
        public string DefaultValue { get; set; }

        /// <summary>
        /// Gets or Sets Uom
        /// </summary>
        [DataMember(Name = "uom", EmitDefaultValue = false)]
        public NameReferenceType Uom { get; set; }

    }

    public class NameReferenceType {
        [DataMember(Name = "name")]
        public string Name { get; set; }

        [DataMember(Name = "reference")]
        public string Reference { get; set; }
    }

    public class Input {
        [DataMember(Name = "literalDataDomains")]
        public List<LiteralDataDomain> LiteralDataDomains { get; set; }
    }

    public class Inputs {
        [DataMember(Name = "id")]
        public string Id { get; set; }

        [DataMember(Name = "title")]
        public string Title { get; set; }

        [DataMember(Name = "abstract")]
        public string Abstract { get; set; }

        [DataMember(Name = "minOccurs")]
        public string MinOccurs { get; set; }

        [DataMember(Name = "maxOccurs")]
        public string MaxOccurs { get; set; }

        [DataMember(Name = "input")]
        public Input Input { get; set; }
    }

    public class Format {
        [DataMember(Name = "default")]
        public bool Default { get; set; }

        [DataMember(Name = "mimeType")]
        public string MimeType { get; set; }
    }

    public class Output {
        [DataMember(Name = "formats")]
        public List<Format> Formats { get; set; }
    }

    public class Outputs {
        [DataMember(Name = "id")]
        public string Id { get; set; }

        [DataMember(Name = "title")]
        public string Title { get; set; }

        [DataMember(Name = "output")]
        public Output Output { get; set; }
    }

    public class Process {
        [DataMember(Name = "id")]
        public string Id { get; set; }

        [DataMember(Name = "title")]
        public string Title { get; set; }

        [DataMember(Name = "abstract")]
        public string Abstract { get; set; }

        [DataMember(Name = "version")]
        public string Version { get; set; }

        [DataMember(Name = "jobControlOptions")]
        public List<string> JobControlOptions { get; set; }

        [DataMember(Name = "outputTransmission")]
        public List<string> OutputTransmission { get; set; }

        [DataMember(Name = "links")]
        public List<Link> Links { get; set; }

        [DataMember(Name = "inputs")]
        public List<Inputs> Inputs { get; set; }

        [DataMember(Name = "outputs")]
        public List<Output> Outputs { get; set; }
    }

    public class ResultOutputs {
        [DataMember(Name = "outputs")]
        public List<ResultOutput> outputs { get; set; }
    }

    public class ResultOutput {
        [DataMember(Name = "id")]
        public string id { get; set; }

        [DataMember(Name = "value")]
        public Href value { get; set; }
    }

    public class Href {
        [DataMember(Name = "href")]
        public string href { get; set; }
    }


    public class Wps3 {
        [DataMember(Name = "process")]
        public Process Process { get; set; }
    }

    public class StacItemResult {
        [DataMember(Name = "StacCatalogUri")]
        public string StacCatalogUri { get; set; }

        [DataMember(Name = "s3_catalog_output")]
        public string S3CatalogOutput { get; set; }
    }

    public class SupervisorPublish {
        [DataMember]
        public string Url { get; set; }
        [DataMember]
        public string AuthorizationHeader { get; set; }
        [DataMember]
        public string Index { get; set; }
        [DataMember]
        public bool CreateIndex { get; set; }
        [DataMember]
        public List<Wps3Utils.SyndicationCategory> Categories { get; set; }
        [DataMember]
        public List<Wps3Utils.SyndicationLink> Links { get; set; }
    }

    public class SupervisorUserImportProduct {
        [JsonProperty("url")]
        public string Url { get; set; }
        [JsonProperty("activation_id")]
        public int ActivationId { get; set; }
        [JsonProperty("additional_links")]
        public List<SupervisorUserImportProductLink> AdditionalLinks { get; set; }
        [JsonProperty("properties")]        
        public Dictionary<string, object> AdditionalProperties { get; set; } = new Dictionary<string, object>();
    }

    public class SupervisorUserImportProductLink {
        [JsonProperty("type")]
        public string Type { get; set; }
        [JsonProperty("rel")]
        public string Rel { get; set; }
        [JsonProperty("title")]
        public string Title { get; set; }
        [JsonProperty("href")]
        public string Href { get; set; }
    }

    public class SupervisorDelete {
        [DataMember]
        public string type { get; set; }
        [DataMember]
        public string identifier { get; set; }
        [DataMember]
        public bool async { get; set; }
    }
}


namespace Terradue.Tep.Wps3Utils {

    public class SyndicationCategory {
        [DataMember(Name = "name")]
        public string Name { get; set; }
        [DataMember(Name = "label")]
        public string Label { get; set; }
        [DataMember(Name = "scheme")]
        public string Scheme { get; set; }
    }

    public class SyndicationLink {
        [DataMember(Name = "title")]
        public string Title { get; set; }
        [DataMember(Name = "rel")]
        public string Rel { get; set; }
        [DataMember(Name = "type")]
        public string Type { get; set; }
        [DataMember(Name = "href")]
        public string Href { get; set; }
        [DataMember(Name = "attributes")]
        public List<KeyValuePair<string, string>> Attributes { get; set; }
    }

}

namespace Terradue.Tep {

    public class StacLink {
        [DataMember(Name = "type")]
        public string Type { get; set; }

        [DataMember(Name = "rel")]
        public string Rel { get; set; }

        [DataMember(Name = "title")]
        public string Title { get; set; }

        [DataMember(Name = "href")]
        public string Href { get; set; }
    }

    public class StacItem {
        [DataMember(Name = "id")]
        public string Id { get; set; }

        [DataMember(Name = "status")]
        public string Status { get; set; }

        [DataMember(Name = "stac_version")]
        public string StacVersion { get; set; }

        [DataMember(Name = "links")]
        public List<StacLink> Links { get; set; }

        [DataMember(Name = "type")]
        public string Type { get; set; }

        [DataMember(Name = "description")]
        public string Description { get; set; }
    }
}

