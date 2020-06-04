using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ADS.Bot.V1.Models
{
    public class SimpleInventoryDetails : IADSCRMRecord
    {
        public bool IsCompleted
        {
            get
            {
                return !string.IsNullOrEmpty(VIN);
            }
        }

        public long? ADS_CRM_ID { get; set; } = null;

        
        public string PrimaryConcern { get; set; }
        public string ConcernGoal { get; set; }
        public string Make { get; set; }
        public string Model { get; set; }

        
        public string VIN { get; set; }

        
        [JsonIgnore]
        public bool SkipPrimaryConcern
        { get { return !string.IsNullOrEmpty(PrimaryConcern); } }

        [JsonIgnore]
        public bool SkipConcernGoal
        { get { return !string.IsNullOrEmpty(ConcernGoal); } }

        [JsonIgnore]
        public bool SkipMake
        { get { return !string.IsNullOrEmpty(Make); } }

        [JsonIgnore]
        public bool SkipModel
        { get { return !string.IsNullOrEmpty(Model); } }
    }
}
