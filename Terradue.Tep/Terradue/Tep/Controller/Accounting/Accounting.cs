using System;


/*! 
\defgroup TepAccounting Accounting
@{

This component makes the integrated accounting of the TEP.

Periodically, it collects the usage records from the \ref ApelServer and use them to balance
the user accounts with credits.

\xrefitem int "Interfaces" "Interfaces" connects \ref ApelReporting to retrieve the aggregated usage records.

\xrefitem dep "Dependencies" "Dependencies" \ref Persistence stores the user accounts in the database

\ingroup Tep

@}
*/

namespace Terradue.Tep.Controller {
    public class Accounting {
        public Accounting() {
        }
    }
}

