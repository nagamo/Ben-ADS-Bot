﻿using ADS.Bot.V1.Dialogs;
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
        private const string WelcomeMessage = "Hey there! I'm Chad. Welcome!";

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
                    await turnContext.SendActivityAsync(WelcomeMessage, cancellationToken: cancellationToken);
                    await DialogManager.OnTurnAsync(turnContext, cancellationToken);
                }
            }

            if(bool.TryParse(Services.Configuration["debug_messages"], out var debug_msg) && debug_msg)
            {
                await turnContext.SendActivityAsync(JsonConvert.SerializeObject(turnContext.Activity));
            }

            await Services.UserProfileAccessor.SetAsync(turnContext, userProfile, cancellationToken);
        }
        
        // This is the primary message handler
        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            var userProfile = await Services.GetUserProfileAsync(turnContext, cancellationToken);


            if (bool.TryParse(Services.Configuration["debug_messages"], out var debug_msg) && debug_msg)
            {
                await turnContext.SendActivityAsync(JsonConvert.SerializeObject(turnContext.Activity));
            }

            //Let the manager handle passing our message to the one-and-only dialog
            var dialogResult = await DialogManager.OnTurnAsync(turnContext, cancellationToken);

            // Save any state changes.
            await Services.UserProfileAccessor.SetAsync(turnContext, userProfile, cancellationToken);
            await User.SaveChangesAsync(turnContext, cancellationToken: cancellationToken);


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
