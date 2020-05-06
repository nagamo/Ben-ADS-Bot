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
    public class JSONVehicleInventoryCardFactory : JSONCardFactory<VehicleInventoryDetails>
    {
        public IBotServices Services { get; }

        public JSONVehicleInventoryCardFactory(IBotServices services)
            : base(nameof(JSONVehicleInventoryCardFactory), Path.Combine(".", "Cards", "json", "inventory-card.json"))
        {
            Services = services;
        }

        internal override async Task<VehicleInventoryDetails> DoPopulate(ITurnContext context, CancellationToken cancellationToken = default)
        {
            var userProfile = await Services.GetUserProfileAsync(context, cancellationToken);

            if (userProfile.VehicleProfile == null)
            {
                userProfile.VehicleProfile = new VehicleInventoryDetails();
            }

            return userProfile.VehicleProfile;
        }

        internal override async Task<bool> DoValidate(VehicleInventoryDetails submission, ITurnContext context, CancellationToken cancellationToken)
        {
            return submission.IsCompleted;
        }

        internal override async Task DoFinalize(VehicleInventoryDetails submission, ITurnContext context, CancellationToken cancellationToken)
        {
            var currentProfiles = await Services.GetUserProfileAsync(context, cancellationToken);

            currentProfiles.VehicleProfile = submission;

            //await Services.SetUserProfileAsync(currentProfiles, context, cancellationToken);

            await context.SendActivityAsync($"Beep boop. Checking inventory for {submission.Model}!\nJust kidding, I can't do that yet. :)", cancellationToken: cancellationToken);
        }
    }
}
