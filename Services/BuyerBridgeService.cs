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

        //TODO: Make all of this properly injected

        private static List<(string Group, int Count)> ListCarsGrouped(string GroupProperty, Func<DB_Car,string> GroupFunc, ADSBotServices botServices, TableQuery<DB_Car> existingQuery)
        {
            var groupQuery = existingQuery ?? botServices.CarStorage.CreateQuery<DB_Car>();
            groupQuery.SelectColumns = new string[] { GroupProperty };
            var groupedCars = botServices.CarStorage.ExecuteQuery(groupQuery as TableQuery<DB_Car>).ToList();

            return groupedCars
                .GroupBy(GroupFunc)
                .Select(group => (Group: group.Key, Count: group.Count()))
                .ToList();
        }

        public static IEnumerable<(string Title, int Count)> ListAvailableNewUsed(ADSBotServices botServices, TableQuery<DB_Car> existingQuery)
        {
            var newUsed = ListCarsGrouped("Used", c => c.Used ? "Used" : "New", botServices, existingQuery)
                .ToDictionary(g => g.Group); //Alphabetical make New first...

            if (newUsed.Count < 2) yield break; //If we don't have new AND used, no point in asking

            yield return ("Doesn't Matter", newUsed["New"].Count + newUsed["Used"].Count);
            yield return ("New", newUsed["New"].Count);
            yield return ("Used", newUsed["Used"].Count);
        }
        public static IEnumerable<(string Make, int Count)> ListAvailableMakes(ADSBotServices botServices, TableQuery<DB_Car> existingQuery)
        {
            return ListCarsGrouped("Make", c => c.Make, botServices, existingQuery)
                .OrderByDescending(group => group.Count);
        }
        public static IEnumerable<(string Model, int Count)> ListAvailableModels(ADSBotServices botServices, TableQuery<DB_Car> existingQuery)
        {
            return ListCarsGrouped("Model", c => c.Model, botServices, existingQuery)
                .OrderByDescending(group => group.Count);
        }
        public static IEnumerable<(string Color, int Count)> ListAvailableColors(ADSBotServices botServices, TableQuery<DB_Car> existingQuery)
        {
            return ListCarsGrouped("Color", c => c.Color, botServices, existingQuery)
                .OrderByDescending(group => group.Count);
        }
        private static IEnumerable<(string Price, int Count)> GetPriceGroups(ADSBotServices botServices, TableQuery<DB_Car> existingQuery)
        {
            return ListCarsGrouped("Price", c =>
            {
                if (c.Price <= 10000) return "< $10k";
                else if (c.Price <= 20000) return "$10k-20k";
                else if (c.Price <= 30000) return "$20k-30k";
                else if (c.Price <= 40000) return "$30k-40k";
                else if (c.Price <= 50000) return "$40k-50k";
                else return "$50k+";
            }, botServices, existingQuery);
        }
        public static IEnumerable<(string Price, int Count)> ListAvailablePriceRanges(ADSBotServices botServices, TableQuery<DB_Car> existingQuery)
        {
            var priceGroups = GetPriceGroups(botServices, existingQuery).ToDictionary(g => g.Price);

            if (priceGroups.ContainsKey("< $10k")) yield return priceGroups["< $10k"];
            if (priceGroups.ContainsKey("$10k-20k")) yield return priceGroups["$10k-20k"];
            if (priceGroups.ContainsKey("$20k-30k")) yield return priceGroups["$20k-30k"];
            if (priceGroups.ContainsKey("$30k-40k")) yield return priceGroups["$30k-40k"];
            if (priceGroups.ContainsKey("$40k-50k")) yield return priceGroups["$40k-50k"];
            if (priceGroups.ContainsKey("$50k+")) yield return priceGroups["$50k+"];
        }
        public static IEnumerable<(string Price, int Count)> ListAvailablePriceMaxes(ADSBotServices botServices, TableQuery<DB_Car> existingQuery)
        {
            var priceGroups = GetPriceGroups(botServices, existingQuery).ToDictionary(g => g.Price);

            int runningCount = 0;

            if (priceGroups.ContainsKey("< $10k")) { runningCount += priceGroups["< $10k"].Count; yield return ("< $10k", runningCount); }
            if (priceGroups.ContainsKey("$10k-20k")) { runningCount += priceGroups["$10k-20k"].Count; yield return ("< $20k", runningCount); }
            if (priceGroups.ContainsKey("$20k-30k")) { runningCount += priceGroups["$20k-30k"].Count; yield return ("< $30k", runningCount); }
            if (priceGroups.ContainsKey("$30k-40k")) { runningCount += priceGroups["$30k-40k"].Count; yield return ("< $40k", runningCount); }
            if (priceGroups.ContainsKey("$40k-50k")) { runningCount += priceGroups["$40k-50k"].Count; yield return ("< $50k", runningCount); }
            if (priceGroups.ContainsKey("$50k+")) yield return priceGroups["$50k+"];
        }
    }
}
