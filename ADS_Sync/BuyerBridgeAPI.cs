using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Text;

namespace ADS_Sync
{
    public class BuyerBridgeAPI
    {
        ILogger logger;
        IConfiguration config;

        IRestClient apiClient;

        public BuyerBridgeAPI(ILogger Logger, IConfiguration Configuration)
        {
            logger = Logger;
            config = Configuration;
            apiClient = new RestClient(Configuration["bb:base"])
                .AddDefaultHeader("Authorization", $"Bearer {Configuration["bb:token"]}")
                .AddDefaultHeader("Accept", "application/json");

            apiClient.Timeout = -1;
        }

        private BB_Response<T> QueryRecords<T>(string Path, int page = 1)
        {
            var dataQuery = new RestRequest(Path, Method.GET, DataFormat.Json);
            if(page != 1)
            {
                dataQuery.AddQueryParameter("page", page.ToString());
            }
            var response = apiClient.Execute(dataQuery);

            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                var apiResponse = JsonConvert.DeserializeObject<BB_Response<T>>(response.Content);
                logger.LogTrace($"Loaded api page [{response.ResponseUri}] {page}/{apiResponse.Meta?.Last_Page}");

                return apiResponse;
            }
            else
            {
                throw new Exception($"Server returned error [{response.StatusCode}] {response.ErrorMessage} \r\n {response.ErrorException}\r\n{config["bb:token"]}");
            }
        }

        private IEnumerable<T> QueryAllRecords<T>(string Path, int PageLimit = -1)
        {
            int page = 1;
            int totalPages;
            do
            {
                var pageResults = QueryRecords<List<T>>(Path, page);
                totalPages = pageResults.Meta.Last_Page;

                foreach(var pageResult in pageResults.Data)
                {
                    yield return pageResult;
                }

                page++;

                if(PageLimit != -1 && page >= PageLimit)
                {
                    break;
                }
            } while (page <= totalPages);
        }

        public IEnumerable<BB_Dealership> ListDealerships()
        {
            return QueryAllRecords<BB_Dealership>("dealers");
        }
        public IEnumerable<BB_Car> ListInventory(string DealerID, int PageLimit = -1)
        {
            return QueryAllRecords<BB_Car>($"dealers/{DealerID}/inventory?with_relationships=vehicles.dealer_vehicle,vehicles.images", PageLimit);
        }

        public class BB_Response<T>
        {
            public T Data { get; set; }
            public BB_Meta Meta { get; set; }
        }

        public class BB_Meta
        {
            public int? From { get; set; }
            public int? To { get; set; }
            public int Current_Page { get; set; }
            public int Last_Page { get; set; }
        }

        public class BB_Dealership
        {
            [JsonProperty("id")]
            public string ID { get; set; }
            [JsonProperty("agency_id")]
            public string Agency_ID { get; set; }
            public string Name { get; set; }
            [JsonProperty("site_provider_id")]
            public string Site_Provider_ID { get; set; }
            public string Site_Provider { get; set; }
            public string Address { get; set; }
            public string City { get; set; }
            public string State { get; set; }
            public string Zip { get; set; }
            public string Phone { get; set; }
            public string Country_Code { get; set; }
        }

        public class BB_Car
        {
            public int ID { get; set; }
            public string Display_Name { get; set; }
            public string Stock_Number { get; set; }
            public string VIN { get; set; }
            public int? Price { get; set; }
            public int? Mileage { get; set; }
            public string Engine { get; set; }
            public string Transmission { get; set; }
            public string Doors { get; set; }
            public string Make_Name_Raw { get; set; }
            public string Model_Name_Raw { get; set; }
            public string Year { get; set; }
            public string Exterior_Color { get; set; }
            public bool Used { get; set; }

            public BB_DealerRecords Dealer_Vehicle { get; set; }
            public BB_Images Images { get; set; }
        }

        public class BB_DealerRecords
        {
            public BB_DealerData[] Data { get; set; }
        }

        public class BB_DealerData
        {
            public string Marketplace_Vehicle_Url { get; set; }
            public string Vdp_Url { get; set; }
            public string Vehicle_Id { get; set; }
        }

        public class BB_Images
        {
            public BB_ImageData[] Data { get; set; }
        }

        public class BB_ImageData
        {
            public int Order { get; set; }
            public string Original_Url { get; set; }
        }
    }
}
