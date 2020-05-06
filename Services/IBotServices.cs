// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using ADS.Bot.V1.Models;
using ADS.Bot.V1.Services;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.Luis;
using Microsoft.Bot.Builder.AI.QnA;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ADS.Bot1
{
    public interface IBotServices
    {
        IStatePropertyAccessor<Dictionary<string, object>> GenericUserProfileAccessor { get; }
        IStatePropertyAccessor<UserProfile> UserProfileAccessor { get; }
        IStatePropertyAccessor<DialogState> DialogStateAccessor { get; }

        IConfiguration Configuration { get; }

        ZohoBotService Zoho { get; }

        QnAMaker LeadQualQnA { get; }

        Task<UserProfile> GetUserProfileAsync(ITurnContext turnContext, CancellationToken cancellationToken = default);
        Task SetUserProfileAsync(UserProfile profile, ITurnContext turnContext, CancellationToken cancellationToken = default);
    }
}
