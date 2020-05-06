using ADS.Bot.V1.Models;
using ADS.Bot1;
using Microsoft.Bot.Builder;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ADS.Bot.V1.Cards
{
    public class JSONTradeInCardFactory : JSONCardFactory<TradeInDetails>
    {
        public IBotServices Services { get; }

        public JSONTradeInCardFactory(IBotServices services)
            : base(nameof(JSONTradeInCardFactory), Path.Combine(".", "Cards", "json", "trade-card.json"))
        {
            Services = services;
        }

        internal override async Task<TradeInDetails> DoPopulate(ITurnContext context, CancellationToken cancellationToken = default)
        {
            var userProfile = await Services.GetUserProfileAsync(context, cancellationToken);

            if (userProfile.Details == null)
            {
                userProfile.TradeDetails = new TradeInDetails();
            }

            return userProfile.TradeDetails;
        }

        internal override async Task<bool> DoValidate(TradeInDetails submission, ITurnContext context, CancellationToken cancellationToken)
        {
            return submission.IsCompleted;
        }

        internal override async Task DoFinalize(TradeInDetails submission, ITurnContext context, CancellationToken cancellationToken)
        {
            var currentProfiles = await Services.GetUserProfileAsync(context, cancellationToken);

            currentProfiles.TradeDetails = submission;

            //await Services.SetUserProfileAsync(currentProfiles, context, cancellationToken);

            await context.SendActivityAsync($"Thanks so much, {currentProfiles.Details.Name}!", cancellationToken: cancellationToken);
        }
    }
}
