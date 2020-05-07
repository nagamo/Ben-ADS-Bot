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
                return
                    !string.IsNullOrEmpty(Name) &&
                    !string.IsNullOrEmpty(Phone) &&
                    !string.IsNullOrEmpty(Email);
            }
        }


        public string Name { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        
        //Not currently used.
        public string Focus { get; set; }
        public string Timeframe { get; set; }
    }

}