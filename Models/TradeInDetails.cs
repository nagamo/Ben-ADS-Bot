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
                    !string.IsNullOrEmpty(Make) &&
                    !string.IsNullOrEmpty(Model) &&
                    !string.IsNullOrEmpty(Year) &&
                    !string.IsNullOrEmpty(Condition) &&
                    !string.IsNullOrEmpty(AmountOwed);
            }
        }

        public long? ADS_CRM_ID { get; set; } = null;

        public string Make { get; set; }
        public string Model { get; set; }
        public string Year { get; set; }
        public string Mileage { get; set; }
        public string Condition { get; set; }
        public string AmountOwed { get; set; }



        [JsonIgnore]
        public bool SkipMake { get { return !string.IsNullOrEmpty(Make); } }

        [JsonIgnore]
        public bool SkipModel { get { return !string.IsNullOrEmpty(Model); } }

        [JsonIgnore]
        public bool SkipYear { get { return !string.IsNullOrEmpty(Year); } }

        [JsonIgnore]
        public bool SkipMileage { get { return !string.IsNullOrEmpty(Mileage); } }

        [JsonIgnore]
        public bool SkipCondition { get { return !string.IsNullOrEmpty(Condition); } }

        [JsonIgnore]
        public bool SkipAmountOwed { get { return !string.IsNullOrEmpty(AmountOwed); } }
    }

}