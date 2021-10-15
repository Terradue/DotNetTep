﻿using System;
using Terradue.Portal;

namespace Terradue.Tep {

    /// <summary>
    /// TEP Role
    /// </summary>
    /// <description>
    /// This class represents the role of a TEP user in a thematic group.
    /// This role may be be:
    /// - Member (End User). As a member the user can view the group members and the activities reported in relation with the group
    /// - Expert (Expert User). As an expert of the group, the user can develop and deploy new thematic application for the group
    /// - Data Provider. As a data provider for the group, the user can propose new dataset series to the group
    /// - ICT Provider. As an ICT provider for the group, the user can propose ICT resources to the group
    /// - Software Vendor. As an software vendore, the user can propose commercial software to the group
    /// - Manager. As the manager of the group, the user grants or revokes roles to user within the group
    /// </description>
    /// \ingroup TepCommunity
    /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
    public class RoleTep : Role {

        private static readonly log4net.ILog log = log4net.LogManager.GetLogger
            (System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public const string OWNER = "owner";
        public const string MANAGER = "manager";
        public const string MEMBER = "member";
        public const string PENDING = "pending";
        public const string VISITOR = "visitor";
        public const string STARTER = "starter";
        public const string EXPLORER = "explorer";
        public const string ENDUSER = "enduser";
        public const string EXPERT = "expert";

        public RoleTep() : base(null) {
        }

        /// <summary>
        /// Name of the Role
        /// </summary>
        /// <value>The name.</value>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        public new string Name {
            get;
            set;
        }
    }
}

