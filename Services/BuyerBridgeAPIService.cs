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
using System.Net;
using System.Threading.Tasks;

namespace ADS.Bot.V1.Services
{
    public class BuyerBridgeAPIService
    {
        public IConfiguration Configuration { get; }

        private IRestClient apiClient;

        public BuyerBridgeAPIService(IConfiguration configuration)
        {
            Configuration = configuration;

            apiClient = new RestClient(Configuration["bb:base"])
                .AddDefaultHeader("Authorization", $"Bearer {Configuration["bb:token"]}")
                .AddDefaultHeader("Accept", "application/json");

            apiClient.Timeout = -1;

            apiClient.Proxy = new WebProxy("127.0.0.1", 8888);
        }

        public void CreateUpdateLead(UserProfile profile)
        {
            if (string.IsNullOrEmpty(profile.Details.DealerID))
                throw new ArgumentNullException("UserProfile.Details.DealerID cannot be empty");

            var lead = BB_Lead.CreateFromProfile(profile);

            var createUpdateQuery = new RestRequest("stored_leads", Method.MERGE, DataFormat.Json);

            var bodyJSON = JsonConvert.SerializeObject(lead);

            createUpdateQuery.AddParameter("application/json", bodyJSON, ParameterType.RequestBody);

            var response = apiClient.Execute<JObject>(createUpdateQuery);
            Console.WriteLine($"Got response [{response.StatusCode}] {response.Content}");

            if(response.StatusCode == HttpStatusCode.OK)
            {
                //hooray!
                var newID = response.Data.Value<string>("id");
                profile.BB_CRM_ID = newID;
            }
            else
            {
                throw new Exception($"Invalid server response [{response.StatusCode}]. {response.Content}");
            }

        }

        /// <summary>
        /// For testing only...
        /// </summary>
        public List<BB_Lead> ListLeads()
        {
            var leadsQuery = new RestRequest("stored_leads", Method.GET, DataFormat.Json);
            var response = apiClient.Get<JObject>(leadsQuery);

            if (response.StatusCode == System.Net.HttpStatusCode.OK) {
                return response.Data.Value<List<BB_Lead>>("data");
            }
            else
            {
                throw new Exception($"Invalid server response [{response.StatusCode}]. {response.Content}");
            }
        }

    }

    public class BB_Lead
    {
        [JsonProperty("dealer_id")]
        public string Dealer_ID { get; set; }
        [JsonProperty("lead_platform")]
        public string Lead_Platform { get; set; }
        [JsonProperty("remote_id")]
        public string Remote_ID { get; set; }
        [JsonProperty("customer_name")]
        public string Name { get; set; }

        
        [JsonProperty("customer_email", NullValueHandling = NullValueHandling.Ignore)]
        public string Email { get; set; }
        [JsonProperty("customer_phone_number", NullValueHandling = NullValueHandling.Ignore)]
        public string Phone { get; set; }


        [JsonProperty("data")]
        public BB_AdditionalDetails Data { get; set; }

        public static BB_Lead CreateFromProfile(UserProfile profile)
        {
            var bbLead = new BB_Lead()
            {
                Dealer_ID = profile.Details.DealerID,
                Lead_Platform = "API",
                Remote_ID = profile.Details.UniqueID,
                Name = profile.Details.Name,
                Email = profile.Details.Email,
                Phone = profile.Details.Phone,
                Data = new BB_AdditionalDetails()
            };

            if (profile.Financing?.IsCompleted ?? false)
            {
                bbLead.Data.Field_Data = new List<BB_Field>();

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

                bbLead.Data.Field_Data.Add(new BB_Field()
                {
                    Name = "Financing",
                    Values = new List<string>(lines)
                });
            }

            if (profile.Inventory?.IsCompleted ?? false)
            {
                var inventory = profile.Inventory;

                var used = inventory.NewUsed == "Used";

                bbLead.Data.Purchase_Vehicle = new BB_PurchaseDetails()
                {
                    Used = used,
                    Make = inventory.Make,
                    Model = inventory.Model,
                    //Year = inventory.Year,
                    Condition = used ? "Used" : "New",
                };
            }

            if (profile.TradeDetails?.IsCompleted ?? false)
            {
                var tradein = profile.TradeDetails;

                bbLead.Data.TradeIn_Vehicle = new BB_VehicleDetails()
                {
                    Make = tradein.Make,
                    Model = tradein.Model,
                    Year = tradein.Year,
                    Condition = tradein.Condition
                };

                if (!string.IsNullOrEmpty(tradein.Mileage))
                {
                    bbLead.Data.TradeIn_Vehicle.Odometer = new BB_Odometer()
                    {
                        Value = tradein.Mileage
                    };
                }
            }

            return bbLead;
        }
    }

    public class BB_AdditionalDetails
    {
        [JsonProperty("email", NullValueHandling = NullValueHandling.Ignore)]
        public BB_EmailDetails Email { get; set; }

        [JsonProperty("purchase_vehicle", NullValueHandling = NullValueHandling.Ignore)]
        public BB_PurchaseDetails Purchase_Vehicle { get; set; }

        [JsonProperty("trade_in_vehicle", NullValueHandling = NullValueHandling.Ignore)]
        public BB_VehicleDetails TradeIn_Vehicle { get; set; }

        [JsonProperty("field_data", NullValueHandling = NullValueHandling.Ignore)]
        public List<BB_Field> Field_Data { get; set; }
    }

    public class BB_EmailDetails
    {
        [JsonProperty("subject")]
        public string Subject { get; set; }
        [JsonProperty("source")]
        public string Source { get; set; }
        [JsonProperty("interest_type")]
        public string InterestType { get; set; }
    }

    public class BB_VehicleDetails
    {
        [JsonProperty("source")]
        public string Year { get; set; }
        [JsonProperty("make_name")]
        public string Make { get; set; }
        [JsonProperty("model_name")]
        public string Model { get; set; }
        [JsonProperty("odometer", NullValueHandling = NullValueHandling.Ignore)]
        public BB_Odometer Odometer { get; set; }
        [JsonProperty("condition")]
        public string Condition { get; set; }
    }

    public class BB_Odometer
    {
        [JsonProperty("status", NullValueHandling = NullValueHandling.Ignore)]
        public string Status { get; set; }
        [JsonProperty("value", NullValueHandling = NullValueHandling.Ignore)]
        public string Value { get; set; }
        [JsonProperty("units", NullValueHandling = NullValueHandling.Ignore)]
        public string Units { get; set; }
    }

    public class BB_PurchaseDetails : BB_VehicleDetails
    {
        [JsonProperty("used")]
        public bool Used { get; set; }
    }

    public class BB_Field
    {
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("values")]
        public List<string> Values { get; set; }
    }
}
