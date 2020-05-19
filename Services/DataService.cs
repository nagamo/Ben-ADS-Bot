using ADS.Bot.V1.Models;
using ADS.Bot1;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ADS.Bot.V1.Services
{
    public class DataService
    {
        public IConfiguration Configuration { get; }
        public CloudTableClient StorageClient { get; }

        public DataService(IConfiguration configuration, CloudTableClient dataStorage)
        {
            Configuration = configuration;
            StorageClient = dataStorage;
        }


        private CloudTable CarStorage
        {
            get { return StorageClient.GetTableReference("Cars"); }
        }
        private CloudTable DealerStorage
        {
            get { return StorageClient.GetTableReference("Dealerships"); }
        }



        public TableQuery<DB_Dealer> CreateDealerQuery()
        {
            return DealerStorage.CreateQuery<DB_Dealer>();
        }

        public TableQuery<DB_Car> CreateCarQuery()
        {
            return CarStorage.CreateQuery<DB_Car>();
        }


        public DB_Dealer GetDealerByFacebookPageID(string FacebookPageID)
        {
            var findQuery = CreateDealerQuery().Where(d => d.FB_PageIDs.Contains(FacebookPageID));
            return DealerStorage.ExecuteQuery(findQuery as TableQuery<DB_Dealer>).FirstOrDefault();
        }


        public DB_Car GetCar(string VIN)
        {
            var findQuery = CreateCarQuery().Where(c => c.RowKey == VIN);
            return GetCars(findQuery as TableQuery<DB_Car>).FirstOrDefault();
        }

        public List<DB_Car> GetCars(TableQuery<DB_Car> query = null)
        {
            var findQuery = query ?? CreateCarQuery();
            var result = CarStorage.ExecuteQuery(findQuery as TableQuery<DB_Car>);
            return result.ToList();
        }

        public int CountCars(TableQuery<DB_Car> existingQuery = null)
        {
            var query = existingQuery ?? CreateCarQuery();
            query.SelectColumns = new string[] { "RowKey" };
            var results = CarStorage.ExecuteQuery(query).ToList();
            return results.Count();
        }

        private List<(string Group, int Count)> ListCarsGrouped(string GroupProperty, Func<DB_Car, string> GroupFunc, TableQuery<DB_Car> existingQuery)
        {
            var groupQuery = existingQuery ?? CreateCarQuery();
            groupQuery.SelectColumns = new string[] { GroupProperty };
            var groupedCars = CarStorage.ExecuteQuery(groupQuery as TableQuery<DB_Car>).ToList();

            return groupedCars
                .GroupBy(GroupFunc)
                .Where(g => g.Key != null)
                .Select(group => (Group: group.Key, Count: group.Count()))
                .ToList();
        }

        public IEnumerable<(string Title, int Count)> ListAvailableNewUsed(TableQuery<DB_Car> existingQuery)
        {
            var newUsed = ListCarsGrouped("Used", c => c.Used ? "Used" : "New", existingQuery)
                .ToDictionary(g => g.Group); //Alphabetical make New first...

            if (newUsed.Count < 2) yield break; //If we don't have new AND used, no point in asking

            yield return ("Doesn't Matter", newUsed["New"].Count + newUsed["Used"].Count);
            yield return ("New", newUsed["New"].Count);
            yield return ("Used", newUsed["Used"].Count);
        }
        public IEnumerable<(string BodyType, int Count)> ListAvailableBodyTypes(TableQuery<DB_Car> existingQuery)
        {
            return ListCarsGrouped("Body", c => c.Body, existingQuery)
                .OrderByDescending(group => group.Count);
        }
        public IEnumerable<(string Make, int Count)> ListAvailableMakes(TableQuery<DB_Car> existingQuery)
        {
            return ListCarsGrouped("Make", c => c.Make, existingQuery)
                .OrderByDescending(group => group.Count);
        }
        public IEnumerable<(string Model, int Count)> ListAvailableModels(TableQuery<DB_Car> existingQuery)
        {
            return ListCarsGrouped("Model", c => c.Model, existingQuery)
                .OrderByDescending(group => group.Count);
        }
        public IEnumerable<(string Color, int Count)> ListAvailableColors(TableQuery<DB_Car> existingQuery)
        {
            return ListCarsGrouped("Color", c => c.Color, existingQuery)
                .OrderByDescending(group => group.Count);
        }
        private IEnumerable<(string Price, int Count)> GetPriceGroups(TableQuery<DB_Car> existingQuery)
        {
            return ListCarsGrouped("Price", c =>
            {
                if (c.Price <= 10000) return "< $10k";
                else if (c.Price <= 20000) return "$10k-20k";
                else if (c.Price <= 30000) return "$20k-30k";
                else if (c.Price <= 40000) return "$30k-40k";
                else if (c.Price <= 50000) return "$40k-50k";
                else return "$50k+";
            }, existingQuery);
        }
        public IEnumerable<(string Price, int Count)> ListAvailablePriceRanges(TableQuery<DB_Car> existingQuery)
        {
            var priceGroups = GetPriceGroups(existingQuery).ToDictionary(g => g.Price);

            if (priceGroups.ContainsKey("< $10k")) yield return priceGroups["< $10k"];
            if (priceGroups.ContainsKey("$10k-20k")) yield return priceGroups["$10k-20k"];
            if (priceGroups.ContainsKey("$20k-30k")) yield return priceGroups["$20k-30k"];
            if (priceGroups.ContainsKey("$30k-40k")) yield return priceGroups["$30k-40k"];
            if (priceGroups.ContainsKey("$40k-50k")) yield return priceGroups["$40k-50k"];
            if (priceGroups.ContainsKey("$50k+")) yield return priceGroups["$50k+"];
        }
        public IEnumerable<(string Price, int Count)> ListAvailablePriceMaxes(TableQuery<DB_Car> existingQuery)
        {
            var priceGroups = GetPriceGroups(existingQuery).ToDictionary(g => g.Price);

            int runningCount = 0;

            if (priceGroups.ContainsKey("< $10k")) { runningCount += priceGroups["< $10k"].Count; yield return ("< $10k", runningCount); }
            if (priceGroups.ContainsKey("$10k-20k")) { runningCount += priceGroups["$10k-20k"].Count; yield return ("< $20k", runningCount); }
            if (priceGroups.ContainsKey("$20k-30k")) { runningCount += priceGroups["$20k-30k"].Count; yield return ("< $30k", runningCount); }
            if (priceGroups.ContainsKey("$30k-40k")) { runningCount += priceGroups["$30k-40k"].Count; yield return ("< $40k", runningCount); }
            if (priceGroups.ContainsKey("$40k-50k")) { runningCount += priceGroups["$40k-50k"].Count; yield return ("< $50k", runningCount); }
            if (priceGroups.ContainsKey("$50k+")) yield return priceGroups["$50k+"];
        }

        public IEnumerable<(string Payment, int Count)> ListAvailablePayments(TableQuery<DB_Car> existingQuery)
        {
            var paymentGroups = ListCarsGrouped("Price", c =>
            {
                if (c.Price <= Utilities.CalculatePayment(250)) return "$250";
                else if (c.Price <= Utilities.CalculatePayment(400)) return "$400";
                else if (c.Price <= Utilities.CalculatePayment(600)) return "$600";
                return null; //These get ignored, the parent adds a show all option
            }, existingQuery).ToDictionary(g => g.Group);

            int runningCount = 0;

            if (paymentGroups.ContainsKey("$250")) { runningCount += paymentGroups["$250"].Count; yield return ("$250", runningCount); }
            if (paymentGroups.ContainsKey("$400")) { runningCount += paymentGroups["$400"].Count; yield return ("$400", runningCount); }
            if (paymentGroups.ContainsKey("$600")) { runningCount += paymentGroups["$600"].Count; yield return ("$600", runningCount); }
        }
    }
}
