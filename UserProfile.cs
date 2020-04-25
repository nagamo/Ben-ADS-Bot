// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Schema;
using System.Linq;

namespace ADS.Bot1
{
    /// <summary>
    /// This is our application state. Just a regular serializable .NET class.
    /// </summary>
    public class UserProfile
    {
        public bool IsRegistered { get { return Details != null; } }

        public string Name { get { return Details?.Name; } }
        public string FirstName { get { return Details?.Name?.Split(' ')?.First(); } }

        public BasicDetails Details { get; set; }
        public FinancingDetails Financing { get; set; }
        public VehicleProfileDetails VehicleProfile { get; set; }
        public TradeInDetails TradeDetails { get; set; }
    }

    public class BasicDetails
    {
        public string Name { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
    }

    public class VehicleProfileDetails
    {
        public bool IsCompleted
        {
            get
            {
                return
                    !string.IsNullOrEmpty(Goals) &&
                    !string.IsNullOrEmpty(Type) &&
                    !string.IsNullOrEmpty(NewUsed) &&
                    !string.IsNullOrEmpty(Make) &&
                    !string.IsNullOrEmpty(Model) &&
                    !string.IsNullOrEmpty(Color);
            }
        }

        public string Goals { get; set; }
        public string Type { get; set; }
        public string NewUsed { get; set; }
        public string Make { get; set; }
        public string Model { get; set; }
        public string Color { get; set; }

    }

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

        public string Make { get; set; }
        public string Model { get; set; }
        public string Year { get; set; }
        public string Condition { get; set; }
        public string AmountOwed { get; set; }
    }

    public class FinancingDetails
    {
        public bool IsCompleted
        {
            get
            {
                return
                    !string.IsNullOrEmpty(CreditScore) &&
                    !string.IsNullOrEmpty(Income) &&
                    !string.IsNullOrEmpty(HomeOwnership) &&
                    !string.IsNullOrEmpty(Employment);
            }
        }

        public string CreditScore { get; set; }
        public string Income { get; set; }
        public string HomeOwnership { get; set; }
        public string Employment { get; set; }
    }
}