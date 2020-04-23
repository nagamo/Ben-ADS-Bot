// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.Luis;
using Microsoft.Bot.Builder.AI.QnA;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using System.Threading;
using System.Threading.Tasks;

namespace ADS.Bot1
{
    public class BotServices : IBotServices
    {
        public BotServices(IConfiguration configuration, ConversationState conversationState, UserState userState)
        {
            // Read the setting for cognitive services (LUIS, QnA) from the appsettings.json
            // If includeApiResults is set to true, the full response from the LUIS api (LuisResult)
            // will be made available in the properties collection of the RecognizerResult

            var luisApplication = new LuisApplication(
                configuration["luis:id"],
                configuration["luis:endpointKey"],
                configuration["luis:endpoint"]);

            // Set the recognizer options depending on which endpoint version you want to use.
            // More details can be found in https://docs.microsoft.com/en-gb/azure/cognitive-services/luis/luis-migration-api-v3
            var recognizerOptions = new LuisRecognizerOptionsV2(luisApplication)
            {
                IncludeAPIResults = true,
                PredictionOptions = new LuisPredictionOptions()
                {
                    IncludeAllIntents = true,
                    IncludeInstanceData = true
                }
            };

            Dispatch = new LuisRecognizer(recognizerOptions);
            ConversationState = conversationState;
            UserState = userState;

            UserProfileAccessor = UserState.CreateProperty<UserProfile>(nameof(UserProfile));
            DialogStateAccessor = ConversationState.CreateProperty<DialogState>(nameof(DialogState));

            
            LeadQualQnA = new QnAMaker(new QnAMakerEndpoint
            {
                KnowledgeBaseId = configuration["qna:QnAKnowledgebaseId"],
                EndpointKey = configuration["qna:QnAEndpointKey"],
                Host = configuration["qna:QnAEndpointHostName"]
            });
        }

        public LuisRecognizer Dispatch { get; private set; }
        public QnAMaker LeadQualQnA { get; private set; }

        public IStatePropertyAccessor<UserProfile> UserProfileAccessor { get; private set; }
        public IStatePropertyAccessor<DialogState> DialogStateAccessor { get; private set; }

        public ConversationState ConversationState { get; private set; }
        public UserState UserState { get; private set; }

        public async Task<UserProfile> GetUserProfileAsync(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            return await UserProfileAccessor.GetAsync(turnContext, () => new UserProfile(), cancellationToken);
        }
    }
}
