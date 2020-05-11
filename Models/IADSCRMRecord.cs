using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ADS.Bot.V1.Models
{
    interface IADSCRMRecord
    {
        public bool IsCompleted { get; }
        public long? ADS_CRM_ID { get; set; }
    }
}
