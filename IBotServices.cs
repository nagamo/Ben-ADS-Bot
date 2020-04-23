// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.Luis;
using Microsoft.Bot.Builder.AI.QnA;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using System.Threading;
using System.Threading.Tasks;

namespace ADS.Bot1
{
    public interface IBotServices
    {
        ConversationState ConversationState { get; }
        UserState UserState { get; }

        IStatePropertyAccessor<UserProfile> UserProfileAccessor { get; }
        IStatePropertyAccessor<DialogState> DialogStateAccessor { get; }

        LuisRecognizer Dispatch { get; }
        QnAMaker LeadQualQnA { get; }

        Task<UserProfile> GetUserProfileAsync(ITurnContext turnContext, CancellationToken cancellationToken);
    }
}
