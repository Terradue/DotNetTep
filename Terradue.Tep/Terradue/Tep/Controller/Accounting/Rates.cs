using System;
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
    public class Rates {
        public Rates() {
        }

        /// <summary>
        /// Service the rate is applicable to.
        /// </summary>
        /// <value>The service.</value>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation" 
        public Entity Service {
            get;
            set;
        }

        /// <summary>
        /// Cost unit
        /// </summary>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation" 
        public string Unit {
            get;
            set;
        }

        /// <summary>
        /// Cost
        /// </summary>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation" 
        public double Cost {
            get;
            set;
        }

    }
}

