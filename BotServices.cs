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
    public class Services : IBotServices
    {
        public Services(IConfiguration configuration, ConversationState conversationState, UserState userState)
        {
            Configuration = configuration;
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

        public QnAMaker LeadQualQnA { get; private set; }

        public IStatePropertyAccessor<UserProfile> UserProfileAccessor { get; private set; }
        public IStatePropertyAccessor<DialogState> DialogStateAccessor { get; private set; }

        public IConfiguration Configuration { get; }

        private ConversationState ConversationState { get; set; }
        private UserState UserState { get; set; }

        public async Task<UserProfile> GetUserProfileAsync(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            return await UserProfileAccessor.GetAsync(turnContext, () => new UserProfile(), cancellationToken);
        }

        public async Task SetUserProfileAsync(UserProfile profile, ITurnContext turnContext, CancellationToken cancellationToken)
        {
            await UserProfileAccessor.SetAsync(turnContext, profile, cancellationToken);
        }
    }
}
