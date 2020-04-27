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
    public class JSONFinanceCardFactory : JSONCardFactory<FinancingDetails>
    {
        public IBotServices Services { get; }

        public JSONFinanceCardFactory(IBotServices services)
            : base(nameof(JSONFinanceCardFactory), Path.Combine(".", "Cards", "json", "finance-card.json"))
        {
            Services = services;
        }

        internal override async Task<FinancingDetails> DoPopulate(ITurnContext context, CancellationToken cancellationToken = default)
        {
            var userProfile = await Services.GetUserProfileAsync(context, cancellationToken);

            if (userProfile.Financing == null)
            {
                userProfile.Financing = new FinancingDetails();
            }

            return userProfile.Financing;
        }

        internal override async Task<bool> DoValidate(FinancingDetails submission, ITurnContext context, CancellationToken cancellationToken)
        {
            return submission.IsCompleted;
        }

        internal override async Task DoFinalize(FinancingDetails submission, ITurnContext context, CancellationToken cancellationToken)
        {
            var currentProfiles = await Services.GetUserProfileAsync(context, cancellationToken);

            currentProfiles.Financing = submission;

            await Services.SetUserProfileAsync(currentProfiles, context, cancellationToken);

            await context.SendActivityAsync($"Thanks so much, {currentProfiles.Name}!", cancellationToken: cancellationToken);
        }
    }
}
