// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Newtonsoft.Json;

namespace ADS.Bot.V1.Models
{
    public class TradeInDetails : IADSCRMRecord
    {
        public bool IsCompleted
        {
            get
            {
                return
                    !string.IsNullOrEmpty(Vehicle) &&
                    !string.IsNullOrEmpty(Condition) &&
                    !string.IsNullOrEmpty(AmountOwed);
            }
        }

        public long? ADS_CRM_ID { get; set; } = null;

        public string Vehicle { get; set; }
        public string Mileage { get; set; }
        public string Condition { get; set; }
        public string AmountOwed { get; set; }



        [JsonIgnore]
        public bool SkipVehicle { get { return !string.IsNullOrEmpty(Vehicle); } }

        [JsonIgnore]
        public bool SkipMileage { get { return !string.IsNullOrEmpty(Mileage); } }

        [JsonIgnore]
        public bool SkipCondition { get { return !string.IsNullOrEmpty(Condition); } }

        [JsonIgnore]
        public bool SkipAmountOwed { get { return !string.IsNullOrEmpty(AmountOwed); } }
    }

}