using System;

/*! 
\defgroup TepApplication Application
@{

This component manages the TEP applications

Thematic Application brings a simple way to define an application of a specific aspect of the thematic.
It specifies togheter the form of the application, its features such as the map and the layers, its data and services.

\xrefitem dep "Dependencies" "Dependencies" delegates \ref TepData for the data management in the application with the definition of the collection and data packages references.

\xrefitem dep "Dependencies" "Dependencies" delegates \ref TepService for the processing service management in the application with the defintion of the service references .

\ingroup Tep

@}
*/
using System.Collections.Generic;

namespace Terradue.Tep.Controller {
    public class ThematicApplication {
        public ThematicApplication() {
        }

        public List<Collection> Collections {
            get;
            set;
        }
    }
}

