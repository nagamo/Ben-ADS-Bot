// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Schema;
using System.Linq;

namespace ADS.Bot.V1.Models
{
    /// <summary>
    /// This is our application state. Just a regular serializable .NET class.
    /// </summary>
    public class UserProfile
    {
        public bool IsRegistered { get { return Details != null && ADS_CRM_ID.HasValue; } }

        public string Name { get { return Details?.Name; } }
        public string FirstName { get { return Details?.Name?.Split(' ')?.First(); } }
        public string LastName { get { return Details?.Name?.Split(' ', 2)?.Last(); } }

        public BasicDetails Details { get; set; }
        public FinancingDetails Financing { get; set; }
        public VehicleInventoryDetails VehicleProfile { get; set; }
        public TradeInDetails TradeDetails { get; set; }

        public long? ADS_CRM_ID { get; set; } = null;
    }
}