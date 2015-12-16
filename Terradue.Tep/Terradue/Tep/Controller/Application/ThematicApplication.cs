﻿using System;

/*! 
\defgroup TepApplication Application
@{

This component manages the TEP applications

Thematic Application brings a simple way to define an application of a specific aspect of the thematic.
It specifies togheter the form of the application, its features such as the map and the layers, its data and services.

Thematic Application over OWS data model
----------------------------------------

<em>This section shall further describe how the component uses the OWS Context model to represent the dataset collections, the services, maps, features layers...</em>

\xrefitem dep "Dependencies" "Dependencies" delegates \ref TepData for the data management in the application with the definition of the collection and data packages references.

\xrefitem dep "Dependencies" "Dependencies" delegates \ref TepService for the processing service management in the application with the defintion of the service references .

\ingroup Tep

@}
*/
using System.Collections.Generic;
using Terradue.Portal;

namespace Terradue.Tep {

    /// <summary>
    /// Thematic application.
    /// </summary>
    /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation" 
    /// \ingroup TepApplication
    public class ThematicApplication : Entity {
        public ThematicApplication() : base(null){
        }

        /// <summary>
        /// Collections
        /// </summary>
        /// <value>contains \ref Collection</value>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation" 
        public List<Collection> Collections {
            get;
            set;
        }

        /// <summary>
        /// WPS Services
        /// </summary>
        /// <value>contains \ref WpsProcessOffering</value>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation" 
        public List<WpsProcessOffering> WPSServices {
            get;
            set;
        }

        /// <summary>
        /// Title
        /// </summary>
        /// <value>The title.</value>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation" 
        public string Description {
            get;
            set;
        }

        /// <summary>
        /// Features and widgets defining the application
        /// </summary>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation" 
        public List<string> Features {
            get;
            set;
        }
    }
}

