using ADS.Bot.V1.Dialogs;
using ADS.Bot1;
using ADS.Bot1.Dialogs;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ADS.Bot.V1.Bots
{
    public class BenBot : ActivityHandler
    {
        // General messages sent to the user.
        private const string WelcomeMessage = "Hey there! I'm Chad. Welcome!";

        private DialogManager DialogManager;
        //Hey there!

        public IBotServices Services { get; }

        // Initializes a new instance of the "WelcomeUserBot" class.
        public BenBot(IBotServices services, RootDialog dialog)
        {
            Services = services;

            DialogManager = new DialogManager(dialog);
        }


        // This is called when a user is added to the conversation. This occurs BEFORE they've typed something in, so it's a good way to
        //initiate the conversation from the bot's perspective
        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            var userProfile = await Services.GetUserProfileAsync(turnContext, cancellationToken);

            foreach (var member in membersAdded)
            {
                if (member.Id != turnContext.Activity.Recipient.Id)
                {
                    await turnContext.SendActivityAsync(WelcomeMessage, cancellationToken: cancellationToken);
                    await DialogManager.OnTurnAsync(turnContext, cancellationToken);
                }
            }

            await Services.UserProfileAccessor.SetAsync(turnContext, userProfile, cancellationToken);
        }
        
        // This is the primary message handler
        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            var userProfile = await Services.GetUserProfileAsync(turnContext, cancellationToken);

            //Let the manager handle passing our message to the one-and-only dialog
            await DialogManager.OnTurnAsync(turnContext, cancellationToken);

            // Save any state changes.
            await Services.UserProfileAccessor.SetAsync(turnContext, userProfile, cancellationToken);
        }

        /*private static async Task SendIntroCardAsync(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            var card = new HeroCard();
            card.Title = "Welcome to Bot Framework!";
            card.Text = @"Welcome to Welcome Users bot sample! This Introduction card
                         is a great way to introduce your Bot to the user and suggest
                         some things to get them started. We use this opportunity to
                         recommend a few next steps for learning more creating and deploying bots.";
            card.Images = new List<CardImage>() { new CardImage("https://aka.ms/bf-welcome-card-image") };
            card.Buttons = new List<CardAction>()
            {
                new CardAction(ActionTypes.OpenUrl, "Get an overview", null, "Get an overview", "Get an overview", "https://docs.microsoft.com/en-us/azure/bot-service/?view=azure-bot-service-4.0"),
                new CardAction(ActionTypes.OpenUrl, "Ask a question", null, "Ask a question", "Ask a question", "https://stackoverflow.com/questions/tagged/botframework"),
                new CardAction(ActionTypes.OpenUrl, "Learn how to deploy", null, "Learn how to deploy", "Learn how to deploy", "https://docs.microsoft.com/en-us/azure/bot-service/bot-builder-howto-deploy-azure?view=azure-bot-service-4.0"),
            };

            var response = MessageFactory.Attachment(card.ToAttachment());
            await turnContext.SendActivityAsync(response, cancellationToken);
        }*/
    }
}
