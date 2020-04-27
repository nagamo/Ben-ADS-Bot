using AdaptiveCards;
using ADS.Bot.V1.Models;
using ADS.Bot1;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ADS.Bot.V1.Cards
{
    public class ProfileCardFactory : ICardFactory<BasicDetails>
    {
        public string Id { get => nameof(ProfileCardFactory); }
        public IBotServices BotServices { get; }

        public ProfileCardFactory(IBotServices botServices)
        {
            BotServices = botServices;
        }

        public AdaptiveCard CreateCard(BasicDetails initalDetails, ITurnContext context, CancellationToken cancellationToken = default)
        {
            return new AdaptiveCard(AdaptiveCard.KnownSchemaVersion)
            {
                Body = new List<AdaptiveElement>()
                {
                    new AdaptiveTextBlock("Name"),
                    new AdaptiveTextInput()
                    {
                        Id = "Name",
                        Value = initalDetails.Name,
                        Placeholder = "Enter Name"
                    },
                    new AdaptiveTextBlock("Phone"),
                    new AdaptiveTextInput()
                    {
                        Id = "Phone",
                        Value = initalDetails.Phone,
                        Placeholder = "(xxx)xxx-xxxx"
                    },
                    new AdaptiveTextBlock("Email"),
                    new AdaptiveTextInput()
                    {
                        Id="Email",
                        Value = initalDetails.Email,
                        Placeholder = "buy@the.car"
                    }
                },
                Actions = new List<AdaptiveAction>()
                {
                    new AdaptiveSubmitAction()
                    {
                        Title = "Submit",
                        Data = new
                        {
                            card_id = nameof(ProfileCardFactory),
                            id = "submit"
                        },
                        Id = "submit",
                        Style = "Positive"
                    },
                    new AdaptiveShowCardAction()
                    {
                        Id = "test",
                        Title = "Details",
                        Card = new AdaptiveCard(AdaptiveCard.KnownSchemaVersion)
                        {
                            Body = new List<AdaptiveElement>()
                            {
                                new AdaptiveTextBlock("Hey! Look at me :)")
                            }
                        }
                    }
                }
            };
        }

        public BasicDetails Populate(ITurnContext context, CancellationToken cancellationToken = default)
        {
            return new BasicDetails()
            {
                Name = "Test",
                Phone = "If",
                Email = "Prefilled",
                Focus = "???",
                Timeframe = "timef"
            };
        }

        async Task<bool> ICardFactory<BasicDetails>.Validate(BasicDetails submission, ITurnContext context, CancellationToken cancellationToken)
        {
            return submission.Name != "Test";
        }

        async Task ICardFactory<BasicDetails>.Finalize(BasicDetails submission, ITurnContext context, CancellationToken cancellationToken)
        {
            await context.SendActivityAsync("Thank you!", cancellationToken: cancellationToken);
        }
    }
}
