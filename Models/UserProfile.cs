// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using System;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace ADS.Bot.V1.Models
{
    /// <summary>
    /// This is our application state. Just a regular serializable .NET class.
    /// </summary>
    public class UserProfile
    {
        public bool IsRegistered { get { return Details?.IsCompleted ?? false; } }

        public string Name { get { return Details?.Name; } }
        public string FirstName { get { return Details?.Name?.Split(' ')?.First(); } }
        public string LastName
        {
            get
            {
                var nameParts = Details?.Name?.Split(' ', 2);
                if(nameParts?.Length == 2)
                {
                    return nameParts.Last();
                }
                else { return ""; }
            }
        }

        public BasicDetails Details { get; set; }
        public FinancingDetails Financing { get; set; }
        public VehicleProfileDetails VehicleProfile { get; set; }
        public TradeInDetails TradeDetails { get; set; }
        public VehicleInventoryDetails Inventory { get; set; }
        public SimpleInventoryDetails SimpleInventory { get; set; }

        public long? ADS_CRM_ID { get; set; } = null;
        public string BB_CRM_ID { get; internal set; }
    }
}