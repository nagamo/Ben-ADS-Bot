using ADS.Bot.V1.Bots;
using ADS.Bot.V1.Cards;
using ADS.Bot.V1.Dialogs;
using ADS.Bot1.Bots;
using ADS.Bot1.Dialogs;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Extensions.DependencyInjection;
using System.Configuration;
using Microsoft.Bot.Connector;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using ADS.Bot.V1.Models;
using ADS.Bot.V1.Services;
using Microsoft.Azure.Cosmos.Table;
using System;
using System.Linq;
using Microsoft.Extensions.Logging;
using ADS.Bot.V1;
using System.Data.Common;
using Microsoft.Bot.Builder.Azure;
using AzureBlobStorage = ADS.Bot.V1.Services.AzureBlobStorage;

namespace ADS.Bot1
{
    public class Startup
    {
        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers().AddNewtonsoftJson();

            // Create the Bot Framework Adapter with error handling enabled.
            services.AddSingleton<IBotFrameworkHttpAdapter, AdapterWithErrorHandler>();

            //Fallback to dev unless otherwise specified
            string environment = Configuration["ads:environment"] ?? "dev";

            // Create the storage we'll be using for User and Conversation state. (Memory is great for testing purposes.)
            var dbConnectionString = Configuration.GetConnectionString("UserData");
            if (dbConnectionString != null)
            {
                services.AddSingleton<IStorage>(new AzureBlobStorage(dbConnectionString, environment));
            }
            else
            {
                services.AddSingleton<IStorage, MemoryStorage>();
            }

            var vinDBConnectionString = Configuration.GetConnectionString("VinSolutionsDB");
            CloudStorageAccount account = CloudStorageAccount.Parse(vinDBConnectionString);
            services.AddSingleton(account);
            services.AddSingleton(account.CreateCloudTableClient());

            // Create the User and Conversation State
            services.AddSingleton<UserState>();
            services.AddSingleton<ConversationState>();

            //Alternatively Conversation state can be assigned to memory only to save space/load
            /*
            var memoryProvider = new MemoryStorage();
            services.AddSingleton<ConversationState>(new ConversationState(memoryProvider));
            */


            services.AddSingleton<DataService>();

            services.AddSingleton<CRMCommitService>();
            services.AddHostedService<BackgroundServiceStarter<CRMCommitService>>();

            // Create the bot services (LUIS, QnA) as a singleton.
            services.AddSingleton<BuyerBridgeAPIService>();
            services.AddSingleton<DealerConfigService>();
            services.AddSingleton<ZohoAPIService>();
            services.AddSingleton<ADSBotServices>();
            services.AddSingleton<CRMService>();

            services.AddSingleton<ICardFactory<BasicDetails>, JSONProfileCardFactory>();
            services.AddSingleton<ICardFactory<FinancingDetails>, JSONFinanceCardFactory>();
            services.AddSingleton<ICardFactory<VehicleProfileDetails>, JSONVehicleInventoryCardFactory>();
            services.AddSingleton<ICardFactory<TradeInDetails>, JSONTradeInCardFactory>();
            //services.AddSingleton<SendAdaptiveDialog<ProfileCardFactory, BasicDetails>>();

            // Create the various dialogs
            services.AddSingleton<UserProfileDialog>();
            services.AddSingleton<VehicleProfileDialog>();
            services.AddSingleton<ValueTradeInDialog>();
            services.AddSingleton<FinanceDialog>();
            services.AddSingleton<SimpleInventoryDialog>();
            //services.AddSingleton<VehicleInventoryDialog>();

            services.AddSingleton<ActiveLeadDialog>();

            services.AddSingleton<RootDialog>();

            //Add the actual bot implementation to use.
            services.AddTransient<IBot, BenBot>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseDefaultFiles()
                .UseStaticFiles()
                .UseRouting()
                .UseAuthorization()
                .UseEndpoints(endpoints =>
                {
                    endpoints.MapControllers();
                });

            // app.UseHttpsRedirection();
        }
    }
}