using System;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Linq;
using Microsoft.WindowsAzure.Storage.Table;

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


            var vinAPI = new VinSolutionsAPI(log, config);


            log.LogInformation("Authenticating with VinSolutions API");
            vinAPI.Authenticate();


            log.LogInformation("Querying VinSolutions API");
            var dealers = vinAPI.ListDealerships();

            log.LogInformation($"Got {dealers.Count()} dealers from API!");
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

    public class CarDetails : TableEntity
    {
        public string Make { get; set; }
        public string Model { get; set; }
        public string Year { get; set; }
    }
}
