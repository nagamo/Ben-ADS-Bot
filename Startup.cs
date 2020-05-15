// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

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
using Microsoft.Bot.Builder.Azure;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using ADS.Bot.V1.Models;
using ADS.Bot.V1.Services;
using Microsoft.Azure.Cosmos.Table;
using System;
using System.Linq;
using Microsoft.Extensions.Logging;

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

            // Create the storage we'll be using for User and Conversation state. (Memory is great for testing purposes.)
            var dbConnectionString = Configuration.GetConnectionString("UserData");
            if(dbConnectionString != null)
            {
                //services.AddSingleton<IStorage>(new AzureBlobStorage(dbConnectionString, "bottest"));
            }
            else
            {
                services.AddSingleton<IStorage, MemoryStorage>();
            }

            var vinDBConnectionString = Configuration.GetConnectionString("VinSolutionsDB");
            if (vinDBConnectionString != null)
            {
                try
                {
                    CloudStorageAccount account = CloudStorageAccount.Parse(vinDBConnectionString);
                    services.AddSingleton(account.CreateCloudTableClient());
                }
                catch (Exception ex)
                {
                    services.AddSingleton((CloudTableClient)null);
                }
            }



            // Create the User and Conversation State
            services.AddSingleton<UserState>();
            services.AddSingleton<ConversationState>();

            // Create the bot services (LUIS, QnA) as a singleton.
            services.AddSingleton<ZohoBotService>();
            services.AddSingleton<ADSBotServices>();

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
            services.AddSingleton<VehicleInventoryDialog>();

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
