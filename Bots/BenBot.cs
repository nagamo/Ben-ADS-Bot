using ADS.Bot.V1.Dialogs;
using ADS.Bot1;
using ADS.Bot1.Dialogs;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;
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
        private const string WelcomeSimple = "Hey there! I'm Chad. Welcome!";
        private const string WelcomePersonal = "Hey there {0}! I'm Chad. Let's get started!";
        private const string WelcomeReturn = "Welcome back {0}! I'm Chad. What can I help you with today?";

        private DialogManager DialogManager;
        //Hey there!

        public IBotServices Services { get; }
        public UserState User { get; }

        // Initializes a new instance of the "WelcomeUserBot" class.
        public BenBot(IBotServices services, ActiveLeadDialog dialog, UserState user)
        {
            Services = services;
            User = user;
            DialogManager = new DialogManager(dialog)
            {
                UserState = user
            };
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
                    //Handle having a users name from the conversation metadata already.
                    if(userProfile.Details == null)
                    {
                        if(!string.IsNullOrWhiteSpace(turnContext.Activity.From.Name) && turnContext.Activity.From.Name != "User")
                        {
                            userProfile.Details = new Models.BasicDetails()
                            {
                                Name = turnContext.Activity.From.Name
                            };
                        }
                    }

                    //Print a personalized hello when we have their information already
                    //And even more "friendly" when we have already converted them as a lead before
                    if(userProfile.ADS_CRM_ID.HasValue)
                    {
                        await turnContext.SendActivityAsync(string.Format(WelcomeReturn, userProfile.FirstName), cancellationToken: cancellationToken);
                    }
                    else if(userProfile.Details != null)
                    {
                        await turnContext.SendActivityAsync(string.Format(WelcomePersonal, userProfile.FirstName), cancellationToken: cancellationToken);
                    }
                    else
                    {
                        await turnContext.SendActivityAsync(WelcomeSimple, cancellationToken: cancellationToken);
                    }
                    await DialogManager.OnTurnAsync(turnContext, cancellationToken);
                }
            }

            if(bool.TryParse(Services.Configuration["debug_messages"], out var debug_msg) && debug_msg)
            {
                await turnContext.SendActivityAsync(JsonConvert.SerializeObject(turnContext.Activity));
            }

            await Services.UserProfileAccessor.SetAsync(turnContext, userProfile, cancellationToken);
        }

        protected override async Task OnEventActivityAsync(ITurnContext<IEventActivity> turnContext, CancellationToken cancellationToken)
        {
            if (bool.TryParse(Services.Configuration["debug_messages"], out var debug_msg) && debug_msg)
            {
                await turnContext.SendActivityAsync(JsonConvert.SerializeObject(turnContext.Activity));
            }
        }

        // This is the primary message handler
        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            if (bool.TryParse(Services.Configuration["debug_messages"], out var debug_msg) && debug_msg)
            {
                await turnContext.SendActivityAsync(JsonConvert.SerializeObject(turnContext.Activity));
            }

            //Get the latest version
            var userProfile = await Services.GetUserProfileAsync(turnContext, cancellationToken);
            
            //Let the manager handle passing our message to the one-and-only dialog
            var dialogResult = await DialogManager.OnTurnAsync(turnContext, cancellationToken);
            await Services.SaveUserProfileAsync(userProfile, turnContext, cancellationToken);

            switch (dialogResult.TurnResult.Status)
            {
                case DialogTurnStatus.Complete:
                    //End of conversation here....
                    if(dialogResult.TurnResult.Result != null)
                    {

                    }
                    break;
            }
        }
    }
}
