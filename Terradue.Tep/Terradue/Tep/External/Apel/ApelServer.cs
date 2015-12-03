using System;

/*! 

\defgroup Apel Apel
@{

APEL is an accounting tool that collects accounting data from sites participating in the EGI and WLCG infrastructures as well as from sites belonging to other Grid organisations that are collaborating with EGI, including OSG, NorduGrid and INFN.

The accounting information is gathered from different resource providers into a central accounting database where it is processed to generate statistical summaries that are available through the reporting interface

We use this system in TEP to record the usage on the different resources provider of the platform in order to provide a central accounting mechanism for the platform.

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

