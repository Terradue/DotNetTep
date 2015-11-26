using System;

namespace Terradue.Tep {

    /// <summary>
    /// Series Filters
    /// </summary>
    /// \ingroup TepData
    /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation" 
    public class Filters {
        public Filters() {
        }

        /// <summary>
        /// Gets or sets the open search parameters.
        /// </summary>
        /// <value>The open search parameters.</value>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation" 
        string OpenSearchParameters {
            get;
            set;
        }
    }
}

