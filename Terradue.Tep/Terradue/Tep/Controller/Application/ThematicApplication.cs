using System;

/*! 
\defgroup TepApplication Application
@{

This component manages the TEP applications

Thematic Application brings a simple way to define an application of a specific aspect of the thematic.
It specifies togheter the form of the application, its features such as the map and the layers, its data and services.


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

