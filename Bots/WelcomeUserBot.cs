using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ADS.Bot1.Dialogs;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;

namespace ADS.Bot1.Bots
{
    // Represents a bot that processes incoming activities.
    // For each user interaction, an instance of this class is created and the OnTurnAsync method is called.
    // This is a Transient lifetime service. Transient lifetime services are created
    // each time they're requested. For each Activity received, a new instance of this
    // class is created. Objects that are expensive to construct, or have a lifetime
    // beyond the single turn, should be carefully managed.
    // For example, the "MemoryStorage" object and associated
    // IStatePropertyAccessor{T} object are created with a singleton lifetime.
    public class WelcomeUserBot : ActivityHandler
    {
        // General messages sent to the user.
        private const string WelcomeMessage = "Welcome! My name is Chad, and though I'm not human, I think I can be very helpful! " +
                        "At any time, type in a question and I'll do my best to answer. You can also opt out at any point by simply " +
                        "typing 'Quit', or 'Cancel' or 'Stop' or any phrase that gets that point across! For your convenience, " +
                        "I've offered a few likely topics for easy selection. Feel free to select one of those.";

        private const string InfoMessage = "You are seeing this message because the bot received at least one " +
                                            "'ConversationUpdate' event, indicating you (and possibly others) " +
                                            "joined the conversation. If you are using the emulator, pressing " +
                                            "the 'Start Over' button to trigger this event again. The specifics " +
                                            "of the 'ConversationUpdate' event depends on the channel. You can " +
                                            "read more information at: " +
                                            "https://aka.ms/about-botframework-welcome-user";

     
        private readonly DialogSet dialogs;

        private BotState _userState;
        private BotAccessors _botAccessors;

        // Initializes a new instance of the "WelcomeUserBot" class.
        public WelcomeUserBot(UserState userState, BotAccessors botAccessors)
        {
            _userState = userState;
            _botAccessors = botAccessors;

            dialogs = new DialogSet(_botAccessors.DialogStateAccessor);
            //dialogs.Add(FinanceDialog.Instance);
            dialogs.Add(TradeDialog.Instance);
            dialogs.Add(InventoryDialog.Instance);
        }


        // Greet when users are added to the conversation.
        // Note that all channels do not send the conversation update activity.
        // If you find that this bot works in the emulator, but does not in
        // another channel the reason is most likely that the channel does not
        // send this activity.
        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            foreach (var member in membersAdded)
            {
                if (member.Id != turnContext.Activity.Recipient.Id)
                {
                    await turnContext.SendActivityAsync(WelcomeMessage, cancellationToken: cancellationToken);
                    await turnContext.SendActivityAsync(InfoMessage, cancellationToken: cancellationToken);
                    //await turnContext.SendActivityAsync(PatternMessage, cancellationToken: cancellationToken);
                }
            }
        }

        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            var welcomeUserStateAccessor = _userState.CreateProperty<WelcomeUserState>(nameof(WelcomeUserState));
            var didBotWelcomeUser = await welcomeUserStateAccessor.GetAsync(turnContext, () => new WelcomeUserState());

            if (!didBotWelcomeUser.DidBotWelcomeUser)
            {
                didBotWelcomeUser.DidBotWelcomeUser = true;

                // the channel should send the user name in the 'From' object
                var userName = turnContext.Activity.From.Name;

                await turnContext.SendActivityAsync($"Hello, {userName!}", cancellationToken: cancellationToken);
            }
            else
            {
                // This example hardcodes specific utterances. You should use LUIS or QnA for more advance language understanding.
                var text = turnContext.Activity.Text.ToLowerInvariant();
                switch (text)
                {
                    case "hello":
                    case "hi":
                    case "yo":
                        await turnContext.SendActivityAsync($"{text} right back at ya!", cancellationToken: cancellationToken);
                        break;
                    case "intro":
                    case "help":
                        await SendIntroCardAsync(turnContext, cancellationToken);
                        break;
                    case "Financing":
                    case "Get financed":
                        await turnContext.SendActivityAsync(WelcomeMessage, cancellationToken: cancellationToken);
                        break;
                }
            }

            // Save any state changes.
            await _userState.SaveChangesAsync(turnContext);
        }

        private static async Task SendIntroCardAsync(ITurnContext turnContext, CancellationToken cancellationToken)
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
        }
    }
}

