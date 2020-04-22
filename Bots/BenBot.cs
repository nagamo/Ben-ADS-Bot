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
        private const string WelcomeMessage = "Hey there! I'm Chad, and though I'm not human, I think I can be very helpful! " +
                        "At any time, type in a question and I'll do my best to answer. You can also opt out at any point by simply " +
                        "typing 'Quit', or 'Cancel' or 'Stop' or any phrase that gets the point across! I love talking shop, " +
                        "so if you ask about financing or trade-ins, I've got some specific areas to explore with you.";

        private readonly DialogSet dialogs;
        private BotState _userState;
        private BotAccessors _botAccessors;
        //Hey there!

        // Initializes a new instance of the "WelcomeUserBot" class.
        public BenBot(UserState userState, BotAccessors botAccessors, ActiveLeadDialog activeLeadDialog)
        {
            _userState = userState;
            _botAccessors = botAccessors;

            dialogs = new DialogSet(_botAccessors.DialogStateAccessor);

            dialogs.Add(new UserProfileDialog(userState));
            dialogs.Add(activeLeadDialog);
        }


        // This is called when a user is added to the conversation. This occurs BEFORE they've typed something in, so it's a good way to
        //initiate the conversation from the bot's perspective
        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            foreach (var member in membersAdded)
            {
                if (member.Id != turnContext.Activity.Recipient.Id)
                {
                    await turnContext.SendActivityAsync(WelcomeMessage, cancellationToken: cancellationToken);
                }
            }
        }

        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            var userDataAccessor = _userState.CreateProperty<UserProfile>(nameof(UserProfile));
            var userData = await userDataAccessor.GetAsync(turnContext, () => new UserProfile(), cancellationToken);
            
            
            var dialogContext = await dialogs.CreateContextAsync(turnContext, cancellationToken);

            
            if (dialogContext.ActiveDialog != null)
            {
                var turnResult = await dialogContext.ContinueDialogAsync(cancellationToken);
            }
            else
            {
                if (!userData.IsRegistered)
                {
                    await dialogContext.BeginDialogAsync(nameof(UserProfileDialog));
                }
                else
                {
                    await dialogContext.BeginDialogAsync(nameof(ActiveLeadDialog));
                }
            }

            // Save any state changes.
            await _userState.SaveChangesAsync(turnContext);
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
