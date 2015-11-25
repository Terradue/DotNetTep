using System;

/*! 

\defgroup Apel Apel
@{

@}

\defgroup ApelServer Apel Server
@{

    This component is in charge of recording the usage of all the resources for the whole TEP.

    \xrefitem int "Interfaces" "Interfaces" implements \ref ApelAccounting interface

    \xrefitem int "Interfaces" "Interfaces" implements \ref ApelReporting interface

    \ingroup Apel

@}

\defgroup ApelAccounting Apel Accounting
@{

    This is the internal interface for the subsystems to send the usage records to.

    \xrefitem cptype_int "Interfaces" "Interfaces"

    \xrefitem norm "Normative References" "Normative References" [OGC Web Processing Service 1.0](http://portal.opengeospatial.org/files/?artifact_id=24151)

    \ingroup Apel
@}


\defgroup ApelReporting Apel Reporting
@{

    This is the interface to retrieve the aggregated usage records.

    \xrefitem cptype_int "Interfaces" "Interfaces"

    \xrefitem norm "Normative References" "Normative References" [OGC Web Processing Service 1.0](http://portal.opengeospatial.org/files/?artifact_id=24151)

    \ingroup Apel

@}
*/

namespace Terradue.Tep {
    public class ApelServer {
        public ApelServer() {
        }
    }
}

