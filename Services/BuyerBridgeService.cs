using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ADS.Bot.V1.Services
{
    public class BuyerBridgeService
    {
        RestClient restClient;

        public BuyerBridgeService()
        {
            restClient = new RestClient("https://app.buyerbridge.io:8001/api/v1/agency-start-onboarding");
            restClient.Timeout = -1;
            var request = new RestRequest(Method.POST);
            request.AddHeader("Authorization", "Bearer {token}");
            request.AddHeader("Accept", "application/json");
            request.AlwaysMultipartFormData = true;
            request.AddParameter("agency_id", "10000");
            request.AddParameter("name", "SuperCars");
            request.AddParameter("address", "1234 Something Rd.");
            request.AddParameter("city", "Somewhere");
            request.AddParameter("state", "FL");
            request.AddParameter("zip", "55555");
            request.AddParameter("phone_number", "555-555-5555");
            request.AddParameter("area_code", "555");
            request.AddParameter("country", "US");
            request.AddParameter("dealer_site_url", "https://www.supercars.com");
            request.AddParameter("remote_dealer_identifier", "123456");
            request.AddParameter("facebook_page_url", "https://www.facebook.com/SuperCars");
            request.AddParameter("facebook_marketplace_opt_out", "0");
            request.AddParameter("product_id", "10");
            request.AddParameter("lead_destination_email", "leads@supercars.com");
            IRestResponse response = restClient.Execute(request);
            Console.WriteLine(response.Content);
        }
    }
}
