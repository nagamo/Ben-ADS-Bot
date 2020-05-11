using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Text;

namespace ADS_Sync
{
    public class VinSolutionsAPI
    {
        ILogger logger;
        IConfiguration config;

        RestClient apiClient;

        public VinSolutionsAPI(ILogger Logger, IConfiguration Configuration)
        {
            logger = Logger;
            config = Configuration;
            apiClient = new RestClient(Configuration["vin_solutions:base"]);
            apiClient.AddDefaultHeader("api_key", Configuration["vin_solutions:api_key"]);
        }

        public bool Authenticate()
        {
            var authClient = new RestClient(config["vin_solutions:auth_base"]);
            var authRequest = new RestRequest("connect/token", Method.POST, DataFormat.Json);
            authRequest.AddParameter("grant_type", "client_credentials");
            authRequest.AddParameter("client_id", config["vin_solutions:client_id"]);
            authRequest.AddParameter("client_secret", config["vin_solutions:client_secret"]);
            authRequest.AddParameter("scope", "PublicAPI");

            try
            {
                var tokenResponse = authClient.Execute<AuthResponse>(authRequest);

                apiClient.AddDefaultHeader("Authorization", $"Bearer {tokenResponse.Data.AccessToken}");

                return true;
            }
            catch(Exception ex)
            {
                logger.LogError(ex, "Error authenticating to VinSolution API");
                return false;
            }
        }

        private IEnumerable<T> QueryRecords<T>(string Path)
        {
            var dataQuery = new RestRequest(Path, Method.GET, DataFormat.Json);
            var response = apiClient.Execute<List<T>>(dataQuery);

            if(response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                return response.Data;
            }
            else
            {
                //?
                throw new NotSupportedException();
            }
        }

        public IEnumerable<Dealership> ListDealerships()
        {
            return QueryRecords<Dealership>("gateway/v1/organization/dealers");
        }

        private class AuthResponse
        {
            [JsonProperty("access_token")]
            public string AccessToken { get; set; }
            [JsonProperty("expires_in")]
            public string ExpiresIn { get; set; }
            [JsonProperty("token_type")]
            public string TokenType { get; set; }
        }

        public class Dealership
        {
            public int ID { get; set; }
            public string Name { get; set; }
            public string City { get; set; }
            public string State { get; set; }
        }
    }
}
