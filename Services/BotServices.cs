// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using ADS.Bot.V1;
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
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using ZCRMSDK.CRM.Library.CRMException;
using ZCRMSDK.CRM.Library.Setup.RestClient;
using ZCRMSDK.OAuth.Client;

namespace ADS.Bot1
{
    public class ADSBotServices
    {
        public ADSBotServices(IConfiguration configuration, ConversationState conversationState, 
            UserState userState, CRMService crmService, DataService dataService, DealerConfigService dealerConfig)
        {
            ConversationState = conversationState;
            Configuration = configuration;
            DataService = dataService;
            DealerConfig = dealerConfig;
            UserState = userState;
            CRM = crmService;

            CRM.Services = this;

            UserProfileAccessor = UserState.CreateProperty<UserProfile>(nameof(UserProfile));
            DialogStateAccessor = ConversationState.CreateProperty<DialogState>(nameof(DialogState));
            //Used by micrsoft dialog classes
            GenericUserProfileAccessor = UserState.CreateProperty<Dictionary<string, object>>(nameof(UserProfile));

            QnAOptions = new QnAMakerOptions();

            LeadQualQnA = new QnAMaker(
                new QnAMakerEndpoint
                {
                    KnowledgeBaseId = configuration["qna:QnAKnowledgebaseId"],
                    EndpointKey = configuration["qna:QnAEndpointKey"],
                    Host = configuration["qna:QnAEndpointHostName"]
                }, QnAOptions);

            //Apparently the constructor overrides the properties with defaults?
            QnAOptions.ScoreThreshold = 0;

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

        public QnAMakerOptions QnAOptions { get; private set; }
        public QnAMaker LeadQualQnA { get; private set; }

        public IStatePropertyAccessor<Dictionary<string, object>> GenericUserProfileAccessor { get; private set; }
        public IStatePropertyAccessor<UserProfile> UserProfileAccessor { get; private set; }
        public IStatePropertyAccessor<DialogState> DialogStateAccessor { get; private set; }

        public IConfiguration Configuration { get; }


        public CRMService CRM { get; }
        public DataService DataService { get; }
        public DealerConfigService DealerConfig { get; }



        public LuisRecognizer LuisRecognizer { get; private set; }

        private ConversationState ConversationState { get; set; }
        private UserState UserState { get; set; }

        public async Task<UserProfile> GetUserProfileAsync(ITurnContext turnContext
            , CancellationToken cancellationToken = default)
        {
            return await UserProfileAccessor.GetAsync(turnContext, () => new UserProfile(), cancellationToken);
        }

        public async Task SaveUserProfileAsync(UserProfile profile, ITurnContext turnContext
            , CancellationToken cancellationToken = default)
        {
            //Update accessors with latest version
            await UserState.SaveChangesAsync(turnContext, cancellationToken: cancellationToken);
        }

        public async Task SetUserProfileAsync(UserProfile profile, DialogContext dialogContext
            , CancellationToken cancellationToken = default)
        {
            //Also update the dialog contexts state
            await UserProfileAccessor.SetAsync(dialogContext.Context, profile, cancellationToken);
            dialogContext.GetState().SetValue("user.UserProfile", profile);
        }
    }
}
