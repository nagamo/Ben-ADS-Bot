using ADS.Bot.V1.Models;
using ADS.Bot1;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ADS.Bot.V1.Services
{
    public class BuyerBridgeAPIService
    {
        public IConfiguration Configuration { get; }

        public BuyerBridgeAPIService(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public void CreateUpdateLead(UserProfile profile)
        {
            if (string.IsNullOrEmpty(profile.Details.DealerID))
                throw new ArgumentNullException("UserProfile.Details.DealerID cannot be empty");

            var profileData = new BB_Lead()
            {
                Dealer_ID = profile.Details.DealerID,
                Lead_Platform = "Bot Chat",
                Remote_ID = profile.Details.UniqueID,
                Customer_Name = profile.Details.Name,
                Customer_Email = profile.Details.Email,
                Customer_Phone_Number = profile.Details.Phone,
                Data = new BB_AdditionalDetails()
            };

            if(profile.Financing?.IsCompleted ?? false)
            {
                profileData.Data.Field_Data = new List<BB_Field>();

                var lines = profile.Financing.GoodCredit ?
                    new string[]
                    {
                        $"Credit Score: {profile.Financing.CreditScore}"
                    } :
                    new string[]
                    {
                        $"Credit Score: {profile.Financing.CreditScore}",
                        $"Income: {profile.Financing.Income}",
                        $"Home Ownership: {profile.Financing.HomeOwnership}",
                        $"Employment History: {profile.Financing.Employment}",
                    };

                profileData.Data.Field_Data.Add(new BB_Field()
                {
                    Name = "Financing",
                    Values = new List<string>(lines)
                });
            }

            if(profile.Inventory?.IsCompleted ?? false)
            {
                var inventory = profile.Inventory;

                var used = inventory.NewUsed == "Used";

                profileData.Data.Purchase_Vehicle = new BB_PurchaseDetails()
                {
                    Used = used,
                    Make_Name = inventory.Make,
                    Model_Name = inventory.Model,
                    //Year = inventory.Year,
                    Condition = used ? "Used" : "New",
                };
            }

            if(profile.TradeDetails?.IsCompleted ?? false)
            {
                var tradein = profile.TradeDetails;

                profileData.Data.TradeIn_Vehicle = new BB_VehicleDetails()
                {
                    Make_Name = tradein.Make,
                    Model_Name = tradein.Model,
                    Year = tradein.Year,
                    Condition = tradein.Condition
                };

                if (!string.IsNullOrEmpty(tradein.Mileage))
                {
                    profileData.Data.TradeIn_Vehicle.Odometer = new BB_Odometer()
                    {
                        Value = tradein.Mileage
                    };
                }
            }
        }
    }

    public class BB_Lead
    {
        public string Dealer_ID { get; set; }
        public string Lead_Platform { get; set; }
        public string Remote_ID { get; set; }
        public string Customer_Name { get; set; }
        public string Customer_Email { get; set; }
        public string Customer_Phone_Number { get; set; }
        public BB_AdditionalDetails Data { get; set; }
    }

    public class BB_AdditionalDetails
    {
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public BB_EmailDetails Email { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public BB_PurchaseDetails Purchase_Vehicle { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public BB_VehicleDetails TradeIn_Vehicle { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public List<BB_Field> Field_Data { get; set; }
    }

    public class BB_EmailDetails
    {
        public string Subject { get; set; }
        public string Source { get; set; }
        public string Interest_Type { get; set; }
    }

    public class BB_VehicleDetails
    {
        public string Year { get; set; }
        public string Make_Name { get; set; }
        public string Model_Name { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public BB_Odometer Odometer { get; set; }
        public string Condition { get; set; }
    }

    public class BB_Odometer
    {
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string Status { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string Value { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string Units { get; set; }
    }

    public class BB_PurchaseDetails : BB_VehicleDetails
    {
        public bool Used { get; set; }
    }

    public class BB_Field
    {
        public string Name { get; set; }
        public List<string> Values { get; set; }
    }
}
