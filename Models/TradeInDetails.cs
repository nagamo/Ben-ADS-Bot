// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace ADS.Bot.V1.Models
{
    public class TradeInDetails
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
    }

}