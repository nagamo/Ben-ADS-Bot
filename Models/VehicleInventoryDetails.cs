using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ADS.Bot.V1.Models
{
    public class VehicleInventoryDetails : IADSCRMRecord
    {
        public bool IsCompleted
        {
            get
            {
                return
                    !string.IsNullOrEmpty(PrimaryConcern) &&
                    !string.IsNullOrEmpty(ConcernGoal);
            }
        }

        public long? ADS_CRM_ID { get; set; } = null;

        public string PrimaryConcern { get; set; }
        public string ConcernGoal { get; set; }

        [JsonIgnore]
        public bool SkipPrimaryConcern
        { get { return !string.IsNullOrEmpty(PrimaryConcern); } }

        [JsonIgnore]
        public bool SkipConcernGoal
        { get { return !string.IsNullOrEmpty(ConcernGoal); } }
    }
}
