using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

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
    }

    public class JsonUrl {
        [DataMember]
        public string url { get; set; }
    }

}
