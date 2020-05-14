using System;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Linq;
using Microsoft.WindowsAzure.Storage.Table;
using System.Collections.Generic;
using Newtonsoft.Json;
using static ADS_Sync.BuyerBridgeAPI;

namespace ADS_Sync
{
    public static class NightlyUpdate
    {
        [FunctionName("NightlyUpdate")]
        public static void Run(
            ExecutionContext context,
            [TimerTrigger("0 0 * * *")]TimerInfo nightlyTimer,
            [Table("Dealerships")] CloudTable Dealerships,
            [Table("Cars")] CloudTable Cars,
            ILogger log)
        {
            var config = GetConfiguration(context);

            //0 0 * * * = Daily at midnight
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

            var existingCars = Cars.ExecuteQuerySegmentedAsync(new TableQuery<CarDetails>(), null).Result;

            log.LogInformation($"Currently {existingCars.Count()} cars in the storage table.");


            var bbAPI = new BuyerBridgeAPI(log, config);

            log.LogInformation("Listing BuyersBridge Dealers");
            var liveDealers = bbAPI.ListDealerships();
            log.LogInformation($"Got {liveDealers.Count()} Dealers from API");

            var dealerRecords = liveDealers.Select(d => DealerDetails.FromBuyerBridge(d));
            var dealerSync = SyncTable<DealerDetails>(dealerRecords, Dealerships).ToList();

            foreach(var dealer in liveDealers)
            {
                log.LogInformation($"Pulling cars for dealer [{dealer.ID}]{dealer.Name}");
                var apiCars = bbAPI.ListInventory(dealer.ID).ToList();
                log.LogInformation($"Got {apiCars.Count()} cars for dealer");

                TableQuery<CarDetails> dealerCurrent = new TableQuery<CarDetails>()
                    .Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, dealer.ID))
                    .Select(new string[] { "PartitionKey" });

                var dbRecords = ExecuteSegmented(dealerCurrent, Cars).ToList();
                var staleRecords = dbRecords.Where(db_car => apiCars.Any(sync_car => sync_car.VIN == db_car.PartitionKey)).ToList();

                //Delete all records for this delear that aren't in current sync set
                log.LogInformation($"Removing {staleRecords.Count()} stale records");
                if (staleRecords.Count() > 0)
                {
                    var updateResults = MultiBatchOperations(staleRecords.Select(dr => TableOperation.Delete(dr)), Cars).ToList();
                }

                //Now do the sync for cars
                log.LogInformation($"Merging {apiCars.Count()} cars for dealer");
                if(apiCars.Count() > 0)
                {
                    var carRecords = apiCars.Select(car => CarDetails.FromBuyerBridge(dealer, car));
                    var carResults = SyncTable(carRecords, Cars).ToList();
                }

            }
        }

        private static IEnumerable<T> ExecuteSegmented<T>(TableQuery<T> query, CloudTable targetTable) where T : TableEntity, new()
        {
            TableContinuationToken continuationToken = null;
            do
            {
                var tableQueryResult = targetTable.ExecuteQuerySegmentedAsync(query, continuationToken);

                continuationToken = tableQueryResult.Result.ContinuationToken;

                foreach(var queryResult in tableQueryResult.Result)
                {
                    yield return queryResult;
                }
            }
            while (continuationToken != null);
        }

        private static IEnumerable<TableResult> SyncTable<T>(IEnumerable<T> records, CloudTable targetTable) where T : TableEntity
        {
            return MultiBatchOperations(records.Select(r => TableOperation.InsertOrMerge(r)), targetTable);
        }

        private static IEnumerable<TableResult> MultiBatchOperations(IEnumerable<TableOperation> operations, CloudTable targetTable, int chunkSize = 100)
        {
            //Chunk all operations into chunks of [n] for efficient batch submission
            List<List<TableOperation>> operationsChunked = operations
                .Select((x, index) => new { Index = index, Value = x })
                .Where(x => x.Value != null)
                .GroupBy(x => x.Index / chunkSize)
                .Select(x => x.Select(v => v.Value).ToList())
                .ToList();

            foreach(var chunk in operationsChunked)
            {
                var opBatch = new TableBatchOperation();
                chunk.ForEach(op => opBatch.Add(op));

                var results = targetTable.ExecuteBatchAsync(opBatch).Result;
                foreach (var result in results) yield return result;
            }
        }

        private static IConfiguration GetConfiguration(ExecutionContext context)
        {
            return new ConfigurationBuilder()
                .SetBasePath(context.FunctionAppDirectory)
                .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true) // <- This gives you access to your application settings in your local development environment
                .AddEnvironmentVariables() // <- This is what actually gets you the application settings in Azure
                .Build();
        }
    }

    public class DealerDetails : TableEntity
    {
        //ParitionKey is Empty
        //RowKey is ID
        public string Agency_ID { get; set; }
        public string Name { get; set; }
        public string Site_Provider_ID { get; set; }
        public string Site_Provider { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Zip { get; set; }
        public string Phone { get; set; }
        public string Country_Code { get; set; }

        public static DealerDetails FromBuyerBridge(BB_Dealership dealer)
        {
            return new DealerDetails()
            {
                PartitionKey = "",
                RowKey = dealer.ID,
                Agency_ID = dealer.Agency_ID,
                Name = dealer.Name,
                Site_Provider_ID = dealer.Site_Provider_ID,
                Site_Provider = dealer.Site_Provider,
                Address = dealer.Address,
                City = dealer.City,
                State = dealer.State,
                Zip = dealer.Zip,
                Phone = dealer.Phone,
                Country_Code = dealer.Country_Code
            };
        }
    }

    public class CarDetails : TableEntity
    {
        //ParitionKey is DealerID
        //RowKey is VIN
        public string Make { get; set; }
        public string Model { get; set; }
        public string Year { get; set; }
        public string Display_Name { get; set; }
        public string Stock_Number { get; set; }
        public int Price { get; set; }
        public int Mileage { get; set; }
        public string Engine { get; set; }
        public string Transmission { get; set; }
        public string Doors { get; set; }
        public bool Used { get; set; }

        public string URL { get; set; }
        public string Image_URL { get; set; }

        public static CarDetails FromBuyerBridge(BB_Dealership dealer, BB_Car car)
        {
            return new CarDetails()
            {
                PartitionKey = dealer.ID,
                RowKey = car.VIN,
                Make = car.Make_Name_Raw,
                Model = car.Model_Name_Raw,
                Year = car.Year,
                Display_Name = car.Display_Name,
                Stock_Number = car.Stock_Number,
                Price = car.Price ?? 0,
                Mileage = car.Mileage ?? 0,
                Engine = car.Engine,
                Transmission = car.Transmission,
                Doors = car.Doors,
                Used = car.Used,
                URL = car?.Dealer_Vehicle?.Data?.FirstOrDefault()?.Vdp_Url ?? "",
                Image_URL = car?.Images?.Data?.FirstOrDefault()?.Original_Url ?? ""
            };
        }
    }
}
