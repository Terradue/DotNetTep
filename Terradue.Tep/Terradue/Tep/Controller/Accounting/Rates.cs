﻿using System;
using System.Collections.Generic;
using Terradue.Portal;

namespace Terradue.Tep {

    /// <summary>
    /// Rates
    /// </summary>
    /// <description>
    /// This object represents the cost of the usage of an entity
    /// </description>
    /// \ingroup TepAccounting
    /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation" 
    [EntityTable("rate", EntityTableConfiguration.Custom, AutoCheckIdentifiers = false)]
    public class Rates : Entity {

        /// <summary>
        /// Service the rate is applicable to.
        /// </summary>
        /// <value>gives a price to the Service</value>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation" 
        private Entity service;
        public Entity Service {
            get {
                if (service == null) {
                    var etype = EntityType.GetEntityTypeFromId(EntityTypeId);
                    service = etype.GetEntityInstanceFromId(context, this.EntityId);
                }
                return service;
            }
            set {
                service = value;
                this.EntityId = service.Id;
                this.EntityTypeId = EntityType.GetEntityType(service.GetType()).Id;
            }
        }

        [EntityDataField("identifier")]
        public new string Identifier { get; set; }

        /// <summary>
        /// Gets or sets the entity identifier.
        /// </summary>
        /// <value>The entity identifier.</value>
        [EntityDataField("id_entity")]
        public int EntityId { get; set; }

        /// <summary>
        /// Gets or sets the entity type identifier.
        /// </summary>
        /// <value>The entity type identifier.</value>
        [EntityDataField("id_type")]
        public int EntityTypeId { get; set; }

        /// <summary>
        /// Cost unit
        /// </summary>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation" 
        [EntityDataField("unit")]
        public long Unit {
            get;
            set;
        }

        /// <summary>
        /// Cost
        /// </summary>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation" 
        [EntityDataField("cost")]
        public double Cost {
            get;
            set;
        }

        public Rates(IfyContext context) : base(context) {
        }

        public override string GetIdentifyingConditionSql() {
            if (EntityTypeId != 0 && EntityId != 0 && Identifier != null) return String.Format("t.id_type={0} AND t.id_entity={1} AND t.identifier='{2}'", EntityTypeId, EntityId, Identifier);
            return null;
        }

        public static Rates FromId(IfyContext context, int id) {
            Rates result = new Rates(context);
            result.Id = id;
            result.Load();
            return result;
        }

        public static Rates FromEntityAndIdentifier(IfyContext context, Entity service, string identifier) {
            Rates result = new Rates(context);
            result.Service = service;
            result.Identifier = identifier;
            result.Load();
            return result;
        }

        /// <summary>
        /// Gets the balance from rates.
        /// </summary>
        /// <returns>The balance from rates.</returns>
        /// <param name="context">Context.</param>
        /// <param name="entity">Entity.</param>
        /// <param name="quantities">Quantities.</param>
        public static double GetBalanceFromRates(IfyContext context, Entity entity, List<T2AccountingQuantity> quantities) {
            double balance = 0;
            foreach (var quantity in quantities) {
                try {
                    balance += GetBalanceFromRate(context, entity, quantity.id, quantity.value);
                } catch (Exception e) {
                    context.LogError(new Rates(context), e.Message);
                }
            }
            return balance;
        }

        /// <summary>
        /// Gets the balance from rate.
        /// </summary>
        /// <returns>The balance from rate.</returns>
        /// <param name="context">Context.</param>
        /// <param name="entity">Entity.</param>
        /// <param name="id">Identifier.</param>
        /// <param name="value">Value.</param>
        public static double GetBalanceFromRate(IfyContext context, Entity entity, string id, double value) {
            double balance = 0;
            try {
                Rates rates = Rates.FromEntityAndIdentifier(context, entity, id);
                if (rates != null && rates.Unit != 0 && rates.Cost != 0) balance = ((value / rates.Unit) * rates.Cost);
            } catch (Exception e) { 
                context.LogError(context, e.Message);
            }
            return balance;
        }
    }
}

