﻿using System;
using System.Collections.Generic;
using ServiceStack.ServiceHost;
using Terradue.Portal;
using Terradue.Tep;
using Terradue.WebService.Model;

namespace Terradue.Tep.Services {

    [Route("/servicestore", "GET", Summary = "GET WPS services for store", Notes = "")]
    public class GetServiceStoreRequestTep : IReturn<List<WebStoreService>> {
    }

    [Route("/servicestore/{Id}", "DELETE", Summary = "DELETE WPS services for store", Notes = "")]
    public class DeleteServiceStoreRequestTep : IReturn<List<WebStoreService>> {
        [ApiMember(Name = "Id", Description = "ServicePack", ParameterType = "query", DataType = "int", IsRequired = true)]
        public int Id { get; set; }
    }

    [Route("/servicestore", "POST", Summary = "POST WPS services for store", Notes = "")]
    public class PostServiceStoreRequestTep : WebStoreService, IReturn<List<WebStoreService>> {
    }

    [Route("/servicestore", "PUT", Summary = "PUT WPS services for store", Notes = "")]
    public class PutServiceStoreRequestTep : WebStoreService, IReturn<List<WebStoreService>> {
    }

    [Route("/servicestore/config", "GET", Summary = "GET WPS services for store", Notes = "")]
    public class GetServiceStoreConfigRequestTep : IReturn<List<KeyValuePair<string,string>>> {
    }

    [Route("/servicestore/config/packs", "POST", Summary = "GET WPS services for store", Notes = "")]
    public class PostServiceStorePacksConfigRequestTep : List<KeyValuePair<string,string>> {
        [ApiMember(Name = "packs", Description = "ServicePack", ParameterType = "query", DataType = "string", IsRequired = true)]
        public string Packs { get; set; }
    }

    [Route("/servicestore/config/subpacks", "POST", Summary = "GET WPS services for store", Notes = "")]
    public class PostServiceStoreSubpacksConfigRequestTep : List<KeyValuePair<string,string>> {
        [ApiMember(Name = "subpacks", Description = "ServicePack", ParameterType = "query", DataType = "string", IsRequired = true)]
        public string Subpacks { get; set; }
    }

    public class WebStoreService : WebEntity {
        [ApiMember(Name = "ServicePack", Description = "ServicePack", ParameterType = "query", DataType = "string", IsRequired = true)]
        public string ServicePack { get; set; }
        [ApiMember(Name = "ServiceLevel", Description = "ServiceLevel", ParameterType = "query", DataType = "string", IsRequired = true)]
        public string ServiceLevel { get; set; }
        [ApiMember(Name = "Description", Description = "Description", ParameterType = "query", DataType = "string", IsRequired = true)]
        public string Description { get; set; }
        [ApiMember(Name = "Abstract", Description = "Abstract", ParameterType = "query", DataType = "string", IsRequired = true)]
        public string Abstract { get; set; }
        [ApiMember(Name = "Link", Description = "Link", ParameterType = "query", DataType = "string", IsRequired = true)]
        public string Link { get; set; }
        // [ApiMember(Name = "Tag", Description = "Tag", ParameterType = "query", DataType = "string", IsRequired = true)]
        // public string Tag { get; set; }
        [ApiMember(Name = "IconUrl", Description = "IconUrl", ParameterType = "query", DataType = "string", IsRequired = true)]
        public string IconUrl { get; set; }
        [ApiMember(Name = "Apps", Description = "Apps", ParameterType = "query", DataType = "string", IsRequired = true)]
        public string Apps { get; set; }
        [ApiMember(Name = "WpsName", Description = "WpsName", ParameterType = "query", DataType = "string", IsRequired = true)]
        public string WpsName { get; set; }    
        [ApiMember(Name = "Price", Description = "Price", ParameterType = "query", DataType = "double", IsRequired = true)]
        public double Price { get; set; }    


        public WebStoreService() { }

        public WebStoreService(StoreService entity) : base(entity) {
            this.WpsName = entity.WpsName;
            this.ServicePack = entity.ServicePack;
            this.ServiceLevel = entity.ServiceLevel;
            this.Description = entity.Description;
            this.Abstract = entity.Abstract;
            this.Link = entity.Link;
            // this.Tag = entity.Tag;
            this.IconUrl = entity.IconUrl;
            this.Apps = entity.Apps;
            this.Price = entity.Price;
        }

        /// <summary>
        /// Tos the entity.
        /// </summary>
        /// <returns>The entity.</returns>
        /// <param name="context">Context.</param>
        /// <param name="input">Input.</param>
        public StoreService ToEntity(IfyContext context, StoreService input) {
            StoreService entity = (input == null ? new StoreService(context) : input);

            entity.Identifier = this.Identifier;
            entity.Name = this.Name;
            entity.WpsName = this.WpsName;
            entity.ServicePack = this.ServicePack;
            entity.ServiceLevel = this.ServiceLevel;
            entity.Description = this.Description;
            entity.Abstract = this.Abstract;
            entity.Link = this.Link;
            // entity.Tag = this.Tag;
            entity.IconUrl = this.IconUrl;
            entity.Apps = this.Apps;
            entity.Price = this.Price;
            return entity;
        }

    }
}
