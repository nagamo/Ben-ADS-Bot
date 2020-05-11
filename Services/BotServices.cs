// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using ADS.Bot.V1.Models;
using ADS.Bot.V1.Services;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.Luis;
using Microsoft.Bot.Builder.AI.QnA;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using ZCRMSDK.CRM.Library.CRMException;
using ZCRMSDK.CRM.Library.Setup.RestClient;
using ZCRMSDK.OAuth.Client;

namespace ADS.Bot1
{
    public class Services : IBotServices
    {
        public Services(IConfiguration configuration, ConversationState conversationState, UserState userState, ZohoBotService zohoService, CloudTableClient dataStorage)
        {
            ConversationState = conversationState;
            Configuration = configuration;
            StorageClient = dataStorage;
            UserState = userState;
            Zoho = zohoService;

            UserProfileAccessor = UserState.CreateProperty<UserProfile>(nameof(UserProfile));
            DialogStateAccessor = ConversationState.CreateProperty<DialogState>(nameof(DialogState));
            //Used by micrsoft dialog classes
            GenericUserProfileAccessor = UserState.CreateProperty<Dictionary<string, object>>(nameof(UserProfile));

            LeadQualQnA = new QnAMaker(new QnAMakerEndpoint
            {
                KnowledgeBaseId = configuration["qna:QnAKnowledgebaseId"],
                EndpointKey = configuration["qna:QnAEndpointKey"],
                Host = configuration["qna:QnAEndpointHostName"]
            });

            var luisApplication = new LuisApplication(
                configuration["luis:id"],
                configuration["luis:endpointKey"],
                configuration["luis:endpoint"]);

            var recognizerOptions = new LuisRecognizerOptionsV3(luisApplication)
            {
                IncludeAPIResults = true,
                PredictionOptions = new Microsoft.Bot.Builder.AI.LuisV3.LuisPredictionOptions()
                {
                    IncludeAllIntents = true,
                    IncludeInstanceData = true
                }
            };

            LuisRecognizer = new LuisRecognizer(recognizerOptions);

        }

        public QnAMaker LeadQualQnA { get; private set; }

        public IStatePropertyAccessor<Dictionary<string, object>> GenericUserProfileAccessor { get; private set; }
        public IStatePropertyAccessor<UserProfile> UserProfileAccessor { get; private set; }
        public IStatePropertyAccessor<DialogState> DialogStateAccessor { get; private set; }

        public IConfiguration Configuration { get; }


        public CloudTableClient StorageClient { get; private set; }

        public CloudTable CarStorage
        {
            get { return StorageClient.GetTableReference("Cars"); }
        }
        public CloudTable DealerStorage
        {
            get { return StorageClient.GetTableReference("Dealerships"); }
        }



        public LuisRecognizer LuisRecognizer { get; private set; }
        public ZohoBotService Zoho { get; private set; }

        private ConversationState ConversationState { get; set; }
        private UserState UserState { get; set; }

        public async Task<UserProfile> GetUserProfileAsync(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            return await UserProfileAccessor.GetAsync(turnContext, () => new UserProfile(), cancellationToken);
        }

        public async Task SaveUserProfileAsync(UserProfile profile, ITurnContext turnContext, CancellationToken cancellationToken)
        {
            //Update accessors with latest version
            await UserState.SaveChangesAsync(turnContext, cancellationToken: cancellationToken);
        }

        public async Task SetUserProfileAsync(UserProfile profile, DialogContext dialogContext, CancellationToken cancellationToken)
        {
            //Also update the dialog contexts state
            await UserProfileAccessor.SetAsync(dialogContext.Context, profile, cancellationToken);
            dialogContext.GetState().SetValue("user.UserProfile", profile);
        }
    }
}
