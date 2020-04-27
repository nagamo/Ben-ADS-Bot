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
    public class JSONProfileCardFactory : JSONCardFactory<BasicDetails>
    {
        public IBotServices Services { get; }

        public JSONProfileCardFactory(IBotServices services)
            : base(nameof(JSONProfileCardFactory), Path.Combine(".", "Cards", "json", "profile-card.json"))
        {
            Services = services;
        }

        internal override async Task<BasicDetails> DoPopulate(ITurnContext context, CancellationToken cancellationToken = default)
        {
            var userProfile = await Services.GetUserProfileAsync(context, cancellationToken);

            if (userProfile.Details == null)
            {
                userProfile.Details = new BasicDetails()
                {
                    Name = "Test",
                    Phone = "Data",
                    Email = "Saving",
                    Focus = "1",
                    Timeframe = "1"
                };
            }

            return userProfile.Details;
        }

        internal override async Task<bool> DoValidate(BasicDetails submission, ITurnContext context, CancellationToken cancellationToken)
        {
            return submission.Name != "Test";
        }

        internal override async Task DoFinalize(BasicDetails submission, ITurnContext context, CancellationToken cancellationToken)
        {
            var currentProfiles = await Services.GetUserProfileAsync(context, cancellationToken);

            currentProfiles.Details = submission;

            await Services.SetUserProfileAsync(currentProfiles, context, cancellationToken);

            await context.SendActivityAsync($"Thanks so much, {currentProfiles.Details.Name}!", cancellationToken: cancellationToken);
        }
    }
}
