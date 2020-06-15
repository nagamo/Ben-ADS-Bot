// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace ADS.Bot.V1.Models
{
    public class BasicDetails
    {
        public bool IsCompleted
        {
            get
            {
                return !string.IsNullOrEmpty(Name) &&
                    (
                        !string.IsNullOrEmpty(Phone) ||
                        !string.IsNullOrEmpty(Email)
                    );
                    //&& !string.IsNullOrEmpty(Timeframe);
            }
        }


        //These are used for CRM
        public string UniqueID { get; set; }
        public string DealerID { get; set; }



        public string Name { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        
        public string Timeframe { get; set; }
        public bool RequestContact { get; set; } = false;
        public bool New { get; set; }
    }

}