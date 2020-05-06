// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace ADS.Bot.V1.Models
{
    public class VehicleInventoryDetails : IADSCRMRecord
    {
        public bool IsCompleted
        {
            get
            {
                return
                    !string.IsNullOrEmpty(Make) &&
                    !string.IsNullOrEmpty(Model) &&
                    !string.IsNullOrEmpty(Year) &&
                    !string.IsNullOrEmpty(Color);
            }
        }

        public long? ADS_CRM_ID { get; set; } = null;

        public string Make { get; set; }
        public string Model { get; set; }
        public string Year { get; set; }
        public string Color { get; set; }
        public bool? NeedFinancing { get; set; }
        public bool? TradingIn { get; set; }
    }

}