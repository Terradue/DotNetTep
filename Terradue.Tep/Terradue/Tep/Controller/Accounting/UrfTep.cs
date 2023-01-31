using System;
using System.Collections.Generic;
using System.Linq;
using Terradue.Portal;
using Terradue.Portal.Urf;

namespace Terradue.Tep
{    
    
    public class UrfTep : Urf
    {        
        public new UrfCreditInformation UrfCreditInformation { get; set; }

        public UrfTep():base(){            
            UrfCreditInformation = new UrfCreditInformation();        
        } 
    }

    public class UrfCreditInformation
    {        
        public double CreditRemaining { get; set; }
        public double Credit { get; set; }

        public UrfCreditInformation(){
            
        }
    }
}