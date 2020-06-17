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
        public DealerConfigService DealerConfig { get; }
        internal CRMService RootCRM { get; set; }

        private IRestClient apiClient;

        public BuyerBridgeAPIService(IConfiguration configuration, DealerConfigService dealerConfig)
        {
            Configuration = configuration;
            DealerConfig = dealerConfig;

            apiClient = new RestClient(Configuration["bb:base"])
                .AddDefaultHeader("Authorization", $"Bearer {Configuration["bb:token"]}")
                .AddDefaultHeader("Accept", "application/json");

            apiClient.Timeout = -1;
        }

        public void CreateUpdateLead(UserProfile profile, string uniqueID)
        {
            BB_Lead bb_lead = BB_Lead.CreateFromProfile(profile, uniqueID, RootCRM.Services).Result;

            var createUpdateQuery = new RestRequest("stored_leads", Method.POST, DataFormat.Json);

            var bodyJSON = JsonConvert.SerializeObject(bb_lead);

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
        public string dealer_ID { get; set; }
        [JsonProperty("lead_platform_id")]
        public string lead_platform_id { get; set; }
        [JsonProperty("remote_id")]
        public string remote_id { get; set; }
        [JsonProperty("customer_name")]
        public string customer_name { get; set; }

        
        [JsonProperty("customer_email", NullValueHandling = NullValueHandling.Ignore)]
        public string customer_email { get; set; }
        [JsonProperty("customer_phone_number", NullValueHandling = NullValueHandling.Ignore)]
        public string customer_phone_number { get; set; }


        [JsonProperty("data")]
        public BB_AdditionalDetails Data { get; set; }

        public static async Task<BB_Lead> CreateFromProfile(UserProfile profile, string CustomUniqueID, ADSBotServices services)
        {
            var bbLead = new BB_Lead()
            {
                dealer_ID = profile.Details.DealerID,
                lead_platform_id = "API",
                remote_id = CustomUniqueID,
                customer_name = profile.Details.Name,
                customer_email = profile.Details.Email,
                customer_phone_number = profile.Details.Phone,
                Data = new BB_AdditionalDetails()
                {
                }
            };

            List<BB_Field> fields = new List<BB_Field>();

            if(profile.Financing != null)
            {
                if (await profile.Financing.IsValidForSubmit(profile, services))
                {
                    var lines = new string[]
                        {
                        $"Credit Score: {profile.Financing.CreditScore}",
                        $"Income: {profile.Financing.Income}",
                        $"Home Ownership: {profile.Financing.HomeOwnership}",
                        $"Employment History: {profile.Financing.Employment}",
                        };

                    fields.Add(new BB_Field()
                    {
                        name = "Financing",
                        values = lines
                    });
                }
            }

            if (profile.SimpleInventory?.IsCompleted ?? false)
            {
                var inventory = profile.SimpleInventory;

                bbLead.Data.purchase_vehicle = new BB_PurchaseDetails()
                {
                    make_name = inventory.Make,
                    model_name = inventory.Model,
                    source = inventory.Year,
                    used = inventory.Used,
                    condition = inventory.Used ? "Used" : "New",
                };

                if (!string.IsNullOrEmpty(inventory.VIN))
                {
                    fields.Add(new BB_Field()
                        {
                            name = "VIN",
                            values = new string[]
                            {
                                inventory.VIN
                            }
                        });
                }
            }
            else if (profile.Inventory?.IsCompleted ?? false)
            {
                var inventory = profile.Inventory;

                var used = inventory.NewUsed == "Used";

                bbLead.Data.purchase_vehicle = new BB_PurchaseDetails()
                {
                    used = used,
                    make_name = inventory.Make,
                    model_name = inventory.Model,
                    condition = used ? "Used" : "New",
                };
            }


            if (profile.TradeDetails?.IsCompleted ?? false)
            {
                var tradein = profile.TradeDetails;

                bbLead.Data.trade_in_vehicle = new BB_VehicleDetails()
                {
                    //TODO: Need to expand vehicle details into make/model/year
                    make_name = tradein.Vehicle,
                    condition = tradein.Condition
                };

                if (!string.IsNullOrEmpty(tradein.Mileage))
                {
                    bbLead.Data.trade_in_vehicle.odometer = new BB_Odometer()
                    {
                        value = tradein.Mileage
                    };
                }
            }

            if (fields.Count > 0)
            {
                bbLead.Data.field_data = fields.ToArray();
            }

            return bbLead;
        }
    }

    public class BB_AdditionalDetails
    {
        [JsonProperty("email", NullValueHandling = NullValueHandling.Ignore)]
        public BB_EmailDetails email { get; set; }

        [JsonProperty("purchase_vehicle", NullValueHandling = NullValueHandling.Ignore)]
        public BB_PurchaseDetails purchase_vehicle { get; set; }

        [JsonProperty("trade_in_vehicle", NullValueHandling = NullValueHandling.Ignore)]
        public BB_VehicleDetails trade_in_vehicle { get; set; }

        [JsonProperty("field_data", NullValueHandling = NullValueHandling.Ignore)]
        public BB_Field[] field_data { get; set; }
    }

    public class BB_EmailDetails
    {
        [JsonProperty("subject")]
        public string subject { get; set; }
        [JsonProperty("source")]
        public string source { get; set; }
        [JsonProperty("interest_type")]
        public string interest_type { get; set; }
    }

    public class BB_VehicleDetails
    {
        [JsonProperty("source")]
        public string source { get; set; }
        [JsonProperty("make_name")]
        public string make_name { get; set; }
        [JsonProperty("model_name")]
        public string model_name { get; set; }
        [JsonProperty("odometer", NullValueHandling = NullValueHandling.Ignore)]
        public BB_Odometer odometer { get; set; }
        [JsonProperty("condition")]
        public string condition { get; set; }
    }

    public class BB_Odometer
    {
        [JsonProperty("status", NullValueHandling = NullValueHandling.Ignore)]
        public string status { get; set; }
        [JsonProperty("value", NullValueHandling = NullValueHandling.Ignore)]
        public string value { get; set; }
        [JsonProperty("units", NullValueHandling = NullValueHandling.Ignore)]
        public string units { get; set; }
    }

    public class BB_PurchaseDetails : BB_VehicleDetails
    {
        [JsonProperty("used")]
        public bool used { get; set; }
    }

    public class BB_Field
    {
        [JsonProperty("name")]
        public string name { get; set; }
        [JsonProperty("values")]
        public string[] values { get; set; }
    }
}
