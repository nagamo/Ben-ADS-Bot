// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace ADS.Bot.V1.Models
{
    public class VehicleInventoryDetails
    {
        public bool IsCompleted
        {
            get
            {
                return
                    !string.IsNullOrEmpty(Goals) &&
                    !string.IsNullOrEmpty(LevelOfInterest) &&
                    !string.IsNullOrEmpty(Type) &&
                    !string.IsNullOrEmpty(Brand) &&
                    !string.IsNullOrEmpty(NewUsed) &&
                    !string.IsNullOrEmpty(Budget);
            }
        }

        public string Goals { get; set; }
        public string LevelOfInterest { get; set; }
        public string Type { get; set; }
        public string Brand { get; set; }
        public string NewUsed { get; set; }
        public string Budget { get; set; }
        public bool? NeedFinancing { get; set; }
        public bool? TradingIn { get; set; }
    }

}