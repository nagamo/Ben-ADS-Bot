using ADS.Bot.V1.Models;
using ADS.Bot1;
using Microsoft.Azure.Cosmos.Table;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ADS.Bot.V1.Services
{
    public class BuyerBridgeService
    {
        public BuyerBridgeService()
        {
        }

        //TODO: Make this properly injected
        public static List<(string Make, int Count)> ListAvailableMakes(IBotServices botServices, TableQuery<DB_Car> existingQuery)
        {
            var makeQuery = existingQuery ?? botServices.CarStorage.CreateQuery<DB_Car>();
            makeQuery.SelectColumns = new string[] { "Make" };
            var allMakes = botServices.CarStorage.ExecuteQuery(makeQuery as TableQuery<DB_Car>).ToList();

            return allMakes
                .GroupBy(c => c.Make)
                .Select(make_group => (make_group.Key, make_group.Count()))
                .ToList();
        }
    }
}
